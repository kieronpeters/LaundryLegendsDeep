# 0012 - Phase 9: arcade expansion contracts

**Date:** 2026-11-02
**Status:** Accepted for Phase 9 planning
**Supersedes:** nothing; builds on decision 0007 and the Phase 8 arcade implementation.

## Context

Phase 8 ships **one** arcade cabinet: a single `ArcadeService`, one `ArcadeCabinet`, one
`ArcadeTraffic` visitor loop with fixed interval/income, a fullscreen fight session, and
exactly-once visit income. Phase 9 must grow this into a small arcade business without
turning the scene into an unmaintainable pile of one-off components.

## Goals

1. More than one machine, bought and placed **spatially**.
2. Upgrades and decoration purchases that visibly change the room.
3. Laundry performance (reputation/footfall) measurably feeds arcade traffic.
4. Income scaling plus an operating cost, so the business has a margin to manage.
5. A room expansion with more capacity, visible geometry.
6. Progression milestones that read clearly in the HUD.

## Non-goals

- New laundry machines or a day/night cycle (later phases).
- Saving/loading (Phase 8 criterion is conditional; the state stays serializable).
- Photoreal art; the fight stays a stylised pixel game.
- Multiplayer, leaderboards, or online services.

## Contracts

### Machine definition

`ArcadeMachineDefinition` (ScriptableObject, `Assets/LaundryLegends/ScriptableObjects/Arcade/`):

| Field | Type | Meaning |
|---|---|---|
| `Id` | string | Stable id, e.g. `arcade.fighter` |
| `DisplayName` | string | HUD/prompt label |
| `Cost` | int | Purchase price, debited once through `EconomyService` |
| `BaseVisitIncome` | int | Coins per completed visit before multipliers |
| `AttractWeight` | float | Share of visitors this machine attracts |
| `Footprint` | Vector2 | metres (x, z) used by placement validation |
| `Prefab` | GameObject | Cabinet visual; may be a variant of the current one |

`ArcadeService` becomes a **multi-slot** owner: a serialized list of
`ArcadeSlot { InstallAnchor, Definition, ArcadeState }`. The existing single-machine
API (`Phase`, `IsActive`, `TryCollectVisitIncome`, `SetStartActive`) delegates to slot 0
so all Phase 8 tests keep passing.

### Acquisition and capacity

- The back room starts with **three** install anchors (east wall, north wall, south wall).
- Each anchor is an `ArcadeInstallPoint` with a definition and a price; buying debits
  once, spawns the cabinet at the anchor, and moves that slot to `Installing -> Active`.
- Capacity = number of anchors unlocked by the room (3, then 5 after expansion).
- Placement is fixed per anchor; no free placement in Phase 9 (spatial enough, testable).

### Upgrades and decoration

`ArcadeUpgradeDefinition`:

| Field | Type | Effect |
|---|---|---|
| `Id` | string | e.g. `arcade.upgrade.neon` |
| `DisplayName` | string | HUD label |
| `Cost` | int | one-time |
| `IncomeMultiplier` | float | multiplies visit income |
| `TrafficMultiplier` | float | divides visitor interval |
| `DecorPrefab` / `DecorObject` | reference | spawned/enabled when bought |

Upgrades are bought at an arcade kiosk interactable (not the player's `U` key, which
stays laundry upgrades). Ownership is recorded in `ArcadeState` and paid through
`EconomyService`, so a repeat purchase is rejected without moving money.

### Laundry reputation and footfall

- `LaundryReputation` is a value 0-100.
- `+4` per paid order, `+2` when a tip is earned, `-2` per abandoned order.
- Decays `1` per 120 s of play to a floor of 0, so neglect has a cost.
- `FootfallMultiplier = 0.8 + 0.6 * (reputation / 100)` (range 0.8-1.4).

### Traffic and income

- Visitor interval = `baseInterval / (FootfallMultiplier * trafficMultiplier * machineFactor)`,
  clamped to a sane floor (e.g. 6 s).
- `machineFactor` grows sub-linearly with active machines (e.g. `sqrt(count)`), so more
  machines help without multiplying traffic linearly.
- Visit income = `definition.BaseVisitIncome * incomeMultiplier`, paid through
  `TryCollectVisitIncome` (exactly-once by visit id, unchanged).
- Each visitor picks a machine by `AttractWeight`; the visit records the machine id.

### Operating cost

- Every **120 s** of active arcade time, debit `activeMachineCount * 1` as
  `"Arcade upkeep"` through `EconomyService`.
- If the wallet cannot pay, log once and skip (no debt, no negative balance).
- The upkeep timer is advanced by `Tick`, so tests can simulate minutes.

### Expansion

- `ArcadeExpansionPoint` in the back room sells a **second bay** for a one-time cost.
- Buying it enables a visible extension (reuses the shell builder with a second 6x4 m
  bay and two more anchors) and raises capacity from 3 to 5.
- Reuses the existing doorway/wall/threshold helpers; do not fork them (see tech debt
  LL-T-005).

### Progression milestones

| Id | Condition | Reward/feedback |
|---|---|---|
| `first-machine` | slot 0 active | toast + HUD check |
| `second-machine` | 2 active | toast |
| `income-250` | arcade lifetime income >= 250 | toast |
| `reputation-50` | reputation >= 50 | toast |
| `expanded` | expansion bought | toast |

Milestones are stored in `ArcadeState`, fire once, and are surfaced in an arcade progress
panel (HUD) plus the order ticket summary.

### Save-shape note

All new fields go on serializable classes (`ArcadeState`, `LaundryReputation`,
`ArcadeUpgradeState`) so the future save system can persist them without scene coupling.

## Balance placeholders (tune in LL-P9-009)

| Machine | Cost | Income/visit | Notes |
|---|---|---|---|
| Fighter (existing) | 120 | 3 | baseline |
| Racer | 250 | 5 | faster visitor turn |
| Puzzle | 400 | 8 | rarer attraction |

Upgrades: Neon sign 150 (+10% income), Better speakers 200 (+20% traffic), Carpet 120
(+10% both). Expansion 300. Upkeep 1/machine/120 s.

## Task order

1. `LL-P9-001` contracts (this document) - complete.
2. `LL-P9-002` machine catalogue + multi-slot service.
3. `LL-P9-003` extra machine purchase/placement.
4. `LL-P9-004` upgrades and decoration.
5. `LL-P9-005` reputation -> traffic.
6. `LL-P9-006` income scaling + upkeep.
7. `LL-P9-007` expansion bay.
8. `LL-P9-008` milestones + HUD.
9. `LL-P9-009` combined tests + long session.
10. `LL-P9-010` owner playtest and closeout.

## Acceptance mapping

| Phase 9 criterion | Task evidence |
|---|---|
| Grow beyond one machine | 002/003 (3 anchors, tests) |
| Laundry actions affect arcade traffic | 005 (reputation -> interval/income test) |
| Expansion is spatial and visible | 007 (new bay geometry + capture) |
| Combined economy understandable/recoverable | 006/009 (upkeep, summary, long-session test) |
| Human verifies the dual-business loop | 010 (owner playtest) |

## Risks

- `ArcadeService` refactor can break Phase 8 tests; keep slot 0 delegating and run suites
  after each step.
- Scene growth increases capture/iteration time; reuse the shell builder and keep geometry
  primitive.
- Balance is placeholder until the long-session test; do not over-tune early.
- Reputation decay can confuse players if invisible; surface it in the HUD from day one.
