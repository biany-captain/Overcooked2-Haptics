using System.Reflection;
using OC2Haptic.Core;

namespace OC2Haptic.GameEvents
{
    internal static class ScoreProbeMetadata
    {
        // Current Windows build's accessor selects this private PC field. Inspect once, never scan per frame.
        private static readonly FieldInfo PcBoundaries = typeof(SceneDirectoryData.PerPlayerCountDirectoryEntry)
            .GetField("m_PCStarBoundaries", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static int FindLevelId(SceneDirectoryData.SceneDirectoryEntry[] scenes,
            SceneDirectoryData.PerPlayerCountDirectoryEntry selected)
        {
            if (scenes == null || selected == null) return -1;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i] == null || scenes[i].SceneVarients == null) continue;
                foreach (var variant in scenes[i].SceneVarients)
                    if (object.ReferenceEquals(variant, selected)) return i;
            }
            return -1;
        }

        internal static bool TryReadThresholds(SceneDirectoryData.PerPlayerCountDirectoryEntry variant, bool fourthAvailable,
            out ScoreThresholds thresholds, out string raw)
        {
            thresholds = null; raw = "Unavailable";
            if (variant == null || PcBoundaries == null || PcBoundaries.GetValue(variant) == null) return false;
            int one = variant.OneStarScore, two = variant.TwoStarScore, three = variant.ThreeStarScore, four = variant.FourStarScore;
            raw = one + "," + two + "," + three + "," + four;
            return ScoreThresholds.TryForLevel(one, two, three, four, fourthAvailable, out thresholds);
        }
    }
}
