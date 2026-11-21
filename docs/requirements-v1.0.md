# 《胡闹厨房2》手柄触觉增强 MOD：AI 开发执行规格书

> 项目暂定名：OC2 Haptic / Overcooked! 2 Haptic Feedback MOD  
> 文档版本：v1.0（2026-11-21）  
> 目标平台：Windows PC 版《Overcooked! 2》（优先验证 Steam 版）；手柄优先支持 XInput（Xbox 兼容模式）。  
> 文档用途：直接交给 Codex、Claude Code 等 AI 编程代理作为项目目标、分阶段实施要求和验收依据。  
> 状态约定：✅ 有公开资料依据；🔎 需结合当前游戏版本反编译／运行确认；💡 后续选配设计，非已实现功能。

## 1. 项目背景、目标与原则

### 1.1 背景

《Overcooked! 2》本身通常不提供手柄震动反馈。Team17 在 Steam 讨论中回应过：未提供震动是有意设计。此项目增加一个独立的、非侵入式的触觉增强 MOD，让投掷、接取、切菜、交付订单、烹饪危险和时间警告产生易辨识的震动。目标是提升操作反馈及危险察觉，而不是改变游戏规则。

### 1.2 核心目标

1. 在不改动原始游戏逻辑、数值、网络协议及存档的前提下，监听真实的游戏事件并输出震动。
2. V0.1 支持单机/本地单手柄：一键测试震动 + 至少一种已验证的真实游戏事件；再逐步扩展到投掷、接住、上菜、订单失效。
3. V0.2 引入燃烧与订单风险提醒，支持可调强度、冷却时间和优先级。
4. V1.0 目标支持 1–4 个本地手柄和基本在线联机模式；仅对本地实际连接的手柄震动。
5. 支持其他 BepInEx MOD 共存，失效时不影响游戏继续运行。
6. 提供可修改的配置文件、完整日志、安装卸载步骤及自动/手动测试记录。

### 1.3 明确不做

- 不自动操作人物、修改分数、技能、食材、关卡和联机同步逻辑。
- 不要求每个在线玩家同时安装 MOD 才能正常进行原版游戏；若某功能受联机角色同步限制，则自动降级。
- 不使用内存地址硬编码、游戏 EXE 二进制补丁或替换游戏原生 DLL 作为首选方案。
- V0.1 不做 PyQt 桌面常驻程序、账号系统、联网服务或大型 GUI。
- 不声称所有手柄均支持同样的震动；XInput 双马达震动不等于特定品牌的高阶触觉/自适应扳机功能。

### 1.4 成功定义

基础验收需要：游戏能正常进入厨房；BepInEx 插件正常加载；测试热键可震动；至少一个经过真实游戏操作验证的 Hook 能触发并准确停止震动；关闭 MOD 后原版游戏行为不变。多手柄和复杂预警属于后续阶段，不能阻塞第一版可运行交付。

## 2. 技术事实、依赖与待验证点

### 2.1 已有资料支持的事实

- ✅ 有《胡闹厨房2》使用 BepInEx 5 + Harmony 的公开 MOD；`gua248/Overcooked2-Jetpack` 明确要求 **BepInEx 5 x86**，采用 .NET Framework 3.5 项目并引用游戏的 `Assembly-CSharp.dll` 和 Unity DLL。
- ✅ 公开 MOD 的插件入口采用 `BaseUnityPlugin`、`[BepInPlugin]`、`[BepInProcess("Overcooked2.exe")]`，安装到 `BepInEx/plugins/`。
- ✅ `hpmv` 的游戏代码研究文档公开了投掷/接住/订单流转/烹饪状态/角色控制的候选方法。
- ✅ Microsoft 的 `XInputSetState` 可设置用户索引 0–3 的左右震动电机强度，`XInputGetState` 可检测连接状态。

### 2.2 必须现场确认的事实

- 🔎 目标游戏发行渠道、具体版本、进程名、x86/x64 位数、Unity/Mono 类型；优先沿用已验证的 BepInEx 5 x86 方案，不能因为系统是 64 位就使用 x64 插件。
- 🔎 游戏程序集和 Mono/.NET 目标框架的实际兼容性；优先参考 Jetpack 的 `net35` 项目，若实际环境不兼容则记录依据并调整。
- 🔎 反编译后方法的真实签名、所在类型、调用频率、当前版本是否已变更。
- 🔎 在线客户端能否收到对应事件，以及事件内能否稳定定位当前本地玩家。
- 🔎 Steam Input、2.4G 接收器、不同手柄模式下的 XInput 槽位关系。**XInput 槽位并不天然等于游戏里第 N 个玩家。**
- 🔎 XInput 震动是否被游戏、驱动、Steam Input 或第三方映射软件覆盖；冲突时需采用可观测降级。

### 2.3 推荐技术栈

