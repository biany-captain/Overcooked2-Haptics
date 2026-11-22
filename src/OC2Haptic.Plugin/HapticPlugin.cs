using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using BepInEx;
using OC2Haptic.Config;
using OC2Haptic.Core;
using OC2Haptic.GameEvents;
using OC2Haptic.Hooks;
using OC2Haptic.XInput;
using OC2Haptic.UI;
using OC2Haptic.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OC2Haptic
{
    [BepInPlugin("dev.oc2haptic.feedback", "OC2 Haptic", "0.6.2")]
    [BepInProcess("Overcooked2.exe")]
    public sealed class HapticPlugin : BaseUnityPlugin
    {
        internal static HapticPlugin Current;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private HapticConfig settings;
        private HapticEngine engine;
        private XInputBackend backend;
        private ThrowHooks hooks;
        private CatchHooks catchHooks;
        private KitchenHooks kitchenHooks;
        private ScoreProbeHooks scoreProbeHooks;
        private PersonalDeliveryProbeHooks personalDeliveryProbeHooks;
        private readonly PersonalDeliveryTracker personalDeliveries=new PersonalDeliveryTracker();
        private bool personalDeliveryFaulted;
        private HazardHooks hazardHooks;
        private readonly HazardStateTracker hazards=new HazardStateTracker();
        private readonly Dictionary<int,CookingUIController.State> cookingProbeStates=new Dictionary<int,CookingUIController.State>();
        private FeedbackCoordinator coordinator;
        private bool hazardFaulted;
        private long previewSequence;
        private ScoreSourceProbe scoreProbe;
        private bool scoreProbeFaulted;
        private readonly CaptureStack<ScoreSourceProbe.Observation> captures=new CaptureStack<ScoreSourceProbe.Observation>();
        private readonly ComboTracker combos=new ComboTracker();
        private readonly StarMilestoneTracker stars=new StarMilestoneTracker();
        private readonly RoundSession rewardRound=new RoundSession();
        private readonly FeedbackQueue pendingFeedback=new FeedbackQueue();
        private UnityRewardAudioBackend rewardAudio;
        private RewardOutput rewards;
        private bool focused=true, manualPreview;
        private HapticMenu menu;
        private bool menuFaulted;
        private bool ready, subscribed, applicationPaused, wasInRound, wasPaused;
        private readonly bool[] faultedEvents = new bool[Enum.GetValues(typeof(GameEventType)).Length];
        private bool? deviceConnected;
        private double nextPollMs;
        private double nextFlowLookupMs;
        private IFlowController currentFlow;
        private Component flowOwner;
        private readonly string[] lastProbeIdentity = new string[Enum.GetValues(typeof(GameEventType)).Length];
        private readonly int[] observedEvents = new int[Enum.GetValues(typeof(GameEventType)).Length];
        private readonly int[] scheduledEvents = new int[Enum.GetValues(typeof(GameEventType)).Length];

        private void Awake()
        {
            try
            {
                settings = new HapticConfig(Config,message=>Logger.LogInfo(message));
                backend = new XInputBackend(message => Logger.LogError(message));
                engine = new HapticEngine(backend);
                rewardAudio=new UnityRewardAudioBackend(message=>Logger.LogInfo(message),()=>manualPreview,()=>settings.Star4Selection.Value,
                    ()=>settings.Star3AudioFile.Value,()=>settings.Star4AudioFile.Value);
                rewardAudio.Prepare(Path.Combine(Path.GetDirectoryName(Info.Location),"audio"));
                rewardAudio.SetMusicFocus(focused);
                rewards=new RewardOutput(engine,rewardAudio,message=>Logger.LogInfo(message),rewardAudio.DurationMs);
                coordinator=new FeedbackCoordinator(engine,rewards);
                coordinator.SetMode(settings.FeedbackMode,clock.Elapsed.TotalMilliseconds);
                coordinator.SetFeatureSwitches(settings.StarHapticsEnabled.Value,settings.StarAudioEnabled.Value,
                    settings.HazardHapticsEnabled.Value,clock.Elapsed.TotalMilliseconds);
                hooks = new ThrowHooks(Logger);
                catchHooks = new CatchHooks(Logger);
                kitchenHooks = new KitchenHooks(Logger);
                scoreProbeHooks = new ScoreProbeHooks(Logger);
                personalDeliveryProbeHooks = new PersonalDeliveryProbeHooks(Logger);
                hazardHooks = new HazardHooks(Logger);
                scoreProbe = new ScoreSourceProbe(message => Logger.LogInfo(message), () => clock.Elapsed.TotalMilliseconds);
                menu = new HapticMenu(this, settings, engine, message => Logger.LogInfo(message),PreviewEvent,StopOutput,rewardAudio.Packs);
                settings.WatchOutputChanges(OnOutputSettingsChanged);
                ready = true;
                Logger.LogInfo("OC2Haptic initialized | 0.6.2-alpha strict personal feedback candidate | x86/net35 | F6 / F10");
                Logger.LogInfo("Explicit binding required: Devices.XInputIndex and Devices.LocalGamePadSlot default to -1. No slot is guessed.");
            }
            catch (Exception error) { Logger.LogError("OC2 Haptic disabled during initialization: " + error); }
        }

        private void OnEnable()
        {
            if (!ready) return;
            Current = this;
            Array.Clear(faultedEvents, 0, faultedEvents.Length);
            scoreProbeFaulted = false;
            personalDeliveryFaulted=false;
            hazardFaulted=false;hazards.Reset();cookingProbeStates.Clear();
            scoreProbe.Reset("plugin enabled");
            SuspendRewardState();
            currentFlow = null; flowOwner = null; nextFlowLookupMs = 0;
            SceneManager.activeSceneChanged += OnSceneChanged;
            subscribed = true;
            InstallAdapter("ThrowSuccess", delegate { hooks.Install(); });
            InstallAdapter("CatchSuccess", catchHooks.Install);
            InstallAdapter("Kitchen", kitchenHooks.Install);
            InstallAdapter("Score capture", scoreProbeHooks.Install);
            InstallAdapter("Personal delivery diagnostics",personalDeliveryProbeHooks.Install);
            InstallAdapter("Candidate hazards",hazardHooks.Install);
        }

        private void InstallAdapter(string name, Action install)
        {
            try { install(); }
            catch (Exception error) { Logger.LogError(name + " adapter Unavailable; other adapters will still be installed: " + error); }
        }

        private void Update()
        {
            if (!ready) return;
            try
            {
                double now = clock.Elapsed.TotalMilliseconds;
                bool inRound = IsInRound(now);
                bool paused = applicationPaused || TimeManager.IsPaused(TimeManager.PauseLayer.Main);
                if (!wasInRound && inRound) scoreProbe.Reset("round began");
                if (wasInRound && !inRound) {scoreProbe.Reset("round ended");EndRewardRound();}
                if ((wasInRound && !inRound) || (!wasPaused && paused)) Stop("round ended or game paused");
                if(wasPaused && !paused)hazards.DeferReminders(now);
                wasInRound = inRound; wasPaused = paused;

                if (settings.MenuKey.Value != KeyCode.None && Input.GetKeyDown(settings.MenuKey.Value) && !menuFaulted)
                {
                    try { menu.Toggle(); } catch (Exception error) { StopFaultedMenu(error); }
                }
                if (!menuFaulted)
                {
                    try { menu.Update(); } catch (Exception error) { StopFaultedMenu(error); }
                }

                if (settings.ToggleKey.Value != KeyCode.None && Input.GetKeyDown(settings.ToggleKey.Value))
                {
                    settings.ToggleAllOutput();
                    Logger.LogInfo("All feedback " + (settings.AnyOutputEnabled ? "enabled" : "disabled"));
                }
                if (now >= nextPollMs) { PollSelectedDevice(); nextPollMs = now + 250; }
                if(!hazardFaulted && hazards.ReminderDue(now) && GameplayOutputAllowed(now))
                    DispatchHazard(GameEventType.FireReminder,hazards.Incident,"ClientFlammable:active-incident");
                pendingFeedback.Drain(DispatchNow);
                if (!settings.HapticsEnabled.Value) return;
                // Explicit menu previews must reach zero even while the game's own time is paused.
                // Actual game events still pass through the pause gate in Dispatch.
                engine.Tick(clock.Elapsed.TotalMilliseconds);
            }
            catch (Exception error)
            {
                rewards.Stop();
                CloseMenuSafely();
                ready = false;
                Logger.LogError("OC2 Haptic stopped after Update failure; original game is left running: " + error);
            }
        }

        private void LateUpdate(){if(ready && rewardAudio!=null)rewardAudio.TickMusic();}

        private void OnGUI()
        {
            if (!ready || menuFaulted || menu == null) return;
            try { menu.Draw(); }
            catch (ExitGUIException) { throw; }
            catch (Exception error) { StopFaultedMenu(error); }
        }

        private void StopFaultedMenu(Exception error)
        {
            menuFaulted = true;
            CloseMenuSafely();
            Logger.LogError("Management menu disabled after an error; other haptic features remain available: " + error);
        }

        private void CloseMenuSafely()
        {
            try { if (menu != null) menu.Close(); }
            catch (Exception error)
            {
                menuFaulted = true;
                Logger.LogError("Management menu cleanup failed; remaining plugin cleanup continues: " + error);
            }
        }

        internal void ObserveFlight(Component item, object thrower)
        {
            if (!ready || faultedEvents[(int)GameEventType.ThrowSuccess]) return;
            GameEvent gameEvent = PlayerResolver.FromFlight(item, thrower, clock.Elapsed.TotalMilliseconds);
            if (gameEvent == null) return;
            Dispatch(gameEvent);
        }

        internal void ObserveCatch(Component chef, GameObject item)
        {
            if (!ready || faultedEvents[(int)GameEventType.CatchSuccess]) return;
            GameEvent gameEvent = PlayerResolver.FromChef(chef, item.GetInstanceID(), GameEventType.CatchSuccess,
                clock.Elapsed.TotalMilliseconds, "ServerAttachmentCatcher.HandleCatch");
            if (gameEvent == null) { LogUnresolved(GameEventType.CatchSuccess, "local chef identity unavailable"); return; }
            Dispatch(gameEvent);
        }

        internal void ObserveKitchen(GameEventType type, int team, int orderId, string source)
        {
            if (!ready || faultedEvents[(int)type]) return;
            LogUnresolved(type,"strict personal: fallback callback has no proven chef/plate/result; team="+team+" order="+orderId);
        }

        internal bool IsScoreCaptureEnabled { get { return ready && !scoreProbeFaulted; } }
        internal bool IsPersonalDeliveryProbeEnabled {get{return ready && settings.ScoreSourceProbe.Value;}}
        internal bool IsPersonalDeliveryCaptureEnabled {get{return ready && !personalDeliveryFaulted;}}
        internal void LogPersonalDeliveryProbe(string detail)
        {if(IsPersonalDeliveryProbeEnabled)Logger.LogInfo("Personal delivery probe | timestampMs="+clock.ElapsedMilliseconds+" round="+(rewardRound.Key??"unprimed")+" "+detail);}
        internal void ObservePersonalRequest(PersonalActor actor,int plate,int station)
        {
            if(!IsPersonalDeliveryCaptureEnabled || actor==null)return;
            bool allowed=actor.Pad==settings.LocalPadSlot.Value && GameplayOutputAllowed(clock.Elapsed.TotalMilliseconds);
            personalDeliveries.Request(actor,allowed?plate:0,allowed?station:0,clock.Elapsed.TotalMilliseconds);
        }
        internal void ObservePersonalCarry(PersonalActor actor,int before,int after)
        {if(IsPersonalDeliveryCaptureEnabled)personalDeliveries.Carry(actor,before,after,clock.Elapsed.TotalMilliseconds);}
        internal void ObservePersonalPlate(int station,int plate,bool stationAccepted)
        {
            if(!IsPersonalDeliveryCaptureEnabled)return;
            var result=personalDeliveries.ReceivePlate(station,plate,stationAccepted,clock.Elapsed.TotalMilliseconds);
            if(result!=null)Dispatch(result.ToEvent(),result.Decision.Correlation);
            if(settings.DebugLogEvents.Value)Logger.LogInfo("Personal delivery decision | station="+station+" plate="+plate+
                " attributed="+(result!=null)+" type="+(result==null?null:result.Decision.Type)+" reason="+personalDeliveries.LastReason);
        }
        internal void StopFaultedPersonalDelivery(Exception error)
        {
            if(personalDeliveryFaulted)return;personalDeliveryFaulted=true;personalDeliveries.Reset();
            Logger.LogError("Personal attribution unavailable after observer fault; no team fallback, explicit previews remain available: "+error);
        }
        internal ScoreSourceProbe.Observation CaptureScoreProbe(ClientKitchenFlowControllerBase flow, KitchenFlowMessage message)
        {
            // In the installed Unity2017 assembly GetHashCode returns Scene.m_Handle; handle itself is internal.
            bool previousRound=rewardRound.Key!=null;
            if(coordinator.ObserveRound(rewardRound,SceneManager.GetActiveScene().GetHashCode(),flow.GetInstanceID(),flow.InRound,clock.Elapsed.TotalMilliseconds))
            {combos.Reset();stars.Reset();captures.Clear();if(previousRound){personalDeliveries.Reset();pendingFeedback.Clear();hazards.Reset();cookingProbeStates.Clear();}}
            var frame=scoreProbe.Before(flow,message);
            if(frame!=null){frame.RoundKey=flow.InRound?rewardRound.Key:null;captures.Push(frame);}return frame;
        }
        internal void ReleaseScoreCapture(ScoreSourceProbe.Observation frame){captures.Release(frame);}
        internal bool OwnsKitchenCapture(ClientKitchenFlowControllerBase flow,GameEventType type,int team,uint order)
        {
            var frame=captures.Current;
            if(!IsScoreCaptureEnabled || frame==null || frame.FlowId!=flow.GetInstanceID() || (int)frame.Team!=team)return false;
            bool match=type==GameEventType.DeliveryUnclassified?frame.Kind==KitchenFlowMessage.MsgType.Delivery && frame.Success && frame.OrderId==order:
                type==GameEventType.DeliveryFailure?frame.Kind==KitchenFlowMessage.MsgType.Delivery && !frame.Success:
                type==GameEventType.OrderExpired && frame.Kind==KitchenFlowMessage.MsgType.OrderExpired && frame.OrderId==order;
            if(match)frame.ConfirmedKitchenType=type;return match;
        }
        internal void CompleteScoreProbe(ClientKitchenFlowControllerBase flow, ScoreSourceProbe.Observation observation)
        {
            if(settings.ScoreSourceProbe.Value)scoreProbe.After(flow, observation);
            if(string.IsNullOrEmpty(observation.RoundKey) || observation.RoundKey!=rewardRound.Key)return;
            var monitor=flow.GetMonitorForTeam(observation.Team);
            var after=ScoreSourceProbe.Sample.Read(monitor==null?null:monitor.Score);
            bool trusted=observation.Context.Calibrated && observation.Before!=null &&
                ScoreSourceProbe.Same(observation.Payload,after);
            string round=observation.RoundKey??"";
            string comboRound=round+":"+observation.Context.Key;
            int team=(int)observation.Team;
            if(observation.Before!=null)combos.ObserveState(comboRound,team,observation.Before.Multiplier,trusted);
            var thresholds=observation.Context.Thresholds;
            if(observation.Before!=null)stars.Prime(new ScoreSnapshot(round,team,observation.Before.Score,thresholds,trusted,observation.Timestamp));
            int milestone=stars.Observe(new ScoreSnapshot(round,team,after==null?0:after.Score,thresholds,trusted,observation.Timestamp),
                settings.StarMode!=StarFeedbackMode.Disabled && GameplayOutputAllowed(observation.Timestamp));
            if(milestone>0 && !settings.IsEventEnabled((GameEventType)((int)GameEventType.StarMilestone1+milestone-1)))milestone=0;
            GameEventType? delivery=null;string key=round+":"+team+":score:"+observation.Sequence;
            ComboResult result=null;
            if(observation.Kind==KitchenFlowMessage.MsgType.Delivery && observation.Success)
            {
                var payload=new DeliveryPayload(comboRound,team,unchecked((int)observation.OrderId),after==null?-1:after.Deliveries,
                    observation.WasCombo,after==null?0:after.Multiplier,after==null?0:after.Combo,trusted,observation.Timestamp);
                result=combos.Resolve(payload,settings.IsEventEnabled(GameEventType.ComboLevel2),settings.ComboMode,settings.MaxHapticTier.Value);
                if(result.Duplicate)return;
                delivery=result.Type;key=payload.Key;
            }
            else
            {
                if(after!=null)combos.ObserveState(comboRound,team,after.Multiplier,trusted);
                delivery=observation.ConfirmedKitchenType;
            }
            var selected=CelebrationCoordinator.Select(delivery,milestone);
            if(settings.DebugLogEvents.Value || !trusted)
                Logger.LogInfo("Reward decision | session="+round+" team="+team+" order="+observation.OrderId+
                    " wasCombo="+observation.WasCombo+" tip="+observation.Tip+" rawMultiplier="+(after==null?-1:after.Multiplier)+
                    " rawStreak="+(after==null?-1:after.Combo)+" tier="+(result==null?null:result.Tier)+
                    " selected="+selected+" star="+milestone+" confidence="+(trusted?"VerifiedClientState":"Unknown")+" arbitration=SameClientMessage");
            if(observation.Kind==KitchenFlowMessage.MsgType.Delivery && !observation.Success && !personalDeliveryFaulted)
            {
                personalDeliveries.Commit(new PersonalDeliveryDecision(team,0,GameEventType.DeliveryFailure,key,observation.Timestamp,false));
                LogUnresolved(GameEventType.DeliveryFailure,"strict personal: client rejection wire has no station identity; drain without output");return;
            }
            if(!selected.HasValue)return;
            if(observation.Kind==KitchenFlowMessage.MsgType.Delivery && !personalDeliveryFaulted)
                personalDeliveries.Commit(new PersonalDeliveryDecision(team,observation.StationId,selected,key,observation.Timestamp,trusted));
            else LogUnresolved(selected.Value,"strict personal: no directly attributed delivery for this score/expiry event");
        }
        internal void ObserveFire(ClientFlammable source,bool active)
        {
            if(!ready || hazardFaulted || source==null)return;
            double now=clock.Elapsed.TotalMilliseconds;var transition=hazards.ObserveFire(source.GetInstanceID(),active,now);
            if(transition.Started || transition.Cleared || settings.DebugLogEvents.Value)
                Logger.LogInfo("Hazard probe | source=ClientFlammable entity="+source.GetInstanceID()+" active="+active+" incident="+transition.Incident+
                    " started="+transition.Started+" cleared="+transition.Cleared+" confidence=AppliedClientStateCandidate");
            if(transition.Cleared)
            {coordinator.Cancel(GameEventType.FireStart,transition.Incident,now);coordinator.Cancel(GameEventType.FireReminder,transition.Incident,now);}
            if(transition.Started && GameplayOutputAllowed(now))DispatchHazard(GameEventType.FireStart,transition.Incident,"ClientFlammable.Ignite:confirmed-state");
        }
        internal void ObserveCooking(ClientCookingHandler source,CookingUIController.State state)
        {
            if(!ready || hazardFaulted || source==null)return;
            int entity=source.GetInstanceID();CookingUIController.State previous;
            if(!cookingProbeStates.TryGetValue(entity,out previous) || previous!=state)
                Logger.LogInfo("Hazard probe | source=ClientCookingHandler entity="+entity+" state="+state+" confidence=AppliedClientStateCandidate");
            if(state==CookingUIController.State.Idle)cookingProbeStates.Remove(entity);else cookingProbeStates[entity]=state;
            var stage=state==CookingUIController.State.OverDoing?CookingHazardState.Warning:state==CookingUIController.State.Ruined?CookingHazardState.Burned:CookingHazardState.None;
            var selected=hazards.ObserveCooking(entity,stage);double now=clock.Elapsed.TotalMilliseconds;
            if(stage!=CookingHazardState.Warning)coordinator.Cancel(GameEventType.BurnWarning,entity,now);
            if(selected.HasValue && GameplayOutputAllowed(now))DispatchHazard(selected.Value,entity,"ClientCookingHandler.ApplyServerUpdate:confirmed-state");
        }
        private void DispatchHazard(GameEventType type,int entity,string source)
        {
            LogUnresolved(type,"strict personal: hazard has no reliable actor attribution; no automatic output");
        }
        internal void StopFaultedHazards(Exception error)
        {if(hazardFaulted)return;hazardFaulted=true;hazards.Reset();cookingProbeStates.Clear();StopOutput();Logger.LogError("Hazard candidates disabled after adapter error; other events continue: "+error);}
        internal void StopFaultedScoreProbe(Exception error)
        {
            if (scoreProbeFaulted) return;
            scoreProbeFaulted = true;
            captures.Clear();
            personalDeliveries.Reset();
            Logger.LogError("Score capture unavailable; unattributed delivery stays silent, explicit previews/direct chef events continue: " + error);
        }

        private bool GameplayOutputAllowed(double now)
        {
            return settings.AnyOutputEnabled && !applicationPaused && (!settings.RespectGameFocus.Value || focused) &&
                !TimeManager.IsPaused(TimeManager.PauseLayer.Main) && IsInRound(now) && (menu==null || !menu.IsOpen);
        }
        private void Dispatch(GameEvent gameEvent,string correlation=null)
        {pendingFeedback.Enqueue(gameEvent,correlation);}
        private void DispatchNow(GameEvent gameEvent,string correlation)
        {
            double now=clock.Elapsed.TotalMilliseconds;
            int eventIndex = (int)gameEvent.EventType;
            observedEvents[eventIndex]++;
            var binding=new ControllerBinding(settings.LocalPadSlot.Value,settings.XInputIndex.Value);
            int output = EventRouter.Resolve(gameEvent,binding);
            bool allowed = PersonalFeedbackPolicy.AllowsAutomatic(gameEvent) && settings.ShouldDispatch(gameEvent.EventType) && GameplayOutputAllowed(now) &&
                (!GameEventTypes.IsHazard(gameEvent.EventType) || (!hazardFaulted && hazards.IsCurrent(gameEvent.EventType,gameEvent.EntityIdentity)));
            EventFeedback feedback = EventFeedback.For(gameEvent.EventType);
            bool accepted = coordinator.Emit(gameEvent,binding,correlation??feedback.Key(gameEvent),settings.PatternFor(gameEvent.EventType),
                now,settings.Intensity,settings.VolumeFor(gameEvent.EventType),allowed);
            if (accepted) scheduledEvents[eventIndex]++;
            string identity = gameEvent.PlayerIdentity + ":" + gameEvent.LocalPad + ":" + gameEvent.IsKeyboard + ":" + output + ":" + gameEvent.TeamIdentity;
            if (settings.DebugLogEvents.Value || lastProbeIdentity[eventIndex] != identity)
            {
                lastProbeIdentity[eventIndex] = identity;
                Logger.LogInfo("Event probe | type=" + gameEvent.EventType + " observed=" + observedEvents[eventIndex] + " scheduled=" + scheduledEvents[eventIndex] +
                    " | player=" + gameEvent.PlayerIdentity + " gamePad=" + gameEvent.LocalPad + " keyboard=" + gameEvent.IsKeyboard +
                    " team=" + gameEvent.TeamIdentity + " localTeam=" + gameEvent.LocalTeamIdentity +
                    " entity=" + gameEvent.EntityIdentity + " source=" + gameEvent.SourceHook +
                    " | XInput=" + output + " allowed=" + allowed + " accepted=" + accepted+" reason="+coordinator.LastDropReason);
            }
        }

        internal void StopFaultedThrow(Exception error)
        {
            StopFaultedEvent(GameEventType.ThrowSuccess, error);
        }

        internal void StopFaultedEvent(GameEventType type, Exception error)
        {
            if (faultedEvents[(int)type]) return;
            faultedEvents[(int)type] = true;
            StopOutput();
            Logger.LogError(type + " adapter disabled after a fault; other event adapters remain available: " + error);
        }

        private void LogUnresolved(GameEventType type, string reason)
        {
            int index = (int)type;
            if (settings.DebugLogEvents.Value || lastProbeIdentity[index] != reason)
            {
                lastProbeIdentity[index] = reason;
                Logger.LogInfo("Event probe | type=" + type + " accepted=False | reason=" + reason);
            }
        }

        private void PollSelectedDevice()
        {
            int index = settings.XInputIndex.Value;
            if (index < 0 || index > 3) return;
            bool connected = backend.IsConnected(index);
            if (deviceConnected != connected)
            {
                StopOutput();
                deviceConnected = connected;
                Logger.LogInfo("XInput " + index + (connected ? " connected" : " disconnected") + " | code=" + backend.LastError);
            }
        }

        private bool IsInRound(double nowMs)
        {
            // GameUtils resolves managers with tag searches. Cache the live component rather than searching every frame.
            if (flowOwner == null && nowMs >= nextFlowLookupMs)
            {
                currentFlow = GameUtils.RequestManagerInterface<IFlowController>();
                flowOwner = currentFlow as Component;
                nextFlowLookupMs = nowMs + 1000;
            }
            return flowOwner != null && currentFlow != null && currentFlow.InRound;
        }

        private void OnOutputSettingsChanged(object sender, EventArgs args)
        {
            personalDeliveries.Reset();
            if(sender==settings.StarHapticsEnabled || sender==settings.StarAudioEnabled || sender==settings.HazardHapticsEnabled)
            {
                double now=clock.Elapsed.TotalMilliseconds;
                coordinator.SetFeatureSwitches(settings.StarHapticsEnabled.Value,settings.StarAudioEnabled.Value,settings.HazardHapticsEnabled.Value,now);
                if(sender==settings.HazardHapticsEnabled)hazards.DeferReminders(now);
                Logger.LogInfo("Feature switches | starHaptics="+settings.StarHapticsEnabled.Value+" starAudio="+settings.StarAudioEnabled.Value+
                    " hazardHaptics="+settings.HazardHapticsEnabled.Value);
                return;
            }
            if(sender==settings.HapticsEnabled || sender==settings.AudioEnabled)
            {
                pendingFeedback.Clear();coordinator.SetMode(settings.FeedbackMode,clock.Elapsed.TotalMilliseconds);hazards.DeferReminders(clock.Elapsed.TotalMilliseconds);
                if(!settings.AnyOutputEnabled)SuspendRewardState();
                return;
            }
            Stop("output settings changed");
            settings.RefreshPatterns();coordinator.SetMode(settings.FeedbackMode,clock.Elapsed.TotalMilliseconds);
            if(settings.InvalidPatterns.Length>0)Logger.LogWarning("Invalid haptic patterns muted: "+settings.InvalidPatterns);
            if(sender==settings.XInputIndex || sender==settings.LocalPadSlot){SuspendRewardState();coordinator.Reset(clock.Elapsed.TotalMilliseconds);}
            if (sender == settings.XInputIndex || sender == settings.LocalPadSlot) CloseMenuSafely();
            deviceConnected = null; nextPollMs = 0; Array.Clear(lastProbeIdentity, 0, lastProbeIdentity.Length);
            if (float.IsNaN(settings.MasterIntensity.Value) || float.IsInfinity(settings.MasterIntensity.Value))
                Logger.LogWarning("Invalid MasterIntensity: output is muted until a finite value is configured.");
        }

        private void OnSceneChanged(Scene previous, Scene next)
        {
            Stop("scene changed");
            if (scoreProbe != null) scoreProbe.Reset("scene changed");
            EndRewardRound();
            CloseMenuSafely();
            Array.Clear(lastProbeIdentity, 0, lastProbeIdentity.Length); wasInRound = false;
            currentFlow = null; flowOwner = null; nextFlowLookupMs = 0;
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (paused) { Stop("application paused"); CloseMenuSafely(); }
        }

        private void Stop(string reason)
        {
            StopOutput();
            if (settings != null && settings.DebugLogEvents.Value) Logger.LogInfo("Stop: " + reason);
        }
        private void StopOutput()
        {
            pendingFeedback.Clear();
            personalDeliveries.Reset();
            hazards.DeferReminders(clock.Elapsed.TotalMilliseconds);
            if(coordinator!=null)coordinator.Stop(clock.Elapsed.TotalMilliseconds);
            else if (rewards != null) rewards.Stop();
            else if(engine!=null)engine.StopAll();
        }

        private void OnDisable()
        {
            Stop("plugin disabled");
            if (scoreProbe != null) scoreProbe.Reset("plugin disabled");
            SuspendRewardState();
            hazards.Reset();cookingProbeStates.Clear();
            CloseMenuSafely();
            if (Current == this) Current = null;
            if (subscribed) { SceneManager.activeSceneChanged -= OnSceneChanged; subscribed = false; }
            foreach (IDisposable adapter in new IDisposable[] { hooks, catchHooks, kitchenHooks, scoreProbeHooks, personalDeliveryProbeHooks, hazardHooks })
            {
                if (adapter == null) continue;
                try { adapter.Dispose(); }
                catch (Exception error) { Logger.LogError("Own Hook cleanup failed: " + error); }
            }
        }

        private void OnApplicationQuit() { Stop("application quit"); CloseMenuSafely(); }

        private void OnApplicationFocus(bool value)
        {
            focused=value;
            if(value)hazards.DeferReminders(clock.Elapsed.TotalMilliseconds);
            FeedbackFocus.Apply(value,settings!=null && settings.RespectGameFocus.Value,
                musicFocus=>{if(rewardAudio!=null)rewardAudio.SetMusicFocus(musicFocus);},
                ()=>{Stop("focus lost");CloseMenuSafely();});
        }
        private void SuspendRewardState(){personalDeliveries.Reset();combos.Reset();stars.Suspend();captures.Clear();}
        private void EndRewardRound(){personalDeliveries.Reset();rewardRound.End();combos.Reset();stars.Reset();captures.Clear();hazards.Reset();cookingProbeStates.Clear();if(coordinator!=null)coordinator.Reset(clock.Elapsed.TotalMilliseconds);}
        private bool PreviewEvent(GameEventType type,StarFeedbackMode? previewMode)
        {
            if(!ready || !settings.AnyOutputEnabled || (!focused && settings.RespectGameFocus.Value))return false;
            manualPreview=true;
            try
            {
                if(previewMode.HasValue)StopOutput();
                return coordinator.Preview(type,settings.XInputIndex.Value,"preview:"+type+":"+(++previewSequence),settings.PatternFor(type,previewMode),
                    clock.Elapsed.TotalMilliseconds,settings.Intensity,settings.VolumeFor(type),previewMode);
            }
            finally {manualPreview=false;}
        }

        private void OnDestroy()
        {
            OnDisable();
            if (menu != null) menu.Dispose();
            if(rewardAudio!=null)rewardAudio.Dispose();
            if (settings != null) settings.UnwatchOutputChanges(OnOutputSettingsChanged);
            Logger.LogInfo("OC2 Haptic shutdown");
            for (int index = 0; index < observedEvents.Length; index++)
                Logger.LogInfo("Event totals | type=" + (GameEventType)index + " observed=" + observedEvents[index] + " scheduled=" + scheduledEvents[index]);
        }
    }
}
