# Phase 5 — Customers and payments: implementation plan

- **Status:** planned
- **Depends on:** Phase 4 (complete at `cf0d79d`)
- **Objective:** a customer arrives, hands over an order, waits, collects it, pays
  exactly once, and leaves — repeatable without reloading the scene.

## What already exists (do not rebuild)

| Piece | Where |
| --- | --- |
| Wash/dry/fold/wrap machine loop | `MachineLoadHook`, `FoldingStation`, `LaundryLoopTests` |
| Staff automation + visible carrying | `LaundryStaff`, `CarrySocket`, `NpcLocomotion` |
| Walking customer with a route | `AmbientNpc`, `Waypoint_*` under `AmbientActors` |
| Pickup customer | `PickupCustomer` |
| Dialogue bank + voices | `DialogueBank`, `DialogueVoiceLibrary`, `DialogueVoicePlayer`, `SubtitleView` |
| Money model | `GameState`, `GameSession` (has constructor tests, no spend API yet) |

## Tasks

| ID | Task | Scope | Acceptance |
| --- | --- | --- | --- |
| LL-P5-001 | Order model | `Scripts/Customers/LaundryOrder.cs`: id, item reference, stage mirror, price, state, timestamps. No scene work. | Unit tests: state transitions legal/illegal, price fixed at creation, id unique. |
| LL-P5-002 | Customer state machine | `Scripts/Customers/CustomerAgent.cs`: `Arrive → Queue → HandOff → Waiting → Collect → Pay → Leave`. Reuses `AmbientNpc` waypoints, `NpcLocomotion`, `CarrySocket`. | PlayMode: a scripted customer visits every state once, in order. |
| LL-P5-003 | Order handoff at the counter | Player (or staff) accepts the order; order is linked to the dropped `LaundryItem`. `FoldingStation` completion marks the order `Ready`. | Order tracks the same item instance from drop-off to wrap. |
| LL-P5-004 | Ticket UI | Small HUD panel: order number, current stage (`Washing 42%`), price. Reuse existing IMGUI prompt style. | PlayMode: ticket text updates with machine progress. |
| LL-P5-005 | Payment | `GameState.TrySpend/TryCredit` with an **exactly-once** guard per order id. Receipt feedback line. | EditMode: duplicate payment attempt is rejected. PlayMode: money changes once. |
| LL-P5-006 | Patience + tip + soft-lock fallback | Patience timer per customer; leaving without paying puts the order on a "ready" shelf; no dead-end state. | PlayMode: impatient customer leaves, order remains valid and collectable. |
| LL-P5-007 | Queue / spots | 2–3 waiting spots by the window, simple slot claim/release. No NavMesh required. | PlayMode: two customers do not overlap or share a spot. |
| LL-P5-008 | End-to-end loop + evidence | One PlayMode test from arrival to payment; manual playtest notes. | Test green; capture set; human confirms it reads clearly. |

## Order of work

1. P5-001 model + tests (pure logic, no scene).
2. P5-003 handoff wiring (smallest path to a real order).
3. P5-002 state machine, then P5-004 ticket UI.
4. P5-005 payment (critical: exactly once).
5. P5-006 patience/fallback, P5-007 queue.
6. P5-008 end-to-end and review.

## Visual debt carried over from Phase 4

These are known defects, recorded so they are not silently forgotten:

| Debt | Impact | Proposed fix |
| --- | --- | --- |
| NPCs are a faceless tinted mannequin | High — weakest visual in the game | Ship a character whose FBX already contains a Unity humanoid avatar, or import a Mixamo character; stop hand-mapping rigs |
| Wrapped bundle renders opaque pale | Medium | Dedicated material pass: verify both renderers, use a proper transparent shader, check `_Mode`/keywords after import |
| Carried bundle floats instead of being gripped | Medium | Drive arm bones toward hand targets (IK or a hand-authored carry clip) instead of a chest anchor |
| Room lighting flat, dark ceiling | Medium | Reflection probes, bake or fake bounce, emissive fixture meshes with matching lights |
| Voice naturalness unverified | Unknown | Human listening pass on 5 representative lines |

Recommendation: fix the **character art** before or alongside P5-002, because the
customer state machine is the first thing a player watches closely.

## Risks

- **Payment duplication** is the critical defect class; the guard belongs in the model, not the UI.
- **Navigation** can balloon; keep waypoint walking and slot claiming, no NavMesh.
- **Fun** is not proven by tests; P5-008 needs a human playtest before the phase is called done.
- **Carryover art debt** can make a mechanically correct phase feel unfinished; decide explicitly whether Phase 5 acceptance includes it.

## Validation

- Compile, EditMode, PlayMode (including the new end-to-end loop), log scan.
- Capture set after the loop: customer at counter, ticket UI, ready bundle,
  payment moment, customer leaving.
- Record unrun checks honestly; no phase completion without evidence.
