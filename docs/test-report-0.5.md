# Test report — 0.5.0-alpha / v1.6 candidate

2026-11-22. Incremental work on a79b635. User confirmed v1.6 standard low/high channels, Combo A, simple F6 with cfg fine switches, proven single-output routing and no guessed multi-player Combo. Independent later rejection/expiry may interrupt celebration; same delivery still selects only star/Combo/ordinary. Preserve personal intensity, game-master/SFX following and relative100%, sound pack and all12 music files. The uninstalled0.4.3 draft was superseded, never delivered.

## Software evidence

- Core105/105 PASS, evidence/core-tests-0.5.txt. Tests cover exact JSON profiles/metadata/policy values, negative950 vs action500/star850 limits, Combo upgrades/cap/reset/unknown prior break; per-device staleness, dedup, cross-key cooldown, active-time budgets excluding gaps, elapsed preemption, independent devices, one team sound, channel/stop behavior; verified/unknown backend identities and future SDL descriptor; fire spread/clear/reminders/cooking/removal, mapping and first-score-vs-current-hazard lifecycle. Existing meaningful engine/star/music regressions remain.
- Actual BepInEx cfg15/15 PASS, evidence/config-tests-0.5.txt. Known old defaults migrate; exact custom precision/invalid values remain. Throw/catch/burned defaultoff, modeA fixed, generic80ms preserved, relative volume/master/pack/hotkeys remain. Legacy coarse success preference is retained when present; event switches now intentionally live only in cfg.
- Actual game-managed-data probe6/6 PASS, evidence/probe-tests-0.5.txt. Previous score/threshold/NGP checks plus existence/signatures of client hazard methods and cooking-state data. These are metadata/data checks, not an executed Unity hook.
- Actual Unity2017.4/net35/x86 build0 warnings/errors; assembly0.5.0.0. Source JSON/generated profile check passes. No runtime library/native dependency added. evidence/build-0.5.txt.

New profile/config/coordinator/hazard tests first failed for missing APIs; profile/config then passed. Cross-key cooldown was separately reproduced RED0/1 and fixed. Exhausted motor-budget stars correctly retain audio and report motorWindowMs=0 (RED0/1→GREEN1/1); initial score epoch no longer cancels an existing current-round hazard (RED missingAPI→GREEN1/1). Old catch/failure/expiry assertions were updated to the explicitly selected v1.6 contract; unknown prior combo now degrades rather than guesses out-of-order. Shared star priority86 uses actual ordinal for higher-star replacement, not numeric priority alone.

## Static game audit

Current game assembly MVID114d606a-29d5-472c-8ebe-c2c20fbe3be3. ClientFlammable.ApplyServerEvent uses onFire state and private Ignite/Extinguish; candidates patch those completed transitions. ClientCookingHandler.ApplyServerUpdate applies/caches CookingStateMessage state; postfix checks applied state, not just input intent. OnDisable cleanup is observed. FoodBurned default remains false until distinct real-event evidence and user approval.

Existing score source already provides before/payload/after within one client message; retain user-approved immediate arbitration, not a blanket80ms delay. Fallback success/rejection/expiry hooks mark capture ownership; confirmed negative callbacks emit once from completion. A failed callback outside capture retains independent fallback.

Delivery messages do not identify the chef directly. Only proven single local output is inferred; unknown multi/online Combo stays silent. Explicit cfg permits ordinary team fallback to the selected binding only. Hazard routing is current Campaign/local-team candidate; no competitive-world guess. See audit-v1.6.md and hook-research.md.

## Acceptance levels

Prior0.4.2 user confirmed “新菜单及两个总控均正常”; logs/cfg corroborate load/previews/F10/migration, with selective physical stop based on user report. G star/music was separately accepted. Historical report/evidence retained in test-report-0.4.2.md and runtime-menu-0.4.2.txt.

User subsequently reported “已测项目正常，触感也合适”. The retained0.5.0 session corroborates XInput0 connection, manual1–4-star haptics, FireStart/FireReminder/FoodBurned and generic previews, normal shutdown and zero own warnings/errors. It contains16 accepted previews and4 rejected repeated previews; rejection is a policy result, not an exception. All real-event totals are zero in this log. See evidence/runtime-preview-0.5.txt. Record user acceptance of the items they tested without treating it as complete real-event coverage.

Real ComboA classifications/ownership, real fire/cooking hook operation, reminder timing/clear and new negative-vs-celebration interruption still need separately observable native coverage. Follow batch-test-v1.6.md. Menu previews, static IL and fake-backend tests cannot certify actual food burned, real2/4/NGP milestones, multi-controller/online/virtual-device physical dedup,15-minute comfort,85% blind accuracy or physical latency. SDL3 is an extension/ownership contract only, no native SDL implementation loaded.

Budget is elapsed nonzero planned-command time, not measured motor energy. Main-thread stalls/forced kill/driver failure cannot guarantee physical stop deadlines. Startup invalid-pattern warning omission from earlier version remains; invalid patterns mute and changed settings log warnings. Unknown actor mapping intentionally may drop feedback and logs why; no automatic broadcast. Low-end/other-device behavior is not inferred from XInput availability.

## Delivery

Independent review of a79b635..0a530d9 found two Important coordinator defects: unbound audio-only negatives and cancelled replacement budget. Both were reproduced RED0/1, fixed in one pass, and final fullcore105/config15/probe6/build0warning checks passed. No second independent review. See review-v1.6.md. With game stopped, cold-backup ownDLL/cfg then install0.5.0.0; installedDLL matches the final artifact, personalcfg bytes unchanged, all12 audio files untouched. First game launch performs tested cfg migration. Earlier quota-blocked Git action was not executed; normal approval later recovered and the candidate commit succeeded. Final delivery commits go to existing feature branch/draftPR without main merge. User preview acceptance and remaining real-event coverage are recorded above. Original documents retained verbatim; chat overrides explicit in design-v1.6.md.
