# Phase 8 — playtest blocker fixes (2026-11-01)

**Task:** repair the four owner-reported Phase 8 playtest blockers, prove two orders complete and pay, then rebuild and hand back for playtest.
**Task ID:** `LL-P8-FIX-01` (owner-requested fix run; not a roadmap task)
**Status:** code, scene, tests, validation, and build complete. Owner playtest still outstanding.
**Report written:** 2026-11-01

## Objective

1. HUD / interaction prompt visible and wired.
2. Order loop reaches payment and the next customer spawns.
3. The wallet actually grows so the $200 shelf becomes reachable.
4. The back-room shelf purchase can be targeted and clears the doorway.

Hard constraint from the handoff: do not touch character models, MakeHuman exports, appliance models, or move `NpcLocomotion.ResolveOverlaps` out of `LateUpdate`.

## What was actually wrong

### 1. HUD and prompt were already wired (no code defect found)
`PlaytestAuditTool` proved `OrderTicketView` on `HUD` and `InteractionPromptView` on `Player` exist, are enabled, and are bound; `PlayerInteractor` has `PlayerCamera`, range 3, key `E`. The ticket box was too short for its five text rows (`margin`/clipping), so `OrderTicketView` now has a self-wiring `Awake` fallback and a 300x290 box (scene value 260x110 was the visible cause of a cramped/overlapping ticket).

### 2. Order loop could stall without ever reporting Done
`CustomerAgent` had several `yield break` paths that never entered `Done`, leaving `CustomerSpawner.ActiveCustomer` set forever. Fixes:
- `Start` now always ends with `Enter(Done)`; the order body moved to `RunOrder()`.
- Every exit path funnels through `LeaveAndFinish()`.
- `WalkTo` has a 40 s per-leg timeout instead of waiting forever on a blocked path.
- Patience raised 90 s -> 180 s (a real cycle is ~72-80 s, so any player interference used to abandon silently).
- Tip window 60 s -> 120 s.
- Ticket create/accept/collect/payment failures now log instead of failing silently.
- `CustomerSpawner` gained a watchdog: a missing, disabled, or `Done` active customer is cleared and the respawn timer re-armed; spawn failures back off instead of retrying every frame; spawn/next-customer events are logged.

### 3. Decorative ambient NPC starved the order pipeline (root cause of "money never reaches $200")
The ambient street customer was wired to drop its decorative laundry prop into the **real order drop-off** (`AmbientNpc._dropOffSlot`). The staff treats anything in that slot as the next order, so the decorative prop consumed a full wash/dry cycle ahead of the real customer. With two orders, the second customer waited past patience and abandoned.

Evidence: the failing end-to-end run logged
`orders=[ORD-0001 Paid, ORD-0002 Abandoned]`, and the earlier trace showed the drop-off non-empty from t=15 s to t=84 s while the staff washed the decoration.

Fix: the scene link is cleared (ambient NPC keeps carrying its prop in and out, matching the intended "carries laundry in/out" presentation), `NpcSceneSetupTool` no longer wires it, and `AmbientNpc` documents why.

### 4. Shelf purchase point sat behind non-interactable shelf colliders
The component was on the thin `SecretAccess_ServicePanel`; the rolling shelf in front had colliders with no interactable, so the player's ray hit a bare board. It now sits on the `SecretAccess` group root, so every collider in the concealment resolves through `GetComponentInParent`. `_sealedDoorCover` is the same group, so paying removes the whole passage blocker instead of leaving the shelf in the doorway.

## Files

- `Assets/LaundryLegends/Scripts/Customers/CustomerAgent.cs` — guaranteed `Done`, walk timeouts, explicit failure logging, patience/tip tuning.
- `Assets/LaundryLegends/Scripts/Customers/CustomerSpawner.cs` — watchdog clear/respawn, backoff, logging, `EnsureEconomy` on spawn.
- `Assets/LaundryLegends/Scripts/Customers/OrderTicketView.cs` — self-wiring fallback, corrected box size.
- `Assets/LaundryLegends/Scripts/NPC/AmbientNpc.cs` — documents that decorative drop-off wiring must stay unassigned.
- `Assets/LaundryLegends/Editor/NpcSceneSetupTool.cs` — stops re-wiring the ambient NPC into the order drop-off.
- `Assets/LaundryLegends/Editor/PlaytestFixTool.cs` (new) — idempotent `Repair()` + `Validate()` for the shelf, HUD, and ambient link.
- `Assets/LaundryLegends/Editor/PlaytestAuditTool.cs` (new) — read-only wiring audit with shelf raycast sampling.
- `Assets/LaundryLegends/Scenes/Laundromat.unity` — purchase point moved to `SecretAccess`, door cover set to it, ticket box 300x290, ambient drop-off cleared.
- `Assets/LaundryLegends/Tests/EditMode/TwoOrderPaymentTests.cs` (new) — two orders credit twice; a duplicate attempt on the first does not block the second.
- `Assets/LaundryLegends/Tests/PlayMode/TwoOrderPlayModeTests.cs` (new) — two real customers through the real staff/machines at production durations, 1/60 s delta.
- `Assets/LaundryLegends/Tests/PlayMode/PlaytestHudTests.cs` (new) — HUD and prompt live and wired; ticket tracks the live ledger.
- `Assets/LaundryLegends/Tests/PlayMode/ShelfPurchaseTests.cs` (new) — aisle ray resolves to the purchase point; buying clears the doorway.

