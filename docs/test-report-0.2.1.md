# Test report — 0.2.1-alpha (2026-11-21, Asia/Shanghai)

Subsequent visual correction: user supplied a screenshot showing clipped explanatory text and distracting background transparency. Functional menu checks below were user-reported passes, but visual layout required the0.2.2 fix. See menu-layout-0.2.2.md.

| Check | Result | Evidence |
|---|---|---|
| Core menu/haptics/input ownership suite | PASS40/40 | evidence/core-tests-0.2.1.txt |
| Production config migration integration | PASS4/4 | evidence/config-tests-0.2.1.txt |
| Plugin build | PASS | net35/x86,0 warnings/errors; evidence/build-0.2.1.txt |
| Fresh review | Findings fixed | review-menu.md; ownership/cleanup and explicit None cases reproduced RED then GREEN |
| Actual 0.2 game events | Historical PASS | User confirmed all four; runtime catch2/success1/failure1/expiry1; test-report-0.2.md |
| 0.2.1 game load/menu rendering | PASS by user observation | Log proves version0.2.1/menu opened; user reported menu checks normal; no assistant screenshot captured |
| Global/event switch persistence, F10 sync | PASS by user report | Shared ConfigEntry inspected, settings-change log and saved cfg observed; user reported checks normal |
| Actual cursor/local-input recovery | PASS by user report | Tracker regression tests pass; user reported actual checks normal, not instrumented movement/cursor measurements |
| Paused-menu preview stops | PASS by user report | User reported paused checks normal; core timing/zero tests and realtime ticking inspected. Preview log alone does not prove pause state or physical duration |

## Test boundaries

Core tests use the actual production profiles/scheduler and a fake device boundary. Config integration source-links production HapticConfig and loads the installed BepInEx ConfigFile/Unity enum, writes isolated fixture files, and never starts Unity or sends vibration. No third-party test runtime DLL is packaged.

Menu previews are explicitly manual, require global enable, and do not enter game-event counters. Individual automatic-event switches do not block a manual pattern preview. Profiles/priorities/intensity/binding remain unchanged. Actual game event output still respects pause and enabled gates.

## Remaining acceptance

Follow menu-test-0.2.1.md: Chinese layout, F6 no automatic pulse, generic/five previews, global/F10 sync, event-off actual gameplay, local input recovery and paused previews. Do not infer these from compile/test success. Guest-client/multiplayer/other devices remain outside current verified coverage.

## User acceptance and runtime evidence

User answered “各项均正常，菜单已留在屏幕上” to the complete joint menu checklist. Current log shows OC2Haptic0.2.1 initialization, menu open with no automatic vibration, accepted Generic80ms and all five per-event previews, plus output-setting changes. Saved cfg contains OpenMenu=F6, ToggleMod=F10, no legacy TestRumble, intensity0.75 and the existing binding; final general/event switches were all true. Manual preview lines are separate from actual game-event counters.

Filtered evidence: evidence/runtime-menu-0.2.1.txt. Two read-only screenshot attempts found the game window minimized; no activation/click/input was performed. Therefore appearance/input/paused-perception acceptance is attributed to the user, not an assistant visual or instrumented verification. Existing other-version/network/device limits remain.
