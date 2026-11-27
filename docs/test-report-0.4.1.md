# Test report — 0.4.1-alpha Ultra Victory v3

## Subsequent native acceptance — 2026-11-21

User explicitly confirms “G星级反馈和音乐系统已实现没问题”. Record G as implemented/user-accepted in this local environment. Filtered evidence/runtime-feedback-0.4.1.txt proves all12 new clips preloaded, TurboSwing1–4 combined manual previews at gain1, and clean shutdown. Selected latest log contains no real milestones; real2/4/NGP and unobserved styles are not inferred from these previews. Game-master/SFX following and physical synchronization were not independently measured. The pending statements below describe the original delivery point, before this acceptance.

2026-11-21. User rejected both old shipped sound sets; approved three whole new packs,12uneditedWAV, defaultturbo_swing, full3.80–4.05s four-star files, unchanged short haptics. No mixed-star or crowd-layer feature. User approved game master/SFX following with default relativegain1 and custom slider, and localvolume.7006757->1; preserve currentHapticsOnly/master1/ordergain.97.

## Software verification

- Core80/80 PASS. Sound-pack focused5/5: retired IDs/case variants excluded, absent root produces empty catalog, all12actual files decode at exact approved millisecond lengths,4050/5000ms accepted and5001ms rejected. Focused firstRED2/5 before implementation. Obsolete synthetic-tone-only test removed with that explicitly removed feature; gameplay/output regressions retained.
- Actual BepInEx config11/11 PASS. Retired selections migrate, unknown custom pack/volume/profile precision/mode/master remain; new relativeSoundVolume defaults1, obsolete MaxSoundDurationMs is removed. FirstRED9/11 thenGREEN11/11.
- Actual game-managed-data probe5/5 PASS. No scoreHook, threshold,Combo or dedup change.
- Actual Unity2017.4/net35/x86 references build0warnings/errors, assembly0.4.1.0. No runtime dependency added.

Evidence: evidence/core-tests-0.4.1.txt,config-tests-0.4.1.txt,probe-tests-0.4.1.txt,build-0.4.1.txt and audio-assets-0.4.1.json. Sourceprovenance/author notices and exact originalWAV hashes: assets/audio/SOURCES.md.

## Audio and scope

Three presets: TurboSwing1050/1570/2300/3850ms; ChampionFanfare1130/1700/2480/4050ms; KitchenRock1010/1530/2350/3800ms. All12WAV48kHz/PCM16/stereo, clipped samples0, final samples0or1/32768; total5,149,960bytes. Mapping JSON and actual frame length agree within one48kHz sample. No file was trimmed, re-encoded or amplified. Digital peaks/RMS do not establish human comfort or equal perceived loudness.

Oldjazz WAV/source assets are removed except the sharedoffline build_music helper required by the new generator; no old generatedtone is compiled or used asfallback. No thirdparty crowd audio/OGG/previews/HTML player/SoundFont/cache is shipped. New material's declaredSoundFont source/license records are preserved without claiming independent ownership clearance orCC0.

Volume continues through the existing gameSFX group and one relative gain; default100% removes extra default attenuation. Game settings/mute and independentcustom slider require focused native acceptance. Existing current modeHapticsOnly intentionally persists; explicitAudioOnly/combined preview works independently, actualautomatic music requires user choosing a mode containingaudio.

## Real runtime evidence and remaining acceptance

Prior0.4 user reported tested menu features normal/distinguishable, then rejected both audio styles. Priorlog only records fourHapticsOnly star levels and oneAudioOnly one-star jazz preview plus clean shutdown, real-event countersall0 (evidence/runtime-menu-0.4.txt). It does not verify everymode, three new styles or actualgame milestones.

Allnew0.4.1 native playback/dropdown/volumefollowing/comfort and hardware synchronization: UNTESTED. Followbatch-test-0.4.1.md. Realstars2/4/NGP and previously unobserved expiry samples remain unverified. FireHooks/multi-controller/online remain later scope. No human blindtest or physical≤50ms assertion. Knownstartup invalid-pattern warning omission unchanged.

## Delivery verification

Freshread-only review found no newCritical/Important/Minor issues (review-ultra-v3.md). Coldbackup completed whilegame stopped; installed0.4.1.0 ownDLL and12source-identicalWAV/source/license files. Verified retiredownfolders absent; onlylocalcfgSoundVolume1 andSoundPackIdturbo_swing changed. ModeHapticsOnly/master1/ordergain.97 and allothercfgtext preserved. The newDLL has noRewardTone type. Native0.4.1 playback and gamevolume acceptance remain pending; software checks do not replace them.
