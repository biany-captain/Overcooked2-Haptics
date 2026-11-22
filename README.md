# OC2 Haptic — 0.6.0-alpha · v1.7星级候选

Windows《胡闹厨房2》XInput触觉增强MOD。在原项目上增量开发，保留Combo A、长低频负反馈、危险协调器、F6/F10和已验收的精简控制。

**当前状态：0.6.0软件核心121/121、实际配置20/20、实际数据/音乐参数探针7/7，Unity/net35/x86编译零警告/错误。新星级规则、音乐与临时BGM静音等待真实游戏/手柄验收。** 0.5.1三个功能开关四项测试已有用户报告通过，相关历史证据保留。模拟、配置结构检查和编译不等于Hook/硬件通过。

[仓库](https://github.com/biany-captain/Overcooked2-Haptics) · [v1.7需求](docs/requirements-star-feedback-v1.7.md) · [已确认增量方案](docs/design-star-feedback-v1.7.md) · [审计](docs/audit-star-feedback-v1.7.md) · [报告](docs/test-report.md) · [测试清单](docs/batch-test-v1.7.md) · [路线图](docs/roadmap.md)

## 星级反馈

| 本局同队首次真实达线 | 震动候选总长 | MOD音效 |
|---|---:|---|
| 一星 | 55ms轻单段 | 强制无声，手动测试也不播旧音效 |
| 二星 | 145ms轻双段 | 强制无声 |
| 三星 | 250ms短/长双段 | simple，实际PCM900ms |
| 四星 | 630ms开头庆祝 | cool，实际PCM10667ms，不循环 |

四星默认使用用户提供的完整cool录音。用户另明确允许复用原三套四星曲，作为下拉可选项保留原长3.80/3.85/4.05秒，不拉伸、不冒充10秒。三星独立使用simple，不随四星选项变化。所有旧12WAV原样保留；旧一到三星曲不再自动播放。

使用当前关卡/模式/人数的真实分数门槛，无合法四星门槛时不触发四星。每局每队每星只提醒一次；掉分回升不重复，中途加载先建立基线，一次跨多星只选最高。星级是当前分数达线提示，不保证最终结算星数。

同次上菜只选择星级、Combo或普通反馈中的一种。已验证同步消息立即仲裁，无统一80ms等待。音乐与马达分离：四星震动630ms后释放手柄，音乐继续，普通/Combo可继续反馈。起火等危险及之后独立拒单/超时沿用立即停庆祝音震、执行负反馈；停止后不补播。保持原星级优先策略：仅音效星级也可能提前结束上一段较低优先级震动。

四星音乐播放时临时静音游戏BGM，保留游戏音效；自然结束、危险/失败中断、暂停/失焦/切关/关闭插件时释放覆盖，恢复音乐状态。只操作实时MusicVolume参数，不写保存选项。三星/纯试震/无声四星不静音BGM；游戏SFX总线已静音时也不额外静音BGM。该功能仍需实际Mixer运行验证。

## 原有触觉与设备

- Combo A仅可信升档到×2/3/4时使用专属235/375/490ms节奏；同档/封顶为90ms普通成功。
- 乱序但成功315ms、真正拒单430ms、订单已超时505ms、近烧焦450ms、起火820ms、持续火警470ms保持v1.6；火灾归并事故，蔓延不逐火焰连震，提醒至多每5秒一次，扑灭取消。
- 投掷/接住/已烧焦自动默认关闭，保留Hook和手动测试；FoodBurned须有独立实际证据与确认后才启用。危险Hook是当前检查版本的客户端状态候选，完整实机时序覆盖仍单列。
- 标准XInput低/高两路命令，不代表物理频率。只乘总强度，新用户0.75，保留个人值。每段500ms，普通总长500ms、星级850ms、负面/危险950ms上限。
- 协调器统一映射/去重/冷却/预算/抢占/停止；10秒内非零排程预算普通1200ms、负面2200ms、紧急2500ms，是排程估算而非物理能量。主线程卡死/强杀/驱动异常不保证物理归零期限。
- 本机曾验证黑武士5Pro、2.4G XInput、Pad0→XInput0。设备索引与游戏Pad独立，默认均-1；不猜未知操作者，不广播。当前可靠单输出优先，本地多手柄/在线/虚拟设备仍后置专项。SDL只有扩展/独占契约，无原生模块或新驱动。

## F6与配置

F6打开中文菜单，不自动暂停，只临时屏蔽绑定厨师。关菜单恢复操作；关卡仍运行，可先手动暂停。F10任一全局通道开启时全关，两路都关时全开，保留功能偏好。暂停/失焦/切关/断连/卸载停止自身输出，无旧事件补播。

- 两个总控：震动＋总强度、音效＋相对音量，滑杆0–100%；默认相对100%，跟随游戏主音量/SFX，保留个人值。本轮按用户选择不采用文档65%。
- 三个自动功能开关：星级震动、星级音效、危险预警震动。危险组不含拒单/订单超时，仍服从逐事件cfg；关闭组仅禁止危险马达，真实危险仍停止庆祝音乐，马达预算不足/冷却也不挡住停音。开组不会启用默认关闭的FoodBurned。两项星级通道都关闭时普通上菜/Combo继续；只开星级声音时一二星无可用星级输出，也回到普通仲裁。
- 「设置」保留各事件试震；「星级测试」一二星只试震，三四星保留试音/试震/同时测试及四星音效下拉。手动测试绕过功能/事件开关，但服从全局总控、预算与冷却。
- `General`：HapticsEnabled、AudioEnabled、MasterIntensity；`Stars`：HapticsEnabled、AudioEnabled和细项Star1Enabled～Star4Enabled；`Hazards.HapticsEnabled`；`Combo.Enabled`固定方案A。
- `StarFeedback.Star3AudioFile`、`Star4AudioFile`为独立自定义PCM WAV路径，默认相对插件audio目录，也可用绝对路径。修改路径后重启。`Star4Selection=custom-v17`用cool，或选择旧pack ID。旧SoundPackId记录保留，已不决定三星/四星选择。
- `Star3Volume`、`Star4Volume`独立系数默认1，与SoundVolume只各乘一次。一二星声音硬性禁止；未知包/缺失/无效文件仅静音音效并记日志，不回落旧三星或空clip。
- `Patterns`可编辑波形，严格校验范围/时长；只精确迁移已知旧默认，自定义精度/无效原文/强度/音量/绑定保留，迁移写日志。编辑cfg先退出游戏，菜单即时保存，无文件监视热重载。

## 安装、开发与来源

正常退出游戏，保留BepInEx和其他MOD，将包内BepInEx/plugins/OC2Haptic复制到游戏对应位置。本轮新安装需要两个新WAV，只替换DLL会缺少默认音乐。不要用示例cfg覆盖个人配置；更新前冷备份自身DLL/cfg。默认绑定为-1，必须明确配置Devices.XInputIndex和LocalGamePadSlot，再用F6确认目标。

当前开发环境WindowsSteam默认x86、Unity2017.4.8、BepInEx5.4.22.0。开发需要.NET SDK10和本地匹配游戏程序集；运行不需要.NET10、FFmpeg、Python或新原生库。PCM支持单/双声道44.1/48kHz、16bit、有界11秒。只监听事件，不改原游戏音乐流、分数、订单、存档或网络。

```powershell
./scripts/test.ps1
./scripts/test-config.ps1
./scripts/test-probe.ps1
./scripts/build.ps1 -GameRoot 'E:\SteamLibrary\steamapps\common\Overcooked! 2'
./scripts/package.ps1 -SkipBuild
```

v1.6 JSON生成非星级默认；v1.7 JSON保留原文并逐段测试星级默认，运行仍使用BepInEx cfg，不新增JSON解析库。源码不分发游戏DLL、个人cfg、缓存或ZIP。退出后恢复冷备份DLL/cfg可回退；其他MOD/存档不变。

旧三套音频的GPL/SoundFont记录见[旧来源](assets/audio/SOURCES.md)。新simple/cool由用户提供并允许公开，用户说明仅非商用，来源链接与完整许可待后补；见[单独声明](assets/audio/user-v1.7/SOURCES.md)，不自动套用旧GPL或宣称CC0/商业使用授权。代码许可证仍待负责人选择。PR保持草稿，未合并main。
