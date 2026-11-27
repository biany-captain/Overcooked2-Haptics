# Decisions

## Confirmed by user (2026-11-21)

- Official requirement: requirements-v1.0.md; core product changes and additional features require discussion.
- First implementation: stages A+B only; single local controller.
- Actual device: 黑武士 5 Pro, 2.4G XInput.
- Initial test 80ms, throw 65ms, cooldown 120ms, master multiplier 0.75; base motor levels follow the source specification.
- F6 test / F10 toggle confirmed in the follow-up answer; original F8/F9 conflict with existing HostUtilities settings.
- Local multiplayer / multiple controllers may be left to the end.
- User has an existing MOD manager for comparison testing; no replacement manager is required.
- In response to the reconnection-interaction proposal, user clarified that only one controller is connected. No extra reconfirmation flow is adopted for A+B; physical device swaps remain a known mapping limitation.

## Local implementation choices

F6 menu scope confirmed: no auto-pause, suppress only local chef input while open, generic test plus five independent event previews. Global Enabled switch appears in menu and stays synchronized with F10. Event previews are explicitly manual; production patterns/strengths stay unchanged.

Stage C batch approved: CatchSuccess, DeliverySuccess, DeliveryFailure and OrderExpired together, specification motor levels/durations and existing priority/drop policy; all still use the single bound controller. No pre-warnings or other actions added.

- Keep one runtime DLL with pure core classes; no new runtime third-party libraries.
- Use installed BepInEx/Harmony and local game references; compile net35/x86.
- Source-link the core into a package-free net10 development harness. .NET 10 is not required by the installed game plugin.
- Explicitly unbound defaults (-1); separate game Pad and XInput binding. First probe log supplies a game Pad candidate, never a guessed output index.
- Select one client flight-state Postfix; other candidates remain unpatched. Degrade safely when required metadata is missing.
- Equal/lower priority effects are dropped while an effect is active, with no deferred replay. Only test and throw ship in A+B; richer collision policies require discussion before later events are added.
- Retain the user-selected directory. It had no Git repository; no branch/worktree or automatic commit is created.

## Deferred decisions

Low-vibration-mode semantics; same-priority handling for later event types; global warning cadence across pots/orders; early-warning thresholds; optional settings UI; multiplayer binding; public source license. None is silently added to A+B.

## New specification and repository (2026-11-21)

- Current formal requirement is requirements-v1.3.md, preserving requirements-v1.0.md as historical baseline.
- User approved standard candidate waveforms / EveryOrderedDelivery / original CrispReward at 0.65, plus haptics-only one-star tail and two-part catch for joint testing.
- v1.3 assumes v1.2 existed; current0.2.2 does not contain delivery classification or Combo tiers. Establish actual client score/threshold evidence first, then implement those features in sequence.
- User authorized GitHub pushes of necessary files, including README and .gitignore. Initial verified0.2.2 baseline is8d28f73; local intermediates and personal/game dependencies remain excluded.
- Continue in the established user-selected project directory using a dedicated feature branch; no extra checkout or environment migration is needed for the single probe patch.

## v1.3 user decisions after actual data capture

- Star thresholds differ by level; use current selected game configuration, never require per-level manual entry or a fixed internet table.
- User approved immediate single-message arbitration on the runtime-proven synchronous path rather than a universal120ms window.
- User approved explicitly clicked reward audio previews in menu/pause; automatic feedback remains gated and focus loss stops output.
- Candidate patterns remain adjustable and require joint real testing. No final tactile-identification/volume/four-star/multiplayer acceptance is inferred from compilation or old-source logs.
