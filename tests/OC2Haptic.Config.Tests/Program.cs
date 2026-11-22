using System;
using System.IO;
using BepInEx.Configuration;
using OC2Haptic.Config;
using OC2Haptic.Core;
using UnityEngine;

internal static class Program
{
    private static int Main(string[] args)
    {
        string root = args[0]; Directory.CreateDirectory(root);
        string[] content = { "", "[Hotkeys]\nTestRumble = None\n", "[Hotkeys]\nTestRumble = F7\n",
            "[Hotkeys]\nTestRumble = None\nOpenMenu = F8\n" };
        KeyCode[] expected = { KeyCode.F6, KeyCode.None, KeyCode.F7, KeyCode.F8 };
        int failed = 0;
        for (int index = 0; index < content.Length; index++)
        {
            string path = Path.Combine(root, "migration-" + index + ".cfg"); File.WriteAllText(path, content[index]);
            var settings = new HapticConfig(new ConfigFile(path, false));
            if (settings.MenuKey.Value != expected[index])
            { failed++; Console.WriteLine("FAIL migration " + index + " expected=" + expected[index] + " actual=" + settings.MenuKey.Value); }
            else Console.WriteLine("PASS migration " + index + " -> " + expected[index]);
        }
        string[] deliveryConfigs={"[Events]\nDeliverySuccess = false\n", "[Events]\nDeliverySuccess = false\nDeliveryOrdered = true\n"};
        for(int i=0;i<deliveryConfigs.Length;i++)
        {
            string path=Path.Combine(root,"delivery-"+i+".cfg");File.WriteAllText(path,deliveryConfigs[i]);
            var settings=new HapticConfig(new ConfigFile(path,false));
            bool correct=settings.IsEventEnabled(GameEventType.DeliveryOrdered)==(i==1) && !settings.IsEventEnabled(GameEventType.DeliveryOutOfOrder) &&
                !settings.IsEventEnabled(GameEventType.DeliveryUnclassified) && !HasSetting(path,"Events","DeliverySuccess");
            if(!correct){failed++;Console.WriteLine("FAIL legacy coarse preference migrates to cfg event switches "+i);}else Console.WriteLine("PASS legacy coarse preference migrates to cfg event switches "+i);
        }
        string gainPath=Path.Combine(root,"gain-preserve.cfg");
        File.WriteAllText(gainPath,"[General]\nMasterIntensity = 0.4\n[StarFeedback]\nMode = AudioOnly\nSoundVolume = 0.7\n[EventStrength]\nStarMilestone1 = 1.5\n");
        var gains=new HapticConfig(new ConfigFile(gainPath,false));
        bool gainCorrect=Math.Abs(gains.Intensity-.4f)<.0001 && Math.Abs(gains.SoundVolume.Value-.7f)<.0001 &&
            !File.ReadAllText(gainPath).Contains("[EventStrength]");
        gainCorrect=gainCorrect && gains.PatternFor(GameEventType.StarMilestone1,StarFeedbackMode.HapticsOnly).TotalDurationMs==180 &&
            Math.Abs(gains.PatternFor(GameEventType.StarMilestone1,StarFeedbackMode.HapticsOnly).SegmentAt(0).Right-.22f)<.0001 &&
            gains.FeedbackMode==StarFeedbackMode.AudioOnly && !gains.HapticsEnabled.Value && gains.AudioEnabled.Value;
        if(!gainCorrect){failed++;Console.WriteLine("FAIL retired gains reset while master volume and mode migrate");}
        else Console.WriteLine("PASS retired gains reset while master volume and mode migrate");
        string invalidPath=Path.Combine(root,"gain-infinity.cfg");
        File.WriteAllText(invalidPath,"[EventStrength]\nThrowSuccess = Infinity\n");
        var clamped=new HapticConfig(new ConfigFile(invalidPath,false));
        bool clampCorrect=!File.ReadAllText(invalidPath).Contains("[EventStrength]") &&
            clamped.PatternFor(GameEventType.ThrowSuccess).SegmentAt(0).Right==.33f;
        if(!clampCorrect){failed++;Console.WriteLine("FAIL retired nonfinite gain cannot affect output");}
        else Console.WriteLine("PASS retired nonfinite gain cannot affect output");
        string musicPath=Path.Combine(root,"v14-preserve.cfg");
        File.WriteAllText(musicPath,"[General]\nMasterIntensity = 0.4\n[StarFeedback]\nSoundPackId = future-pack\nSoundVolume = 0.7006757\nMaxSoundDurationMs = 700\n[Patterns]\nStarMilestone1 = 0.05,0.22,75\nStarMilestone2 = 0.9,0.1,123\nStarMilestone1HapticsOnly = 0.05,0.22,75;0,0,45;0.03,0.13,25\n");
        var music=new HapticConfig(new ConfigFile(musicPath,false));
        bool musicCorrect=music.SoundPackId.Value=="future-pack" && Math.Abs(music.SoundVolume.Value-.7006757f)<.000001 && music.Intensity==.4f && !File.ReadAllText(musicPath).Contains("MaxSoundDurationMs =") &&
            music.PatternFor(GameEventType.StarMilestone1).TotalDurationMs==180 && music.PatternFor(GameEventType.StarMilestone2).TotalDurationMs==123 &&
            music.PatternFor(GameEventType.StarMilestone1,StarFeedbackMode.HapticsOnly).TotalDurationMs==180 && gains.SoundPackId.Value==RewardSoundPack.DefaultId;
        if(!musicCorrect){failed++;Console.WriteLine("FAIL v14 defaults migrate while custom profiles and pack preference survive");}
        else Console.WriteLine("PASS v14 defaults migrate while custom profiles and pack preference survive");
        string precisionPath=Path.Combine(root,"v14-custom-precision.cfg");
        const string precise="0.0504,0.2204,75",preciseTail="0.0504,0.2204,75;0,0,45;0.0304,0.1304,25";
        File.WriteAllText(precisionPath,"[Patterns]\nStarMilestone1 = "+precise+"\nStarMilestone1HapticsOnly = "+preciseTail+"\n");
        var precision=new HapticConfig(new ConfigFile(precisionPath,false));
        bool precisionCorrect=precision.Patterns[GameEventType.StarMilestone1].Value==precise && precision.OneStarTail.Value==preciseTail &&
            precision.PatternFor(GameEventType.StarMilestone1).TotalDurationMs==75 &&
            precision.PatternFor(GameEventType.StarMilestone1,StarFeedbackMode.HapticsOnly).TotalDurationMs==145;
        if(!precisionCorrect){failed++;Console.WriteLine("FAIL v14 migration preserves submilliscale custom motor values");}
        else Console.WriteLine("PASS v14 migration preserves submilliscale custom motor values");
        bool retiredCorrect=true;
        foreach(string id in new[]{"joyful-cartoon-jazz","crisp-reward","JOYFUL-CARTOON-JAZZ"})
        {
            string path=Path.Combine(root,"retired-"+id+".cfg");File.WriteAllText(path,"[StarFeedback]\nSoundPackId = "+id+"\nSoundVolume = 0.42\n");
            var retired=new HapticConfig(new ConfigFile(path,false));
            retiredCorrect=retiredCorrect && retired.SoundPackId.Value=="turbo_swing" && retired.SoundVolume.Value==.42f;
        }
        string defaultPath=Path.Combine(root,"new-audio-default.cfg");File.WriteAllText(defaultPath,"");
        var defaults=new HapticConfig(new ConfigFile(defaultPath,false));
        retiredCorrect=retiredCorrect && defaults.SoundPackId.Value=="turbo_swing" && defaults.SoundVolume.Value==1;
        if(!retiredCorrect){failed++;Console.WriteLine("FAIL retired selections migrate and relative gameSFX gain defaults100percent");}
        else Console.WriteLine("PASS retired selections migrate and relative gameSFX gain defaults100percent");
        bool channelsCorrect=defaults.HapticsEnabled.Value && defaults.AudioEnabled.Value;
        foreach(string mode in new[]{"HapticsAndAudio","HapticsOnly","AudioOnly","Disabled","BadMode"})
        {
            string path=Path.Combine(root,"channels-"+mode+".cfg");
            File.WriteAllText(path,"[General]\nEnabled = true\n[StarFeedback]\nMode = "+mode+"\n");
            var migrated=new HapticConfig(new ConfigFile(path,false));
            // Invalid enum text is rejected by BepInEx and falls back to the legacy default.
            bool h=mode=="HapticsAndAudio" || mode=="HapticsOnly" || mode=="BadMode";
            bool a=mode=="HapticsAndAudio" || mode=="AudioOnly" || mode=="BadMode";
            channelsCorrect=channelsCorrect && migrated.HapticsEnabled.Value==h && migrated.AudioEnabled.Value==a &&
                !HasSetting(path,"StarFeedback","Mode") && !HasSetting(path,"General","Enabled");
        }
        string offPath=Path.Combine(root,"channels-off.cfg");
        File.WriteAllText(offPath,"[General]\nEnabled = false\n[StarFeedback]\nMode = HapticsAndAudio\n");
        var off=new HapticConfig(new ConfigFile(offPath,false));channelsCorrect=channelsCorrect && !off.AnyOutputEnabled;
        if(!channelsCorrect){failed++;Console.WriteLine("FAIL legacy modes migrate to independent master channels");}
        else Console.WriteLine("PASS legacy modes migrate to independent master channels");
        string explicitPath=Path.Combine(root,"channels-explicit.cfg");
        File.WriteAllText(explicitPath,"[General]\nEnabled = false\nHapticsEnabled = false\nAudioEnabled = true\nMasterIntensity = 0.42\n[Combo]\nEnabled = false\n[Stars]\nEnabled = false\n[StarFeedback]\nMode = Disabled\nSoundVolume = 0.37\nSoundPackId = kitchen_rock\n");
        var explicitChannels=new HapticConfig(new ConfigFile(explicitPath,false));
        bool explicitCorrect=!explicitChannels.HapticsEnabled.Value && explicitChannels.AudioEnabled.Value && explicitChannels.FeedbackMode==StarFeedbackMode.AudioOnly;
        explicitChannels.ToggleAllOutput();explicitCorrect=explicitCorrect && !explicitChannels.AnyOutputEnabled;
        explicitChannels.ToggleAllOutput();explicitCorrect=explicitCorrect && explicitChannels.HapticsEnabled.Value && explicitChannels.AudioEnabled.Value &&
            explicitChannels.Intensity==.42f && explicitChannels.SoundVolume.Value==.37f && explicitChannels.SoundPackId.Value=="kitchen_rock";
        var reloaded=new HapticConfig(new ConfigFile(explicitPath,false));
        explicitCorrect=explicitCorrect && reloaded.HapticsEnabled.Value && reloaded.AudioEnabled.Value &&
            !HasSetting(explicitPath,"General","Enabled");
        if(!explicitCorrect){failed++;Console.WriteLine("FAIL explicit channel precedence F10 and persistence");}
        else Console.WriteLine("PASS explicit channel precedence F10 and persistence");
        const string oldThree="0,0.3,55;0,0,50;0,0.35,60;0,0,50;0.22,0.43,80";
        const string oldFour="0,0.31,55;0,0,40;0,0.35,55;0,0,40;0,0.39,55;0,0,40;0.25,0.48,95";
        string v16Path=Path.Combine(root,"v16-upgrade.cfg");
        File.WriteAllText(v16Path,"[General]\nMasterIntensity = 0.42\n[StarFeedback]\nSoundVolume = 0.37\nSoundPackId = future-pack\n[Combo]\nTriggerMode = EveryOrderedDelivery\n[Patterns]\nComboLevel3 = "+oldThree+"\nComboLevel4 = "+oldFour+"\nOrderExpired = 0.4,0.3,250\n");
        var v16=new HapticConfig(new ConfigFile(v16Path,false));
        bool v16Correct=v16.ComboMode==ComboTriggerMode.OnLevelUp && !v16.IsEventEnabled(GameEventType.ThrowSuccess) &&
            !v16.IsEventEnabled(GameEventType.CatchSuccess) && !v16.IsEventEnabled(GameEventType.FoodBurned) &&
            v16.PatternFor(GameEventType.DeliveryOrdered).TotalDurationMs==90 && v16.PatternFor(GameEventType.DeliveryFailure).TotalDurationMs==430 &&
            v16.PatternFor(GameEventType.OrderExpired).TotalDurationMs==505 && v16.PatternFor(GameEventType.ComboLevel3).TotalDurationMs==375 &&
            v16.PatternFor(GameEventType.ComboLevel4).TotalDurationMs==490 && v16.PatternFor(GameEventType.FireStart).TotalDurationMs==820 &&
            v16.Intensity==.42f && v16.SoundVolume.Value==.37f && v16.SoundPackId.Value=="future-pack" && v16.PatternFor(GameEventType.TestPulse).TotalDurationMs==80;
        if(!v16Correct){failed++;Console.WriteLine("FAIL v16 defaults known profiles and Combo A migrate without resetting global preferences");}
        else Console.WriteLine("PASS v16 defaults known profiles and Combo A migrate without resetting global preferences");
        string customPath=Path.Combine(root,"v16-custom.cfg");
        const string customThree="0.0004,0.3,55;0,0,50;0,0.35,60;0,0,50;0.22,0.43,80";
        File.WriteAllText(customPath,"[Patterns]\nComboLevel3 = "+customThree+"\nComboLevel4 = "+oldFour+"\nDeliveryFailure = invalid\nOrderExpired = 0.5,0,505\n");
        var custom=new HapticConfig(new ConfigFile(customPath,false));
        bool customCorrect=custom.Patterns[GameEventType.ComboLevel3].Value==customThree && custom.PatternFor(GameEventType.ComboLevel3).SegmentAt(0).Left==.0004f &&
            custom.PatternFor(GameEventType.ComboLevel4).SegmentAt(6).Right==.79f && custom.Patterns[GameEventType.DeliveryFailure].Value=="invalid" &&
            custom.PatternFor(GameEventType.DeliveryFailure)==null && custom.PatternFor(GameEventType.OrderExpired)==null;
        // Single segments remain capped at500ms even for extended negative patterns.
        if(!customCorrect){failed++;Console.WriteLine("FAIL v16 exact custom and invalid patterns preserved independently");}
        else Console.WriteLine("PASS v16 exact custom and invalid patterns preserved independently");
        Console.WriteLine("RESULT " + (content.Length+11 - failed) + "/" + (content.Length+11) + " config migration passed");
        return failed == 0 ? 0 : 1;
    }
    private static bool HasSetting(string path,string section,string key)
    {
        string current="";foreach(string line in File.ReadAllLines(path))
        {string text=line.Trim();if(text.StartsWith("[") && text.EndsWith("]"))current=text.Substring(1,text.Length-2);
            else if(current==section && text.StartsWith(key+" ="))return true;}
        return false;
    }
}
