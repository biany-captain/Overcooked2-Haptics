# 《胡闹厨房2》手柄触觉增强 MOD：AI 开发执行规格书

> 项目暂定名：OC2 Haptic / Overcooked! 2 Haptic Feedback MOD  
> 文档版本：v1.3（2026-11-21；新增庆祝型实时星级达标、震动＋清脆奖励音效 65% 候选配置及触觉辨识度改进）  
> 目标平台：Windows PC 版《Overcooked! 2》（优先验证 Steam 版）；手柄优先支持 XInput（Xbox 兼容模式）。  
> 文档用途：直接交给 Codex、Claude Code 等 AI 编程代理作为项目目标、分阶段实施要求和验收依据。  
> 状态约定：✅ 有公开资料依据；🔎 需结合当前游戏版本反编译／运行确认；💡 后续选配设计，非已实现功能。

## 1. 项目背景、目标与原则

### 1.1 背景

《Overcooked! 2》本身通常不提供手柄震动反馈。Team17 在 Steam 讨论中回应过：未提供震动是有意设计。此项目增加一个独立的、非侵入式的触觉增强 MOD，让投掷、接取、切菜、交付订单、烹饪危险和时间警告产生易辨识的震动。目标是提升操作反馈及危险察觉，而不是改变游戏规则。

### 1.2 核心目标

1. 在不改动原始游戏逻辑、数值、网络协议及存档的前提下，监听真实的游戏事件并输出震动。
2. V0.1 支持单机/本地单手柄：一键测试震动 + 至少一种已验证的真实游戏事件；再逐步扩展到投掷、接住、上菜（含顺序/非顺序成功上菜辨识、Combo 等级震动）、订单失效。
3. V0.2 引入燃烧与订单风险提醒，支持可调强度、冷却时间和优先级。
4. V1.0 目标支持 1–4 个本地手柄和基本在线联机模式；仅对本地实际连接的手柄震动。
5. 支持其他 BepInEx MOD 共存，失效时不影响游戏继续运行。
6. 提供可修改的配置文件、完整日志、安装卸载步骤及自动/手动测试记录。
7. **v1.3 需求：** 庆祝型 1～4 星实时分数门槛里程碑反馈（游戏允许的星级范围内），初始候选配置“震动＋音效、MOD 音量 65%、清脆奖励音”；并改善各事件震动辨识度。详细设计以 §19 为准，未实机验证不等于已实现。

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
          |       +-- ScoreStateHooks（关卡分数/星级目标读取：只读且先验证）
          |
          +-- EventNormalizer：标准化事件、去重、标注作用范围
          |       |
          |       +-- PlayerResolver：游戏角色 → 本地玩家 → 手柄槽位
          |       +-- WarningStateTracker：阈值监视与进入/离开状态
          |       +-- ComboTracker：分队伍记录连续上菜、权威等级与同步状态
          |       +-- StarMilestoneTracker：关卡阈值/首次达成/会话去重
          |
          +-- HapticEngine：优先级/限频/合成/暂停/超时
          |       |
          |       +-- IControllerRumbleBackend
          |                 +-- XInputBackend（首发）
          |                 +-- FakeBackend（测试）
          |
          +-- CelebrationCoordinator：星级与同一次上菜触觉冲突仲裁
          |       +-- IRewardAudioBackend → UnityAudioBackend（游戏主线程，2D 音效）
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
| 顺序/非顺序成功上菜及 Combo 等级 | `ClientKitchenFlowControllerBase.OnSuccessfulDelivery(TeamID, OrderID, float, int, bool, ClientPlateStation)`；另核查游戏 Combo 状态/计分同步 | `_wasCombo` 仅为布尔候选判据，不含数值倍率；须验证首单、重名菜谱、连击中断、真实 Combo 等级及在线客机 | V0.2 |
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


### 4.3 新增：顺序上菜 / 非顺序上菜差异化事件（v1.1 必选需求）

**产品需求：** 成功交付订单后，根据**是否延续按顺序上菜的连击**输出明显不同的震动；“上菜失败”保持第三种独立震动。玩家应能凭触觉区分“保持节奏”和“连击中断”，且本 MOD **只反馈，不改变分数、订单顺序、Combo 或小费**。

**纠正一个容易误解的机制：** “非顺序上菜”不等于绝对没有任何小费。游戏存在订单剩余时间相关的小费和连续按顺序交付带来的倍数加成；乱序可能导致 Combo/小费倍率中断。**禁止直接使用 `tip > 0` 判断是否顺序交付**。

**优先候选 Hook（现有逆向资料所列，必须在当前游戏程序集核对）：**

```csharp
// 仅为目标方法签名线索，不是可直接粘贴运行的 Harmony Patch
ClientKitchenFlowControllerBase.OnSuccessfulDelivery(
    TeamID teamId,
    OrderID orderId,
    float timePropRemainingPercentage,
    int tip,
    bool wasCombo,
    ClientPlateStation station);
```

- `wasCombo`：游戏向客户端传递的该次上菜是否计入 Combo 的结果，优先用作**已确认的连击状态**；不要未经实测就假设它精确等价于“玩家投出的餐盘是否对应屏幕最左边订单”，尤其在首单及连击刚重置时。
- `tip`：本次交付相关小费数值，可用于调试或后续触觉等级设计；**不是**“是否顺序上菜”的唯一依据。
- `orderId` 与 `teamId`：同一订单/队伍内做事件去重并确定团队级震动接收者。若发生同菜谱多张订单，不能单凭菜名判断该订单是否最左。
- 源资料还指出 `ServerTeamMonitor.OnFoodDelivered(...)` 的 `out bool o_wasCombo`、`ServerOrderControllerBase.IsComboOrder(...)`，以及 `ServerOrderControllerBase.m_activeOrders` / `m_comboIndex`、`TeamScoreStats.TotalCombo` 等位置。仅作为验证/诊断参考；**在线客机优先消费已同步的客户端确认结果**，不依赖服务器独占方法。

**建议事件模型：**

```text
DeliveryConfirmed            // 仅用于逻辑中转，不直接震动
  ├── DeliveryOrdered        // 本次按顺序 / Combo 维持（需实测确证）
  ├── DeliveryOutOfOrder     // 本次非顺序 / Combo 断开（需实测确证）
  └── DeliveryUnclassified   // 暂无法确信分类：采用低强度通用成功震动或不震
DeliveryFailed               // 真正交付失败：独立于乱序成功上菜
```

**分类优先级：** 首先检验游戏传入的 `wasCombo`、连击状态及相应的订单 ID；必要时结合“上菜前订单列表/Combo 状态”进行验证（允许短期记录，必须只读）。如发现 `wasCombo=false` 同时包含“第一单正常起始”和“乱序中断”两种情形，则必须通过**前后 Combo 状态或订单队列**进一步区分，不能强行判成乱序。无法确认时发 `DeliveryUnclassified`，避免误提示。不要依赖 UI 图像或菜品名称推断。

**初始震动建议（需提供给用户选择，可调整）：**

