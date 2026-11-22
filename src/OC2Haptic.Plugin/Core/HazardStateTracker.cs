using System.Collections.Generic;
namespace OC2Haptic.Core
{
    public enum CookingHazardState { None, Warning, Burned }
    public struct FireTransition
    {public readonly bool Started,Cleared;public readonly int Incident;internal FireTransition(bool started,bool cleared,int incident){Started=started;Cleared=cleared;Incident=incident;}}
    public sealed class HazardStateTracker
    {
        private readonly HashSet<int> fires=new HashSet<int>();
        private readonly Dictionary<int,CookingHazardState> cooking=new Dictionary<int,CookingHazardState>();
        private double nextReminder;
        public int Incident {get;private set;}
        public bool FireActive {get{return fires.Count>0;}}
        public FireTransition ObserveFire(int entity,bool active,double now)
        {
            bool before=FireActive;if(active)fires.Add(entity);else fires.Remove(entity);
            bool started=!before && FireActive,cleared=before && !FireActive;
            if(started){Incident++;nextReminder=now+V16Preset.FireReminderIntervalMs;}
            return new FireTransition(started,cleared,Incident);
        }
        public bool ReminderDue(double now)
        {if(!FireActive || now<nextReminder)return false;nextReminder=now+V16Preset.FireReminderIntervalMs;return true;}
        public void DeferReminders(double now){if(FireActive)nextReminder=now+V16Preset.FireReminderIntervalMs;}
        public GameEventType? ObserveCooking(int entity,CookingHazardState state)
        {
            CookingHazardState previous;if(!cooking.TryGetValue(entity,out previous))previous=CookingHazardState.None;
            if(state==CookingHazardState.None)cooking.Remove(entity);else cooking[entity]=state;
            if(previous==state)return null;
            return state==CookingHazardState.Warning?(GameEventType?)GameEventType.BurnWarning:
                state==CookingHazardState.Burned?(GameEventType?)GameEventType.FoodBurned:null;
        }
        public bool IsCurrent(GameEventType type,int entity)
        {
            if(type==GameEventType.FireStart || type==GameEventType.FireReminder)return FireActive && entity==Incident;
            CookingHazardState state;if(!cooking.TryGetValue(entity,out state))return false;
            return type==GameEventType.BurnWarning?state==CookingHazardState.Warning:type==GameEventType.FoodBurned && state==CookingHazardState.Burned;
        }
        public void Reset(){fires.Clear();cooking.Clear();Incident=0;nextReminder=0;}
    }
}
