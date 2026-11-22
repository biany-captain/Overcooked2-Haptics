# Changelog

## 0.5.0-alpha — 2026-11-22 · v1.6 candidate

- Adopt confirmed Combo A: trusted upgrades only; same-tier/capped or unclassified success uses one90ms cue. Out-of-order requires a previously confirmed combo; actual rejection remains separate.
- Import validated/generated balanced v1.6 defaults: out-of-order315ms, rejection430ms, expiry505ms, burn-warning450ms, fire820ms and470ms reminders. Negative/emergency ceiling950ms; action500ms/star850ms retained. Exact old defaults migrate; custom precision, invalid-profile mute and personal settings remain.
- Centralize routing, staleness, dedup, per-device cooldown/active-time budgets, preemption/cancellation and stop. Hooks enqueue; no stale replay. Same-message arbitration stays immediate. One team audio emission can accompany separately mapped outputs in core tests.
- Retain1–4-star haptics and all12 music files. Same-delivery star replaces ordinary/Combo; independent later rejection/expiry can interrupt celebration, as user confirmed. Positive success no longer uses a numeric-priority audio interruption threshold.
- Add inspected-build-only client fire/cooking candidate hooks, aggregate incident edges, max5-second reminder frequency, clear/cancel and applied-state probe. FoodBurned remains defaultoff. Real hook/comfort acceptance is pending; no new game mechanism or score changes.
- Default automatic throw/catch off, keep hooks/previews. Keep F6 two masters/two sliders; fine switches in cfg. Preserve gameSFX following/current relative100%, overriding stale65% source wording. Uninstalled0.4.3 left-fill/unified-motor drafts superseded by user's v1.6 dual-channel choice.
- Add future backend descriptor/verified-identity exclusivity contract and fake SDL descriptor test; no SDL native/Steam Input runtime or broad multiplayer claim. Unknown multi/online owner never gets guessed Combo.

## 0.4.2-alpha — 2026-11-21

- Simplify F6 to independent haptic/audio masters with master intensity/relative volume. Keep pack selection and all event/star previews; remove per-event toggles/gains and the redundant four-mode selector.
- Both automatic feedback and explicit previews respect the channel masters. Disabling one channel stops only that channel; enabling does not replay. F10 remains all-off/all-on, including mixed channel states.
- Retire old per-event keys and extra gains; migrate legacy combined switch/mode once, honoring explicit new keys. Preserve custom patterns, binding, master intensity, relative volume and selected pack. Keep original Combo baseline and all waveform timing/priorities.
- User confirms G star feedback/music works; record the actual0.4.1 TurboSwing1–4 preview log separately from unobserved real NGP milestones. Roadmap now maps every A–G stage and later optional scope.

## 0.4.1-alpha — 2026-11-21

- Remove both rejected shipped sound sets and generated-tone fallback. AddUltra Victory v3: TurboSwing(default), ChampionFanfare and KitchenRock,12uneditedPCM WAVs with sourceMIDI/scripts/credits.
- Preserve full3.80–4.05s four-star tracks; decoder bound5s, haptic profiles remain180/285/425/660ms with850ms ceiling. No mixed-star selector or unbundled crowd layers.
- Default relative gain100% through existing gameSFX group; keep user volume slider and game mute/master behavior. Existing custom gains remain; local user explicitly approved .7006757->1.
- Migrate only retiredpack selections; keep unknown/custom IDs, other preferences and user-owned packs. Empty catalog safely has no audio. Remove obsolete generated-tone duration key and tests tied only to the removed feature.
- Retain0.4 functional/user evidence separately from rejected audio quality. New native music/volume acceptance remains pending.

## 0.4.0-alpha — 2026-11-21

- Adopt formalv1.4 star haptics180/285/425/660ms within850ms; migrate only known old defaults, retaining custom profiles and user gains/volume/binding.
- Preload four supplied unedited PCM16 WAV jingles and retain original generated crisp rewards. Add persistent all-star sound-pack dropdown to the existing star test tab; new local pack manifests are discovered on restart.
- User approved retaining3.25s four-star audio. Include supplied GPL-2.0 notices/attribution, source MIDI and generation script, without SoundFont/OGG duplicates/cache or runtime dependencies.
- Hooks queue feedback; Update plays prepared clips and schedules motors. Higher stars replace existing celebration, failure/expiry can preempt; switching/close/pause/focus/round/disconnect/disable stop own outputs without replay.
- Include approved0.3.1 intensity controls/two tabs and short-window whole-body scroll. New game/UI/music comfort and hardware synchronization remain pending user acceptance; no real fire/multiplayer Hook is added.

## 0.3.1-alpha — 2026-11-21

- Add F6 master intensity0–100% and individual event gain0–200%, default100% retaining existing profiles. Scale uniformly within motor limits, preserving timings/balance; zero gain never owns a motor task.
- Split the existing panel into event settings and explicit star tests. Each star has audio-only/haptics-only/combined manual buttons and its own gain; auto mode is not mutated.
- Preserve existing user intensity/audio/binding values and source/counters/input/cursor behavior. New gain/mode tests and actual config integration pass; new layout awaits game acceptance.
- 0.3 actual user report/log confirms one selected feedback per delivery, Combo progression/out-of-order/recovery, star1/3 audio. Unobserved cases remain pending.

## 0.3.0-alpha — candidate delivery/Combo/star feedback

- Implement runtime-calibrated client delivery classification and authoritative ×1–×4 feedback, preserving raw streak and per-team/session state. Retain disabled legacy success preference during configuration migration.
- Add first committed-score threshold milestones from current-level data, no retroactive/repeated star celebration, highest-only for multiple crossings. Real NGP gates still determine fourth availability.
- User approved immediate arbitration within the verified single client message, replacing the proposed fixed120ms wait. One final waveform, no old generic double-pulse duplicate.
- Add original generated PCM reward sounds, independent2D source routed through game SFX mixer, four output modes and gain0.65 applied once. Audio exceptions cannot break haptics or menu/input cleanup.
- Add editable candidate patterns, approved two-part115ms catch and145ms one-star haptics-only tail; four-star default500ms. Extend the existing measured/scrolled F6 page with category/Combo/star controls, previews, audio modes and volume.
- Manual audio previews in menu/pause approved; automatic feedback is gated there. Focus/pause/scene/disable stop own outputs. No new danger hooks, multiplayer mapping, custom music or animation.
- Compilation and shared-core/config/game-data tests pass; actual0.3 interaction, audio, waveform quality and star events remain pending. Current source calibration is documented separately from new feature acceptance.

## 0.2.3-alpha — 2026-11-21 · score source calibration

- Add opt-in, read-only Prefix/Postfix capture around client score synchronization. Freeze before/payload/after score values and log actual multiplier, raw Combo, delivery metadata and observation context.
- Read current session/platform/player-count thresholds and both game's four-star NGP gates; reject invalid/mismatched threshold candidates. No hardcoded score tables.
- Add immutable threshold boundary tests. Probe failure is isolated from existing feedback.
- Preserve 0.2.2 haptic patterns and F6/F10 behavior. New classified delivery/Combo/star/audio effects remain pending actual data calibration; static inspection/build are not game verification.
- Add public README, ignore rules and latest formal v1.3 requirements to the GitHub repository. Exclude game DLLs, build output, caches and local configuration.

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
