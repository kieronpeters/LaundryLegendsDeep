# Decision 0005 - Phase 3 Interaction and Carry Contracts

**Date:** 2026-10-30
**Status:** Accepted
**Decision owner:** Lead agent, under the project owner's 2026-10-30 instruction to review, approve, and complete the next step

## Context

Phase 2 is complete and owner-reported accepted. The existing interaction layer is only a stub:

- `IInteractable` exposes a prompt and `Interact(GameObject)`.
- `SimpleInteractable` is a minimal event relay.
- `PlayerInteractor` raycasts, reports one target/prompt, and dispatches the E key directly.

There is no eligibility check, carry state, item identity, prompt view, or machine load hook. Phase 3 must add those without pulling Phase 4 machine cycles, economy, orders, customers, or save behavior forward.

## Decisions

### Interaction contract

- `IInteractable` exposes:
  - `string InteractionPrompt { get; }`
  - `bool CanInteract(GameObject interactor)`
  - `void Interact(GameObject interactor)`
- `SimpleInteractable` remains eligible by default, supports being disabled, raises its event only when eligible, and stays usable by existing tests.
- `PlayerInteractor` owns raycast target resolution and exposes:
  - `CurrentInteractable`, `CurrentTarget`, `CurrentPrompt`
  - `InteractKey` and `InteractKeyLabel`
  - a public `TryInteract()` path for tests and future input layers
  - a target/prompt change notification used by the prompt view
- Out-of-range, blocked, disabled, ineligible, and paused targets never dispatch.
- The interaction key has one source in `PlayerInteractor`; the prompt view reads its label and does not hard-code `E`.
- Legacy Input Manager remains the input path. No Input System package or `ProjectSettings/InputManager.asset` change is included.

### Type ownership

| Folder | Types and responsibility |
|---|---|
| `Scripts/Interaction` | `IInteractable`, `SimpleInteractable`, `PlayerInteractor`, `CarrySlot` |
| `Scripts/Player` | `CarryController` on the Player prefab |
| `Scripts/Laundry` | `LaundryItem`, `LaundryItemKind`, `MachineLoadSlot` |
| `Scripts/UI` | `InteractionPromptView` placeholder |
| `Editor` | Idempotent Phase 3 setup and validation methods |

### Item identity and kinds

- `LaundryItemKind` has exactly `Basket`, `LooseLaundry`, and `Bag`.
- `TemplateId` is a non-empty prefab-template identifier, unique per prefab asset. Validation checks template uniqueness.
- `InstanceId` is generated uniquely per spawned item instance and remains stable for that instance's lifetime. Phase 3 does not add save/load; a future save task must serialize the instance identity.
- `Basket` is a carryable container placeholder. Its contents and load grouping are **out of scope** for Phase 3.
- `LooseLaundry` is the only kind marked washer-loadable.
- `Bag` is a carryable completed-order placeholder. It has no order behavior in Phase 3.

### Carry and placement

- Carry is attachment-based, not physics-based. The player may hold at most one item.
- `CarryController` attaches the item to an explicit hold anchor and safely disables its world collider/physics while held.
- Drop restores a valid world state. A blocked drop uses a safe fallback rather than losing the item or placing it inside geometry.
- `CarrySlot` placement accepts one compatible item only when empty; occupied or incompatible slots are rejected without changing ownership.
- No-stuck safeguards cover a held item being destroyed or disabled, the carry component being disabled or the scene unloading while holding, blocked drop, and double interaction in one frame.
- Machine load/unload is a separate hook: one washers accepts one `LooseLaundry` item, rejects `Basket`, `Bag`, and occupied loads, and returns the exact same item on unload.
- Machine cycles, timers, economy, orders, customers, and save/load remain out of scope.

### Prompt UI

- Phase 3 uses `InteractionPromptView` as an IMGUI placeholder on the Player prefab.
- It renders from testable `IsVisible` and `CurrentLabel` state, not directly from untestable `OnGUI` internals.
- It hides when there is no valid target, the target becomes ineligible, or the game is paused, including `Time.timeScale == 0`.
- It uses the built-in IMGUI font. It does not import TextMeshPro Essentials, add a package, or create final UI prefabs.
- Final HUD/UI work remains in the UI phase.

### Scene and prefab safety

- Item prefabs live under `Prefabs/Gameplay/Laundry` and use the existing simple naming style (`Basket.prefab`, `LooseLaundry.prefab`, `Bag.prefab`) rather than introducing a mixed `PF_` convention in Phase 3.
- Scene/prefab edits are additive and require a Git checkpoint before editing.
- Existing room geometry, route clearances, and references must be preserved.
- Phase 3 setup methods are idempotent; validators fail loudly if a hook is missing or duplicated.
- Captures must run without `-nographics`; Phase 2 proved that capture mode crashes under `-nographics`.

## Out of scope

- Basket contents, load grouping, weight, and capacity rules.
- Washer/dryer cycle timers, state machines, progress UI, and audio.
- Order lifecycle, economy, customers, payment, and save/load.
- Final HUD, TextMeshPro import, package changes, input-system migration, or project-setting changes.
- Secret-room reveal or arcade behavior.

## Consequences

- Phase 3 can be tested without waiting for machine or order systems.
- Item identity exists early but is not persisted until the save phase.
- The IMGUI prompt is intentionally temporary and must not be mistaken for final UI.
- The machine hook is a load/unload boundary only; Phase 4 must extend it rather than replace item identity or carry rules.

## Revisit triggers

- A decision to introduce container/load grouping before Phase 4.
- A save-format task that must persist item instance IDs.
- A UI task that replaces the IMGUI prompt with final UGUI/TextMeshPro.
- A machine-cycle task that needs a different load contract.
- Evidence that attachment-based carrying causes clipping or discomfort.
