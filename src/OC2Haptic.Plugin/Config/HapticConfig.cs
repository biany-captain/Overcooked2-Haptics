using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using OC2Haptic.Core;
using UnityEngine;

namespace OC2Haptic.Config
{
    internal sealed class HapticConfig
    {
        internal readonly ConfigEntry<bool> HapticsEnabled, AudioEnabled, RespectGameFocus;
        internal readonly ConfigEntry<bool> DebugLogEvents, ScoreSourceProbe;
        internal readonly ConfigEntry<float> MasterIntensity, SoundVolume;
        internal readonly ConfigEntry<int> XInputIndex, LocalPadSlot, MaxHapticTier;
        internal readonly ConfigEntry<KeyCode> MenuKey, ToggleKey;
        internal readonly ConfigEntry<ComboTriggerMode> ComboMode;
        internal readonly Dictionary<GameEventType, ConfigEntry<string>> Patterns = new Dictionary<GameEventType, ConfigEntry<string>>();
        internal readonly ConfigEntry<string> OneStarTail;
        internal readonly ConfigEntry<string> SoundPackId;
        private readonly Dictionary<GameEventType, HapticPattern> parsed = new Dictionary<GameEventType, HapticPattern>();
        private readonly List<Action<EventHandler, bool>> watchers = new List<Action<EventHandler, bool>>();
        private HapticPattern tailPattern;
        internal string InvalidPatterns { get; private set; }

