# Phase 5 — closure record

- **Date:** 2026-11-01
- **Decision:** closed, with one criterion delegated to the owner (playtest).
- **Final suites:** EditMode **67/67**, PlayMode **49 passed, 0 failed, 1 skipped**
  (`TestResults/close-editmode.xml`, `TestResults/close-playmode.xml`).

## Acceptance criteria — final

| Criterion | Status | Evidence |
| --- | --- | --- |
| Customer arrives, hands over, waits, collects, pays, leaves | **Met** | `CustomerWalksInWaitsPaysOnceAndLeaves` (~11 s, real staff + machines) |
| Loop repeatable without console or reload | **Met** | `CustomerRespawnsAfterLeaving` — second customer spawns after the first leaves |
| Money changes exactly once per order | **Met** | `PayingTwiceCreditsMoneyOnce`, `PayingTheSameOrderTwiceCreditsOnce` |
| No soft-lock on a long wait; fallback documented | **Met** | Patience abandon + pipeline cleanup; `ImpatientCustomerAbandonsAndLeavesWithoutPaying` asserts the folding station stays free |
| Human verifies the loop is understandable and satisfying | **Delegated** | Owner is away; capture set + this record handed over. Not claimed as passed. |

## Deliverables

| Deliverable | Status |
| --- | --- |
| Customer lifecycle and spawn behaviour | Met (single customer at a time) |
| Order creation and handoff | Met |
| Waiting, patience, collection, payment | Met |
| Money HUD update and receipt feedback | Met — ticket shows wallet balance, order stage, price, and a 5-second receipt line |
| At least one customer archetype | Met |
| End-to-end playable order loop | Met |
| Tests for payment and loop completion | Met (three PlayMode lifecycle tests + ledger tests) |
| Queue behaviour | **Partial** — one customer at a time; no multi-customer waiting spots |

## Fixes made while closing

1. Abandoned orders left their bundle in the single-slot folding station, blocking
   the next customer. `MachineLoadHook.ForceUnload` plus a full cleanup sweep now
   clears slot, machines, folding station, and any carry socket.
2. Tinted shirt material was created in memory and lost on prefab save, rendering
   magenta. It is now a saved material asset.
3. Order ledger test compared a laundry item against an order.

## Honest limits carried forward

- Multi-customer queue is not implemented.
- Tip is a flat constant (10 coins if served inside the tip window).
- No player-facing payment screen; payment happens at collection.
- Phase 4 visual debt still open: carried bundle floats rather than being gripped,
  room lighting is flat, voice lines have never been heard by a human.
