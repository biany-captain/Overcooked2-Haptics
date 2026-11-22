using System;
namespace OC2Haptic.Core
{
    public static class FeedbackFocus
    {
        public static void Apply(bool focused,bool respectGameFocus,Action<bool> musicFocus,Action stopFeedback)
        {
            musicFocus(focused);
            if(!focused && respectGameFocus)stopFeedback();
        }
    }
}