| 子系统 | 默认方案 | 理由 |
|---|---|---|
| 游戏插件加载 | BepInEx 5（按游戏位数匹配，既有项目为 x86） | 已有实际 MOD 先例 |
| 游戏事件挂钩 | Harmony / HarmonyX（使用随 BepInEx 提供的兼容程序集） | 不直接修改游戏 EXE |
| 开发语言 | C# / .NET Framework（优先与现有 OC2 MOD 对齐） | Unity Mono 插件兼容 |
| 手柄震动 | Win32 XInput，`XInputSetState` P/Invoke | 无需外部 Python 常驻进程 |
| 配置 | BepInEx `ConfigEntry<T>` | 安装后可修改 cfg |
| 日志 | BepInEx Logger | 便于定位 Hook 与硬件问题 |
| 反编译工具 | dnSpyEx 或 ILSpy | 验证游戏程序集和字段 |
| 自动化测试 | 可拆分的纯 C# 核心逻辑 + 假手柄后端 | 不必每次启动游戏就能测试调度器 |

可选后续工具：Python/PyQt6 配置编辑器（非 MVP）、硬件厂商 SDK（仅在确有文档和授权时接入）。

## 3. 总体架构

```text
Overcooked2.exe / Unity Mono
    |
    +-- BepInEx 插件入口 (Plugin)
          |
          +-- HookRegistry：检测/安装/卸载可选 Harmony Patch
          |       |
          |       +-- ClientActionHooks（本地玩家的投掷/交互/冲刺等）
          |       +-- ClientFlowHooks（订单成功/失败/超时）
          |       +-- CookStateHooks（烹饪状态变化）
          |
          +-- EventNormalizer：标准化事件、去重、标注作用范围
          |       |
          |       +-- PlayerResolver：游戏角色 → 本地玩家 → 手柄槽位
          |       +-- WarningStateTracker：阈值监视与进入/离开状态
          |
          +-- HapticEngine：优先级/限频/合成/暂停/超时
          |       |
          |       +-- IControllerRumbleBackend
          |                 +-- XInputBackend（首发）
          |                 +-- FakeBackend（测试）
          |
          +-- Config / Diagnostics / Logging
```

### 3.1 严格分层要求

- Hook 中仅采集最少量的信息并发送规范化事件，**不得** `Thread.Sleep`、阻塞网络、频繁查找场景全对象、每帧反射扫描或直接安排长时间马达震动。
- HapticEngine **不依赖 Unity 游戏类**，可在不启动游戏时单元测试。
- GameEvent 统一字段：`EventType`, `Timestamp`, `PlayerIdentity?`, `TeamIdentity?`, `EntityIdentity?`, `Scope`, `SourceHook`, `Confidence`。
- `Scope` 用 `LocalPlayer`、`LocalTeam`、`AllLocal`、`Unknown`；遇到未知玩家映射的动作事件默认不广播全部手柄。
- 所有游戏 Hook 都可独立失败和关闭；某个 Patch 安装失败不应使整个插件崩溃。

## 4. 真实游戏事件：研究与 Hook 策略

**重要：下列函数是公开逆向文档提到的“候选方法”，不是在本机游戏中已经测试通过的现成 API。AI 必须逐个反编译并在日志中证明触发。**

| 目标事件 | 候选位置 | 触发条件与风险 | 建议阶段 |
|---|---|---|---|
| 投掷意图 | `ClientPlayerControlsImpl_Default.Update_Throw` | 客户端更容易定位本地玩家，但按键意图不保证投出 | V0.1 探针 |
| 服务端收到投掷事件 | `ServerPlayerControlsImpl_Default.ReceiveThrowEvent(GameObject)` | 可能包含失败请求；在线客机不一定执行 | V0.1 对照 |
| 真实投出物品 | `ServerThrowableItem.HandleThrow(IThrower, Vector2)` | 接近“成功投掷”，但不一定能直接找到正确本地操作者 | V0.1 对照 |
| 物品开始飞行 | `ClientThrowableItem.StartFlight(IClientThrower)` | 客户端视觉状态候选，需避免场上其他玩家投掷导致自己震动 | V0.1 备选 |
| 厨师接住物品 | `ServerAttachmentCatcher.HandleCatch(ICatchable, Vector2)` | 接住动作的候选；在线客机和玩家映射要验证 | V0.2 |
| 上菜成功 | `ClientKitchenFlowControllerBase.OnSuccessfulDelivery(...)` | 客户端全局事件，适合所有本地玩家提示 | V0.2 |
| 上菜失败 | `ClientKitchenFlowControllerBase.OnFailedDelivery(...)` | 客户端全局事件 | V0.2 |
| 订单超时 | `ClientKitchenFlowControllerBase.OnOrderExpired(...)` 或订单控制器事件 | 单次事件；需避免多层重复 Hook | V0.2 |
| 烹饪进度/完成/烧焦 | `ServerCookingHandler.SetCookingProgress(float)` | 高频调用；必须按状态边沿触发，在线客户端优先寻找同步后的客户端事件 | V0.3 |
| 捡起/放下物品 | `ClientPlayerControlsImpl_Default` 对应客户端交互/拾取路径（具体签名待查） | 不要把按键当作成功动作 | V0.3 |
| 冲刺/碰撞/坠落 | 客户端角色控制和受击状态（具体签名待查） | 动作频繁，需要严格限频 | V0.3 |
| 即将过期/即将烧焦 | 客户端订单 GUI 计时状态、客户端烹饪同步状态（具体字段待查） | 属于“状态阈值”，不能凭空声称存在预警事件 | V0.3+ |
| 真正起火/灭火 | 火焰实体或状态/特效组件（待定位） | 必须先发现稳定的状态来源 | 可选 |

