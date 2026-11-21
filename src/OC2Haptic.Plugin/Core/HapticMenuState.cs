namespace OC2Haptic.Core
{
    public sealed class HapticMenuState
    {
        private readonly HapticEngine engine;
        private readonly HapticPattern generic = new HapticPattern(new HapticSegment(.22f, .28f, 80));
        public bool IsOpen { get; private set; }
        public HapticMenuState(HapticEngine engine) { this.engine = engine; }
        public void Toggle()
        {
            engine.StopAll();
            IsOpen = !IsOpen;
        }
        public void Close()
        {
            if (!IsOpen) return;
            engine.StopAll();
            IsOpen = false;
        }
        public bool Preview(GameEventType? type, int index, bool globallyEnabled, float intensity, double nowMs)
        {
            if (!IsOpen || !globallyEnabled) return false;
            EventFeedback feedback = type.HasValue ? EventFeedback.For(type.Value) : null;
            return engine.Play(index, "preview:" + (type.HasValue ? type.Value.ToString() : "generic"),
                feedback == null ? generic : feedback.Pattern, feedback == null ? 10 : feedback.Priority,
                120, nowMs, intensity);
        }
    }
}
