using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OC2Haptic.Core;

internal static class V17Tests
{
    internal static void Add(Dictionary<string,Action> tests)
    {
        tests.Add("v17 star patterns match supplied JSON and retain nonstar profiles", Profiles);
        tests.Add("v17 first two stars never call audio in any output mode", SilentStars);
        tests.Add("v17 ten second celebration releases motors before normal actions and danger", Timeline);
        tests.Add("v17 zero cooldown stars still deduplicate committed correlation", Dedup);
        tests.Add("v17 audio duration and custom descriptors keep legacy four star choices", Assets);
        tests.Add("v17 supplied PCM assets meet approved durations without clipping", ActualAudio);
    }
    private static void Check(bool value){if(!value)throw new Exception("v17 behavior mismatch");}
    private sealed class Motors:IControllerRumbleBackend
    {
        internal float Left,Right;
        public bool IsConnected(int index){return true;}
        public bool SetMotors(int index,float left,float right){Left=left;Right=right;return true;}
    }
    private sealed class Sound:IRewardAudioBackend
    {internal int Plays;internal bool Playing;public bool Play(int star,float volume){Plays++;Playing=true;return true;}public void Stop(){Playing=false;}}
    private static void Profiles()
    {
        using(var json=JsonDocument.Parse(File.ReadAllText("config/star_feedback_v1.7.json")))
        {
            var events=json.RootElement.GetProperty("events");
            for(int star=1;star<=4;star++)
            {
                var source=events.GetProperty("star_"+star+"_milestone");
                var actual=EventFeedback.For((GameEventType)((int)GameEventType.StarMilestone1+star-1));
                Check(actual.Priority==86 && actual.CooldownMs==0 && actual.Category==FeedbackCategory.Positive);
                var steps=source.GetProperty("steps");Check(actual.Pattern.Count==steps.GetArrayLength());int i=0;
                foreach(var step in steps.EnumerateArray())
                {var s=actual.Pattern.SegmentAt(i++);Check(s.DurationMs==step.GetProperty("ms").GetInt32() && s.Left==step.GetProperty("low").GetSingle() && s.Right==step.GetProperty("high").GetSingle());}
            }
        }
        Check(EventFeedback.For(GameEventType.ComboLevel3).Pattern.TotalDurationMs==375 && EventFeedback.For(GameEventType.DeliveryFailure).Pattern.TotalDurationMs==430);
    }
    private static void SilentStars()
    {
        foreach(var mode in new[]{StarFeedbackMode.HapticsAndAudio,StarFeedbackMode.AudioOnly,StarFeedbackMode.HapticsOnly,StarFeedbackMode.Disabled})
        for(int star=1;star<=2;star++)
        {
            var b=new Motors();var a=new Sound();var output=new RewardOutput(new HapticEngine(b),a);output.Mode=mode;
            bool result=output.Emit(0,"star",(GameEventType)((int)GameEventType.StarMilestone1+star-1),null,0,1,1);
            Check(a.Plays==0 && result==(mode==StarFeedbackMode.HapticsAndAudio || mode==StarFeedbackMode.HapticsOnly));
        }
    }
    private static void Timeline()
    {
        var b=new Motors();var e=new HapticEngine(b);var a=new Sound();var output=new RewardOutput(e,a,audioDurationMs:_=>10667);
        Check(output.Emit(0,"four",GameEventType.StarMilestone4,null,0,1,1));e.Tick(0);Check(b.Right>0 && a.Playing);
        e.Tick(630);Check(b.Left==0 && b.Right==0 && a.Playing);
        Check(output.Emit(0,"ordinary",GameEventType.DeliveryOrdered,null,1000,1,1));e.Tick(1000);Check(b.Right>0 && a.Plays==1);
        Check(output.Emit(0,"fire",GameEventType.FireStart,null,1100,1,1));e.Tick(1100);Check(b.Left>.8f && !a.Playing);
        e.Tick(12000);Check(b.Left==0 && b.Right==0 && a.Plays==1);
    }
    private static void Dedup()
    {
        var e=new HapticEngine(new Motors());var c=new FeedbackCoordinator(e,new RewardOutput(e,new Sound()));
        var p=EventFeedback.For(GameEventType.StarMilestone1).Pattern;
        var first=new GameEvent(GameEventType.StarMilestone1,EventScope.LocalTeam,0,0,0,1,true,false,EventConfidence.ConfirmedState,"fixture",0,0);
        Check(c.Emit(first,new ControllerBinding(0,0),"round:team:commit",p,0,1,1,true));e.Tick(55);
        var duplicate=new GameEvent(GameEventType.StarMilestone1,EventScope.LocalTeam,1000,0,0,1,true,false,EventConfidence.ConfirmedState,"fixture",0,0);
        Check(!c.Emit(duplicate,new ControllerBinding(0,0),"round:team:commit",p,1000,1,1,true));
    }
    private static void Assets()
    {
        Check(!StarAudioPolicy.CanPlay(1) && !StarAudioPolicy.CanPlay(2) && StarAudioPolicy.CanPlay(3) && StarAudioPolicy.CanPlay(4));
        Check(StarAudioPolicy.DurationAllowed(3,900,false) && !StarAudioPolicy.DurationAllowed(3,2300,false));
        Check(StarAudioPolicy.DurationAllowed(4,10667,false) && !StarAudioPolicy.DurationAllowed(4,3850,false) && StarAudioPolicy.DurationAllowed(4,3850,true));
        var custom=RewardSoundPack.Custom("three.wav","four.wav");
        Check(custom.Id==RewardSoundPack.CustomStarId && custom.FileFor(1)==null && custom.FileFor(3)=="three.wav" && custom.FileFor(4)=="four.wav");
        Check(StarAudioPolicy.ModeFor(1,StarFeedbackMode.AudioOnly)==StarFeedbackMode.Disabled && StarAudioPolicy.ModeFor(2,StarFeedbackMode.HapticsAndAudio)==StarFeedbackMode.HapticsOnly);
    }
    private static void ActualAudio()
    {
        string[] files={"star3_progress.wav","star4_victory.wav"};int[] expected={900,10667};
        for(int i=0;i<files.Length;i++)using(var file=File.OpenRead("assets/audio/user-v1.7/"+files[i]))
        {
            var pcm=PcmWaveReader.Read(file);Check(pcm.DurationMs==expected[i] && pcm.SampleRate==48000 && pcm.Channels==2 && StarAudioPolicy.DurationAllowed(i+3,pcm.DurationMs,false));
            float peak=0;foreach(float value in pcm.Samples){Check(Math.Abs(value)<1);peak=Math.Max(peak,Math.Abs(value));}Check(peak>.1f);
        }
    }
}
