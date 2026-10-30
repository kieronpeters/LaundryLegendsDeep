# Laundry Legends — Architecture

**Status:** Phase 2 complete; Phase 3 interaction foundation approved and in progress. The Unity project, scenes, prefabs, player, and test assemblies exist. Phase 3 implementation begins with `LL-P3-002`.
**Engine decision:** Unity 6.0 LTS `6000.0.64f1`, Built-In Render Pipeline, Windows 64-bit.

## 1. Architecture principles

- Prefer simple Unity components and direct references over frameworks.
- One script, one clear responsibility.
- Feature folders mirror gameplay domains.
- Static data lives in ScriptableObjects; runtime state lives in scene services/components.
- Events communicate state changes; UI observes, never owns, domain state.
- Explicit dependencies over service locators and hidden singletons.
- Preserve working behavior; refactor only with a demonstrated need and tests.
- Every scene and prefab has one owner at a time.

## 2. Repository layout

```text
LaundryLegends/                     Unity project root
├── Assets/
│   └── LaundryLegends/
│       ├── Art/
│       │   ├── Characters/
│       │   ├── Environment/
│       │   ├── Props/
│       │   ├── Materials/
│       │   └── Textures/
│       ├── Audio/
│       │   ├── Ambience/
│       │   ├── Music/
│       │   ├── SFX/
│       │   └── UI/
│       ├── Prefabs/
│       │   ├── Gameplay/
│       │   ├── Environment/
│       │   └── UI/
│       ├── Scenes/
│       │   ├── Bootstrap.unity
│       │   ├── MainMenu.unity
│       │   └── Laundromat.unity
│       ├── Scripts/
│       │   ├── Core/
│       │   ├── Player/
│       │   ├── Interaction/
│       │   ├── Laundry/
│       │   ├── Customers/
│       │   ├── Economy/
│       │   ├── Arcade/
│       │   ├── UI/
│       │   └── Save/
│       ├── Tests/
│       ├── Settings/
│       ├── ScriptableObjects/
│       └── UI/
├── ArtSource/                      Editable source files, not runtime imports
├── Packages/                      Unity manifest and lockfile (after Phase 1)
├── ProjectSettings/               Unity project settings (after Phase 1)
├── Builds/                        Local builds, ignored
├── TestResults/                   Local test output, ignored
├── tools/                         PowerShell validation/status scripts
├── .opencode/                     Agents, commands, project config
├── .orchestrator/                 Persistent orchestration state
└── *.md                           Durable documentation
```

The Unity project exists. Paths above are the implemented structure plus the planned feature folders for later phases. Do not fabricate files that are not present.

## 3. Assembly definitions

Implemented:

- `LaundryLegends.Runtime.asmdef` - all runtime code.
- `LaundryLegends.Editor.asmdef` - editor-only tooling.
- `LaundryLegends.Tests.EditMode.asmdef` - edit-mode tests; references runtime and test framework.
- `LaundryLegends.Tests.PlayMode.asmdef` - play-mode tests; references runtime and test framework.

Rules:

- Runtime must not reference editor assemblies.
- Tests may reference runtime; runtime never references tests.
- Avoid circular references; split by feature only when a real dependency appears.

## 4. Runtime layers

```text
Input / interaction
       ↓
Player + Interactable components
       ↓
Domain services (GameState, Economy, Orders, Machines, Customers, Arcade)
       ↓
Events / state snapshots
       ↓
UI, audio, animation, VFX
       ↓
Save DTO
```

Domain services may know about data and other services. UI/presentation may read domain state and subscribe to events. Domain services must not depend on specific UI widgets.

## 5. Responsibilities

| System | Owns | Must not own |
|---|---|---|
| `GameState` | Money, day/time, progression flags, current profile | Scene objects, UI layout |
| `EconomyService` | Transactions, price lookup, daily summary | Machine timers, customer movement |
| `OrderService` / `OrderStateMachine` | Order lifecycle, contents, payout, state transitions | Player carrying, scene animation |
| `LaundryItem` / `LaundryLoad` | Item kind, template ID, runtime instance ID, carry/load eligibility, held/dropped representation | Order payment, machine timers |
| `MachineLoadSlot` | One-load boundary, accepted item kind, exact item ownership while loaded | Cycle timers, economy, order state |
| `MachineController` | Machine state, cycle timer, capacity, load reference | Money mutation, UI rendering |
| `CarryController` | Held item/basket attachment, drop/place rules, held-state cleanup | Order completion, machine cycles |
| `CarrySlot` | One compatible item placement boundary | Inventory aggregation, orders |
| `InteractionPromptView` | Display of the current prompt and key label | Gameplay state mutation |
| `CustomerAgent` | Arrival, queue, handoff, patience, collection, payment request | Economy balance internals |
| `PlayerInteractor` (`InteractionController` role) | Raycast/target selection, eligibility, prompt text, interaction dispatch | Domain state mutation beyond calling interfaces |
| `SecretRoomController` | Discovery flags, reveal sequence, room state | Arcade income calculation |
| `ArcadeService` | Machine ownership, appeal, plays, arcade income | Player movement |
| `SaveService` | Snapshot/restore, versioning, corruption handling | Gameplay rules |
| `HUDController` | Displaying cash, order, prompt, machine state | Mutating cash/orders |
| `AudioDirector` | Ambience/music state and one-shots | Gameplay decisions |