### 4.1 Hook 的具体研究工作流

1. 备份可正常运行的原版游戏和 BepInEx 配置，记录游戏 exe 哈希、游戏版本/分支、插件版本和依赖版本。
2. 使用 dnSpyEx/ILSpy 打开 `Overcooked2_Data/Managed/Assembly-CSharp.dll`；核对表中每个候选类及方法是否真实存在。
3. 建立 `docs/hook-research.md`，每条记录：类型、方法名、参数、返回值、访问权限、触发方（Client/Server/Both）、证据、误触发风险、是否跨联机客机。
4. 第一次只安装 **一个** `Postfix` 探针，通过日志记录关键事件及本地玩家身份。不要同时给 10 个方法打 Patch。
5. 对照人工动作测试：成功投掷、失败投掷、其他玩家投掷、单机、双本地玩家、在线客机；制作表格逐项标记。
6. 优先把 Hook 放在事件成功发生后的状态转换处；如果只能监听动作意图，必须将其标记为“预测反馈”，与“确认反馈”区分。
7. 网络游戏里优先使用客户端已收到的事件；仅服务端存在的路径不能直接作为所有玩家的默认实现。
8. 每个 Patch 进行 `try/catch`、调用耗时控制及禁用开关；Patch 查找失败应记日志后跳过，不允许中止加载。

### 4.2 Hook 选择与重复调用

- 同一个逻辑事件可能同时触发客户端预测、服务端确认和状态同步。定义去重键：`EventType + PlayerIdentity + EntityIdentity + 短时间窗口`，缺少 ID 时采用保守的节流策略。
- “成功上菜”与“订单超时”属于队伍级事件，可向所有本地手柄广播；“投掷”只应震动真正操作的手柄。
- 如果客户端无法可靠定位投掷者，**宁可暂时关闭该事件**，也不要让 4 个玩家一起震动。
- 事件 Hook 只读，避免修改游戏消息、返回值、操作逻辑；除非确有原因，不使用 Prefix 拦截原调用。

## 5. XInput 震动后端

### 5.1 基础调用契约

- 使用 `XInputSetState(userIndex, ref XINPUT_VIBRATION)`，`userIndex` 范围 0–3。
- 电机值为 `ushort`，范围 0–65535；配置中使用统一的 0.0–1.0 强度，再换算为 16 位整数。
- 左侧（低频/较强体感）与右侧（高频/较细体感）分别设置，不保证手柄真实物理布局与 Xbox 完全一致。
- 使用 `XInputGetState` 或 `XInputSetState` 返回值判定连接，`0` 表示成功；设备不在线要停止对其继续输出。
- 首先使用系统的 `xinput1_4.dll`（Windows 10/11），不主动向游戏目录放置“同名代理 DLL”。

### 5.2 C# P/Invoke 参考（可以直接作为后端起点）

```csharp
using System;
using System.Runtime.InteropServices;

internal static class XInputNative
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct XINPUT_VIBRATION
    {
        public ushort wLeftMotorSpeed;
        public ushort wRightMotorSpeed;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
    private static extern uint XInputSetState(uint userIndex, ref XINPUT_VIBRATION vibration);

    internal static bool SetMotors(uint userIndex, float left, float right)
    {
        if (userIndex > 3) return false;
        left = Math.Max(0f, Math.Min(1f, left));
        right = Math.Max(0f, Math.Min(1f, right));
        var vibration = new XINPUT_VIBRATION
        {
            wLeftMotorSpeed = (ushort)Math.Round(left * 65535f),
            wRightMotorSpeed = (ushort)Math.Round(right * 65535f)
        };
        try { return XInputSetState(userIndex, ref vibration) == 0; }
        catch (DllNotFoundException) { return false; }
        catch (EntryPointNotFoundException) { return false; }
    }

    internal static void Stop(uint userIndex) { SetMotors(userIndex, 0f, 0f); }
}
```

**使用限制：** 以上代码是底层接口示例，不包含调度器、线程安全、断连处理及游戏内人物映射；AI 不得把它当作完整可用的游戏 MOD。

### 5.3 设备初始化与检测

- `Awake` 初始化配置和日志，不立即在所有手柄上运行震动测试。
- 游戏内提供手动测试热键（初期如 F8）和单手柄 80ms 轻震；在菜单、输入界面或热键被占用时可改配置关闭。
- 每次连接状态变化记录一次日志，避免每帧刷日志；空槽位按一定间隔（例如 1 秒）轮询。
- Windows/Steam Input/手柄驱动可能提供虚拟 XInput 设备。设备编号由系统分配，不应当写死为“玩家1永远使用索引0”。
- 游戏退出、插件销毁、切换到主菜单、设备映射改变、异常处理路径，都应输出 `(0,0)` 停震。

## 6. 多人/本地/在线映射策略

### 6.1 首发行为：只在“知道是谁”时精确震动

需分清三个概念：**游戏网络玩家 ID**、**本机游戏操作槽位（ControlPadInput / Local Pad）**、**Windows XInput 设备索引**。三者可能不一致。不可使用玩家颜色、角色位置或列表顺序盲猜。

映射过程：

