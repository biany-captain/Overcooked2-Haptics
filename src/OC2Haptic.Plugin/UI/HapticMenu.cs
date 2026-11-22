using System;
using System.Collections.Generic;
using OC2Haptic.Config;
using OC2Haptic.Core;
using UnityEngine;
using BepInEx.Configuration;

namespace OC2Haptic.UI
{
    internal sealed class HapticMenu : IDisposable
    {
        private static readonly GameEventType[] Types = { GameEventType.ThrowSuccess, GameEventType.CatchSuccess,
            GameEventType.DeliveryOrdered,GameEventType.DeliveryOutOfOrder,GameEventType.DeliveryUnclassified,
            GameEventType.DeliveryFailure,GameEventType.OrderExpired,GameEventType.ComboLevel2,GameEventType.ComboLevel3,GameEventType.ComboLevel4,
            GameEventType.BurnWarning,GameEventType.FoodBurned,GameEventType.FireStart,GameEventType.FireReminder };
        private static readonly string[] Names = { "投掷 · 自动默认关", "接住 · 自动默认关", "成功上菜 · 短震", "乱序成功 · 轻双段", "成功 · 安全降级",
            "真正拒单 · 长拖尾", "订单超时 · 两重击", "Combo ×2 升级", "Combo ×3 升级", "Combo ×4 升级",
            "烧焦预警 · 候选", "食物烧焦 · 自动默认关", "起火 · 候选", "持续火警 · 候选" };
        private static readonly GameEventType[] StarTypes={GameEventType.StarMilestone1,GameEventType.StarMilestone2,GameEventType.StarMilestone3,GameEventType.StarMilestone4};
        private static readonly string[] StarNames={"一星","二星","三星","四星"};
        private const string BottomNote = "菜单打开时只屏蔽当前绑定厨师的操作。\n关卡仍会运行；F6 或右上角 × 关闭菜单。";
        private const string Footer = "修改即时保存  ·  手动试震不计入真实游戏事件";
        private readonly UnityEngine.Object owner;
        private readonly HapticConfig settings;
        private readonly HapticMenuState state;
        private readonly Action<string> log;
        private readonly Func<GameEventType,StarFeedbackMode?,bool> previewEvent;
        private readonly Action stopPreviews;
        private readonly RewardSoundPack[] soundPacks;
        private readonly MenuInputScope<PlayerControls> inputs = new MenuInputScope<PlayerControls>();
        private readonly HashSet<PlayerControls> eligible = new HashSet<PlayerControls>();
        private readonly Func<PlayerControls, Action> acquireInput;
        private bool ownsCursor, previousCursorVisible, positioned;
        private CursorLockMode previousCursorLock;
        private Rect rectangle;
        private Vector2 scroll;
        private Vector2 outerScroll;
        private int tab;
        private bool packDropdown;
        private int screenWidth, screenHeight;
        private Font font;
        private Texture2D background;
        private GUIStyle windowStyle, labelStyle, noteStyle, buttonStyle, toggleStyle;
        private string status = "点击试震，可直接比较现有触觉节奏。";

        internal HapticMenu(UnityEngine.Object owner, HapticConfig settings, HapticEngine engine,
            Action<string> log, Func<GameEventType,StarFeedbackMode?,bool> previewEvent, Action stopPreviews,RewardSoundPack[] soundPacks)
        {
            this.owner = owner; this.settings = settings; this.log = log;
            this.previewEvent=previewEvent;this.stopPreviews=stopPreviews;
            this.soundPacks=soundPacks;
            state = new HapticMenuState(engine,stopPreviews);
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
            packDropdown=false;
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
            if (GUI.Button(new Rect(width - 38, 6, 28, 24), "×", buttonStyle)) { Close(); return; }
            float fixedHeight=metrics.TotalHeight-VisibleRows()*metrics.Row;
            if(height<fixedHeight+48)
            {
                float innerWidth=width-20;
                metrics=Measure(innerWidth);
                float virtualHeight=metrics.TotalHeight+(ContentRows()-VisibleRows())*metrics.Row;
                outerScroll=GUI.BeginScrollView(new Rect(0,36,width,height-36),outerScroll,
                    new Rect(0,36,innerWidth,virtualHeight-36));
                try {DrawContent(innerWidth,virtualHeight,metrics,true);}
                finally {GUI.EndScrollView();}
            }
            else DrawContent(width,height,metrics);
            GUI.DragWindow(new Rect(0,0,width-46,34));
            Event input=Event.current;
            if((input.type==EventType.MouseDown || input.type==EventType.MouseUp || input.type==EventType.ScrollWheel) &&
                new Rect(0,0,width,height).Contains(input.mousePosition))input.Use();
        }

