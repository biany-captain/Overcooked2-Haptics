using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using OC2Haptic.Core;
using UnityEngine;

namespace OC2Haptic.Config
{
    internal sealed class HapticConfig
    {
        internal readonly ConfigEntry<bool> Enabled, ThrowSuccess, CatchSuccess, DeliveryOrdered, DeliveryOutOfOrder,
            DeliveryUnclassified, DeliveryFailure, OrderExpired, ComboEnabled, StarsEnabled, RespectGameFocus;
        internal readonly ConfigEntry<bool> DebugLogEvents, ScoreSourceProbe;
        internal readonly ConfigEntry<float> MasterIntensity, ComboIntensity, SoundVolume;
        internal readonly ConfigEntry<int> XInputIndex, LocalPadSlot, MaxHapticTier, MaxSoundDurationMs;
        internal readonly ConfigEntry<KeyCode> MenuKey, ToggleKey;
        internal readonly ConfigEntry<ComboTriggerMode> ComboMode;
        internal readonly ConfigEntry<StarFeedbackMode> StarMode;
        internal readonly Dictionary<GameEventType, ConfigEntry<string>> Patterns = new Dictionary<GameEventType, ConfigEntry<string>>();
        internal readonly ConfigEntry<string> OneStarTail;
        private readonly Dictionary<GameEventType, HapticPattern> parsed = new Dictionary<GameEventType, HapticPattern>();
        private readonly List<Action<EventHandler, bool>> watchers = new List<Action<EventHandler, bool>>();
        private HapticPattern tailPattern;
        internal string InvalidPatterns { get; private set; }

        internal HapticConfig(ConfigFile config)
        {
            Enabled = Output(config.Bind("General", "Enabled", true, "Global switch for this plugin's haptics and reward audio."));
            MasterIntensity = Output(config.Bind("General", "MasterIntensity", .75f,
                new ConfigDescription("Haptic motor multiplier, independent of audio gain.", new AcceptableValueRange<float>(0, 1))));
            DebugLogEvents = config.Bind("General", "DebugLogEvents", false, "Log real event routing, classification and reward decisions.");
            ScoreSourceProbe = config.Bind("Diagnostics", "ScoreSourceProbe", false, "Detailed read-only before/payload/after diagnostics; capture required by rewards remains active independently.");
            XInputIndex = Output(config.Bind("Devices", "XInputIndex", -1,
                new ConfigDescription("Explicit Windows output slot, -1 unbound.", new AcceptableValueRange<int>(-1, 3))));
            LocalPadSlot = Output(config.Bind("Devices", "LocalGamePadSlot", -1,
                new ConfigDescription("Actual game pad number, independent of XInput. -1 unbound.", new AcceptableValueRange<int>(-1, 14))));
            ThrowSuccess = Output(config.Bind("Events", "ThrowSuccess", true, "Confirmed local throw feedback."));
            CatchSuccess = Output(config.Bind("Events", "CatchSuccess", true, "Candidate two-part catch feedback; current local/host Hook."));
            var legacySuccess = config.Bind("Events", "DeliverySuccess", true, "Legacy success switch, migrated to the three categories.");
            DeliveryOrdered = Output(config.Bind("Events", "DeliveryOrdered", legacySuccess.Value, "Ordered delivery and its permitted Combo feedback."));
            DeliveryOutOfOrder = Output(config.Bind("Events", "DeliveryOutOfOrder", legacySuccess.Value, "Successful out-of-order delivery, distinct from failure."));
            DeliveryUnclassified = Output(config.Bind("Events", "DeliveryUnclassified", legacySuccess.Value, "Conservative light success feedback when classification is unavailable."));
            config.Remove(legacySuccess.Definition);
            DeliveryFailure = Output(config.Bind("Events", "DeliveryFailure", true, "Rejected delivery, low-motor feedback."));
            OrderExpired = Output(config.Bind("Events", "OrderExpired", true, "Actual expiry feedback, no advance warning."));
            ComboEnabled = Output(config.Bind("Combo", "Enabled", true, "Enable trusted tier feedback; disabling retains ordered/out-of-order classification."));
            ComboMode = Output(config.Bind("Combo", "TriggerMode", ComboTriggerMode.EveryOrderedDelivery, "EveryOrderedDelivery or OnLevelUp; authoritative game state only."));
            MaxHapticTier = Output(config.Bind("Combo", "MaxHapticTier", 4,
                new ConfigDescription("Haptic ceiling only; raw streak is never truncated.", new AcceptableValueRange<int>(1, 4))));
            ComboIntensity = Output(config.Bind("Combo", "ComboMasterIntensity", .85f,
                new ConfigDescription("Additional Combo motor multiplier.", new AcceptableValueRange<float>(0, 1))));
            StarsEnabled = Output(config.Bind("Stars", "Enabled", true, "First committed-score threshold crossing; no retroactive or final-star guarantee."));
            StarMode = Output(config.Bind("StarFeedback", "Mode", StarFeedbackMode.HapticsAndAudio, "HapticsAndAudio, HapticsOnly, AudioOnly or Disabled."));
            SoundVolume = Output(config.Bind("StarFeedback", "SoundVolume", .65f,
                new ConfigDescription("MOD linear gain only, applied once; game SFX mixer and mute remain respected.", new AcceptableValueRange<float>(0, 1))));
            MaxSoundDurationMs = Output(config.Bind("StarFeedback", "MaxSoundDurationMs", 1000,
                new ConfigDescription("Original generated reward sounds only.", new AcceptableValueRange<int>(100, 1000))));
            RespectGameFocus = Output(config.Bind("Audio", "RespectGameFocus", true, "Stop this plugin's feedback on focus loss. Recommended true."));
            foreach (GameEventType type in Enum.GetValues(typeof(GameEventType)))
            {
                if (type == GameEventType.DeliverySuccess) continue;
                Patterns[type] = Output(config.Bind("Patterns", type.ToString(), PatternCodec.Format(EventFeedback.For(type).Pattern),
                    "Editable candidate: left,right,milliseconds;... Silent segment: 0,0,gap. Invalid patterns mute this event's haptics."));
            }
            OneStarTail = Output(config.Bind("Patterns", "StarMilestone1HapticsOnly", PatternCodec.Format(EventFeedback.OneStarHapticsOnly),
                "Approved candidate one-star tail for haptics-only mode."));
            var legacyTestKey = config.Bind("Hotkeys", "TestRumble", KeyCode.F6, "Legacy key, migrated to OpenMenu.");
            MenuKey = config.Bind("Hotkeys", "OpenMenu", legacyTestKey.Value, "F6 management menu. None disables the key.");
            config.Remove(legacyTestKey.Definition);
            ToggleKey = config.Bind("Hotkeys", "ToggleMod", KeyCode.F10, "Global feedback switch, same setting as the F6 page.");
            RefreshPatterns();
            config.Save();
        }

