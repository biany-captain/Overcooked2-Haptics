# Test report — 0.5.1-alpha / three feature switches

2026-11-22. Incremental change on e883cee: user explicitly selected three automatic switches (star haptics, star audio, grouped hazard haptics), preserving existing masters/sliders/previews/F10. No waveform, music, score-source or Hook change. See design-feature-switches.md.

## Software evidence

- Core110/110 PASS, evidence/core-tests-0.5.1.txt. Five new coordinator regressions cover automatic star channel isolation, selective active cancellation and no replay, grouped hazard refusal without suppressing rejection/expiry or interrupting unrelated music, explicit preview bypass with global masters, and audio-only team dedup without re-enabling motors. Existing105 tests remain.
- Actual BepInEx config18/18 PASS, evidence/config-tests-0.5.1.txt. Three new cases cover legacy Stars.Enabled migration and explicit-new-key precedence, all16 global/star channel combinations, F10/persistence, disabled-star ordinary fallback and no later milestone replay, and hazard group/individual-switch composition. Prior15 migrations remain.
- New tests first failed at compilation because the new feature APIs were absent, then focusedcore5/5 and config18/18 passed after implementation. Final full suites above passed after native integration/menu changes.
- Actual Unity2017.4/net35/x86 build0 warnings/errors; assembly0.5.1.0. evidence/build-0.5.1.txt. Source JSON/generated profiles match. No runtime dependency added.
- Existing0.5.0 managed-data/signature probe6/6 remains applicable: no Hook/signature/game-source changes this round. It was not rerun or relabeled as new native evidence.

## Behavior and acceptance

Settings adds exactly three grouped switches. Star channel preferences intersect global masters; if both effective star channels are disabled, the milestone is consumed silently and ordinary/Combo arbitration continues. Closing one feature cancels only its current channel; reopening does not replay. The hazard group includes warning/burned/fire/reminders, excludes rejected/expired orders, and preserves FoodBurned=false and other per-event preferences. Native change callbacks defer fire reminders by5s on hazard group change. Manual previews bypass feature switches, still obey masters/cooldowns/budgets. F10 changes masters only.

User accepted tested0.5.0 items and tactile feel. The saved session corroborates manual star1–4 and fire/burned previews, zero own warnings/errors, but no real event totals. Historical software/runtime scope is preserved in test-report-0.5.md and evidence/runtime-preview-0.5.txt. G music/menu acceptance from earlier versions remains valid for its original scope.

The three new controls, their layout/persistence and actual-game selective automatic channels are pending native user testing; see batch-test-0.5.1.md. Real ComboA/hazard transitions, reminder/clear timing, independent failure-vs-celebration, NGP four stars, multiplayer/online/virtual-device ownership, physical latency and15-minute comfort remain limited to recorded evidence. SDL is still an extension contract, not a loaded native backend. Software tests do not replace hardware/game acceptance.

## Delivery

Final independent delta review, package validation and installation are recorded when completed. Existing branch/draft PR is retained; no main merge or public release. Preserve personal config bytes during installation; migration occurs on first startup, without replacing the selected pack, volume, intensity, binding or custom patterns.
