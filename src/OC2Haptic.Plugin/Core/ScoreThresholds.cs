namespace OC2Haptic.Core
{
    public sealed class ScoreThresholds
    {
        private readonly int[] values;
        private ScoreThresholds(int[] values) { this.values = (int[])values.Clone(); }
        public int Count { get { return values.Length; } }
        public int At(int index) { return values[index]; }

        public static bool TryCreate(int[] values, out ScoreThresholds result)
        {
            result = null;
            if (values == null || values.Length == 0 || values.Length > 4) return false;
            int previous = 0;
            foreach (int value in values)
            {
                if (value <= previous) return false;
                previous = value;
            }
            result = new ScoreThresholds(values);
            return true;
        }

        public static bool TryForLevel(int one, int two, int three, int four, bool fourthAvailable,
            out ScoreThresholds result)
        {
            return TryCreate(fourthAvailable ? new[] { one, two, three, four } : new[] { one, two, three }, out result);
        }
    }
}
