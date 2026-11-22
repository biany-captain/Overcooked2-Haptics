using System;
using System.Collections.Generic;
using System.Reflection;
using OC2Haptic.Core;
using OC2Haptic.GameEvents;

internal static class Program
{
    private static int Main()
    {
        var tests = new Dictionary<string, Action>
        {
            { "missing progress/directory produces unavailable level without game lookup", MissingDirectory },
            { "selected variant resolves actual level by identity", SelectedVariant },
            { "missing PC star boundaries remain unavailable", MissingBoundaries },
            { "actual PC score getters retain NGP gate", ActualBoundaries },
            { "unchanged client state cannot hide a differing payload", MismatchMustLog },
            { "candidate client hazard signatures and cooking state data exist", HazardSignatures }
        };
        int failed = 0;
        foreach (var test in tests)
        {
            try { test.Value(); Console.WriteLine("PASS " + test.Key); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + test.Key + ": " + error.GetBaseException().Message); }
        }
        Console.WriteLine("RESULT " + (tests.Count - failed) + "/" + tests.Count + " probe integration passed");
        return failed == 0 ? 0 : 1;
    }
    private static void MissingDirectory()
    {
        Check(ScoreProbeMetadata.FindLevelId(null, new SceneDirectoryData.PerPlayerCountDirectoryEntry()) == -1);
        Check(ScoreProbeMetadata.FindLevelId(new SceneDirectoryData.SceneDirectoryEntry[0], null) == -1);
    }
    private static void SelectedVariant()
    {
        var selected = new SceneDirectoryData.PerPlayerCountDirectoryEntry();
        var scenes = new[] { null, new SceneDirectoryData.SceneDirectoryEntry { SceneVarients = null },
            new SceneDirectoryData.SceneDirectoryEntry { SceneVarients = new[] { selected } } };
        Check(ScoreProbeMetadata.FindLevelId(scenes, selected) == 2);
        Check(ScoreProbeMetadata.FindLevelId(scenes, new SceneDirectoryData.PerPlayerCountDirectoryEntry()) == -1);
    }
    private static void MissingBoundaries()
    {
        var variant = new SceneDirectoryData.PerPlayerCountDirectoryEntry();
        PcField().SetValue(variant, null);
        ScoreThresholds thresholds; string raw;
        Check(!ScoreProbeMetadata.TryReadThresholds(variant, true, out thresholds, out raw));
        Check(thresholds == null && raw == "Unavailable");
    }
    private static void ActualBoundaries()
    {
        var variant = new SceneDirectoryData.PerPlayerCountDirectoryEntry();
        PcField().SetValue(variant, new SceneDirectoryData.StarBoundaries
            { m_OneStarScore = 100, m_TwoStarScore = 200, m_ThreeStarScore = 300, m_FourStarScore = 500 });
        ScoreThresholds thresholds; string raw;
        Check(ScoreProbeMetadata.TryReadThresholds(variant, false, out thresholds, out raw) && thresholds.Count == 3);
        Check(ScoreProbeMetadata.TryReadThresholds(variant, true, out thresholds, out raw) && thresholds.At(3) == 500);
    }
    private static void MismatchMustLog()
    {
        Check(ScoreSourceProbe.ShouldLog(true, true, false));
        Check(!ScoreSourceProbe.ShouldLog(true, true, true));
        Check(ScoreSourceProbe.ShouldLog(false, true, true));
    }
    private static void HazardSignatures()
    {
        var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        Check(typeof(ClientFlammable).GetMethod("Ignite",flags,null,Type.EmptyTypes,null)!=null);
        Check(typeof(ClientFlammable).GetMethod("Extinguish",flags,null,new[]{typeof(bool),typeof(bool)},null)!=null);
        Check(typeof(CookingStateMessage).GetField("m_cookingState").FieldType==typeof(CookingUIController.State));
        Check((int)CookingUIController.State.OverDoing==3 && (int)CookingUIController.State.Ruined==4);
    }
    private static FieldInfo PcField()
    {
        return typeof(SceneDirectoryData.PerPlayerCountDirectoryEntry).GetField("m_PCStarBoundaries",
            BindingFlags.Instance | BindingFlags.NonPublic);
    }
    private static void Check(bool condition) { if (!condition) throw new Exception("unexpected probe boundary result"); }
}
