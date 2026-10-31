# Laundry Legends — Development Roadmap

**Last updated:** 2026-10-30
**Current phase:** 3 - in progress
**Next phase:** 4 - planned
**Authority:** This roadmap defines sequence, dependencies, deliverables, and acceptance. Update it when a genuine dependency requires a change; record the reason in a decision record.

## 0. Status legend

| Status | Meaning |
|---|---|
| Complete | All acceptance criteria have evidence |
| In progress | Active phase, at least one task underway |
| Ready | Dependencies met; can start |
| Planned | Defined but blocked by a dependency |
| Blocked | Cannot proceed without a decision, asset, tool, or fix |
| Paused | Intentionally stopped within budget |

## 1. Milestones

| Milestone | Phases | Definition |
|---|---|---|
| M0 — Bootstrap | 0 | Persistent development system ready |
| M1 — Playable laundry | 1–5 | One complete, repeatable order loop |
| M2 — Business and secret | 6–7 | Economy, upgrades, discovered hidden room |
| M3 — Arcade | 8–9 | First machine, income, expansion |
| M4 — Presentation | 10–12 | Characters, art, animation, audio |
| M5 — Reliability | 13–14 | UI, save, balance, usability |
| M6 — Release | 15–16 | QA, optimization, reproducible build |

## 2. Global validation policy

Every phase must run the applicable subset of:

- Structural checks: `tools/validate-bootstrap.ps1`, JSON validity, required documentation/state updates.
- Unity compile check in batch mode once the project exists.
- EditMode and PlayMode tests once test assemblies exist.
- Scene/prefab validation for missing references, duplicate IDs, and broken build settings.
- Log inspection for exceptions, missing scripts, and import errors.
- Manual playtest for feel, readability, visual quality, audio, and fun.
- Release build validation in Phase 16.

A phase is not complete because code compiles. Human-sensitive criteria must be explicitly marked verified or pending.

---

## Phase 0 — Project bootstrap

**Status:** Complete (machine-verified; human review recommended)
**Objective:** Establish a durable, safe, phase-based development system before game implementation.
**Depends on:** None.

### Deliverables

- Empty-workspace and environment verification.
- Git repository, `main` branch, Unity-aware `.gitignore`, initial checkpoint/tag.
- Required documentation and full roadmap.
- Project-local OpenCode model, permissions, agents, and slash commands.
- Orchestration state, tasks, budgets, reports, decisions, recovery procedure.
- Bootstrap validation tooling.

### Tasks

| ID | Task | Status |
|---|---|---|
| LL-P0-001 | Verify workspace is empty and identify existing user data | Complete |
| LL-P0-002 | Verify OpenCode version, model, agents, tools, and permissions | Complete |
| LL-P0-003 | Verify Git, Unity, .NET, Python, and asset-tool availability | Complete |
| LL-P0-004 | Initialize Git, select branch, create `.gitignore` | Complete |
| LL-P0-005 | Create required documentation, design/architecture baseline, and roadmap | Complete |
| LL-P0-006 | Create orchestration state, tasks, budgets, decisions, and recovery | Complete |
| LL-P0-007 | Configure OpenCode agents, commands, model, and guardrails | Complete |
| LL-P0-008 | Validate bootstrap structure and create initial checkpoint | Complete |
| LL-P0-009 | Human review of bootstrap and Phase 1 start decision | Pending (non-blocking) |

### Acceptance criteria

- All required Markdown files exist and are internally consistent.
- `.orchestrator/state.json`, task files, and budgets parse as valid JSON; the run report exists and is consistent with them.
- Git repository exists on `main`; ignored generated paths are not tracked.
- No secrets, builds, caches, or user data are committed.
- Current phase/task/next action are unambiguous.
- Tool/model claims match verified evidence; missing tools are documented.
- No game feature is falsely reported as implemented.

### Expected files

Root documentation, `.opencode/`, `.orchestrator/`, `tools/`, `ArtSource/`, `Builds/`, `TestResults/`.

### Validation

- Automated: `tools/validate-bootstrap.ps1` passes.
- Manual: review docs and command behavior; no gameplay verification applicable.

### Risks

- Model/provider availability may change.
- Unity licensing is not yet batch-tested.
- Documentation can drift if state updates are skipped.

---

## Phase 1 — Unity foundation

