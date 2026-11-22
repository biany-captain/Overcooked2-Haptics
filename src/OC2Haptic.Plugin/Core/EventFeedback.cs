using System;
using System.Collections.Generic;
namespace OC2Haptic.Core
{
    public enum FeedbackCategory { Positive, Neutral, Combo, Negative, Emergency }
    public sealed class EventFeedback
    {
        public readonly HapticPattern Pattern;
        public readonly int Priority, CooldownMs;
        public readonly FeedbackCategory Category;
        public readonly bool DefaultEnabled;
        internal EventFeedback(HapticPattern pattern,int priority,int cooldown=120,FeedbackCategory category=FeedbackCategory.Combo,bool enabled=true)
        {Pattern=pattern;Priority=priority;CooldownMs=cooldown;Category=category;DefaultEnabled=enabled;}
        private static readonly Dictionary<GameEventType,EventFeedback> Profiles=V16Preset.Create();
        private static readonly EventFeedback LegacySuccess=new EventFeedback(new HapticPattern(
            new HapticSegment(0,.20f,70),new HapticSegment(0,0,80),new HapticSegment(0,.24f,70)),30,120,FeedbackCategory.Positive);
        private static readonly EventFeedback Test=new EventFeedback(new HapticPattern(new HapticSegment(.22f,.28f,80)),10,120,FeedbackCategory.Positive);
        private static readonly EventFeedback[] Stars = {
            new EventFeedback(HapticPattern.Celebration(new HapticSegment(.10f,.22f,55),new HapticSegment(0,0,90),new HapticSegment(.05f,.17f,35)),86),
            new EventFeedback(HapticPattern.Celebration(new HapticSegment(.14f,.23f,70),new HapticSegment(0,0,105),new HapticSegment(.17f,.35f,110)),86),
            new EventFeedback(HapticPattern.Celebration(new HapticSegment(.18f,.26f,55),new HapticSegment(0,0,70),new HapticSegment(.23f,.34f,80),
                new HapticSegment(0,0,75),new HapticSegment(.32f,.43f,145)),86),
            new EventFeedback(HapticPattern.Celebration(new HapticSegment(.20f,.28f,70),new HapticSegment(0,0,75),new HapticSegment(.24f,.34f,75),
                new HapticSegment(0,0,75),new HapticSegment(.31f,.43f,85),new HapticSegment(0,0,90),new HapticSegment(.43f,.51f,190)),86)
        };
        public static readonly HapticPattern OneStarHapticsOnly = Stars[0].Pattern;

        public static EventFeedback For(GameEventType type)
        {
            if(GameEventTypes.IsStar(type))return Stars[GameEventTypes.StarLevel(type)-1];
            if(type==GameEventType.DeliveryUnclassified)type=GameEventType.DeliveryOrdered;
            if(type==GameEventType.DeliverySuccess)return LegacySuccess;
            if(type==GameEventType.TestPulse)return Test;
            EventFeedback value;if(Profiles.TryGetValue(type,out value))return value;
            throw new ArgumentOutOfRangeException("type");
        }
        public string Key(GameEvent gameEvent)
        {
            if(gameEvent.Scope==EventScope.LocalTeam)return gameEvent.EventType+":"+gameEvent.TeamIdentity+":"+gameEvent.EntityIdentity;
            return gameEvent.EventType.ToString();
        }
    }
}
