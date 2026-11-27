# 0.4.0 sound-pack review and rulings

2026-11-21. One fresh read-only reviewer checked a9f4f5e..276231c, against the v1.4 design and user-confirmed exceptions. No game interaction, branch mutation or broad test rerun by reviewer. No Critical; two Important and one Minor found. Executor made one material fix pass, no second review.

## Important findings fixed

1. Default migration compared PatternCodec.Format output rounded to three decimals, so custom0.0504/0.2204 could be overwritten as0.05/0.22 old default. Root agreed. Config regression first failed9/10; migration now compares complete parsed float values, segment count and duration without rounding.10/10 config tests pass. Both general one-star and haptics-only custom strings are retained.
2. A higher star with gain0 could replace audio while the engine rejected its motor pattern and left the lower-star task active. Root also identified this case and reviewer confirmed it in memory. Regression first failed0/1; accepted star replacement now cancels only that output's prior owned task before scheduling any replacement, even if replacement has no/invalid haptics. Recent action keys remain, so cancellation does not bypass cooldowns. Focusedv1.4 tests6/6 and fullcore80/80 pass; actual game-reference build0warnings/errors.

Minor: README still mentioned0.3.1 ZIP; corrected to0.4.0. Existing startup-invalid-profile warning omission remains explicitly deferred.

## Declined scope and executor rulings

- Native menu/dropdown/short-window clipping: static layout/scroll inspected; retain pending user UI acceptance.
- Actual audio, mixer/mute, human identification/loudness/cheers/ending: native audition/human measurements absent; retain pending, do not infer from peaks/file hashes.
- Soundcard/controller onset and physical stop/disconnect latency: no measurements; only software scheduling/cancellation is verified.
- Real stars2/4,NGP/fire/multiplayer/online: retain unverified or deferred status. No new fire or multiplayer Hook claimed.
- Original composition history/legal ownership: supplied declarations/provenance/files verified, not independently established legal conclusions. No plugin-code license was selected.
- Final installation/updated archives: outside review snapshot; executor must inspect archives and installed version/config transfer before delivery. No new runtime code beyond the two reported fixes should bypass this one-pass gate.