## 6. Core state machines

### Order

```text
Created
  → Accepted
  → LaundryCollected
  → Washing
  → WashComplete
  → Drying
  → DryComplete
  → Folding
  → Folded
  → ReadyForPickup
  → Returned
  → Paid
  → Closed
```

Allowed exceptional transitions:

- `Cancelled` from `Created`/`Accepted` before handoff.
- `Abandoned` only after a documented customer-leave rule.
- Repair/invalid-state recovery must be explicit, logged, and testable.

### Machine

```text
Empty → Loaded → Running → Complete → Unloaded → Empty
```

Later states: `Broken`, `Reserved`, `OutOfOrder`.

Invariants:

- Only one load per machine.
- A running machine cannot accept items.
- A completed machine must be unloaded before reloading.
- Timer progress is derived from timestamps, not frame accumulation.

### Customer

```text
Arriving → Queueing → Handoff → Waiting → Collecting → Paying → Leaving
```

Customer patience is presentation/economy input, never a permanent soft-lock.

## 7. Data ownership and flow

- `GameState` is the single source of truth for money and progression.
- `EconomyService` is the only writer of money, exposing `TrySpend`, `AddIncome`, and transaction events.
- Orders reference laundry item IDs and machine IDs, not direct scene object ownership, where practical.
- Machines own their load reference while loaded.
- The folding station requests order-state advancement; it does not edit money.
- Customers request payment through `EconomyService`; they do not write `GameState` fields.
- UI receives read-only snapshots/events and emits intents (button presses) to the relevant service.
- Save data is built from services, not discovered by scanning the scene at save time.

### Runtime state lifetime

- The `Bootstrap` scene contains one `GameSession` GameObject with `GameSession` and `BootstrapController`.
- `GameSession.Awake` creates exactly one `GameState` and calls `DontDestroyOnLoad`; duplicate sessions destroy themselves.
- `GameState` lives until the application quits. Scene transitions replace presentation and gameplay objects, not the session.
- `GameSession.Instance` is a service handle only. Money, day/time, and progression remain instance fields inside `GameState`, not static globals.
- `BootstrapController` verifies that `GameState` exists and then routes to `MainMenu`; later flow uses `SceneRouter` rather than scattered scene-name strings.

## 8. Static configuration

Planned ScriptableObject types:

- `LaundryOrderDefinition` — service type, base payout, item count, duration modifiers.
- `MachineDefinition` — washer/dryer type, capacity, cycle time, operating cost, unlock tier.
- `CustomerDefinition` — archetype, patience, tip behavior, visuals, audio.
- `UpgradeDefinition` — cost, prerequisites, effect, display text.
- `ArcadeMachineDefinition` — genre, appeal, power, purchase cost, play income, repair state.
- `ProgressionDefinition` — discovery thresholds and milestone flags.
- `AudioCueDefinition` — clip references and mixing metadata, if useful.

Rules:

- Do not store runtime state in ScriptableObjects.
- Do not create a definition asset until a system uses it.
- Keep identity fields stable; renaming an asset ID can break saves.

## 9. Scenes and prefabs

### Scenes

- `Bootstrap` — persistent services, save load, scene routing; loaded first in builds.
- `MainMenu` — title, continue/new game, settings, quit.
- `Laundromat` — the main playable building, including the hidden room shell initially.
- `SecretRoom` — optional additive scene later if it improves lighting/loading isolation; do not split until justified.

Scene rules:

- One owner per scene.
- Avoid changing build settings unless the task explicitly covers scene registration.
- Prefer prefab instances over duplicated scene objects.
- A scene must be recoverable from Git; no manual unreproducible setup.

### Prefabs

- Gameplay prefabs under `Prefabs/Gameplay`.
- Environment pieces under `Prefabs/Environment`.
- UI prefabs under `Prefabs/UI`.
- Prefab variants for material/color changes, not duplicated hierarchies.
- Do not edit a shared prefab from two parallel tasks.

## 10. Input

- Legacy Input Manager initially: WASD movement, mouse look, `E` interact, `Q` drop/back, `Tab` order ticket, `Esc` open a minimal pause overlay and release the cursor; `Resume` closes it and locks the cursor again.
- Input mappings live in one place, not scattered string literals. For Phase 3 interaction, `PlayerInteractor.InteractKey` is the source of the key and `InteractKeyLabel` is the display label; the prompt view must not hard-code `E`.
- Rebindings and gamepad support are later, and require approval if they add a package.
- Do not mix legacy input and the new Input System in one feature.

## 11. Save architecture

