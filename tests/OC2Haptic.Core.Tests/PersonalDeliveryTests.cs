using System;
using System.Collections.Generic;
using OC2Haptic.Core;

internal static class PersonalDeliveryTests
{
    internal static void Add(Dictionary<string,Action> tests)
    {
        tests.Add("personal confirmed request release score and plate produce one local result", Confirmed);
        tests.Add("personal recorded ownership trace without request never emits", NoRequestTrace);
        tests.Add("personal remote keyboard unknown and wrong team never claim a delivery", InvalidActors);
        tests.Add("personal transferred plate invalidates the original request", Transfer);
        tests.Add("personal changed placement target cannot reuse an old request", TargetChanged);
        tests.Add("personal station acceptance alone never implies successful delivery", NoScore);
        tests.Add("personal two unmatched station scores quarantine rather than guess", Ambiguous);
        tests.Add("personal different plate or conflicting native result stays silent", WrongReceipt);
        tests.Add("personal late confirmed pair and reverse order cannot become a later delivery", Late);
        tests.Add("personal reset drops all pending attribution without replay", Reset);
        tests.Add("personal authoritative rejection keeps the existing negative category", Rejection);
        tests.Add("personal unscoped client rejection drains silently without poisoning next success", UnscopedRejection);
        tests.Add("personal runtime policy rejects team delivery fallback danger and expiry", StrictGate);
        tests.Add("shared star policy allows only confirmed same-team recipients", SharedStarGate);
        tests.Add("shared star receipt cannot duplicate celebration or poison next personal delivery", SharedStarReceipt);
        tests.Add("shared rare alerts allow same-team scope without sharing routine actions", SharedAlertGate);
        tests.Add("personal remote star crossing is consumed instead of replayed on own delivery", StarProgress);
        tests.Add("personal remote combo upgrade is consumed before own same-tier delivery", ComboProgress);
    }
    private static void Check(bool ok){if(!ok)throw new Exception("strict personal attribution mismatch");}
    private static PersonalActor A(int chef=1,bool local=true,bool keyboard=false,int player=0,int pad=0,int team=0)
    {return new PersonalActor(chef,player,pad,team,local,keyboard);}
    private static PersonalDeliveryDecision D(GameEventType? type=GameEventType.DeliveryOrdered,int station=20,int team=0,double at=11,bool trusted=true,string key="r:0:1")
    {return new PersonalDeliveryDecision(team,station,type,key,at,trusted);}
    private static PersonalDeliveryTracker Ready(PersonalActor actor=null)
    {var t=new PersonalDeliveryTracker();actor=actor??A();t.Request(actor,11,20,0);t.Carry(actor,11,0,10);return t;}
    private static void Confirmed()
    {
        var t=Ready();t.Commit(D());var result=t.ReceivePlate(20,11,true,12);
        Check(result!=null && result.Actor.Chef==1 && result.Plate==11 && result.Decision.Correlation=="r:0:1");
        var e=result.ToEvent();Check(e.Scope==EventScope.LocalPlayer && e.EventType==GameEventType.DeliveryOrdered &&
            EventRouter.Resolve(e,new ControllerBinding(0,0))==0 && PersonalFeedbackPolicy.AllowsAutomatic(e));
        Check(t.ReceivePlate(20,11,true,13)==null);
    }
    private static void NoRequestTrace()
    {
        // Native0.6.1 excerpt: these exact IDs/times were observed, but no request was captured.
        var t=new PersonalDeliveryTracker();var local=A(-92212);var remote=A(-83034,false,player:11,pad:-1);
        t.Carry(remote,0,-113274,155263);t.Carry(remote,-113274,0,157053);
        t.Commit(D(station:-77790,at:157055.3));Check(t.ReceivePlate(-77790,-113274,true,157063)==null);
        t.Carry(local,0,-107884,169380);t.Carry(local,-107884,0,171650);
        t.Commit(D(station:-89546,at:171650.4));Check(t.ReceivePlate(-89546,-107884,true,171652)==null);
    }
    private static void InvalidActors()
    {
        foreach(var actor in new[]{A(local:false),A(keyboard:true),A(chef:0),A(player:11),A(pad:-1),A(team:-1)})
        {var t=Ready(actor);t.Commit(D());Check(t.ReceivePlate(20,11,true,12)==null);}
        var wrong=Ready(A(team:1));wrong.Commit(D(team:0));Check(wrong.ReceivePlate(20,11,true,12)==null);
    }
    private static void Transfer()
    {
        var t=Ready();t.Carry(A(2,false,player:11,pad:-1),0,11,10.1);
        t.Carry(A(2,false,player:11,pad:-1),11,0,10.2);t.Commit(D());Check(t.ReceivePlate(20,11,true,12)==null);
        t.Reset();t.Request(A(),11,20,0);t.Carry(A(),11,0,1);t.Carry(A(),0,11,2);t.Carry(A(),11,0,3);
        t.Commit(D(at:4));Check(t.ReceivePlate(20,11,true,5)==null);
    }
    private static void TargetChanged()
    {
        var t=Ready();t.Request(A(),11,0,10.2);t.Commit(D());Check(t.ReceivePlate(20,11,true,12)==null);
        t.Reset();t.Request(A(),11,20,0);t.Request(A(),11,21,1);t.Carry(A(),11,0,10);t.Commit(D());
        Check(t.ReceivePlate(20,11,true,12)==null);
    }
    private static void NoScore()
    {
        var t=Ready();Check(t.ReceivePlate(20,11,true,12)==null);t.Commit(D(at:13));
        t.Request(A(),12,20,14);t.Carry(A(),12,0,15);Check(t.ReceivePlate(20,12,true,16)==null);
        t.Reset();t.Commit(D());Check(t.ReceivePlate(20,0,false,12)==null);
    }
    private static void Ambiguous()
    {
        var t=Ready();t.Commit(D());t.Commit(D(at:11.1,key:"r:0:2"));Check(t.ReceivePlate(20,11,true,12)==null);
        t.Request(A(),12,20,13);t.Carry(A(),12,0,14);t.Commit(D(at:15,key:"r:0:3"));Check(t.ReceivePlate(20,12,true,16)==null);
        t.Request(A(),13,21,20);t.Carry(A(),13,0,21);t.Commit(D(station:21,at:22));Check(t.ReceivePlate(21,13,true,23)!=null);
    }
    private static void WrongReceipt()
    {
        var t=Ready();t.Commit(D());Check(t.ReceivePlate(20,12,true,12)==null && t.ReceivePlate(20,11,true,13)==null);
        t.Reset();t.Request(A(),11,20,0);t.Carry(A(),11,0,10);t.Commit(D());Check(t.ReceivePlate(20,11,false,12)==null);
        t.Reset();t.Request(A(),11,20,0);t.Carry(A(),11,0,10);t.Commit(D(trusted:false));Check(t.ReceivePlate(20,11,true,12)==null);
    }
    private static void Late()
    {
        var t=Ready();t.Commit(D());Check(t.ReceivePlate(20,11,true,192)==null);
        t.Request(A(),12,20,200);t.Carry(A(),12,0,210);t.Commit(D(at:211));Check(t.ReceivePlate(20,12,true,212)==null);
        t.Reset();t.Request(A(),11,20,0);t.Commit(D(at:9));t.Carry(A(),11,0,10);Check(t.ReceivePlate(20,11,true,12)==null);
    }
    private static void Reset()
    {var t=Ready();t.Commit(D());t.Reset();Check(t.ReceivePlate(20,11,true,12)==null);t.Reset();t.Request(A(),11,20,20);t.Carry(A(),11,0,21);t.Commit(D(at:22));Check(t.ReceivePlate(20,11,true,23)!=null);}
    private static void Rejection()
    {var t=Ready();t.Commit(D(GameEventType.DeliveryFailure));var result=t.ReceivePlate(20,11,true,12);Check(result!=null && result.ToEvent().EventType==GameEventType.DeliveryFailure && EventFeedback.For(result.ToEvent().EventType).Category==FeedbackCategory.Negative);}
    private static void UnscopedRejection()
    {
        var t=Ready();t.Commit(D(GameEventType.DeliveryFailure,station:0));Check(t.ReceivePlate(20,11,true,12)==null);
        t.Request(A(),12,20,20);t.Carry(A(),12,0,21);t.Commit(D(at:22));Check(t.ReceivePlate(20,12,true,23)!=null);
        t.Request(A(),14,21,30);t.Carry(A(),14,0,31);t.Commit(D(station:21,at:32));Check(t.ReceivePlate(21,14,true,33)!=null);
        t.Reset();t.Commit(D(GameEventType.DeliveryFailure,station:0));t.Commit(D(GameEventType.DeliveryFailure,station:0,at:12));
        Check(t.ReceivePlate(21,31,true,13)==null && t.ReceivePlate(22,32,true,14)==null);
        t.Request(A(),13,21,20);t.Carry(A(),13,0,21);t.Commit(D(station:21,at:22));Check(t.ReceivePlate(21,13,true,23)!=null);
    }
    private static GameEvent E(GameEventType type,EventScope scope=EventScope.LocalPlayer)
    {return new GameEvent(type,scope,0,0,0,11,true,false,EventConfidence.ConfirmedState,"fixture",0,0);}
    private static void StrictGate()
    {
        Check(!PersonalFeedbackPolicy.AllowsAutomatic(E(GameEventType.DeliveryOrdered,EventScope.LocalTeam)));
        foreach(var type in new[]{GameEventType.OrderExpired,GameEventType.FireStart,GameEventType.BurnWarning,GameEventType.FireReminder})
        {Check(!PersonalFeedbackPolicy.AllowsAutomatic(E(type)));var t=Ready();t.Commit(D(type));Check(t.ReceivePlate(20,11,true,12)==null);}
        Check(PersonalFeedbackPolicy.AllowsAutomatic(E(GameEventType.ThrowSuccess)) && PersonalFeedbackPolicy.AllowsAutomatic(E(GameEventType.CatchSuccess)));
        Check(!PersonalFeedbackPolicy.AllowsAutomatic(new GameEvent(0,0,0,1,false,false,EventConfidence.ConfirmedState,"remote")));
    }
    private static void SharedStarGate()
    {
        for(int level=1;level<=4;level++)
        {
            var type=(GameEventType)((int)GameEventType.StarMilestone1+level-1);
            Check(PersonalFeedbackPolicy.AllowsAutomatic(E(type,EventScope.LocalTeam)));
            Check(!PersonalFeedbackPolicy.AllowsAutomatic(E(type)));
        }
        foreach(var e in new[]{
            new GameEvent(GameEventType.StarMilestone3,EventScope.LocalTeam,0,0,0,1,true,false,EventConfidence.ConfirmedState,"team-star",1,0),
            new GameEvent(GameEventType.StarMilestone3,EventScope.LocalTeam,0,0,0,1,true,false,EventConfidence.ConfirmedState,"team-star"),
            new GameEvent(GameEventType.StarMilestone3,EventScope.LocalTeam,0,0,0,1,false,false,EventConfidence.ConfirmedState,"team-star",0,0),
            new GameEvent(GameEventType.StarMilestone3,EventScope.LocalTeam,0,0,0,1,true,true,EventConfidence.ConfirmedState,"team-star",0,0),
            new GameEvent(GameEventType.StarMilestone3,EventScope.LocalTeam,0,0,0,1,true,false,EventConfidence.InputIntent,"team-star",0,0),
            E(GameEventType.StarMilestone3,EventScope.AllLocal)})
            Check(!PersonalFeedbackPolicy.AllowsAutomatic(e));
        Check(EventRouter.Resolve(E(GameEventType.StarMilestone3,EventScope.LocalTeam),new ControllerBinding(1,0))==-1);
    }
    private static void SharedStarReceipt()
    {
        for(int level=1;level<=4;level++)
        {
            var selected=CelebrationCoordinator.Select(GameEventType.ComboLevel2,level);
            var t=Ready();t.Commit(D(selected));
            Check(t.ReceivePlate(20,11,true,12)==null);
            t.Request(A(),12,20,20);t.Carry(A(),12,0,21);t.Commit(D(at:22));
            var next=t.ReceivePlate(20,12,true,23);
            Check(next!=null && next.ToEvent().EventType==GameEventType.DeliveryOrdered);
        }
    }
    private static void SharedAlertGate()
    {
        foreach(var type in new[]{GameEventType.DeliveryFailure,GameEventType.OrderExpired,GameEventType.BurnWarning,GameEventType.FoodBurned,GameEventType.FireStart,GameEventType.FireReminder})
        {
            Check(PersonalFeedbackPolicy.AllowsAutomatic(E(type,EventScope.LocalTeam)));
            Check(!PersonalFeedbackPolicy.AllowsAutomatic(E(type)));
        }
        foreach(var type in new[]{GameEventType.ThrowSuccess,GameEventType.CatchSuccess,GameEventType.DeliveryOrdered,
            GameEventType.DeliveryOutOfOrder,GameEventType.DeliveryUnclassified,GameEventType.ComboLevel2,GameEventType.ComboLevel3,GameEventType.ComboLevel4})
            Check(!PersonalFeedbackPolicy.AllowsAutomatic(E(type,EventScope.LocalTeam)));
    }
    private static void StarProgress()
    {
        var tracker=new StarMilestoneTracker();ScoreThresholds thresholds;Check(ScoreThresholds.TryCreate(new[]{100,200,300},out thresholds));
        tracker.Prime(new ScoreSnapshot("r",0,0,thresholds,true,0));Check(tracker.Observe(new ScoreSnapshot("r",0,100,thresholds,true,1))==1);
        Check(tracker.Observe(new ScoreSnapshot("r",0,150,thresholds,true,2))==0);
        var t=Ready();t.Commit(D());Check(t.ReceivePlate(20,11,true,12).ToEvent().EventType==GameEventType.DeliveryOrdered);
    }
    private static void ComboProgress()
    {
        var c=new ComboTracker();c.ObserveState("r",0,1,true);
        Check(c.Resolve(new DeliveryPayload("r",0,1,1,true,2,2,true,0),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.ComboLevel2);
        Check(c.Resolve(new DeliveryPayload("r",0,2,2,true,2,3,true,1),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryOrdered);
    }
}
