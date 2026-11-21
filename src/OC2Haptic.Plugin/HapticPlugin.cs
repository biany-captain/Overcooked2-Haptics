using System;
using System.Diagnostics;
using BepInEx;
using OC2Haptic.Config;
using OC2Haptic.Core;
using OC2Haptic.GameEvents;
using OC2Haptic.Hooks;
using OC2Haptic.XInput;
using OC2Haptic.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OC2Haptic
{
    [BepInPlugin("dev.oc2haptic.feedback", "OC2 Haptic", "0.2.3")]
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
        private HapticMenu menu;
        private bool menuFaulted;
        private bool ready, subscribed, applicationPaused, wasInRound, wasPaused;
        private readonly bool[] faultedEvents = new bool[5];
        private bool? deviceConnected;
        private double nextPollMs;
        private double nextFlowLookupMs;
        private IFlowController currentFlow;
        private Component flowOwner;
        private readonly string[] lastProbeIdentity = new string[5];
        private readonly int[] observedEvents = new int[5];
        private readonly int[] scheduledEvents = new int[5];

        private void Awake()
        {
            try
            {
                settings = new HapticConfig(Config);
                backend = new XInputBackend(message => Logger.LogError(message));
                engine = new HapticEngine(backend);
                hooks = new ThrowHooks(Logger);
                catchHooks = new CatchHooks(Logger);
                kitchenHooks = new KitchenHooks(Logger);
                scoreProbeHooks = new ScoreProbeHooks(Logger);
                scoreProbe = new ScoreSourceProbe(message => Logger.LogInfo(message), () => clock.Elapsed.TotalMilliseconds);
                menu = new HapticMenu(this, settings, engine, () => clock.Elapsed.TotalMilliseconds, message => Logger.LogInfo(message));
                settings.WatchOutputChanges(OnOutputSettingsChanged);
                ready = true;
                Logger.LogInfo("OC2Haptic initialized | 0.2.3-alpha | x86/net35 | F6 management menu / F10 global toggle (configurable)");
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
            currentFlow = null; flowOwner = null; nextFlowLookupMs = 0;
            SceneManager.activeSceneChanged += OnSceneChanged;
            subscribed = true;
            InstallAdapter("ThrowSuccess", delegate { hooks.Install(); });
            InstallAdapter("CatchSuccess", catchHooks.Install);
            InstallAdapter("Kitchen", kitchenHooks.Install);
            if (settings.ScoreSourceProbe.Value) InstallAdapter("Score source probe", scoreProbeHooks.Install);
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
                if (wasInRound && !inRound) scoreProbe.Reset("round ended");
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
                engine.StopAll();
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

        internal bool IsScoreProbeEnabled { get { return ready && settings.ScoreSourceProbe.Value && !scoreProbeFaulted; } }
        internal ScoreSourceProbe.Observation CaptureScoreProbe(ClientKitchenFlowControllerBase flow, KitchenFlowMessage message)
        {
            return scoreProbe.Before(flow, message);
        }
        internal void CompleteScoreProbe(ClientKitchenFlowControllerBase flow, ScoreSourceProbe.Observation observation)
        {
            scoreProbe.After(flow, observation);
        }
        internal void StopFaultedScoreProbe(Exception error)
        {
            if (scoreProbeFaulted) return;
            scoreProbeFaulted = true;
            Logger.LogError("Score source probe disabled after a fault; existing feedback continues: " + error);
        }

        private void Dispatch(GameEvent gameEvent)
        {
            int eventIndex = (int)gameEvent.EventType;
            observedEvents[eventIndex]++;
            int output = EventRouter.Resolve(gameEvent, new ControllerBinding(settings.LocalPadSlot.Value, settings.XInputIndex.Value));
            bool allowed = settings.Enabled.Value && settings.IsEventEnabled(gameEvent.EventType) && !applicationPaused &&
                !TimeManager.IsPaused(TimeManager.PauseLayer.Main) && IsInRound(gameEvent.TimestampMs);
            EventFeedback feedback = EventFeedback.For(gameEvent.EventType);
            bool accepted = allowed && output >= 0 && engine.Play(output, feedback.Key(gameEvent), feedback.Pattern,
                feedback.Priority, feedback.CooldownMs, gameEvent.TimestampMs, settings.Intensity);
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
            if (sender == settings.XInputIndex || sender == settings.LocalPadSlot) CloseMenuSafely();
            deviceConnected = null; nextPollMs = 0; Array.Clear(lastProbeIdentity, 0, lastProbeIdentity.Length);
            if (float.IsNaN(settings.MasterIntensity.Value) || float.IsInfinity(settings.MasterIntensity.Value))
                Logger.LogWarning("Invalid MasterIntensity: output is muted until a finite value is configured.");
        }

        private void OnSceneChanged(Scene previous, Scene next)
        {
            Stop("scene changed");
            if (scoreProbe != null) scoreProbe.Reset("scene changed");
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
            if (engine != null) engine.StopAll();
            if (settings != null && settings.DebugLogEvents.Value) Logger.LogInfo("Stop: " + reason);
        }

        private void OnDisable()
        {
            Stop("plugin disabled");
            if (scoreProbe != null) scoreProbe.Reset("plugin disabled");
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

        private void OnDestroy()
        {
            OnDisable();
            if (menu != null) menu.Dispose();
            if (settings != null) settings.UnwatchOutputChanges(OnOutputSettingsChanged);
            Logger.LogInfo("OC2 Haptic shutdown");
            for (int index = 0; index < observedEvents.Length; index++)
                Logger.LogInfo("Event totals | type=" + (GameEventType)index + " observed=" + observedEvents[index] + " scheduled=" + scheduledEvents[index]);
        }
    }
}
