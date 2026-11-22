namespace OC2Haptic.Core
{
    public static class StarAudioPolicy
    {
        public static bool CanPlay(int star){return star==3 || star==4;}
        public static StarFeedbackMode ModeFor(int star,StarFeedbackMode requested)
        {
            if(star<1 || star>4)return StarFeedbackMode.Disabled;
            if(CanPlay(star))return requested;
            return requested==StarFeedbackMode.HapticsAndAudio || requested==StarFeedbackMode.HapticsOnly
                ?StarFeedbackMode.HapticsOnly:StarFeedbackMode.Disabled;
        }
        public static bool DurationAllowed(int star,int ms,bool legacyFour)
        {return star==3?ms>0 && ms<=11000:star==4 && ms>0 && ms<=11000 && (legacyFour || ms>=9000);}
    }
}
