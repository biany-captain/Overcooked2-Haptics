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
            { "candidate client hazard signatures and cooking state data exist", HazardSignatures },
            { "actual music and SFX mixer controls have separate names", MusicControls },
            { "actual game audio sources use known shared parent and music group", MusicSourceBindings },
            { "personal attribution probe has actual chef item and delivery station fields", PersonalAttributionSignatures },
            { "actual local placement sends the typed chef and station request", PersonalPlacementSignature },
            { "failed kitchen observation never reads unsent stale Unity station or success fields", FailedKitchenObservation }
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
    private static void MusicControls()
    {
        var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        Check(typeof(AudioManager).GetField("m_audioMixer",flags)!=null);
        foreach(string name in new[]{"MusicVolume","SFXVolume"})
        {
            Type type=typeof(AudioManager).Assembly.GetType(name,true);
            var getter=type.GetProperty("MixerControlName",flags).GetGetMethod(true);
            byte[] body=getter.GetMethodBody().GetILAsByteArray();
            Check(body.Length==6 && body[0]==0x72 && body[5]==0x2a && getter.Module.ResolveString(BitConverter.ToInt32(body,1))==name);
        }
    }
    private static void MusicSourceBindings()
    {
        var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        var type=typeof(AudioManager);
        Check(type.GetField("m_sharedSource",flags).FieldType.FullName=="UnityEngine.GameObject");
        Check(type.GetField("m_musAudioGroup",flags).FieldType.FullName=="UnityEngine.Audio.AudioMixerGroup");
        var group=type.GetNestedType("AudioGroup",BindingFlags.Public|BindingFlags.NonPublic);
        var method=type.GetMethod("AddSource",flags,null,new[]{group,typeof(int)},null);
        byte[] body=method.GetMethodBody().GetILAsByteArray();
        Check(body.Length>=6 && body[1]==0x7b && method.Module.ResolveField(BitConverter.ToInt32(body,2)).Name=="m_sharedSource");
    }
    private static void PersonalAttributionSignatures()
    {
        var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        var serialisable=typeof(Team17.Online.Multiplayer.Messaging.Serialisable);
        Check(typeof(ClientPlayerAttachmentCarrier).GetMethod("ApplyServerEvent",flags,null,new[]{serialisable},null)!=null);
        Check(typeof(ClientPlayerAttachmentCarrier).GetMethod("InspectCarriedItem",flags,null,new[]{typeof(PlayerAttachTarget)},null).ReturnType==typeof(UnityEngine.GameObject));
        Check(typeof(ChefCarryMessage).GetField("m_carriableItem").FieldType==typeof(uint));
        Check(typeof(ChefCarryMessage).GetField("m_playerAttachTarget").FieldType==typeof(PlayerAttachTarget));
        Check(typeof(ClientPlateStation).GetMethod("ApplyServerEvent",flags,null,new[]{serialisable},null)!=null);
        Check(typeof(PlateStationMessage).GetField("m_delivered").FieldType==typeof(UnityEngine.GameObject));
        Check(typeof(PlateStationMessage).GetField("m_success").FieldType==typeof(bool));
        Check(typeof(KitchenFlowMessage).GetField("m_plateStation").FieldType==typeof(UnityEngine.GameObject));
    }
    private static void PersonalPlacementSignature()
    {
        var flags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        var type=typeof(Team17.Online.Multiplayer.Messaging.ChefEventMessage.ChefEventType);
        Check((int)Team17.Online.Multiplayer.Messaging.ChefEventMessage.ChefEventType.Place==1);
        Check((int)Team17.Online.Multiplayer.Messaging.ChefEventMessage.ChefEventType.Take==2);
        var messenger=typeof(GameUtils).Assembly.GetType("ClientMessenger",true);
        Check(messenger.GetMethod("ChefEventMessage",flags,null,new[]{type,typeof(UnityEngine.GameObject),typeof(UnityEngine.MonoBehaviour)},null)!=null);
        Check(typeof(PlayerControlsHelper).GetMethod("PlaceHeldItem_Client",flags,null,new[]{typeof(PlayerControls)},null)!=null);
        Check(typeof(ClientPlayerAttachmentCarrier).GetMethod("InspectCarriedItem",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,Type.EmptyTypes,null).ReturnType==typeof(UnityEngine.GameObject));
    }
    private static void FailedKitchenObservation()
    {
        // Failure wire payload omits these fields. A stale Unity reference must not be touched.
        var message=new KitchenFlowMessage();
        message.m_msgType=KitchenFlowMessage.MsgType.Delivery;message.m_teamID=TeamID.One;message.m_success=false;
        message.m_plateStation=(UnityEngine.GameObject)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(UnityEngine.GameObject));
        message.m_orderID.m_id=777;message.m_wasCombo=true;message.m_tip=63;
        var observation=new ScoreSourceProbe.Observation(1,1,1,0,message,null,null,null);
        Check(observation.StationId==0 && observation.OrderId==0 && !observation.WasCombo && observation.Tip==0);
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
