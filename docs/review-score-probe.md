# Score source probe — final review

Read-only fresh review of baseline8d28f73 through55ac3fd. Reviewer inspected the source and local game IL; no game interaction or second review pass.

## Findings and resolution

- Important: GetLevelID was called before checking the progress/directory it dereferences, and score getters were read without checking private PC boundary data. Missing metadata therefore stopped all capture. Replaced the unsafe lookup with the same selected-variant identity matching over nullable scene data; guarded the actual current-build boundary field before getters. Scores can still be logged with unavailable thresholds.
- Reviewer labeled the lost ScoreOnly mismatch diagnostic Minor. Implementer regraded it Important: detecting applied-state disagreements is the purpose of this calibration phase, and suppressing them could hide an unreliable source. Only skip an unchanged ScoreOnly when payload agrees as well.
- Actual game-data regression cases were1/5 before these fixes and5/5 after. Full core44/44 and config4/4 stayed green; net35/x86 build0 warnings/errors.

## Scope rulings

- Classified delivery, Combo/star tracking, audio and new candidate patterns remain later phases because the formal requirement calls for actual source calibration first. Cost: no new reward behavior in0.2.3, explicitly documented.
- Real HUD/map agreement, Harmony invocation and physical behavior remain unverified for this build until the joint runtime test. No static result substitutes for that evidence.
- Multiplayer, online guest and actual four-star environments remain deferred as agreed. No compatibility claims added.
- Installation/rollback/push were outside the reviewer's read-only action; the implementer verifies those separately.

No deferred Minor findings remain. No original game method arguments/results, profile values or existing menu logic were changed.