1. 研究 `PlayerIDProvider`、`ClientPlayerControlsImpl_Default` 的 `m_controlsPlayer` / `m_playerIDProvider` / `m_controlScheme`，定位本地厨师与输入槽位的关联。
2. 通过日志打印玩家加入、退出、输入控制类型、角色实体 ID、游戏本地 Pad ID；不打印个人账号、好友等敏感信息。
3. 映射游戏槽位到 XInput 槽位：**优先明确 API 关系；没有可靠 API 时，提供手动绑定**。
4. 手动绑定方案：用户在游戏配置界面选择“玩家 N → XInput 槽位 X”，分别使用“测试震动”确认；不做自动推断的隐藏操作。
5. 键盘玩家不输出 XInput 震动。单人切换两位厨师时，同一实体/玩家的控制权变化必须单独验证。
6. 在线客机只驱动该客机本地连接的手柄，绝不通过网络向其他人的手柄发送震动命令。

### 6.2 联机兼容验证矩阵

| 场景 | 预期行为 |
|---|---|
| 单机一手柄 | 自己投掷/接住仅自己震动 |
| 单机键盘 | 不发送震动，原游戏操作正常 |
| 本地双手柄 | 玩家 1/2 动作对应各自设备 |
| 一键盘一手柄 | 仅手柄玩家收到个人动作反馈 |
| 在线房主 | 本机手柄能接到本机动作与队伍事件 |
| 在线客机 | 即使房主没装 MOD，也尽可能让本地客户端事件生效 |
| Steam Remote Play Together | 先验证设备/虚拟槽位，不能预先保证映射 |
| 大量 MOD 共存 | 不因其他 MOD 修改某个 Hook 而让游戏崩溃 |
| 两个控制程序同时震动 | 要有冲突诊断，说明最后写入设备值可能覆盖彼此 |

## 7. 震动设计规范（默认建议，可全部调整）

采用统一字段：`enabled`, `leftIntensity`, `rightIntensity`, `durationMs`, `priority`, `cooldownMs`, `scope`, `pattern`。以下强度是相对值（0.0–1.0），具体手柄感觉需实测调整。**所有强度和时间均为建议值，不是实际游戏标准。**

| 事件 | 默认反馈例子 | 优先级 | 初始状态 |
|---|---|---:|---|
| F8 测试 | 左 0.22 / 右 0.28 × 80ms | 10 | V0.1 开 |
| 真实投掷成功 | 左 0.15 / 右 0.32 × 65ms | 20 | V0.1 开 |
| 接住飞来的物品 | 左 0.25 / 右 0.18 × 90ms | 25 | 验证后开 |
| 拾取食材 | 右 0.10 × 40ms | 8 | 默认关 |
| 放下食材 | 左 0.10 × 40ms | 8 | 默认关 |
| 切菜每次有效进度 | 右 0.08 × 30ms，最短冷却 160ms | 8 | 默认关 |
| 切菜完成 | 双马达 0.22 × 90ms | 18 | 验证后开 |
| 冲刺 | 左 0.14 / 右 0.22 × 70ms | 12 | 默认关 |
| 与角色/障碍碰撞 | 左 0.28 × 90ms | 22 | 默认关 |
| 食物完成烹饪 | 右 0.23 × 100ms | 25 | 验证后开 |
| 即将烧焦 | 每 0.8 秒轻震 65ms，随风险缩短至 0.4 秒 | 60 | 验证后开 |
| 锅内食物烧焦 | 左 0.45 / 右 0.35 × 200ms | 80 | 验证后开 |
| 场景出现火焰 | 双马达 0.55 × 250ms，至多一次/状态进入 | 90 | 默认关 |
| 成功上菜 | 双震：右 0.20 × 70ms，间隔 80ms，再右 0.24 × 70ms | 30 | V0.2 开 |
| 上菜失败 | 左 0.34 × 220ms | 55 | V0.2 开 |
| 订单剩余 10 秒 | 右 0.25 × 100ms，单次提示 | 50 | 验证后开 |
| 订单剩余 5 秒 | 右 0.35 × 80ms 两次 | 70 | 验证后开 |
| 订单已过期 | 左 0.40 / 右 0.30 × 250ms | 85 | V0.2 开 |
| 关卡最后 10 秒 | 低频 0.20 × 80ms，每秒最多一次 | 45 | 默认关 |
| 本局结束/成功 | 双马达短促三连震 | 35 | 默认关 |

### 7.1 震动品质规则

- **短、轻、明确**：默认操作反馈不超过 120ms；连续切菜不能形成马达常开。
- **有辨识度**：危险采用明显的低频节奏，成功用短双震，失败用较长单震，不靠一味提高强度。
- **不打扰**：菜单、载入、暂停时停止震动；普通操作的默认总强度可设置 0.5–0.8 全局倍率。
- **可关闭**：有“总开关”“动作反馈”“危险提醒”“订单提醒”“振动强度”“低震动模式”设置。
- **避免误报**：只对“即将烧焦”等已能稳定判断的状态发警告；先完成准确性，再优化触感。

## 8. HapticEngine 震动调度器（关键模块）

### 8.1 行为要求

