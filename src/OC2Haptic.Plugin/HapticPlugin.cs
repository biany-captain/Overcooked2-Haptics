using System;
using System.Diagnostics;
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
    [BepInPlugin("dev.oc2haptic.feedback", "OC2 Haptic", "0.3.0")]
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
        private ScoreSourceProbe scoreProbe;
        private bool scoreProbeFaulted;
        private readonly CaptureStack<ScoreSourceProbe.Observation> captures=new CaptureStack<ScoreSourceProbe.Observation>();
        private readonly ComboTracker combos=new ComboTracker();
        private readonly StarMilestoneTracker stars=new StarMilestoneTracker();
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
                settings = new HapticConfig(Config);
                backend = new XInputBackend(message => Logger.LogError(message));
                engine = new HapticEngine(backend);
                rewardAudio=new UnityRewardAudioBackend(message=>Logger.LogInfo(message),()=>settings.MaxSoundDurationMs.Value,()=>manualPreview);
                rewards=new RewardOutput(engine,rewardAudio,message=>Logger.LogWarning(message));
                rewards.Mode=settings.FeedbackMode;
                hooks = new ThrowHooks(Logger);
                catchHooks = new CatchHooks(Logger);
                kitchenHooks = new KitchenHooks(Logger);
                scoreProbeHooks = new ScoreProbeHooks(Logger);
                scoreProbe = new ScoreSourceProbe(message => Logger.LogInfo(message), () => clock.Elapsed.TotalMilliseconds);
                menu = new HapticMenu(this, settings, engine, () => clock.Elapsed.TotalMilliseconds, message => Logger.LogInfo(message),PreviewEvent,rewards.Stop);
                settings.WatchOutputChanges(OnOutputSettingsChanged);
                ready = true;
                Logger.LogInfo("OC2Haptic initialized | 0.3.0-alpha | x86/net35 | F6 management menu / F10 global toggle (configurable)");
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
            scoreProbe.Reset("plugin enabled");
            ResetRewardState();
            currentFlow = null; flowOwner = null; nextFlowLookupMs = 0;
            SceneManager.activeSceneChanged += OnSceneChanged;
            subscribed = true;
            InstallAdapter("ThrowSuccess", delegate { hooks.Install(); });
            InstallAdapter("CatchSuccess", catchHooks.Install);
            InstallAdapter("Kitchen", kitchenHooks.Install);
            InstallAdapter("Score capture", scoreProbeHooks.Install);
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
                if (!wasInRound && inRound) {scoreProbe.Reset("round began");ResetRewardState();}
                if (wasInRound && !inRound) {scoreProbe.Reset("round ended");ResetRewardState();}
                if ((wasInRound && !inRound) || (!wasPaused && paused)) Stop("round ended or game paused");
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
                    settings.Enabled.Value = !settings.Enabled.Value;
                    Logger.LogInfo("Haptics " + (settings.Enabled.Value ? "enabled" : "disabled"));
                }
                if (now >= nextPollMs) { PollSelectedDevice(); nextPollMs = now + 1000; }
                if (!settings.Enabled.Value) return;
                // Explicit menu previews must reach zero even while the game's own time is paused.
                // Actual game events still pass through the pause gate in Dispatch.
                engine.Tick(now);
            }
            catch (Exception error)
            {
                rewards.Stop();
                CloseMenuSafely();
                ready = false;
                Logger.LogError("OC2 Haptic stopped after Update failure; original game is left running: " + error);
            }
        }

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
            GameEvent gameEvent = PlayerResolver.FromTeam(type, team, orderId, settings.LocalPadSlot.Value,
                clock.Elapsed.TotalMilliseconds, source);
            if (gameEvent == null) { LogUnresolved(type, "bound local team unavailable; eventTeam=" + team + " order=" + orderId); return; }
            Dispatch(gameEvent);
        }

        internal bool IsScoreCaptureEnabled { get { return ready && !scoreProbeFaulted; } }
        internal ScoreSourceProbe.Observation CaptureScoreProbe(ClientKitchenFlowControllerBase flow, KitchenFlowMessage message)
        {
            var frame=scoreProbe.Before(flow,message);if(frame!=null)captures.Push(frame);return frame;
        }
        internal void ReleaseScoreCapture(ScoreSourceProbe.Observation frame){captures.Release(frame);}
        internal bool OwnsSuccessCapture(ClientKitchenFlowControllerBase flow,int team,uint order)
        {
            var frame=captures.Current;
            return IsScoreCaptureEnabled && frame!=null && frame.FlowId==flow.GetInstanceID() &&
                frame.Kind==KitchenFlowMessage.MsgType.Delivery && frame.Success && (int)frame.Team==team && frame.OrderId==order;
        }
        internal void CompleteScoreProbe(ClientKitchenFlowControllerBase flow, ScoreSourceProbe.Observation observation)
        {
            if(settings.ScoreSourceProbe.Value)scoreProbe.After(flow, observation);
            var monitor=flow.GetMonitorForTeam(observation.Team);
            var after=ScoreSourceProbe.Sample.Read(monitor==null?null:monitor.Score);
            bool trusted=observation.Context.Calibrated && observation.Before!=null &&
                ScoreSourceProbe.Same(observation.Payload,after);
            string round=observation.Generation+":"+observation.FlowId+":"+observation.Context.Key;
            int team=(int)observation.Team;
            if(observation.Before!=null)combos.ObserveState(round,team,observation.Before.Multiplier,trusted);
            var thresholds=observation.Context.Thresholds;
            if(observation.Before!=null)stars.Prime(new ScoreSnapshot(round,team,observation.Before.Score,thresholds,trusted,observation.Timestamp));
            int milestone=stars.Observe(new ScoreSnapshot(round,team,after==null?0:after.Score,thresholds,trusted,observation.Timestamp),
                settings.StarsEnabled.Value && settings.FeedbackMode!=StarFeedbackMode.Disabled && GameplayOutputAllowed(observation.Timestamp));
            GameEventType? delivery=null;string key=round+":"+team+":score:"+observation.Sequence;
            ComboResult result=null;
            if(observation.Kind==KitchenFlowMessage.MsgType.Delivery && observation.Success)
            {
                var payload=new DeliveryPayload(round,team,unchecked((int)observation.OrderId),after==null?-1:after.Deliveries,
                    observation.WasCombo,after==null?0:after.Multiplier,after==null?0:after.Combo,trusted,observation.Timestamp);
                result=combos.Resolve(payload,settings.ComboEnabled.Value,settings.ComboMode.Value,settings.MaxHapticTier.Value);
                if(result.Duplicate)return;
                delivery=result.Type;key=payload.Key;
            }
            else if(after!=null)combos.ObserveState(round,team,after.Multiplier,trusted);
            var selected=CelebrationCoordinator.Select(delivery,milestone);
            if(settings.DebugLogEvents.Value || !trusted)
                Logger.LogInfo("Reward decision | session="+round+" team="+team+" order="+observation.OrderId+
                    " wasCombo="+observation.WasCombo+" tip="+observation.Tip+" rawMultiplier="+(after==null?-1:after.Multiplier)+
                    " rawStreak="+(after==null?-1:after.Combo)+" tier="+(result==null?null:result.Tier)+
                    " selected="+selected+" star="+milestone+" confidence="+(trusted?"VerifiedClientState":"Unknown")+" arbitration=SameClientMessage");
            if(!selected.HasValue)return;
            GameEvent gameEvent=PlayerResolver.FromTeam(selected.Value,team,unchecked((int)observation.OrderId),settings.LocalPadSlot.Value,
                observation.Timestamp,"ClientKitchenFlowControllerBase.ApplyServerEvent:committed");
            if(gameEvent!=null)Dispatch(gameEvent,key+":"+selected.Value);
        }
        internal void StopFaultedScoreProbe(Exception error)
        {
            if (scoreProbeFaulted) return;
            scoreProbeFaulted = true;
            captures.Clear();
            Logger.LogError("Score capture disabled after a fault; delivery falls back to unclassified and other feedback continues: " + error);
        }

        private bool GameplayOutputAllowed(double now)
        {
            return settings.Enabled.Value && !applicationPaused && (!settings.RespectGameFocus.Value || focused) &&
                !TimeManager.IsPaused(TimeManager.PauseLayer.Main) && IsInRound(now) && (menu==null || !menu.IsOpen);
        }
        private void Dispatch(GameEvent gameEvent,string correlation=null)
        {
            int eventIndex = (int)gameEvent.EventType;
            observedEvents[eventIndex]++;
            int output = EventRouter.Resolve(gameEvent, new ControllerBinding(settings.LocalPadSlot.Value, settings.XInputIndex.Value));
            bool allowed = settings.IsEventEnabled(gameEvent.EventType) && GameplayOutputAllowed(gameEvent.TimestampMs) &&
                EventRouter.IsLocalTarget(gameEvent,settings.LocalPadSlot.Value);
            EventFeedback feedback = EventFeedback.For(gameEvent.EventType);
            if(allowed && feedback.Priority>=55)rewards.Interrupt();
            float intensity=settings.Intensity*(GameEventTypes.IsCombo(gameEvent.EventType)?RewardOutput.SafeVolume(settings.ComboIntensity.Value):1);
            bool accepted = allowed && rewards.Emit(output,correlation??feedback.Key(gameEvent),gameEvent.EventType,
                settings.PatternFor(gameEvent.EventType),gameEvent.TimestampMs,intensity,settings.SoundVolume.Value,false);
            if (accepted) scheduledEvents[eventIndex]++;
            string identity = gameEvent.PlayerIdentity + ":" + gameEvent.LocalPad + ":" + gameEvent.IsKeyboard + ":" + output + ":" + gameEvent.TeamIdentity;
            if (settings.DebugLogEvents.Value || lastProbeIdentity[eventIndex] != identity)
            {
                lastProbeIdentity[eventIndex] = identity;
                Logger.LogInfo("Event probe | type=" + gameEvent.EventType + " observed=" + observedEvents[eventIndex] + " scheduled=" + scheduledEvents[eventIndex] +
                    " | player=" + gameEvent.PlayerIdentity + " gamePad=" + gameEvent.LocalPad + " keyboard=" + gameEvent.IsKeyboard +
                    " team=" + gameEvent.TeamIdentity + " localTeam=" + gameEvent.LocalTeamIdentity +
                    " entity=" + gameEvent.EntityIdentity + " source=" + gameEvent.SourceHook +
                    " | XInput=" + output + " allowed=" + allowed + " accepted=" + accepted);
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
            engine.StopAll();
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
                engine.StopAll();
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
            Stop("output settings changed");
            settings.RefreshPatterns();rewards.Mode=settings.FeedbackMode;
            if(settings.InvalidPatterns.Length>0)Logger.LogWarning("Invalid haptic patterns muted: "+settings.InvalidPatterns);
            if(sender==settings.Enabled || sender==settings.XInputIndex || sender==settings.LocalPadSlot)ResetRewardState();
            if (sender == settings.XInputIndex || sender == settings.LocalPadSlot) CloseMenuSafely();
            deviceConnected = null; nextPollMs = 0; Array.Clear(lastProbeIdentity, 0, lastProbeIdentity.Length);
            if (float.IsNaN(settings.MasterIntensity.Value) || float.IsInfinity(settings.MasterIntensity.Value))
                Logger.LogWarning("Invalid MasterIntensity: output is muted until a finite value is configured.");
        }

        private void OnSceneChanged(Scene previous, Scene next)
        {
            Stop("scene changed");
            if (scoreProbe != null) scoreProbe.Reset("scene changed");
            ResetRewardState();
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
            if (rewards != null) rewards.Stop();
            else if(engine!=null)engine.StopAll();
            if (settings != null && settings.DebugLogEvents.Value) Logger.LogInfo("Stop: " + reason);
        }

        private void OnDisable()
        {
            Stop("plugin disabled");
            if (scoreProbe != null) scoreProbe.Reset("plugin disabled");
            ResetRewardState();
            CloseMenuSafely();
            if (Current == this) Current = null;
            if (subscribed) { SceneManager.activeSceneChanged -= OnSceneChanged; subscribed = false; }
            foreach (IDisposable adapter in new IDisposable[] { hooks, catchHooks, kitchenHooks, scoreProbeHooks })
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
            if(!value && settings!=null && settings.RespectGameFocus.Value){Stop("focus lost");CloseMenuSafely();}
        }
        private void ResetRewardState(){combos.Reset();stars.Reset();captures.Clear();}
        private bool PreviewEvent(GameEventType type)
        {
            if(!ready || !settings.Enabled.Value || (!focused && settings.RespectGameFocus.Value))return false;
            manualPreview=true;
            try
            {
                float intensity=settings.Intensity*(GameEventTypes.IsCombo(type)?RewardOutput.SafeVolume(settings.ComboIntensity.Value):1);
                return rewards.Emit(settings.XInputIndex.Value,"preview:"+type,type,settings.PatternFor(type),clock.Elapsed.TotalMilliseconds,
                    intensity,settings.SoundVolume.Value,false);
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
