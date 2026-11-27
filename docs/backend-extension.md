# SDL3扩展与设备独占契约

2026-11-22。当前交付运行后端只有XInput；没有SDL原生库、SDL事件泵、Steam Input初始化或设备自动识别功能。

现有IControllerRumbleBackend保持不变，HapticEngine使用0..3逻辑输出槽。未来SDL实现可提供该接口及FeedbackCoordinator的describeDevice回调，明确返回Backend、Port、PhysicalKey与身份是否已验证；复用同一个OutputDeviceRegistry和协调器，不让第二后端绕过预算/去重/停止。

- 相同物理Key只能领取一个逻辑输出；相同后端/端口不能重复领取。
- 不同后端存在身份未验证的一方时，保守拒绝并行领取。设备同名不作为身份；XInput索引只是当前会话槽位。
- 改绑/移交前必须先停止原输出，再Release/Reset，不能同时写两个后端。绑定失败只静音，不自动尝试另一个后端补震。
- 插件不能阻止其他进程、游戏或MOD直接写同一设备。XInput虚拟双枚举也需要实机/明确映射；目前单绑定不自动广播。

SDL3官方接口提供低/高频和duration参数，每次调用替换上次震动，仍要求处理SDL事件或更新摇杆状态。未来实现需维护设备打开/关闭、移除和事件泵，以及主动归零；不能只增加一个P/Invoke就声称兼容。[SDL3官方说明](https://wiki.libsdl.org/SDL3/SDL_RumbleGamepad)

当前模拟测试验证未知跨后端拒绝、相同已知身份拒绝、不同已知身份允许，以及未来SDL描述器可走同一协调器。未验证任何真实SDL/Steam Input后端或硬件去重。