        private void DrawContent(float width,float height,MenuMetrics metrics,bool wholeScroll=false)
        {
            float y=42;
            GUI.Label(new Rect(18, y, width - 36, metrics.Device), DeviceText(), labelStyle);
            y += metrics.Device + 8;
            bool haptics = GUI.Toggle(new Rect(18, y, width - 225, metrics.MainRow), settings.HapticsEnabled.Value, "震动总开关", toggleStyle);
            if (haptics != settings.HapticsEnabled.Value) settings.HapticsEnabled.Value = haptics;
            bool originalEnabled = GUI.enabled;
            GUI.enabled = originalEnabled && CanPreview();
            if (GUI.Button(new Rect(width - 206, y, 188, metrics.MainRow), "通用试震 · 80ms", buttonStyle)) Preview(null, "通用测试");
            GUI.enabled = originalEnabled;
            y += metrics.MainRow + 10;
            DrawStrength(18,y,width-36,metrics.Master,"总震动强度",settings.MasterIntensity);
            y+=metrics.Master+8;
            bool audio=GUI.Toggle(new Rect(18,y,width-36,metrics.MainRow),settings.AudioEnabled.Value,"音效总开关",toggleStyle);
            if(audio!=settings.AudioEnabled.Value)settings.AudioEnabled.Value=audio;
            y+=metrics.MainRow+10;
            DrawStrength(18,y,width-36,metrics.Master,"相对音量",settings.SoundVolume);
            y+=metrics.Master+8;
            int nextTab=GUI.SelectionGrid(new Rect(18,y,width-36,metrics.Tabs),tab,new[]{"设置","星级测试"},2,buttonStyle);
            if(nextTab!=tab){stopPreviews();tab=nextTab;packDropdown=false;scroll=Vector2.zero;outerScroll=Vector2.zero;status=tab==1?"每个星级可独立试音、试震或同时测试。":"所有事件统一使用总震动强度。";}
            y+=metrics.Tabs+8;
            GUI.Label(new Rect(18, y, width - 36, metrics.Top), TopText(), noteStyle);
            y += metrics.Top + 10;

            float contentWidth = width - 52;
            float footerHeight = metrics.Bottom + 8 + metrics.Status + 6 + metrics.Footer + 16;
            float listHeight = Mathf.Max(48, height - y - 12 - footerHeight);
            Rect listRect=new Rect(14,y,width-28,listHeight);
            if(wholeScroll)GUI.BeginGroup(listRect);
            else scroll=GUI.BeginScrollView(listRect,scroll,new Rect(0,0,contentWidth,ContentRows()*metrics.Row));
            try
            {
                if(tab==0)DrawEventSettings(contentWidth,metrics.Row,originalEnabled);
                else DrawStarTests(contentWidth,metrics.Row,originalEnabled);
            }
            finally { if(wholeScroll)GUI.EndGroup();else GUI.EndScrollView();GUI.enabled=originalEnabled; }

            y += listHeight + 12;
            GUI.Label(new Rect(18, y, width - 36, metrics.Bottom), BottomNote, noteStyle);
            y += metrics.Bottom + 8;
            GUI.Label(new Rect(18, y, width - 36, metrics.Status), status, noteStyle);
            y += metrics.Status + 6;
            GUI.Label(new Rect(18, y, width - 36, metrics.Footer), Footer, noteStyle);
        }