| 细分事件 | 建议波形（左/右为强度 0～1） | 触觉意图 | 默认等级 |
|---|---|---|---|
| `DeliveryOrdered` | 右 0.25 × 70ms → 空档 80ms → 左 0.18 / 右 0.32 × 100ms | 清脆、积极的双脉冲 | 30 |
| `DeliveryOutOfOrder` | 左 0.38 / 右 0.08 × 160ms | 沉闷的单脉冲，提醒 Combo 可能断开 | 42 |
| `DeliveryFailed` | 左 0.34 × 220ms | 更长的失败触感 | 55 |
| `DeliveryUnclassified` | 右 0.14 × 70ms（或选择关闭） | 不作正误判断的保守反馈 | 20 |

**禁止重复震动：** 对一次 `OnSuccessfulDelivery`，只输出上述**一种**细分成功事件，不得再额外发旧版 `DeliverySuccess` 通用双震。`DeliveryFailed` 不是 `DeliveryOutOfOrder`：乱序上菜仍可能成功并获得基础分/小费。

**必须验证的边界情形：**

1. 首次正常交付最左侧订单（无既有连击），避免误标为乱序。
2. 多次连续按订单顺序交付，Combo 递增时保持正确波形。
3. 跳过第一张订单，交付第二张有效订单，触发乱序波形，而不是“上菜失败”。
4. 乱序之后重新开始连续上菜，测试 Combo 恢复与判定边界。
5. 两张或多张**相同菜谱**订单同时存在：确认服务器实际匹配的 `OrderID`，不能仅按菜名判断。
6. 订单超时后重置/延长显示、临近结束交付、UI 动画仍在播放等条件，不因画面排序与逻辑排序不一致而误判。
7. 单机、本地 2 手柄、在线房主和客机：同一团队成功上菜的提示按团队广播配置处理，不能让其他团队震动。
8. 其他 MOD 修改菜谱或订单逻辑时，若官方判据不可靠，自动降级为未分类，且记录诊断信息。

**AI 的沟通义务：** 开发前须向用户提供两种基本上菜模式的演示参数以及至少一种备选波形，并询问用户选择。**用户已批准新增 Combo ×1～×4 等级触觉（见 §4.4），但具体强度/节奏和额外花样仍需在提出比较建议后由用户确认。** 额外的“连击中断特别提醒”“仅上菜反馈模式”等想法可以主动建议，未经确认不开发。

**证据来源：** https://hpmv.dev/docs/tech/kitchen_flow_controller/ （提供 `OnSuccessfulDelivery` 六参数、`wasCombo`、`tip`，以及服务端订单匹配与 Combo 计算路径）。


### 4.4 新增：连续正确上菜 Combo 等级震动（v1.2 必选需求）

**目标：** 在 §4.3 的“顺序 / 乱序成功上菜”分类基础上，增加连续正确上菜 Combo 等级的差异化触感。**只在一次成功上菜后产生一种最终震动，Combo 等级震动取代普通顺序上菜震动，而不是额外叠加。** 震动表达连击成功的成就感，不能改变游戏中的得分、小费、连击数或订单。

**游戏规则依据与实现边界：** Team17 官方解释：按接单顺序连续完成菜品，会让分数倍率上升。玩家指南显示的小费倍率通常以 ×4 为上限，但部分挑战可继续记录更长的连续成功次数。**“Combo 连续次数（streak）”与“界面或计分倍率（multiplier）”必须分别建模，不可直接认为它们永远相等。** 先按 ×1 / ×2 / ×3 / ×4 四档设计触觉，×4 之后保持 ×4 触觉，不继续提高强度；具体每档与游戏内部变量的对应关系需要实机确认。参考：https://www.team17.com/news/the-a-z-of-overcooked-2 和 https://www.playstationtrophies.org/game/overcooked-2/guide/ 。

#### 4.4.1 事件、输入和权威数据来源

```text
OnSuccessfulDelivery（单次成功交付）
      │ 读取 teamId、orderId、wasCombo、tip + 交付前/后的可靠快照
      ▼
DeliveryClassifier（§4.3，先确认顺序/乱序/未知）
      │
      ├── DeliveryOutOfOrder → ComboTracker.ResetConfirmed() → 乱序震动
      ├── DeliveryUnclassified → 不猜 Combo 等级 → 通用轻震/静默
      └── DeliveryOrdered → ComboTracker.ResolveTier()
                                  ├── 已验证 ×1 → 原顺序上菜普通震动
                                  ├── 已验证 ×2 → ComboLevel2 震动
                                  ├── 已验证 ×3 → ComboLevel3 震动
                                  ├── 已验证 ×4+ → ComboLevel4 震动
                                  └── 无法可靠得到等级 → 原顺序上菜普通震动
```

**候选观察点（必须反编译核实，不是已验证 API）：**

1. `ClientKitchenFlowControllerBase.OnSuccessfulDelivery(...)`：确认每次成功交付事件，获取 `TeamID`、`OrderID`、`wasCombo`、`tip`；`wasCombo` 是 **bool**，绝不能将 `true` 直接理解为 ×2/×3/×4。
2. `ServerOrderControllerBase.m_comboIndex`、`ServerTeamMonitor.OnFoodDelivered(... out bool o_wasCombo)`、`TeamScoreStats.TotalCombo`：作为 **服务端逻辑研究线索**。这些字段可能表示索引、总次数或累计统计，**不能未检验即解释为当前连续等级**，也不能假设在线客机能访问。
3. 优先寻找**客户端可读取的已同步、代表本次上菜后的当前 Combo 倍率**的状态；若只在服务端可见，则研究客户端 HUD/计分状态的权威数据来源，并验证与真实 UI 数值一致。
4. 如果读取时机有差异，可通过 Harmony `Prefix` 只读采集“交付前状态”，`Postfix` 读取“交付后状态”；验证事件实际触发顺序，不能在回调参数不够时擅自推断。
5. **绝不以 `tip > 0`、`wasCombo == true`、上一次正确交付时间、菜名或固定 x4 截断逻辑单独算出等级。**

**三级可信度与降级：**

| 可信度 | 获取条件 | 可输出效果 |
|---|---|---|
| `VerifiedGameState` | 当前版本验证过的客户端真实倍率或经权威源确认的 Combo 数值 | 按 ×1～×4 分级震动 |
| `ValidatedDerived` | 运行期完整观察到了本队伍所有有效交付、重置、过期和初始化事件，且已在实机对照校准 | 允许按推导等级震动，日志标明 `Derived` |
| `Unknown` | 首次进入中途局面、漏事件、换关、断线重连、同步缺失、冲突 MOD、无法证实重置语义 | 不升级等级；使用 §4.3 的普通成功/未分类波形 |

#### 4.4.2 状态机与更新流程

为每个**队伍**而不是每个手柄创建一份 `ComboState`：`SessionId`、`TeamId`、`ObservedOrderedStreak`、`VerifiedMultiplier?`、`HapticTier?`、`Confidence`、`LastDeliveryKey`、`LastValidEventTime`、`IsSynchronized`。Versus 模式不同队伍必须完全独立；本地手柄只接收所属队伍事件。

**状态转换：**

