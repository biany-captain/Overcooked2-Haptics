using System;
using System.Collections.Generic;
namespace OC2Haptic.Core
{
    // Unity Update is the single output owner. Hook callbacks only enqueue observations.
    public sealed class FeedbackCoordinator
    {
        private readonly HapticEngine engine;
        private readonly RewardOutput rewards;
        private readonly OutputDeviceRegistry devices;
        private readonly Func<int,OutputDevice> describeDevice;
        private readonly State[] states={new State(),new State(),new State(),new State(),new State()};
        private readonly Dictionary<string,double> sounds=new Dictionary<string,double>();
        private bool starHapticsEnabled=true,starAudioEnabled=true,hazardHapticsEnabled=true;
        public string LastDropReason {get;private set;}
        public FeedbackCoordinator(HapticEngine engine,RewardOutput rewards,OutputDeviceRegistry devices=null,Func<int,OutputDevice> describeDevice=null)
        {this.engine=engine;this.rewards=rewards;this.devices=devices??new OutputDeviceRegistry();
            this.describeDevice=describeDevice??(index=>new OutputDevice(ControllerBackendKind.XInput,index.ToString(),null,false));}
        public bool Emit(GameEvent e,ControllerBinding binding,string key,HapticPattern pattern,double now,float intensity,float volume,bool allowed)
        {
            if(!allowed || !EventRouter.IsLocalTarget(e,binding.LocalPad))return Drop("disabled, paused or unresolved local target");
            EventFeedback f=EventFeedback.For(e.EventType);
            if(f.Category!=FeedbackCategory.Negative && f.Category!=FeedbackCategory.Emergency &&
                (now<e.TimestampMs || now-e.TimestampMs>V16Preset.StaleNormalMs))return Drop("stale normal feedback");
            return EmitCore(EventRouter.Resolve(e,binding),e.EventType,key??f.Key(e),e.EntityIdentity,pattern,now,intensity,volume,null,false);
        }
        public bool Preview(GameEventType type,int index,string key,HapticPattern pattern,double now,float intensity,float volume,StarFeedbackMode? mode)
        {return EmitCore(index,type,key,int.MinValue,pattern,now,intensity,volume,mode,true);}
        private bool EmitCore(int index,GameEventType type,string key,int entity,HapticPattern pattern,double now,float intensity,float volume,StarFeedbackMode? preview,bool manual)
        {
            LastDropReason="";
            if(string.IsNullOrEmpty(key) || double.IsNaN(now) || double.IsInfinity(now) || now<0)return Drop("invalid event key/time");
            bool physical=index>=0 && index<4,star=GameEventTypes.IsStar(type);
            EventFeedback feedback=EventFeedback.For(type);
            bool hazardMotorsDisabled=!manual && GameEventTypes.IsHazard(type) && !hazardHapticsEnabled;
            if(!manual)
            {
                if(star)
                {
                    // Reuse the output channel mask without changing the global masters.
                    preview=starHapticsEnabled?(starAudioEnabled?StarFeedbackMode.HapticsAndAudio:StarFeedbackMode.HapticsOnly)
                        :(starAudioEnabled?StarFeedbackMode.AudioOnly:StarFeedbackMode.Disabled);
                    if(preview.Value==StarFeedbackMode.Disabled)return Drop("star feedback disabled");
                }
            }
            bool negative=feedback.Category==FeedbackCategory.Negative || feedback.Category==FeedbackCategory.Emergency;
            if(!physical && !star && !negative)return Drop("unbound physical output");
            State state=states[physical?index:4];Accrue(state,now);
            double previous;
            if(state.Keys.TryGetValue(key,out previous) && ((star && !manual) || now-previous<feedback.CooldownMs))return Drop("event duplicate");
            // Audio/celebration cancellation precedes motor-only suppression. A new
            // confirmed incident must remain audible even when no new rumble can run.
            bool interrupted=negative && rewards.Interrupt(now);
            if(negative && physical && state.Active!=null && GameEventTypes.IsStar(state.Active.Type))
            {engine.Cancel(index);state.Active=null;}
            if(state.Cooldowns.TryGetValue(type,out previous) && now-previous<feedback.CooldownMs)
            {
                if(!interrupted)return Drop("per-device event cooldown");
                state.Keys[key]=now;Prune(state.Keys,now);LastDropReason="motor cooldown; celebration stopped";return true;
            }
            bool haptics=rewards.Mode==StarFeedbackMode.HapticsAndAudio || rewards.Mode==StarFeedbackMode.HapticsOnly;
            if(star && preview.HasValue)haptics=haptics && (preview.Value==StarFeedbackMode.HapticsAndAudio || preview.Value==StarFeedbackMode.HapticsOnly);
            if(hazardMotorsDisabled){intensity=0;LastDropReason="hazard motors disabled; audio priority retained";}
            haptics=haptics && physical && pattern!=null && pattern.HasMotorOutput && intensity>0;
            if(haptics && !devices.TryClaim(index,describeDevice(index)))
            {if(!negative)return Drop("backend ownership unresolved");intensity=0;haptics=false;LastDropReason="motor ownership unresolved; audio priority retained";}
            int bucket=Bucket(feedback.Category);
            int requested=pattern==null?0:ActiveMs(pattern,0,pattern.TotalDurationMs);
            if(haptics && Used(state,bucket,now)+requested>Limit(bucket))
            {if(!star && !negative)return Drop("per-device active-time budget");intensity=0;haptics=false;LastDropReason="motor budget exhausted; audio priority retained";}
            Prune(sounds,now);
            if(star && !manual && sounds.ContainsKey(key))preview=haptics?StarFeedbackMode.HapticsOnly:StarFeedbackMode.Disabled;
            int audioBefore=rewards.AudioEmissionSequence;
            bool accepted=rewards.Emit(index,key,type,pattern,now,intensity,volume,false,preview) || interrupted;
            // Emit may cancel a lower star even when the new pattern produces no output.
            if(physical && state.Active!=null && !engine.IsPlaying(index,state.Active.Key,now))state.Active=null;
            if(!accepted)return Drop("output mode, device or priority refused");
            state.Keys[key]=now;state.Cooldowns[type]=now;Prune(state.Keys,now);
            if(!manual && rewards.AudioEmissionSequence!=audioBefore)sounds[key]=now;
            if(physical && engine.IsPlaying(index,key,now))
                state.Active=new Active{Type=type,Entity=entity,Pattern=pattern,Start=now,RecordedUntil=now,Bucket=bucket};
            else if(physical && state.Active!=null && !engine.IsPlaying(index,state.Active.Key,now))state.Active=null;
            if(state.Active!=null && engine.IsPlaying(index,key,now))state.Active.Key=key;
            return true;
        }
        private bool Drop(string reason){LastDropReason=reason;return false;}
        private static int Bucket(FeedbackCategory category){return category==FeedbackCategory.Emergency?2:category==FeedbackCategory.Negative?1:0;}
        private static int Limit(int bucket){return bucket==2?V16Preset.EmergencyBudgetMs:bucket==1?V16Preset.NegativeBudgetMs:V16Preset.NormalBudgetMs;}
        private static int ActiveMs(HapticPattern pattern,double from,double until)
        {
            double at=0,total=0;
            for(int i=0;i<pattern.Count;i++)
            {var s=pattern.SegmentAt(i);double end=at+s.DurationMs;if(s.Left>0 || s.Right>0)total+=Math.Max(0,Math.Min(end,until)-Math.Max(at,from));at=end;}
            return (int)Math.Ceiling(total);
        }
        private static void Accrue(State state,double now)
        {
            var active=state.Active;
            if(active!=null)
            {
                double until=Math.Min(now,active.Start+active.Pattern.TotalDurationMs),at=active.Start;
                for(int i=0;i<active.Pattern.Count;i++)
                {
                    var s=active.Pattern.SegmentAt(i);double end=at+s.DurationMs;
                    double begin=Math.Max(at,active.RecordedUntil),finish=Math.Min(end,until);
                    if(finish>begin && (s.Left>0 || s.Right>0))state.History.Add(new Interval{Start=begin,End=finish,Bucket=active.Bucket});
                    at=end;
                }
                active.RecordedUntil=Math.Max(active.RecordedUntil,until);
                if(now>=active.Start+active.Pattern.TotalDurationMs)state.Active=null;
            }
            state.History.RemoveAll(interval=>interval.End<=now-10000);
        }
        private static double Used(State state,int bucket,double now)
        {double total=0;foreach(var interval in state.History)if(interval.Bucket==bucket)total+=Math.Max(0,interval.End-Math.Max(interval.Start,now-10000));return total;}
        private static void Prune(Dictionary<string,double> entries,double now)
        {var old=new List<string>();foreach(var pair in entries)if(pair.Value<now-10000)old.Add(pair.Key);foreach(string key in old)entries.Remove(key);}
        public void SetMode(StarFeedbackMode mode,double now)
        {
            foreach(var state in states)Accrue(state,now);
            rewards.Mode=mode;
            if(mode==StarFeedbackMode.AudioOnly || mode==StarFeedbackMode.Disabled)foreach(var state in states)state.Active=null;
        }
        public void SetFeatureSwitches(bool starHaptics,bool starAudio,bool hazardHaptics,double now)
        {
            bool stopStars=starHapticsEnabled && !starHaptics,stopHazards=hazardHapticsEnabled && !hazardHaptics;
            for(int index=0;index<4;index++)
            {
                State state=states[index];Accrue(state,now);
                if(state.Active!=null && ((stopStars && GameEventTypes.IsStar(state.Active.Type)) ||
                    (stopHazards && GameEventTypes.IsHazard(state.Active.Type))))
                {engine.Cancel(index);state.Active=null;}
            }
            if(starAudioEnabled && !starAudio)rewards.Interrupt();
            starHapticsEnabled=starHaptics;starAudioEnabled=starAudio;hazardHapticsEnabled=hazardHaptics;
        }
        public void Cancel(GameEventType type,int entity,double now)
        {
            for(int index=0;index<4;index++)
            {State state=states[index];Accrue(state,now);if(state.Active!=null && state.Active.Type==type && state.Active.Entity==entity){engine.Cancel(index);state.Active=null;}}
        }
        public void Stop(double now)
        {foreach(var state in states){Accrue(state,now);state.Active=null;}rewards.Stop();}
        public void Reset(double now)
        {
            Stop(now);foreach(var state in states){state.Keys.Clear();state.Cooldowns.Clear();state.History.Clear();}
            sounds.Clear();devices.Clear();
        }
        public bool ObserveRound(RoundSession round,int scene,int flow,bool inRound,double now)
        {
            bool previous=round.Key!=null;bool changed=round.Observe(scene,flow,inRound);
            // Initial score capture may follow an already observed fire in this same round.
            if(changed && previous)Reset(now);return changed;
        }
        private sealed class State
        {internal readonly Dictionary<string,double> Keys=new Dictionary<string,double>();internal readonly Dictionary<GameEventType,double> Cooldowns=new Dictionary<GameEventType,double>();internal readonly List<Interval> History=new List<Interval>();internal Active Active;}
        private sealed class Active{internal GameEventType Type;internal int Entity,Bucket;internal HapticPattern Pattern;internal double Start,RecordedUntil;internal string Key;}
        private struct Interval{internal double Start,End;internal int Bucket;}
    }
}
