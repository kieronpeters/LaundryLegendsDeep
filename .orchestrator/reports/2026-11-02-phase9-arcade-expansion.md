# Phase 9 — Arcade expansion: implementation report

**Date:** 2026-11-02
**Phase:** 9 — Arcade expansion
**Status:** Automated implementation complete; **owner playtest (LL-P9-010) pending**
**Commit:** `5881945` (implementation) and `b8e66ad` (self-review fixes) on `phase-9`
**Build:** `Build/LaundryLegends.exe` (198 MB, 0 build errors)
**Decision:** `.orchestrator/decisions/0012-phase-9-arcade-expansion.md`

## Objective

Grow the single Phase 8 cabinet into a small arcade business: multiple machines,
upgrades and decoration, laundry reputation feeding footfall, scaled income with
upkeep, a visible expansion bay, and milestone feedback.

## Tasks completed

| Task | Result |
|---|---|
| LL-P9-001 | Contracts and task breakdown accepted |
| LL-P9-002 | Machine catalogue + multi-slot `ArcadeService`; slot 0 keeps the Phase 8 API |
| LL-P9-003 | Four extra anchors; purchase, placement and capacity rules |
| LL-P9-004 | Three upgrade kiosks with income/traffic multipliers and decor |
| LL-P9-005 | Reputation from paid/tip/abandon + decay, feeding footfall and income |
| LL-P9-006 | Scaled visit income and 120 s upkeep, reconciling with the wallet |
| LL-P9-007 | Visible, walkable 6x4 m expansion bay; capacity 3 -> 5 |
| LL-P9-008 | Five one-time milestones and an IMGUI arcade panel |
| LL-P9-009 | Combined economy/progression verification and Windows build |
| LL-P9-010 | **Pending owner playtest** |

## Files and assets

### Created

- `Assets/LaundryLegends/Scripts/Arcade/ArcadeMachineDefinition.cs`
- `Assets/LaundryLegends/Scripts/Arcade/ArcadeExtraMachine.cs`
- `Assets/LaundryLegends/Scripts/Arcade/ArcadeUpgradeDefinition.cs`
- `Assets/LaundryLegends/Scripts/Arcade/ArcadeUpgradePoint.cs`
- `Assets/LaundryLegends/Scripts/Arcade/ArcadeExpansionPoint.cs`
- `Assets/LaundryLegends/Scripts/Arcade/ArcadeMilestones.cs`
- `Assets/LaundryLegends/Scripts/Arcade/LaundryReputation.cs`
- `Assets/LaundryLegends/Scripts/Arcade/ArcadePanelView.cs`
- `Assets/LaundryLegends/ScriptableObjects/Arcade/ArcadeMachine_Fighter.asset`
- `Assets/LaundryLegends/ScriptableObjects/Arcade/ArcadeMachine_Racer.asset`
- `Assets/LaundryLegends/ScriptableObjects/Arcade/ArcadeMachine_Puzzle.asset`
- `Assets/LaundryLegends/Art/Materials/MAT_ArcadeNeon.mat`
- `Assets/LaundryLegends/Tests/EditMode/ArcadeExpansionTests.cs`
- `Assets/LaundryLegends/Tests/PlayMode/ArcadeExpansionPlayModeTests.cs`

### Modified

- `Assets/LaundryLegends/Scripts/Arcade/ArcadeService.cs` — multi-slot owner,
  upgrades, reputation, upkeep, expansion, milestones.
- `Assets/LaundryLegends/Scripts/Arcade/ArcadeState.cs` — upgrades, milestones,
  multipliers, prefixed visit ids.
- `Assets/LaundryLegends/Scripts/Arcade/ArcadeCabinet.cs` — slot-aware power.
- `Assets/LaundryLegends/Scripts/Arcade/ArcadeInstallPoint.cs` — slot-aware purchase.
- `Assets/LaundryLegends/Scripts/Arcade/ArcadeTraffic.cs` — machine selection,
  footfall-scaled cadence, per-slot income.
- `Assets/LaundryLegends/Scripts/Arcade/ArcadeVisitor.cs` — slot-aware visits.
- `Assets/LaundryLegends/Scripts/Customers/OrderLedger.cs` — reputation hooks.
- `Assets/LaundryLegends/Scripts/Customers/CustomerSpawner.cs` — reputation wiring.
- `Assets/LaundryLegends/Editor/HiddenRoomSetupTool.cs` — bay geometry, anchors,
  kiosks, decor, machine definitions, reputation and HUD.
- `Assets/LaundryLegends/Scenes/Laundromat.unity` — rebuilt back room.
- `Assets/LaundryLegends/Tests/PlayMode/ArcadeCabinetInteractTests.cs` — selects
  slot 0 explicitly now that the scene contains inactive extra cabinets.
- `MAT_Poster_SpinCycle.mat`, `MAT_Poster_StreetFolders.mat` — emission keyword
  refreshed by the poster builder during the rebuild.

## Scene layout