- **关卡开始 / 结束 / 重开 / 返回菜单 / 切换队伍：** 清空所有旧状态、旧事件和震动队列，新建 `SessionId`。不能将上一关的 ×4 延续到下一关。
- **有可靠等级快照：** 从游戏读取本次交付后的真实倍率，并以其确定 `HapticTier = min(max(level, 1), 4)`；同时记录原始数值，不把 `×4` 误写成“仅交付了四次”。
- **确认按顺序上菜：** 首次明确的顺序交付为正常起步；随后才可能提升等级。如果有被验证的倍数来源，直接以真实等级为准。没有时，仅当 `IsSynchronized=true` 且已验证所有会打断连击的事件时，才能由连续次数推导等级。
- **确认乱序成功上菜：** 输出 `DeliveryOutOfOrder`，重置或置为失同步，并等待下一次可确认的起点；不能同次又输出“Combo ×1”奖励。
- **订单过期或其他打断事件：** 只有实机证明会影响 Combo 时，才执行等级重置；未确认其影响时标记 `Unknown`，等待下一次权威同步，防止虚构等级。
- **上菜失败：** 单独输出失败波形；是否打断 Combo 需要实机验证，默认不自行修改连续等级。
- **连续成功超过四次：** 保留原始 `ObservedOrderedStreak`（如果可信），`HapticTier` 仍固定为 4。不能按五次、六次无限增大马达值或时长。
- **在线客机、漏包、旧事件乱序到达：** 若无法确定完整事件序列，停止本地推导，只按权威可读取值处理；没有权威数值时退化普通顺序上菜触感。

**核心流程参考伪代码（语义示例；AI 需根据实际反编译方法构造类型和 Hook）：**

```csharp
void OnDeliveryConfirmed(DeliveryPayload e)
{
    var key = e.SessionId + ":" + e.TeamId + ":" + e.OrderId;
    if (!deduplicator.TryAccept(key)) return;

    // 与 §4.3 共用同一个分类器；不得在多个 Hook 中分别震动。
    var classification = deliveryClassifier.Classify(e);
    HapticEvent output;

    if (classification == DeliveryClass.OutOfOrder)
    {
        comboTracker.OnConfirmedBreak(e.TeamId, e.SessionId);
        output = HapticEvent.DeliveryOutOfOrder;
    }
    else if (classification == DeliveryClass.Unclassified)
    {
        comboTracker.MarkUnsynchronized(e.TeamId);
        output = HapticEvent.DeliveryUnclassified;
    }
    else
    {
        // 优先读取实际游戏已确认的数值；否则仅采用经实机验证的推导值。
        var tier = comboTracker.ResolveVerifiedOrValidatedTier(e);
        output = tier == 2 ? HapticEvent.ComboLevel2
               : tier == 3 ? HapticEvent.ComboLevel3
               : tier >= 4 ? HapticEvent.ComboLevel4
               : HapticEvent.DeliveryOrdered;
    }

    hapticEngine.TriggerForLocalTeam(e.TeamId, output, key);
}
```

注意：上面的 `key` 只是示意。**如果同一关卡中 `OrderID` 可被重用，必须额外包含游戏生成的唯一交付序号或可靠时间代际**，不能永久按订单 ID 抑制后续真实交付。真实事件去重也不能因为两张不同订单在短时间完成而错误丢弃其中一张。

#### 4.4.3 默认触觉分级方案（待用户确认手感）

每档是**一套独占波形**，不是 `DeliveryOrdered` 后再播放等级特效。右马达为清脆感主导，左马达用于低频加强。所有强度都是 0～1 的建议比例；不同设备实际体验需测试。

| 结果 | 默认震动片段（左 / 右，时长；片段间归零） | 设计意图 | 事件优先级 |
|---|---|---|---:|
| 顺序起步 / 等级未知（×1 / fallback） | 右 `0.25 × 70ms` → 停 `80ms` → 左 `0.18` + 右 `0.32 × 100ms` | 保持既有正常双震 | 30 |
| Combo ×2 | 左 `0.12` + 右 `0.28 × 60ms` → 停 `55ms` → 左 `0.20` + 右 `0.38 × 110ms` | 节奏更紧、第二下有奖励感 | 32 |
| Combo ×3 | 右 `0.30 × 55ms` → 停 `50ms` → 右 `0.35 × 60ms` → 停 `50ms` → 左 `0.22` + 右 `0.43 × 80ms` | 轻快三级递进，不只是变强 | 34 |
| Combo ×4 或更高 | 右 `0.31 × 55ms` → 停 `40ms` → 右 `0.35 × 55ms` → 停 `40ms` → 右 `0.39 × 55ms` → 停 `40ms` → 左 `0.25` + 右 `0.48 × 95ms` | 有辨识度的四连击与收尾重音 | 36 |
| 乱序 / Combo 中断 | 左 `0.38` + 右 `0.08 × 160ms` | 沉闷提醒，优先级高于奖励 | 42 |
| 上菜失败 | 左 `0.34 × 220ms` | 失败警示 | 55 |

- 默认选择 `EveryOrderedDelivery`：本次顺序上菜且可信地已到 ×N，就播放对应等级**一次**；例如 ×4 之后继续正常上菜也只播放 ×4 这套，不随次数增强。
- 可选 `OnLevelUp`：仅在等级**真实提升**（×1→×2 等）时用等级波形；未升级的正确上菜用原顺序双震。具体提升判据必须可靠，不能仅比较推测的历史级别。
- 当高优先级危险事件覆盖 Combo 波形时，**终止并丢弃**被打断的剩余奖励片段；不能过几秒补播放迟到的奖励。
- `Combo` 与 `DeliveryOrdered`、`DeliveryOutOfOrder`、`DeliveryFailed` **互斥**；禁止新旧 Hook 重复提交震动。
- 连续快速交付时按**不同交付 ID** 保留真实事件计数，可按时效丢弃过期触觉，不允许为限制震动而篡改 Combo 等级。推荐奖励最长播放窗口 450ms；普通操作仍须服从危险事件优先级。
- 此表为**待实机试听的初稿，不是用户已确认的最终体验**。AI 在正式固定波形前必须提供此方案及至少一种替代风格（例如更克制的 `OnLevelUp`），主动询问用户选择。

#### 4.4.4 设备分发、配置和日志

- Combo 属于**队伍级事件**：在协作模式按 `TeamId → 本地队伍所属手柄` 发送；如果用户关闭队伍广播，可选择“本地交付者”模式，但**只有可可靠定位实际交付者时**才启用，否则提示无法使用并降级。
- 联机房主或客机**仅驱动本机手柄**；不新增联网消息，也不要求其他玩家安装本 MOD。
- 若一台电脑有两个同队手柄，二者可以同步收到队伍级奖励，但不可错误发到敌队/其他队伍；键盘玩家不对应物理震动。
- 日志在调试模式记录：`SessionId, TeamId, OrderId, wasCombo, tip, before/after gameMultiplier, derivedStreak, resolvedTier, confidence, waveform, fallbackReason, targetXInputSlots`。不要每帧输出；不记录 Steam 昵称、账号等无关信息。
- 配置详见 §10 新增的 `[Combo]`；可分别关闭分级功能、设置最大触觉等级、选择升级时才震或每单震、调整波形倍率。关闭后**仍保留 §4.3 普通顺序/乱序震动**。

#### 4.4.5 必须验收的真实情景

