# Live playtest report - 2026-11-04

**Build:** `Build/LaundryLegends.exe`, 212 MB, 0 errors
**Branch:** `main` (merged) with `phase-13-16` checked out
**Session:** automated first-person drive on the owner's desktop, 2560x1440 fullscreen
**Result:** game runs and the economy/queue/billing systems function, but the manual interaction loop is blocked and several visual defects were found.

## What was exercised

- Main menu -> START GAME -> Laundromat.
- WASD movement and mouse look; walked through the storefront and back inside.
- Machine wall close-up, counter, waiting area, ambient customer, staff.
- `U` purchase list, `B` bills menu, `Esc` pause/resume, `N` day skip, `M` map.
- Reached day 36; observed stock depletion, stopped orders, a $400 bill, an unaffordable payment attempt, and the one-time $40 late fee ($440 total).

## What worked

- Build, launch, menu, movement, camera, scene loading.
- Ten-order queue filled and the staff processed orders automatically.
- Payments accrued (coins 100 -> ~266) and the HUD showed daily net.
- Stock blocking worked: detergent 0 stopped processing.
- Bills: rent $250 + electricity $90 + water $60 = $400 due day 30.
- Unaffordable payment was refused with "Not enough money for the $400 bill" and the wallet stayed positive.
- Late fee applied once: total became $440.
- `N`, `M`, `Esc`/Resume and the U/B menus respond.

## Defects filed

| ID | Pri | Title |
|---|---|---|
| LL-B-016 | P1 | No interaction prompt at the washing machines |
| LL-A-007 | P1 | NPC carry pose throws arms out in a T-pose |
| LL-B-017 | P1 | Purchase menu rows overlap the header and are unreadable |
| LL-B-018 | P1 | Stock/order economy soft-locks after ~30 seconds |
| LL-B-019 | P2 | Order rack slots and ready parcels are invisible |
| LL-B-020 | P2 | Eight white blocks sit on the floor in front of the machines |
| LL-B-021 | P2 | Stray grey and orange boxes near the counter |
| LL-A-008 | P2 | Machine bodies blow out to pure white up close |
| LL-B-022 | P3 | Pause overlay is nearly invisible and has no settings |
| LL-B-023 | P3 | Bills panel uses a tiny text block in a large empty panel |
| LL-B-024 | P3 | Shop map is blank before the back room and overlaps the scene |

## Not reached

- Hidden room, expansion bay, arcade cabinets, carpet and cabinet themes.
- A full manual load -> wash -> dry -> fold -> handoff order.
- Manual restock purchase.

The interaction prompt (LL-B-016) and the stock soft-lock (LL-B-018) are the reasons the session could not progress to the arcade content.

## Recommended fix order

1. LL-B-016 interaction raycast/collider and prompt condition.
2. LL-A-007 carry pose.
3. LL-B-017 purchase menu layout.
4. LL-B-018 stock/order/price balance and a guaranteed restock path.
5. LL-B-019/020/021 prop placement; LL-A-008 exposure.
6. LL-B-022/023/024 UI polish (Phase 13).

## Follow-up iteration (same day)

Fixed and verified:

- **LL-B-016** machine interaction collider: the old collider covered only y 0.05-1.05, so the eye-level ray passed over it. `ApplianceIntegrationTool` now sizes the root BoxCollider to the detailed model bounds (probe rays hit `Machine_1..8` at 1.12 m). Added `Phase12SceneTests.MachineCollidersCoverEyeHeight`.
- **LL-A-007** T-pose regression: removed the Load/Fold/Handoff animator states and staff calls that produced the arms-out pose; restored the prior Idle/Walk controller. The fresh start-of-game capture shows arms at the sides.
- **LL-B-017** U menu: solid dark backdrop, per-row bands, rows moved below the header.
- **LL-B-018** economy: starting stock 10/10/10, respawn 20 s, bills $200 + $70 + $50 = $320, and `SetRespawnDelay` clamps the live timer. Live HUD confirmed 10/10/10.

New ticket: **LL-B-025** (P2) order customer walks through the player camera at spawn.

Verification: EditMode 164/164; PlayMode 69 total, 67 passed, 0 failed, 2 skipped; build 212 MB / 0 errors.

Not yet started: Phase 13 audio, Phase 14 save/load, Phase 15 balance/tutorial, Phase 16 QA/release. The combined 13-16 branch is therefore not ready to merge.
