# Test report — 0.2.3-alpha source calibration

Date: 2026-11-21. Current environment: Steam build20236421, x86 / net35, existing BepInEx5.4.22.0. This is the read-only data-source phase of requirements-v1.3, not completion of the new reward features.

## Verified in software

- Core suite44/44 passes, including4 new score-threshold boundary cases. The new cases were first RED because ScoreThresholds did not exist, then GREEN.
- Actual BepInEx configuration migration suite4/4 passes.
- Probe data integration suite5/5 passes using actual game data types/getters with synthetic inputs, without starting Unity. Review defects were reproduced as1/5 RED, then fixed to5/5 GREEN.
- Plugin compiles against current local game assemblies with0 warnings/0 errors.
- Existing throw/catch/delivery/failure/expiry profiles, engine behavior, F6/F10 and menu logic were not changed in this phase.
- Read-only IL inspection confirms client score copy precedes delivery callbacks, HUD consumes TotalMultiplier, and current scene variant / NGP gates feed game's star calculation. This is static evidence only.

Commands: `scripts/test.ps1`, `scripts/test-config.ps1`, `scripts/test-probe.ps1`, `scripts/build.ps1`. Filtered output: evidence/core-tests-0.2.3.txt, evidence/config-tests-0.2.3.txt, evidence/probe-tests-0.2.3.txt and evidence/build-0.2.3.txt.

Fresh review found missing directory/PC-boundary paths and a suppressed mismatch log. Fixed in one pass with actual game-data regression cases. See review-score-probe.md. Metadata lookup matches the game's selected-variant identity algorithm while treating missing entries as unavailable. These tests do not exercise native Unity scene state or Harmony invocation.

## Pending actual game validation

- Probe Patch loading and before/payload/after score agreement.
- Map thresholds vs selected variant and actual star calculation.
- First ordered delivery, subsequent1–4/5/6 tiers, out-of-order/recovery, failure and expiry semantics compared with HUD.
- Four-star NGP environment, other modes, local multiplayer and online clients.

Use calibration-0.2.3.md. The probe emits no new haptic/audio events. Classification, ComboTracker, StarMilestoneTracker, reward audio and candidate waveform changes follow calibration; they are not claimed implemented or tested here.

## Previous real evidence

Verified0.2.2 remains the rollback baseline. See test-report-0.2.2.md, test-report-0.2.1.md, test-report-0.2.md and test-report-0.1.md for user-reported perception, menu acceptance and filtered runtime evidence.

## Actual runtime capture — 2026-11-21

User reported roughly5 ordered,1 out-of-order and1 failed delivery. Actual0.2.3 log captured6 ordered deliveries, then1 successful out-of-order and1 failed callback. All8 payload/after samples agree on main thread1. Filtered evidence: evidence/runtime-score-calibration-0.2.3.txt.

| Delivery | Raw multiplier after | Raw TotalCombo after | Result |
|---|---:|---:|---|
| 1–4 ordered | 1/2/3/4 | 1/2/3/4 | wasCombo=True |
| 5–6 ordered | 4/4 | 5/6 | Streak retained beyond tier ceiling |
| Out-of-order success | 0 | 0 | wasCombo=False, tip20 still positive |
| Failed delivery | 0 | 0 | Already at0 before failure; this sample does not prove a high-Combo reset on failure |

Current tutorial scene had selected thresholds20/40/60 and raw fourth600; both NGP gates false, so fourth unavailable. These are dynamically read current-level values, not a universal table. User did not provide independent map/HUD numeric comparison; that remains a joint-test item. Recovery and expiry were not present in this sample. Actual audio/new waveform behavior is not part of this0.2.3 validation.
