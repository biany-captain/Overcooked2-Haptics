# 0.3.0 whole-feature review

Fresh read-only review of575cfac..589263c. No original game mutation, UI interaction, installation or second reviewer pass.

## Material finding fixed

Same-round star history was erased by global/binding changes, temporary untrusted data and threshold changes. Reproduction: reach2stars→lose score below1star→toggle/rebind/resync→cross1star again. The initial combined history cases reproduced0/2, then passed2/2 after the fix.

History now lives under a real local round boundary key (scene handle/client flow/round epoch), separate from diagnostic/context generations. F10, bindings and temporary component disable suspend the baseline while preserving the announced bitset; trusted pre-commit state re-establishes the baseline. Threshold/authority changes retain the same round's bitset. Actual round end/scene/new client round creates fresh history. Captures freeze their round identity; stale completions are dropped. Unity2017's public Scene.GetHashCode was inspected locally and returns the internal handle.

An additional boundary case verifies round continuity vs actual end/new scene. Full core65/65, config6/6 and managed-data5/5 pass after the fix. No second review requested; final build/artifact checks are recorded separately.

## Deferred Minor

Invalid pattern text supplied before startup safely mutes that haptic pattern, but the diagnostic warning currently appears only after an output-setting change. Initialization warning is missing. Default patterns are valid; this does not affect the joint candidate test. Documented for later, not silently fixed in the material pass.

## Scope rulings / evidence limits

- Subjective hand-feel, sound comfort/identification, actual Unity mute/pause/focus/device behavior remain joint real-game tests, not inferred from logic or compilation.
- Independent HUD/map comparison, recovery/expiry transitions and actual NGP fourth-star context remain pending. F6four-star preview is not a real fourth-star milestone test.
- Multiplayer, online joining/reconnect ordering and MODs changing thresholds/session/callback semantics in place lack compatibility samples. Current source/version/mode and ordinary context changes are checked; arbitrary modifications are not claimed reliable. Cost: these cases may degrade or be inaccurate and require separate support work.
- Current integration/push/install is root's responsibility, outside the read-only reviewer's actions. The branch remains a draft until real acceptance.
