# Test report —0.6.2-alpha / strict personal feedback candidate

Updated2026-11-27; software candidate built2026-11-22. User approved strict personal delivery/Combo/star output; unattributed danger/expiry are silent. No team feedback fallback. Incremental on5eda7ec, reviewed candidate08ea2f0 plus focused rejection fix5140e61. User now confirms own-delivery rumble and peer-delivery silence when joining another host with one local chef/controller. This is limited native acceptance, not all-event or all-context acceptance.

## Native0.6.2 session

The preserved log initializes0.6.2 and installs the explicit placement Hook. User reports “现在自己上菜手柄有触发震动了” and separately confirms “队友上菜时保持安静”. Own-plugin evidence contains225 kitchen Delivery results:217 successes pair uniquely with plate receipts, including40 local-confirmed deliveries with matching accepted output and177 remote-release successes with no output. The40 accepted events are ordered5, out-of-order7, unclassified20 and Combo×2/3/4 upgrades4/3/1; all target player0/gamePad0/XInput0. These are scheduler/log counts, not independently counted physical pulses or a new comfort assessment.

Five star selections (one-star3, two-star1, three-star1) pair with remote releases and remain silent. No own-caused star output was recorded, so personal star/music acceptance remains pending. Eight client rejections and one non-plate receipt remain silent under the approved unknown-attribution policy. Two rejected receipts have prior local release records; that alone is not reliable personal proof. No own-plugin Error/Fatal or personal observer-fault line appears in the saved session. See[evidence/runtime-personal-0.6.2.txt](evidence/runtime-personal-0.6.2.txt).

## Native evidence before this candidate

User reports0.6.1 menu works but rumble remains rare, with an uncontrolled online sequence. Actual probes ran without a recorded observer fault. Preserved own-plugin trace has2310 plate carry observations,581 kitchen results with unique station receipts,183 preceding local-chef release candidates and398remote, plus6 non-plate station rejections. Kitchen-to-receipt median0.8ms,max35.6ms. The old probe lacked explicit placement requests, so none of these is proof that personal output worked. See evidence/runtime-personal-probe-0.6.1.txt. No real failed-recipe coverage was established.

## Implemented candidate

Read-only exact local Place request plus same-chef plate release, unique trusted kitchen result and same-plate/station receipt. Bind the expected plate when the result arrives, never guess from last holder at receipt time. Native pickup/transfer/re-target invalidates intent; conflicting/reversed/missing/stale streams stay silent. Pairing reuses existing180ms freshness without a fixed wait. Root runtime gate rejects team events even if legacy fallback configuration is true. All team score/Combo/star progression is consumed before personal filtering, so peer milestones do not replay on a later own action.

Existing modes/profiles/music/masters/sliders/feature controls and previews remain. Unknown hazards/expiry and client rejection without reliable station identity do not emit or stop music. The latter protocol omission was found during review: failed messages reuse preceding success fields. Snapshot now ignores unsent fields, silently drains rejection receipt and does not poison later same/previous-station valid successes. Manual negative profiles and the generic fully attributed rejection contract remain; no native rejection support is implied.

## Software verification — retained2026-11-22 results

- Core140/140, evidence/core-tests-0.6.2.txt: all125 previous regressions plus15 personal tests including actual old trace without request yielding zero, remote/keyboard/wrong team, transfer/re-pickup/retarget, unique pairing, conflicts/reverse/late/reset, rejection, strict runtime scope, peer progression consumption and unscoped-rejection recovery.
- Actual BepInEx configuration20/20, evidence/config-tests-0.6.2.txt. No personal values migrated or new settings introduced; only legacy fallback descriptions updated.
- Actual game API/data and failed-message projection11/11, evidence/probe-tests-0.6.2.txt. New request overload/helper signature checked; stale failure fields are ignored without Unity execution. This is not native Hook or transport acceptance. Linked diagnostic-source fields may produce test-project unused-field notices; the actual MOD target below has no warning.
- Actual Unity2017/net35/x86 target build0warnings/errors, assembly0.6.2.0, evidence/build-0.6.2.txt.
- New pipeline first REDmissingAPI then GREEN. Reviewer Important reproduced with two RED regressions (unsent fields and following success quarantine), fixed GREEN. No fix re-review; rulings in review-personal-feedback.md.

## Pending / delivery

With the game stopped, cold-backed up0.6.1 ownDLL/cfg and installed only the verified0.6.2.0 DLL. Installed hash matched the final tested artifact; cfg bytes unchanged, no audio or other MOD files written. Fresh request Hook and basic own-versus-peer physical behavior now have native evidence above. Controlled plate handoff, real personally caused star, host/local-multiple controllers and uncalibrated contexts remain unverified. No rigid action order or repeat of the whole music suite is required; optional handoff and a naturally occurring own star can extend coverage. Default focus/stop/round/config/binding changes clear associations; existing RespectGameFocus=false advanced opt-out semantics are preserved, with independent BGM focus release.

Audio assets and personal configuration remain unchanged. No new runtime dependency, game-state writes, per-frame scene search, driver/backend, autoplay or game-window automation.

This2026-11-27 update records user/runtime evidence only. No product code, configuration, waveform, installed DLL or audio was changed; previous software tests/build were not rerun for this documentation update.
