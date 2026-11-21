using System;
namespace OC2Haptic.Core
{
    public sealed class RewardOutput
    {
        private readonly HapticEngine engine;private readonly IRewardAudioBackend audio;
        private readonly Action<string> log;
        private bool audioFaulted;
        private StarFeedbackMode mode=StarFeedbackMode.HapticsAndAudio;
        public RewardOutput(HapticEngine engine,IRewardAudioBackend audio,Action<string> log=null){this.engine=engine;this.audio=audio;this.log=log;}
        public StarFeedbackMode Mode {get{return mode;}set{if(mode!=value){Stop();mode=value;}}}
        public bool Emit(int index,string key,GameEventType type,HapticPattern pattern,double now,float intensity,float volume,bool useDefaultPattern=true)
        {
            EventFeedback feedback=EventFeedback.For(type);bool star=GameEventTypes.IsStar(type);
            if(star && mode==StarFeedbackMode.Disabled)return false;
            if(star && engine.IsBusyAtOrAbove(index,feedback.Priority,now))return false;
            bool haptics=!star || mode!=StarFeedbackMode.AudioOnly;
            if(pattern==null && useDefaultPattern)pattern=star && type==GameEventType.StarMilestone1 && mode==StarFeedbackMode.HapticsOnly
                ?EventFeedback.OneStarHapticsOnly:feedback.Pattern;
            bool accepted=haptics && engine.Play(index,key,pattern,feedback.Priority,feedback.CooldownMs,now,intensity);
            if(star && (mode==StarFeedbackMode.HapticsAndAudio || mode==StarFeedbackMode.AudioOnly))
            {
                float gain=SafeVolume(volume);
                if(gain>0 && !audioFaulted)
                    try {accepted=audio.Play(GameEventTypes.StarLevel(type),gain)||accepted;}catch(Exception error){AudioFailed(error);}
            }
            return accepted;
        }
        public static float SafeVolume(float value){return float.IsNaN(value)||float.IsInfinity(value)?0:Math.Max(0,Math.Min(1,value));}
        private void AudioFailed(Exception error)
        {
            bool first=!audioFaulted;audioFaulted=true;
            if(first && log!=null)log("Audio backend failed; haptics and cleanup continue: "+error);
        }
        public void Interrupt(){try{audio.Stop();}catch(Exception error){AudioFailed(error);}}
        public void Stop(){engine.StopAll();Interrupt();}
    }
}
