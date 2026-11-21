using System;
using System.Collections.Generic;
namespace OC2Haptic.Core
{
    public enum ComboTriggerMode { EveryOrderedDelivery, OnLevelUp }
    public sealed class DeliveryPayload
    {
        public readonly string Round,Key; public readonly int Team,Ordinal,Multiplier,RawStreak;
        public readonly bool WasCombo,Authoritative; public readonly double Timestamp;
        public DeliveryPayload(string round,int team,int order,int ordinal,bool combo,int multiplier,int streak,bool authoritative,double time)
        { Round=round;Team=team;Ordinal=ordinal;Multiplier=multiplier;RawStreak=streak;WasCombo=combo;Authoritative=authoritative;Timestamp=time;
            Key=round+":"+team+":"+order+":"+ordinal; }
    }
    public sealed class ComboResult
    {
        public readonly GameEventType Type; public readonly int? Tier; public readonly int RawStreak; public readonly bool Duplicate;
        internal ComboResult(GameEventType type,int? tier,int streak,bool duplicate) {Type=type;Tier=tier;RawStreak=streak;Duplicate=duplicate;}
    }
    public sealed class ComboTracker
    {
        private readonly Dictionary<string,State> states=new Dictionary<string,State>();
        public void ObserveState(string round,int team,int multiplier,bool authoritative)
        {
            string key=round+":"+team;State state;
            if(!states.TryGetValue(key,out state)){state=new State();states[key]=state;}
            state.Tier=authoritative && multiplier>=0?(int?)Math.Min(multiplier,4):null;
        }
        public ComboResult Resolve(DeliveryPayload p,bool enabled,ComboTriggerMode mode,int maxTier)
        {
            string key=p.Round+":"+p.Team; State state;
            if(!states.TryGetValue(key,out state)) {state=new State();states[key]=state;}
            if(state.LastKey==p.Key || (p.Authoritative && p.Ordinal<state.LastOrdinal))
                return new ComboResult(GameEventType.DeliveryUnclassified,null,p.RawStreak,true);
            state.LastKey=p.Key;state.LastOrdinal=p.Ordinal;
            bool ordered=p.Authoritative && p.WasCombo && p.Multiplier>=1 && p.RawStreak>=1;
            bool unordered=p.Authoritative && !p.WasCombo && p.Multiplier==0 && p.RawStreak==0;
            if(!ordered) {state.Tier=null;return new ComboResult(unordered?GameEventType.DeliveryOutOfOrder:GameEventType.DeliveryUnclassified,null,p.RawStreak,false);}
            int tier=Math.Min(Math.Max(maxTier,1),Math.Min(p.Multiplier,4));
            bool levelUp=state.Tier.HasValue && tier>state.Tier.Value;state.Tier=tier;
            var type=GameEventType.DeliveryOrdered;
            if(enabled && tier>=2 && (mode==ComboTriggerMode.EveryOrderedDelivery || levelUp))
                type=(GameEventType)((int)GameEventType.ComboLevel2+tier-2);
            return new ComboResult(type,tier,p.RawStreak,false);
        }
        public void Reset(){states.Clear();}
        private sealed class State {internal string LastKey;internal int LastOrdinal=-1;internal int? Tier;}
    }
}
