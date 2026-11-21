# F6 menu independent review and fixes

Fresh read-only review found one Important input-ownership issue and one disabled-shortcut migration issue. No second review pass is used; fixes are verified with regressions.

## Input ownership (fixed)

Original menu update only acquired tokens until Close. A chef reassigned to another pad/remote ownership could remain suppressed. Added MenuInputScope used by the real UI: each update independently builds currently eligible local bound chefs, releases retired participants, retains unchanged tokens, then acquires new ones. Clear releases all remaining tokens even if one external release fails, then surfaces the error. Cursor restoration remains in a finally block.

Regression tests reproduced missing release on ownership change and failed cleanup after one release error, then passed after correction. The unchanged-participant case ensures no per-frame token churn.

## Disabled shortcut migration (fixed)

Initial migration conflated missing TestRumble with explicitly configured None. Regraded as Important because migration must retain a user's explicit disabled-hotkey preference. Default legacy binding is now F6 and its actual value seeds OpenMenu, so missing→F6, None→None, custom→custom; an existing OpenMenu value wins.

An integration project source-links the production HapticConfig and uses the installed BepInEx ConfigFile/Unity enum without starting Unity or accessing devices. It reproduced 3/4 passing with explicit None incorrectly becoming F6, then passed all4 cases after the two-line correction. Test dependencies are development-only and excluded from packages.

## Not judged / runtime acceptance

Chinese rendering/layout, actual local input/cursor recovery and paused-device preview still need game observation. Reviewer confirmed the plugin keeps realtime ticks during game pause, real events retain the pause gate, F6 is processed before global-disable gate, F10/UI share Enabled, and manual preview does not enter actual-event counters.
