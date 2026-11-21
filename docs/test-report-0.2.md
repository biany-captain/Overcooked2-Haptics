# Test report — 0.2.0-alpha (2026-11-21, Asia/Shanghai)

## Current evidence

| Check | Result | Evidence |
|---|---|---|
| Plugin build | PASS | net35/x86, 0 warnings/errors; evidence/build-0.2.txt |
| Core suite | PASS | 31/31; evidence/core-tests-0.2.txt |
| New tests RED | Observed | New batch initially 3/9 pass, 6 failures before profiles/team routing implementation |
| Existing 0.1 game checks | Historical PASS | test-report-0.1.md and prior filtered logs; not a substitute for 0.2 regression |
| 0.2 installation | PASS | Own DLL updated to 0.2.0.0 with 0.1 backup; binding/intensity preserved |
| 0.2 game load / four new Hooks | PASS | Actual log contains initialization and all new Hook installations |
| Catch / successful delivery / failed delivery / order expiry | PASS in current environment | User confirmed all four; log schedules catch2/success1/failure1/expiry1 |
| Original throw / F6 / F10 regression in 0.2 | PARTIAL | Throw6 observed/scheduled; separate F6/F10 recheck not explicitly reported in this batch (0.1 history passed) |
| Fresh review | Complete, no material findings | review-stage-c.md |
| Package inspection | PASS | Runtime/source ZIP includes checklist and only intended plugin DLL, no game/loader DLLs |

## Automated scope

Existing pulse/cooldown/ownership/disconnect/invalid-input tests remain green. New tests exercise actual approved profiles, successful-delivery silent gap, team/unknown/opponent rejection, personal catch binding, repeated invalid-order failures after cooldown, duplicate order callback and expiry preemption without stale replay.

The fake backend records requested values and does not measure motor response or validate actual Unity event sources. Native device/Steam Input and lifecycle observations remain separate evidence.

## Batch procedure

See batch-test-0.2.md. All four new toggles enabled; use current single-controller binding; leave DebugLogEvents=true for this test. Record each event as tested/untested and physical behavior. Preserve the 0.1 DLL/package before replacing the installed plugin. Restore ordinary logging after testing if complete.

## Known limits

One local bound controller only. Catch currently uses the local/host server path; guest-client coverage unverified. Advanced warnings/UI/multiplayer are outside this batch. Physical device replacement behind an unchanged XInput slot cannot be identified; mapping must be manually checked. Higher-priority effects can interrupt lower-priority effects; no queued replay. No guarantee for forced process termination or a frozen main thread.

## Actual batch outcome

User confirmed CatchSuccess, DeliverySuccess, DeliveryFailure and OrderExpired all tested successfully. The filtered 0.2 log records all Hooks installed and scheduled totals: ThrowSuccess6, CatchSuccess2, DeliverySuccess1, DeliveryFailure1, OrderExpired1. Team results had team0/localTeam0 and routed XInput0. This proves the current small trial; it does not measure broad error rate or cover other modes. Evidence: evidence/runtime-0.2-batch.txt.
