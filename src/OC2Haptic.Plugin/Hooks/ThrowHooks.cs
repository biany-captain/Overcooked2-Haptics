using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace OC2Haptic.Hooks
{
    internal sealed class ThrowHooks : IDisposable
    {
        private readonly Harmony harmony = new Harmony("dev.oc2haptic.feedback.throw");
        private readonly ManualLogSource log;
        private bool installed;

        internal ThrowHooks(ManualLogSource log) { this.log = log; }

        internal bool Install()
        {
            try
            {
                Type type = AccessTools.TypeByName("ClientThrowableItem");
                Type thrower = AccessTools.TypeByName("IClientThrower");
                if (type == null || thrower == null) { log.LogWarning("Throw Hook Unavailable: required type not found."); return false; }
                MethodInfo original = AccessTools.Method(type, "StartFlight", new[] { thrower });
                if (original == null || AccessTools.Field(type, "m_isFlying") == null)
                { log.LogWarning("Throw Hook Unavailable: method/state signature not found."); return false; }
                harmony.Patch(original, postfix: new HarmonyMethod(typeof(ThrowHooks), "Postfix"));
                installed = true;
                log.LogInfo("Throw Hook installed: ClientThrowableItem.StartFlight(IClientThrower); routes confirmed flight state.");
                return true;
            }
            catch (Exception error)
            {
                // Harmony installation is an external/version boundary; leave original game execution intact.
                log.LogError("Throw Hook Unavailable: " + error);
                Dispose();
                return false;
            }
        }

        private static void Postfix(object __instance, object __0, bool ___m_isFlying)
        {
            HapticPlugin plugin = HapticPlugin.Current;
            if (plugin == null || !___m_isFlying) return;
            try { plugin.ObserveFlight(__instance as Component, __0); }
            catch (Exception error) { plugin.StopFaultedThrow(error); }
        }

        public void Dispose()
        {
            if (!installed) return;
            harmony.UnpatchSelf();
            installed = false;
        }
    }
}