- Save location: `Application.persistentDataPath`.
- Format: versioned JSON DTO initially; binary only if profiling proves a need.
- DTO contains: save version, profile ID, money, day/time, progression flags, order snapshots, machine snapshots, customer/queue snapshot if safe, arcade snapshot, settings.
- Do not serialize `GameObject`, `Transform`, or component references. Use stable IDs and resolve on load.
- Validate on load; a missing/invalid version falls back safely and preserves the bad file.
- Autosave at well-defined safe points only.
- Tests must cover round-trip, missing fields, unknown future version, and corrupt JSON.

## 12. UI

- UGUI + TextMeshPro initially (built-in packages; no new dependency).
- Canvas prefabs separated by concern: HUD, interaction prompt, pause menu, order ticket, settings.
- Phase 3 uses an IMGUI `InteractionPromptView` placeholder on the Player prefab, with testable `IsVisible`/`CurrentLabel` state and the built-in font. It must not import TextMeshPro Essentials or create final UI prefabs; the UI phase replaces it.
- UI never finds domain objects with `FindObjectOfType` in production paths; inject or register references.
- Prompts must be localizable later; avoid hard-coded long strings in scene components.
- Support keyboard/mouse first; focus navigation later.

## 13. Asset integration

1. Source art lives in `ArtSource/` until imported.
2. Runtime asset goes into the relevant `Assets/LaundryLegends/...` folder.
3. Add/verify `.meta` files through Unity; never hand-edit GUIDs.
4. Set scale, pivot, colliders, import settings, and compression deliberately.
5. Register the asset in `ASSET_INVENTORY.md` with license and verification status.
6. Integrate into a scene/prefab; verify in play mode.
7. Mark placeholder vs final candidate honestly.

No dedicated 3D/image/audio generation tool is currently verified. Use Unity primitives, procedural meshes/materials, code-generated tones, or documented briefs until a tool is approved.

## 14. Naming conventions

- C# types: `PascalCase`; private fields: `_camelCase`; methods/properties: `PascalCase`.
- Interfaces: `I` prefix (`IInteractable`).
- ScriptableObject assets: `SO_<Type>_<Name>`.
- Prefabs: `PF_<Category>_<Name>`.
- Materials: `MAT_<Surface>_<Variant>`.
- Scenes: `SCN_<Name>` or the documented scene names above.
- Textures: `T_<Subject>_<Type>`; audio: `SFX_`, `AMB_`, `MUS_`, `VO_`.
- IDs for saves are stable strings, not Unity instance IDs.

## 15. Testing conventions

- EditMode tests for pure state machines, economy math, save serialization, and definitions.
- PlayMode tests for interaction, order flow, machine transitions, and scene smoke checks.
- Add a test at the same time as a new state machine or save field.
- Use deterministic time providers for timers; do not wait real minutes in tests.
- Playmode tests may use a small test scene or prefab, not the full art scene.
- Manual verification is required for feel, readability, lighting, audio, and fun.
- See `TESTING_GUIDE.md`.

## 16. Package and dependency policy

- Baseline: Unity built-in modules, UGUI, TextMeshPro, Unity Test Framework. No third-party runtime packages.
- Any package addition/removal/upgrade requires human approval.
- Record the reason, version, license, and rollback plan in a decision record.
- Do not add a DI framework, ECS/DOTS, behavior tree library, tweening library, or networking package without a demonstrated blocker.
- Render pipeline changes are architecture-level decisions and require approval.

## 17. Build configuration (planned)

- Target: Windows x86_64 standalone.
- Start scene: `Bootstrap`.
- Scripting backend: Mono for iteration; evaluate IL2CPP for release.
- Color space: Linear.
- Resolution: 1920×1080 default, windowed, resizable; fullscreen option.
- Quality: start with one modest preset for reliable performance.
- Builds output to `Builds/`, never committed.
- A reproducible build check is required in Phase 16.

## 18. Decision records

Architecture decisions live in `.orchestrator/decisions/`:

- `0001-unity-version-and-render-pipeline.md`
- `0002-orchestration-and-agent-model.md`
- `0003-documentation-and-state-sources.md`
- Add a new numbered record for any decision that changes a confirmed choice, package policy, scene structure, save format, or pipeline.

## 19. Technical debt and risks

- Unity batch-mode licensing can expire; refresh it in Unity Hub before treating exit `198` as a project defect.
- The project has compile and test harnesses; the suite is 10 EditMode and 12 PlayMode tests and does not yet cover carry, machine cycles, orders, economy, customers, save/load, or UI.
- Scene/prefab merge conflicts are the highest collaboration risk; enforce one owner and small commits.
- Carry is attachment-based in Phase 3; clipping, drop-point validity, and held-item cleanup need automated and human review.
- Item identity exists before save/load; instance IDs must be serialized in the save phase or they will not survive a reload.
- Save compatibility must be designed before content grows.
- Placeholder art can hide readability problems; playtest early.
- The IMGUI prompt is temporary and is not final UI quality.
- Built-In RP is appropriate for scope but may limit advanced lighting; do not switch without evidence and approval.
- Performance targets are unknown until the greybox runs on the target machine.
