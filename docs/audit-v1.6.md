# v1.6 增量审计（2026-11-22）

基线：a79b635，已安装0.4.2；用户确认该菜单/两个总控和G星级音乐正常。工作区另有本助手未提交、未安装的0.4.3左电机候选；它由本次v1.6设计取代，不作为已交付功能。

## 已有实现

- 客户端投掷StartFlight、服务端接住HandleCatch、客户端成功/失败/超时Hook存在且有前期单手柄证据。客机接住未验证。
- ApplyServerEvent前/载荷/后比分不可变快照校准了当前MVID114d606a-29d5-472c-8ebe-c2c20fbe3be3；倍率来源是真实TotalMultiplier，streak单独保留。不是按成功次数推导。
- 同一已验证客户端消息已包含交付、倍率与最终分数；星级或Combo/普通上菜只选一个。没有真实重叠震动证据，保留此路径立即仲裁，不额外增加固定80ms；异步来源如出现需单独校准。
- HapticEngine已有按槽位独占、冷却、抢占、主动归零。RewardOutput已有星级音乐、替换、失败/超时停止、自有音源与游戏SFX组路由。
- 缺少：每设备分类预算、普通过时事件丢弃、跨后端身份独占、危险状态跟踪；旧队列未加锁。旧500ms普通模式上限不能承载505ms超时/820ms起火。

## 已发现的语义和优先级问题

- 旧Combo默认EveryOrderedDelivery；OnLevelUp逻辑已存在但未默认采用。旧乱序判定只看本次wasCombo=false且倍率/streak为0，缺少之前确有Combo的约束。
- 新JSON普通成功优先级55，旧音效抢占规则priority>=55会误停星级音乐，需改为明确负面/危险类别。
- JSON星级统一priority86，必须另外用星级等级判断更高星替换，不能只依赖旧46..49的优先级递增。
- 成功、失败、超时fallback Hook与比分捕获必须共享所有权，捕获可用时不再重复提交fallback。

## 实际程序集危险路径（仅静态证据）

- ClientFlammable.ApplyServerEvent(Serialisable)读取FlammableMessage.m_onFire并调用Ignite/Extinguish；OnDisable/OnDestroy也走Extinguish。候选监听成功后的Ignite与Extinguish，不监听每帧强度变化。
- ClientCookingHandler.ApplyServerUpdate(Serialisable)读取CookingStateMessage.m_cookingState并缓存。枚举含Idle/Progressing/Completed/OverDoing/Ruined；相同状态不重新发送回调。
- 食物已烧焦仍默认关闭，独立客户端状态和实际触发/重置行为须游戏验证。起火、过度烹饪的自动反馈为待实机验证候选，不宣称已验收。

## 路由和后端

- KitchenFlowMessage只有team/order/plate station，缺少直接厨师身份。用户确认：只在能证明同队玩家共用一个本地输出设备时保留当前反馈；多人/在线未知上菜者时Combo静默，普通交付只在显式配置允许队伍回退时提醒，不自动广播。
- 当前仅黑武士5Pro、2.4G XInput、Pad0→XInput0有实机证据。IControllerRumbleBackend可复用；SDL3没有运行模块。只设计独占绑定契约，不引入SDL DLL/驱动或Steam Input第二输出路径。
- 插件内的身份不明设备不得与另一后端并行激活；不能用同名认定同一物理设备，也不能承诺阻止其他软件写马达。

用户新确认：v1.6高低频区别替代此前两路同值提案；菜单保持两总控/两滑杆，细项只在cfg。音量保留已明确的跟随游戏主/SFX、相对100%及当前个人值，不倒退到文档旧65%。

Git提交/推送上一轮被自动审批额度限制阻止，动作未执行；不绕过审批。先完成本地代码/测试/打包，最终再报告实际可交付状态。
