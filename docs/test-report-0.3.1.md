# Test report — 0.3.1-alpha menu controls

2026-11-21. User approved master0–100%, independent per-event gain0–200%(default100%), two F6 tabs and explicit per-star audio/haptics/combined buttons without changing automatic mode.

## Software verification

- Core70/70 passes, including5 new gain/mode cases first RED on missing implementation then GREEN.
- Actual BepInEx config integration8/8, including gain defaults, existing user values, scaled one-star tail, independent preview selection and the actual BepInEx nonfinite range clamp.
- Actual managed game-data tests5/5 remain green.
- net35/x86 build0 warnings/errors. No new runtime dependency.

Gain1 preserves existing profiles. Gain0 produces no motor task or priority ownership. Gain above base limits is capped uniformly to retain left/right ratios, timings and safety limits. Explicit preview mode is an Emit argument, not a temporary write/restore of the automatic-mode property. UI changes stop previous explicit star preview, retain manual counters and save only changed settings.

Evidence: evidence/core-tests-0.3.1.txt, config-tests-0.3.1.txt, probe-tests-0.3.1.txt and build-0.3.1.txt.

## Pending UI acceptance

New tab layout, slider comfort/visibility and all four star preview modes require game testing. Existing0.3 actual experience/source evidence is archived in test-report-0.3.md; the user reported one feedback set and no general issue. Actual stars2/4, expiry and NGP/multiplayer/other-MOD behavior remain unverified where no log/sample exists.

Deferred known Minor from0.3: startup-invalid pattern text safely mutes haptics but its initial warning is missing until an output setting changes.

## Review and layout follow-up

Fresh read-only review found no motor scaling or preview-mode defect. Corrected two documentation inconsistencies: actual BepInEx range clamping and independence of explicit preview modes. Added a config regression for the former. Short windows now scroll the whole body, retaining a fixed title/close button; the full-height event list uses a group instead of a nested scroll view so it cannot consume the outer wheel. Rebuilt against the actual net35/x86 game references with0 warnings/errors; visual acceptance remains pending. The audio-pack request arrived before0.3.1 installation, so the installed version is still0.3.0.
