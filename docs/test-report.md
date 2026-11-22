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

User explicitly answered “四项均正常” to the0.5.1 native checklist: menu/F10/persistence; real-star audio-only/haptics-only and both-off ordinary/Combo fallback; hazards-off without disabling rejection/expiry; explicit-preview bypass with global-master enforcement. Record these as native acceptance by user report. The retained session independently corroborates initialization, feature toggles, manual star/fire/burned previews, clean shutdown and zero own warnings/errors, but all real-event totals remain zero; it does not independently trace every reported gameplay step. See batch-test-0.5.1.md and evidence/runtime-controls-0.5.1.txt.

Real ComboA progression/cap classifications, full fire reminder/spread/clear timing, independent failure-vs-celebration, NGP four stars, multiplayer/online/virtual-device ownership, physical latency and15-minute comfort remain limited to recorded evidence. The grouped switch user acceptance does not certify this entire matrix. SDL is still an extension contract, not a loaded native backend. Software tests do not replace hardware/game acceptance. Newv1.7 requirements are under audit; no v1.7 build or installation is claimed.

## Delivery

Independent read-only delta review of e883cee..7d4e7df found no clear Critical/Important/Minor issues. See review-feature-switches.md. Retain the existing star replacement rule: audio-only stars may end a preceding lower-priority motor task early; selective cancellation when changing a switch remains independent. Native controls were subsequently accepted by the user as recorded above.

Runtime/source ZIP checks passed: ownDLL1/sourceDLL0, unchanged12WAV/12MIDI with credits, canonical JSON and new controls design/checklist/review, no caches/game binaries. See evidence/packages-0.5.1.txt. With the game stopped, backed up the old ownDLL/cfg and installed0.5.1.0 only; installed DLL matches the verified artifact, personal cfg bytes unchanged, audio untouched. First startup performs tested configuration migration. Existing authorized feature branch/draft PR is retained; no main merge or public release. Native controls checklist accepted; broader unobserved scenarios remain separately listed.
