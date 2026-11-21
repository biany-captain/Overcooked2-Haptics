# OC2 Haptic — 0.4.1-alpha

Windows《胡闹厨房2》的本地单手柄触觉增强 MOD。支持投掷、接住、顺序/乱序上菜、Combo ×1～×4、订单过期，以及实时星级门槛庆祝。F6打开中文管理页，可调整强度、试音试震并选择星级音效包；F10控制总开关。

**状态：0.4.1按用户要求移除旧两套声音，接入Ultra Victory v3三套12首音乐；新音色及游戏音量跟随待实测。** 已有分类/Combo及部分真实星级有旧版本日志，未观察到的真实二星/四星、超时及其他兼容场景保留未验收。正式需求v1.4与后续用户确认记录共同约束开发，插件版本号分别管理。

[源码仓库](https://github.com/biany-captain/Overcooked2-Haptics) · [正式需求](docs/requirements-v1.4.md) · [当前接入设计](docs/design-ultra-v3.md) · [测试报告](docs/test-report.md) · [本轮实机清单](docs/batch-test-0.4.1.md)

[后续开发与验收顺序](docs/roadmap.md) · [音效包格式与新增方式](docs/audio-packs.md)

## 功能与边界

- 只读游戏事件，不改分数、订单、存档或网络消息。
- 投掷/接住只驱动明确绑定的本地手柄；上菜和星级按独立确认的本地队伍过滤。当前仅验收一手柄，本地多人/在线客机后置。
- 顺序上菜使用真实客户端确认的倍率，×4以后不继续增强，原始连续计数仍保留；乱序成功与真正失败分开。
- 分数首次达到当前关卡实际星级门槛时庆祝。门槛按关卡/模式/人数读取，不使用固定分数表。一次跨多星只庆祝最高星，分数下降再涨回不重播；中途启用不补播旧成绩。
- 同一条已验证的客户端消息内，立即选择星级或上菜/Combo中的一种。没有固定120ms等待，也不会额外播放旧通用成功双震。
- 星级表示“曾达到实时分数线”，不保证最终结算星数。第四星必须满足游戏的真实NGP条件；F6四星预览不等于真实四星验收。
- 新数据路径限已校准的当前游戏程序集/普通模式；未知版本/状态保守降级，失败与过期等原有事件独立工作。

## 安装

1. 退出游戏，使用匹配游戏架构的BepInEx 5。当前开发环境为Steam默认分支、x86、BepInEx5.4.22.0；保留已有加载器及其他MOD。
2. 将安装包的 `BepInEx/plugins/OC2Haptic/`（DLL及audio目录，含许可/来源）放到游戏目录同一位置。只复制DLL会缺少音乐，不再有内置铃音兜底。升级可删除本插件audio/joyful-cartoon-jazz旧目录，其他自备包保留；新版本也不会发现已退役的两个旧ID。
3. 启动后检查 `BepInEx/LogOutput.log` 中的 `OC2Haptic initialized | 0.4.1-alpha`、12条新包 `Reward audio prepared` 及各Hook安装信息。加载成功不等于事件验收通过。
4. 配置位于 `BepInEx/config/dev.oc2haptic.feedback.cfg`；示例 `config/OC2Haptic.cfg.example` 不应覆盖个人配置。旧上菜成功开关自动迁移，原有禁用偏好、强度、绑定和快捷键保留。

## 首次手柄绑定

游戏本地Pad、网络玩家ID与Windows XInput索引不是同一标识。安装包两个绑定默认均为-1，不猜测设备。

1. 选定实际连接的 `Devices.XInputIndex=0..3`；开发环境可运行 `scripts/test.ps1 -Filter '--devices'` 查询，查询不震动。
2. F6打开管理页，点击通用80ms试震，确认是目标设备。
3. 保持 `Devices.LocalGamePadSlot=-1`，暂启 `General.DebugLogEvents=true`，在允许投掷的关卡成功投出食材，读取 `Event probe` 中 `keyboard=False` 的 `gamePad=N`。
4. 将LocalGamePadSlot设为实际N，重启后验证成功投掷、空手不震、F10关闭后不震。

本机开发设备为黑武士5 Pro、2.4G XInput，已实测Pad0→XInput0。其他环境须自行确认。键盘玩家不输出手柄震动。XInput没有持久硬件身份，更换物理设备后要重新核对绑定。

## F6管理页

- F6开关页面，右上角×关闭；F10和页面总反馈开关控制同一设置。关闭总开关后仍可打开F6。
- 顶部总震动强度0–100%。「事件设置」中每项倍率0–200%，默认100%保留原手感；与总强度及既有Combo倍率叠加，保持节奏，达到上限时统一限制峰值以维持左右比例。
- 「星级测试」集中显示一到四星，每档有试音、试震、同时测试按钮和单独倍率。明确点击测试会替换前一次星级预览，不改变星级自动输出模式，也不计真实事件。
- 同页「音效包」下拉框统一选择四星音乐：默认超燃卡通大乐队，另有冠军加冕号角、厨房摇滚派对。切换停旧输出并保存；新增本地包后重启识别，见[包格式](docs/audio-packs.md)。本轮没有逐星混搭或欢呼分轨。
- 页面不自动暂停，只屏蔽当前绑定厨师操作并显示鼠标，关闭后恢复。关卡时间继续，调试可先手动暂停。
- 事件开关控制自动反馈；Combo和星级各有总开关。手动预览独立于自动事件开关，仍受总开关、相应强度/音量和设备条件限制。
- 星级自动模式：震动＋音效、仅震动、仅音效、关闭；相对音量默认100%，跟随游戏主音量及SFX音效组，并可调低。独立试音/试震按钮不受此自动模式及自动事件开关限制，但仍受总开关、焦点、相应强度/音量和设备条件限制。
- 用户已批准在主菜单/暂停中点击试听；自动反馈在菜单/暂停中静音。失焦、关闭菜单、F10关闭、切场景会停止本插件输出。
- 设置即时保存。手动预览单独记录，不计真实游戏事件。优先级较低的预览可能被拒绝，等当前反馈结束再点。

## 首轮候选波形

下表为基础马达强度，仍乘总震动倍率0.75；Combo ×2～×4另乘Combo倍率0.85。实际设备感受尚待本轮确认，可在cfg的Patterns节调整，格式为 `左,右,毫秒;...`。

| 事件 | 基础片段，左/右 | 时长 | 优先级 |
|---|---|---:|---:|
| 通用试震 | .22/.28 | 80ms | 10 |
| 投掷 | .15/.32 | 65ms | 20 |
| 接住双段 | .22/.10×35 → 停35 → .08/.30×45 | 115ms | 25 |
| 顺序 / ×1 | 0/.25×70 → 停80 → .18/.32×100 | 250ms | 30 |
| Combo ×2 | .12/.28×60 → 停55 → .20/.38×110 | 225ms | 32 |
| Combo ×3 | 0/.30×55 → 停50 → 0/.35×60 → 停50 → .22/.43×80 | 295ms | 34 |
| Combo ×4+ | 0/.31×55 → 停40 → 0/.35×55 → 停40 → 0/.39×55 → 停40 → .25/.48×95 | 380ms | 36 |
| 乱序成功 | .38/.08 | 160ms | 42 |
| 未分类成功 | 0/.14 | 70ms | 20 |
| 上菜失败 | .34/0 | 220ms | 55 |
| 订单过期 | .40/.30 | 250ms | 85 |
| 一星 | .10/.22×55 → 停90 → .05/.17×35 | 180ms | 46 |
| 二星 | .14/.23×70 → 停105 → .17/.35×110 | 285ms | 47 |
| 三星 | .18/.26×55 → 停70 → .23/.34×80 → 停75 → .32/.43×145 | 425ms | 48 |
| 四星 | .20/.28×70 → 停75 → .24/.34×75 → 停75 → .31/.43×85 → 停90 → .43/.51×190 | 660ms | 49 |

一星各模式默认均含轻尾脉冲；旧自定义HapticsOnly波形保留。上表为用户选择的正式v1.4候选，不采用音乐包JSON的更长时间轴。普通模式最长500ms，庆祝上限850ms，单片段最长500ms；四星660ms后不随音乐尾声持续震。非法模式字符串使对应震动静音，修改设置时记录警告。

## 音效与停止策略

Ultra Victory v3为用户提供的三套12首WAV：超燃卡通大乐队1.05/1.57/2.30/3.85秒，冠军加冕号角1.13/1.70/2.48/4.05秒，厨房摇滚派对1.01/1.53/2.35/3.80秒。原样接入，不含模拟或真实欢呼/掌声。用户允许四星完整播放；马达仍在660ms结束。旧欢乐卡通爵士素材与清脆生成音已移除，旧选择会迁移到新默认。

供包说明声明原创MIDI编排，渲染使用TimGM6mb（Tim Brechbill/David Bolton）；所附GPL/署名、MIDI与必要制作源码一并保留，见[来源记录](assets/audio/SOURCES.md)。音色库许可与渲染输出的适用边界按供包声明记录，不称CC0；项目代码许可证尚未选择。

相对音量默认1，只应用一次；游戏主音量/SFX组继续控制最终声音，0为静音，滑杆可调0–100%。普通升级保留现有自定义音量，本机用户已明确选择将约70%改100%。自有2D声源尊重游戏静音，不改BGM、原声源或Mixer。启动预载，Hook不读文件、不解码、不等待；输出在下次Update调度。缺失/损坏或无可用包时声音不可用，震动及游戏继续。新的响度、实际调音跟随与音震同步尚待用户实测。

失败/订单过期等高优先级反馈可停止奖励声音和覆盖震动；取消后不补播。音频初始化或播放失败只影响本插件音效，震动及输入清理继续。无SFX混音组时音效不可用，不绕过游戏音量。

退出、禁用、失焦、暂停、切关、切包和绑定改变会清空音震及待输出事件。手柄断连停止声音与马达任务（Update中最多每250ms查询），重连不补播；只对本插件持有的槽位归零。游戏主线程卡死、强制结束和驱动/设备故障仍不能保证及时停止。真实火灾Hook与多人功能仍待后续校准。

## 配置要点

- General：Enabled、MasterIntensity=0.75、DebugLogEvents=false。
- Devices：XInputIndex、LocalGamePadSlot，默认-1。
- Events：ThrowSuccess、CatchSuccess、DeliveryOrdered、DeliveryOutOfOrder、DeliveryUnclassified、DeliveryFailure、OrderExpired。
- Combo：Enabled、TriggerMode=EveryOrderedDelivery、MaxHapticTier=4、ComboMasterIntensity=0.85。OnLevelUp只在已知的真实等级提升时分级；关闭分级仍保留分类反馈。
- Stars.Enabled与StarFeedback.Mode；SoundPackId=turbo_swing，SoundVolume默认1为游戏SFX相对100%；旧生成音专属MaxSoundDurationMs移除。
- Patterns：各事件片段及StarMilestone1HapticsOnly。EventStrength为各事件额外倍率0–2，默认1；0不创建马达任务。BepInEx会将越界配置限制到有效范围（例如正无穷限制为2）；实际马达仍统一限幅。非法片段/负时长/超长模式拒绝。
- Diagnostics.ScoreSourceProbe控制详细快照日志；新功能所需的只读采集不依赖详细日志开关。
- Hotkeys：OpenMenu=F6、ToggleMod=F10，None禁用。F8/F9已被本机其他MOD占用。

输出相关修改立即停当前反馈。手动编辑cfg请先退出游戏，没有文件监视热重载。已有Configuration Manager可修改同一配置。

## 构建、测试与打包

开发需要.NET SDK10、本地游戏及匹配的BepInEx；安装插件无需.NET10。不下载NuGet包，引用的游戏/Unity/加载器程序集不会分发。

```powershell
git clone https://github.com/biany-captain/Overcooked2-Haptics.git
Set-Location Overcooked2-Haptics
./scripts/test.ps1
./scripts/test-config.ps1
./scripts/test-probe.ps1
./scripts/build.ps1 -GameRoot 'E:\SteamLibrary\steamapps\common\Overcooked! 2'
./scripts/package.ps1 -SkipBuild
```

不传GameRoot时从Steam库查找，也可设置OC2_GAME_DIR。Core测试不启动Unity；config/probe测试使用本机程序集和合成数据，不能代替游戏运行。输出在artifacts：OC2Haptic.dll及0.4.1-alpha安装/源码ZIP。

Git只跟踪必要源码、脚本、配置示例和需求/验证文档。`.gitignore`排除bin/obj/artifacts/.cache、游戏文件、二进制、日志及个人配置。经筛选的docs/evidence文本为验证记录。

## 回退、卸载与来源

本地已保留历史安装包和升级前DLL/配置。使用MOD管理器时，某些关闭操作仅影响下次加载；F10是即时总开关。退出游戏后删除本插件目录即可卸载，不改原版DLL/存档/其他MOD。

历史实测：[0.2.2菜单](docs/test-report-0.2.2.md)、[0.2.3数据校准](docs/test-report-0.2.3.md)、[0.3反馈](docs/test-report-0.3.md)。本地多人、在线客机、实际NGP四星、跨平台、其他版本和完整MOD兼容矩阵仍未验收。

技术参考：[BepInEx](https://docs.bepinex.dev/)、[XInput](https://learn.microsoft.com/en-us/windows/win32/api/xinput/nf-xinput-xinputsetstate)、[hpmv研究](https://hpmv.dev/docs/tech/kitchen_flow_controller/)、[Unity2017.4 PlayOneShot](https://docs.unity3d.com/2017.4/Documentation/ScriptReference/AudioSource.PlayOneShot.html)。项目许可证尚待负责人选择；不附带原版游戏或第三方库。
