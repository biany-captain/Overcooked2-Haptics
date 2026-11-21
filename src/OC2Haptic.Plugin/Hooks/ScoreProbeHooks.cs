using System;
using BepInEx.Logging;
using HarmonyLib;
using OC2Haptic.GameEvents;

namespace OC2Haptic.Hooks
{
    internal sealed class ScoreProbeHooks : IDisposable
    {
        private readonly ManualLogSource log;
        private readonly Harmony owner = new Harmony("dev.oc2haptic.feedback.score-probe");
        internal ScoreProbeHooks(ManualLogSource log) { this.log = log; }
        internal void Install()
        {
            try
            {
                var method = AccessTools.Method(typeof(ClientKitchenFlowControllerBase), "ApplyServerEvent",
                    new[] { typeof(Team17.Online.Multiplayer.Messaging.Serialisable) });
                if (method == null) { log.LogWarning("Score source probe Unavailable: ApplyServerEvent signature missing."); return; }
                owner.Patch(method, prefix: new HarmonyMethod(typeof(ScoreProbeHooks), "Before"),
                    postfix: new HarmonyMethod(typeof(ScoreProbeHooks), "After"));
                log.LogInfo("Score source probe installed: read-only client sync capture; no new feedback; runtime calibration pending.");
            }
            catch (Exception error)
            {
                log.LogError("Score source probe Unavailable; existing feedback continues: " + error);
                Dispose();
            }
        }

        private static void Before(ClientKitchenFlowControllerBase __instance, object __0,
            out ScoreSourceProbe.Observation __state)
        {
            __state = null;
            HapticPlugin plugin = HapticPlugin.Current;
            if (plugin == null || !plugin.IsScoreProbeEnabled) return;
            try { __state = plugin.CaptureScoreProbe(__instance, __0 as KitchenFlowMessage); }
            catch (Exception error) { plugin.StopFaultedScoreProbe(error); }
        }

        private static void After(ClientKitchenFlowControllerBase __instance, ScoreSourceProbe.Observation __state)
        {
            HapticPlugin plugin = HapticPlugin.Current;
            if (plugin == null || !plugin.IsScoreProbeEnabled || __state == null) return;
            try { plugin.CompleteScoreProbe(__instance, __state); }
            catch (Exception error) { plugin.StopFaultedScoreProbe(error); }
        }

        public void Dispose()
        {
            try { owner.UnpatchSelf(); }
            catch (Exception error) { log.LogError("Own score probe cleanup failed: " + error); }
        }
    }
}
