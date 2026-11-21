using System;
using System.Collections.Generic;
using OC2Haptic.Config;
using OC2Haptic.Core;
using UnityEngine;

namespace OC2Haptic.UI
{
    internal sealed class HapticMenu : IDisposable
    {
        private static readonly GameEventType[] Types = { GameEventType.ThrowSuccess, GameEventType.CatchSuccess,
            GameEventType.DeliverySuccess, GameEventType.DeliveryFailure, GameEventType.OrderExpired };
        private static readonly string[] Names = { "投掷成功", "接住食材", "上菜成功", "上菜失败", "订单超时" };
        private const string TopNote = "开关控制游戏自动反馈；试震为独立手动预览。\n总开关关闭时，所有试震均不输出。";
        private const string BottomNote = "菜单打开时只屏蔽当前绑定厨师的操作。\n关卡仍会运行；F6 或右上角 × 关闭菜单。";
        private const string Footer = "修改即时保存  ·  手动试震不计入真实游戏事件";
        private readonly UnityEngine.Object owner;
        private readonly HapticConfig settings;
        private readonly HapticMenuState state;
        private readonly Func<double> now;
        private readonly Action<string> log;
        private readonly MenuInputScope<PlayerControls> inputs = new MenuInputScope<PlayerControls>();
        private readonly HashSet<PlayerControls> eligible = new HashSet<PlayerControls>();
        private readonly Func<PlayerControls, Action> acquireInput;
        private bool ownsCursor, previousCursorVisible, positioned;
        private CursorLockMode previousCursorLock;
        private Rect rectangle;
        private Vector2 scroll;
        private int screenWidth, screenHeight;
        private Font font;
        private Texture2D background;
        private GUIStyle windowStyle, labelStyle, noteStyle, buttonStyle, toggleStyle;
        private string status = "点击试震，可直接比较现有触觉节奏。";

        internal HapticMenu(UnityEngine.Object owner, HapticConfig settings, HapticEngine engine,
            Func<double> now, Action<string> log)
        {
            this.owner = owner; this.settings = settings; this.now = now; this.log = log;
            state = new HapticMenuState(engine);
            acquireInput = AcquireInput;
        }

        internal bool IsOpen { get { return state.IsOpen; } }

        internal void Toggle()
        {
            if (state.IsOpen) { Close(); return; }
            state.Toggle();
            previousCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            ownsCursor = true;
            status = "菜单不会暂停关卡；调试前可先手动暂停。";
            Update();
            log("Haptic menu opened; local input suppressed, no automatic vibration.");
        }

        internal void Update()
        {
            if (!state.IsOpen) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            eligible.Clear();
            var providers = PlayerIDProvider.s_AllProviders;
            if (providers != null && settings.LocalPadSlot.Value >= 0)
            {
                for (int index = 0; index < providers.Count; index++)
                {
                    PlayerIDProvider provider = providers._items[index];
                    if (provider == null || !provider.IsLocallyControlled()) continue;
                    PlayerControls controls = provider.GetComponent<PlayerControls>();
                    if (controls == null || controls.ControlScheme == null) continue;
                    ControlPadInput.PadNum pad = PlayerInputLookup.GetPadForPlayer(controls.ControlScheme.Player);
                    if ((int)pad != settings.LocalPadSlot.Value || PCPadInputProvider.IsKeyboard(pad)) continue;
                    eligible.Add(controls);
                }
            }
            inputs.Reconcile(eligible, acquireInput);
        }

        private Action AcquireInput(PlayerControls controls)
        {
            Suppressor token = controls.Suppress(owner);
            return delegate
            {
                if (controls != null) controls.ReleaseSuppressor(token);
                else token.Release();
            };
        }

        internal void Close()
        {
            bool wasOpen = state.IsOpen;
            state.Close();
            try
            {
                inputs.Clear();
            }
            finally
            {
                eligible.Clear();
                if (ownsCursor)
                {
                    Cursor.lockState = previousCursorLock;
                    Cursor.visible = previousCursorVisible;
                    ownsCursor = false;
                }
            }
            if (wasOpen) log("Haptic menu closed; local input and cursor restored.");
        }

