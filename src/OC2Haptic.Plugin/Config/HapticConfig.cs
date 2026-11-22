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
        internal readonly ConfigEntry<bool> StarHapticsEnabled, StarAudioEnabled, HazardHapticsEnabled;
        internal readonly ConfigEntry<bool> DebugLogEvents, ScoreSourceProbe;
        internal readonly ConfigEntry<bool> TeamDeliveryFallback, TeamHazardFallback;
        internal readonly ConfigEntry<float> MasterIntensity, SoundVolume;
        internal readonly ConfigEntry<int> XInputIndex, LocalPadSlot, MaxHapticTier;
        internal readonly ConfigEntry<KeyCode> MenuKey, ToggleKey;
        internal ComboTriggerMode ComboMode {get{return ComboTriggerMode.OnLevelUp;}}
        internal readonly Dictionary<GameEventType,ConfigEntry<bool>> EventSwitches=new Dictionary<GameEventType,ConfigEntry<bool>>();
        internal readonly Dictionary<GameEventType, ConfigEntry<string>> Patterns = new Dictionary<GameEventType, ConfigEntry<string>>();
        internal readonly ConfigEntry<string> OneStarTail;
        internal readonly ConfigEntry<string> SoundPackId;
        internal readonly ConfigEntry<string> Star3AudioFile,Star4AudioFile,Star4Selection;
        internal readonly ConfigEntry<float> Star3Volume,Star4Volume;
        private readonly Dictionary<GameEventType, HapticPattern> parsed = new Dictionary<GameEventType, HapticPattern>();
        private readonly List<Action<EventHandler, bool>> watchers = new List<Action<EventHandler, bool>>();
        private readonly Action<string> migrationLog;
        private HapticPattern tailPattern;
        internal string InvalidPatterns { get; private set; }

        internal HapticConfig(ConfigFile config,Action<string> migrationLog=null)
        {
            this.migrationLog=migrationLog;
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
            var legacySuccess=config.Bind("Events","DeliverySuccess",true,"Legacy coarse success preference.");
            var comboEnabled=Output(config.Bind("Combo","Enabled",true,"Verified upgrade feedback only; ordinary success remains independent."));
            var starsEnabled=config.Bind("Stars","Enabled",true,"Retired combined star switch; migrates to two feature channels.");
            StarHapticsEnabled=Output(config.Bind("Stars","HapticsEnabled",starsEnabled.Value,
                "Automatic star haptics, also subject to the haptic master. Explicit previews remain available."));
            StarAudioEnabled=Output(config.Bind("Stars","AudioEnabled",starsEnabled.Value,
                "Automatic star audio, also subject to the audio master. Explicit previews remain available."));
            config.Remove(starsEnabled.Definition);
            HazardHapticsEnabled=Output(config.Bind("Hazards","HapticsEnabled",true,
                "Automatic burn-warning, burned and fire haptics as a group. Individual Events switches still apply; excludes rejected/expired orders. Explicit previews remain available."));
            config.Remove(config.Bind("Combo","TriggerMode",ComboTriggerMode.OnLevelUp,"Retired selector; v1.6 requires upgrade-only mode A.").Definition);
            TeamDeliveryFallback=Output(config.Bind("Routing","AllowTeamDeliveryFallback",false,
                "Legacy preference retained; strict personal feedback ignores team delivery fallback. No guessed actor or broadcast."));
            TeamHazardFallback=Output(config.Bind("Routing","AllowTeamHazardFallback",false,
                "Legacy preference retained; strict personal feedback ignores unattributed team hazard fallback. Explicit previews remain available."));
            MaxHapticTier = Output(config.Bind("Combo", "MaxHapticTier", 4,
                new ConfigDescription("Haptic ceiling only; raw streak is never truncated.", new AcceptableValueRange<int>(1, 4))));
            config.Remove(config.Bind("Combo","ComboMasterIntensity",.85f,"Retired multiplier; v1.6 commands use the single master intensity.").Definition);
            SoundVolume = Output(config.Bind("StarFeedback", "SoundVolume", 1f,
                new ConfigDescription("Relative to game master/SFX volume: 1=100%, lower values attenuate. Applied once; respects game mute.", new AcceptableValueRange<float>(0, 1))));
            var legacyDuration=config.Bind("StarFeedback","MaxSoundDurationMs",1000,"Removed with generated crisp sound.");
            config.Remove(legacyDuration.Definition);
            SoundPackId=Output(config.Bind("StarFeedback","SoundPackId",RewardSoundPack.DefaultId,
                "One local sound pack for all four stars. Unknown/missing pack mutes audio only; restart to discover newly installed packs."));
            if(RewardSoundPack.IsRetired(SoundPackId.Value))SoundPackId.Value=RewardSoundPack.DefaultId;
            Star3AudioFile=Output(config.Bind("StarFeedback","Star3AudioFile","user-v1.7/star3_progress.wav","Independent three-star PCM16 WAV; relative to plugin audio folder or an absolute path. Restart after editing. Missing/invalid file mutes only sound."));
            Star4AudioFile=Output(config.Bind("StarFeedback","Star4AudioFile","user-v1.7/star4_victory.wav","Custom four-star PCM16 WAV,9-11s; relative to plugin audio folder or absolute. Restart after editing."));
            Star4Selection=Output(config.Bind("StarFeedback","Star4Selection",RewardSoundPack.CustomStarId,"Custom four-star file or an existing pack ID. Legacy pack music retains its original shorter duration. Old SoundPackId preference is preserved separately."));
            Star3Volume=Output(config.Bind("StarFeedback","Star3Volume",1f,new ConfigDescription("Three-star audio multiplier; user approved100% initially, applied once with relative volume.",new AcceptableValueRange<float>(0,1))));
            Star4Volume=Output(config.Bind("StarFeedback","Star4Volume",1f,new ConfigDescription("Four-star audio multiplier; user approved100% initially, applied once with relative volume.",new AcceptableValueRange<float>(0,1))));
            RespectGameFocus = Output(config.Bind("Audio", "RespectGameFocus", true, "Stop this plugin's feedback on focus loss. Recommended true."));
            foreach (GameEventType type in Enum.GetValues(typeof(GameEventType)))
            {
                config.Remove(config.Bind("EventStrength",type.ToString(),1f,"Retired extra event multiplier; use master intensity.").Definition);
                if (type == GameEventType.DeliverySuccess || type==GameEventType.TestPulse) continue;
                if(!GameEventTypes.IsStar(type))EventSwitches[type]=GameEventTypes.IsCombo(type)?comboEnabled:
                    Output(config.Bind("Events",type.ToString(),IsDeliverySuccess(type)?legacySuccess.Value:EventFeedback.For(type).DefaultEnabled,
                        "Automatic candidate feedback. Explicit tests bypass this event switch but always obey channel masters. Unverified hooks are not acceptance."));
                else EventSwitches[type]=Output(config.Bind("Stars","Star"+GameEventTypes.StarLevel(type)+"Enabled",true,"Automatic milestone only. Explicit previews bypass this switch; stars1/2 can never play audio."));
                Patterns[type] = Output(config.Bind("Patterns", type.ToString(), PatternCodec.Format(EventFeedback.For(type).Pattern),
                    "Editable candidate: left,right,milliseconds;... Silent segment: 0,0,gap. Invalid patterns mute this event's haptics."));
                if(GameEventTypes.IsStar(type))MigrateDefault(Patterns[type],LegacyStars[GameEventTypes.StarLevel(type)-1],EventFeedback.For(type).Pattern);
                if(GameEventTypes.IsStar(type))MigrateDefault(Patterns[type],LegacyV14Stars[GameEventTypes.StarLevel(type)-1],EventFeedback.For(type).Pattern);
                string previous;if(LegacyProfiles.TryGetValue(type,out previous))MigrateDefault(Patterns[type],previous,EventFeedback.For(type).Pattern);
            }
            config.Remove(legacySuccess.Definition);
            OneStarTail = Output(config.Bind("Patterns", "StarMilestone1HapticsOnly", PatternCodec.Format(EventFeedback.OneStarHapticsOnly),
                "Approved candidate one-star tail for haptics-only mode."));
            MigrateDefault(OneStarTail,"0.05,0.22,75;0,0,45;0.03,0.13,25",EventFeedback.OneStarHapticsOnly);
            MigrateDefault(OneStarTail,LegacyV14Stars[0],EventFeedback.OneStarHapticsOnly);
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
        private static readonly string[] LegacyV14Stars={"0.1,0.22,55;0,0,90;0.05,0.17,35",
            "0.14,0.23,70;0,0,105;0.17,0.35,110","0.18,0.26,55;0,0,70;0.23,0.34,80;0,0,75;0.32,0.43,145",
            "0.2,0.28,70;0,0,75;0.24,0.34,75;0,0,75;0.31,0.43,85;0,0,90;0.43,0.51,190"};
        private static readonly Dictionary<GameEventType,string> LegacyProfiles=new Dictionary<GameEventType,string>
        {
            {GameEventType.ThrowSuccess,"0.15,0.32,65"},{GameEventType.CatchSuccess,"0.22,0.1,35;0,0,35;0.08,0.3,45"},
            {GameEventType.DeliveryOrdered,"0,0.25,70;0,0,80;0.18,0.32,100"},{GameEventType.DeliveryOutOfOrder,"0.38,0.08,160"},
            {GameEventType.DeliveryUnclassified,"0,0.14,70"},{GameEventType.DeliveryFailure,"0.34,0,220"},{GameEventType.OrderExpired,"0.4,0.3,250"},
            {GameEventType.ComboLevel2,"0.12,0.28,60;0,0,55;0.2,0.38,110"},
            {GameEventType.ComboLevel3,"0,0.3,55;0,0,50;0,0.35,60;0,0,50;0.22,0.43,80"},
            {GameEventType.ComboLevel4,"0,0.31,55;0,0,40;0,0.35,55;0,0,40;0,0.39,55;0,0,40;0.25,0.48,95"}
        };
        private static bool IsDeliverySuccess(GameEventType type)
        {return type==GameEventType.DeliveryOrdered || type==GameEventType.DeliveryOutOfOrder || type==GameEventType.DeliveryUnclassified;}
        private void MigrateDefault(ConfigEntry<string> entry,string legacy,HapticPattern replacement)
        {
            HapticPattern old,baseline;
            if(!PatternCodec.TryParse(entry.Value,950,out old) || !PatternCodec.TryParse(legacy,950,out baseline) || old.Count!=baseline.Count)return;
            for(int i=0;i<old.Count;i++)
            {
                HapticSegment a=old.SegmentAt(i),b=baseline.SegmentAt(i);
                if(a.Left!=b.Left || a.Right!=b.Right || a.DurationMs!=b.DurationMs)return;
            }
            entry.Value=PatternCodec.Format(replacement);
            if(migrationLog!=null)migrationLog("Config migration | key="+entry.Definition.Section+"."+entry.Definition.Key+" known default -> current "+replacement.TotalDurationMs+"ms");
        }
        internal void WatchOutputChanges(EventHandler callback) { foreach (var watch in watchers) watch(callback, true); }
        internal void UnwatchOutputChanges(EventHandler callback) { foreach (var watch in watchers) watch(callback, false); }
        internal float Intensity { get { return RewardOutput.SafeVolume(MasterIntensity.Value); } }
        internal bool AnyOutputEnabled { get { return HapticsEnabled.Value || AudioEnabled.Value; } }
        internal StarFeedbackMode FeedbackMode { get { return OutputMode(HapticsEnabled.Value,AudioEnabled.Value); } }
        internal StarFeedbackMode StarMode {get{return OutputMode(HapticsEnabled.Value && StarHapticsEnabled.Value,
            AudioEnabled.Value && StarAudioEnabled.Value);}}
        internal StarFeedbackMode StarModeFor(GameEventType type){return StarAudioPolicy.ModeFor(GameEventTypes.StarLevel(type),StarMode);}
        internal float VolumeFor(GameEventType type)
        {int star=GameEventTypes.StarLevel(type);return star==1 || star==2?0:RewardOutput.SafeVolume(SoundVolume.Value)*
            (star==3?RewardOutput.SafeVolume(Star3Volume.Value):star==4?RewardOutput.SafeVolume(Star4Volume.Value):1);}
        private static StarFeedbackMode OutputMode(bool haptics,bool audio)
        {return haptics?(audio?StarFeedbackMode.HapticsAndAudio:StarFeedbackMode.HapticsOnly):(audio?StarFeedbackMode.AudioOnly:StarFeedbackMode.Disabled);}
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
                if (!PatternCodec.TryParse(pair.Value.Value, EventFeedback.For(pair.Key).Pattern.MaxDurationMs, out pattern)) InvalidPatterns += pair.Key + " ";
                parsed[pair.Key] = pattern;
            }
            if (!PatternCodec.TryParse(OneStarTail.Value, true, out tailPattern)) InvalidPatterns += "StarMilestone1HapticsOnly";
        }
        internal HapticPattern PatternFor(GameEventType type,StarFeedbackMode? previewMode=null)
        {
            if(type==GameEventType.TestPulse)return EventFeedback.For(type).Pattern;
            if (type == GameEventType.StarMilestone1 && (previewMode??StarMode) == StarFeedbackMode.HapticsOnly) return tailPattern;
            HapticPattern pattern;
            return parsed.TryGetValue(type, out pattern) ? pattern : null;
        }
        internal bool IsEventEnabled(GameEventType type)
        {
            if(GameEventTypes.IsStar(type))return EventSwitches[type].Value && StarModeFor(type)!=StarFeedbackMode.Disabled;
            if(GameEventTypes.IsHazard(type) && !HazardHapticsEnabled.Value)return false;
            ConfigEntry<bool> entry;return EventSwitches.TryGetValue(type,out entry) && entry.Value;
        }
        internal bool ShouldDispatch(GameEventType type)
        {return GameEventTypes.IsHazard(type)?EventSwitches[type].Value:IsEventEnabled(type);}
    }
}
