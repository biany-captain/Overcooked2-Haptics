using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
namespace OC2Haptic.Hooks
{
    // Candidate client-state hooks, not yet game/hardware acceptance.
    internal sealed class HazardHooks:IDisposable
    {
        private readonly ManualLogSource log;
        private readonly List<Harmony> owners=new List<Harmony>();
        internal HazardHooks(ManualLogSource log){this.log=log;}
        internal void Install()
        {
            if(typeof(ClientFlammable).Assembly.ManifestModule.ModuleVersionId!=new Guid("114d606a-29d5-472c-8ebe-c2c20fbe3be3"))
            {log.LogWarning("Hazard hooks unavailable: game assembly is not the statically inspected build.");return;}
            InstallOne(typeof(ClientFlammable),"Ignite",Type.EmptyTypes,"Ignited");
            InstallOne(typeof(ClientFlammable),"Extinguish",new[]{typeof(bool),typeof(bool)},"Extinguished");
            InstallOne(typeof(ClientCookingHandler),"ApplyServerUpdate",new[]{typeof(Team17.Online.Multiplayer.Messaging.Serialisable)},"CookingUpdated");
            InstallOne(typeof(ClientCookingHandler),"OnDisable",Type.EmptyTypes,"CookingRemoved");
        }
        private void InstallOne(Type type,string name,Type[] args,string callback)
        {
            var owner=new Harmony("dev.oc2haptic.feedback.hazard."+type.Name+"."+name);
            try
            {
                var method=AccessTools.Method(type,name,args);
                if(method==null){log.LogWarning("Hazard hook unavailable: "+type.Name+"."+name);return;}
                owners.Add(owner);owner.Patch(method,postfix:new HarmonyMethod(typeof(HazardHooks),callback));
                log.LogInfo("Candidate hazard hook installed: "+type.Name+"."+name+"; native validation pending.");
            }
            catch(Exception error){log.LogError("Hazard hook unavailable: "+name+" "+error);try{owner.UnpatchSelf();}catch(Exception cleanup){log.LogError(cleanup);}owners.Remove(owner);}
        }
        private static void Ignited(ClientFlammable __instance){ObserveFire(__instance,true);}
        private static void Extinguished(ClientFlammable __instance){ObserveFire(__instance,false);}
        private static void ObserveFire(ClientFlammable instance,bool expected)
        {
            var plugin=HapticPlugin.Current;if(plugin==null)return;
            try{if(instance!=null && instance.OnFire()==expected)plugin.ObserveFire(instance,expected);}
            catch(Exception error){plugin.StopFaultedHazards(error);}
        }
        private static void CookingUpdated(ClientCookingHandler __instance,object __0,CookingUIController.State ___m_cachedCookingState)
        {
            var plugin=HapticPlugin.Current;if(plugin==null)return;
            try{var message=__0 as CookingStateMessage;if(message!=null && message.m_cookingState==___m_cachedCookingState)
                plugin.ObserveCooking(__instance,___m_cachedCookingState);}
            catch(Exception error){plugin.StopFaultedHazards(error);}
        }
        private static void CookingRemoved(ClientCookingHandler __instance)
        {var plugin=HapticPlugin.Current;if(plugin==null)return;try{plugin.ObserveCooking(__instance,CookingUIController.State.Idle);}catch(Exception error){plugin.StopFaultedHazards(error);}}
        public void Dispose()
        {foreach(var owner in owners)try{owner.UnpatchSelf();}catch(Exception error){log.LogError("Own hazard hook cleanup failed: "+error);}owners.Clear();}
    }
}
