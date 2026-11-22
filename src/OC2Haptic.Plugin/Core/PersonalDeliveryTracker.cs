using System;
using System.Collections.Generic;

namespace OC2Haptic.Core
{
    public sealed class PersonalActor
    {
        public readonly int Chef,Player,Pad,Team;
        public readonly bool Local,Keyboard;
        public PersonalActor(int chef,int player,int pad,int team,bool local,bool keyboard)
        {Chef=chef;Player=player;Pad=pad;Team=team;Local=local;Keyboard=keyboard;}
        public bool Eligible {get{return Chef!=0 && Local && !Keyboard && Player>=0 && Player<11 && Pad>=0 && Pad<15 && Team>=0 && Team<=1;}}
        internal bool Same(PersonalActor other)
        {return other!=null && Eligible && other.Eligible && Chef==other.Chef && Player==other.Player && Pad==other.Pad && Team==other.Team;}
    }
    public sealed class PersonalDeliveryDecision
    {
        public readonly int Team,Station;
        public readonly GameEventType? Type;
        public readonly string Correlation;
        public readonly double Timestamp;
        public readonly bool Trusted;
        public PersonalDeliveryDecision(int team,int station,GameEventType? type,string correlation,double timestamp,bool trusted)
        {Team=team;Station=station;Type=type;Correlation=correlation;Timestamp=timestamp;Trusted=trusted;}
    }
    public sealed class PersonalDeliveryConfirmation
    {
        public readonly PersonalActor Actor;
        public readonly PersonalDeliveryDecision Decision;
        public readonly int Plate;
        internal PersonalDeliveryConfirmation(PersonalActor actor,PersonalDeliveryDecision decision,int plate)
        {Actor=actor;Decision=decision;Plate=plate;}
        public GameEvent ToEvent()
        {return new GameEvent(Decision.Type.Value,EventScope.LocalPlayer,Decision.Timestamp,Actor.Player,Actor.Pad,Plate,true,false,
            EventConfidence.ConfirmedState,"PersonalDelivery:request+release+kitchen+plate",Decision.Team,Actor.Team);}
    }
    public static class PersonalFeedbackPolicy
    {
        public static bool IsDelivery(GameEventType type)
        {return type==GameEventType.DeliverySuccess || type==GameEventType.DeliveryFailure ||
            (type>=GameEventType.DeliveryOrdered && type<=GameEventType.StarMilestone4);}
        public static bool AllowsAutomatic(GameEvent e)
        {
            if(e==null || e.Scope!=EventScope.LocalPlayer || !EventRouter.IsLocalTarget(e,e.LocalPad))return false;
            return e.EventType==GameEventType.ThrowSuccess || e.EventType==GameEventType.CatchSuccess || IsDelivery(e.EventType);
        }
    }
    // Only the calibrated order request -> native release -> committed kitchen -> plate receipt
    // is supported. A conflicting stream is quarantined for this round, never paired by guess.
    public sealed class PersonalDeliveryTracker
    {
        private readonly Dictionary<int,RequestState> requests=new Dictionary<int,RequestState>();
        private readonly Dictionary<int,Pending> pending=new Dictionary<int,Pending>();
        private readonly HashSet<int> blocked=new HashSet<int>();
        private readonly List<int> finished=new List<int>();
        private int unscopedRejections;
        public string LastReason {get;private set;}
        public void Request(PersonalActor actor,int plate,int station,double now)
        {
            if(actor==null)return;
            finished.Clear();foreach(var pair in requests)if(pair.Value.Actor.Chef==actor.Chef)finished.Add(pair.Key);
            foreach(int id in finished)requests.Remove(id);
            if(!actor.Eligible || plate==0 || station==0 || !ValidTime(now))return;
            requests[plate]=new RequestState{Actor=actor,Plate=plate,Station=station,Timestamp=now};
        }
        public void Carry(PersonalActor actor,int before,int after,double now)
        {
            if(before==after || !ValidTime(now))return;
            if(after!=0)requests.Remove(after); // A pickup, even by the same chef, needs a fresh request.
            RequestState request;
            if(before==0 || !requests.TryGetValue(before,out request))return;
            if(!request.Actor.Same(actor) || now<request.Timestamp){requests.Remove(before);return;}
            request.ReleasedAt=now;
        }
        public void Commit(PersonalDeliveryDecision decision)
        {
            if(decision==null || !ValidTime(decision.Timestamp))return;
            if(decision.Station==0 && decision.Type==GameEventType.DeliveryFailure)
            {
                // The actual client protocol omits the failed order's station. Drain
                // its following plate receipt silently, without poisoning a station.
                requests.Clear();pending.Clear();unscopedRejections++;return;
            }
            if(decision.Station==0 || unscopedRejections>0)return;
            Expire(decision.Timestamp);
            if(blocked.Contains(decision.Station))return;
            if(pending.ContainsKey(decision.Station)){Quarantine(decision.Station,"multiple unmatched kitchen results");return;}
            RequestState candidate=null;int count=0;
            if(decision.Trusted && decision.Type.HasValue && PersonalFeedbackPolicy.IsDelivery(decision.Type.Value) && !string.IsNullOrEmpty(decision.Correlation))
                foreach(var request in requests.Values)
                    if(request.Station==decision.Station && request.Actor.Team==decision.Team && request.ReleasedAt.HasValue && request.ReleasedAt.Value<=decision.Timestamp)
                    {candidate=request;count++;}
            pending[decision.Station]=new Pending{Decision=decision,Expected=count==1?candidate:null};
        }
        public PersonalDeliveryConfirmation ReceivePlate(int station,int plate,bool stationAccepted,double now)
        {
            LastReason="";
            if(station==0 || !ValidTime(now))return Drop("invalid native station receipt");
            if(unscopedRejections>0 && plate!=0)
            {
                unscopedRejections--;requests.Remove(plate);pending.Remove(station);
                return Drop("unattributed client rejection drained; no personal feedback");
            }
            Expire(now);
            RequestState request;requests.TryGetValue(plate,out request);requests.Remove(plate);
            Pending observation;pending.TryGetValue(station,out observation);pending.Remove(station);
            if(blocked.Contains(station))return Drop("ambiguous station stream; silent until reset");
            if(plate==0)
            {if(observation!=null)Quarantine(station,"kitchen result without a plate");return Drop("non-plate station receipt");}
            if(observation==null){Quarantine(station,"plate receipt without prior kitchen result");return null;}
            if(observation.Expected==null)return Drop("no explicit local request and matching native release");
            if(!object.ReferenceEquals(request,observation.Expected) || request.Plate!=plate)
            {Quarantine(station,"plate or ownership differs from committed personal candidate");return null;}
            var decision=observation.Decision;
            if(now<decision.Timestamp || now-decision.Timestamp>V16Preset.StaleNormalMs)
            {Quarantine(station,"late or reversed confirmed result");return null;}
            if(!stationAccepted && decision.Type!=GameEventType.DeliveryFailure)
            {Quarantine(station,"station rejected object despite successful kitchen result");return null;}
            return new PersonalDeliveryConfirmation(request.Actor,decision,plate);
        }
        private void Expire(double now)
        {
            finished.Clear();foreach(var pair in pending)if(now-pair.Value.Decision.Timestamp>V16Preset.StaleNormalMs)finished.Add(pair.Key);
            foreach(int station in finished)Quarantine(station,"confirmed association exceeded existing freshness bound");
        }
        private void Quarantine(int station,string reason){blocked.Add(station);pending.Remove(station);LastReason=reason;}
        private PersonalDeliveryConfirmation Drop(string reason){LastReason=reason;return null;}
        private static bool ValidTime(double now){return now>=0 && !double.IsNaN(now) && !double.IsInfinity(now);}
        public void Reset(){requests.Clear();pending.Clear();blocked.Clear();finished.Clear();unscopedRejections=0;LastReason="";}
        private sealed class RequestState
        {internal PersonalActor Actor;internal int Plate,Station;internal double Timestamp;internal double? ReleasedAt;}
        private sealed class Pending {internal PersonalDeliveryDecision Decision;internal RequestState Expected;}
    }
}
