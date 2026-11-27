# Compatibility matrix

Version note: 0.1 baseline and all four 0.2 added events have user/runtime evidence. The0.2.1 management menu passed code/config checks and user-reported menu/interaction acceptance; runtime logs confirm menu/previews/key migration. No assistant screenshot was captured because the window was minimized. See test-report.md for evidence levels and remaining coverage.

| Environment | Build/static inspection | Game runtime | Physical rumble |
|---|---|---|---|
| Steam Windows x86, build 20236421, Unity 2017.4.8, BepInEx 5.4.22.0 | PASS | Load / local throw / empty-hand / F10-off comparison PASS (small trial) | F6 and bound throw user-confirmed PASS |
| 黑武士 5 Pro / 2.4G XInput | User-confirmed target | Slot 0 detected; receiver disconnect/reconnect observed | F6 short pulse/stop, long hold and no reconnect replay user-confirmed |
| Existing DIYLevel / HostUtilities / other installed MODs | Original baseline has loader-version warnings | Small controlled throw trial works with installed set; broader coexistence pending | F6 and throw PASS |
| Existing Configuration Manager 19.1 | Installed; standard Config.Bind used | UI compatibility pending | Not applicable |
| Steam Input on/off | No assumption | Pending | Pending |
| Keyboard input | Core routing rejects | Pending | Must remain silent |
| Multiple local controllers / mixed input | Deferred by user | Unverified | Unverified |
| Joined online client, one local controller/chef |0.6.3 personal actions/shared alerts compiled;144 core/20 cfg/11 API+projection |0.6.2 request Hook ran;40 personal accepted outputs,177 remote successes silent. New0.6.3 shared scope pending |0.6.2 user confirms own delivery rumbles and peer routine delivery silent;0.6.3 team stars/negative/danger and controlled handoff remain pending |
| Online host / multiple local outputs | Deferred | Unverified | Unverified |
| Epic / Proton / other devices | Outside first test scope | Unverified | Unverified |

Existing loader-version warnings alone do not establish broken compatibility. This plugin does not upgrade or replace existing dependencies.