**Status:** Complete (human verification recorded 2026-10-20)
**Objective:** Create a validated Unity 6.0 LTS project with Built-In RP, project structure, scene flow, player movement/camera, greybox environment, and baseline tests.
**Depends on:** Phase 0.

### Deliverables

- Unity project at repository root using `6000.0.64f1` and Built-In RP.
- Baseline package manifest with no unnecessary third-party packages.
- `Assets/LaundryLegends` folder structure and assembly definitions.
- `Bootstrap`, `MainMenu`, and `Laundromat` scenes with build registration.
- `GameState` foundation and scene routing.
- First-person player controller and camera using legacy Input Manager.
- Greybox laundromat shell, lighting, and a placeholder interaction target.
- EditMode/PlayMode test scaffolding and a compile smoke check.
- Unity batch-mode command documentation and verified logs.

### Tasks

| ID | Task | Status |
|---|---|---|
| LL-P1-001 | Create Unity project at repo root, preserve bootstrap files, verify license/log | Complete |
| LL-P1-002 | Record and approve baseline package manifest and project settings | Complete |
| LL-P1-003 | Create folder structure, runtime/editor/test assembly definitions | Complete |
| LL-P1-004 | Create Bootstrap/MainMenu/Laundromat scenes and build settings | Complete |
| LL-P1-005 | Implement minimal GameState and scene routing | Complete |
| LL-P1-006 | Implement first-person movement, mouse look, and interaction raycast stub | Complete |
| LL-P1-007 | Build greybox room, lighting, and placeholder props | Complete |
| LL-P1-008 | Add EditMode/PlayMode smoke tests and batch-mode validation commands | Complete |
| LL-P1-009 | Verify compile, scene load, movement/camera, and record evidence | Complete |

### Acceptance criteria

- Unity opens the project without compile errors.
- Batch-mode compile/test command runs and its log is recorded, or the blocked reason is documented.
- Build settings contain the three scenes, with `Bootstrap` first.
- Player can move and look in `Laundromat`; `Esc` releases the cursor.
- Greybox room has readable scale, floor, walls, entrance, and lighting.
- No third-party package or render-pipeline change was made without approval.
- Tests pass or are accurately reported as absent/failing.
- Human verifies movement feel, camera, and greybox readability.

### Expected files/assets

`Assets/LaundryLegends/Scripts/Core`, `Player`, `Interaction`, `Scenes`, `Prefabs`, `Settings`, `Tests`; `Packages/manifest.json`; `ProjectSettings/**`; greybox materials and prefabs.

### Automated validation

- `tools/validate-bootstrap.ps1`
- Unity batch-mode compile
- EditMode/PlayMode tests
- Log scan for compile errors/exceptions
- Build-settings scene list check

### Human verification

- Open the project, enter play mode, move/look, confirm scale and lighting.
- Approve the baseline package manifest and project settings.

### Risks

- Unity activation may fail headlessly.
- Built-In RP template/package defaults may need adjustment.
- Scene/prefab serialization can be brittle; keep changes scoped.
- First-person choice remains provisional.

---

## Phase 2 — Physical laundromat

**Status:** Complete
**Objective:** Build the physical laundry space: room, entrance, windows, counter, waiting area, furniture, storage, and concealed back-room access.
**Depends on:** Phase 1.

### Deliverables

- Playable laundromat layout with clear circulation and customer space.
- Entrance door, windows, counter, waiting chairs, shelves, signage, utility area.
- Collision, navigation surfaces, and lighting zones.
- Secret door/shelf access blocked but visually foreshadowed.
- Greybox or placeholder furniture with consistent scale.
- Scene validation for missing colliders/references.

### Tasks

| ID | Task | Status |
|---|---|---|
| LL-P2-001 | Audit the greybox layout and define circulation/clearance constraints | Complete |
| LL-P2-002 | Add entrance door, storefront windows, and threshold | Complete |
| LL-P2-003 | Add waiting area, storage shelving, utility alcove, and signage | Complete |
| LL-P2-004 | Conceal the secret door behind a shelf/service panel | Complete |
| LL-P2-005 | Add collision, navigation, and camera-clearance validation | Complete |
| LL-P2-006 | Verify Phase 2 acceptance and record evidence | Complete |

### Acceptance criteria

- Player can traverse the public laundromat without leaving geometry or getting stuck.
- Customer path from entrance to counter is clear.
- Secret access is visible enough to foreshadow but not obviously open.
- Lighting reads the space and supports interaction visibility.
- No missing references or colliders on critical paths.
- Human verifies layout, readability, and secret-room subtlety.