        internal HapticConfig(ConfigFile config)
        {
            var legacyEnabled=config.Bind("General","Enabled",true,"Retired combined output switch.");
            var legacyMode=config.Bind("StarFeedback","Mode",StarFeedbackMode.HapticsAndAudio,"Retired star output mode.");
            HapticsEnabled=Output(config.Bind("General","HapticsEnabled",legacyEnabled.Value &&
                (legacyMode.Value==StarFeedbackMode.HapticsAndAudio || legacyMode.Value==StarFeedbackMode.HapticsOnly),
                "Master switch for all haptics, including explicit tests. Independent of audio."));
            AudioEnabled=Output(config.Bind("General","AudioEnabled",legacyEnabled.Value &&
                (legacyMode.Value==StarFeedbackMode.HapticsAndAudio || legacyMode.Value==StarFeedbackMode.AudioOnly),
                "Master switch for reward audio, including explicit tests. Follows game master/SFX volume."));
            config.Remove(legacyEnabled.Definition);config.Remove(legacyMode.Definition);
            MasterIntensity = Output(config.Bind("General", "MasterIntensity", .75f,
                new ConfigDescription("Haptic motor multiplier, independent of audio gain.", new AcceptableValueRange<float>(0, 1))));
            DebugLogEvents = config.Bind("General", "DebugLogEvents", false, "Log real event routing, classification and reward decisions.");
            ScoreSourceProbe = config.Bind("Diagnostics", "ScoreSourceProbe", false, "Detailed read-only before/payload/after diagnostics; capture required by rewards remains active independently.");
            XInputIndex = Output(config.Bind("Devices", "XInputIndex", -1,
                new ConfigDescription("Explicit Windows output slot, -1 unbound.", new AcceptableValueRange<int>(-1, 3))));
            LocalPadSlot = Output(config.Bind("Devices", "LocalGamePadSlot", -1,
                new ConfigDescription("Actual game pad number, independent of XInput. -1 unbound.", new AcceptableValueRange<int>(-1, 14))));
            foreach(string key in new[]{"ThrowSuccess","CatchSuccess","DeliverySuccess","DeliveryOrdered","DeliveryOutOfOrder",
                "DeliveryUnclassified","DeliveryFailure","OrderExpired"})
                config.Remove(config.Bind("Events",key,true,"Retired per-event switch; use the channel masters.").Definition);
            config.Remove(config.Bind("Combo","Enabled",true,"Retired per-event switch.").Definition);
            config.Remove(config.Bind("Stars","Enabled",true,"Retired per-event switch.").Definition);
            ComboMode = Output(config.Bind("Combo", "TriggerMode", ComboTriggerMode.EveryOrderedDelivery, "EveryOrderedDelivery or OnLevelUp; authoritative game state only."));
            MaxHapticTier = Output(config.Bind("Combo", "MaxHapticTier", 4,
                new ConfigDescription("Haptic ceiling only; raw streak is never truncated.", new AcceptableValueRange<int>(1, 4))));
            config.Remove(config.Bind("Combo","ComboMasterIntensity",.85f,"Retired adjustable multiplier; keep the original Combo baseline.").Definition);
            SoundVolume = Output(config.Bind("StarFeedback", "SoundVolume", 1f,
                new ConfigDescription("Relative to game master/SFX volume: 1=100%, lower values attenuate. Applied once; respects game mute.", new AcceptableValueRange<float>(0, 1))));
            var legacyDuration=config.Bind("StarFeedback","MaxSoundDurationMs",1000,"Removed with generated crisp sound.");
            config.Remove(legacyDuration.Definition);
            SoundPackId=Output(config.Bind("StarFeedback","SoundPackId",RewardSoundPack.DefaultId,
                "One local sound pack for all four stars. Unknown/missing pack mutes audio only; restart to discover newly installed packs."));
            if(RewardSoundPack.IsRetired(SoundPackId.Value))SoundPackId.Value=RewardSoundPack.DefaultId;
            RespectGameFocus = Output(config.Bind("Audio", "RespectGameFocus", true, "Stop this plugin's feedback on focus loss. Recommended true."));
            foreach (GameEventType type in Enum.GetValues(typeof(GameEventType)))
            {
                config.Remove(config.Bind("EventStrength",type.ToString(),1f,"Retired extra event multiplier; use master intensity.").Definition);
                if (type == GameEventType.DeliverySuccess) continue;
                Patterns[type] = Output(config.Bind("Patterns", type.ToString(), PatternCodec.Format(EventFeedback.For(type).Pattern),
                    "Editable candidate: left,right,milliseconds;... Silent segment: 0,0,gap. Invalid patterns mute this event's haptics."));
                if(GameEventTypes.IsStar(type))MigrateDefault(Patterns[type],LegacyStars[GameEventTypes.StarLevel(type)-1],EventFeedback.For(type).Pattern);
            }
            OneStarTail = Output(config.Bind("Patterns", "StarMilestone1HapticsOnly", PatternCodec.Format(EventFeedback.OneStarHapticsOnly),
                "Approved candidate one-star tail for haptics-only mode."));
            MigrateDefault(OneStarTail,"0.05,0.22,75;0,0,45;0.03,0.13,25",EventFeedback.OneStarHapticsOnly);
            var legacyTestKey = config.Bind("Hotkeys", "TestRumble", KeyCode.F6, "Legacy key, migrated to OpenMenu.");
            MenuKey = config.Bind("Hotkeys", "OpenMenu", legacyTestKey.Value, "F6 management menu. None disables the key.");
            config.Remove(legacyTestKey.Definition);
            ToggleKey = config.Bind("Hotkeys", "ToggleMod", KeyCode.F10, "All-off if either channel is on; all-on when both are off. F6 channel switches remain independent.");
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
        private static readonly string[] LegacyStars={"0.05,0.22,75","0.06,0.26,65;0,0,55;0.14,0.34,95",
            "0.07,0.26,55;0,0,45;0.13,0.33,65;0,0,45;0.2,0.42,105",
            "0.09,0.26,55;0,0,45;0.12,0.34,60;0,0,45;0.18,0.42,65;0,0,60;0.32,0.48,170"};
        private static void MigrateDefault(ConfigEntry<string> entry,string legacy,HapticPattern replacement)
        {
            HapticPattern old,baseline;
            if(!PatternCodec.TryParse(entry.Value,true,out old) || !PatternCodec.TryParse(legacy,true,out baseline) || old.Count!=baseline.Count)return;
            for(int i=0;i<old.Count;i++)
            {
                HapticSegment a=old.SegmentAt(i),b=baseline.SegmentAt(i);
                if(a.Left!=b.Left || a.Right!=b.Right || a.DurationMs!=b.DurationMs)return;
            }
            entry.Value=PatternCodec.Format(replacement);
        }
        internal void WatchOutputChanges(EventHandler callback) { foreach (var watch in watchers) watch(callback, true); }
        internal void UnwatchOutputChanges(EventHandler callback) { foreach (var watch in watchers) watch(callback, false); }
        internal float Intensity { get { return RewardOutput.SafeVolume(MasterIntensity.Value); } }
        internal bool AnyOutputEnabled { get { return HapticsEnabled.Value || AudioEnabled.Value; } }
        internal StarFeedbackMode FeedbackMode { get { return HapticsEnabled.Value
            ?(AudioEnabled.Value?StarFeedbackMode.HapticsAndAudio:StarFeedbackMode.HapticsOnly)
            :(AudioEnabled.Value?StarFeedbackMode.AudioOnly:StarFeedbackMode.Disabled); } }
        internal void ToggleAllOutput()
        {
            bool next=!AnyOutputEnabled;
            HapticsEnabled.Value=next;AudioEnabled.Value=next;
        }
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
        internal HapticPattern PatternFor(GameEventType type,StarFeedbackMode? previewMode=null)
        {
            if (type == GameEventType.StarMilestone1 && (previewMode??FeedbackMode) == StarFeedbackMode.HapticsOnly) return tailPattern;
            HapticPattern pattern;
            return parsed.TryGetValue(type, out pattern) ? pattern : null;
        }
    }
}