1. 以结构化 `HapticPattern` 代表多个片段，如 `[(left,right,durationMs),(0,0,gapMs), ...]`。
2. `Trigger(event)` 按映射选择手柄与模式，添加进待执行队列；不在 Hook 方法内睡眠/等待。
3. 每个手柄单独维护当前片段、结束时间、最小冷却、最近事件去重键及安全停止时间。
4. 普通事件可丢弃或被更高优先级事件打断；高优先级危险覆盖普通动作震动。
5. 默认**不对所有事件强度直接相加**（容易饱和），可使用“最高优先级独占 + 同级延后/合并”的简单策略；合成模式仅作为后续选项。
6. 同一事件短时间重复只触发一次；同一锅/订单阈值使用状态边沿 `False→True`，不得每帧触发。
7. 各手柄按一定刷新节奏（建议 20–50Hz 或在输出变化时）下发马达值；若状态未变化不必频繁调用。
8. 每次震动总长度有上限（例如 500ms 操作效果；危险脉冲模式也有单脉冲上限），防止异常持续震动。
9. 使用单调时间或 Unity 非缩放实时钟实现持续时间；暂停时显式清空，不依赖 `Time.timeScale` 继续推进。
10. 禁用/退出/崩溃可控路径/切关卡/手柄断连必须主动归零；异常关闭无法保证终止设备残留状态，需记录限制。

### 8.2 推荐优先级

`紧急火灾(90) > 订单过期(85) > 食物烧焦(80) > 临期提醒(60-70) > 上菜结果(30-55) > 接住/投掷(20-25) > 拾取/切菜(8)`。

**规则示例：** 正在切菜时进入烧焦预警：立即结束当前切菜震动并执行危险模式；预警解除后不要补播放过期的切菜事件。

### 8.3 状态跟踪

构建 `WarningStateTracker`，维护 `entityId → currentWarningLevel` 与 `orderId → lastWarningThreshold`，支持事件：`Enter`, `Escalate`, `Recover`, `Remove`。离开关卡或订单完成必须清理缓存。若无稳定实体 ID，优先关闭对应预警而非每帧误震。

## 9. 推荐工程目录

```text
OC2Haptic/
├── OC2Haptic.sln
├── src/
│   ├── OC2Haptic.Plugin/
│   │   ├── HapticPlugin.cs
│   │   ├── Hooks/
│   │   │   ├── HookRegistry.cs
│   │   │   ├── ThrowHooks.cs
│   │   │   ├── CatchHooks.cs
│   │   │   ├── DeliveryHooks.cs
│   │   │   └── CookingHooks.cs
│   │   ├── GameEvents/
│   │   │   ├── EventNormalizer.cs
│   │   │   ├── PlayerResolver.cs
│   │   │   └── WarningStateTracker.cs
│   │   └── Config/HapticConfig.cs
│   ├── OC2Haptic.Core/
│   │   ├── GameEvent.cs
│   │   ├── HapticPattern.cs
│   │   ├── HapticEngine.cs
│   │   └── IControllerRumbleBackend.cs
│   └── OC2Haptic.XInput/
│       └── XInputBackend.cs
├── tests/
│   └── OC2Haptic.Core.Tests/
├── docs/
│   ├── hook-research.md
│   ├── compatibility-matrix.md
│   ├── test-report.md
│   └── decisions.md
├── scripts/
│   ├── build.ps1
│   └── package.ps1
├── README.md
├── CHANGELOG.md
└── LICENSE（待选择）
```

根据 .NET Framework 3.5 与测试框架的兼容情况，可以将纯核心模块的源代码复用到现代 .NET 测试项目，而不是强行让所有项目使用同一目标框架。避免多余分层：V0.1 可以合并为单个插件工程 + 核心类，规模变大再拆分。

## 10. 配置文件设计

配置由 BepInEx Config.Bind 生成。下面是**建议的配置项示意**，字段名可根据实现调整，但功能语义需要保留：

```ini
[General]
Enabled = true
MasterIntensity = 0.75
LowVibrationMode = false
PauseStopsRumble = true
DebugLogEvents = false

[Devices]
BindingMode = ManualOrVerifiedAuto
LocalPlayer1XInputIndex = 0
LocalPlayer2XInputIndex = -1
LocalPlayer3XInputIndex = -1
LocalPlayer4XInputIndex = -1
AllowTeamEventsAllLocal = true

[Events]
ThrowSuccess = true
CatchSuccess = true
DeliverySuccess = true
DeliveryFailure = true
OrderExpired = true
Pickup = false
Drop = false
ChopTick = false
BurningWarning = true

[Safety]
MinActionCooldownMs = 120
MaxSinglePulseMs = 500
MaxMasterIntensity = 1.0
StopOnSceneChange = true

[Hotkeys]
TestRumble = F8
ToggleMod = F9
```

进一步增强：每个事件能单独调整强度、持续时间、节奏；配置热重载；用户预设“轻量、标准、沉浸、仅警告”；在游戏内使用简单 IMGUI 配置页（不要阻碍操作），最终再研究设置菜单整合。

## 11. 具体实施阶段（按顺序推进，禁止跳过关键验证）

### 阶段 A：环境审查与最小插件