### Expected files/assets

Environment prefabs, materials, scene objects, lighting settings, collision/nav data, placeholder props.

### Automated validation

- Compile, PlayMode traversal smoke test, scene reference check, log scan.

### Human verification

- Walk the room, test sightlines, confirm the secret door is discoverable but not spoiled.
- Result: project owner reported pass for all six review items on 2026-10-20. Recorded as owner-reported in `.orchestrator/reports/2026-10-20-ll-p2-006-human-verification.md`.

### Risks

- Layout churn can invalidate later machine/interaction placement.
- Poor sightlines can hide the secret too well.
- Placeholder art may mislead scale decisions.

---

## Phase 3 - Laundry interaction foundation

**Status:** In progress
**Objective:** Implement player interaction, carrying, baskets, laundry items, prompts, and machine interaction hooks.
**Depends on:** Phase 2.

### Deliverables

- `IInteractable` contract and interaction controller.
- Raycast target selection, prompt UI hook, and input mapping.
- Carry/place/drop handling for baskets and laundry items.
- Basket and laundry item prefabs with stable IDs.
- At least one washer/dryer interaction target.
- Interaction tests and no-stuck-state safeguards.

### Tasks

| ID | Task | Status |
|---|---|---|
| LL-P3-001 | Reconcile Phase 3 context and lock interaction contracts | Complete |
| LL-P3-002 | Harden interaction contract and target resolution | Complete |
| LL-P3-003 | Implement carry state, item identity, and drop/place rules | Complete |
| LL-P3-004 | Add basket, loose-laundry, and bag placeholder prefabs | Complete |
| LL-P3-005 | Add interaction prompt UI and controls hint | Complete |
| LL-P3-006 | Add one machine load/unload interaction hook | Complete |
| LL-P3-007 | Verify Phase 3 acceptance and record evidence | Complete |
| LL-P3-008 | Add pickup interaction for laundry items (repair for acceptance gap) | Complete |
| LL-P3-009 | Add drop input and drop hint (repair for owner review) | Complete |
| LL-P3-010 | Research the free hybrid Blender and CC0 asset pipeline | Complete |
| LL-P3-011 | Install Blender and build the first polished laundry assets | Complete |
| LL-P3-012 | Polish loose laundry, bag, and machine visuals | Ready |
| LL-P3-013 | Create three free-licensed NPC archetypes | Planned |

### Acceptance criteria

- Player can pick up, carry, and place a basket and loose laundry.
- Prompts correctly describe the object and action.
- Items cannot be duplicated or lost through normal interactions.
- Machine interaction accepts a valid load and rejects invalid loads.
- Human verifies carrying feel and prompt clarity.

### Expected files/assets

Interaction scripts, carry scripts, item/basket prefabs, prompt UI placeholder, tests, placeholder materials.

### Automated validation

- Compile, EditMode/PlayMode interaction tests, duplicate-item regression test.

### Human verification

- Carry/place/drop feel, prompt readability, no visual clipping.

### Risks

- Physics-based carrying can destabilize; prefer attachment-based carrying initially.
- Item identity is foundational for saves and orders.

---

## Phase 4 — Laundry processing

**Status:** Planned
**Objective:** Implement washers, dryers, cycle states, folding, completed orders, and a coherent order state machine.
**Depends on:** Phase 3.

### Deliverables

- Machine state machine with deterministic timers.
- Washer and dryer cycle behavior, progress feedback, and sounds/placeholders.
- Transfer flow from washer to dryer.
- Folding station interaction.
- Order state machine covering collection through folded/ready.
- Tests for transitions, invalid actions, and timer completion.

### Tasks

- Implement machine states and time provider.
- Implement wash and dry cycles with cost hooks.
- Implement transfer and unload rules.
- Implement folding station and folded order state.
- Add progress UI/world feedback.
- Add tests for all transitions and recovery.

### Acceptance criteria

- An order can be collected, washed, dried, folded, and marked ready.
- Invalid transitions are rejected without corrupting state.
- Timers survive pause/load according to the defined rule.
- Progress is visible without opening a debug console.
- Human verifies cycle timing and folding interaction feel.

### Expected files/assets

Machine/order scripts, ScriptableObject definitions, placeholder audio/VFX, folding station prefab, tests.

### Automated validation

