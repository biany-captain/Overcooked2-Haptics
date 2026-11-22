# Test report — 0.6.0-alpha / v1.7 star candidate

2026-11-22. Incremental on e89ef4c. User approved minimum scope, supplied simple/cool, allowed existing shorter four-star options, retained100% gain and immediate stop/arbitration, multiplayer later. No new Hook/backend/runtime dependency. Original requirements/JSON preserved; chat overrides in design-star-feedback-v1.7.md.

## Software and assets

- Core121/121 PASS, evidence/core-tests-0.6.txt. v1.7 exact JSON4 profiles/categories/zero cooldown, hard1/2 silence in all modes,10.667s sound with630ms motors/ordinary action/emergency stop, same-commit dedup despite0ms cooldown, allowed custom/legacy durations, actual new PCM durations/no clipping. Final fixes cover urgent audio stop with exhausted motor budget/cooldown, hazards-off audio priority, advisory custom3 duration. Three new temporary-music-mute tests cover restore, game updates/external changes, initially muted/unavailable levels. Existing meaningful regressions retained.
- Actual BepInEx config20/20 PASS, evidence/config-tests-0.6.txt. Includes known0.5 star defaults55/145/250/630 migration and5 migration log entries, custom precision preserved, master/volume/oldpack/feature preferences unchanged, new default100% coefficients/custom selection, per-level channel gates, independent paths/selection/gain persistence. Prior migration cases remain with explicitly superseded star expectations updated.
- Actual Unity2017.4/net35/x86 build0 warnings/errors, assembly0.6.0.0; evidence/build-0.6.txt. Actual managed probe7/7, evidence/probe-tests-0.6.txt: original6 plus separate MusicVolume/SFXVolume getter literals and mixer field. This validates game metadata, not executed Unity mixer controls.
- New core/config APIs first RED(missing APIs), then focused GREEN.11s PCM boundary separately RED0/1→GREEN; migration-log contract REDmissing overload→GREEN. First fullcore115/116 exposed one old660ms/three-segment assertion; officialv1.7 JSON test already passed, updated that obsolete expectation to630ms/one-segment, final116/116. No runtime failure was hidden by relaxing the new policy.
- Supplied MP3 containers0.9404/10.7102s converted with existing offline FFmpeg7.0.2 to full PCM16 stereo48k without trimming, looping, gain/fade/time edits. Actual PCM900/10666.667ms, peaks0.2656/0.3894, no clipping. Runtime ceilings11s; custom3 range0.8–1.3s is advisory (other bounded valid PCM allowed/logged); custom4 accepts9–11s. Legacy4 originals3.8–4.05s explicitly allowed by user. All old12WAV/MIDI/source notices preserved; new two WAV have separate user-stated noncommercial condition, full source/license pending.

## Behavior and limits

Runtime/previews cannot call audio for1/2. UI gives them haptics-only tests;3/4 retain independent preview modes. Three functional switches/masters/sliders remain. Three-star file is independent; four-star dropdown selects customcool or legacy pack4. Only custom3/custom4/legacy4 are preloaded, not old1/2/3 music. Missing/invalid audio logs unavailable and leaves haptics; no oldjingle fallback. A failed configured path is isolated by the existing per-asset load boundary.

Tracker, actual thresholds/epochs/NGP gating and same-message arbitration retained. Per-level disabled/no-channel milestone is still consumed but filtered before selection so ordinary/Combo can continue. Same commit correlation keeps dedup with new0ms star cooldown. Danger/independent rejection/expiry immediately stops own music/motors and runs its existing negative/emergency cue, no resume. Existing audio-only-star replacement policy preserved; it may end a prior lower-priority cue early.

User additionally approved dangerous-event music interruption even with hazard motors disabled. This uses separate dispatch/motor gates; an exhausted motor budget/ownership/cooldown does not prevent a new eligible incident from stopping music. Duplicate incident keys still cannot interrupt a later celebration again.

User requested temporary game-BGM mute during four-star music. Actual IL shows independent MusicVolume/SFXVolume and per-frame AudioVolume.Update writes. Implemented a late-frame temporary override of MusicVolume only, restore/release on natural end/Stop/cleanup and skip if MOD/SFX audio muted. No AudioVolume.SetOption/persistent-setting writes or new game Hooks. Core simulation and actual getter metadata pass; real mixer, settings-menu interactions, SFX preservation and coexistence with other audio MODs remain native-pending.

## Native acceptance and delivery

0.5.1 new controls were accepted by explicit user report “四项均正常”; historical report/evidence retained in test-report-0.5.1.md and runtime-controls-0.5.1.txt. Logs partially corroborate toggles/previews, not every reported gameplay step. Earlier G music acceptance does not certify the new tracks or rules.

All new0.6 star/UI/Unity playback, subjective loudness/crowd content, real thresholds/NGP4, same-delivery selection and real danger interruption await user native testing, batch-test-v1.7.md. No real-game automation or physical-latency/15-minute comfort claim. Multi-controller/online/virtual-device/SDL native acceptance still deferred.

Final independent review, archives and cold-backup installation are recorded after completion. Keep existing authorized feature branch/draft PR; no main merge/public release. Personal cfg bytes and old audio remain unchanged during installation; first launch performs tested default migration.
