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
            GameEventType.DeliveryFailure,GameEventType.OrderExpired,GameEventType.ComboLevel2,GameEventType.ComboLevel3,GameEventType.ComboLevel4 };
        private static readonly string[] Names = { "投掷成功", "接住食材 · 双段", "顺序上菜 / Combo ×1", "乱序成功上菜", "成功上菜 · 未分类",
            "上菜失败", "订单超时", "Combo ×2", "Combo ×3", "Combo ×4+" };
        private static readonly GameEventType[] StarTypes={GameEventType.StarMilestone1,GameEventType.StarMilestone2,GameEventType.StarMilestone3,GameEventType.StarMilestone4};
        private static readonly string[] StarNames={"一星","二星","三星","四星"};
        private const string BottomNote = "菜单打开时只屏蔽当前绑定厨师的操作。\n关卡仍会运行；F6 或右上角 × 关闭菜单。";
        private const string Footer = "修改即时保存  ·  手动试震不计入真实游戏事件";
        private readonly UnityEngine.Object owner;
        private readonly HapticConfig settings;
        private readonly HapticMenuState state;
        private readonly Func<double> now;
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
            Func<double> now, Action<string> log, Func<GameEventType,StarFeedbackMode?,bool> previewEvent, Action stopPreviews,RewardSoundPack[] soundPacks)
        {
            this.owner = owner; this.settings = settings; this.now = now; this.log = log;
            this.previewEvent=previewEvent;this.stopPreviews=stopPreviews;
            this.soundPacks=soundPacks;
            state = new HapticMenuState(engine);
            acquireInput = AcquireInput;
        }

        internal bool IsOpen { get { return state.IsOpen; } }

        internal void Toggle()
        {
            if (state.IsOpen) { Close(); return; }
            state.Toggle();
            stopPreviews();
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
            stopPreviews();
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
            bool enabled = GUI.Toggle(new Rect(18, y, width - 225, metrics.MainRow), settings.Enabled.Value, "总反馈开关（F10）", toggleStyle);
            if (enabled != settings.Enabled.Value) settings.Enabled.Value = enabled;
            bool originalEnabled = GUI.enabled;
            GUI.enabled = originalEnabled && CanPreview();
            if (GUI.Button(new Rect(width - 206, y, 188, metrics.MainRow), "通用试震 · 80ms", buttonStyle)) Preview(null, "通用测试");
            GUI.enabled = originalEnabled;
            y += metrics.MainRow + 10;
            DrawStrength(18,y,width-36,metrics.Master,"总震动强度",settings.MasterIntensity,1);
            y+=metrics.Master+8;
            int nextTab=GUI.SelectionGrid(new Rect(18,y,width-36,metrics.Tabs),tab,new[]{"事件设置","星级测试"},2,buttonStyle);
            if(nextTab!=tab){stopPreviews();tab=nextTab;packDropdown=false;scroll=Vector2.zero;outerScroll=Vector2.zero;status=tab==1?"每个星级可独立试音、试震或同时测试。":"可分别调整每类事件的震动倍率。";}
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
            return tab==0?"单项倍率叠加总强度，不改变节奏；列表可滚动。\n0%不震动；星级试听请点击上方「星级测试」。"
                :"音效包统一作用于四个星级；下方列表可滚动。\n测试不改自动模式；主菜单 / 暂停中也可试听。";
        }
        private int ContentRows(){return tab==0?1+Types.Length*2:4+(packDropdown?soundPacks.Length:0)+StarTypes.Length*2;}
        private int VisibleRows(){return tab==0?Math.Min(ContentRows(),10):ContentRows();}
        private void DrawEventSettings(float width,float row,bool originalEnabled)
        {
            bool combo=GUI.Toggle(new Rect(4,0,width/2-8,row-8),settings.ComboEnabled.Value,"Combo 分级",toggleStyle);
            if(combo!=settings.ComboEnabled.Value)settings.ComboEnabled.Value=combo;
            bool stars=GUI.Toggle(new Rect(width/2,0,width/2-4,row-8),settings.StarsEnabled.Value,"星级自动反馈",toggleStyle);
            if(stars!=settings.StarsEnabled.Value)settings.StarsEnabled.Value=stars;
            for(int i=0;i<Types.Length;i++)
            {
                float y=(1+i*2)*row;
                if(GameEventTypes.IsCombo(Types[i]))GUI.Label(new Rect(4,y,width-112,row-8),Names[i],labelStyle);
                else
                {
                    bool current=settings.IsEventEnabled(Types[i]);
                    bool next=GUI.Toggle(new Rect(4,y,width-112,row-8),current,Names[i],toggleStyle);
                    if(next!=current)settings.SetEventEnabled(Types[i],next);
                }
                GUI.enabled=originalEnabled && CanPreview(Types[i]);
                if(GUI.Button(new Rect(width-96,y,92,row-8),"试震",buttonStyle))Preview(Types[i],Names[i]);
                GUI.enabled=originalEnabled;
                DrawStrength(4,y+row,width-8,row-8,"事件倍率",settings.EventGains[Types[i]],2);
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
            bool enabled=GUI.Toggle(new Rect(4,header,width/2,row-8),settings.StarsEnabled.Value,"星级自动反馈",toggleStyle);
            if(enabled!=settings.StarsEnabled.Value)settings.StarsEnabled.Value=enabled;
            GUI.Label(new Rect(width/2,header,width/2-4,row-8),"下方为自动输出模式",noteStyle);
            int mode=GUI.SelectionGrid(new Rect(4,header+row,width-8,row-8),(int)settings.FeedbackMode,
                new[]{"震动＋音效","仅震动","仅音效","关闭"},4,buttonStyle);
            if(mode!=(int)settings.FeedbackMode)settings.StarMode.Value=(StarFeedbackMode)mode;
            DrawStrength(4,header+row*2,width-8,row-8,"庆祝音量",settings.SoundVolume,1);
            var modes=new[]{StarFeedbackMode.AudioOnly,StarFeedbackMode.HapticsOnly,StarFeedbackMode.HapticsAndAudio};
            var labels=new[]{"试音","试震","同时测试"};
            for(int i=0;i<StarTypes.Length;i++)
            {
                float y=header+(3+i*2)*row;
                GUI.Label(new Rect(4,y,84,row-8),StarNames[i],labelStyle);
                float buttonWidth=(width-100)/3;
                for(int j=0;j<modes.Length;j++)
                {
                    GUI.enabled=originalEnabled && CanPreview(StarTypes[i],modes[j]);
                    if(GUI.Button(new Rect(96+j*buttonWidth,y,buttonWidth-4,row-8),labels[j],buttonStyle))Preview(StarTypes[i],StarNames[i],modes[j]);
                }
                GUI.enabled=originalEnabled;
                DrawStrength(4,y+row,width-8,row-8,"星级倍率",settings.EventGains[StarTypes[i]],2);
            }
        }
        private string SelectedPackName()
        {
            foreach(var pack in soundPacks)if(string.Equals(pack.Id,settings.SoundPackId.Value,StringComparison.OrdinalIgnoreCase))return pack.Name;
            return "所选包不可用（仅震动）";
        }
        private void DrawStrength(float x,float y,float width,float height,string name,ConfigEntry<float> entry,float maximum)
        {
            float current=maximum==2?HapticPattern.ClampGain(entry.Value):RewardOutput.SafeVolume(entry.Value);
            GUI.Label(new Rect(x,y,142,height),name+" "+Math.Round(current*100)+"%",noteStyle);
            float next=GUI.HorizontalSlider(new Rect(x+148,y+10,width-160,20),current,0,maximum);
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
                MainRow = Mathf.Max(36, Mathf.Max(TextHeight(toggleStyle, "总震动开关（F10）", width - 225),
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
            result.TotalHeight = 42 + result.Device + 8 + result.MainRow + 10 + result.Master+8+result.Tabs+8+result.Top + 10 +
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
                return settings.Enabled.Value && (previewMode??settings.FeedbackMode)!=StarFeedbackMode.Disabled &&
                    ((previewMode??settings.FeedbackMode)!=StarFeedbackMode.HapticsOnly || (settings.XInputIndex.Value>=0 && settings.XInputIndex.Value<=3));
            return settings.Enabled.Value && settings.XInputIndex.Value >= 0 && settings.XInputIndex.Value <= 3;
        }

        private void Preview(GameEventType? type, string name,StarFeedbackMode? previewMode=null)
        {
            bool accepted = type.HasValue?previewEvent(type.Value,previewMode):state.Preview(type, settings.XInputIndex.Value, settings.Enabled.Value, settings.Intensity, now());
            status = accepted ? "已安排「" + name + "」手动预览。" : "未播放：检查连接、模式，或等待当前反馈结束。";
            log("Menu preview | type=" + (type.HasValue ? type.Value.ToString() : "Generic80ms") +
                " mode="+(previewMode.HasValue?previewMode.Value.ToString():"Automatic")+" accepted=" + accepted + " XInput=" + settings.XInputIndex.Value + " master=" + settings.Intensity+
                " gain="+(type.HasValue?HapticPattern.ClampGain(settings.EventGains[type.Value].Value):1));
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
