# 0013 - Phase 10: business operations, stock, order queue and bills

**Date:** 2026-11-02
**Status:** Accepted for Phase 10 planning
**Supersedes:** nothing; adds a new phase between the arcade (Phase 9) and characters (Phase 11).

## Context

The laundromat currently processes one customer at a time and has no consumable
costs or recurring overhead. Phase 10 adds the missing business layer: stock that
can run out, several orders in flight, a ready-order shelf, and a calendar with
rent and utility bills.

The owner's requirements:

- Separate shelves for consumables, 10 per item, one unit per purchase at a fixed cost.
- Running out blocks processing until the player restocks.
- Up to 5 new orders at once; more than one order can be ready.
- A separate shelf for ready orders.
- A 24-hour clock, Day 1 onward, with a bill every 30 days for rent, electricity and water.
- Bills are paid from a menu.

## Goals

1. Make consumables a real operating cost, not a cosmetic counter.
2. Let several orders progress independently without the shop feeling like a queue of one.
3. Give the player a visible ready-order shelf and a clear pickup/payment flow.
4. Create a predictable 30-day cash cycle with recoverable consequences.
5. Keep every new state serializable for the Phase 14 save work.

## Non-goals

- A full day/night visual cycle. The clock is economic; lighting stays static for now.
- Employee wages, taxes, loans, or dynamic utility pricing.
- Save/load in this phase (Phase 14).
- New art beyond placeholder shelf/prop geometry.

## Contracts

### Business clock

`BusinessClock` (MonoBehaviour, `Scripts/Business/`):

| Field | Type | Meaning |
|---|---|---|
| `Day` | int | Starts at 1, increments at midnight, unbounded |
| `MinuteOfDay` | int | 0-1439 |
| `RealSecondsPerGameMinute` | float | Default `1.0` (1 real second = 1 game minute) |
| `SkipToNextDay()` | method | Jumps to 08:00 the next day; used by a counter/office interaction and tests |
| `AdvanceMinutes(int)` | method | Deterministic test/authoring advance |
| `Tick(float)` | method | Real-time advance, public for tests |

Display: `Day 1  08:00`. A 24-hour day is 24 real minutes at the default; the skip
action keeps a 30-day bill reachable without waiting 12 real hours. Pacing is a
Phase 15 balance item.

### Billing

- A bill is generated when the clock rolls from day 29 to day 30, and every 30
  days after that (day 60, 90, ...).
- Line items: **Rent $250**, **Electricity $90**, **Water $60** = **$400**.
- Due at generation. A **3-day grace** period is allowed.
- After the grace period, add a **10% late fee once** and keep the bill open.
- Paying uses `EconomyService.TrySpend`; an unaffordable bill stays open and the
  wallet never goes negative. No shutoff in this phase; the open bill is the
  pressure and the recovery path.
- Bills are paid from a `Bills` menu (`B` key) or a counter/office interactable.
  The menu lists each line and a `Pay bill` action.
- Only the current bill is active. If it is still unpaid when the next bill is
  generated, the old one remains listed; the player can pay oldest first.

### Consumables

`ConsumableDefinition` (ScriptableObject):

| Field | Type | Meaning |
|---|---|---|
| `Id` | string | `consumable.detergent`, etc. |
| `DisplayName` | string | HUD label |
| `UnitCost` | int | Cost to add one unit |
| `Capacity` | int | 10 per shelf in Phase 10 |
| `Stage` | enum | `Wash`, `Dry`, `Wrap` |

Initial items:

| Item | Stage | Unit cost | Capacity |
|---|---|---|---|
| Detergent | Wash | $2 | 10 |
| Fragrance | Dry/finish | $3 | 10 |
| Plastic | Wrap | $1 | 10 |

- One shelf per item, placed in the shop; interacting buys **one unit** and
  debits `UnitCost` through `EconomyService`.
- A full shelf rejects the purchase without spending.
- Stock is a serializable count per item.

### Consumption and blocking

- Starting a wash consumes 1 detergent.
- Starting a dry/finish consumes 1 fragrance.
- Wrapping consumes 1 plastic.
- If the required stock is 0, the stage refuses and the interaction/ticket says
  which item is missing. The order stays in its current stage; nothing is
  silently skipped.
- The customer's patience still applies, so a long stock-out can lead to an
  abandoned order. That is recoverable by restocking.

### Order queue and ready shelf

- Up to **5 active orders** at once (configurable).
- Up to **10 wrapped orders** on the ready shelf.
- Each active order has its own drop-off slot and ticket. Customers can wait
  concurrently; the player is not forced to finish one order before another starts.
- When an order reaches `Wrapped`, its parcel moves to a ready-shelf slot and the
  matching customer walks to the shelf, pays, and leaves.
- If the ready shelf is full, wrapping waits and the ticket says so.
- A queue HUD lists ticket id, state, and blocker (stock/ready shelf/customer).

### HUD

- Clock and day, stock counts (`Detergent 8/10`), active/ready counts, next bill.
- Bills menu on `B`.
- Keep it IMGUI for now, matching the Phase 9 panel.

## Balance placeholders (tune in Phase 15)

| Item | Value |
|---|---|
| Detergent / Fragrance / Plastic | $2 / $3 / $1 per unit |
| Shelf capacity | 10 |
| Active orders | 5 |
| Ready shelf | 10 |
| Rent / Electricity / Water | $250 / $90 / $60 per 30 days |
| Late fee | 10% after 3 days |
| Clock | 1 real second = 1 game minute |

## Task order

1. `LL-P10-001` contracts (this document) - complete.
2. `LL-P10-002` business clock and day counter.
3. `LL-P10-003` consumable definitions, shelves, restock, stock HUD.
4. `LL-P10-004` consumption and out-of-stock blocking.
5. `LL-P10-005` five-order queue and concurrent customers.
6. `LL-P10-006` ready-order shelf and ticket pickup.
7. `LL-P10-007` 30-day billing and bills menu.
8. `LL-P10-008` HUD for clock, stock, queue and bills.
9. `LL-P10-009` combined balance and long-session tests.
10. `LL-P10-010` owner playtest and closeout.

## Acceptance mapping

| Phase 10 criterion | Task evidence |
|---|---|
| Stock blocks processing and is recoverable | 003/004 |
| Five active orders, multiple ready | 005/006 |
| 30-day itemized bill, payable from a menu | 007 |
| No negative balance / recoverable bill | 007/009 |
| Clock advances and can skip days | 002 |
| HUD readable | 008 |
| Human verifies the loop | 010 |

## Risks

- `CustomerSpawner`/`CustomerAgent` are shared with Phases 5-9; keep the single-order
  path working while adding concurrency.
- The ready shelf changes payment timing; exactly-once payment must remain.
- Clock pacing: 30 days at 1 real minute per game minute is long; the skip action
  must be present from day one of implementation.
- Bills must not deadlock a broke player; the open-bill state is the recovery path.
- Save work (Phase 14) must persist clock, stock, queue and bills; keep them serializable.

## Save-shape note

`BusinessClockState`, `ConsumableStock`, `OrderQueueState` and `BillState` are
serializable and scene-free, so Phase 14 can persist them without coupling to
the scene.