        internal void Draw()
        {
            if (!state.IsOpen) return;
            EnsureStyles();
            float width = Mathf.Min(520, Screen.width - 20);
            MenuMetrics metrics = Measure(width);
            float height = Mathf.Min(metrics.TotalHeight, Screen.height - 20);
            if (!positioned || screenWidth != Screen.width || screenHeight != Screen.height)
            {
                screenWidth = Screen.width; screenHeight = Screen.height;
                rectangle = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
                positioned = true;
                log("Menu layout | width=" + width + " height=" + height + " noteHeight=" + metrics.Top + " rowHeight=" + metrics.Row);
            }
            rectangle.width = width;
            rectangle.height = height;
            rectangle.x = Mathf.Clamp(rectangle.x, 0, Screen.width - rectangle.width);
            rectangle.y = Mathf.Clamp(rectangle.y, 0, Screen.height - rectangle.height);
            bool originalEnabled = GUI.enabled;
            Color originalColor = GUI.color;
            Color originalBackground = GUI.backgroundColor;
            try
            {
                GUI.color = Color.white;
                GUI.backgroundColor = Color.white;
                rectangle = GUI.Window(owner.GetInstanceID(), rectangle, DrawWindow, "手柄触觉管理", windowStyle);
            }
            finally { GUI.enabled = originalEnabled; GUI.color = originalColor; GUI.backgroundColor = originalBackground; }
        }

        private void DrawWindow(int id)
        {
            float width = rectangle.width;
            float height = rectangle.height;
            MenuMetrics metrics = Measure(width);
            float y = 42;
            if (GUI.Button(new Rect(width - 38, 6, 28, 24), "×", buttonStyle)) { Close(); return; }
            GUI.Label(new Rect(18, y, width - 36, metrics.Device), DeviceText(), labelStyle);
            y += metrics.Device + 8;
            bool enabled = GUI.Toggle(new Rect(18, y, width - 225, metrics.MainRow), settings.Enabled.Value, "总震动开关（F10）", toggleStyle);
            if (enabled != settings.Enabled.Value) settings.Enabled.Value = enabled;
            bool originalEnabled = GUI.enabled;
            GUI.enabled = originalEnabled && CanPreview();
            if (GUI.Button(new Rect(width - 206, y, 188, metrics.MainRow), "通用试震 · 80ms", buttonStyle)) Preview(null, "通用测试");
            GUI.enabled = originalEnabled;
            y += metrics.MainRow + 10;
            GUI.Label(new Rect(18, y, width - 36, metrics.Top), TopNote, noteStyle);
            y += metrics.Top + 10;

            float contentWidth = width - 52;
            float footerHeight = metrics.Bottom + 8 + metrics.Status + 6 + metrics.Footer + 16;
            float listHeight = Mathf.Max(48, height - y - 12 - footerHeight);
            scroll = GUI.BeginScrollView(new Rect(14, y, width - 28, listHeight), scroll,
                new Rect(0, 0, contentWidth, Types.Length * metrics.Row));
            try
            {
                for (int index = 0; index < Types.Length; index++)
                {
                    float rowY = index * metrics.Row;
                    bool current = settings.IsEventEnabled(Types[index]);
                    bool next = GUI.Toggle(new Rect(4, rowY, contentWidth - 112, metrics.Row - 8), current, Names[index], toggleStyle);
                    if (next != current) settings.SetEventEnabled(Types[index], next);
                    GUI.enabled = originalEnabled && CanPreview();
                    if (GUI.Button(new Rect(contentWidth - 96, rowY, 92, metrics.Row - 8), "试震", buttonStyle)) Preview(Types[index], Names[index]);
                    GUI.enabled = originalEnabled;
                }
            }
            finally { GUI.EndScrollView(); GUI.enabled = originalEnabled; }

            y += listHeight + 12;
            GUI.Label(new Rect(18, y, width - 36, metrics.Bottom), BottomNote, noteStyle);
            y += metrics.Bottom + 8;
            GUI.Label(new Rect(18, y, width - 36, metrics.Status), status, noteStyle);
            y += metrics.Status + 6;
            GUI.Label(new Rect(18, y, width - 36, metrics.Footer), Footer, noteStyle);
            GUI.DragWindow(new Rect(0, 0, width - 46, 34));
            Event input = Event.current;
            if ((input.type == EventType.MouseDown || input.type == EventType.MouseUp || input.type == EventType.ScrollWheel) &&
                new Rect(0, 0, width, height).Contains(input.mousePosition)) input.Use();
        }

