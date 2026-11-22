using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using OC2Haptic.Core;

namespace OC2Haptic.Hooks
{
    internal sealed class KitchenHooks : IDisposable
    {
        private readonly ManualLogSource log;
        private readonly List<Harmony> owners = new List<Harmony>();
        internal KitchenHooks(ManualLogSource log) { this.log = log; }

        internal void Install()
        {
            InstallOne("OnSuccessfulDelivery", "SuccessPostfix", new[] { typeof(TeamID), typeof(OrderController.OrderID),
                typeof(float), typeof(int), typeof(bool), typeof(ClientPlateStation) });
            InstallOne("OnFailedDelivery", "FailurePostfix", new[] { typeof(TeamID), typeof(OrderController.OrderID) });
            InstallOne("OnOrderExpired", "ExpiredPostfix", new[] { typeof(TeamID), typeof(OrderController.OrderID) });
        }

        private void InstallOne(string methodName, string postfixName, Type[] arguments)
        {
            var owner = new Harmony("dev.oc2haptic.feedback.kitchen." + methodName);
            try
            {
                Type type = AccessTools.TypeByName("ClientKitchenFlowControllerBase");
                var method = type == null ? null : AccessTools.Method(type, methodName, arguments);
                if (method == null) { log.LogWarning(methodName + " Hook Unavailable: signature missing."); return; }
                owners.Add(owner); // Keep ownership before patching so a partial failure can be undone.
                owner.Patch(method, postfix: new HarmonyMethod(typeof(KitchenHooks), postfixName));
                log.LogInfo("Kitchen Hook installed: ClientKitchenFlowControllerBase." + methodName + "; runtime validation required.");
            }
            catch (Exception error)
            {
                log.LogError(methodName + " Hook Unavailable: " + error);
                try { owner.UnpatchSelf(); } catch (Exception cleanup) { log.LogError("Own Hook cleanup failed: " + cleanup); }
                owners.Remove(owner);
            }
        }

        private static void SuccessPostfix(ClientKitchenFlowControllerBase __instance, TeamID __0, OrderController.OrderID __1)
        {
            HapticPlugin plugin=HapticPlugin.Current;
            if(plugin==null || plugin.OwnsKitchenCapture(__instance,GameEventType.DeliveryUnclassified,(int)__0,__1.m_id))return;
            Observe(GameEventType.DeliveryUnclassified,__0,__1,"OnSuccessfulDelivery:fallback");
        }
        private static void FailurePostfix(ClientKitchenFlowControllerBase __instance,TeamID __0,OrderController.OrderID __1)
        {var plugin=HapticPlugin.Current;if(plugin!=null && plugin.OwnsKitchenCapture(__instance,GameEventType.DeliveryFailure,(int)__0,__1.m_id))return;Observe(GameEventType.DeliveryFailure,__0,__1,"OnFailedDelivery");}
        private static void ExpiredPostfix(ClientKitchenFlowControllerBase __instance,TeamID __0,OrderController.OrderID __1)
        {var plugin=HapticPlugin.Current;if(plugin!=null && plugin.OwnsKitchenCapture(__instance,GameEventType.OrderExpired,(int)__0,__1.m_id))return;Observe(GameEventType.OrderExpired,__0,__1,"OnOrderExpired");}

        private static void Observe(GameEventType type, object team, object order, string method)
        {
            HapticPlugin plugin = HapticPlugin.Current;
            if (plugin == null) return;
            try { plugin.ObserveKitchen(type, (int)(TeamID)team, unchecked((int)((OrderController.OrderID)order).m_id), "ClientKitchenFlowControllerBase." + method); }
            catch (Exception error) { plugin.StopFaultedEvent(type, error); }
        }

        public void Dispose()
        {
            foreach (Harmony owner in owners)
            {
                try { owner.UnpatchSelf(); }
                catch (Exception error) { log.LogError("Own kitchen Hook cleanup failed: " + error); }
            }
            owners.Clear();
        }
    }
}