- Compile, EditMode state-machine tests, PlayMode cycle smoke test, save-relevant state snapshot test.

### Human verification

- Washer/dryer/folding pacing and clarity.

### Risks

- Real-time waits may feel tedious; playtest early.
- Timer/time-scale edge cases.
- Folding design may need iteration.

---

## Phase 5 — Customers and payments

**Status:** Planned
**Objective:** Complete the first playable laundry loop with customer arrival, order handoff, waiting, collection, payment, satisfaction, and repeatable flow.
**Depends on:** Phase 4.

### Deliverables

- Customer lifecycle and spawn/queue behavior.
- Order creation and handoff.
- Waiting, patience, collection, and payment.
- Money HUD update and receipt feedback.
- At least one customer archetype.
- End-to-end playable order loop.
- Tests for payment and loop completion.

### Tasks

- Implement customer state machine and simple navigation.
- Implement order creation and ticket display.
- Implement handoff/collection and payment through economy.
- Add patience/tip placeholder behavior.
- Add loop completion and repeat spawning.
- Run a full manual order and record evidence.

### Acceptance criteria

- A customer can arrive, hand over an order, wait, collect it, pay, and leave.
- The player can repeat the loop without console commands or scene reload.
- Money changes exactly once per completed order.
- No soft-lock when a customer waits too long; fallback behavior is documented.
- Human verifies the loop is understandable and satisfying enough for a vertical slice.

### Expected files/assets

Customer scripts/prefabs, order ticket UI, payment effects/audio placeholders, tests, scene updates.

### Automated validation

- Compile, PlayMode end-to-end loop test, payment duplication test, log scan.

### Human verification

- Full manual playtest; assess clarity, pacing, and fun.

### Risks

- Navigation and queue behavior can become complex; keep it simple.
- Economy/payment duplication is a critical defect class.
- This phase is the first real fun test; be ready to revise assumptions.

---

## Phase 6 — Business simulation

**Status:** Planned
**Objective:** Add money, prices, operating costs, upgrades, order management, progression, and a simple business summary.
**Depends on:** Phase 5.

### Deliverables

- `EconomyService` with transaction events and daily summary.
- Prices/costs and simple operating expenses.
- Upgrade purchase flow and at least two meaningful upgrades.
- Order board/management if multiple orders are supported.
- Progression flags and next-goal display.
- Economy tests for affordability, duplicate purchase, and rollback.

### Tasks

- Implement economy service and transaction log.
- Add costs and daily summary.
- Implement upgrade definitions/purchase UI.
- Add progression flags and a simple summary screen.
- Add tests for money invariants.

### Acceptance criteria

- All money changes flow through one service and are auditable.
- Upgrades cost money, apply once, and visibly change capability.
- Player cannot buy an unaffordable upgrade.
- Daily summary accurately reflects the session.
- Human verifies upgrade value and economy readability.

### Expected files/assets

Economy/upgrade scripts, ScriptableObjects, summary UI, tests, placeholder signs.

### Automated validation

- Compile, EditMode economy tests, PlayMode purchase test.

### Human verification

- Economy clarity, upgrade satisfaction, and pricing sanity.

### Risks

- Premature economy complexity can slow the loop.
- Numbers are untuned placeholders.
- Save integration is not yet complete.

---

## Phase 7 — Secret room discovery

**Status:** Planned
**Objective:** Implement discovery, access conditions, reveal sequence, room preparation, and the first usable hidden space.
**Depends on:** Phase 6.

### Deliverables

- Discovery clues and eligibility threshold.
- Locked/sealed access and reveal interaction.
- Room cleaning, power, repair, and usable-space progression.
- Distinct secret-room lighting/atmosphere.
- Progression state and journal/checklist if appropriate.
- Tests for threshold gating and irreversible/irreversible-safe transitions.

### Tasks

- Add clues and eligibility tracking.
- Implement reveal sequence and access.
- Add preparation tasks and visible room transformation.
- Add first usable room state and save flags.
- Add tests for gating and state restoration.

### Acceptance criteria

- Secret room cannot be accessed before the defined conditions.
- Reveal is understandable, earned, and visually distinct.
- The room can be prepared into a usable space through player actions.
- State restores correctly after save/load once saving exists.
- Human verifies surprise, readability, and payoff.

### Expected files/assets

Secret-room scripts, room prefabs/materials, lighting, clue props, progression flags, tests.

### Automated validation

