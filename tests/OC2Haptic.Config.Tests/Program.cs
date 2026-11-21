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
        Console.WriteLine("RESULT " + (content.Length+2 - failed) + "/" + (content.Length+2) + " config migration passed");
        return failed == 0 ? 0 : 1;
    }
}
