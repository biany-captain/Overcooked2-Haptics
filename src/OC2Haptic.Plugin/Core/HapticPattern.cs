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
        public int MaxDurationMs {get;private set;}
        public HapticPattern(params HapticSegment[] segments) : this(500, segments) { }
        private HapticPattern(int maximum, params HapticSegment[] segments)
        {
            if (segments == null || segments.Length == 0) throw new ArgumentException("A pattern needs segments.", "segments");
            int total = 0;
            foreach (HapticSegment segment in segments)
            {
                if (segment.DurationMs <= 0 || total > maximum - segment.DurationMs)
                    throw new ArgumentException("Pattern exceeds its action/celebration duration limit.", "segments");
                total += segment.DurationMs;
            }
            this.segments = (HapticSegment[])segments.Clone();
            MaxDurationMs=maximum;
            foreach(var segment in segments)if(segment.Left>0 || segment.Right>0)HasMotorOutput=true;
            TotalDurationMs = total;
        }
        public int Count { get { return segments.Length; } }
        public bool HasMotorOutput {get;private set;}
        public static float ClampGain(float gain)
        {return float.IsNaN(gain)||float.IsInfinity(gain)?0:Math.Max(0,Math.Min(2,gain));}
        public HapticPattern WithGain(float gain)
        {
            gain=ClampGain(gain);if(gain==1)return this;
            float peak=0;foreach(var segment in segments)peak=Math.Max(peak,Math.Max(segment.Left,segment.Right));
            if(peak>0)gain=Math.Min(gain,1/peak);
            var scaled=new HapticSegment[segments.Length];
            for(int i=0;i<segments.Length;i++)scaled[i]=new HapticSegment(
                Math.Min(1,segments[i].Left*gain),Math.Min(1,segments[i].Right*gain),segments[i].DurationMs);
            return new HapticPattern(MaxDurationMs,scaled);
        }
        public static HapticPattern Celebration(params HapticSegment[] segments) { return new HapticPattern(850, segments); }
        public static HapticPattern Extended(params HapticSegment[] segments) { return new HapticPattern(950, segments); }
        public int TotalDurationMs { get; private set; }
        public HapticSegment SegmentAt(int index) { return segments[index]; }
    }
}