**任务：**
1. 确认游戏位置、正版可访问程序集、版本号、exe 位数；检视已有 BepInEx 和其他插件，避免覆盖文件。
2. 创建独立仓库/工作目录；添加 `.gitignore`，不提交原版游戏的专有 DLL。
3. 编译一个 `[BepInPlugin]` DLL，能在游戏主菜单写入 `OC2Haptic initialized` 日志。
4. F8 震动测试：用 `XInputSetState` 震动一个明确选定的设备 80ms，并由 `Update()` 的调度器归零。
5. 实现禁用与退出停止震动；打包测试版安装/卸载说明。

**验收：** DLL 实际加载；F8 可以按配置触发震动；长按不导致无限震动；没插手柄时不崩溃。若没有游戏环境，标为 `编译通过 / 尚未实机验证`，不可声称已成功安装。

### 阶段 B：真实事件探针与投掷 MVP

**任务：**
1. 对候选 Throw Hook 逐个验证签名与调用；记录“按下投掷键但没有物品”“拿了不能投掷的物品”“成功投掷”“别人投掷”四种情形。
2. 确定优先的成功投掷 Hook；确认其可关联的本地玩家身份。
3. 标准化 `ThrowSuccess` 事件，设置 65ms 震动与 120ms 冷却。
4. 构建 FakeBackend 单元测试：高频事件去重、设备断连、优先级覆盖、停止归零。

**验收：** 单机一手柄实际投出时震动；空手按键不误震；日志中的事件数量与实际次数相符（或记录仍需修复的偏差）。

### 阶段 C：上菜及接住

**任务：**
1. 验证 `ClientKitchenFlowControllerBase` 的成功/失败/订单过期事件，并只选一个不重复的 Hook 层级。
2. 接住事件优先验证 `ServerAttachmentCatcher.HandleCatch`，联机客机无执行路径时寻找对应客户端实体/消息事件。
3. 完成触觉双震、长震和去重行为；遇到无法识别的个人操作则保守降级。

**验收：** 上菜成功、错误上菜、订单超时对应准确震动；仅个人动作属于个人手柄；全局事件可按配置广播给本地手柄。

### 阶段 D：危险提醒

**任务：**
1. 研究 `ServerCookingHandler.SetCookingProgress` 的同步模型，以及客户端可观察的 cooking state、订单倒计时来源。
2. 用状态边沿检测 `Completed`、`OverDoing`、`Ruined` 或当前版本对应状态；不要每帧提交提醒。
3. 先支持“真实变成已烧焦”“订单已超时”后再增加“提前 10/5 秒”阈值。
4. 做多锅、多订单、取消/完成、游戏暂停、切场景和状态丢失处理。

**验收：** 预警不因重复刷新而连震；发生真实风险能提示；状态解除立即停止；多锅同时处于危险时限频正确。

### 阶段 E：多人和联机

**任务：**
1. 研究本地游戏 Pad 与 XInput 索引的可靠映射；实现手动映射和逐台测试功能。
2. 验证双手柄、键盘+手柄、本地多人、房主、客机及 Steam Input。
3. 对无法完成绑定的个人事件关闭震动，对队伍级事件可配置广播。
4. 增加版本/兼容检测，遇到无法找到 Hook 时跳过该功能并显示日志诊断。

**验收：** 任一玩家投掷只震对应设备；不存在意外操控或跨用户震动；在线客机即使无法支持所有功能也稳定运行且不破坏原版游戏。

### 阶段 F：可发布版本

**任务：**
1. 发布版默认关闭冗长日志和实验性 Hook；检查对游戏 FPS/帧时间影响。
2. 整理 README、事件状态、配置项、使用演示、已知问题、卸载步骤与许可证。
3. 提供 ZIP 包：`OC2Haptic.dll` + 配置示例 + 说明；若附带第三方库必须核对许可与再分发条件。
4. 将“经过实机验证的平台/版本”和“未经测试的平台/版本”分开展示。

**验收：** 用干净的游戏副本复现安装；不用开发工具即可启用/禁用；删除本插件文件后游戏恢复原状。

## 12. 测试策略与验收清单

### 12.1 自动化单元测试（不依赖 Unity）

- [ ] `Trigger(Throw)` 输出一个短脉冲，并在规定时刻归零。
- [ ] 同一动作 20ms 内连续出现 3 次，只输出一次（可配置去重间隔）。
- [ ] 火灾事件打断正在执行的普通操作震动，普通事件不抢占火灾。
- [ ] 高优先级事件停止后不回放已经过期的普通事件。
- [ ] 暂停、切换场景、禁用插件、结束、设备断连时状态归零。
- [ ] 对 XInput 索引 -1 或 4 的请求不会发送实际指令。
- [ ] 浮点配置越界、NaN/Infinity、负持续时间、超长持续时间均被拒绝或安全钳制。
- [ ] 全局强度倍率为 0 时完全无震动。
- [ ] 动作事件缺失玩家映射时不会广播给所有本地手柄。

### 12.2 人工实机测试