        private string TopText()
        {
            return tab==0?"F10 一键全关 / 全开；两个开关也控制手动测试。\n音量跟随游戏主音量 / 音效，相对100%为原声。"
                :"两个总开关同样控制下方测试，关闭后对应按钮不可用。\n音效包统一四星；主菜单 / 暂停中可单独测试。";
        }
        private int ContentRows(){return tab==0?Types.Length:1+(packDropdown?soundPacks.Length:0)+StarTypes.Length;}
        private int VisibleRows(){return tab==0?Math.Min(ContentRows(),10):ContentRows();}
        private void DrawEventSettings(float width,float row,bool originalEnabled)
        {
            for(int i=0;i<Types.Length;i++)
            {
                float y=i*row;
                GUI.Label(new Rect(4,y,width-112,row-8),Names[i],labelStyle);
                GUI.enabled=originalEnabled && CanPreview(Types[i]);
                if(GUI.Button(new Rect(width-96,y,92,row-8),"试震",buttonStyle))Preview(Types[i],Names[i]);
                GUI.enabled=originalEnabled;
            }
        }
        private void DrawStarTests(float width,float row,bool originalEnabled)
        {
            if(GUI.Button(new Rect(4,0,width-8,row-8),"音效包："+SelectedPackName()+(packDropdown?"  ▲":"  ▼"),buttonStyle))packDropdown=!packDropdown;
            int extra=packDropdown?soundPacks.Length:0;
            for(int i=0;i<extra;i++)
            {
                bool selected=string.Equals(settings.SoundPackId.Value,soundPacks[i].Id,StringComparison.OrdinalIgnoreCase);
                if(GUI.Button(new Rect(14,(i+1)*row,width-28,row-8),(selected?"● ":"  ")+soundPacks[i].Name,buttonStyle))
                {
                    stopPreviews();settings.SoundPackId.Value=soundPacks[i].Id;packDropdown=false;
                    status="已选择「"+soundPacks[i].Name+"」，下方可试听一到四星。";
                }
            }
            float header=(1+extra)*row;
            var modes=new[]{StarFeedbackMode.AudioOnly,StarFeedbackMode.HapticsOnly,StarFeedbackMode.HapticsAndAudio};
            var labels=new[]{"试音","试震","同时测试"};
            for(int i=0;i<StarTypes.Length;i++)
            {
                float y=header+i*row;
                GUI.Label(new Rect(4,y,84,row-8),StarNames[i],labelStyle);
                float buttonWidth=(width-100)/3;
                for(int j=0;j<modes.Length;j++)
                {
                    GUI.enabled=originalEnabled && CanPreview(StarTypes[i],modes[j]);
                    if(GUI.Button(new Rect(96+j*buttonWidth,y,buttonWidth-4,row-8),labels[j],buttonStyle))Preview(StarTypes[i],StarNames[i],modes[j]);
                }
                GUI.enabled=originalEnabled;
            }
        }
        private string SelectedPackName()
        {
            foreach(var pack in soundPacks)if(string.Equals(pack.Id,settings.SoundPackId.Value,StringComparison.OrdinalIgnoreCase))return pack.Name;
            return "所选包不可用（仅震动）";
        }
        private void DrawStrength(float x,float y,float width,float height,string name,ConfigEntry<float> entry)
        {
            float current=RewardOutput.SafeVolume(entry.Value);
            GUI.Label(new Rect(x,y,142,height),name+" "+Math.Round(current*100)+"%",noteStyle);
            float next=GUI.HorizontalSlider(new Rect(x+148,y+10,width-160,20),current,0,1);
            if(Math.Abs(next-current)>.0001f)
            {
                next=(float)Math.Round(next*100)/100;
                if(next!=entry.Value)entry.Value=next;
            }
        }

