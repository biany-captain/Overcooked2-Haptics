# Test report — 0.4.2-alpha simplified controls

2026-11-21. User approved two independent master switches plus master intensity/relative volume, removal of individual event switches/gains and four-mode UI, retention of all previews/packs, and F10 all-off/all-on. No new Hook, waveform timing, priority, music edit or multiplayer feature.

## Verification

- Core83/83 PASS, including three new regressions first observed RED0/3: actions/explicit previews cannot bypass channel masters; haptics-off leaves current music playing; audio-off leaves current motor task running. Re-enabling does not replay. Existing routing, cooldown, queue, milestone and replacement tests remain green.
- Actual BepInEx config13/13 PASS. New keys initially caused expected missing-API RED. Check legacy modes/global-off, explicit new-key precedence, F10 from a mixed state then all-on, persistence, retired event switches and gains removed, custom precise patterns/master/relative volume/pack/binding/hotkeys retained. An intermediate test oracle mistakenly matched TriggerMode when checking retired Mode; correcting the match to the complete key line fixed that test, with no production change.
- Actual game-managed-data probe5/5 PASS; score/classification/threshold sources unchanged.
- Actual Unity2017.4/net35/x86 build0 warnings/errors, assembly0.4.2.0; no new dependency.

Evidence: evidence/core-tests-0.4.2.txt, config-tests-0.4.2.txt, probe-tests-0.4.2.txt, build-0.4.2.txt. These checks do not execute the Unity menu or prove device behavior.

## Behavior and migration

General.HapticsEnabled and AudioEnabled control both automatic and explicit output. Existing internal enum is derived from these flags, not an editable third control. Disabling one flag stops only that channel, clears pending feedback and does not re-poll/reset the device into stopping the other channel. Both disabled suppress output while score snapshots continue consuming crossed milestones. F10 turns all-off if either channel is on, otherwise all-on. Old combined Enabled and star Mode migrate once; explicit new keys win. Extra EventStrength and per-event switches are removed; custom waveform values remain. Original fixed0.85 Combo baseline is retained, not an extra user multiplier.

Menu retains two tabs, all event/star previews and the whole-pack dropdown; corresponding disabled-channel buttons are unavailable. Existing pause/focus/menu-close/scene/device cleanup remains. No arbitrary external programs, native UI automation or game mechanism changes.

## Existing acceptance and remaining work

User now explicitly confirms G star/music system works. Native0.4.1 log shows12 clips prepared and TurboSwing1–4 combined previews, with clean shutdown; see test-report-0.4.1.md and evidence/runtime-feedback-0.4.1.txt. Prior0.3 real-event evidence remains valid for recorded cases. Do not infer realNGP4, every style, blind-listening accuracy, hardware synchronization or all compatibility cases from manual previews.

Current0.4.2 menu layout, independent controls and physical behavior: pending user test via batch-test-0.4.2.md. D cooking/burning/advance warnings unimplemented; E multi-controller/online deferred. Known startup invalid-pattern warning omission remains; rejected waveform still mutes.

## Delivery

Candidate review, ZIP inspection, installation and remote push will be recorded after their actual checks. Original requirement snapshot remains unchanged; docs/roadmap.md maps A–G progress and later confirmed overrides.