- [ ] Windows 10 / Windows 11；Steam 版优先。
- [ ] 黑武士 5 Pro 2.4G XInput 模式，记录实际设备名称和固件/驱动版本。
- [ ] 一手柄、两手柄、一键盘一手柄；拔插设备与切换手柄模式。
- [ ] 空手投掷、投掷成功、被挡住投掷、别人投掷、接住食材。
- [ ] 上菜成功、错误上菜、订单超时、烹饪完成/过度/烧焦。
- [ ] 暂停/恢复/主菜单/关卡结束/加载界面/游戏退出。
- [ ] Steam Input 开与关（如果关掉会导致手柄无法用，则记录为不适用）。
- [ ] 同时装入其他已有 BepInEx MOD；确认 Hook 冲突不会导致进程崩溃。
- [ ] 房主与客机的事件覆盖率分别记录，不使用“联机全部支持”这样的未经验证结论。

### 12.3 建议性能目标（非保证）

- 常态 Hook 快速返回，避免 Unity 主线程出现可感知卡顿。
- Hook 本身不分配大量对象，不按帧打印日志。
- 动作至感知震动：目标尽量低于 100ms；用录像或日志测量，不编造结果。
- 如性能有问题，优先减少反射和重复 XInputSetState 调用；不牺牲事件准确性强行追求刷新率。

## 13. 故障与保护机制

| 问题 | 必须处理的行为 |
|---|---|
| 找不到游戏方法 | Hook 跳过并标记该事件 `Unavailable`，其他功能可继续 |
| 某个 Harmony Patch 报错 | 捕获并日志记录；不吞掉导致原版逻辑破坏的错误 |
| 找不到 XInput DLL | 禁用震动输出但游戏和插件不崩溃 |
| 手柄断连 | 清除任务、尝试停震、低频重连检测 |
| 接入多个手柄 | 不凭 XInput 索引猜测厨师角色，默认等待绑定 |
| 其他程序控制震动 | 提供排查提示；不要注入驱动、静默关闭其他程序 |
| 游戏刚结束或加载中 | 停止震动并清空上一关所有预警状态 |
| 配置损坏 | 回退安全默认值，记录配置加载错误 |
| 震动任务异常 | 单次脉冲硬上限、全局禁用后统一归零 |
| 安装/更新失败 | 保持原版文件不变，可通过删除本插件一键回滚 |

## 14. 后续可选增强模块（全部非首发，按价值挑选）

### 14.1 高性价比功能（优先建议）

1. **游戏内轻量设置页**：总开关、单事件开关、强度滑杆、当前手柄、测试按钮。建议使用简洁游戏内 UI，不要一上来开发独立窗口软件。
2. **手柄自动绑定助手**：逐一确认设备，让玩家触发测试操作/按钮，再建立 `LocalPad → XInput` 映射，明确区分推断和人工确认。
3. **仅预警模式**：关闭切菜/投掷等高频触感，仅保留烧焦、订单过期、火灾等危险。
4. **震动模式预设**：轻量、标准、沉浸、仅警告、自定义；允许用户导入/导出简单配置。
5. **游戏内诊断面板**：当前事件、来源 Hook、匹配手柄、输出强度、最近丢弃原因，帮助发现多人映射问题。
6. **设置热重载**：可运行时调整强度，无需重新开游戏；热重载失败回退旧配置。

### 14.2 增强沉浸体验（有条件开发）

7. **方向性反馈**：依据食材从哪侧撞击、危机相对位置改变左右强度。注意普通 XInput 的左右电机并非标准左右方向空间音效，此功能需实机验证是否具有可辨识性。
8. **连续动作质感**：灭火器喷射、持续切菜、洗盘等支持低强度间歇/包络变化；必须严格限时，避免持续噪声。
9. **反馈事件学习器**：录制匿名本地事件序列用于测试、回放/调参；不记录账号、聊天或个人数据。
10. **按菜谱/场景分类**：烧烤、油炸、烤箱分别设计震动图案；只有能辨识实体种类时才启用。
11. **组合动作反馈**：投掷 + 接住连击、连续正确上菜连击等成就感反馈，属于触觉表现，不改变游戏分数。
12. **时间感增强**：关卡倒计时最后 5 秒逐渐增强触感，默认关闭以免干扰其他提醒。

### 14.3 扩展兼容（研发成本较高）

13. **多品牌设备后端**：未来新增厂商 SDK、SDL/hidapi、特定设备高级触觉；遵守 SDK 许可，切勿把 XInput 双马达误称 HD 触觉。
14. **配置可视化编辑器**：用 Python + PyQt6 做独立配置工具，可导入/导出 JSON/CFG、图形化预览震动波形；仅在 MOD 稳定后启动。
15. **用户分享震动预设**：导出文件/社区分享；加载外部预设须校验大小、时长和强度，禁止执行任意代码。
16. **其他平台/游戏版本**：Epic、macOS/Linux（Proton）、Steam Deck；不是 Windows XInput 首发支持范围，需不同后端与额外实测。
17. **与其他 MOD 联动**：可选提供轻量本地事件 API，供“投掷达人”“菜谱扩展”等 MOD 调用触觉效果；建立事件版本号，不能依赖别人内部私有实现不经验证。
18. **内置触觉编辑器**：把震动片段做成可拖动时间轴，实时测试，支持左右马达波形和优先级。适合长期维护，非 MVP。

### 14.4 不建议优先做的功能

- AI 识别游戏画面再决定震动：延迟高、误判高、难映射多人，并非首选。
- 额外 Python 监听常驻进程：增加安装复杂度和运行故障点，除非未来用于管理工具。
- 让所有动作都高频强震：会削弱危险提示的识别性。
- 所有本地手柄同步震动：对于投掷/接住会产生严重错觉。
- 默认启用网络服务或上传数据：本项目可完全本地运行，没必要增加隐私与安全风险。