        private string DeviceText()
        {
            return "手柄 XInput " + settings.XInputIndex.Value + "  ·  游戏 Pad " + settings.LocalPadSlot.Value +
                "  ·  可在下方调整震动强度";
        }

        private MenuMetrics Measure(float width)
        {
            float body = width - 36;
            var result = new MenuMetrics
            {
                Device = TextHeight(labelStyle, DeviceText(), body),
                MainRow = Mathf.Max(36, Mathf.Max(TextHeight(toggleStyle, "震动总开关", width - 225),
                    TextHeight(buttonStyle, "通用试震 · 80ms", 188))),
                Master=36,Tabs=36,
                Top = TextHeight(noteStyle, TopText(), body),
                Row = Mathf.Max(32, TextHeight(buttonStyle, "试震", 92)) + 8,
                Bottom = TextHeight(noteStyle, BottomNote, body),
                Status = TextHeight(noteStyle, status, body),
                Footer = TextHeight(noteStyle, Footer, body)
            };
            foreach (string name in Names) result.Row = Mathf.Max(result.Row, TextHeight(toggleStyle, name, width - 164) + 8);
            if(tab==1)
            {
                result.Row=Mathf.Max(result.Row,TextHeight(buttonStyle,"音效包："+SelectedPackName()+"  ▼",width-60)+8);
                foreach(var pack in soundPacks)result.Row=Mathf.Max(result.Row,TextHeight(buttonStyle,"● "+pack.Name,width-80)+8);
            }
            result.TotalHeight = 42 + result.Device + 8 + 2*(result.MainRow + 10 + result.Master+8)+result.Tabs+8+result.Top + 10 +
                VisibleRows() * result.Row + 12 + result.Bottom + 8 + result.Status + 6 + result.Footer + 16;
            return result;
        }

        private static float TextHeight(GUIStyle style, string text, float width)
        {
            return Mathf.Ceil(style.CalcHeight(new GUIContent(text), width)) + 4;
        }

        private struct MenuMetrics
        {
            internal float Device, MainRow, Master, Tabs, Top, Row, Bottom, Status, Footer, TotalHeight;
        }

        private bool CanPreview(GameEventType? type=null,StarFeedbackMode? previewMode=null)
        {
            if(type.HasValue && GameEventTypes.IsStar(type.Value))
            {
                StarFeedbackMode requested=previewMode??settings.FeedbackMode;
                bool haptics=requested==StarFeedbackMode.HapticsOnly || requested==StarFeedbackMode.HapticsAndAudio;
                bool audio=requested==StarFeedbackMode.AudioOnly || requested==StarFeedbackMode.HapticsAndAudio;
                return (haptics || audio) && (!audio || settings.AudioEnabled.Value) &&
                    (!haptics || (settings.HapticsEnabled.Value && settings.XInputIndex.Value>=0 && settings.XInputIndex.Value<=3));
            }
            return settings.HapticsEnabled.Value && settings.XInputIndex.Value >= 0 && settings.XInputIndex.Value <= 3;
        }

        private void Preview(GameEventType? type, string name,StarFeedbackMode? previewMode=null)
        {
            bool accepted = previewEvent(type??GameEventType.TestPulse,previewMode);
            status = accepted ? "已安排「" + name + "」手动预览。" : "未播放：检查连接、模式，或等待当前反馈结束。";
            log("Menu preview | type=" + (type.HasValue ? type.Value.ToString() : "Generic80ms") +
                " mode="+(previewMode.HasValue?previewMode.Value.ToString():"Automatic")+" accepted=" + accepted + " XInput=" + settings.XInputIndex.Value + " master=" + settings.Intensity+
                " haptics="+settings.HapticsEnabled.Value+" audio="+settings.AudioEnabled.Value);
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
