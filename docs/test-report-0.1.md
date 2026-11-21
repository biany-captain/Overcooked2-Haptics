# Test report — 2026-11-21 (Asia/Shanghai)

## Evidence levels

Compilation, simulated logic, native device enumeration, game load and physical perception are separate checks.

| Check | Result | Evidence |
|---|---|---|
| First plugin compilation | PASS | net35/x86; 0 warnings, 0 errors |
| Core initial RED | Observed | 11 failures / 17 tests before scheduler/routing implementation |
| Core and boundary suite | PASS | Final suite: 22/22 via scripts/test.ps1; raw output in evidence/core-tests.txt |
| Native XInput enumeration | Executed | Initial sample: all 1167. After user confirmed connection, a new sample found slot 0 connected (code 0), slots 1–3 disconnected (1167). No vibration sent by this query. |
| BepInEx load of this plugin | PASS | Actual new-run log: Loading OC2 Haptic 0.1.0; initialized; Throw Hook installed |
| Physical F6 vibration | PASS (user observed) | User confirmed short vibration and normal stop; runtime log scheduled XInput 0 / 80ms / master 0.75 |
| Bound throw / empty-hand / disabled comparison | PASS (controlled small trial) | Actual log observed=2 scheduled=1; user confirms only initial successful throw vibrated, empty attempt and disabled throw silent |
| Pause/menu/exit/disable lifecycle | PARTIAL | F10 disable, scene transitions and normal exit callbacks observed; mid-pulse pause timing and MOD-manager hot-disable not tested |
| Package inspection | PASS (initial package) | Own DLL, readme/changelog/config example; no proprietary/loader DLLs. Final package also includes the verification/compatibility records. |
| Fresh code review | Complete, disposition recorded | See review-ab-alpha.md; two scheduler defects fixed. Physical device replacement behind an unchanged slot remains a disclosed limitation of the current single-controller flow. |
| Build process environment cleanup | PASS | Reproduced APPDATA leak, then verified caller environment restored |
| Final plugin build | PASS | 0 warnings / 0 errors after fixes; evidence/build.txt |
| Normal shutdown callbacks | Observed | Actual log includes application quit, plugin disabled, shutdown. Does not prove every mid-pulse exit timing case. |
| Long-held F6 / receiver unplug / reconnect | PASS (user observed) | User reported all three normal; log shows two test schedules total and disconnect 1167 → reconnect 0; no automatic extra test schedule |

## Automated coverage

80/65ms logical boundaries; multiplier 0.75; cooldown after pulse completion; duplicates; higher/lower priority; silent gaps; late tick no replay; zero intensity; owned-slot stop and ownership retirement; disconnect detected during Tick or Play (including cooldown); invalid indices and nonfinite inputs; bounded patterns; exact manual mapping; keyboard/remote/unknown/intent rejection; native boundary guards.

The fake backend records requested output. These tests do not measure motor response, frame timing or Steam Input interference.

## First game smoke evidence

User launched the game manually after stopping UI automation. The actual BepInEx log contains plugin initialization, successful client-flight Hook installation, XInput 0 connected, test pulse scheduling and shutdown callbacks. The user reported “短震并正常停止”. Filtered evidence is saved in `evidence/runtime-f6-smoke.txt`. Actual physical duration was not instrumented; only the target 80ms and perceived short pulse were verified. No successful throw occurred in this first run (observed=0, scheduled=0).

## Unbound real-throw probe

The next run captured five local hand-controller flight events: player=0, gamePad=0, keyboard=False, allowed=True, routed XInput=-1 and scheduled=0 because binding was intentionally absent. The user said actual counts differed from the proposed two/two and that successful throws preceded empty-hand attempts. Exact counts are unknown, so event accuracy is not claimed from this run. Filtered evidence: `evidence/runtime-throw-probe.txt`.

With the game exited, LocalGamePadSlot was set to 0 using this observed identity; XInput 0 was independently detected as the only connected output device. The controlled bound throw / empty-hand / disabled comparison was then performed as recorded below.

## Bound acceptance comparison

The user followed the bound trial and reported “只有第①步短震，空手和关闭后都不震”. Actual runtime logs show the first throw accepted (observed=1/scheduled=1), F10 changing Enabled to false, the next actual throw rejected by the enabled gate (observed=2/scheduled=1), then normal shutdown. There is no intermediate empty-hand flight event. Evidence: `evidence/runtime-bound-acceptance.txt`.

This is a small successful single-controller trial, not a broad false-positive/false-negative rate measurement. The plugin was re-enabled after the explicit disable test. No feedback parameters changed.

## Device safety trial

User reported “三项均正常” for holding F6 about two seconds (one short pulse), unplugging the 2.4G receiver and attempting F6 (game remained normal), reconnecting and waiting (no automatic replay), then pressing F6 (normal short pulse). The log records exactly two scheduled tests in the run, XInput 0 disconnected with code 1167 and reconnected with code 0, followed by normal shutdown. Evidence: `evidence/runtime-device-safety.txt`.

Final installed profile: Enabled=true, MasterIntensity=0.75, XInputIndex=0, LocalGamePadSlot=0, DebugLogEvents=false. All changes affect this plugin's own configuration only. The distributable default remains unbound (-1) for other installations.

## Remaining limits

Nonthrowable items, collision-blocked failures, keyboard actions in the actual game, switching chefs, mid-pulse pause timing, MOD-manager live unload, Steam Input on/off comparison, other versions/devices, multiplayer and broad performance/error-rate measurements are unverified. Rapid physical device replacement behind an unchanged XInput slot cannot be identified. No claim is made for those cases.

## Manual acceptance procedure

Follow README initial binding steps. Record actual device name, connection mode, firmware/driver where available, Steam Input on/off and active MOD set. Count ten successful throws and ten failed/empty attempts separately; record observed logs, scheduled pulses and felt pulses. Verify long-held F6, F10 disable, master zero, unplug/reconnect, pause/resume, menu return and normal game exit. Report the host/client role separately if testing network play.

The first A+B runtime acceptance requires menu load, selected-device F6, a correctly attributed successful throw, no empty-button false positive and stop transitions. Any missing item remains pending.