- Compile, gating tests, PlayMode reveal smoke test, scene reference check.

### Human verification

- Play through discovery; assess clue subtlety and reveal impact.

### Risks

- Over-telegraphing spoils the surprise; under-telegraphing feels arbitrary.
- Room transformation scope can expand indefinitely; cap the first version.

---

## Phase 8 — First arcade machine

**Status:** Planned
**Objective:** Implement purchasing, placement/installation, activation, customer attraction, machine usage, and arcade income.
**Depends on:** Phase 7.

### Deliverables

- First arcade machine definition and prefab.
- Purchase/install interaction with cost and placement validation.
- Power/activation state and attract mode.
- Arcade customer attraction/usage simulation.
- Arcade income collection or summary.
- End-to-end laundry → secret room → first machine → income test.

### Tasks

- Create machine definition/prefab and activation states.
- Implement purchase/install flow and placement.
- Implement arcade traffic and play cycle.
- Implement income transaction and feedback.
- Add tests for purchase/income duplication.
- Run the first cross-business loop manually.

### Acceptance criteria

- Player can buy and activate the first machine after preparing the room.
- The machine visibly/audibly comes alive.
- Arcade visitors generate income through a documented, non-duplicating flow.
- Arcade state persists once saving exists.
- Human verifies the first-machine payoff.

### Expected files/assets

Arcade machine prefab/definition, arcade scripts, lights/audio placeholders, income UI, tests.

### Automated validation

- Compile, economy/arcade tests, PlayMode activation smoke test.

### Human verification

- Payoff of first machine; clarity of arcade income.

### Risks

- Arcade simulation may feel disconnected if not spatial.
- Income duplication and time-scaling bugs.
- Asset scope can explode.

---

## Phase 9 — Arcade expansion

**Status:** Planned
**Objective:** Add machines, upgrades, room expansion, decoration, improved income, progression, and meaningful laundry/arcade links.
**Depends on:** Phase 8.

### Deliverables

- Multiple machine types and acquisition paths.
- Arcade upgrades/decorations and room expansion.
- Laundry reputation/footfall effects on arcade traffic.
- Arcade income scaling and operating costs.
- Expanded room layout and capacity constraints.
- Progression milestones and endgame direction.

### Tasks

- Add second/third machine types.
- Add arcade upgrade and decoration purchases.
- Connect laundry reputation to arcade traffic.
- Add room expansion/placement constraints.
- Balance placeholder income and costs.
- Test combined economy and progression.

### Acceptance criteria

- Player can grow the arcade beyond one machine.
- Laundry actions measurably affect arcade traffic/income.
- Expansion is spatial and visible, not only a menu number.
- Combined economy remains understandable and recoverable.
- Human verifies the dual-business loop is coherent and fun.

### Expected files/assets

Arcade definitions/prefabs, decoration props, expansion geometry, balance data, tests.

### Automated validation

- Compile, economy/progression tests, PlayMode multi-machine smoke test.

### Human verification

- Long-session playtest of both businesses.

### Risks

- Economy runaway or grind.
- Space/layout churn.
- Feature creep into a full arcade game.

---

## Phase 10 — Characters and animation

**Status:** Planned
**Objective:** Develop necessary player, customer, and other character assets, animations, movement, interactions, and visual feedback.
**Depends on:** Phases 5 and 9 (stable loops and interactions).

### Deliverables

- Player representation/hands appropriate to the chosen perspective.
- Customer character placeholders or final candidates.
- Idle, walk, carry, load, fold, handoff, and reaction animations.
- Animation state wiring and root motion policy.
- Integration into customer/machine flows.
- Asset inventory entries with licensing/status.

### Tasks

- Decide first-person body/hands scope.
- Source or create placeholder characters.
- Build animation state machines.
- Wire interactions and feedback.
- Validate animation transitions and clipping.

### Acceptance criteria

- Player interactions have readable character feedback.
- Customers move and react without obvious popping or broken states.
- Animations do not break interaction timing.
- All assets are inventoried with source/license/status.
- Human verifies animation quality and readability.

### Expected files/assets

Character models/rigs/animations, animator controllers, prefabs, materials, inventory rows.

### Automated validation

- Compile, PlayMode animation smoke test, missing-reference scan.

### Human verification

- Visual animation review; feel and clipping check.

### Risks

- No verified 3D/character tool; external assets may require licensing and approval.
- Rig/animation integration can be expensive.
- First-person hands can conflict with carry poses.

