using System;
using System.Collections.Generic;
using OC2Haptic.Core;

internal static class V13Tests
{
    internal static void Add(Dictionary<string, Action> tests)
    {
        tests.Add("v13 combo first and four tiers use authoritative raw values", ComboTiers);
        tests.Add("v13 combo unknown cannot infer levels from boolean or tips", ComboUnknown);
        tests.Add("v13 combo duplicate and reused order IDs retain real deliveries", ComboIds);
        tests.Add("v13 combo reset teams mode and disabled grading", ComboModes);
        tests.Add("v13 stars precise crossings highest only and no loss replay", StarCrossings);
        tests.Add("v13 stars join restart teams thresholds and unavailable data", StarSessions);
        tests.Add("v13 stars disabled and stale snapshots never replay", StarDisabled);
        tests.Add("v13 synchronous delivery combo star arbitration chooses one", Arbitration);
        tests.Add("v13 profiles distinct rhythms and strict duration limits", Profiles);
        tests.Add("v13 editable patterns reject invalid unsafe values", EditablePatterns);
        tests.Add("v13 original PCM has bounded peaks and silent boundaries", Pcm);
        tests.Add("v13 audio failure preserves rumble and modes stop output", OutputModes);
        tests.Add("v13 danger interrupts celebration without replay", Danger);
        tests.Add("v13 1000 duplicate delivery callbacks remain bounded in output", Burst);
        tests.Add("v13 nested capture finalizers release only their own frame", CaptureOwnership);
        tests.Add("v13 thrown audio failures cannot break rumble or cleanup", AudioExceptions);
        tests.Add("v13 real calibration replay selects first and third star only", CalibrationReplay);
        tests.Add("v13 level-up reads reliable pre-commit and reset snapshots", ComboSnapshots);
    }
    private static DeliveryPayload P(int n, bool combo=true, int mult=1, int streak=1, string round="r", int team=0, bool trusted=true, int order=1)
    { return new DeliveryPayload(round,team,order,n,combo,mult,streak,trusted,n*10); }
    private static ScoreSnapshot S(int score, string round="r", int team=0, double time=0, bool trusted=true, int[] thresholds=null)
    {
        ScoreThresholds set; ScoreThresholds.TryCreate(thresholds ?? new[]{100,200,300},out set);
        return new ScoreSnapshot(round,team,score,set,trusted,time);
    }
    private static void ComboTiers()
    {
        var t=new ComboTracker();
        var expected=new[]{GameEventType.DeliveryOrdered,GameEventType.ComboLevel2,GameEventType.ComboLevel3,GameEventType.ComboLevel4};
        for(int i=1;i<=6;i++) { var r=t.Resolve(P(i,true,Math.Min(i,4),i),true,ComboTriggerMode.EveryOrderedDelivery,4);
            Check(r.Type==expected[Math.Min(i,4)-1] && r.RawStreak==i && r.Tier==Math.Min(i,4)); }
    }
    private static void ComboUnknown()
    {
        var t=new ComboTracker();
        Check(t.Resolve(P(1,true,0,0),true,ComboTriggerMode.EveryOrderedDelivery,4).Type==GameEventType.DeliveryUnclassified);
        Check(t.Resolve(P(2,true,4,8,trusted:false),true,ComboTriggerMode.EveryOrderedDelivery,4).Type==GameEventType.DeliveryUnclassified);
        Check(t.Resolve(P(3,false,0,0),true,ComboTriggerMode.EveryOrderedDelivery,4).Type==GameEventType.DeliveryOutOfOrder);
    }
    private static void ComboIds()
    {
        var t=new ComboTracker(); var a=P(1); Check(!t.Resolve(a,true,ComboTriggerMode.EveryOrderedDelivery,4).Duplicate);
        Check(t.Resolve(a,true,ComboTriggerMode.EveryOrderedDelivery,4).Duplicate);
        Check(!t.Resolve(P(2,order:1),true,ComboTriggerMode.EveryOrderedDelivery,4).Duplicate);
        Check(!t.Resolve(P(3,order:2),true,ComboTriggerMode.EveryOrderedDelivery,4).Duplicate);
    }
    private static void ComboModes()
    {
        var t=new ComboTracker();
        Check(t.Resolve(P(1,true,4,6),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryOrdered);
        Check(t.Resolve(P(2,false,0,0),true,ComboTriggerMode.OnLevelUp,4).Tier==null);
        Check(t.Resolve(P(3,true,1,1),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryOrdered);
        Check(t.Resolve(P(4,true,2,2),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.ComboLevel2);
        Check(t.Resolve(P(5,true,2,3),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryOrdered);
        Check(t.Resolve(P(6,true,4,6),false,ComboTriggerMode.EveryOrderedDelivery,4).Type==GameEventType.DeliveryOrdered);
        Check(t.Resolve(P(7,true,4,7,team:1),true,ComboTriggerMode.EveryOrderedDelivery,2).Type==GameEventType.ComboLevel2);
        Check(t.Resolve(P(1,true,1,1,round:"new"),true,ComboTriggerMode.OnLevelUp,4).Tier==1);
    }
    private static void StarCrossings()
    {
        var t=new StarMilestoneTracker(); Check(t.Observe(S(0))==0); Check(t.Observe(S(99))==0);
        Check(t.Observe(S(100))==1); Check(t.Observe(S(200))==2); Check(t.Observe(S(300))==3);
        Check(t.Observe(S(150))==0); Check(t.Observe(S(300))==0); Check(t.Observe(S(99999))==0);
        t.Reset(); Check(t.Observe(S(0))==0); Check(t.Observe(S(350))==3); Check(t.Observe(S(250))==0);
    }
    private static void StarSessions()
    {
        var t=new StarMilestoneTracker(); Check(t.Observe(S(220))==0); Check(t.Observe(S(310))==3);
        Check(t.Observe(S(0,round:"new"))==0); Check(t.Observe(S(100,round:"new"))==1);
        Check(t.Observe(S(0,team:1))==0); Check(t.Observe(S(100,team:1))==1);
        var four=new[]{100,200,300,500}; t.Prime(S(0,round:"four",thresholds:four));
        Check(t.Observe(S(500,round:"four",thresholds:four))==4);
        Check(t.Observe(S(600,round:"four",thresholds:new[]{150,250,350,550}))==0);
        Check(t.Observe(S(800,round:"bad",trusted:false))==0);
        Check(t.Observe(S(800,round:"bad",thresholds:new[]{0,1,2}))==0);
    }
    private static void StarDisabled()
    {
        var t=new StarMilestoneTracker(); t.Prime(S(0,time:10)); Check(t.Observe(S(200,time:20),false)==0);
        Check(t.Observe(S(200,time:21))==0); Check(t.Observe(S(300,time:15))==0);
        Check(t.Observe(S(300,time:22))==3);
    }
    private static void Arbitration()
    {
        Check(CelebrationCoordinator.Select(GameEventType.ComboLevel3,3)==GameEventType.StarMilestone3);
        Check(CelebrationCoordinator.Select(GameEventType.DeliveryOutOfOrder,0)==GameEventType.DeliveryOutOfOrder);
        Check(CelebrationCoordinator.Select(null,0)==null);
    }
    private static void Profiles()
    {
        Check(EventFeedback.For(GameEventType.CatchSuccess).Pattern.Count==3);
        Check(EventFeedback.For(GameEventType.CatchSuccess).Pattern.TotalDurationMs==115);
        Check(EventFeedback.For(GameEventType.ComboLevel4).Pattern.Count==7);
        Check(EventFeedback.For(GameEventType.StarMilestone4).Pattern.TotalDurationMs==500);
        Check(EventFeedback.OneStarHapticsOnly.Count==3);
        for(int i=1;i<=4;i++) Check(EventFeedback.For((GameEventType)((int)GameEventType.StarMilestone1+i-1)).Pattern.TotalDurationMs<=850);
    }
    private static void EditablePatterns()
    {
        HapticPattern p;
        Check(PatternCodec.TryParse("0.1,0.2,75;0,0,45;0,0.1,25",false,out p) && p.TotalDurationMs==145);
        foreach(var text in new[]{"", "NaN,0.1,40", "1.1,0,40", "0,0,-1", "0.2,0.2,501", "0,0,500;0,1,100"})
            Check(!PatternCodec.TryParse(text,false,out p));
        Check(PatternCodec.TryParse("0.2,0.3,400;0.1,0.2,400",true,out p));
        Check(!PatternCodec.TryParse("0,1,500;0,1,400",true,out p));
    }
    private static void Pcm()
    {
        for(int star=1;star<=4;star++) { var pcm=RewardTone.Generate(star,1000); Check(pcm.Length<=44100 && pcm.Length>0);
            Check(Math.Abs(pcm[0])<.0001 && Math.Abs(pcm[pcm.Length-1])<.0001);
            foreach(float x in pcm) Check(!float.IsNaN(x) && !float.IsInfinity(x) && Math.Abs(x)<=.601); }
    }
    private static void OutputModes()
    {
        var b=new Motors(); var e=new HapticEngine(b); var a=new Sound { Fail=true }; var o=new RewardOutput(e,a);
        Check(o.Emit(0,"s",GameEventType.StarMilestone1,null,0,.75f,.65f)); e.Tick(0); Check(b.Nonzero>0);
        o.Mode=StarFeedbackMode.AudioOnly; Check(a.Stops>0); a.Fail=false;
        int previous=b.Nonzero; Check(o.Emit(0,"a",GameEventType.StarMilestone2,null,1000,.75f,.65f)); e.Tick(1000);
        Check(b.Nonzero==previous && Math.Abs(a.Volume-.65f)<.00001);
        o.Mode=StarFeedbackMode.Disabled; Check(!o.Emit(0,"off",GameEventType.StarMilestone3,null,2000,.75f,1));
        Check(RewardOutput.SafeVolume(float.NaN)==0 && RewardOutput.SafeVolume(2)==1 && RewardOutput.SafeVolume(-1)==0);
    }
    private static void Danger()
    {
        var b=new Motors(); var e=new HapticEngine(b); var a=new Sound(); var o=new RewardOutput(e,a);
        Check(o.Emit(0,"star",GameEventType.StarMilestone4,null,0,.75f,.65f)); e.Tick(0); o.Interrupt();
        var danger=EventFeedback.For(GameEventType.OrderExpired); Check(e.Play(0,"danger",danger.Pattern,85,120,10,.75f));
        Check(!o.Emit(0,"blocked",GameEventType.StarMilestone1,null,11,.75f,.65f)); Check(a.Plays==1 && a.Stops>0);
        e.Tick(1000); Check(b.LastZero);
    }
    private static void Burst()
    {
        var t=new ComboTracker(); int accepted=0; for(int i=0;i<1000;i++) if(!t.Resolve(P(1),true,ComboTriggerMode.EveryOrderedDelivery,4).Duplicate) accepted++;
        Check(accepted==1);
    }
    private static void CaptureOwnership()
    {
        var frames=new CaptureStack<object>();var outer=new object();var inner=new object();
        frames.Push(outer);frames.Push(inner);frames.Release(outer);Check(object.ReferenceEquals(frames.Current,inner));
        frames.Release(inner);frames.Release(inner);Check(object.ReferenceEquals(frames.Current,outer));
        try {throw new Exception("original failure");}catch(Exception){}finally{frames.Release(outer);}
        Check(frames.Current==null);frames.Clear();
    }
    private static void AudioExceptions()
    {
        var b=new Motors();var e=new HapticEngine(b);var sound=new Sound{ThrowOnPlay=true,ThrowOnStop=true};var output=new RewardOutput(e,sound);
        Check(output.Emit(0,"safe",GameEventType.StarMilestone1,null,0,.75f,.65f));e.Tick(0);Check(b.Nonzero>0);
        output.Stop();Check(b.LastZero);
    }
    private static void CalibrationReplay()
    {
        var t=new StarMilestoneTracker();var thresholds=new[]{20,40,60};t.Prime(S(0,thresholds:thresholds));
        int[] scores={28,76,152,207,247,287,327,327};int[] expected={1,3,0,0,0,0,0,0};
        for(int i=0;i<scores.Length;i++)Check(t.Observe(S(scores[i],thresholds:thresholds))==expected[i]);
    }
    private static void ComboSnapshots()
    {
        var t=new ComboTracker();t.ObserveState("r",0,2,true);
        Check(t.Resolve(P(1,true,3,8),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.ComboLevel3);
        t.ObserveState("r",0,0,true);
        Check(t.Resolve(P(2,true,1,1),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryOrdered);
        t.ObserveState("r",0,1,false);
        Check(t.Resolve(P(3,true,3,3),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryOrdered);
    }
    private static void Check(bool b) { if(!b) throw new Exception("v1.3 behavior mismatch"); }
    private sealed class Sound:IRewardAudioBackend { public bool Fail,ThrowOnPlay,ThrowOnStop; public int Plays,Stops; public float Volume;
        public bool Play(int star,float volume){Plays++;Volume=volume;if(ThrowOnPlay)throw new InvalidOperationException("audio unavailable");return !Fail;}
        public void Stop(){Stops++;if(ThrowOnStop)throw new InvalidOperationException("audio stop unavailable");} }
    private sealed class Motors:IControllerRumbleBackend {public int Nonzero; public bool LastZero;
        public bool IsConnected(int i){return true;} public bool SetMotors(int i,float l,float r){LastZero=l==0&&r==0;if(!LastZero)Nonzero++;return true;} }
}
