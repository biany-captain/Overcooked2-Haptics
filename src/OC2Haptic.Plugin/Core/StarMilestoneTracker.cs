using System.Collections.Generic;
namespace OC2Haptic.Core
{
    public sealed class ScoreSnapshot
    {
        public readonly string Round; public readonly int Team,Score; public readonly ScoreThresholds Thresholds;
        public readonly bool Authoritative; public readonly double Timestamp;
        public ScoreSnapshot(string round,int team,int score,ScoreThresholds thresholds,bool authoritative,double timestamp)
        {Round=round;Team=team;Score=score;Thresholds=thresholds;Authoritative=authoritative;Timestamp=timestamp;}
        internal string Key {get{return Round+":"+Team;}}
    }
    public sealed class StarMilestoneTracker
    {
        private readonly Dictionary<string,State> states=new Dictionary<string,State>();
        public void Prime(ScoreSnapshot s)
        {
            State state;
            if(!states.TryGetValue(s.Key,out state) || !state.Synchronized || !SameThresholds(state.Thresholds,s.Thresholds))Observe(s,false);
        }
        public int Observe(ScoreSnapshot s){return Observe(s,true);}
        public int Observe(ScoreSnapshot s,bool enabled)
        {
            if(string.IsNullOrEmpty(s.Round) || s.Team<0 || s.Team>1)return 0;
            State state;
            if(!states.TryGetValue(s.Key,out state)){state=new State();states[s.Key]=state;}
            if(!s.Authoritative || s.Thresholds==null){state.Synchronized=false;return 0;}
            if(s.Timestamp<state.Timestamp)return 0;
            bool initial=!state.Synchronized || !SameThresholds(state.Thresholds,s.Thresholds);
            state.Thresholds=s.Thresholds;state.Synchronized=true;
            state.Timestamp=s.Timestamp;int selected=0;
            for(int i=0;i<s.Thresholds.Count;i++) if(s.Score>=s.Thresholds.At(i) && (state.Announced&(1<<i))==0)
            {state.Announced|=1<<i;selected=i+1;}
            return initial || !enabled?0:selected;
        }
        public void Reset(){states.Clear();}
        public void Suspend(){foreach(var state in states.Values)state.Synchronized=false;}
        private static bool SameThresholds(ScoreThresholds a,ScoreThresholds b)
        {if(a==null||b==null||a.Count!=b.Count)return false;for(int i=0;i<a.Count;i++)if(a.At(i)!=b.At(i))return false;return true;}
        private sealed class State {internal ScoreThresholds Thresholds;internal int Announced;internal double Timestamp;internal bool Synchronized;}
    }
}
