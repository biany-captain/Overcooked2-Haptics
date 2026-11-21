using System;
using System.Collections.Generic;
using OC2Haptic.Core;

internal static class V14Tests
{
    internal static void Add(Dictionary<string,Action> tests)
    {
        tests.Add("v14 formal star pulses retain exact offsets within850ms",Patterns);
        tests.Add("v14 long audio blocks lower star while higher star replaces it",Replace);
        tests.Add("v14 ordinary action does not stop musical tail",MusicalTail);
        tests.Add("v14 failed audio still permits haptics and later stop",MissingAudio);
        tests.Add("v14 queued feedback waits for drain and cancellation discards pending",Deferred);
    }
    private static void Patterns()
    {
        int[][] offsets={new[]{0,145},new[]{0,175},new[]{0,125,280},new[]{0,145,295,470}};
        int[] durations={180,285,425,660};
        for(int star=1;star<=4;star++)
        {
            var p=EventFeedback.For((GameEventType)((int)GameEventType.StarMilestone1+star-1)).Pattern;
            Check(p.TotalDurationMs==durations[star-1]);int at=0,pulse=0;
            for(int i=0;i<p.Count;i++){var s=p.SegmentAt(i);if(s.Left!=0 || s.Right!=0)Check(at==offsets[star-1][pulse++]);at+=s.DurationMs;}
            Check(pulse==offsets[star-1].Length && p.TotalDurationMs<=850);
        }
        Check(EventFeedback.OneStarHapticsOnly.TotalDurationMs==180);
    }
    private static void Replace()
    {
        var b=new Motors();var e=new HapticEngine(b);var a=new Sound();var o=new RewardOutput(e,a,audioDurationMs:_=>3250);
        Check(o.Emit(0,"old",GameEventType.StarMilestone3,null,0,1,.65f));e.Tick(0);
        Check(!o.Emit(0,"lower",GameEventType.StarMilestone2,null,1000,1,.65f));
        Check(o.Emit(0,"higher",GameEventType.StarMilestone4,null,1000,1,.65f));Check(a.Stops==1 && a.Plays==2);
        o.Stop();e.Tick(5000);Check(b.LastZero && a.Stops==2);
    }
    private static void MusicalTail()
    {
        var e=new HapticEngine(new Motors());var a=new Sound();var o=new RewardOutput(e,a,audioDurationMs:_=>3250);
        Check(o.Emit(0,"star",GameEventType.StarMilestone4,null,0,1,.65f));
        Check(o.Emit(0,"throw",GameEventType.ThrowSuccess,null,1000,1,.65f));Check(a.Plays==1 && a.Stops==0);
        Check(o.Emit(0,"expiry",GameEventType.OrderExpired,null,1100,1,.65f));Check(a.Stops==1);
    }
    private static void MissingAudio()
    {
        var b=new Motors();var e=new HapticEngine(b);var a=new Sound{Fail=true};var o=new RewardOutput(e,a,audioDurationMs:_=>3250);
        Check(o.Emit(0,"star",GameEventType.StarMilestone4,null,0,1,.65f));e.Tick(0);Check(!b.LastZero);
        o.Stop();e.Tick(5000);Check(b.LastZero && a.Stops==1);
        o.Mode=StarFeedbackMode.AudioOnly;Check(!o.Emit(0,"missing",GameEventType.StarMilestone1,null,6000,1,.65f));
    }
    private static void Deferred()
    {
        var q=new FeedbackQueue();int played=0;var ev=new GameEvent(GameEventType.ThrowSuccess,EventScope.LocalPlayer,0,0,0,1,true,false,EventConfidence.ConfirmedState,"fixture");
        q.Enqueue(ev,"one");Check(played==0);q.Drain((e,key)=>{Check(key=="one");played++;});Check(played==1);
        q.Enqueue(ev,"stale");q.Clear();q.Drain((e,key)=>played++);Check(played==1);
    }
    private static void Check(bool b){if(!b)throw new Exception("v1.4 behavior mismatch");}
    private sealed class Sound:IRewardAudioBackend{public int Plays,Stops;public bool Fail;public bool Play(int star,float volume){Plays++;return !Fail;}public void Stop(){Stops++;}}
    private sealed class Motors:IControllerRumbleBackend{public bool LastZero=true;public bool IsConnected(int i){return true;}public bool SetMotors(int i,float l,float r){LastZero=l==0 && r==0;return true;}}
}