## Checks performed (real results)

| Check | Command | Result | Evidence |
|---|---|---|---|
| EditMode suite | `Unity.exe -batchmode -nographics -projectPath <repo> -runTests -testPlatform EditMode -testResults TestResults\editmode.xml -logFile TestResults\editmode.log` | exit 0, **147/147 passed**, 0 failed, 0 skipped | `TestResults/editmode.xml`, `TestResults/editmode.log` |
| PlayMode suite (final) | `Unity.exe -batchmode -projectPath <repo> -runTests -testPlatform PlayMode -testResults TestResults\playmode.xml -logFile TestResults\playmode.log` | exit 0, **57 passed, 0 failed, 1 skipped** (58 total) | `TestResults/playmode.xml`, `TestResults/playmode.log` |
| Two-order end-to-end | inside PlayMode suite | `ORD-0001 Paid`, `ORD-0002 Paid`; wallet 100; 2 payouts; finished at 182 s game time; 13.1 s wall time | `[TEST] two orders paid; wallet=100; payouts=2; ...` in `playmode.log` |
| Scene repair | `-executeMethod LaundryLegends.Editor.PlaytestFixTool.Repair` | exit 0; cleared 1 ambient link | `TestResults/fix.log` |
| Scene validation | `-executeMethod LaundryLegends.Editor.PlaytestFixTool.Validate` | exit 0; HUD, prompt, camera, spawner, ambient slot, staff, shelf reachable, shelf clears doorway | `TestResults/validate.log` |
| Log scan | grep for `NullReferenceException`, `MissingReferenceException`, `error CS`, `Assertion failed` | none in either suite log | `editmode.log`, `playmode.log` |
| Player build | `Unity.exe -batchmode -quit -projectPath <repo> -executeMethod LaundryLegends.Editor.BuildGameTool.BuildDirect -logFile TestResults\build.log` | exit 0, `Build/LaundryLegends.exe`, 196 MB, 0 errors, scenes Bootstrap/MainMenu/Laundromat | `TestResults/build.log` |

### Interim failure that shaped the fix

First PlayMode run after adding the new tests: 55 passed, 2 failed (`ShelfPurchaseTests` ray aimed from the wrong anchor; `TwoOrderPlayModeTests` saw `ORD-0002 Abandoned`). The second failure led directly to the ambient drop-off root cause. Both were fixed and the suites re-run.

## Checks not performed

- Owner playtest of the shipped build (the acceptance criterion).
- Manual in-game purchase of the shelf in the built player; covered by the PlayMode test and scene validation only.
- Human listening check of dialogue/ambient audio.
- No capture/screenshot review of the new HUD in the built player.
- Appliance model fidelity and character eyes/teeth/clothing re-export: explicitly out of scope per the handoff.

## Git

- Base: `579d39f` (`main` = `phase-8`), tag `phase-8`.
- This run is committed as one task-scoped commit on `phase-8`/`main`. Not pushed (requires owner approval).

## Known risks and limitations

- The 182 s two-order run is close to the new 180 s patience from hand-off; a slower machine or a player using the machines concurrently can still cause an abandonment (by design, but it is the tightest margin).
- The staff still has no notion of "whose item this is": any future prop placed in the order drop-off will again be processed as an order. The scene/authoring rule is documented in `AmbientNpc` and enforced by `PlaytestFixTool.Validate`.
- `PlaytestFixTool` is idempotent but rewrites the scene; re-run `Validate` after any scene edit.
- Batch PlayMode with `Time.captureFramerate` runs game time faster than wall time, so the 13.1 s wall time is not a real-time duration.

## Next recommended task

Owner playtest of `Build/LaundryLegends.exe`: complete at least two orders, confirm the wallet passes $200, buy the back-room shelf, and enter the room. Then Phase 9 (arcade expansion) or the deferred appliance/character art work.
