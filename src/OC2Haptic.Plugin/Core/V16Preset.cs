// Generated from config/haptic_patterns_v1.6.json; run scripts/generate-haptic-preset.ps1.
using System.Collections.Generic;
namespace OC2Haptic.Core
{
    internal static class V16Preset
    {
        internal const int NormalBudgetMs=1200;
        internal const int NegativeBudgetMs=2200;
        internal const int EmergencyBudgetMs=2500;
        internal const int StaleNormalMs=180;
        internal const int FireReminderIntervalMs=5000;
        internal static Dictionary<GameEventType,EventFeedback> Create()
        {
            return new Dictionary<GameEventType,EventFeedback>
            {
                {GameEventType.DeliveryOrdered,new EventFeedback(new HapticPattern(new HapticSegment(0.02f,0.72f,90)),55,300,FeedbackCategory.Positive,true)},
                {GameEventType.DeliveryOutOfOrder,new EventFeedback(new HapticPattern(new HapticSegment(0.29f,0.02f,125),new HapticSegment(0f,0f,90),new HapticSegment(0.17f,0f,100)),63,600,FeedbackCategory.Neutral,true)},
                {GameEventType.DeliveryFailure,new EventFeedback(HapticPattern.Extended(new HapticSegment(0.72f,0.02f,210),new HapticSegment(0.47f,0f,95),new HapticSegment(0.24f,0f,125)),80,1200,FeedbackCategory.Negative,true)},
                {GameEventType.ComboLevel2,new EventFeedback(new HapticPattern(new HapticSegment(0f,0.49f,70),new HapticSegment(0f,0f,70),new HapticSegment(0f,0.66f,95)),68,650,FeedbackCategory.Combo,true)},
                {GameEventType.ComboLevel3,new EventFeedback(new HapticPattern(new HapticSegment(0f,0.47f,65),new HapticSegment(0f,0f,65),new HapticSegment(0f,0.55f,70),new HapticSegment(0f,0f,65),new HapticSegment(0f,0.72f,110)),68,650,FeedbackCategory.Combo,true)},
                {GameEventType.ComboLevel4,new EventFeedback(new HapticPattern(new HapticSegment(0f,0.47f,60),new HapticSegment(0f,0f,60),new HapticSegment(0f,0.55f,60),new HapticSegment(0f,0f,60),new HapticSegment(0f,0.62f,60),new HapticSegment(0f,0f,60),new HapticSegment(0.12f,0.79f,130)),68,700,FeedbackCategory.Combo,true)},
                {GameEventType.OrderExpired,new EventFeedback(HapticPattern.Extended(new HapticSegment(0.61f,0.02f,190),new HapticSegment(0f,0f,85),new HapticSegment(0.55f,0f,230)),83,1800,FeedbackCategory.Negative,true)},
                {GameEventType.BurnWarning,new EventFeedback(HapticPattern.Extended(new HapticSegment(0.44f,0.09f,150),new HapticSegment(0f,0f,120),new HapticSegment(0.52f,0.09f,180)),92,5000,FeedbackCategory.Emergency,true)},
                {GameEventType.FoodBurned,new EventFeedback(HapticPattern.Extended(new HapticSegment(0.66f,0f,230),new HapticSegment(0.45f,0f,90),new HapticSegment(0.23f,0f,150)),88,2500,FeedbackCategory.Negative,false)},
                {GameEventType.FireStart,new EventFeedback(HapticPattern.Extended(new HapticSegment(0.81f,0.1f,260),new HapticSegment(0f,0f,90),new HapticSegment(0.69f,0.06f,310),new HapticSegment(0.3f,0f,160)),100,5000,FeedbackCategory.Emergency,true)},
                {GameEventType.FireReminder,new EventFeedback(HapticPattern.Extended(new HapticSegment(0.42f,0.08f,160),new HapticSegment(0f,0f,130),new HapticSegment(0.42f,0.08f,180)),95,5000,FeedbackCategory.Emergency,true)},
                {GameEventType.ThrowSuccess,new EventFeedback(new HapticPattern(new HapticSegment(0.06f,0.33f,75)),20,270,FeedbackCategory.Positive,false)},
                {GameEventType.CatchSuccess,new EventFeedback(new HapticPattern(new HapticSegment(0f,0.32f,65),new HapticSegment(0f,0f,60),new HapticSegment(0f,0.39f,80)),20,300,FeedbackCategory.Positive,false)},
            };
        }
    }
}
