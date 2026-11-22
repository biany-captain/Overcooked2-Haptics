using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OC2Haptic.Core;

internal static class V16Tests
{
    internal static readonly Dictionary<string,GameEventType> Names=new Dictionary<string,GameEventType>
    {
        {"delivery_success",GameEventType.DeliveryOrdered},{"delivery_out_of_order",GameEventType.DeliveryOutOfOrder},
        {"delivery_rejected",GameEventType.DeliveryFailure},{"combo_x2_upgrade",GameEventType.ComboLevel2},
        {"combo_x3_upgrade",GameEventType.ComboLevel3},{"combo_x4_upgrade",GameEventType.ComboLevel4},
        {"order_expired",GameEventType.OrderExpired},{"burn_warning",GameEventType.BurnWarning},
        {"food_burned",GameEventType.FoodBurned},{"fire_start",GameEventType.FireStart},
        {"fire_reminder",GameEventType.FireReminder},{"throw",GameEventType.ThrowSuccess},{"catch",GameEventType.CatchSuccess}
    };
    internal static void Add(Dictionary<string,Action> tests)
    {
        tests.Add("v16 JSON is the exact candidate profile contract", Profiles);
        tests.Add("v16 extended negative limit does not relax normal or star limits", Limits);
        tests.Add("v16 Combo A only confirmed upgrade and prior break", ComboA);
    }
    private static void Profiles()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null && !File.Exists(Path.Combine(root.FullName,"config","haptic_patterns_v1.6.json")))root=root.Parent;
        Check(root!=null);
        using(var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,"config","haptic_patterns_v1.6.json"))))
        {
            var events=json.RootElement.GetProperty("events");int checkedEvents=0;
            var settings=json.RootElement.GetProperty("settings");
            Check(V16Preset.NormalBudgetMs==settings.GetProperty("normal_active_budget_ms_per_10s").GetInt32() &&
                V16Preset.NegativeBudgetMs==settings.GetProperty("negative_active_budget_ms_per_10s").GetInt32() &&
                V16Preset.EmergencyBudgetMs==settings.GetProperty("emergency_active_budget_ms_per_10s").GetInt32() &&
                V16Preset.StaleNormalMs==settings.GetProperty("drop_stale_normal_event_after_ms").GetInt32() &&
                V16Preset.FireReminderIntervalMs==settings.GetProperty("max_same_fire_reminder_interval_ms").GetInt32());
            foreach(var e in events.EnumerateObject())
            {
                if(e.Name=="star_milestone")continue;
                Check(Names.ContainsKey(e.Name));var expected=e.Value;var actual=EventFeedback.For(Names[e.Name]);
                Check(actual.Priority==expected.GetProperty("priority").GetInt32() && actual.CooldownMs==expected.GetProperty("cooldown_ms").GetInt32());
                Check(actual.DefaultEnabled==expected.GetProperty("enabled").GetBoolean() &&
                    actual.Category.ToString().ToLowerInvariant()==expected.GetProperty("category").GetString());
                var steps=expected.GetProperty("steps");Check(actual.Pattern.Count==steps.GetArrayLength());int index=0,total=0;
                foreach(var step in steps.EnumerateArray())
                {
                    var s=actual.Pattern.SegmentAt(index++);int ms=step.GetProperty("ms").GetInt32();
                    Check(s.DurationMs==ms && Math.Abs(s.Left-step.GetProperty("low").GetSingle())<.00001 &&
                        Math.Abs(s.Right-step.GetProperty("high").GetSingle())<.00001);total+=ms;
                }
                Check(actual.Pattern.TotalDurationMs==total);checkedEvents++;
            }
            Check(checkedEvents==Names.Count);
            foreach(var star in new[]{GameEventType.StarMilestone1,GameEventType.StarMilestone2,GameEventType.StarMilestone3,GameEventType.StarMilestone4})
                Check(EventFeedback.For(star).Priority==86);
            Check(PatternCodec.Format(EventFeedback.For(GameEventType.DeliveryUnclassified).Pattern)==
                PatternCodec.Format(EventFeedback.For(GameEventType.DeliveryOrdered).Pattern));
        }
    }
    private static void Limits()
    {
        Check(HapticPattern.Extended(new HapticSegment(.5f,0,500),new HapticSegment(.2f,0,450)).TotalDurationMs==950);
        bool rejected=false;try{HapticPattern.Extended(new HapticSegment(.5f,0,500),new HapticSegment(.2f,0,451));}catch(ArgumentException){rejected=true;}Check(rejected);
        HapticPattern p;Check(PatternCodec.TryParse("0.5,0,500;0.2,0,450",950,out p));
        Check(!PatternCodec.TryParse("0.5,0,500;0.2,0,451",950,out p));
        Check(!PatternCodec.TryParse("0.5,0,500;0.2,0,1",false,out p));
        Check(!PatternCodec.TryParse("0.5,0,500;0.2,0,351",true,out p));
    }
    private static DeliveryPayload P(int ordinal,bool combo,int tier,int streak,bool trusted=true)
    {return new DeliveryPayload("r",0,ordinal,ordinal,combo,tier,streak,trusted,ordinal*1000);}
    private static void ComboA()
    {
        var c=new ComboTracker();c.ObserveState("r",0,0,true);
        Check(c.Resolve(P(1,false,0,0),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryUnclassified);
        c.ObserveState("r",0,0,true);Check(c.Resolve(P(2,true,1,1),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryOrdered);
        Check(c.Resolve(P(3,true,2,2),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.ComboLevel2);
        Check(c.Resolve(P(4,true,2,3),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryOrdered);
        Check(c.Resolve(P(5,true,4,4),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.ComboLevel4);
        Check(c.Resolve(P(6,true,4,5),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryOrdered);
        Check(c.Resolve(P(7,false,0,0),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryOutOfOrder);
        c.ObserveState("r",0,0,true);Check(c.Resolve(P(8,false,0,0),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryUnclassified);
        c.ObserveState("r",0,0,false);Check(c.Resolve(P(9,true,3,3),true,ComboTriggerMode.OnLevelUp,4).Type==GameEventType.DeliveryOrdered);
    }
    internal static void Check(bool condition){if(!condition)throw new Exception("v1.6 behavior mismatch");}
}
