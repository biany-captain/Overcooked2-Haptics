using System;

namespace OC2Haptic.Core
{
    public sealed class EventFeedback
    {
        public readonly HapticPattern Pattern;
        public readonly int Priority;
        public readonly int CooldownMs = 120;
        private EventFeedback(HapticPattern pattern, int priority) { Pattern = pattern; Priority = priority; }
        private static readonly EventFeedback Throw = new EventFeedback(new HapticPattern(new HapticSegment(.15f, .32f, 65)), 20);
        private static readonly EventFeedback Catch = new EventFeedback(new HapticPattern(
            new HapticSegment(.22f,.10f,35),new HapticSegment(0,0,35),new HapticSegment(.08f,.30f,45)),25);
        private static readonly EventFeedback Success = new EventFeedback(new HapticPattern(
            new HapticSegment(0, .20f, 70), new HapticSegment(0, 0, 80), new HapticSegment(0, .24f, 70)), 30);
        private static readonly EventFeedback Failure = new EventFeedback(new HapticPattern(new HapticSegment(.34f, 0, 220)), 55);
        private static readonly EventFeedback Expired = new EventFeedback(new HapticPattern(new HapticSegment(.40f, .30f, 250)), 85);
        private static readonly EventFeedback Ordered = new EventFeedback(new HapticPattern(
            new HapticSegment(0,.25f,70),new HapticSegment(0,0,80),new HapticSegment(.18f,.32f,100)),30);
        private static readonly EventFeedback OutOfOrder = new EventFeedback(new HapticPattern(new HapticSegment(.38f,.08f,160)),42);
        private static readonly EventFeedback Unknown = new EventFeedback(new HapticPattern(new HapticSegment(0,.14f,70)),20);
        private static readonly EventFeedback Level2 = new EventFeedback(new HapticPattern(
            new HapticSegment(.12f,.28f,60),new HapticSegment(0,0,55),new HapticSegment(.20f,.38f,110)),32);
        private static readonly EventFeedback Level3 = new EventFeedback(new HapticPattern(
            new HapticSegment(0,.30f,55),new HapticSegment(0,0,50),new HapticSegment(0,.35f,60),
            new HapticSegment(0,0,50),new HapticSegment(.22f,.43f,80)),34);
        private static readonly EventFeedback Level4 = new EventFeedback(new HapticPattern(
            new HapticSegment(0,.31f,55),new HapticSegment(0,0,40),new HapticSegment(0,.35f,55),new HapticSegment(0,0,40),
            new HapticSegment(0,.39f,55),new HapticSegment(0,0,40),new HapticSegment(.25f,.48f,95)),36);
        private static readonly EventFeedback[] Stars = {
            new EventFeedback(HapticPattern.Celebration(new HapticSegment(.05f,.22f,75)),46),
            new EventFeedback(HapticPattern.Celebration(new HapticSegment(.06f,.26f,65),new HapticSegment(0,0,55),new HapticSegment(.14f,.34f,95)),47),
            new EventFeedback(HapticPattern.Celebration(new HapticSegment(.07f,.26f,55),new HapticSegment(0,0,45),new HapticSegment(.13f,.33f,65),
                new HapticSegment(0,0,45),new HapticSegment(.20f,.42f,105)),48),
            new EventFeedback(HapticPattern.Celebration(new HapticSegment(.09f,.26f,55),new HapticSegment(0,0,45),new HapticSegment(.12f,.34f,60),
                new HapticSegment(0,0,45),new HapticSegment(.18f,.42f,65),new HapticSegment(0,0,60),new HapticSegment(.32f,.48f,170)),49)
        };
        public static readonly HapticPattern OneStarHapticsOnly = HapticPattern.Celebration(
            new HapticSegment(.05f,.22f,75),new HapticSegment(0,0,45),new HapticSegment(.03f,.13f,25));

        public static EventFeedback For(GameEventType type)
        {
            switch (type)
            {
                case GameEventType.ThrowSuccess: return Throw;
                case GameEventType.CatchSuccess: return Catch;
                case GameEventType.DeliverySuccess: return Success;
                case GameEventType.DeliveryFailure: return Failure;
                case GameEventType.OrderExpired: return Expired;
                case GameEventType.DeliveryOrdered: return Ordered;
                case GameEventType.DeliveryOutOfOrder: return OutOfOrder;
                case GameEventType.DeliveryUnclassified: return Unknown;
                case GameEventType.ComboLevel2: return Level2;
                case GameEventType.ComboLevel3: return Level3;
                case GameEventType.ComboLevel4: return Level4;
                case GameEventType.StarMilestone1: case GameEventType.StarMilestone2:
                case GameEventType.StarMilestone3: case GameEventType.StarMilestone4: return Stars[GameEventTypes.StarLevel(type)-1];
                default: throw new ArgumentOutOfRangeException("type");
            }
        }

        public string Key(GameEvent gameEvent)
        {
            if (gameEvent.Scope == EventScope.LocalTeam)
                return gameEvent.EventType + ":" + gameEvent.TeamIdentity + ":" + gameEvent.EntityIdentity;
            return gameEvent.EventType.ToString();
        }
    }
}
