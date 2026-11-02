# Phase 9 — owner approval and closeout

**Date:** 2026-11-03
**Phase:** 9 — Arcade expansion
**Status:** **Complete — owner approved**
**Branch:** `phase-9`
**Approval:** Owner approved the round 2 fullscreen build on 2026-11-03.
**Build:** `Build/LaundryLegends.exe` — 199 MB, 0 errors, fullscreen 2560x1440.
**Tag:** `phase-9-complete`

## Owner approval

The owner reviewed the round 2 build (purchase menu, bay-gated carpet, moved
posters, clear doorway, parcel bundle, rotating drums, NPC fixes) and approved it.
The approval is the human verification required by the Phase 9 acceptance
criteria. Known follow-ups are listed below and are not phase blockers.

## Acceptance mapping

| Criterion | Evidence |
|---|---|
| Grow beyond one machine | 5 slots (starter + 4 extra); PlayMode buys slot 1 and slot 3; EditMode multi-slot tests |
| Laundry actions affect arcade traffic/income | Reputation from paid/tip/abandon; footfall scales cadence and income; EditMode long-session test |
| Expansion is spatial and visible | 6x4 m bay, opening/seal, two anchors, bay-floor raycast; owner walked the build |
| Combined economy understandable/recoverable | Income/upkeep/expansion reconcile with the wallet; owner approved the loop |
| Human verifies the dual-business loop | Owner approval 2026-11-03 |

## Verification

- EditMode **159 / 159**
- PlayMode **59 passed / 0 failed / 2 explicit skipped**
- Machine wall **4 washers + 4 dryers**, each with a load slot and rotating drum
- Build **199 MB / 0 errors**
- Menu capture `TestResults/review2/02-purchase-menu.png`
- Fullscreen release capture `TestResults/release2/menu.png`

## Known follow-ups (not blockers)

| Ticket | Target | Summary |
|---|---|---|
| `LL-B-009` | Phase 11 | Ambient street customer stands on the counter and cannot hand off |
| `LL-B-010` | Phase 10 | Staff reuses the leftmost free washer/dryer instead of rotating |
| `LL-A-005` | Phase 12 | Washer/dryer realism pass |
| `LL-A-006` | Phase 12 | Cabinet side art/marquee |
| `LL-F-009` | Phase 11 | Male-customer voice reassignment |
| `LL-F-010` | Phase 12 | Racer/Puzzle play the same fight game; add per-machine modes |
| `LL-B-011` | Phase 10 | Purchase menu shows duplicate Racer/Puzzle labels; add `(bay)` |
| `LL-B-008` | Phase 16 | Final 1080p/visual HUD review |

## Next

Phase 10 (business operations) is planned: 10 active orders, consumable shelves,
stock blocking, ready shelf, 24-hour clock and 30-day bills. Partial Phase 10
work is preserved on the local `phase-10-wip` branch, not this release.

## Post-approval playtest tickets (2026-11-03)

Owner approved the build, then noted these for later. They are **not** part of
the approved build and do not reopen Phase 9.

- `LL-B-012` (P1): orders stop processing after walking from the shop into the back room. Needs a runtime audit; target Phase 10.
- `LL-B-013` (P2): the phone regular stands in the storefront debris; move him to the waiting area. Target Phase 11.
- `LL-A-005` (P2): replace all eight square-block appliances with real washer/dryer models. Target Phase 12.
- `LL-B-014` (P2): the dryer drum face spins but the visible clothes do not tumble. Target Phase 12.
- `LL-B-015` (P2): the expansion carpet is a small rug; make it a full-bay patterned bar/casino carpet, gated to the expansion. Target Phase 12.

Partial experiments for the order-stall audit and drum-clothes work are preserved
on the local `phase-9-postapproval-wip` branch, not in the approved build.
