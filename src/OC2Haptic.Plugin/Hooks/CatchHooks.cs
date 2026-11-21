using System;
using BepInEx.Logging;
using HarmonyLib;
using OC2Haptic.Core;
using UnityEngine;

namespace OC2Haptic.Hooks
{
    internal sealed class CatchHooks : IDisposable
    {
        private readonly Harmony harmony = new Harmony("dev.oc2haptic.feedback.catch");
        private readonly ManualLogSource log;
        private bool installed;
        internal CatchHooks(ManualLogSource log) { this.log = log; }

        internal void Install()
        {
            try
            {
                Type type = AccessTools.TypeByName("ServerAttachmentCatcher");
                var method = type == null ? null : AccessTools.Method(type, "HandleCatch", new[] { typeof(ICatchable), typeof(Vector2) });
                if (method == null || AccessTools.Field(type, "m_carrier") == null)
                { log.LogWarning("CatchSuccess Hook Unavailable: method/carrier signature missing."); return; }
                installed = true;
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(CatchHooks), "Postfix"));
                log.LogInfo("CatchSuccess Hook installed: ServerAttachmentCatcher.HandleCatch; local/host runtime validation required.");
            }
            catch (Exception error)
            {
                log.LogError("CatchSuccess Hook Unavailable: " + error);
                Dispose();
            }
        }

        private static void Postfix(object __instance, object __0, object ___m_carrier)
        {
            HapticPlugin plugin = HapticPlugin.Current;
            if (plugin == null) return;
            try
            {
                var catchable = __0 as ICatchable;
                var carrier = ___m_carrier as ServerPlayerAttachmentCarrier;
                if (catchable == null || carrier == null) return;
                GameObject item = catchable.AccessGameObject();
                if (item == null || carrier.InspectCarriedItem() != item) return;
                plugin.ObserveCatch(__instance as Component, item);
            }
            catch (Exception error) { plugin.StopFaultedEvent(GameEventType.CatchSuccess, error); }
        }

        public void Dispose()
        {
            if (!installed) return;
            harmony.UnpatchSelf();
            installed = false;
        }
    }
}