- Room: 8 x 8 m at x=8.2, door at z=3.0.
- Slot 0: Fighter cabinet on the east wall (shipped active).
- Slot 1: Racer anchor on the north wall.
- Slot 2: Puzzle anchor on the south wall.
- Slots 3-4: expansion bay south of the room, sealed until purchase.
- Upgrade kiosks: neon, speakers, carpet.
- Decor: neon sign, two speakers, arcade rug (hidden until bought).
- `ArcadeBusiness`: `LaundryReputation` (starts at 20) and `ArcadePanelView`.

## Checks performed

| Check | Result | Evidence |
|---|---|---|
| Script compile (batch) | Passed, exit 0 | `Logs/phase9-compile.log` |
| Scene builder | Passed, exit 0; `[BACKROOM]` layout logged | `Logs/phase9-scene.log` |
| EditMode suite | **158 / 158 passed** | `TestResults/phase9-editmode-7.xml` |
| PlayMode suite | **59 passed, 0 failed, 2 explicit skipped** | `TestResults/phase9-playmode-4.xml` |
| Explicit $300 progression | **1 / 1 passed** | `TestResults/phase9-progression-2.xml` |
| Windows build | **Succeeded, 198 MB, 0 errors** | `Logs/phase9-build.log`, `Build/LaundryLegends.exe` |
| Scene wiring (PlayMode) | 5 slots, capacity 3, second machine playable, upgrade kiosk, expansion bay, floor raycast, HUD text | `ArcadeExpansionPlayModeTests` |
| Combined economy (EditMode) | 9 laundry orders -> reputation 54 -> traffic -> slot income -> second machine -> expansion; wallet/ledger reconcile | `LongSessionLinksLaundryReputationTrafficIncomeSecondMachineAndExpansion` |

## Checks not performed

- **Visual capture review:** no new screenshots of the expansion bay, HUD or
  kiosks were taken. The PlayMode test verifies geometry and colliders, not looks.
- **Build launch smoke test:** the exe was produced but not launched from a clean
  directory in this run. Owner manual launch is the next check.
- **Owner dual-business playtest:** pending (LL-P9-010).
- **Save/load:** out of scope for Phase 9.

## Self-review fixes (`b8e66ad`)

- **Purchase pedestal blocked the bought cabinet.** The install-point box sat
  1.15 m in front of the anchor and stayed after purchase, between the player and
  the cabinet. It is now hidden when the slot activates; `IsReachable` uses the
  anchor only, so capacity is unaffected.
- **Extra cabinets' attract modes never stopped when played.** Each extra slot's
  `ArcadeAttractMode` now follows its own power and busy state every frame.
- **The expansion kiosk stood in the middle of the new doorway.** Moved to the
  south-west corner so the 2.4 m opening is clear.
- **Scene rebuild could select the wrong cabinet.** It now uses
  `service.GetSlotCabinet(0)` instead of the first cabinet found, which previously
  could be an inactive extra clone that was then destroyed with the shell.
- **Reputation decay looped per point.** It now applies the whole elapsed decay in
  one step, so a large simulated delta cannot stall a test.
- **Inline upgrade definitions leaked.** The kiosk releases its runtime
  ScriptableObject on destroy.
- Added PlayMode assertions that the service, spawner and HUD all share the same
  reputation tracker.

## Known risks and limitations

- The four extra cabinets are duplicates of the Fighter cabinet; Racer and Puzzle
  differ in price, income, attract weight and label, not in visuals or screen art.
  Deferred as `LL-A-003`.
- Upgrade decor is primitive geometry (boxes/rug). Deferred as `LL-A-004`.
- The HUD is IMGUI; placement/readability at 1920x1080 is unverified. Deferred as
  `LL-B-008`.
- Balance values are placeholders: Fighter 120/3, Racer 250/5, Puzzle 400/8,
  upgrades 150/200/120, expansion 300, upkeep 1/machine/120 s. Tune after the
  owner session.
- Reputation starts at 20 so the room is not dead on arrival; adjust after play.
- No save/load for the new arcade state yet (serializable, but not persisted).

## Deferred defects

| Ticket | Severity | Target | Summary |
|---|---|---|---|
| `LL-A-003` | P2 | Phase 11 | Extra cabinets reuse the Fighter model/screen; author Racer/Puzzle visuals. |
| `LL-A-004` | P2 | Phase 11 | Arcade upgrade decor is placeholder primitive geometry. |
| `LL-B-008` | P2 | Phase 15 | Capture/visual review of the expansion bay, kiosks and HUD at 1080p. |

None block the Phase 9 acceptance criteria; they are recorded per guardrails 13.

## Build and controls

- Executable: `Build/LaundryLegends.exe`
- Launch: Bootstrap -> MainMenu -> Laundromat.
- Controls: WASD move, mouse look, E interact, Q drop, U buy laundry upgrade, M map, Esc pause.
- Phase 9 interactions: use the kiosk pedestals in the back room to buy Racer/Puzzle;
  use the three upgrade kiosks; use the expansion pedestal near the south opening.
- Arcade panel is top-right once the back room is usable.

## Next task

**LL-P9-010 — owner playtest and Phase 9 closeout.** Verify the dual-business loop
is coherent and fun, then record the result, mark Phase 9 complete, and tag it.