        private string DeviceText()
        {
            return "手柄 XInput " + settings.XInputIndex.Value + "  ·  游戏 Pad " + settings.LocalPadSlot.Value +
                "  ·  总强度 " + Math.Round(settings.Intensity * 100) + "%";
        }

        private MenuMetrics Measure(float width)
        {
            float body = width - 36;
            var result = new MenuMetrics
            {
                Device = TextHeight(labelStyle, DeviceText(), body),
                MainRow = Mathf.Max(36, Mathf.Max(TextHeight(toggleStyle, "总震动开关（F10）", width - 225),
                    TextHeight(buttonStyle, "通用试震 · 80ms", 188))),
                Top = TextHeight(noteStyle, TopNote, body),
                Row = Mathf.Max(32, TextHeight(buttonStyle, "试震", 92)) + 8,
                Bottom = TextHeight(noteStyle, BottomNote, body),
                Status = TextHeight(noteStyle, status, body),
                Footer = TextHeight(noteStyle, Footer, body)
            };
            foreach (string name in Names) result.Row = Mathf.Max(result.Row, TextHeight(toggleStyle, name, width - 164) + 8);
            result.TotalHeight = 42 + result.Device + 8 + result.MainRow + 10 + result.Top + 10 +
                Types.Length * result.Row + 12 + result.Bottom + 8 + result.Status + 6 + result.Footer + 16;
            return result;
        }

        private static float TextHeight(GUIStyle style, string text, float width)
        {
            return Mathf.Ceil(style.CalcHeight(new GUIContent(text), width)) + 4;
        }

        private struct MenuMetrics
        {
            internal float Device, MainRow, Top, Row, Bottom, Status, Footer, TotalHeight;
        }

        private bool CanPreview()
        {
            return settings.Enabled.Value && settings.XInputIndex.Value >= 0 && settings.XInputIndex.Value <= 3;
        }

        private void Preview(GameEventType? type, string name)
        {
            bool accepted = state.Preview(type, settings.XInputIndex.Value, settings.Enabled.Value, settings.Intensity, now());
            status = accepted ? "已安排「" + name + "」手动试震。" : "未播放：检查连接、强度，或等待当前反馈结束。";
            log("Menu preview | type=" + (type.HasValue ? type.Value.ToString() : "Generic80ms") +
                " accepted=" + accepted + " XInput=" + settings.XInputIndex.Value + " master=" + settings.Intensity);
        }

        private void EnsureStyles()
        {
            if (windowStyle != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 18);
            windowStyle = new GUIStyle(GUI.skin.window) { font = font, fontSize = 20, fontStyle = FontStyle.Bold };
            background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            background.SetPixel(0, 0, new Color(.12f, .13f, .15f, .98f));
            background.Apply();
            foreach (GUIStyleState style in new[] { windowStyle.normal, windowStyle.hover, windowStyle.active, windowStyle.focused,
                windowStyle.onNormal, windowStyle.onHover, windowStyle.onActive, windowStyle.onFocused })
            {
                style.background = background;
                style.textColor = Color.white;
            }
            windowStyle.border = new RectOffset(0, 0, 0, 0);
            windowStyle.padding = new RectOffset(12, 12, 6, 6);
            windowStyle.alignment = TextAnchor.UpperCenter;
            labelStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 17 };
            noteStyle = new GUIStyle(labelStyle) { fontSize = 14, wordWrap = true };
            buttonStyle = new GUIStyle(GUI.skin.button) { font = font, fontSize = 16 };
            toggleStyle = new GUIStyle(GUI.skin.toggle) { font = font, fontSize = 17 };
        }

        public void Dispose()
        {
            Close();
            if (font != null) UnityEngine.Object.Destroy(font);
            if (background != null) UnityEngine.Object.Destroy(background);
        }
    }
}
