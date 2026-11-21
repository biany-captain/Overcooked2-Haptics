# Test report — 0.4.0-alpha sound packs

2026-11-21. User approved formalv1.4 star haptics, unchanged suppliedWAV including3.25s four-star, startup preload, persistent pack dropdown/defaultjazz and necessary GPL/source files inGitHub. Menu intensity/two-tab changes from0.3.1 are included;0.3.1 was not separately installed/tested.

## Software verification

- Core79/79 PASS: existing70 plus4 WAV/catalog/actual-asset cases and5 v1.4 pattern/audio-tail/preemption/queue cases. New API tests first failed, then passed after implementation.
- Actual BepInEx config9/9 PASS: known old defaults migrate; custom patterns, selected/unknown future pack, existing master/volume and original crisp duration remain. Default new pack isjoyful-cartoon-jazz.
- Actual game-managed-data probe5/5 PASS: score/threshold source and NGP gate tests retained.
- net35/x86 using current game references:0 warnings/errors, assembly0.4.0.0. Unity's stripped framework lacksInvalidDataException; format errors usemscorlib FormatException without an extra dependency.

Evidence: evidence/core-tests-0.4.txt, config-tests-0.4.txt, probe-tests-0.4.txt and build-0.4.txt. Prior real user/log acceptance is archived in test-report-0.3.md, with0.3.1 UI software work in test-report-0.3.1.md.

## Asset evidence

Four original userWAV copied without trim, gain edits or re-encoding. All48kHz/PCM16/stereo. Durations980/1490/2160/3250ms; digital clipped samples0; first samples above-60dBFS approximately0.6–0.8ms; final samples at most1/32768. This does not prove the absence of an audible click or establish perceived loudness.

Peak dBFS: -7.35/-8.13/-4.07/-2.38. RMS dBFS: -23.68/-21.78/-19.13/-18.26. Four-star peak exceeds the spec's suggested-3dBFS headroom but has no digital clipping; files retained unchanged for the explicitly approved prototype audition. Subjective loudness and orchestration were not evaluated by the assistant. SHA256/provenance/license records: assets/audio/SOURCES.md and supplied notices, MIDI and generator.

## Integration and limits

AudioClip upload occurs inAwake;Play only selects a prepared clip and appliesgain once through the game'sSFX group. Hooks enqueue immutable feedback,Update schedules playback. Software uses a common monotonic start timestamp, not measured soundcard/controller onset. No claim ofsample-level or≤50ms physical synchronization.

Higher stars replace active celebration; ordinary actions do not stop audio tails. Existingfailure/expiry stopaudio and can override motors. Queue is discarded oncancel/roundidentity change;globaloff drains with output gated, so no backlog replay. Disconnect stops own audio/motors after observed state change (Update poll250ms); no physical latency guarantee under a blockedmainthread/devicefault.

## Pending real acceptance

Allnew0.4 game playback/menus/dropdown/restart persistence/slider visibility and perceived motor/music timing: **UNTESTED**. Follow batch-test-0.4.md. Real stars2/4,NGP and any prior unobserved expiry sample remain unverified. No new fire/burning warningHook, local multi-controller oronline capability is claimed. M02 human blind identification≥85% is a goal, with nohuman trial results. Missing-file behavior is tested at the decoder/catalog/output seam, not yet in nativeUnity runtime.

Review/package/install status is recorded below when actually completed. Deferred knownMinor: initial invalid-pattern text mutes the event but does not log its initial warning until an output settingchanges.
