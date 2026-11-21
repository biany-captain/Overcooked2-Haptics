using System;
namespace OC2Haptic.Core
{
    public enum StarFeedbackMode { HapticsAndAudio, HapticsOnly, AudioOnly, Disabled }
    public interface IRewardAudioBackend {bool Play(int star,float volume);void Stop();}
    public static class RewardTone
    {
        public const int SampleRate=44100;
        public static float[] Generate(int star,int maxDurationMs)
        {
            if(star<1 || star>4)throw new ArgumentOutOfRangeException("star");
            int duration=Math.Min(Math.Max(maxDurationMs,1),new[]{180,310,440,700}[star-1]);
            var data=new float[SampleRate*duration/1000];var frequencies=new[]{880.0,1108.7305,1318.5102,1760.0};
            double peak=0;
            for(int i=0;i<data.Length;i++) {double value=0,time=(double)i/SampleRate;
                for(int n=0;n<star;n++){double t=time-n*.115;if(t<0)continue;
                    double remaining=(double)(data.Length-1-i)/SampleRate;
                    double envelope=Math.Min(1,t/.005)*Math.Min(1,remaining/.012)*Math.Exp(-7*t);
                    value+=.42*envelope*(Math.Sin(2*Math.PI*frequencies[n]*t)+.22*Math.Sin(4*Math.PI*frequencies[n]*t));}
                data[i]=(float)value;peak=Math.Max(peak,Math.Abs(value));}
            if(peak>.6)for(int i=0;i<data.Length;i++)data[i]=(float)(data[i]*.6/peak);
            return data;
        }
    }
}
