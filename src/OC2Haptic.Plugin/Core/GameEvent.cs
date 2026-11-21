namespace OC2Haptic.Core
{
    public enum EventConfidence { InputIntent, ConfirmedState }
    public enum GameEventType { ThrowSuccess, CatchSuccess, DeliverySuccess, DeliveryFailure, OrderExpired,
        DeliveryOrdered, DeliveryOutOfOrder, DeliveryUnclassified, ComboLevel2, ComboLevel3, ComboLevel4,
        StarMilestone1, StarMilestone2, StarMilestone3, StarMilestone4 }
    public static class GameEventTypes
    {
        public static bool IsStar(GameEventType type) { return type >= GameEventType.StarMilestone1 && type <= GameEventType.StarMilestone4; }
        public static int StarLevel(GameEventType type) { return IsStar(type) ? (int)type - (int)GameEventType.StarMilestone1 + 1 : 0; }
        public static bool IsCombo(GameEventType type) { return type >= GameEventType.ComboLevel2 && type <= GameEventType.ComboLevel4; }
    }
    public enum EventScope { LocalPlayer, LocalTeam, AllLocal, Unknown }

    public sealed class GameEvent
    {
        public readonly double TimestampMs;
        public readonly GameEventType EventType;
        public readonly EventScope Scope;
        public readonly int? TeamIdentity;
        public readonly int? LocalTeamIdentity;
        public readonly int PlayerIdentity;
        public readonly int LocalPad;
        public readonly int EntityIdentity;
        public readonly bool IsLocal;
        public readonly bool IsKeyboard;
        public readonly EventConfidence Confidence;
        public readonly string SourceHook;

        public GameEvent(double timestampMs, int playerIdentity, int localPad, int entityIdentity,
            bool isLocal, bool isKeyboard, EventConfidence confidence, string sourceHook)
            : this(GameEventType.ThrowSuccess, EventScope.LocalPlayer, timestampMs, playerIdentity, localPad,
                entityIdentity, isLocal, isKeyboard, confidence, sourceHook) { }

        public GameEvent(GameEventType eventType, EventScope scope, double timestampMs, int playerIdentity,
            int localPad, int entityIdentity, bool isLocal, bool isKeyboard, EventConfidence confidence,
            string sourceHook, int? teamIdentity = null, int? localTeamIdentity = null)
        {
            EventType = eventType; Scope = scope; TeamIdentity = teamIdentity; LocalTeamIdentity = localTeamIdentity;
            TimestampMs = timestampMs; PlayerIdentity = playerIdentity; LocalPad = localPad;
            EntityIdentity = entityIdentity; IsLocal = isLocal; IsKeyboard = isKeyboard;
            Confidence = confidence; SourceHook = sourceHook;
        }
    }

    public struct ControllerBinding
    {
        public readonly int LocalPad;
        public readonly int XInputIndex;
        public ControllerBinding(int localPad, int xInputIndex) { LocalPad = localPad; XInputIndex = xInputIndex; }
    }

    public static class EventRouter
    {
        public static int Resolve(GameEvent gameEvent, ControllerBinding binding)
        {
            if(binding.XInputIndex<0 || binding.XInputIndex>3 || !IsLocalTarget(gameEvent,binding.LocalPad))return -1;
            return binding.XInputIndex;
        }
        public static bool IsLocalTarget(GameEvent gameEvent,int localPad)
        {
            if (gameEvent == null || !gameEvent.IsLocal || gameEvent.IsKeyboard ||
                gameEvent.Confidence != EventConfidence.ConfirmedState ||
                gameEvent.PlayerIdentity < 0 || gameEvent.PlayerIdentity >= 11 ||
                gameEvent.LocalPad < 0 || gameEvent.LocalPad >= 15 ||
                localPad != gameEvent.LocalPad)
                return false;
            bool personal = gameEvent.EventType == GameEventType.ThrowSuccess || gameEvent.EventType == GameEventType.CatchSuccess;
            if (personal) return gameEvent.Scope == EventScope.LocalPlayer;
            bool teamResult = gameEvent.EventType == GameEventType.DeliverySuccess ||
                gameEvent.EventType == GameEventType.DeliveryFailure || gameEvent.EventType == GameEventType.OrderExpired ||
                (gameEvent.EventType >= GameEventType.DeliveryOrdered && gameEvent.EventType <= GameEventType.StarMilestone4);
            if (!teamResult || gameEvent.Scope != EventScope.LocalTeam || !gameEvent.TeamIdentity.HasValue ||
                !gameEvent.LocalTeamIdentity.HasValue || gameEvent.TeamIdentity.Value < 0 || gameEvent.TeamIdentity.Value > 1 ||
                gameEvent.TeamIdentity != gameEvent.LocalTeamIdentity) return false;
            return true;
        }
    }
}
