# Changelog

## 0.2.2-alpha — 2026-11-21

- Fixed screenshot-confirmed clipping caused by fixed-height Chinese explanatory labels.
- Menu text uses actual GUIStyle.CalcHeight; sections flow vertically with measured gaps and content-sized window height, while smaller screens scroll the event list.
- Added a near-opaque dark panel and scoped GUI tint restoration so scene graphics do not compete with text.
- Haptics, switching, input ownership and config behavior unchanged. Build0 warnings/errors; user reloaded and confirmed text complete/obstruction resolved.

## 0.2.1-alpha — 2026-11-21

- F6 now toggles an in-game Chinese management menu instead of directly vibrating.
- Added the global switch synchronized with F10, five existing-event switches, generic80ms and per-event manual previews.
- Menu stays accessible when global vibration is off; previews respect master enable and real-time stop while game paused.
- Added scoped local input suppression/cursor restore and ownership reconciliation; remote/other-pad controls are not acquired.
- Migrated legacy test shortcut to OpenMenu while preserving explicit None/custom/existing settings.
- 40 core tests and 4 actual ConfigFile integration tests pass; net35/x86 build0 warnings/errors.
- User reported menu/preview/switch/input-recovery/paused-preview checks normal; runtime log confirms new menu and all preview types, and cfg confirms OpenMenu=F6 / ToggleMod=F10 migration.

## 0.2.0-alpha — 2026-11-21

- Added the approved batch: catch, delivery success, delivery failure and order expiry.
- Added exact approved event profiles, per-event configuration/counters/fault isolation and local-team filtering.
- Catch Postfix verifies actual carried item; kitchen results patch only the client base layer once.
- Preserved tested throw profile, F6/F10, one-controller mapping, output ownership retirement and stop behavior.
- 31 automated tests pass; net35/x86 build has 0 warnings/errors. User confirmed all four new events work; runtime log schedules catch2/success1/failure1/expiry1 with matching local team.
- Kept 0.1 packages and historical test evidence for rollback/reference.

## 0.1.0-alpha — 2026-11-21

- Added single-plugin net35/x86 build for the installed BepInEx 5 environment.
- Added explicit local-game-pad / XInput binding; no default guessed mapping.
- Added F6 80ms test and F10 toggle, master intensity 0.75.
- Added one read-only client-flight Postfix candidate for chef throw feedback (65ms, 120ms cooldown).
- Added scheduler, priority interruption, stale-event dropping, lifecycle stop paths and native-error isolation.
- Added offline automated tests, build/package scripts and initial Hook/compatibility/test records.
- Verified actual game load and F6 short pulse/stop with user observation on 黑武士 5 Pro / 2.4G XInput.
- Verified a controlled bound trial: first successful throw vibrated; empty-hand attempt did not create a flight event; F10-disabled throw stayed silent (observed=2/scheduled=1).
- Verified long-held F6 produces one pulse, receiver disconnect does not crash the game, and reconnect does not replay an old pulse; next explicit F6 works (user observation plus native transition logs).
- Broader throw-failure cases, other versions/devices, pause timing, MOD-manager live toggles and multiplayer remain unverified; see the test report.