        private ConfigEntry<T> Output<T>(ConfigEntry<T> entry)
        {
            watchers.Add(delegate(EventHandler callback, bool attach)
            {
                if (attach) entry.SettingChanged += callback;
                else entry.SettingChanged -= callback;
            });
            return entry;
        }
        internal void WatchOutputChanges(EventHandler callback) { foreach (var watch in watchers) watch(callback, true); }
        internal void UnwatchOutputChanges(EventHandler callback) { foreach (var watch in watchers) watch(callback, false); }
        internal float Intensity { get { return RewardOutput.SafeVolume(MasterIntensity.Value); } }
        internal StarFeedbackMode FeedbackMode { get { return Enum.IsDefined(typeof(StarFeedbackMode), StarMode.Value) ? StarMode.Value : StarFeedbackMode.Disabled; } }
        internal void RefreshPatterns()
        {
            InvalidPatterns = "";
            foreach (var pair in Patterns)
            {
                HapticPattern pattern;
                if (!PatternCodec.TryParse(pair.Value.Value, GameEventTypes.IsStar(pair.Key), out pattern)) InvalidPatterns += pair.Key + " ";
                parsed[pair.Key] = pattern;
            }
            if (!PatternCodec.TryParse(OneStarTail.Value, true, out tailPattern)) InvalidPatterns += "StarMilestone1HapticsOnly";
        }
        internal HapticPattern PatternFor(GameEventType type)
        {
            if (type == GameEventType.StarMilestone1 && FeedbackMode == StarFeedbackMode.HapticsOnly) return tailPattern;
            HapticPattern pattern;
            return parsed.TryGetValue(type, out pattern) ? pattern : null;
        }
        internal bool IsEventEnabled(GameEventType type)
        {
            switch (type)
            {
                case GameEventType.ThrowSuccess: return ThrowSuccess.Value;
                case GameEventType.CatchSuccess: return CatchSuccess.Value;
                case GameEventType.DeliverySuccess: return DeliveryOrdered.Value || DeliveryOutOfOrder.Value || DeliveryUnclassified.Value;
                case GameEventType.DeliveryOrdered: return DeliveryOrdered.Value;
                case GameEventType.DeliveryOutOfOrder: return DeliveryOutOfOrder.Value;
                case GameEventType.DeliveryUnclassified: return DeliveryUnclassified.Value;
                case GameEventType.DeliveryFailure: return DeliveryFailure.Value;
                case GameEventType.OrderExpired: return OrderExpired.Value;
                default: return GameEventTypes.IsStar(type) ? StarsEnabled.Value : GameEventTypes.IsCombo(type) && ComboEnabled.Value && DeliveryOrdered.Value;
            }
        }
        internal void SetEventEnabled(GameEventType type, bool enabled)
        {
            switch (type)
            {
                case GameEventType.ThrowSuccess: ThrowSuccess.Value = enabled; break;
                case GameEventType.CatchSuccess: CatchSuccess.Value = enabled; break;
                case GameEventType.DeliveryOrdered: DeliveryOrdered.Value = enabled; break;
                case GameEventType.DeliveryOutOfOrder: DeliveryOutOfOrder.Value = enabled; break;
                case GameEventType.DeliveryUnclassified: DeliveryUnclassified.Value = enabled; break;
                case GameEventType.DeliveryFailure: DeliveryFailure.Value = enabled; break;
                case GameEventType.OrderExpired: OrderExpired.Value = enabled; break;
                default:
                    if (GameEventTypes.IsStar(type)) StarsEnabled.Value = enabled;
                    else if (GameEventTypes.IsCombo(type)) ComboEnabled.Value = enabled;
                    break;
            }
        }
    }
}
