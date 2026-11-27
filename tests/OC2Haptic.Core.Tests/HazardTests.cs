using System;
using System.Collections.Generic;
using OC2Haptic.Core;

internal static class HazardTests
{
    internal static void Add(Dictionary<string,Action> tests)
    {
        tests.Add("v16 fire spread is one incident and reminders have five second floor", Fire);
        tests.Add("v16 fire clear removes queued relevance and no resume replay", Clear);
        tests.Add("v16 cooking warning burned and removal are separate edges", Cooking);
        tests.Add("v16 only a proved single team output can infer delivery device", Mapping);
        tests.Add("v16 hazard cancellation does not stop unrelated output", Cancellation);
        tests.Add("v16 first score epoch preserves current hazard while true replacement stops it", RoundBoundary);
    }
    private static void Check(bool b){V16Tests.Check(b);}
    private static void Fire()
    {
        var t=new HazardStateTracker();var start=t.ObserveFire(10,true,0);
        Check(start.Started && !start.Cleared && start.Incident==1);
        Check(!t.ObserveFire(10,true,1).Started && !t.ObserveFire(11,true,2).Started);
        Check(!t.ReminderDue(4999) && t.ReminderDue(5000) && !t.ReminderDue(5001) && t.ReminderDue(10000));
        Check(!t.ObserveFire(10,false,10001).Cleared && t.IsCurrent(GameEventType.FireReminder,1));
        Check(t.ObserveFire(11,false,10002).Cleared && !t.ReminderDue(20000));
        Check(t.ObserveFire(12,true,20001).Incident==2);
    }
    private static void Clear()
    {
        var t=new HazardStateTracker();t.ObserveFire(1,true,0);t.DeferReminders(10000);
        Check(!t.ReminderDue(10001) && !t.ReminderDue(14999) && t.ReminderDue(15000));
        t.ObserveFire(1,false,15001);Check(!t.IsCurrent(GameEventType.FireStart,1) && !t.ReminderDue(20001));
        t.Reset();Check(!t.ReminderDue(50000));
    }
    private static void Cooking()
    {
        var t=new HazardStateTracker();Check(t.ObserveCooking(7,CookingHazardState.Warning)==GameEventType.BurnWarning);
        Check(t.ObserveCooking(7,CookingHazardState.Warning)==null && t.IsCurrent(GameEventType.BurnWarning,7));
        Check(t.ObserveCooking(7,CookingHazardState.Burned)==GameEventType.FoodBurned && !t.IsCurrent(GameEventType.BurnWarning,7));
        Check(t.ObserveCooking(7,CookingHazardState.Burned)==null);
        t.ObserveCooking(7,CookingHazardState.None);Check(!t.IsCurrent(GameEventType.FoodBurned,7));
        Check(t.ObserveCooking(7,CookingHazardState.Warning)==GameEventType.BurnWarning);
    }
    private static OutputParticipant P(int pad,bool local=true,bool keyboard=false,int team=0)
    {return new OutputParticipant(team,pad,local,keyboard);}
    private static void Mapping()
    {
        Check(LocalOutputMap.IsSoleOutput(new[]{P(0),P(0)},0,0));
        Check(!LocalOutputMap.IsSoleOutput(new[]{P(0),P(1)},0,0));
        Check(!LocalOutputMap.IsSoleOutput(new[]{P(0),P(0,false)},0,0));
        Check(!LocalOutputMap.IsSoleOutput(new[]{P(0),P(2,keyboard:true)},0,0));
        Check(LocalOutputMap.IsSoleOutput(new[]{P(0),P(1,false,team:1)},0,0));
        Check(!LocalOutputMap.IsSoleOutput(new OutputParticipant[0],0,0));
    }
    private sealed class Motors:IControllerRumbleBackend
    {internal float Left;public bool IsConnected(int i){return true;}public bool SetMotors(int i,float l,float r){Left=l;return true;}}
    private sealed class Sound:IRewardAudioBackend{public bool Play(int s,float v){return true;}public void Stop(){}}
    private static void Cancellation()
    {
        var b=new Motors();var e=new HapticEngine(b);var c=new FeedbackCoordinator(e,new RewardOutput(e,new Sound()));
        var fire=new GameEvent(GameEventType.FireStart,EventScope.LocalTeam,0,0,0,1,true,false,EventConfidence.ConfirmedState,"fixture",0,0);
        Check(c.Emit(fire,new ControllerBinding(0,0),"fire",EventFeedback.For(fire.EventType).Pattern,0,1,1,true));e.Tick(0);Check(b.Left>0);
        c.Cancel(GameEventType.FireStart,2,100);Check(b.Left>0);c.Cancel(GameEventType.FireStart,1,100);Check(b.Left==0);
        Check(c.Preview(GameEventType.TestPulse,0,"test",EventFeedback.For(GameEventType.TestPulse).Pattern,1000,1,1,null));e.Tick(1000);
        c.Cancel(GameEventType.FireStart,1,1010);Check(b.Left>0);e.Tick(1080);Check(b.Left==0);
    }
    private static void RoundBoundary()
    {
        var b=new Motors();var e=new HapticEngine(b);var c=new FeedbackCoordinator(e,new RewardOutput(e,new Sound()));var round=new RoundSession();
        Check(c.Preview(GameEventType.FireStart,0,"fire-before-first-score",EventFeedback.For(GameEventType.FireStart).Pattern,0,1,1,null));e.Tick(0);
        Check(c.ObserveRound(round,1,10,true,100));e.Tick(100);Check(b.Left>0);
        Check(!c.ObserveRound(round,1,10,true,200));e.Tick(200);Check(b.Left>0);
        Check(c.ObserveRound(round,1,11,true,300));Check(b.Left==0);e.Tick(1000);Check(b.Left==0);
    }
}