## 15. 发布和兼容性要求

- 插件必须使用唯一 BepInEx GUID，比如 `dev.oc2haptic.feedback`，避免与其他人的 MOD 冲突。
- 插件自己的 `Harmony` ID 必须唯一；`OnDestroy` 时只撤销自己的 Patch，而不是撤销所有 MOD 的 Patch。
- 不打包未经允许再分发的《胡闹厨房2》游戏原始 DLL；编译时本地引用即可，设置 `Copy Local = false`。
- 遵守 BepInEx、Harmony 和参考项目各自的开源许可证。
- 不自动修改原版存档、游戏升级机制或用户 Steam 配置。
- README 包含安装、配置、测试设备、卸载、排错、已知限制和第三方来源。
- 版本号建议 `0.1.0-alpha`（震动测试与首个 Hook）、`0.2.0`（核心事件）、`0.3.0`（危险提醒）、`1.0.0`（多人基本稳定）；实际发布取决于验收结果，而非日期。

## 16. AI 编程代理执行指令（直接复制给代理）

> 你是《胡闹厨房2》触觉增强 MOD 项目的主开发代理。以本规格书为正式需求，并严格区分“已验证事实”和“待实机验证假设”。
>
> 执行方式：先检查现有工程/游戏程序集和依赖，再建立可编译的最小 BepInEx 5 插件、实现 XInput 震动测试、做 Throw Hook 探针、验证真实事件、逐步添加反馈系统。优先可运行代码、可重现测试和完整交付，不要停留在空洞的架构设计。
>
> 若当前环境没有游戏或 Windows 手柄，则完成能离线实现的全部内容：工程、逻辑、单测、构建脚本、Hook 候选研究、配置与使用文档；将实机验收明确标记为未完成，**绝不声称已在游戏内测试成功**。
>
> 开发时可以自行选择更稳定的实现细节，但不得悄悄改变“本地、只读 Hook、个人操作只震个人设备、危险提醒可关闭、故障不影响游戏”的产品原则。每阶段完成后更新 `docs/test-report.md`、`docs/hook-research.md` 和 `CHANGELOG.md`，总结验证证据与下一步。
>
> 当某个 Hook 因版本或联机限制无法工作时，不要卡死：记录失败原因，检查替代的客户端已同步事件；暂时关闭受影响功能，继续推进其他可验证模块。不得为了凑功能而把按键输入误标为成功游戏事件。
>
> 最终交付：项目全部源码、可构建 DLL（若环境支持）、ZIP 安装包、默认配置、文档、自动化测试结果、可复现的手柄/游戏实测步骤、未验证清单和已知风险。重大破坏性操作和需要用户账号或额外授权的步骤需先获得批准。

## 17. 参考资料与链接

**官方 API：**

- Microsoft — XInputSetState: https://learn.microsoft.com/en-us/windows/win32/api/xinput/nf-xinput-xinputsetstate
- Microsoft — XInputGetState: https://learn.microsoft.com/en-us/windows/win32/api/xinput/nf-xinput-xinputgetstate
- Microsoft — Getting Started With XInput: https://learn.microsoft.com/en-us/windows/win32/xinput/getting-started-with-xinput
- BepInEx 文档: https://docs.bepinex.dev/

**已公开的《胡闹厨房2》MOD 源码：**

- gua248 / Overcooked2-Jetpack（BepInEx 5 x86、构建参照）：https://github.com/gua248/Overcooked2-Jetpack
- gua248 / Overcooked2-DIYChef（BepInPlugin + Harmony 入口）：https://github.com/gua248/Overcooked2-DIYChef
- hpmv / overcooked-supercharged（游戏状态与 TAS）：https://github.com/hpmv/overcooked-supercharged
- toasterparty / oc2-modding（通用改动框架）：https://github.com/toasterparty/oc2-modding

**游戏内部逻辑研究：**

- 技术文档目录：https://hpmv.dev/docs/tech/
- 投掷与接住：https://hpmv.dev/docs/tech/throwing/
- 订单、上菜与失效：https://hpmv.dev/docs/tech/kitchen_flow_controller/
- 烹饪状态：https://hpmv.dev/docs/tech/cooking/
- 角色控制：https://hpmv.dev/docs/tech/controls/

**原生震动讨论：**

- Team17 Steam 社区答复：https://steamcommunity.com/app/728880/discussions/1/632318982807161167/

---

## 18. 给项目负责人的选择建议

**第一优先级（必须完成）**：BepInEx 最小插件、XInput 测试、真实投掷事件、停震保护、可追踪日志。  
**第二优先级（推荐加入）**：接住食材、上菜结果、订单过期、低震动模式与配置文件。  
**第三优先级（根据测试决定）**：烧焦提前预警、多手柄正确绑定、在线联机兼容。  
**后续按兴趣添加**：游戏内设置 UI、连击震动、方向感、高级手柄特性、波形编辑器、第三方 MOD 联动。

该排序是为了优先得到一款“实际可玩、不会乱震、容易卸载”的 MOD，而不是一开始堆砌功能。