---

## Phase 11 — Visual art and environment

**Status:** Planned
**Objective:** Improve models, materials, textures, lighting, signage, room identity, cozy laundry atmosphere, and colorful arcade.
**Depends on:** Phases 2, 5, 7, 8, 9, 10.

### Deliverables

- Cohesive stylized assets for core props, machines, furniture, signage, arcade cabinets.
- Material/texture library with consistent scale and palette.
- Improved lighting and post-processing within Built-In RP limits.
- Environmental storytelling: notices, wear, decor, secret-room transformation.
- Before/after captures and asset inventory updates.

### Tasks

- Define final art direction and palette.
- Replace priority placeholders.
- Build material library and lighting passes.
- Add signage, clutter, and identity props.
- Optimize draw calls/materials.
- Capture visual evidence.

### Acceptance criteria

- Laundromat and arcade are visually distinct and readable.
- Interaction targets remain clear under final lighting.
- No missing materials, pink shaders, or broken scale.
- Performance remains within the target on the development machine.
- Human verifies visual cohesion and atmosphere.

### Expected files/assets

Models, textures, materials, lighting settings, post-processing profiles, signage, captures.

### Automated validation

- Compile, missing-material/reference checks, basic performance measurement, build smoke test.

### Human verification

- Art direction review and playtest in final lighting.

### Risks

- Art production capacity is the largest unknown.
- Built-In RP lighting limits.
- Visual polish can consume unbounded time.

---

## Phase 12 — Audio and presentation

**Status:** Planned
**Objective:** Implement ambience, machine sounds, interaction effects, customer audio where appropriate, music, UI feedback, and audio settings.
**Depends on:** Phases 5, 8, 9, 11.

### Deliverables

- Audio mixer and settings.
- Laundry ambience, machine loops, interaction SFX, payment/UI sounds.
- Arcade ambience/attract modes and music layer.
- Customer greetings/reactions (voice or text), subtitles if voiced.
- Licensed/provenance records for all audio.
- Audio tests/smoke checks.

### Tasks

- Define audio map and mixer groups.
- Source/create placeholder and final candidates.
- Implement looping/one-shot playback and spatialization.
- Add music transitions and settings.
- Verify mix and subtitle behavior.

### Acceptance criteria

- Essential actions have audible feedback.
- Loops do not click or stack incorrectly.
- Music/ambience transitions between laundry and arcade.
- Volume settings work and persist.
- All audio is inventoried with license/provenance.
- Human verifies mix and atmosphere.

### Expected files/assets

Audio clips, mixer assets, audio scripts, subtitles, inventory rows, settings UI.

### Automated validation

- Compile, missing-clip scan, PlayMode audio smoke test, settings round-trip.

### Human verification

- Listening test with headphones/speakers; mix balance.

### Risks

- No verified audio-generation tool; licensing required.
- Looping/3D audio complexity.
- Voice scope can expand.

---

## Phase 13 — UI, saving, and progression reliability

**Status:** Planned
**Objective:** Implement menus, HUD, prompts, progression display, saving/loading, settings, and recovery from invalid data.
**Depends on:** Phases 6, 9, 12.

### Deliverables

- Main menu, pause, settings, HUD, order ticket, progression display.
- Versioned save/load with autosave and safe points.
- Corrupt/missing-data recovery.
- Settings persistence and accessibility options.
- UI tests and save round-trip tests.
- Reload/resume behavior verified.

### Tasks

- Finalize HUD/menu structure.
- Implement save DTO/versioning and migrations.
- Implement autosave/manual save and recovery.
- Add settings/accessibility.
- Test save corruption, missing fields, and scene reload.
- Verify full progression display.

### Acceptance criteria

- New game, save, quit, continue, and resume work without losing progression.
- Corrupt save produces a safe fallback and preserves diagnostic evidence.
- UI reflects domain state without desync.
- Settings persist and apply.
- Human verifies menu flow and save reliability.

### Expected files/assets

UI prefabs, save scripts/DTOs, settings assets, tests, documentation.

### Automated validation

- Compile, EditMode save tests, PlayMode save/load smoke test, missing-reference scan.

### Human verification

- Manual save/reload and menu flow.

### Risks

- Save compatibility debt.
- UI/domain desync.
- Scene reload edge cases.

---

## Phase 14 — Balancing and usability

