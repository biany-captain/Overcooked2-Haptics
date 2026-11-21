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
        private static readonly EventFeedback Catch = new EventFeedback(new HapticPattern(new HapticSegment(.25f, .18f, 90)), 25);
        private static readonly EventFeedback Success = new EventFeedback(new HapticPattern(
            new HapticSegment(0, .20f, 70), new HapticSegment(0, 0, 80), new HapticSegment(0, .24f, 70)), 30);
        private static readonly EventFeedback Failure = new EventFeedback(new HapticPattern(new HapticSegment(.34f, 0, 220)), 55);
        private static readonly EventFeedback Expired = new EventFeedback(new HapticPattern(new HapticSegment(.40f, .30f, 250)), 85);

        public static EventFeedback For(GameEventType type)
        {
            switch (type)
            {
                case GameEventType.ThrowSuccess: return Throw;
                case GameEventType.CatchSuccess: return Catch;
                case GameEventType.DeliverySuccess: return Success;
                case GameEventType.DeliveryFailure: return Failure;
                case GameEventType.OrderExpired: return Expired;
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
