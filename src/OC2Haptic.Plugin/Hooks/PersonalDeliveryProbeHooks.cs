using System;
using BepInEx.Logging;
using HarmonyLib;
using OC2Haptic.Core;
using OC2Haptic.GameEvents;
using UnityEngine;
using ChefRequest=Team17.Online.Multiplayer.Messaging.ChefEventMessage.ChefEventType;

namespace OC2Haptic.Hooks
{
    // Read-only observations feed the strict personal tracker; never change game results.
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
                var messenger=AccessTools.TypeByName("ClientMessenger");
                var request=messenger==null?null:AccessTools.Method(messenger,"ChefEventMessage",new[]{typeof(ChefRequest),typeof(GameObject),typeof(MonoBehaviour)});
                if(carry==null || plate==null || request==null)throw new MissingMethodException("Personal attribution requires exact request, carry and plate methods.");
                owner.Patch(carry,prefix:new HarmonyMethod(typeof(PersonalDeliveryProbeHooks),"BeforeCarry"),
                    postfix:new HarmonyMethod(typeof(PersonalDeliveryProbeHooks),"AfterCarry"));
                owner.Patch(plate,postfix:new HarmonyMethod(typeof(PersonalDeliveryProbeHooks),"AfterPlate"));
                owner.Patch(request,prefix:new HarmonyMethod(typeof(PersonalDeliveryProbeHooks),"BeforeRequest"));
                log.LogInfo("Strict personal attribution installed: explicit placement request plus confirmed carry/kitchen/plate; no team fallback.");
            }
            catch(Exception error){Fail(error);Dispose();}
        }
        private static bool Enabled
        {get{return active!=null && !active.faulted && HapticPlugin.Current!=null && HapticPlugin.Current.IsPersonalDeliveryCaptureEnabled;}}
        private static int ObjectId(GameObject item){return item==null?0:item.GetInstanceID();}
        private static int PlateId(GameObject item)
        {return item!=null && item.GetComponent<ClientPlate>()!=null?item.GetInstanceID():0;}
        private static string Actor(PersonalActor actor)
        {
            return actor==null?"actor=unavailable":"chef="+actor.Chef+" local="+actor.Local+" player="+actor.Player+
                " gamePad="+actor.Pad+" keyboard="+actor.Keyboard+" team="+actor.Team;
        }
        private static void Fail(Exception error)
        {
            if(active==null || active.faulted)return;active.faulted=true;
            if(HapticPlugin.Current!=null)HapticPlugin.Current.StopFaultedPersonalDelivery(error);
            else active.log.LogError("Personal attribution unavailable; no guessed feedback: "+error);
        }
        private static void BeforeRequest(ChefRequest __0,GameObject __1,MonoBehaviour __2)
        {
            if(!Enabled || (__0!=ChefRequest.Place && __0!=ChefRequest.Take) || __1==null)return;
            try
            {
                var chef=__1.GetComponent<PlayerControls>();var actor=PlayerResolver.ReadPersonalActor(chef);
                var carrier=__1.GetComponent<ClientPlayerAttachmentCarrier>();
                int plate=carrier==null?0:PlateId(carrier.InspectCarriedItem());
                var station=__0==ChefRequest.Place && __2!=null?__2.GetComponent<ClientPlateStation>():null;
                int target=station==null?0:station.gameObject.GetInstanceID();
                HapticPlugin.Current.ObservePersonalRequest(actor,plate,target);
                HapticPlugin.Current.LogPersonalDeliveryProbe("event=placement-request "+Actor(actor)+" plate="+plate+" station="+target+" request="+__0);
            }
            catch(Exception error){Fail(error);}
        }
        private static void BeforeCarry(ClientPlayerAttachmentCarrier __instance,object __0,out CarryFrame __state)
        {
            __state=null;if(!Enabled)return;
            try
            {
                var message=__0 as ChefCarryMessage;if(message==null)return;
                __state=new CarryFrame{Actor=PlayerResolver.ReadPersonalActor(__instance),Before=PlateId(__instance.InspectCarriedItem(message.m_playerAttachTarget)),
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
                HapticPlugin.Current.ObservePersonalCarry(__state.Actor,__state.Before,after);
                HapticPlugin.Current.LogPersonalDeliveryProbe("event=carry-applied "+Actor(__state.Actor)+" target="+__state.Target+
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
                HapticPlugin.Current.ObservePersonalPlate(__instance.gameObject.GetInstanceID(),PlateId(message.m_delivered),message.m_success);
            }
            catch(Exception error){Fail(error);}
        }
        private sealed class CarryFrame
        {internal PersonalActor Actor;internal int Before;internal PlayerAttachTarget Target;internal uint NetworkItem;}
        public void Dispose()
        {
            if(active==this)active=null;
            try{owner.UnpatchSelf();}catch(Exception error){log.LogError("Own personal delivery probe cleanup failed: "+error);}
        }
    }
}
