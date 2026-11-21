using System;
namespace OC2Haptic.Core
{
    public sealed class RewardOutput
    {
        private readonly HapticEngine engine;private readonly IRewardAudioBackend audio;
        private readonly Action<string> log;
        private readonly Func<int,int> audioDurationMs;
        private double audioUntil;
        private int audioPriority;
        private bool audioFaulted;
        private StarFeedbackMode mode=StarFeedbackMode.HapticsAndAudio;
        public RewardOutput(HapticEngine engine,IRewardAudioBackend audio,Action<string> log=null,Func<int,int> audioDurationMs=null)
        {this.engine=engine;this.audio=audio;this.log=log;this.audioDurationMs=audioDurationMs??(star=>RewardTone.DurationMs(star,1000));}
        public StarFeedbackMode Mode {get{return mode;}set{if(mode!=value){Stop();mode=value;}}}
        public bool Emit(int index,string key,GameEventType type,HapticPattern pattern,double now,float intensity,float volume,bool useDefaultPattern=true,
            StarFeedbackMode? previewMode=null)
        {
            EventFeedback feedback=EventFeedback.For(type);bool star=GameEventTypes.IsStar(type);
            StarFeedbackMode effectiveMode=previewMode??mode;
            if(star && effectiveMode==StarFeedbackMode.Disabled)return false;
            if(star && now<audioUntil && feedback.Priority<=audioPriority)return false;
            if(star && engine.IsBusyAtOrAbove(index,feedback.Priority,now))return false;
            if((star && now<audioUntil) || (!star && feedback.Priority>=55))Interrupt();
            bool haptics=!star || effectiveMode!=StarFeedbackMode.AudioOnly;
            if(pattern==null && useDefaultPattern)pattern=star && type==GameEventType.StarMilestone1 && effectiveMode==StarFeedbackMode.HapticsOnly
                ?EventFeedback.OneStarHapticsOnly:feedback.Pattern;
            bool accepted=haptics && engine.Play(index,key,pattern,feedback.Priority,feedback.CooldownMs,now,intensity);
            if(star && (effectiveMode==StarFeedbackMode.HapticsAndAudio || effectiveMode==StarFeedbackMode.AudioOnly))
            {
                float gain=SafeVolume(volume);
                if(gain>0 && !audioFaulted)
                    try
                    {
                        int level=GameEventTypes.StarLevel(type);
                        if(audio.Play(level,gain)){accepted=true;audioUntil=now+audioDurationMs(level);audioPriority=feedback.Priority;}
                    }
                    catch(Exception error){AudioFailed(error);}
            }
            if(star && accepted && log!=null)log("Reward timeline | star="+GameEventTypes.StarLevel(type)+" startMs="+now+
                " motorWindowMs="+(haptics && pattern!=null?pattern.TotalDurationMs:0)+" mode="+effectiveMode+" plannedAudioEndMs="+audioUntil);
            return accepted;
        }
        public static float SafeVolume(float value){return float.IsNaN(value)||float.IsInfinity(value)?0:Math.Max(0,Math.Min(1,value));}
        private void AudioFailed(Exception error)
        {
            bool first=!audioFaulted;audioFaulted=true;
            if(first && log!=null)log("Audio backend failed; haptics and cleanup continue: "+error);
        }
        public void Interrupt(){audioUntil=0;audioPriority=0;try{audio.Stop();}catch(Exception error){AudioFailed(error);}}
        public void Stop(){engine.StopAll();Interrupt();}
    }
}