1. **第一单**正常按序交付：触觉属于 ×1/普通顺序，绝不能误报“乱序”或“×2”。
2. **×1 → ×2 → ×3 → ×4：** 每次实际游戏出现的倍数与日志及触觉严格一致；用录屏或可复现测试记录交付顺序。
3. **第 5、6 单继续按序：** 如游戏倍率仍显示 ×4，触觉保持 ×4，不继续增强；同时正确保存可观测的真实连续次数。
4. **中途乱序：** 本次只有乱序波形；确认重置后的下一次正确上菜以真实游戏等级重新开始，不沿用旧 ×4。
5. **订单过期 / 上菜失败：** 验证它们实际是否中断 Combo，记录不同结果；未经实测不擅自重置。
6. **两张相同菜谱、同一帧/很短时间内两次交付、订单 ID 复用：** 不重复也不漏算真实交付。
7. **在线客机加入中途 / 重连 / 换关卡：** 缺少可靠起始状态时降级普通震动，不伪造已达 ×3/×4。
8. **本地 2～4 手柄、Versus 双队伍：** 只震本机相应队伍的设备，不把一方 Combo 播到另一方手柄。
9. **火灾/烧焦提醒覆盖奖励：** 高优先级提醒立即打断 Combo 波形；之后不重放过期奖励。
10. **开启/关闭、波形模式切换：** 关闭 Combo 后仍有普通顺序交付反馈；切换配置不会残留震动。

**交付产物：** `ComboTracker`、必要的只读 Hook、可靠的数据分级来源说明、单测/实机对照记录、配置说明、失同步降级测试、`docs/hook-research.md` 中的原始游戏变量语义证据。该模块未验证前不得宣称“已准确识别游戏 Combo ×2/×3/×4”。

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
| 按顺序上菜（`DeliveryOrdered` / Combo ×1 / 未知档位） | 双震：右 0.25 × 70ms，间隔 80ms，再左 0.18/右 0.32 × 100ms | 30 | V0.2 验证后开 |
| Combo ×2 (`ComboLevel2`) | 两次递进脉冲：60ms、110ms，间隔 55ms（§4.4） | 32 | V0.2 验证后开 |
| Combo ×3 (`ComboLevel3`) | 三段轻快脉冲：55ms、60ms、80ms（§4.4） | 34 | V0.2 验证后开 |
| Combo ×4+ (`ComboLevel4`) | 四段脉冲并以 95ms 重音收尾（§4.4） | 36 | V0.2 验证后开 |
| 乱序上菜成功（`DeliveryOutOfOrder`） | 低沉单震：左 0.38/右 0.08 × 160ms | 42 | V0.2 验证后开 |
| 上菜成功但未分类（`DeliveryUnclassified`） | 右 0.14 × 70ms，允许关闭 | 20 | V0.2 保守降级 |
| 上菜失败 | 左 0.34 × 220ms | 55 | V0.2 开 |
| 订单剩余 10 秒 | 右 0.25 × 100ms，单次提示 | 50 | 验证后开 |
| 订单剩余 5 秒 | 右 0.35 × 80ms 两次 | 70 | 验证后开 |
| 订单已过期 | 左 0.40 / 右 0.30 × 250ms | 85 | V0.2 开 |
| 关卡最后 10 秒 | 低频 0.20 × 80ms，每秒最多一次 | 45 | 默认关 |
| 本局结束/成功 | 双马达短促三连震 | 35 | 默认关 |

### 7.1 震动品质规则

**v1.3 补充：** 星级庆祝模式必须呈现独立的节奏轮廓，不能仅提高马达强度；新增触觉盲测与混淆矩阵见 §19.3。星级的 4 星庆祝可在严格总时长限制下突破普通操作 ≤500ms 的限制（建议 850ms），但不得覆盖火灾、烧焦等紧急预警。

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

**v1.3 补充：** 星级奖励默认优先于同一次上菜的 Combo 普通震动，但低于紧急预警；星级反馈与该次 Combo/上菜震动采用互斥替换，普通成功事件最多允许做短时等待合并，不允许重复震动。具体算法见 §19.6。

`紧急火灾(90) > 订单过期(85) > 食物烧焦(80) > 临期提醒(60-70) > 失败/乱序上菜(42-55) > Combo 分级奖励(32-36) > 普通顺序上菜(30) > 接住/投掷(20-25) > 拾取/切菜(8)`。

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
│   │   │   ├── CookingHooks.cs
│   │   │   └── ScoreHooks.cs
│   │   ├── GameEvents/
│   │   │   ├── EventNormalizer.cs
│   │   │   ├── PlayerResolver.cs
│   │   │   ├── WarningStateTracker.cs
│   │   │   ├── ComboTracker.cs
│   │   │   └── StarMilestoneTracker.cs
│   │   ├── Audio/UnityRewardAudioBackend.cs
│   │   ├── Celebration/CelebrationCoordinator.cs
│   │   └── Config/HapticConfig.cs
│   ├── OC2Haptic.Core/
│   │   ├── GameEvent.cs
│   │   ├── HapticPattern.cs
│   │   ├── ComboState.cs
│   │   ├── HapticEngine.cs
│   │   ├── StarMilestoneState.cs
│   │   ├── CelebrationEvent.cs
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
DeliveryOrdered = true
DeliveryOutOfOrder = true
DeliveryUnclassified = true
DeliveryFailure = true
ComboLevelFeedback = true
# 旧版 DeliverySuccess 为兼容开关时需迁移到上述分类配置；不可同时触发两种成功震动
OrderExpired = true
Pickup = false
Drop = false
ChopTick = false
BurningWarning = true

[Combo]
Enabled = true
TriggerMode = EveryOrderedDelivery
# EveryOrderedDelivery = 每次可信顺序交付按当前档位反馈；OnLevelUp = 仅升级时用分级波形
MaxHapticTier = 4
# ×4 以上保持 ×4 触觉，原始连续计数不可截断
AllowValidatedDerivedTier = false
# 首发优先只采用实测的游戏权威等级；完成校准后才允许启用推导
UnknownTierFallback = DeliveryOrdered
# 等级未知时不猜档位
TeamBroadcast = true
ComboMasterIntensity = 0.85

[Stars]
Enabled = true
# 游戏内实时得分首次达到门槛即触发；不是最终结算星级保证
TriggerMode = FirstScoreThresholdCrossing
# 无可靠门槛/分数时只禁用星级模块，不能猜测
RequireVerifiedThreshold = true
HighestOnlyWhenMultipleCrossed = true
TeamBroadcast = true
MilestoneReplayWithinLevel = false
CelebrationPreset = Celebration

[StarFeedback]
Mode = HapticsAndAudio
# HapticsAndAudio / HapticsOnly / AudioOnly / Disabled
SoundStyle = CrispReward
# CrispReward 为清脆奖励音预设，不包含商业歌曲
SoundVolume = 0.65
# 仅 MOD 相对音量，不修改游戏/系统音量；候选默认值待实测调校
MaxSoundDurationMs = 1000
MaxCelebrationRumbleMs = 850
SuppressDeliveryFeedbackOnMilestone = true

[Audio]
RespectGameFocus = true
MuteInMenuAndOnPause = true
# 当前仅支持由 MOD 自行生成或授权分发的音效
AllowLocalCustomAudio = false

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
1. 验证 `ClientKitchenFlowControllerBase` 的成功/失败/订单过期事件，并只选一个不重复的 Hook 层级；重点记录成功上菜事件中的 `OrderID`、`tip`、`wasCombo` 和 Combo 状态。
2. 按 §4.3 区分 `DeliveryOrdered` / `DeliveryOutOfOrder` / `DeliveryUnclassified`，验证首单、乱序、重复菜谱、连击恢复等条件；AI 先向用户展示两种震动方案并征求确认。
3. 按 §4.4 实现 `ComboTracker`、Combo 等级来源探针及 ×1～×4 波形；优先验证游戏真实等级，不能仅靠 `wasCombo` 推测。若当前版本无法获取可信等级，先交付完整状态机/单测并将等级触觉安全降级。
4. 接住事件优先验证 `ServerAttachmentCatcher.HandleCatch`，联机客机无执行路径时寻找对应客户端实体/消息事件。
5. 完成触觉双震、Combo 分级波形、长震和去重行为；一次成功上菜只触发一种最终震动；遇到无法识别的个人操作则保守降级。

