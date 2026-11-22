using System;
using System.Collections.Generic;
using OC2Haptic.Core;

internal static class MenuStrengthTests
{
    internal static void Add(Dictionary<string,Action> tests)
    {
        tests.Add("menu strength default gain preserves profiles", DefaultGain);
        tests.Add("menu strength saturation preserves balance and timing", SafeGain);
        tests.Add("menu strength zero never owns or blocks motors", ZeroGain);
        tests.Add("menu strength nonfinite gains safely mute", InvalidGain);
        tests.Add("menu strength explicit star tests preserve automatic mode", IndependentStarTests);
        tests.Add("channel masters gate actions and explicit previews", MasterGates);
        tests.Add("channel haptics off preserves active music", HapticsOff);
        tests.Add("channel audio off preserves active motors", AudioOff);
    }
    private static void DefaultGain()
    {
        var source=EventFeedback.For(GameEventType.CatchSuccess).Pattern;var scaled=source.WithGain(1);
        Check(source.Count==scaled.Count && source.TotalDurationMs==scaled.TotalDurationMs);
        for(int i=0;i<source.Count;i++){Near(source.SegmentAt(i).Left,scaled.SegmentAt(i).Left);Near(source.SegmentAt(i).Right,scaled.SegmentAt(i).Right);}
    }
    private static void SafeGain()
    {
        var source=new HapticPattern(new HapticSegment(.8f,.4f,100),new HapticSegment(0,0,20),new HapticSegment(.2f,.1f,30));
        var scaled=source.WithGain(2);Near(1,scaled.SegmentAt(0).Left);Near(.5f,scaled.SegmentAt(0).Right);
        Near(.25f,scaled.SegmentAt(2).Left);Near(.125f,scaled.SegmentAt(2).Right);
        Check(scaled.TotalDurationMs==150 && scaled.SegmentAt(1).DurationMs==20);
        Check(HapticPattern.Celebration(new HapticSegment(.5f,.4f,400),new HapticSegment(.4f,.3f,400)).WithGain(2).TotalDurationMs==800);
    }
    private static void ZeroGain()
    {
        var b=new Motors();var engine=new HapticEngine(b);var zero=EventFeedback.For(GameEventType.StarMilestone4).Pattern.WithGain(0);
        Check(!zero.HasMotorOutput);Check(!engine.Play(0,"zero",zero,99,120,0,1));
        Check(engine.Play(0,"action",EventFeedback.For(GameEventType.ThrowSuccess).Pattern,20,120,0,1));engine.Tick(0);
        Check(b.Nonzero==1);
    }
    private static void InvalidGain()
    {
        var source=EventFeedback.For(GameEventType.ThrowSuccess).Pattern;
        Check(!source.WithGain(float.NaN).HasMotorOutput && !source.WithGain(float.PositiveInfinity).HasMotorOutput);
        Check(!source.WithGain(-1).HasMotorOutput);Near(2,HapticPattern.ClampGain(5));
    }
    private static void IndependentStarTests()
    {
        var b=new Motors();var sound=new Sound();var engine=new HapticEngine(b);var output=new RewardOutput(engine,sound);
        output.Mode=StarFeedbackMode.HapticsAndAudio;
        Check(output.Emit(0,"audio",GameEventType.StarMilestone3,null,0,1,.4f,previewMode:StarFeedbackMode.AudioOnly));engine.Tick(0);
        Check(sound.Plays==1 && b.Nonzero==0 && output.Mode==StarFeedbackMode.HapticsAndAudio);
        output.Stop();Check(output.Emit(0,"haptic",GameEventType.StarMilestone2,null,1000,1,.4f,previewMode:StarFeedbackMode.HapticsOnly));engine.Tick(1000);
        Check(sound.Plays==1 && b.Nonzero==1 && output.Mode==StarFeedbackMode.HapticsAndAudio);
        output.Stop();Check(output.Emit(0,"both",GameEventType.StarMilestone3,null,2000,1,.4f,previewMode:StarFeedbackMode.HapticsAndAudio));engine.Tick(2000);
        Check(sound.Plays==2 && b.Nonzero==2 && output.Mode==StarFeedbackMode.HapticsAndAudio);
    }
    private static void MasterGates()
    {
        var b=new Motors();var a=new Sound();var e=new HapticEngine(b);var o=new RewardOutput(e,a);
        o.Mode=StarFeedbackMode.AudioOnly;
        Check(!o.Emit(0,"throw",GameEventType.ThrowSuccess,null,0,1,1));e.Tick(0);Check(b.Nonzero==0);
        Check(!o.Emit(0,"haptic-preview",GameEventType.StarMilestone1,null,1000,1,1,previewMode:StarFeedbackMode.HapticsOnly));
        Check(o.Emit(0,"audio",GameEventType.StarMilestone3,null,2000,1,1));e.Tick(2000);Check(a.Plays==1 && b.Nonzero==0);
        o.Mode=StarFeedbackMode.HapticsOnly;
        Check(!o.Emit(0,"audio-preview",GameEventType.StarMilestone2,null,3000,1,1,previewMode:StarFeedbackMode.AudioOnly));
        Check(o.Emit(0,"catch",GameEventType.CatchSuccess,null,4000,1,1));e.Tick(4000);Check(b.Nonzero==1 && a.Plays==1);
        o.Mode=StarFeedbackMode.Disabled;
        Check(!o.Emit(0,"bypass",GameEventType.StarMilestone3,null,5000,1,1,previewMode:StarFeedbackMode.HapticsAndAudio));
        e.Tick(5000);Check(b.LastZero && a.Plays==1);
    }
    private static void HapticsOff()
    {
        var b=new Motors();var a=new Sound();var e=new HapticEngine(b);var o=new RewardOutput(e,a,audioDurationMs:_=>4050);
        Check(o.Emit(0,"star",GameEventType.StarMilestone4,null,0,1,1));e.Tick(0);Check(!b.LastZero && a.Playing);
        o.Mode=StarFeedbackMode.AudioOnly;Check(b.LastZero && a.Playing && a.Stops==0);
        e.Tick(100);o.Mode=StarFeedbackMode.HapticsAndAudio;e.Tick(200);Check(b.LastZero && a.Playing);
        o.Mode=StarFeedbackMode.Disabled;Check(!a.Playing && a.Stops==1);
    }
    private static void AudioOff()
    {
        var b=new Motors();var a=new Sound();var e=new HapticEngine(b);var o=new RewardOutput(e,a,audioDurationMs:_=>4050);
        Check(o.Emit(0,"star",GameEventType.StarMilestone4,null,0,1,1));e.Tick(0);
        o.Mode=StarFeedbackMode.HapticsOnly;Check(!b.LastZero && !a.Playing && a.Stops==1);
        o.Mode=StarFeedbackMode.HapticsAndAudio;Check(!a.Playing && a.Plays==1);
        e.Tick(700);Check(b.LastZero);
    }
    private static void Near(float a,float b){Check(Math.Abs(a-b)<.00001f);}
    private static void Check(bool b){if(!b)throw new Exception("menu strength behavior mismatch");}
    private sealed class Sound:IRewardAudioBackend {public int Plays,Stops;public bool Playing;public bool Play(int star,float volume){Plays++;Playing=true;return true;}public void Stop(){Stops++;Playing=false;}}
    private sealed class Motors:IControllerRumbleBackend {public int Nonzero;public bool LastZero=true;public bool IsConnected(int i){return true;}
        public bool SetMotors(int i,float l,float r){LastZero=l==0 && r==0;if(!LastZero)Nonzero++;return true;} }
}
