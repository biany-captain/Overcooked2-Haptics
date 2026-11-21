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
            bool correct=settings.DeliveryOrdered.Value==(i==1)&&!settings.DeliveryOutOfOrder.Value&&!settings.DeliveryUnclassified.Value;
            correct=correct&&!File.ReadAllText(path).Contains("DeliverySuccess =");
            if(!correct){failed++;Console.WriteLine("FAIL delivery preference migration "+i);}else Console.WriteLine("PASS delivery preference migration "+i);
        }
        string gainPath=Path.Combine(root,"gain-preserve.cfg");
        File.WriteAllText(gainPath,"[General]\nMasterIntensity = 0.4\n[StarFeedback]\nMode = AudioOnly\nSoundVolume = 0.7\n[EventStrength]\nStarMilestone1 = 1.5\n");
        var gains=new HapticConfig(new ConfigFile(gainPath,false));
        bool gainCorrect=Math.Abs(gains.Intensity-.4f)<.0001 && Math.Abs(gains.SoundVolume.Value-.7f)<.0001 &&
            Math.Abs(gains.EventGains[GameEventType.StarMilestone1].Value-1.5f)<.0001;
        gainCorrect=gainCorrect && gains.PatternFor(GameEventType.StarMilestone1,StarFeedbackMode.HapticsOnly).TotalDurationMs==145 &&
            Math.Abs(gains.PatternFor(GameEventType.StarMilestone1,StarFeedbackMode.HapticsOnly).SegmentAt(0).Right-.33f)<.0001 &&
            gains.FeedbackMode==StarFeedbackMode.AudioOnly && gains.EventGains[GameEventType.CatchSuccess].Value==1;
        if(!gainCorrect){failed++;Console.WriteLine("FAIL event gains and independent preview configuration");}
        else Console.WriteLine("PASS event gains and independent preview configuration");
        string invalidPath=Path.Combine(root,"gain-infinity.cfg");
        File.WriteAllText(invalidPath,"[EventStrength]\nThrowSuccess = Infinity\n");
        var clamped=new HapticConfig(new ConfigFile(invalidPath,false));
        bool clampCorrect=clamped.EventGains[GameEventType.ThrowSuccess].Value==2 &&
            clamped.PatternFor(GameEventType.ThrowSuccess).SegmentAt(0).Right<=1;
        if(!clampCorrect){failed++;Console.WriteLine("FAIL configured nonfinite gain range clamp");}
        else Console.WriteLine("PASS configured nonfinite gain range clamp");
        Console.WriteLine("RESULT " + (content.Length+4 - failed) + "/" + (content.Length+4) + " config migration passed");
        return failed == 0 ? 0 : 1;
    }
}