**验收：** 顺序上菜和乱序成功上菜震动可明显区分；Combo ×1～×4 在可靠等级可用时按 §4.4 准确触发，×4 以后封顶；Combo 未验证时安全降级；同一次交付只震一次；错误上菜和订单超时另有独立反馈；仅个人动作属于个人手柄，团队事件可按配置广播给本地手柄。

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

### 阶段 G：v1.3 星级庆祝音效与辨识度改造（新增）

**任务：** 见 §19 的详细技术方案。依次验证实际分数及门槛、完成 StarMilestoneTracker、实现独立音效后端、将星级奖励与同一次上菜/Combo 仲裁、改善高频动作震动并完成盲测。优先完成无 Unity 的阈值/状态机/调度器单测，再进行游戏实测。音效素材不得未经许可打包。

**验收：** 在当前游戏版本至少测试一个可达到 1～3 星的关卡，以及允许 4 星的 New Game+ 关卡；首次跨越精确触发，4 星只在存在可信第 4 阈值时生效；音量默认 0.65，可一键关闭；同次上菜只震一次；紧急危险可中断庆祝；测试记录完整。无法进入 4 星环境时记为未实机验证，不得直接判通过。

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
- [ ] DeliveryOrdered、DeliveryOutOfOrder、DeliveryUnclassified 三种分类分别触发正确波形，一次成功交付只能触发一种分类。
- [ ] Combo ×1/×2/×3/×4+ 分别对应正确波形，超过 ×4 不继续增强；从 ×4 乱序重置到新的 ×1 不沿用旧等级。
- [ ] `wasCombo=true` 但没有可靠数值等级时不能伪造 ×2/×3/×4；未知等级按普通成功波形降级。
- [ ] `EveryOrderedDelivery`、`OnLevelUp`、`Combo.Enabled=false` 的震动行为正确；Combo 和普通上菜波形不叠加。
- [ ] 不同 TeamId 独立计数；场景切换/中途加入/漏事件失同步后清空或降级；同菜谱与真实重复订单不会被误去重。
- [ ] 首次正常上菜、Combo 中断、同菜谱订单等边界输入无法判断时安全降级；`tip > 0` 不会自动判定为顺序上菜。
- [ ] 动作事件缺失玩家映射时不会广播给所有本地手柄。

### 12.2 人工实机测试

- [ ] Windows 10 / Windows 11；Steam 版优先。
- [ ] 黑武士 5 Pro 2.4G XInput 模式，记录实际设备名称和固件/驱动版本。
- [ ] 一手柄、两手柄、一键盘一手柄；拔插设备与切换手柄模式。
- [ ] 空手投掷、投掷成功、被挡住投掷、别人投掷、接住食材。
- [ ] 顺序上菜、乱序但成功上菜、首单成功、Combo ×1→×2→×3→×4→连续第 5/6 单封顶、连击中断与恢复、同菜谱订单、错误上菜、订单超时、烹饪完成/过度/烧焦。
- [ ] 对照游戏实际 Combo / 小费 / 订单 UI 验证分类和分级，记录 `wasCombo`、客户端真实倍率来源与所有候选字段的语义；没有权威等级时，不声称 Combo 分级已实机验证。
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
11. **组合动作额外扩展**：投掷 + 接住的协作节奏特效、Combo 升级里程碑的自定义特效；**基本 Combo ×1～×4 分级已经是 v1.2 必选功能**，这里仅指更多可选花样，不改变游戏分数。
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

- **v1.3 特别提示：** 2026 年的 Side Dish 社区 MOD 文档区分 Steam 默认分支（x86）与 Steam `crossplay_branch`/GOG（x64）。本项目必须按用户实际游戏分支核对并选对应构建，不可假设 Windows 64 位 = 游戏 x64。参见 §19.9 来源。

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
> 执行方式：先检查现有工程/游戏程序集和依赖，再建立可编译的最小 BepInEx 5 插件、实现 XInput 震动测试、做 Throw Hook 探针、验证真实事件、逐步添加反馈系统。按 §4.3 与 §4.4 做顺序上菜、乱序及 Combo 等级触觉，同次上菜只播放一种波形；候选变量必须验证后才能称为当前倍率。优先可运行代码、可重现测试和完整交付，不要停留在空洞的架构设计。
>
> **v1.3 新要求：** 增加 §19 庆祝型星级达标反馈。用户已经选定“庆祝型”；“震动＋音效、MOD 音量 65%、清脆奖励音”为候选默认配置。开始前必须核实当前游戏版本、实时分数及关卡星级阈值；以可信的首次跨门槛而不是每帧或结算 UI 动画作为触发判据。音效不得使用未经授权的商业音乐；星级与同次上菜/Combo 反馈必须冲突仲裁，危险提醒优先。具体波形、强度及长音效是否会打扰玩家，提出至少两种实测方案供用户选择，不得擅自确定为不可修改常量。
>
> 若当前环境没有游戏或 Windows 手柄，则完成能离线实现的全部内容：工程、逻辑、单测、构建脚本、Hook 候选研究、配置与使用文档；将实机验收明确标记为未完成，**绝不声称已在游戏内测试成功**。
>
> **沟通要求（优先级高于自行扩展）：用户特别喜欢被询问并参与设计。** 遇到需求不明确、波形手感有多个合理方案、认为存在更好的设计或有额外新点子时，主动提出自己的建议、替代方案、利弊和推荐理由，等用户确定后再修改产品行为。已批准范围内的普通代码编写、局部错误修复和测试可以自主推进；不得把未确认的新玩法/震动体验擅自当成已批准。Combo 分级波形参数只是待体验的默认草案，正式固定前必须询问用户。
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
- 订单、上菜、Combo 判定与失效：https://hpmv.dev/docs/tech/kitchen_flow_controller/
- Team17 官方解释顺序交付和 Combo：https://www.team17.com/news/the-a-z-of-overcooked-2
- 连击计数与倍率显示的玩家指南（仅供行为核对，不替代实测）：https://www.playstationtrophies.org/game/overcooked-2/guide/
- 上菜顺序影响小费倍率的玩家经验：https://steamcommunity.com/app/728880/discussions/0/1652171126130895739/
- 烹饪状态：https://hpmv.dev/docs/tech/cooking/
- 角色控制：https://hpmv.dev/docs/tech/controls/

**原生震动讨论：**

- Team17 Steam 社区答复：https://steamcommunity.com/app/728880/discussions/1/632318982807161167/

---

## 18. 给项目负责人的选择建议

