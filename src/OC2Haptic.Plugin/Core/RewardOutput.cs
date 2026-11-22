using System;
namespace OC2Haptic.Core
{
    public sealed class RewardOutput
    {
        private readonly HapticEngine engine;private readonly IRewardAudioBackend audio;
        private readonly Action<string> log;
        private readonly Func<int,int> audioDurationMs;
        private double audioUntil;
        private int audioStar;
        private readonly string[] starMotorKeys=new string[4];
        private readonly int[] motorStars=new int[4];
        public int AudioEmissionSequence {get;private set;}
        private bool audioFaulted;
        private StarFeedbackMode mode=StarFeedbackMode.HapticsAndAudio;
        public RewardOutput(HapticEngine engine,IRewardAudioBackend audio,Action<string> log=null,Func<int,int> audioDurationMs=null)
        {this.engine=engine;this.audio=audio;this.log=log;this.audioDurationMs=audioDurationMs??(star=>0);}
        public StarFeedbackMode Mode
        {
            get{return mode;}
            set
            {
                if(mode==value)return;
                if(HasHaptics(mode) && !HasHaptics(value))engine.StopAll();
                if(HasAudio(mode) && !HasAudio(value))Interrupt();
                mode=value;
            }
        }
        private static bool HasHaptics(StarFeedbackMode value){return value==StarFeedbackMode.HapticsAndAudio || value==StarFeedbackMode.HapticsOnly;}
        private static bool HasAudio(StarFeedbackMode value){return value==StarFeedbackMode.HapticsAndAudio || value==StarFeedbackMode.AudioOnly;}
        public bool Emit(int index,string key,GameEventType type,HapticPattern pattern,double now,float intensity,float volume,bool useDefaultPattern=true,
            StarFeedbackMode? previewMode=null)
        {
            EventFeedback feedback=EventFeedback.For(type);bool star=GameEventTypes.IsStar(type);
            int level=GameEventTypes.StarLevel(type);
            StarFeedbackMode requested=previewMode??StarFeedbackMode.HapticsAndAudio;
            bool haptics=HasHaptics(mode) && (!star || HasHaptics(requested));
            bool sound=star && HasAudio(mode) && HasAudio(requested);
            StarFeedbackMode effectiveMode=haptics?(sound?StarFeedbackMode.HapticsAndAudio:StarFeedbackMode.HapticsOnly)
                :(sound?StarFeedbackMode.AudioOnly:StarFeedbackMode.Disabled);
            if(star && !haptics && !sound)return false;
            if(mode==StarFeedbackMode.Disabled)return false;
            bool oldStar=index>=0 && index<4 && engine.IsPlaying(index,starMotorKeys[index],now);
            if(star && sound && now<audioUntil && level<=audioStar)return false;
            if(star && engine.IsBusyAtOrAbove(index,feedback.Priority,now) && !(oldStar && level>motorStars[index]))return false;
            if(star)engine.Cancel(index);
            bool negative=feedback.Category==FeedbackCategory.Negative || feedback.Category==FeedbackCategory.Emergency;
            if(!star && negative && oldStar)engine.Cancel(index);
            bool interrupted=false;
            if((star && sound && now<audioUntil) || (!star && negative)){interrupted=now<audioUntil;Interrupt();}
            if(pattern==null && useDefaultPattern)pattern=star && type==GameEventType.StarMilestone1 && effectiveMode==StarFeedbackMode.HapticsOnly
                ?EventFeedback.OneStarHapticsOnly:feedback.Pattern;
            bool motorAccepted=haptics && engine.Play(index,key,pattern,feedback.Priority,feedback.CooldownMs,now,intensity);
            bool accepted=motorAccepted || interrupted;
            if(motorAccepted && index>=0 && index<4){starMotorKeys[index]=star?key:null;motorStars[index]=star?level:0;}
            if(sound)
            {
                float gain=SafeVolume(volume);
                if(gain>0 && !audioFaulted)
                    try
                    {
                        if(audio.Play(level,gain)){accepted=true;audioUntil=now+audioDurationMs(level);audioStar=level;AudioEmissionSequence++;}
                    }
                    catch(Exception error){AudioFailed(error);}
            }
            if(star && accepted && log!=null)log("Reward timeline | star="+GameEventTypes.StarLevel(type)+" startMs="+now+
                " motorWindowMs="+(motorAccepted && pattern!=null?pattern.TotalDurationMs:0)+" mode="+effectiveMode+" plannedAudioEndMs="+audioUntil);
            return accepted;
        }
        public static float SafeVolume(float value){return float.IsNaN(value)||float.IsInfinity(value)?0:Math.Max(0,Math.Min(1,value));}
        private void AudioFailed(Exception error)
        {
            bool first=!audioFaulted;audioFaulted=true;
            if(first && log!=null)log("Audio backend failed; haptics and cleanup continue: "+error);
        }
        public void Interrupt(){audioUntil=0;audioStar=0;try{audio.Stop();}catch(Exception error){AudioFailed(error);}}
        public void Stop(){engine.StopAll();Interrupt();}
    }
}
