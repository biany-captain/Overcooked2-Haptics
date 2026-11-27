# Test report — 0.3.0-alpha candidate feedback

Date:2026-11-21. Current Steam default build20236421, x86/net35, existing BepInEx5.4.22.0. Requirementv1.3, plugin0.3.0-alpha.

## Software verification

- Shared core65/65 passes. New cases cover authoritative Combo, raw streak/ceiling, duplicates/reused IDs, pre/post state and mode behavior; exact star crossings/midjoin/restart/team/threshold changes; single-result arbitration; editable patterns; bounded original PCM; audio modes/failure isolation; owned capture cleanup and interruption.
- Actual BepInEx configuration integration6/6 passes, preserving legacy disabled success preferences and explicit category overrides alongside shortcut migration.
- Actual managed game-data integration5/5 passes. Uses synthetic data and real data types/getters, no Unity session.
- Actual net35/x86 build0 warnings/errors, assembly0.3.0.0. References existing local Unity AudioModule; no new runtime dependency.
- Audio-throw regression first failed0/1 then passed1/1. New pre-commit state case first failed missing API then passed. Core feature cases were initially RED on missing implementation.

Evidence: evidence/core-tests-0.3.txt, config-tests-0.3.txt, probe-tests-0.3.txt, build-0.3.txt. These checks do not prove native Unity audio/Harmony rendering or physical timing.

## Actual source evidence, separate from new feedback

The user's0.2.3 test provides8 applied client snapshots: six ordered1/2/3/4/4/4, raw Combo1..6, then out-of-order success with positive tip and one failed delivery. Current tutorial threshold20/40/60, fourth disallowed. See test-report-0.2.3.md and evidence/runtime-score-calibration-0.2.3.txt. No independent HUD/map numeric report was provided.

The pure tracker replays that actual score sequence as star1 then star3 because a single delivery crosses2+3. This is simulated replay, not a live0.3 star test.

## Pending0.3 real acceptance

- New source capture Finalizer and immediate classification/arbitration in a running game.
- F6 extended layout/input/cursor behavior, all profiles and sound modes/volume/mute.
- Physical catch/tier/star distinction and comfort; real one-to-three-star crossings, reset and no duplicate/replay.
- Failure/expiry interrupt audio, focus/pause/menu/scene/disable cleanup.
- Actual recovery and high-Combo failure/expiry semantics; user first calibration did not exercise these transitions.
- Actual four-star NGP context, multiplayer/online, other branches/devices/MODs and blind-test matrix remain unverified.

Joint steps: batch-test-0.3.md. Historic five-event/menu real acceptance remains in the0.1/0.2/0.2.1/0.2.2 reports. Candidate hand-feel is not fixed as final and will change only after discussion.

## Final review correction

Same-round history-loss combinations were reproduced and corrected (0/2→2/2); round-boundary continuity also passed. History survives F10/binding/distrust/threshold changes and clears at true round boundaries. See review-v13.md. Deferred Minor: startup-invalid pattern text is safely muted but lacks its initial warning until an output setting changes.

## Actual0.3 user run

User reported generally no problems and only one feedback per delivery, then requested direct intensity controls and more visible star tests. Actual log confirms loaded0.3.0, no capture/audio failure in selected entries, normal shutdown, ordered1→2→3→4, out-of-order and recovery to1, then a new round with star1 and star3 replacing the matching deliveries. Audio clips1/3 accepted at user-selected gain0.7006757.

Final observed/scheduled: throw15/15, catch2/2, failed2/2, ordered2/2, out-of-order1/1, Combo2=1/1, Combo3=2/2, Combo4=1/1, star1=1/1, star3=1/1. Legacy DeliverySuccess0, expiry0, real stars2/4=0. Manual logs contain top action/generic tests but no star/Combo preview, consistent with the discoverability issue. Filtered evidence: evidence/runtime-feedback-0.3.txt.

This supports the current single-controller normal-mode path and the user's broad experience report. It does not certify unobserved expiry, star2/4, NGP, all mute/focus cases, blind identification or multiplayer. No blanket acceptance added.
