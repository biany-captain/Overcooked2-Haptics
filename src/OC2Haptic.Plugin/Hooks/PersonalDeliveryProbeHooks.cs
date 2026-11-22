using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace OC2Haptic.Hooks
{
    // Read-only diagnostics: never schedules feedback or changes the game method's result.
    // A plate-station success flag means the station accepted an object, not a correct order.
    internal sealed class PersonalDeliveryProbeHooks : IDisposable
    {
        private readonly ManualLogSource log;
        private readonly Harmony owner=new Harmony("dev.oc2haptic.feedback.personal-delivery-probe");
        private static PersonalDeliveryProbeHooks active;
        private bool faulted;
        internal PersonalDeliveryProbeHooks(ManualLogSource log){this.log=log;}
        internal void Install()
        {
            faulted=false;active=this;
            try
            {
                var args=new[]{typeof(Team17.Online.Multiplayer.Messaging.Serialisable)};
                var carry=AccessTools.Method(typeof(ClientPlayerAttachmentCarrier),"ApplyServerEvent",args);
                var plate=AccessTools.Method(typeof(ClientPlateStation),"ApplyServerEvent",args);
                if(carry==null || plate==null)throw new MissingMethodException("Personal delivery probe requires carry and plate client sync methods.");
                owner.Patch(carry,prefix:new HarmonyMethod(typeof(PersonalDeliveryProbeHooks),"BeforeCarry"),
                    postfix:new HarmonyMethod(typeof(PersonalDeliveryProbeHooks),"AfterCarry"));
                owner.Patch(plate,postfix:new HarmonyMethod(typeof(PersonalDeliveryProbeHooks),"AfterPlate"));
                log.LogInfo("Personal delivery probe installed: optional read-only chef/plate/station observations; routing unchanged.");
            }
            catch(Exception error){log.LogError("Personal delivery probe unavailable; existing feedback unchanged: "+error);Dispose();}
        }
        private static bool Enabled
        {get{return active!=null && !active.faulted && HapticPlugin.Current!=null && HapticPlugin.Current.IsPersonalDeliveryProbeEnabled;}}
        private static int ObjectId(GameObject item){return item==null?0:item.GetInstanceID();}
        private static int PlateId(GameObject item)
        {return item!=null && item.GetComponent<ClientPlate>()!=null?item.GetInstanceID():0;}
        private static string Actor(Component chef)
        {
            var identity=chef.GetComponent<PlayerIDProvider>();
            bool local=identity!=null && identity.IsLocallyControlled();
            int pad=-1;bool keyboard=false;
            var controls=chef.GetComponent<PlayerControls>();
            if(local && controls!=null && controls.ControlScheme!=null)
            {var input=PlayerInputLookup.GetPadForPlayer(controls.ControlScheme.Player);pad=(int)input;keyboard=PCPadInputProvider.IsKeyboard(input);}
            return "chef="+chef.gameObject.GetInstanceID()+" local="+local+" player="+(identity==null?-1:(int)identity.GetID())+
                " gamePad="+pad+" keyboard="+keyboard+" team="+(identity==null?-1:(int)identity.GetTeam());
        }
        private static void Fail(Exception error)
        {if(active==null || active.faulted)return;active.faulted=true;active.log.LogError("Personal delivery diagnostics stopped after observer error; existing feedback unchanged: "+error);}
        private static void BeforeCarry(ClientPlayerAttachmentCarrier __instance,object __0,out CarryFrame __state)
        {
            __state=null;if(!Enabled)return;
            try
            {
                var message=__0 as ChefCarryMessage;if(message==null)return;
                __state=new CarryFrame{Actor=Actor(__instance),Before=PlateId(__instance.InspectCarriedItem(message.m_playerAttachTarget)),
                    Target=message.m_playerAttachTarget,NetworkItem=message.m_carriableItem};
            }
            catch(Exception error){Fail(error);}
        }
        private static void AfterCarry(ClientPlayerAttachmentCarrier __instance,CarryFrame __state)
        {
            if(!Enabled || __state==null)return;
            try
            {
                int after=PlateId(__instance.InspectCarriedItem(__state.Target));
                if(__state.Before==0 && after==0)return;
                HapticPlugin.Current.LogPersonalDeliveryProbe("event=carry-applied "+__state.Actor+" target="+__state.Target+
                    " oldPlate="+__state.Before+" newPlate="+after+" messageItem="+__state.NetworkItem);
            }
            catch(Exception error){Fail(error);}
        }
        private static void AfterPlate(ClientPlateStation __instance,object __0)
        {
            if(!Enabled)return;
            try
            {
                var message=__0 as PlateStationMessage;if(message==null)return;
                HapticPlugin.Current.LogPersonalDeliveryProbe("event=plate-applied station="+__instance.gameObject.GetInstanceID()+
                    " deliveredObject="+ObjectId(message.m_delivered)+" deliveredPlate="+PlateId(message.m_delivered)+
                    " stationAccepted="+message.m_success+" orderResult=not-in-this-message");
            }
            catch(Exception error){Fail(error);}
        }
        private sealed class CarryFrame
        {internal string Actor;internal int Before;internal PlayerAttachTarget Target;internal uint NetworkItem;}
        public void Dispose()
        {
            if(active==this)active=null;
            try{owner.UnpatchSelf();}catch(Exception error){log.LogError("Own personal delivery probe cleanup failed: "+error);}
        }
    }
}