**Status:** Planned
**Objective:** Tune difficulty, economy, pacing, tutorials, customer behavior, upgrade costs, and discovery-to-arcade progression.
**Depends on:** Phase 13.

### Deliverables

- Tuned order payouts/costs/cycle times.
- Tutorial/onboarding pass.
- Customer patience/tip tuning.
- Upgrade and arcade progression pacing.
- Usability fixes from playtests.
- Balance data documented and versioned.

### Tasks

- Run structured playtests and record timings.
- Tune economy and pacing.
- Implement/refine tutorial prompts.
- Fix usability blockers.
- Validate recovery from low-cash and missed-order states.

### Acceptance criteria

- A new player can complete the first order with contextual guidance.
- Progression to the secret room and first arcade machine is achievable without excessive grind.
- Failure states are recoverable.
- Balance changes are documented and tested.
- Human verifies pacing and fun.

### Expected files/assets

Balance ScriptableObjects, tutorial UI/triggers, tuning notes, playtest reports.

### Automated validation

- Economy simulation tests, progression smoke test, regression suite.

### Human verification

- Multiple fresh playtests; compare session timings and feedback.

### Risks

- Small sample size.
- Over-tuning to one tester.
- Tutorials can feel intrusive.

---

## Phase 15 — QA, optimization, and polish

**Status:** Planned
**Objective:** Perform regression testing, bug fixing, performance work, visual/audio refinement, accessibility improvements, and gameplay testing.
**Depends on:** Phase 14.

### Deliverables

- Regression checklist and defect log.
- Performance profiling and fixes.
- Visual/audio polish pass.
- Accessibility review.
- Gameplay bug fixes and edge-case hardening.
- Updated documentation and known-issues list.

### Tasks

- Run full regression suite.
- Profile CPU/GPU/memory/load times.
- Fix critical and high-severity defects.
- Polish feedback, lighting, audio, UI.
- Verify accessibility settings.
- Freeze features and prepare release branch/candidate.

### Acceptance criteria

- No known game-breaking defects.
- All critical/high defects fixed or documented with approval.
- Performance meets the agreed target on the development machine.
- Accessibility basics work.
- Full loop and secret progression can be completed.
- Human verifies final play quality.

### Expected files/assets

Test reports, profiling captures, fix commits, polished assets, known-issues list.

### Automated validation

- Full test suite, build smoke test, log scan, performance capture, save compatibility test.

### Human verification

- Extended playtest and final QA sign-off.

### Risks

- Late defects.
- Optimization tradeoffs.
- Scope creep after feature freeze.

---

## Phase 16 — Build and release preparation

**Status:** Planned
**Objective:** Create a reproducible PC build, validate startup/gameplay, verify essential features, prepare menus/settings, capture promotional material, and document release requirements.
**Depends on:** Phase 15.

### Deliverables

- Reproducible Windows x86_64 build.
- Startup, menu, gameplay, save/load, settings, and quit validation.
- Version/build number and release notes.
- Known-issues and credits/licenses.
- Promotional captures/trailer plan.
- Release checklist and human approval package.

### Tasks

- Configure build settings and version.
- Produce clean build from a known commit.
- Run startup and end-to-end smoke tests.
- Capture screenshots/video.
- Prepare release notes, credits, and licensing.
- Present candidate for approval; do not publish.

### Acceptance criteria

- Build launches from a clean directory and reaches gameplay.
- Essential features and settings work in the build.
- Save/load works in the build.
- No secrets or development-only files are shipped.
- Release documentation is complete.
- Human approves publication as a separate action.

### Expected files/assets

Build output in `Builds/` (ignored), release notes, known issues, captures, credits, checklist.

### Automated validation

- Clean build command, build log scan, startup smoke test, save path check.

### Human verification

- Play the candidate build; approve or reject release.

### Risks

- Build-only bugs.
- Missing runtime assets/licenses.
- Platform/store requirements.
- Publishing is explicitly outside autonomous authority.

---

## 3. Change control

- Reordering phases requires a documented dependency reason.
- Splitting a phase is allowed when a real dependency boundary requires it.
- Do not remove the laundry loop or secret arcade progression.
- Any change to a confirmed engine, pipeline, input, save format, or scene architecture requires a decision record and human approval.
- Update `PROJECT_STATE.md`, `.orchestrator/state.json`, and `DEVELOPMENT_LOG.md` whenever phase status changes.
