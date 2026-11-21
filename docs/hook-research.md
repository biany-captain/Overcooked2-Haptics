# Hook research — 2026-11-21

## Stage C local inspection / implementation (0.2)

- Catch: `void ServerAttachmentCatcher.HandleCatch(ICatchable,Vector2)`. Local IL calls `m_carrier.CarryItem(catchable.AccessGameObject())` before returning. Postfix additionally checks the public `InspectCarriedItem()` equals that exact object. Requires chef PlayerControls/PlayerIDProvider and matching bound pad; other catcher types (pot/counter) are not patched. Local/host coverage is a candidate; guest clients remain unverified.
- Results: only `ClientKitchenFlowControllerBase.OnSuccessfulDelivery(TeamID,OrderController.OrderID,float,int,bool,ClientPlateStation)`, `OnFailedDelivery(TeamID,OrderController.OrderID)` and `OnOrderExpired(TeamID,OrderController.OrderID)` receive Postfixes. Both campaign and competitive overrides in the local binary call these bases. No derived-layer duplicate patches.
- OrderID is a namespace struct `OrderController.OrderID` with public `uint m_id`; it is normalized with an unchecked int conversion, retaining the invalid sentinel. Deduplication is short-window rather than permanent so subsequent wrong deliveries are not lost.
- TeamID One=0, Two=1, None=2. Resolve the bound local chef's team independently from `PlayerIDProvider.s_AllProviders` (`FastList._items`/Count), `ControlScheme.Player`, pad lookup and `GetTeam()`. Ambiguous/unknown/wrong team stays silent. Provider enumeration only occurs on actual result events.
- Optional installation and adapter faults are isolated per event; each Harmony owner only removes its own patches.
- 0.2 compiles and pure profile/routing tests pass. New actual-game observation is pending the joint batch test.

## Local baseline

- Steam build ID 20236421; executable x86 (PE Machine 0x014C).
- Unity executable file version 2017.4.8.5945332; this is not the game's release version.
- Executable SHA256: D5D1757E11D298B9BB487E47B0AFFEB0BA4F5D563BE3F082BB17FE6F032FCE81.
- Assembly-CSharp metadata runtime v2.0.50727; mscorlib 2.0 and System.Core 3.5.
- BepInEx 5.4.22.0; installed 0Harmony assembly version 2.9.0.0.

All local inspection used Mono.Cecil metadata/IL reads; the game assembly was not executed inside PowerShell.

## Selected first probe

`private void ClientThrowableItem.StartFlight(IClientThrower)`; Postfix only.

Local IL directly sets `m_isFlying = true` and stores the provided thrower in `m_thrower`, then sets pickup referral / particle state. The installed Postfix additionally requires the resulting flying flag. This is flight state, rather than an input-button request.

Chef resolver: thrower as Unity Component → PlayerControls and PlayerIDProvider on the same object → IsLocallyControlled() → ControlScheme.Player → PlayerInputLookup.GetPadForPlayer → PCPadInputProvider.IsKeyboard. All called APIs/signatures were found in the local assembly and compilation passed.

`PlayerIDProvider.IsLocallyControlled()` only checks its ID against the Count sentinel; it does not identify keyboard vs gamepad. The resolver checks keyboard separately. Neither the game's PadNum nor its local-player enum proves a Windows XInput index. Explicit manual binding is required.

Item identity uses Unity GetInstanceID, valid for this process/scene; it is not claimed to be a network-stable entity ID. Runtime de-duplication/cooldown uses the selected output and throw event key, so repeated callbacks cannot reset the pulse within 120ms.

Still unverified: actual call count, chef component co-location in all cases, local chef switching, client/host coverage, MOD-modified throw paths, real latency. Public research notes teleportals can also throw; their lack of chef input/identity should filter them, but this needs a game test.

## Other candidates (not patched)

| Candidate | Local signature | Status |
|---|---|---|
| Input intent | void ClientPlayerControlsImpl_Default.Update_Throw(float,bool,bool,bool) | Exists; not success evidence |
| Server request | void ServerPlayerControlsImpl_Default.ReceiveThrowEvent(GameObject) | Exists; may reject |
| Server throw | void ServerThrowableItem.HandleThrow(IThrower,Vector2) | Exists; host-only risk |
| Catch | void ServerAttachmentCatcher.HandleCatch(ICatchable,Vector2) | Exists; outside A+B |
| Delivery | void ClientKitchenFlowControllerBase.OnSuccessfulDelivery(TeamID,OrderID,float,int,bool,ClientPlateStation) | Exists; outside A+B |
| Failed delivery / expiry | void OnFailedDelivery(TeamID,OrderID), void OnOrderExpired(TeamID,OrderID) | Exists; outside A+B |
| Cooking | void ServerCookingHandler.SetCookingProgress(float) | Exists; outside A+B |

## Lifecycle evidence

TimeManager.IsPaused(PauseLayer.Main) checks the game's manager and safely returns false when absent. IFlowController.InRound is a stored round-active flag. GameUtils manager lookup uses GameObject.FindGameObjectWithTag; cache the returned live flow component and retry missing lookups at 1Hz, rather than searching every frame. Actual pause/end/menu transitions remain runtime acceptance items.

## Runtime probe matrix

| Action | Expected | Actual |
|---|---|---|
| Successful chef throw | One observed flight; one 65ms output after binding | PASS in controlled bound trial; target timing logical, not instrumented physical duration |
| Empty-hand throw button | No success event / no rumble | PASS in same small trial, user confirmed |
| Nonthrowable item | No success event / no rumble | Pending |
| Collision-blocked failure | No success event / no rumble | Pending |
| Keyboard throw | Probe may log keyboard=True; no rumble | Pending |
| Teleportal throw | No chef event routed | Pending |
| Pause / leave / end | Output stopped, no replay | Pending |
| Other chef / remote player | No output without matching local binding | Deferred / pending |

Initial runtime probe: StartFlight Postfix installed in the actual game and captured five local gamepad flight events (player0 / gamePad0 / keyboardFalse). No adapter fault logged. User confirmed successful attempts followed by empty-hand attempts but did not provide exact counts; do not infer five-for-five accuracy. Binding was based on this observed Pad0, with the independently observed XInput0.

Bound comparison update: one successful throw accepted, empty-hand attempt produced no flight event, F10 disabled feedback, another actual throw observed but rejected (observed=2/scheduled=1). User confirmed only the initial success vibrated. Local single-controller hook is now verified for this small trial/build/MOD set; blocked throws, nonthrowable items, remote players and other versions remain unverified.

Sources: https://hpmv.dev/docs/tech/throwing/ and local Assembly-CSharp.dll inspection. Public documentation and local signature matching do not replace runtime evidence.
