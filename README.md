# OC2 Haptic — 0.2.3-alpha（数据校准版）

Windows 版《胡闹厨房2》的单手柄触觉增强测试版。支持投掷、接住、上菜成功/失败、订单已过期，以及 F6 游戏内管理页、手动试震、配置、日志及停震保护。

源码仓库：[biany-captain/Overcooked2-Haptics](https://github.com/biany-captain/Overcooked2-Haptics)。当前已验证基线为 **0.2.2-alpha**。新版设计文档的 **v1.3 是需求版本号**，不代表插件已经发布到 1.3。

新版开发范围：顺序/乱序上菜分类、可信 Combo ×1～×4、首次达到实时星级分数门槛的庆祝震动及原创奖励音效。它们正在研究和开发，当前基线尚不具备这些功能。多手柄和在线兼容仍后置；实现及实测状态会分别记录。

**0.2.1 验证状态：编译 0 警告/0 错误，40 项核心测试和 4 项实际配置迁移测试通过。用户报告菜单、各试震、总开关同步、输入恢复及暂停试震均正常；日志确认菜单加载、各类预览成功和快捷键迁移。五类游戏事件的实测保留在 `docs/test-report-0.2.md`，证据边界见 `docs/test-report.md`。**

0.2.2 修复截图中说明文字被裁切的问题：按实际字体测量行高，说明/列表/底部文字依次排布，窗口高度随内容调整，小窗口列表可滚动；面板使用深色背景提高可读性。用户重新加载后已确认“文字完整，遮挡已解决”。触觉和开关逻辑沿用已验证版本。画面验收见 `docs/menu-layout-0.2.2.md`。

实际目标设备：黑武士 5 Pro，2.4G XInput。按本地一手柄验收；本地多人、多手柄映射和联机覆盖后置。接住当前使用本地/房主服务端路径，在线客机尚未验证。新增管理页只控制已有触觉，不自动暂停关卡。

## 新版数据校准（0.2.3）

新版需求的首个实施步骤是独立的只读探针：对照客户端实际提交的分数、HUD 倍率和关卡星级门槛。**这一版没有新增自动 Combo/星级反馈或声音，也没有改变已有触觉参数。** 校准后才开放后续反馈，避免把未知来源当成真实等级。

- 在本插件 cfg 的 `[Diagnostics]` 中设置 `ScoreSourceProbe = true` 后重启游戏启用；默认 false，关闭后立即停止采集。无需启用其他事件的冗长日志。
- 日志条目为 `Score source probe`，包含复制前/消息中/应用后的数值、订单 ID、wasCombo、tip、当前关卡/人数/模式和候选门槛；不记录玩家账号。
- 第四星同时检查关卡 NGP 和游戏 NGP 提示状态；数据或模式不匹配时显示 Unavailable，不猜门槛。
- `confidence=StaticILOnly runtimeComparison=Pending` 表示尚需实际 HUD/地图对照；Patch 安装成功不等于校准通过。
- 探针错误只关闭探针，原有五类事件和菜单继续运行。

开发顺序见 [v1.3 实施设计](docs/design-v1.3.md)，本轮操作见 [校准清单](docs/calibration-0.2.3.md)。

## 安装

1. 关闭游戏，保留现有 MOD 环境。需要与游戏架构匹配的 BepInEx 5；本机检查到 x86、BepInEx 5.4.22.0。无需替换现有加载器。
2. 将安装包中的 `BepInEx/plugins/OC2Haptic/OC2Haptic.dll` 放到游戏目录相同位置。
3. 启动游戏，检查 `BepInEx/LogOutput.log` 是否出现：
   - `OC2Haptic initialized`
   - `Throw Hook installed: ClientThrowableItem.StartFlight(IClientThrower)`
   - `CatchSuccess Hook installed: ServerAttachmentCatcher.HandleCatch`
   - 三条 `Kitchen Hook installed`（成功上菜、失败上菜、订单过期）
4. 首次加载会生成 `BepInEx/config/dev.oc2haptic.feedback.cfg`。`config/OC2Haptic.cfg.example` 仅供参考，不会覆盖现有配置。

插件初始化日志只证明加载；Hook 安装日志只证明补丁安装，均不能单独证明真实操作正确。

## F6 触觉管理页

- F6 打开/关闭菜单，打开时不会自动震动；右上角 × 也可关闭。
- 菜单的“总震动开关”与 F10 控制同一个 Enabled 设置，实时同步。总开关关闭时 F6 仍可打开菜单。
- “通用试震”保留原 80ms 测试；五个事件各有独立试震按钮，沿用已确认的节奏与强度。
- 事件复选框控制游戏自动触发；独立试震不受该事件复选框限制，但必须开启总开关。手动预览单独记录，不计入真实 Hook 事件统计。
- 开关修改即时保存到本插件 cfg。原总强度和绑定在页面中只读显示。
- 菜单不自动暂停。只抑制当前绑定手柄的本地厨师操作；关闭、切场景或禁用时释放输入和光标。关卡仍运行，调试前可先手动暂停。
- 已暂停时仍可点试震，实时调度器负责结束并归零。正在播放更高优先级反馈时，低优先级预览可能被拒绝，请稍后再点。
- 旧 TestRumble 快捷键迁移到 OpenMenu；旧值 None 保持禁用，已有 OpenMenu 配置优先。

菜单实机清单：`docs/menu-test-0.2.1.md`。

## 首次绑定和测试

本机开发验收已完成 Pad0 → XInput0 绑定。安装包默认仍为未绑定，供其他环境按各自实测结果设置。

游戏 Pad 编号、本地玩家编号、Windows XInput 索引是不同标识。默认两个绑定值均为 `-1`，不输出震动。

1. 将手柄接入 2.4G XInput 模式，按已知连接状态选定 `[Devices] XInputIndex = 0..3`。
   - 开发环境可运行 `./scripts/test.ps1 -Filter '--devices'` 查询连接状态；该命令不输出震动。
   - 单个设备也不应仅凭“玩家 1”就推断 XInput 索引是 0。
2. 按 **F6** 打开管理页，再点“通用试震”。选定手柄应轻震 **80ms** 后停止；再次按 F6 关闭。
3. 暂时保持 `LocalGamePadSlot = -1`，将 `DebugLogEvents = true`，进入允许投掷的关卡，用手柄真正投出食材。
4. 查看 `Event probe | type=ThrowSuccess` 日志。确认 `keyboard=False`，并记录 `gamePad=N`。将 `LocalGamePadSlot` 设为该 N。
5. 再投掷一次，检查日志是否为 `allowed=True accepted=True`，并确认只有指定手柄短震。
6. 验证成功投掷、空手按键、不可投物品、被挡住投掷；每种情况分别记录实际次数、日志次数和震动次数。空手及失败操作不应产生成功反馈。
7. 按 **F10** 关闭，再重复操作，应无震动。切换到菜单、暂停和退出时检查是否停止。

F6 页面和已有 Configuration Manager 读写同一组配置。修改输出相关配置会立即清空已有震动；手动编辑 cfg 请在关闭游戏时进行，没有文件监视热重载。

## 触觉初值

| 事件 | 左/右基础强度 | 时长 | 冷却 |
|---|---|---|---|
| 菜单通用试震 | 0.22 / 0.28 | 80ms | 120ms |
| 成功投掷候选 | 0.15 / 0.32 | 65ms | 120ms |
| 厨师接住飞行物 | 0.25 / 0.18 | 90ms | 120ms |
| 成功上菜 | 右0.20，间隔后右0.24 | 70ms + 静默80ms + 70ms | 120ms |
| 上菜失败 | 0.34 / 0 | 220ms | 120ms |
| 订单已过期 | 0.40 / 0.30 | 250ms | 120ms |

默认总强度倍率为 **0.75**，因此实际下发的投掷强度为 0.1125 / 0.24。时长由 Unity 更新调度归零，实际设备时长存在帧调度和驱动误差；65ms 是逻辑目标，不是已测得的物理值。

优先级为：订单过期85 > 上菜失败55 > 上菜成功30 > 接住25 > 投掷20 > 测试10。高优先级可打断，低/同优先级不排队补播。订单反馈按独立解析到的本地队伍筛选，只驱动已绑定手柄。整批测试步骤见 `docs/batch-test-0.2.md`。

## 配置

| 项 | 默认值 | 含义 |
|---|---|---|
| General.Enabled | true | 总开关；关闭立即清空输出 |
| General.MasterIntensity | 0.75 | 0–1；0 完全无震动；非有限值静音 |
| General.DebugLogEvents | false | 开启后记录每次探针和路由结果 |
| Diagnostics.ScoreSourceProbe | false | 新版比分/倍率/门槛只读校准；启用需重启或重新启用插件 |
| Devices.XInputIndex | -1 | 手动选定 Windows 设备槽，0–3 |
| Devices.LocalGamePadSlot | -1 | 探针报告的游戏 Pad，0–14 |
| Events.ThrowSuccess | true | 投掷反馈开关；不影响手动测试 |
| Events.CatchSuccess | true | 接住反馈开关 |
| Events.DeliverySuccess | true | 本地队伍上菜成功双震 |
| Events.DeliveryFailure | true | 本地队伍上菜失败长震 |
| Events.OrderExpired | true | 本地队伍订单实际过期提醒 |
| Hotkeys.OpenMenu | F6 | 管理页开关键；None 可关闭 |
| Hotkeys.ToggleMod | F10 | 总开关键；None 可关闭 |

F8/F9 已由本机主机工具占用，因此采用用户确认的 F6/F10。其他环境如有快捷键冲突，可在配置中修改。

## 停震和故障行为

- 游戏暂停、关卡结束、活动场景变化、应用暂停、配置禁用、组件禁用/销毁及正常退出时清空任务。
- 使用单调时间；不依赖 Time.timeScale；不在 Hook 中等待或睡眠。
- 仅对本插件当前持有的明确输出槽位发送停止指令；最后一次归零后释放该槽位，不向其他手柄广播。
- 断连清空任务，不在重连后补播。换模式或重新插接后请重新确认绑定。
- 首版保留单手柄原有交互，不增加强制重连确认步骤。XInput 不提供持久硬件身份；更换设备后若沿用同一槽位，插件无法识别这种替换，必须由用户核对绑定。未宣称支持多设备热插拔映射。
- 找不到 Hook 时记录 `Unavailable`；测试功能仍可用。单个事件适配器异常时关闭对应反馈并记录异常，其余事件继续工作。
- 所有片段和完整动作模式最长 500ms；已过期事件不会排队补播。
- 主线程完全卡死、异常强制结束及设备/驱动故障时，无法保证及时停震。
- 游戏、Steam Input 或其他程序也可能改写马达输出；编译通过不代表此类冲突不存在。

## MOD 管理器、禁用与卸载

可使用已有管理器安排共存测试。部分管理器的“关闭”只改变下次启动的文件加载状态，并不卸载当前进程中的程序集；实际行为需要用你的管理器验证。**F10 / General.Enabled 是本插件即时停震入口。**

卸载：退出游戏，删除 `BepInEx/plugins/OC2Haptic/` 中本插件文件；配置文件可按需保留或另行删除。无需改动原版 DLL、存档、其他插件、Steam 设置或 BepInEx 核心。

## 开发与构建

运行 PowerShell，需 .NET SDK 10（仅用于开发和测试；游戏插件仍为 net35）。脚本使用已安装的游戏及加载器程序集，不下载 NuGet 包。

先克隆仓库并进入目录：

```powershell
git clone https://github.com/biany-captain/Overcooked2-Haptics.git
Set-Location Overcooked2-Haptics
```

需要本地安装游戏和匹配的 BepInEx。随后执行：

```powershell
./scripts/test.ps1
./scripts/test-config.ps1
./scripts/test-probe.ps1
./scripts/build.ps1 -GameRoot 'E:\SteamLibrary\steamapps\common\Overcooked! 2'
./scripts/package.ps1 -SkipBuild
```

不传 GameRoot 时从 Steam 库查找，也可设置 `OC2_GAME_DIR`。脚本的输出、构建缓存和 NuGet 配置隔离都留在工作区。

`test-config.ps1` 使用本机游戏/加载器程序集测试真实配置迁移，不启动游戏或触发震动；这些程序集仅作为开发依赖，不打包分发。

`test-probe.ps1` 使用本机游戏的纯数据类型及实际 PC 分数属性做探针回归测试，不启动 Unity、不输出震动。它覆盖缺失门槛/目录和同步不一致，不能代替实际 HUD 校准。

- DLL：`artifacts/OC2Haptic.dll`
- 安装包：`artifacts/OC2Haptic-0.2.3-alpha.zip`
- 源码包：`artifacts/OC2Haptic-0.2.3-alpha-source.zip`

不将游戏原始 DLL 或加载器 DLL 打包再分发。项目自身许可证尚待负责人选择；当前交付为本地测试 alpha。

## 仓库内容

提交范围为源码、测试、构建/打包脚本、配置示例和需求/验证文档。`.gitignore` 排除 `bin/`、`obj/`、`artifacts/`、`.cache/`、安装后的游戏和 BepInEx 文件、二进制文件、日志及个人配置。安装包在本地构建生成，不存入 Git 历史。仓库中的 `docs/evidence/*.txt` 是经筛选的验证记录。

## 来源

- 当前正式需求：[v1.3](docs/requirements-v1.3.md)；历史基线：[v1.0](docs/requirements-v1.0.md)
- [XInputSetState](https://learn.microsoft.com/en-us/windows/win32/api/xinput/nf-xinput-xinputsetstate)
- [BepInEx 文档](https://docs.bepinex.dev/)
- [Jetpack MOD 工程参照](https://github.com/gua248/Overcooked2-Jetpack)
- [hpmv 投掷研究](https://hpmv.dev/docs/tech/throwing/)

BepInEx、Harmony 及参考项目各有其许可证；本包仅本地引用它们，没有附带第三方库。游戏内容归原权利人所有。