**第一优先级（必须完成）**：BepInEx 最小插件、XInput 测试、真实投掷事件、停震保护、可追踪日志。  
**第二优先级（核心上菜模块必须实现；其余按阶段推进）**：接住食材、**顺序/乱序成功上菜差异化触觉 + Combo ×1～×4 分级震动（v1.2 已批准，需实际验证等级来源）**、失败上菜、订单过期、低震动模式与配置文件。  
**第三优先级（根据测试决定）**：烧焦提前预警、多手柄正确绑定、在线联机兼容。  
**已进入 v1.3 新增范围（候选默认配置、未开发）：** 庆祝型 1～4 星实时达标、星级与 Combo 互斥的震动＋音效、65% 音量、清脆奖励音、提高事件震动辨识度；需要当前游戏版本实测以及用户体验确认。
**后续按兴趣添加**：游戏内设置 UI、连击等级以外的额外触觉创意、方向感、高级手柄特性、波形编辑器、第三方 MOD 联动。

该排序是为了优先得到一款“实际可玩、不会乱震、容易卸载”的 MOD，而不是一开始堆砌功能。


---

## 19. v1.3 新增需求：庆祝型星级反馈 + 音效系统 + 触觉辨识度提升

> **需求状态：** 用户已确认选用“庆祝型”交互方向；用户要求将“震动＋音效、65% 音量、清脆奖励音”作为**候选默认配置**加入开发需求。下述节奏、长度、频率、马达强度均为供实机体验的初始建议，并非用户已批准的最终固化手感。禁止将此章节误写成已实现/已验证功能。

### 19.1 产品目标与范围

1. 当本局**已结算并同步到客户端的实时队伍总分**首次跨过关卡对应的第 1、2、3、4 星分数线时，按当前关卡**实际可用等级**触发一次庆祝型反馈。第 4 星必须在 New Game+ / 实际具有第 4 门槛的模式才可用。
2. 默认体验为 `HapticsAndAudio` + `SoundVolume=0.65` + `SoundStyle=CrispReward`（清脆奖励音）；提供 `HapticsOnly` / `AudioOnly` / `Disabled` 选项。MOD 音量与操作系统/游戏主音量分离，不更改用户其他音量设置；不得绕过游戏静音或焦点策略。
3. 提高投掷、接住、顺序上菜、乱序、Combo、临期、火灾的辨识度：**不同节奏轮廓优先于纯强度提升**。
4. **只读监听**：不得改变得分、订单、星级门槛、倒计时、联机同步或游戏存档。
5. 正常上菜 + Combo 升级 + 星级达标同一事件内不得播放三组反馈；通过统一仲裁选择一组波形和至多一条庆祝音效。

### 19.2 星级判定的数据来源与验证顺序

**公开确定的事实：** Team17 官方支持 New Game+ 第 4 星；社区现有 Side Dish 模组具备实时显示分数、预测星级和下一星所需分数的功能。这只能证明可行性有先例，**不代表存在稳定公开 API，也不授权直接复制该项目的代码/素材**。

**只读探针工作流（先探针、后发布）：**

1. 识别 `GameBuild`, `PlatformBranch`, `LevelId`, `DlcId`, `GameMode`, `NewGamePlus`, `TeamId`, `RoundInstanceId` 和实际本地玩家设备。缺失必要信息时标记 `Unknown`。
2. 通过当前版本实际 `Assembly-CSharp.dll` 和游戏数据中的关卡配置/运行时对象，查找 `CurrentCommittedScore`、`StarThresholds`、模式和人数对应的分数线。对照**世界地图显示门槛**和关卡结束时的实际星级。不要将网上的固定分数表硬编码为权威。
3. 为候选 Hook 写“只记录日志、无震动”的探针：字段包括 `roundId, levelId, teamId, mode, playerCount, previousScore, currentScore, thresholds[1..N], source, timestamp, confidence`；记录字段类型、调用线程、日志样例。禁止收集 SteamID、聊天等无关个人信息。
4. 优先选择**分数已经提交**后的客户端回调/同步结果；若有可靠的本地镜像状态，可按分数变化读取，建议变更驱动而非每帧全局扫描；读数不可信时应关闭该功能，而不是用 UI OCR 或上菜数量推测星级。
5. 分数可能降低；本需求是**首次曾达到门槛的庆祝提示**，并不是“锁定最终获得星级”。可选文案写“当前分数已达到 X 星门槛”，不得承诺最后结算必为 X 星。
6. 如果关卡模式没有星级或可用等级不足 4，`availableThresholds` 只包含真实值；第 4 星不存在时不可猜测也不可触发。

**用于逻辑层的纯数据输入：**

```csharp
// 伪代码：仅表示数据契约，并非当前游戏中已确认的方法或类型
struct ScoreSnapshot {
    string RoundInstanceId;
    string LevelId;
    string TeamId;
    int CommittedScore;
    int[] ThresholdsAscending; // 1..N，N 通常 3，具备 4 星时可为 4
    bool IsAuthoritative;
    long MonotonicTimestampMs;
}
```

**阈值校验：** 分数不得为非法/未初始化的占位值，门槛必须严格递增、为有效正整数、对应当前关卡/模式/人数；若由队伍同步产生多个近似通知，以 `RoundInstanceId + TeamId + ScoreVersion/commitId` 消重。没有 commitId 时采用**同一快照指纹 + 短窗口**做防重复，且绝不单靠时间窗口吞掉两个真正的不同得分事件。

### 19.3 庆祝型四星触觉语言（首轮候选波形）

强度按 `0.0..1.0` 相对值表示，括号内为 `(左电机,右电机,时长ms)`；停顿用 `(0,0,时长ms)`。数值只是出厂候选，并非可跨手柄保证的实际触觉强度。

| 达标星级 | 候选震动序列 | 音效创意 | 总时长目标 | 设计语言 |
|---|---|---|---|---|
| ⭐ 1 星 | `(0.05,0.22,75)` | 单声清脆铃音 / 短亮音 | ≤150ms | 一个清晰的“达标”标点 |
| ⭐⭐ 2 星 | `(0.06,0.26,65) → 停55 → (0.14,0.34,95)` | 两个上行短音 | ≤300ms | 上升、成双 |
| ⭐⭐⭐ 3 星 | `(0.07,0.26,55) → 停45 → (0.13,0.33,65) → 停45 → (0.20,0.42,105)` | 三音上行 + 清晰终音 | ≤500ms | 层层递进、末尾重音 |
| ⭐⭐⭐⭐ 4 星 | `(0.09,0.26,55) → 停45 → (0.12,0.34,60) → 停45 → (0.18,0.42,65) → 停60 → (0.32,0.48,170)` | 4 音短琶音 + 和声音尾（原创，≤1s） | 震动 ≤850ms，音效 ≤1s | 明显的关卡成就庆祝，但不持续骚扰 |

**基础约束：** 星级的声音和震动分别能开关；星级 1～4 的“旋律走向”要具有家族相似性，但节奏长度不同且可识别。音频不应骤然开始/结束，需 3～12ms attack/release 包络减轻爆音；所有波形经马达硬上限、全局强度系数和负载保护。为简化控制，**音效的最后一小段可以延续于最后一个震动片段后，不要求强行逐毫秒完全同步**。当用户反馈 4 星持续时间太长，先缩短音效尾音而不是提高或延长马达强度。

