using System;
using System.Collections.Generic;
using OC2Haptic.Core;

internal static class CoordinatorTests
{
    internal static void Add(Dictionary<string,Action> tests)
    {
        tests.Add("v16 coordinator stale normal and unmapped targets stay silent", Stale);
        tests.Add("v16 coordinator cooldown and dedup are per device", Cooldown);
        tests.Add("v16 coordinator normal budget excludes gaps and isolates devices", Budget);
        tests.Add("v16 coordinator negative budget cannot block first emergency", NegativeBudget);
        tests.Add("v16 coordinator preemption charges elapsed work and never replays", Preemption);
        tests.Add("v16 coordinator one team sound with separate mapped motors", TeamAudio);
        tests.Add("v16 coordinator stop and channel change remain independent", Stop);
        tests.Add("v16 backend registry rejects unknown cross backend and duplicate identity", Ownership);
        tests.Add("v16 coordinator accepts a future backend descriptor with exclusive identity", Extension);
        tests.Add("v16 success does not interrupt star and independent failure does", MusicPriority);
        tests.Add("v16 star with exhausted motor budget keeps audio and reports zero motor window", AudioBudget);
        tests.Add("v16 audio-only negative interrupts celebration without bound XInput", UnboundNegative);
        tests.Add("v16 refused higher star retires cancelled motor budget immediately", CancelledBudget);
        tests.Add("feature switches isolate automatic star audio and motors", FeatureChannels);
        tests.Add("feature switches cancel only affected active channels without replay", FeatureCancellation);
        tests.Add("feature switches block hazards without suppressing rejection or music", FeatureHazards);
        tests.Add("feature switches do not block explicit previews or bypass global masters", FeaturePreviews);
        tests.Add("feature switches audio-only team dedup never restores star motors", FeatureTeamAudio);
    }
    private static GameEvent E(GameEventType type,double at,int pad=0,int entity=1)
    {return new GameEvent(type,EventScope.LocalTeam,at,pad,pad,entity,true,false,EventConfidence.ConfirmedState,"fixture",0,0);}
    private static HapticPattern Pulse(int ms){return new HapticPattern(new HapticSegment(.3f,.2f,ms));}
    private static void Check(bool b){V16Tests.Check(b);}
    private sealed class Motors:IControllerRumbleBackend
    {
        internal readonly float[] Left=new float[4];internal int Writes;
        public bool IsConnected(int index){return true;}
        public bool SetMotors(int index,float left,float right){Writes++;Left[index]=left;return true;}
    }
    private sealed class Sound:IRewardAudioBackend
    {internal int Plays;internal bool Playing;public bool Play(int star,float volume){Plays++;Playing=true;return true;}public void Stop(){Playing=false;}}
    private sealed class Rig
    {
        internal readonly Motors Motors=new Motors();internal readonly Sound Sound=new Sound();
        internal readonly List<string> Logs=new List<string>();
        internal readonly HapticEngine Engine;internal readonly RewardOutput Output;internal readonly FeedbackCoordinator Coordinator;
        internal Rig(){Engine=new HapticEngine(Motors);Output=new RewardOutput(Engine,Sound,Logs.Add,audioDurationMs:_=>4050);Coordinator=new FeedbackCoordinator(Engine,Output);}
        internal bool Emit(GameEvent e,string key,double now,HapticPattern p=null,int output=0,bool allowed=true)
        {return Coordinator.Emit(e,new ControllerBinding(e.LocalPad,output),key,p??EventFeedback.For(e.EventType).Pattern,now,1,1,allowed);}
        internal bool Preview(int index,string key,HapticPattern pattern,double now)
        {return Coordinator.Preview(GameEventType.TestPulse,index,key,pattern,now,1,1,null);}
    }
    private static void Stale()
    {
        var r=new Rig();Check(!r.Emit(E(GameEventType.DeliveryOrdered,0),"stale",181));
        var e=E(GameEventType.DeliveryOrdered,200,1);
        Check(!r.Coordinator.Emit(e,new ControllerBinding(0,0),"wrong-pad",EventFeedback.For(e.EventType).Pattern,200,1,1,true));
        Check(r.Motors.Writes==0);Check(r.Emit(E(GameEventType.DeliveryFailure,0),"negative-not-stale",500));
    }
    private static void Cooldown()
    {
        var r=new Rig();Check(r.Emit(E(GameEventType.DeliveryFailure,0),"one",0));r.Engine.Tick(0);
        Check(!r.Emit(E(GameEventType.DeliveryFailure,20),"different-object",20));
        Check(r.Emit(E(GameEventType.DeliveryFailure,20,1),"one",20,output:1));
        Check(r.Emit(E(GameEventType.DeliveryFailure,1200),"two",1200));
        Check(!r.Emit(E(GameEventType.DeliveryFailure,1201),"two",1201));
        Check(!r.Emit(E(GameEventType.DeliveryFailure,1201),"one",1201));
        r.Engine.Tick(1630);Check(!r.Emit(E(GameEventType.DeliveryFailure,2000),"one",2000));
    }
    private static void Budget()
    {
        var r=new Rig();var p=new HapticPattern(new HapticSegment(.2f,0,400),new HapticSegment(0,0,100));
        for(int i=0;i<3;i++){Check(r.Preview(0,"normal"+i,p,i*1000));r.Engine.Tick(i*1000);r.Engine.Tick(i*1000+500);}
        Check(!r.Preview(0,"blocked",p,3000));Check(r.Preview(1,"other-device",p,3000));
        Check(r.Preview(0,"window-expired",p,10400));
    }
    private static void NegativeBudget()
    {
        var r=new Rig();for(int i=0;i<5;i++){Check(r.Emit(E(GameEventType.DeliveryFailure,i*1500),"failure"+i,i*1500));r.Engine.Tick(i*1500+430);}
        Check(!r.Emit(E(GameEventType.DeliveryFailure,7500),"over-negative-budget",7500));
        Check(r.Emit(E(GameEventType.FireStart,7500),"first-fire",7500));r.Engine.Tick(7500);Check(r.Motors.Left[0]>.7f);
    }
    private static void Preemption()
    {
        var r=new Rig();Check(r.Preview(0,"ordinary",Pulse(400),0));r.Engine.Tick(0);
        Check(r.Emit(E(GameEventType.DeliveryFailure,100),"urgent",100));r.Engine.Tick(100);r.Engine.Tick(530);
        Check(r.Preview(0,"after-a",Pulse(500),700));r.Engine.Tick(1200);
        Check(r.Preview(0,"after-b",Pulse(500),1500));r.Engine.Tick(2000);
        Check(r.Preview(0,"last100",Pulse(100),2200));r.Engine.Tick(2300);
        Check(!r.Preview(0,"budget-full",Pulse(1),2500));r.Engine.Tick(5000);Check(r.Motors.Left[0]==0);
    }
    private static void TeamAudio()
    {
        var r=new Rig();Check(r.Emit(E(GameEventType.StarMilestone2,0),"same-team-commit",0));r.Engine.Tick(0);
        Check(r.Emit(E(GameEventType.StarMilestone2,10,1),"same-team-commit",10,output:1));r.Engine.Tick(10);
        Check(r.Sound.Plays==1 && r.Motors.Left[0]>0 && r.Motors.Left[1]>0);
    }
    private static void Stop()
    {
        var r=new Rig();Check(r.Emit(E(GameEventType.StarMilestone4,0),"star",0));r.Engine.Tick(0);
        r.Coordinator.SetMode(StarFeedbackMode.AudioOnly,10);Check(r.Motors.Left[0]==0 && r.Sound.Playing);
        r.Coordinator.SetMode(StarFeedbackMode.Disabled,20);Check(!r.Sound.Playing);
        r.Coordinator.SetMode(StarFeedbackMode.HapticsAndAudio,30);r.Engine.Tick(1000);Check(r.Motors.Left[0]==0 && r.Sound.Plays==1);
        r.Coordinator.Stop(1000);Check(!r.Emit(E(GameEventType.DeliveryOrdered,0),"stopped-old",1000));
    }
    private static void Ownership()
    {
        var registry=new OutputDeviceRegistry();
        Check(registry.TryClaim(0,new OutputDevice(ControllerBackendKind.XInput,"0",null,false)));
        Check(!registry.TryClaim(1,new OutputDevice(ControllerBackendKind.SDL3,"a",null,false)));
        registry.Release(0);
        Check(registry.TryClaim(0,new OutputDevice(ControllerBackendKind.XInput,"0","physical-a",true)));
        Check(!registry.TryClaim(1,new OutputDevice(ControllerBackendKind.SDL3,"a","physical-a",true)));
        Check(registry.TryClaim(1,new OutputDevice(ControllerBackendKind.SDL3,"b","physical-b",true)));
        Check(!registry.TryClaim(2,new OutputDevice(ControllerBackendKind.XInput,"0","physical-a",true)));
    }
    private static void MusicPriority()
    {
        var r=new Rig();Check(r.Emit(E(GameEventType.StarMilestone4,0),"star",0));r.Engine.Tick(700);
        Check(r.Emit(E(GameEventType.DeliveryOrdered,1000),"success",1000));Check(r.Sound.Playing && r.Sound.Plays==1);
        Check(r.Emit(E(GameEventType.DeliveryFailure,1100),"different-failure",1100));r.Engine.Tick(1100);
        Check(!r.Sound.Playing && r.Motors.Left[0]>.7f);
    }
    private static void Extension()
    {
        var b=new Motors();var engine=new HapticEngine(b);var output=new RewardOutput(engine,new Sound());
        var coordinator=new FeedbackCoordinator(engine,output,describeDevice:index=>new OutputDevice(ControllerBackendKind.SDL3,index.ToString(),"same-device",true));
        Check(coordinator.Preview(GameEventType.TestPulse,0,"sdl-mock",Pulse(80),0,1,1,null));engine.Tick(0);Check(b.Left[0]>0);
        Check(!coordinator.Preview(GameEventType.TestPulse,1,"duplicate-physical",Pulse(80),0,1,1,null));
    }
    private static void AudioBudget()
    {
        var r=new Rig();for(int i=0;i<3;i++){Check(r.Preview(0,"budget"+i,Pulse(400),i*1000));r.Engine.Tick(i*1000+400);}
        Check(r.Emit(E(GameEventType.StarMilestone1,3000),"audio-without-budget",3000));r.Engine.Tick(3000);
        Check(r.Sound.Playing && r.Motors.Left[0]==0 && r.Logs.Exists(line=>line.Contains("motorWindowMs=0")));
    }
    private static void UnboundNegative()
    {
        var r=new Rig();r.Coordinator.SetMode(StarFeedbackMode.AudioOnly,0);
        Check(r.Emit(E(GameEventType.StarMilestone4,0),"audio-unbound",0,output:-1));Check(r.Sound.Playing);
        Check(r.Emit(E(GameEventType.DeliveryFailure,100),"later-rejection",100,output:-1));
        Check(!r.Sound.Playing && r.Motors.Writes==0);
    }
    private static void CancelledBudget()
    {
        var r=new Rig();r.Coordinator.SetMode(StarFeedbackMode.HapticsOnly,0);
        var old=HapticPattern.Celebration(new HapticSegment(.5f,0,500));
        Check(r.Emit(E(GameEventType.StarMilestone3,0),"old-star",0,old));r.Engine.Tick(0);
        Check(!r.Coordinator.Emit(E(GameEventType.StarMilestone4,20),new ControllerBinding(0,0),"invalid-higher",null,20,1,1,true));r.Engine.Tick(20);Check(r.Motors.Left[0]==0);
        Check(r.Preview(0,"after-cancel-a",Pulse(500),1000));r.Engine.Tick(1500);
        Check(r.Preview(0,"after-cancel-b",Pulse(500),2000));r.Engine.Tick(2500);
        Check(r.Preview(0,"remaining180",Pulse(180),3000));r.Engine.Tick(3180);
        Check(!r.Preview(0,"budget-now-full",Pulse(1),3500));
    }
    private static void FeatureChannels()
    {
        var r=new Rig();r.Coordinator.SetFeatureSwitches(false,true,true,0);
        Check(r.Emit(E(GameEventType.StarMilestone1,0),"audio-star",0));r.Engine.Tick(0);
        Check(r.Sound.Plays==1 && r.Motors.Writes==0);
        Check(r.Emit(E(GameEventType.DeliveryOrdered,200),"ordinary",200));r.Engine.Tick(200);
        Check(r.Motors.Left[0]>0 && r.Sound.Playing);
        var h=new Rig();h.Coordinator.SetFeatureSwitches(true,false,true,0);
        Check(h.Emit(E(GameEventType.StarMilestone1,0),"motor-star",0));h.Engine.Tick(0);
        Check(h.Motors.Left[0]>0 && h.Sound.Plays==0);
        h.Coordinator.SetFeatureSwitches(false,false,true,20);Check(h.Motors.Left[0]==0);
        Check(!h.Emit(E(GameEventType.StarMilestone2,1000),"disabled-star",1000));
        Check(h.Emit(E(GameEventType.ComboLevel2,1100),"combo-still-enabled",1100));
    }
    private static void FeatureCancellation()
    {
        var r=new Rig();Check(r.Emit(E(GameEventType.StarMilestone4,0),"star",0));r.Engine.Tick(0);
        r.Coordinator.SetFeatureSwitches(false,true,true,20);Check(r.Motors.Left[0]==0 && r.Sound.Playing);
        r.Coordinator.SetFeatureSwitches(true,true,true,30);r.Engine.Tick(40);Check(r.Motors.Left[0]==0 && r.Sound.Plays==1);
        var h=new Rig();Check(h.Emit(E(GameEventType.StarMilestone4,0),"star",0));h.Engine.Tick(0);
        h.Coordinator.SetFeatureSwitches(true,false,true,20);h.Engine.Tick(20);Check(h.Motors.Left[0]>0 && !h.Sound.Playing);
        h.Coordinator.SetFeatureSwitches(true,true,true,30);Check(h.Sound.Plays==1 && !h.Sound.Playing);
    }
    private static void FeatureHazards()
    {
        var r=new Rig();Check(r.Emit(E(GameEventType.StarMilestone4,0),"star",0));r.Engine.Tick(0);
        r.Coordinator.SetFeatureSwitches(true,true,false,20);
        Check(!r.Emit(E(GameEventType.FireStart,30),"disabled-fire",30));Check(r.Sound.Playing && r.Motors.Left[0]>0);
        Check(r.Emit(E(GameEventType.DeliveryFailure,40),"rejection",40));r.Engine.Tick(40);Check(!r.Sound.Playing && r.Motors.Left[0]>.7f);
        var h=new Rig();Check(h.Emit(E(GameEventType.FireStart,0),"fire",0));h.Engine.Tick(0);
        h.Coordinator.SetFeatureSwitches(true,true,false,20);Check(h.Motors.Left[0]==0);
        h.Coordinator.SetFeatureSwitches(true,true,true,30);h.Engine.Tick(40);Check(h.Motors.Left[0]==0);
        h.Coordinator.SetFeatureSwitches(true,true,false,50);
        foreach(var type in new[]{GameEventType.BurnWarning,GameEventType.FoodBurned,GameEventType.FireStart,GameEventType.FireReminder})
            Check(!h.Emit(E(type,1000),"off-"+type,1000));
        Check(h.Emit(E(GameEventType.OrderExpired,1100),"expiry",1100));
    }
    private static void FeaturePreviews()
    {
        var r=new Rig();r.Coordinator.SetFeatureSwitches(false,false,false,0);
        Check(r.Coordinator.Preview(GameEventType.StarMilestone4,0,"manual-star",EventFeedback.For(GameEventType.StarMilestone4).Pattern,0,1,1,StarFeedbackMode.HapticsAndAudio));
        r.Engine.Tick(0);Check(r.Motors.Left[0]>0 && r.Sound.Playing);
        r.Coordinator.SetMode(StarFeedbackMode.AudioOnly,20);Check(r.Motors.Left[0]==0 && r.Sound.Playing);
        r.Coordinator.Stop(30);
        Check(r.Coordinator.Preview(GameEventType.StarMilestone4,0,"manual-audio",EventFeedback.For(GameEventType.StarMilestone4).Pattern,1000,1,1,StarFeedbackMode.HapticsAndAudio));
        r.Engine.Tick(1000);Check(r.Motors.Left[0]==0 && r.Sound.Playing);
        var h=new Rig();h.Coordinator.SetFeatureSwitches(false,false,false,0);
        Check(h.Coordinator.Preview(GameEventType.FireStart,0,"manual-fire",EventFeedback.For(GameEventType.FireStart).Pattern,0,1,1,null));
        h.Engine.Tick(0);Check(h.Motors.Left[0]>0);
    }
    private static void FeatureTeamAudio()
    {
        var r=new Rig();r.Coordinator.SetFeatureSwitches(false,true,true,0);
        Check(r.Emit(E(GameEventType.StarMilestone2,0),"same",0));
        Check(!r.Emit(E(GameEventType.StarMilestone2,10,1),"same",10,output:1));r.Engine.Tick(10);
        Check(r.Sound.Plays==1 && r.Motors.Writes==0);
    }
}
