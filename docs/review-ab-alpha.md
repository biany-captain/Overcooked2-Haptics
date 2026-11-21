# Independent A+B alpha review

Review performed on 2026-11-21 by a fresh read-only reviewer, before runtime deployment. No second review pass is used; fixes are covered by regression tests.

## Material findings and disposition

1. Device slot ownership may change after disconnect or component disable. **Disposition:** the user clarified that only one controller is connected. Keep the already approved single-controller flow; the proposed extra F6/reconfiguration interaction was not adopted. The alpha cannot identify a replacement physical controller behind an unchanged XInput slot. Device replacement, mode changes and multiple-controller ownership require manual slot verification and remain a documented limit, not a claimed guarantee.
2. Scheduler Touched flag never retired historical output slots. A later StopAll could send zero to an old slot now used by another device. **Fixed:** final zero attempt retires ownership. Covered by stop/reassignment and disconnect/retirement tests.
3. Play could observe disconnection and return without clearing an existing pulse, including skipping the check during cooldown. **Fixed:** connection failure clears the pulse/cooldown before returning; connection check precedes cooldown. Both same-key and new-key regression cases reproduced failure and passed after repair.

No Critical issue was reported. No Minor issue was reported.

Ruling: retain the approved single-controller flow instead of adding an unapproved reconfirmation interaction — user clarified the one-device setup and multiplayer is deferred — if a physical device is replaced behind the same slot, the old mapping may target the replacement until manually reviewed. This limitation is disclosed to the user and is not marked fixed.

## Runtime items deliberately not judged

Actual game loading, Hook accuracy, pause/menu states, MOD manager live disable and physical rumble require actual execution and user/controller observation. They remain acceptance items, not inferred passes. Harmony partial-patch failure and third-party MOD coexistence remain runtime uncertainties; keep a unique Harmony owner and optional Hook with explicit diagnostics. Deferred multiplayer is outside A+B.

## Additional author verification

The build environment originally leaked its process-local APPDATA change into the caller's PowerShell session. A command comparing the caller environment before/after the test script reproduced the leak; finally-based restoration fixed it. The same test now passes.
