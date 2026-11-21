namespace OC2Haptic.Core
{
    public enum StarFeedbackMode { HapticsAndAudio, HapticsOnly, AudioOnly, Disabled }
    public interface IRewardAudioBackend {bool Play(int star,float volume);void Stop();}
}