**与普通动作触觉区分：** `ThrowSuccess` 用单次清脆短脉冲；`CatchSuccess` 用两段不同间隔；`DeliveryOrdered/Combo` 用快节奏上行型；`DeliveryOutOfOrder` 用偏左低沉长单脉冲；`Fire/Burnt` 用离散且有紧迫感的强提醒。避免 1 星被误认为普通投掷：1 星必须带不同的明亮铃音（静音模式下则在动作短震之外增加明显的尾随轻脉冲，参数待用户体验选择）。

### 19.4 声音技术实现（首选不加独立进程）

1. 建 `IRewardAudioBackend` 与 `UnityRewardAudioBackend`；在主线程生成或维护自己的 `GameObject + AudioSource`，`spatialBlend=0`（2D），`playOnAwake=false`，默认关闭循环。注意生命周期、场景切换和插件卸载时清理；不能改变游戏已有 AudioSource、Mixer、音乐音量。
2. 调用 Unity 支持的 `AudioSource.PlayOneShot(clip, volumeScale)` 实现短音效；要中断当前庆祝音，优先用独立的 `AudioSource` 实例/当前 clip `Stop` 清除该源，不影响游戏本体的声音。若当前 Unity 版本需要其他调用方式，以目标程序集实测为准。
3. `SoundVolume=0.65` 视为**MOD 自己的音效线性增益系数**，不是系统主音量百分比也不是 65% 听觉响度。为了避免重复乘 0.65，建议 `AudioSource.volume=1`，`PlayOneShot(clip, 0.65f * perStarVolumeScale)`；`perStarVolumeScale` 默认为 `1.0`，并将所有结果夹在 0～1。需要峰值归一、削波检查、主观音量对照。
4. 清脆奖励音优先使用**本项目自行生成的原创短 PCM 波形**（可用 `AudioClip.Create/SetData`；目标 Unity API 需核对）。可选使用确实允许分发的 CC0/CC BY 音效，留好来源、作者、许可证和 attribution；不得把“网上能听到的商业歌曲”直接打包到 GitHub，也不能暗中抓取别的 MOD 音效。
5. 自定义本地音乐文件属于**后续可选功能**，默认关闭，需用户另行确认。若今后加入，仅引用用户本地文件，不自动上传/分发；文件路径必须在本机做白名单、大小/时长、格式/解码异常检查。
6. 尊重游戏暂停、焦点丢失、切关、切菜单的关闭策略，星级反馈不在后台突然播放。主线程执行 Unity Audio API，逻辑层独立于 Unity 便于 Mock 测试；音频初始化/加载失败只禁用音频，不影响震动或游戏。

### 19.5 StarMilestoneTracker 状态机与伪代码

**状态键：** `RoundInstanceId + TeamId`；维护 `PreviousScore`, `MilestonesAlreadyAnnounced` (1～4 bitset), `Thresholds`, `LastCommitId`, `Enabled`。必须能处理重开关卡、人数变动、关卡切换、联机重连、时间暂停、漏事件。

```text
OnScoreSnapshot(s):
  if !s.IsAuthoritative or invalid(s.Thresholds):
      MarkStarFeatureUnavailable(reason); return
  if RoundChanged(s) or TeamChanged(s):
      ResetTrackerForThisRoundTeam()   # 不跨关卡继承已庆祝状态
  if FirstSnapshotInThisSession(s):
      # 初始快照已产生分数时不回放过去的庆祝
      if s.CommittedScore > 0:
          Announced = starsAlreadyReached(s)
      PreviousScore = s.CommittedScore
      return
  newStars = []
  for star in 1..len(s.Thresholds):
      if s.CommittedScore >= s.Thresholds[star]
         and star not in Announced:
          Announced.add(star)
          newStars.add(star)
  if newStars is empty: return
  selected = max(newStars)              # 默认只播放本次跨越最高星
  Enqueue(StarMilestoneReached(selected, correlationId))
  PreviousScore = s.CommittedScore
```

**语义细节：** 首次观察到的合法快照可能已经是 2 星（例如 MOD 中途启用）；默认不追溯庆祝旧成绩，先记录“进入时已达标的星级”为已播集合，然后只处理**后续**真实得分变化。若在正常开局前拿到初始 `score=0`，按 0→首次得分判断。超过一次多个门槛，默认只播放最高一档且将所有较低等级标记已播。得分降到门槛以下再涨回来，不重播；同一 roundId 的重发消息不重播。无可靠 RoundId 时用场景进入+明确的 round begin/end 事件管理会话，无法确认时保守禁用星级功能。

### 19.6 同次上菜/Combo/星级事件的统一仲裁

**痛点：** 同一盘上菜可能触发 `DeliveryOrdered`、`ComboLevel3`、`ScoreSnapshot` 达标以及 `StarMilestone3`。若不合并，马达会连震、声音重叠。

**建议：** 统一通过 `CelebrationCoordinator` 消费标准化游戏事件，建立 `(RoundId, TeamId, DeliveryId/ScoreCommitId)` 关联；有 `scoreCommitId` 以它为准。为异步到达的上菜与分数变动，给“普通上菜/Combo”短暂合并窗口 **80～150ms**（默认建议 120ms，可配置）。窗口中如果确认跨过星级线，发 `StarMilestone` 取代同一次上菜/Combo 反馈。窗口内未跨线则按原规则播放 Combo/成功上菜；没有相关 DeliveryId 的分数变更照样能独立触发星级反馈。

**优先级建议：** `火灾/烧焦/订单过期 > 星级庆祝 > 同次上菜 Combo > 普通动作`；高风险实时提醒可中断震动和 MOD 自己的庆祝音，取消的旧庆祝不应在危险解除后回放。不同队伍独立仲裁，**本机星级音效只播一次**，本地手柄按所属队伍广播。分数更新晚于窗口时不得伪造完美同步，记录 `DelayedScoreEvent`、观察延迟并重新评估窗口。不能强行用长时间等待拖慢全部上菜反馈。

### 19.7 配置 / UX / 手柄辨识度改进

**本版候选默认：** `Mode=HapticsAndAudio`, `SoundStyle=CrispReward`, `SoundVolume=0.65`, `CelebrationPreset=Celebration`；允许用户现场修改，禁用时立即停止 MOD 音效/震动，并保持其他游戏声音正常。音效播放日志只记录事件、clip 标识、音量和成功/失败，不保存音频个人文件名。

**推荐添加测试能力：** 独立 `HapticPattern` 预设集，能在不进入厨房的测试页/CLI 上驱动假设备、也能在真实手柄上逐个体验；做“听/摸后猜是哪个事件”的盲测，统计混淆矩阵。例如 10 次随机触发中，投掷与接住仍被混淆超过 3 次，就优先调整间隔、段数和左右比例，不先增加强度。具体判断阈值属于研发验收建议，不是强制产品指标。

**兼容约束：** 一星只有一次小脉冲时，纯震动模式可能与普通操作相似；需提供更可辨认的纯震动备选，并交给用户确定。主机扬声器播放团队庆祝音，远端玩家未安装 MOD 不会接收到远程声音；不能把“只读本地触觉”升级成强制网络同步。

### 19.8 自动化测试矩阵（可由 AI 直接写单测）

