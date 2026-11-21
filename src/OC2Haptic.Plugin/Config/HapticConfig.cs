using System;
using BepInEx.Configuration;
using OC2Haptic.Core;
using UnityEngine;

namespace OC2Haptic.Config
{
    internal sealed class HapticConfig
    {
        internal readonly ConfigEntry<bool> Enabled;
        internal readonly ConfigEntry<float> MasterIntensity;
        internal readonly ConfigEntry<int> XInputIndex;
        internal readonly ConfigEntry<int> LocalPadSlot;
        internal readonly ConfigEntry<bool> ThrowSuccess;
        internal readonly ConfigEntry<bool> CatchSuccess;
        internal readonly ConfigEntry<bool> DeliverySuccess;
        internal readonly ConfigEntry<bool> DeliveryFailure;
        internal readonly ConfigEntry<bool> OrderExpired;
        internal readonly ConfigEntry<bool> DebugLogEvents;
        internal readonly ConfigEntry<KeyCode> MenuKey;
        internal readonly ConfigEntry<KeyCode> ToggleKey;

        internal HapticConfig(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true, "Enable haptics. Disabling immediately stops this plugin's output.");
            MasterIntensity = config.Bind("General", "MasterIntensity", .75f,
                new ConfigDescription("Global intensity multiplier. Approved initial value: 0.75.", new AcceptableValueRange<float>(0, 1)));
            DebugLogEvents = config.Bind("General", "DebugLogEvents", false, "Log every observed game event and routing result. Enable for batch event validation.");
            XInputIndex = config.Bind("Devices", "XInputIndex", -1,
                new ConfigDescription("Explicit output slot: 0-3. -1 means unbound; no automatic vibration or slot guessing.", new AcceptableValueRange<int>(-1, 3)));
            LocalPadSlot = config.Bind("Devices", "LocalGamePadSlot", -1,
                new ConfigDescription("Game pad number reported by the throw probe log (0-14), NOT the XInput index. -1 disables personal feedback until bound.", new AcceptableValueRange<int>(-1, 14)));
            ThrowSuccess = config.Bind("Events", "ThrowSuccess", true, "Enable feedback after a local chef's confirmed flight-state event. Hook behavior still requires game testing.");
            CatchSuccess = config.Bind("Events", "CatchSuccess", true, "90 ms local chef catch feedback; current Hook covers local/host execution, online-client coverage unverified.");
            DeliverySuccess = config.Bind("Events", "DeliverySuccess", true, "Short double pulse after a confirmed delivery for the bound player's team.");
            DeliveryFailure = config.Bind("Events", "DeliveryFailure", true, "220 ms low motor pulse after a rejected delivery for the bound player's team.");
            OrderExpired = config.Bind("Events", "OrderExpired", true, "250 ms dual motor pulse when an order has actually expired; no advance warning.");
            // Bind/remove the old entry once so its existing value can seed the new key without a duplicate handler.
            var legacyTestKey = config.Bind("Hotkeys", "TestRumble", KeyCode.F6, "Legacy direct-test key, migrated to OpenMenu.");
            MenuKey = config.Bind("Hotkeys", "OpenMenu", legacyTestKey.Value,
                "Open/close haptic management menu. Opening does not vibrate. None disables this hotkey.");
            config.Remove(legacyTestKey.Definition);
            ToggleKey = config.Bind("Hotkeys", "ToggleMod", KeyCode.F10, "Toggle Enabled. Confirmed alternative to the existing MOD's F8/F9 bindings.");
            config.Save();
        }

        internal float Intensity
        {
            get
            {
                float value = MasterIntensity.Value;
                return float.IsNaN(value) || float.IsInfinity(value) ? 0 : Math.Max(0, Math.Min(1, value));
            }
        }

        internal void WatchOutputChanges(EventHandler callback)
        {
            Enabled.SettingChanged += callback;
            MasterIntensity.SettingChanged += callback;
            XInputIndex.SettingChanged += callback;
            LocalPadSlot.SettingChanged += callback;
            ThrowSuccess.SettingChanged += callback;
            CatchSuccess.SettingChanged += callback;
            DeliverySuccess.SettingChanged += callback;
            DeliveryFailure.SettingChanged += callback;
            OrderExpired.SettingChanged += callback;
        }

        internal void UnwatchOutputChanges(EventHandler callback)
        {
            Enabled.SettingChanged -= callback;
            MasterIntensity.SettingChanged -= callback;
            XInputIndex.SettingChanged -= callback;
            LocalPadSlot.SettingChanged -= callback;
            ThrowSuccess.SettingChanged -= callback;
            CatchSuccess.SettingChanged -= callback;
            DeliverySuccess.SettingChanged -= callback;
            DeliveryFailure.SettingChanged -= callback;
            OrderExpired.SettingChanged -= callback;
        }

        internal bool IsEventEnabled(GameEventType type)
        {
            switch (type)
            {
                case GameEventType.ThrowSuccess: return ThrowSuccess.Value;
                case GameEventType.CatchSuccess: return CatchSuccess.Value;
                case GameEventType.DeliverySuccess: return DeliverySuccess.Value;
                case GameEventType.DeliveryFailure: return DeliveryFailure.Value;
                case GameEventType.OrderExpired: return OrderExpired.Value;
                default: return false;
            }
        }

        internal void SetEventEnabled(GameEventType type, bool enabled)
        {
            switch (type)
            {
                case GameEventType.ThrowSuccess: ThrowSuccess.Value = enabled; break;
                case GameEventType.CatchSuccess: CatchSuccess.Value = enabled; break;
                case GameEventType.DeliverySuccess: DeliverySuccess.Value = enabled; break;
                case GameEventType.DeliveryFailure: DeliveryFailure.Value = enabled; break;
                case GameEventType.OrderExpired: OrderExpired.Value = enabled; break;
            }
        }
    }
}
