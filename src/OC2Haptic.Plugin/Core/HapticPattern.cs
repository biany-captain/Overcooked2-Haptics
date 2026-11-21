using System;

namespace OC2Haptic.Core
{
    public struct HapticSegment
    {
        public readonly float Left;
        public readonly float Right;
        public readonly int DurationMs;

        public HapticSegment(float left, float right, int durationMs)
        {
            if (float.IsNaN(left) || float.IsInfinity(left) || left < 0 || left > 1)
                throw new ArgumentOutOfRangeException("left");
            if (float.IsNaN(right) || float.IsInfinity(right) || right < 0 || right > 1)
                throw new ArgumentOutOfRangeException("right");
            if (durationMs <= 0 || durationMs > 500)
                throw new ArgumentOutOfRangeException("durationMs", "A segment must last 1-500 ms.");
            Left = left; Right = right; DurationMs = durationMs;
        }
    }

    public sealed class HapticPattern
    {
        private readonly HapticSegment[] segments;
        public HapticPattern(params HapticSegment[] segments) : this(false, segments) { }
        private HapticPattern(bool celebration, params HapticSegment[] segments)
        {
            if (segments == null || segments.Length == 0) throw new ArgumentException("A pattern needs segments.", "segments");
            int total = 0;
            foreach (HapticSegment segment in segments)
            {
                if (segment.DurationMs <= 0 || total > (celebration ? 850 : 500) - segment.DurationMs)
                    throw new ArgumentException("Pattern exceeds its action/celebration duration limit.", "segments");
                total += segment.DurationMs;
            }
            this.segments = (HapticSegment[])segments.Clone();
            TotalDurationMs = total;
        }
        public int Count { get { return segments.Length; } }
        public static HapticPattern Celebration(params HapticSegment[] segments) { return new HapticPattern(true, segments); }
        public int TotalDurationMs { get; private set; }
        public HapticSegment SegmentAt(int index) { return segments[index]; }
    }
}