| 编号 | 输入与情形 | 必须输出 |
|---|---|---|
| S01 | 可信分数 `0→t1-1→t1` | 仅第一次达到 t1 时触发 1 星 |
| S02 | `t1→t2→t3→t4` 且四星模式开启 | 星级按 1、2、3、4 各一次，顺序正确 |
| S03 | 单次 `0→t3+` | 仅播放 3 星（较低 1/2 星标记已播） |
| S04 | 分数 `t2+→t2-→t2+` | 不重复播放 2 星 |
| S05 | 同一个 ScoreSnapshot/commit 重复 5 次 | 只触发一次 |
| S06 | Normal / 3 星阈值缺第 4 项 | 永不触发 4 星，不猜测 |
| S07 | 无法读取阈值、比分不同步、门槛乱序 | 关闭星级模块并警示日志，其他模块继续 |
| S08 | 中途加入时首个快照已达 2 星 | 初始化为已达，无追溯 1/2 星奖励；后续 3 星正常 |
| S09 | 重开同一地图但新 RoundId | 重新允许 1～4 星奖励 |
| S10 | 星级达标 + 同次上菜 Combo 升级 | 星级震动/音效取代 Combo，不叠加 |
| S11 | 星级 4 庆祝正在播放，发生火灾 | 立即停 MOD 庆祝音/震动，执行火灾警示 |
| S12 | 音效模式切到 HapticsOnly/Disabled | 立即不播放声音；Disabled 两者都停止 |
| S13 | 65% 音量配置，以及 0、100、越界、NaN | 0.65 系数正确，非法输入安全钳制/回退，不爆音 |
| S14 | 丢失音频资源 / Unity 音频设备失效 | 星级震动继续正常；不影响其他游戏音效 |
| S15 | 多手柄本地合作且只有 A 队取得星级 | A 队已绑定手柄振动；别队不震动；本机音效仅一次 |
| S16 | 暂停、关卡切换、失焦、返回菜单 | 及时停震停音并清空当前队列 |
| S17 | 重复关卡开始/掉线重连/TeamId 改变 | 会话状态边界安全，不能串队、不能重播旧庆祝 |
| S18 | 星级功能关闭 / 已经 4 星以后得分继续增加 | 不触发额外庆祝；无后台资源泄漏 |
| S19 | 虚拟回放 1000 个高频动作事件 | 限频/丢弃正确，未出现持续震动或音频爆量 |
| S20 | 30 次随机震动盲测，记录答案 | 输出事件混淆矩阵和修订建议（不是自动修改玩家手感） |

**人工实机最小验收：** （1）游戏正常进入厨房；（2）在有可核对星级门槛的关卡按顺序完成 1→3 星真实得分跨越；（3）New Game+ 可达 4 星的关卡验证 4 星；（4）用游戏 HUD/结算画面对照门槛与真实积分；（5）单次跨多星；（6）两人本地手柄 + 在线客机分别检验；（7）后台/暂停停音；（8）音量初始 65%，调到 0、50、100 都能观察到变化；（9）交付＋Combo＋星级三重事件无叠加；（10）火灾打断庆祝。没有真实游戏或无法解锁 4 星时，仅可标“逻辑通过/未实机验证”。

**判定标准（研发建议值，允许开发前经用户批准调整）：** 单测 S01～S19 全部通过；真实可重现的星级边界事件误报 0 次、漏报 0 次（按测试样本）；同一关卡每个等级至多一次；确认跨线到反馈输出的程序端延迟目标 ≤150ms（真实设备端感知时间单独记）；音效最大 1000ms、震动最大 850ms；在用户盲测中目标主要事件类别辨识率 ≥80%。实际性能数据必须带日志/样本，不得猜测。

### 19.9 开发风险、关键决策与资料依据

**风险及应对：**

- **星级门槛与版本/模式/人数有关：** 默认只使用实际权威门槛，不用网上固定分数表填补；跨平台或 DLC 必须另测。
- **New Game+ 才有四星目标：** 缺第 4 门槛时不能播放 4 星音效。关卡结算可能存在扣分、未最终结算等差异，首次跨线庆祝不等于最终评星保证。
- **反馈打扰操作：** 4 星庆祝太长可能掩盖火灾，危险可抢占；建议让用户比较 450ms 紧凑庆祝 vs 850ms 标准庆祝。
- **某次上菜同时触发 Combo/星级：** 在已验证事件身份基础上合并，不能盲目把不同玩家的事件串成同一次。
- **在线不同版本：** Side Dish 2026 年文档指出 Steam 默认分支为 x86，Steam crossplay_branch 与 GOG 使用 x64，BepInEx 版本/架构必须匹配；本项目还需自己验证。
- **音效版权：** 默认仅原创/可再分发的授权音频。CC BY 必须保留署名与许可；商业流行歌曲不可因为用户喜欢就直接打包或发布。
- **“音量 65%”不等于舒适：** 相对增益 0.65 仍可能过响；必须考虑音频素材峰值、响度、游戏背景音乐和个人听力差异。

**必须向用户主动询问的产品决策（不阻塞已批准范围内的研究/单测）：**

1. 4 星奖励：标准 850ms vs 紧凑 450ms，哪个更合适？建议优先实测标准，然后听用户感受，不允许静默锁定。
2. 游戏失焦时：默认静音停震（推荐）；用户是否希望允许后台播放？这是涉及体验的新选项。
3. 某次跨 2 星或 3 星：仅播放最高星（推荐，防止连续庆祝）还是连续播每一档？默认设计仅最高，若改变须用户确认。
4. 只开震动模式下，1 星的单脉冲是否足以与投掷区分？若不够，推荐额外短尾脉冲。
5. 是否需要“显示祝贺字幕/星星动画”？**当前未获批准，不开发**，先保证纯音效和震动精准。

**资料：**

- Team17 官方 New Game+ 与第四星：https://www.team17.com/news/overcooked-2-new-game-available-now-for-all-platforms
- Team17 FAQ：https://www.team17.com/news/overcooked-2-faq
- Side Dish MOD（实时 Score & Forecast、x86/x64 分支说明）：https://www.nexusmods.com/overcooked2/mods/10
- Unity 2018 AudioSource.PlayOneShot：https://docs.unity.cn/2018.3/Documentation/ScriptReference/AudioSource.PlayOneShot.html
- Unity AudioClip.SetData 2018.4：https://docs.unity3d.com/cn/2018.4/ScriptReference/AudioClip.SetData.html
- Creative Commons 作品许可说明（实际素材应以每份许可证为准）：https://freemusicarchive.org/faq

### 19.10 Codex 执行补充指令（由用户明确选择的范围）

> 在不破坏 v1.2 已完成功能的前提下，按 §19 实现庆祝型实时星级反馈。默认候选配置为 `HapticsAndAudio / CrispReward / 0.65`，先以可切换的默认值开发并向用户展示效果，不得视为无法更改的最终体验。先获取当前游戏版本/关卡/模式/人数及真实星级门槛，证明评分事件来源可信，再开发 `StarMilestoneTracker` 与 `CelebrationCoordinator`。音效采用游戏进程内 Unity AudioSource，优先原创/明确授权素材，独立可静音。开发单测与事件回放后再实机验证 1→3 星及 New Game+ 4 星。星级达标时替代同次交付 Combo 波形；危险警告优先并能停止庆祝。同步改善动作触觉区分度；先做盲测，再征求用户对波形的意见。自行修复常规编码问题，但涉及产品方案、体验强弱、音频素材方向、额外 GUI 或新增玩法时，必须主动给出比较与推荐并询问用户。每阶段写明真实验证证据及尚未验证事项，不得宣称已完成未经实测的 Hook。
