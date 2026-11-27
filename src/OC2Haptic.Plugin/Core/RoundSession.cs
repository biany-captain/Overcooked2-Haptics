namespace OC2Haptic.Core
{
    public sealed class RoundSession
    {
        private int scene,flow,serial;
        private bool active;
        public string Key {get;private set;}
        public bool Observe(int sceneHandle,int flowId,bool inRound)
        {
            if(!inRound || (active && scene==sceneHandle && flow==flowId))return false;
            scene=sceneHandle;flow=flowId;active=true;serial++;
            Key=scene+":"+flow+":"+serial;return true;
        }
        public void End(){active=false;Key=null;}
    }
}
