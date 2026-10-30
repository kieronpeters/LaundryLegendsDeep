# Laundry Legends — Project State

**Last updated:** 2026-10-30 19:12 (UTC+08:00)
**Current milestone:** M1 - Playable laundry, **in progress**
**Current phase:** Phase 3 - Laundry interaction foundation, **in progress**
**Next phase:** Phase 4 - Laundry processing, planned
**Active task:** None - `LL-P3-002` is complete
**Next recommended task:** `LL-P3-003` - implement carry state, item identity, and drop/place rules

## 1. Verified configuration

| Item | Value | Status |
|---|---|---|
| Unity editor | `6000.0.64f1` Unity 6.0 LTS | Project created and batch-verified |
| Render pipeline | Built-In Render Pipeline | Verified: no custom SRP asset, no URP/HDRP package |
| Platform | Windows 64-bit | Windows standalone module installed |
| Input | Legacy Input Manager (`activeInputHandler: 0`) | Verified in generated settings; no Input System package |
| Color space | Linear | Approved and persisted 2026-10-19 |
| Quality / display | Medium preset; windowed 1920×1080, resizable | Approved and persisted 2026-10-19 |
| Company / product | `LaundryLegends` / `Laundry Legends` | Approved and persisted 2026-10-19 |
| Primary model | `opencode/deepseek-v4.1-flash-free` | Verified in model catalog |
| OpenCode | CLI `2.0.28`, server `2.0.32` | Verified |
| Git | `2.55.0.windows.3`, branch `main` | Repository initialized; bootstrap checkpoint recorded |
| Git LFS | `3.7.1` | Available; not configured for asset patterns yet |
| .NET SDK | `10.0.401` | Verified |
| Python | `3.15.0`, no graphics/audio packages | Verified |
| Blender / FFmpeg / ImageMagick / GitHub CLI | Not found on `PATH` | Unavailable; documented |

**Unity licensing:** the Unity Personal offline license validity window expired on 2026-10-19 and caused batch exit code `198`. The human refreshed the license through Unity Hub; the retry resolved entitlements and exited `0`. Unity may require another online refresh when the offline window expires.

## 2. Existing project content

- **Scenes:** `Bootstrap.unity`, `MainMenu.unity`, and `Laundromat.unity`. All are registered in build settings with Bootstrap at index 0. Bootstrap contains `GameSession` + `BootstrapController`; MainMenu contains `MainMenuController`; Laundromat contains a greybox room prefab, the Player prefab instance, and a directional light.
- **Scripts:** runtime `GameState`, `GameSession`, `SceneRouter`, `BootstrapController`, `MainMenuController`, `FirstPersonController`, `PauseController`, `PlayerInteractor`, `IInteractable`, `SimpleInteractable`, and an assembly marker. Editor tools: `SceneSetupTool`, `PlayerSetupTool`, `GreyboxSetupTool`, `Phase2SetupTool`.
- **Assemblies:** `LaundryLegends.Runtime`, `LaundryLegends.Editor`, `LaundryLegends.Tests.EditMode`, `LaundryLegends.Tests.PlayMode`. Runtime, editor, and PlayMode test assemblies compile.
- **Prefabs:** `Prefabs/Gameplay/Player.prefab` with CharacterController, first-person controller, pause controller, interaction raycast, child camera, and AudioListener. `Prefabs/Environment/GreyboxRoom.prefab` with room shell, counter, folding table, eight-machine wall, secret door, ceiling point lights, a south storefront (window openings/frames/glass, entrance frame with open glass leaves and threshold, and a minimal exterior ground/backdrop), a south-east waiting area, west-wall storage shelving, a north-west utility alcove with placeholder signage, and a service-panel/rolling-shelf cover in front of the secret door.
- **Materials:** eleven flat Standard greybox materials under `Art/Materials`, including transparent `Grey_Glass`, emissive `Grey_Sky`, `Grey_Chair`, and `Grey_Sign`; no textures.
- **Tests:** 10 EditMode tests covering `GameState`, build-settings order, and failure reporting; 12 PlayMode tests covering scene routing, player movement/look/pause/interaction, greybox traversal, storefront enter/leave, public-zone presence/clearance, secret-door concealment, public navigation/camera clearance, wall containment, secret-door closure, scale dimensions, and lighting presence. Test assemblies are isolated from runtime.
- **Folders:** full `Assets/LaundryLegends` feature tree for art, audio, prefabs, scenes, scripts, settings, ScriptableObjects, UI, editor, and tests.
- **Textures/models/audio:** none; placeholder primitives only.
- **Unity project files:** `Packages/manifest.json` (34 direct dependencies), `Packages/packages-lock.json` (37 resolved built-in packages), and generated files under `ProjectSettings/`.
- **Bootstrap content:** documentation, `.opencode/` configuration, `.orchestrator/` state, validation tooling, and source/build/test directories.

The workspace was empty before bootstrap. No user work was overwritten.

## 3. Implemented and verified

- Git repository initialized on `main` with Unity-aware `.gitignore`.
- Required project documentation created.
- Phase roadmap covering Phases 0–16.
- Persistent orchestration state, task queue, budgets, recovery procedure, decision records, and run reports.
- Project-local OpenCode model, permissions, agents, and slash commands.
- Bootstrap validation tooling.
- Environment/tool inventory recorded without secrets.
- Unity 6.0 LTS project created at the repository root with `6000.0.64f1`, Built-In Render Pipeline, and a minimal built-in package baseline plus UGUI/TextMeshPro and Unity Test Framework.
- Batch-mode launch and compile smoke checks exited `0`; logs under `TestResults/` (local-only).
- Baseline manifest and settings approved: Linear color space, `LaundryLegends` / `Laundry Legends` naming, Medium quality, windowed 1920×1080 resizable, and removal of the unused `com.unity.multiplayer.center` package.
- `Assets/LaundryLegends` folder structure and four assembly definitions created.
- Foundation scenes created and validated with no missing scripts.
- Minimal instance-based `GameState` and persistent `GameSession` implemented without static mutable gameplay state.
- Scene routing: Bootstrap → MainMenu → Laundromat, verified by PlayMode test.
- First-person player prefab with CharacterController movement, mouse look, Esc pause overlay, cursor lock/release code, and a camera-raycast interaction stub using the legacy Input Manager.
- Greybox laundromat: 16×12 m room, entrance gap, counter, folding table, eight-machine wall, closed foreshadowed secret door, collision, four ceiling point lights, and directional fill.
- Greybox preview captures inspected by the agent: `TestResults/greybox-entrance.png` and `TestResults/greybox-secret.png` (local-only).
- Phase 2 storefront: south wall split into two window openings with frames and transparent glass; entrance jambs/header, open glass door leaves, 0.05 m threshold, and a minimal exterior ground strip and backdrop. Validation passed and the agent inspected `TestResults/phase2-entrance.png` and `TestResults/phase2-storefront.png`.
- Phase 2 public zones: two south-east waiting chairs with a side table and planter; two shallow west-wall shelving units with bins; a north-west utility alcove with a half-height divider, cabinet/sink, mop bucket, and staff-only sign; and storefront, counter, machine-wall, and window signage. Public-zone validation passed; the agent inspected `TestResults/phase2-waiting.png`, `phase2-machine-signage.png`, and `phase2-utility.png`.
- Phase 2 secret access: a shallow service panel and rolling supply shelf cover the east-wall door while leaving the top of the dark door, outer frame edges, casters, and a `SERVICE` marker readable. Concealment validation passed; the agent inspected `TestResults/phase2-secret-approach.png` and `phase2-secret-close.png`.
- Phase 2 navigation: seven public routes, critical-prop colliders, renderer materials, missing references, and 1.5 m/1.7 m camera clearance samples are validated in the editor and PlayMode suite. Validation and tests passed on 2026-10-20.
- Phase 2 acceptance: the project owner reported pass for all six Phase 2 review items on 2026-10-20: traversal, entrance-to-counter path, secret-access subtlety, lighting/readability, signage, and overall layout. Recorded as owner-reported human verification in `.orchestrator/reports/2026-10-20-ll-p2-006-human-verification.md`. Phase 2 is complete.
- Phase 3 plan: seven sequential tasks (`LL-P3-001` through `LL-P3-007`) recorded in `.orchestrator/tasks/phase-03.json` and approved on 2026-10-30 after two independent read-only reviews. `LL-P3-001` reconciled `MASTER_CONTEXT.md` and `ARCHITECTURE.md` and locked interaction, carry, item-identity, prompt, and machine-hook contracts in `.orchestrator/decisions/0005-phase-3-interaction-contracts.md`. No gameplay code changed in `LL-P3-001`.
- Phase 3 interaction contract: `IInteractable` now exposes prompt, `CanInteract`, and `Interact`; `SimpleInteractable` is eligible by default and can be disabled; `PlayerInteractor` exposes the current interactable/target/prompt, the interaction key and label, `TryInteract`, and a `TargetChanged` notification. Out-of-range, blocked, ineligible, disabled, and paused targets are neither reported nor dispatched. Verified on 2026-10-30 with compile exit `0`, EditMode 15/15, PlayMode 18/18, and a clean log scan.
- EditMode suite: 15/15 tests passed on 2026-10-30; the added tests cover interaction eligibility, disabled components, and event-only-when-eligible behavior.
- PlayMode suite: 18/18 tests passed on 2026-10-30; the added tests cover prompt/key exposure, `TryInteract` dispatch, target-change notification, ineligible/blocked/out-of-range/paused rejection, and target clearing.
- Batch commands, evidence paths, failure-probe steps, and test-assembly isolation documented in `TESTING_GUIDE.md`.
- The project owner completed all six Phase 1 Game-view checks on 2026-10-20: movement/speed, camera/comfort, pause/cursor release, room layout, secret door, and lighting/readability. Recorded as owner-reported human verification in `.orchestrator/reports/2026-10-20-ll-p1-009-human-verification.md`.
- Phase 1 is complete. Automated results and screenshots are preserved unchanged; the human confirmation is a separate addendum.
- No production assets, save system, economy, customers, machine behavior, UI, or player build exists yet.

## 4. Incomplete and unverified

- Phase 3 implementation: carry state, item identity, item prefabs, prompt UI, and machine load/unload hooks. Interaction contract and eligibility are implemented; the rest has not been written.
- Interaction, carrying, baskets, prompts, machine load/unload, customers, orders, and the secret-room reveal.
- Production art, audio, UI, animations, and models.
- Save system, economy, customers, laundry machines, orders, secret room, and arcade.
- Player build and performance validation.
- Git LFS configuration for large future assets.

## 5. Blockers and dependencies

- **No current hard blocker.** Phase 3 is in progress; `LL-P3-002` is ready. Implementation must remain sequential because one Unity editor may operate on the project at a time.
- Unity batch-mode licensing was resolved on 2026-10-19 by a Unity Hub refresh; it can expire again.
- Unity Personal offline licensing expires periodically. If batch mode fails with exit `198`, refresh the license in Unity Hub before retrying; do not treat it as a project defect.
- Package additions and project-wide settings changes are treated as approval gates. Phase 1 uses only the installed editor baseline plus UGUI/TextMeshPro and Test Framework.
- No image, 3D, or audio generation tool is verified. Placeholder and procedural assets are the default.

## 6. Approval status

- The human granted a standing, full access approval on 2026-10-19 for execution requests and reiterated it on 2026-10-20. Routine in-repository work continues without prompts. Destructive Git/filesystem actions, package changes, broad project-settings changes, and external publishing still require explicit approval.
- Cleanup pending: `C:\Users\Asus\Desktop\AI` (995-byte Unity log created by an unquoted command). The shell policy hard-denies delete commands; manual removal is required.
- Human review task `LL-P0-009` remains pending and non-blocking.

## 7. Git state

- Branch: `main`
- Bootstrap checkpoint tag: `bootstrap-v0.1` (`da130ddad18b633fb9bad82a3df5107168c91dd0`)
- Baseline commit: `b78218cccfeea741d3d227ffa95e679e9ba92a72` (`chore: bootstrap Laundry Legends development system`)
- Bootstrap close-out commit: `ab511d7d945307d55308f0e880f695ddecece7fc`
- Unity project commit: `e244ba434ace223ff064df52b6a683615d9fb503`
- Baseline settings commit: `f276530257735f9d656c422df68b5b8717ce59bd`
- Folders/assemblies commit: `af52084f0ca41fa29c5be912e5d6759392949aff`
- Scenes/build settings commit: `8bd3fb76ddc6fb1fb2eb99e99caf0ef6bb7f9e72`
- GameState/routing commit: `6e02469a891198072299c9f17980383bf6764fc5`
- Player/movement commit: `e263d1d18cd70e50ffc55e3a2f19b5c7152ae8f7`
- Greybox environment commit: `13ceed9bb7c72d3519f50acbb5d6fe45c7bed6ac`
- Test scaffolding commit: `69baa1b68868e95c127c80465ac78160a2c44e10`
- Phase 1 verification/blocker record: `38b63e645d85f63febda5351f62e0c0d213950df`
- Phase 1 closeout and Phase 2 task-plan commit: `1902ce0879d7cf91ecf67c4317c605c33c39ba61`.
- Phase 2 storefront facade commit: `b92186b`.
- Phase 2 public-zone commit: `89a9ad4`.
- Phase 2 secret-door concealment commit: `2ef6b27`.
- Phase 2 navigation/camera-validation commit: `56501e1`.
- Phase 2 acceptance-evidence checkpoint: `92c3dd8`.
- Phase 2 closure/owner-verification commit: `bf52d9b`.
- Remote: none. No push performed or authorized.
- Note: `Library/`, `Logs/`, `UserSettings/`, and `TestResults/` are ignored and not committed.
- One untracked Unity-generated file exists: `ProjectSettings/PackageManagerSettings.asset`. It is preserved but not committed; it contains package-manager UI/default registry state, not a package addition. Review before deciding whether to version it.

## 8. Run budget defaults

- 25 model calls per task
- 3 repair attempts per failing check
- 120 minutes wall-clock per run
- 8 queued tasks per run
- 30 changed files per task
- $0 estimated API spend while using a free model; paid-model use requires approval

See `.orchestrator/budgets.json`.

## 9. Last successful development run

**Last successful run:** `2026-10-20-ll-p1-008`
**Report:** `.orchestrator/reports/2026-10-20-ll-p1-008.md`
**Result:** `LL-P1-008` complete. Added the first EditMode suite (10 tests) for `GameState`, build-settings order, and failure reporting; hardened the PlayMode movement test against uncapped batch frame rates; and documented runnable `Start-Process` compile/test commands, exit-code expectations, failure-probe evidence, and test isolation in `TESTING_GUIDE.md`. EditMode passed 10/10, the deliberate failure probe exited `2` with `result="Failed"`, and PlayMode passed 8/8 twice. No human playtest or player build was performed.

**Phase 1 verification run:** `2026-10-20-ll-p1-009` - **complete**
**Report:** `.orchestrator/reports/2026-10-20-ll-p1-009.md`
**Human verification:** `.orchestrator/reports/2026-10-20-ll-p1-009-human-verification.md`
**Result:** Automated Phase 1 verification passed: structural validation, compile, foundation scene/player/greybox validation, 10/10 EditMode tests, 8/8 PlayMode tests, and package/pipeline checks. The project owner then confirmed all six human Game-view checks on 2026-10-20. Phase 1 is complete.

**Previous run:** `2026-10-20-ll-p2-001` - **complete**
**Report:** `.orchestrator/reports/2026-10-20-phase-2-layout-audit.md`
**Result:** Read-only audit of the actual greybox layout. Recorded dimensions, zones, circulation, clearances, Phase 2 gaps, constraints, and a placement plan. No scene, prefab, script, package, or project-setting change was made.

**Previous run:** `2026-10-20-ll-p2-002` - **complete**
**Report:** `.orchestrator/reports/2026-10-20-ll-p2-002.md`
**Result:** Added the storefront facade to `GreyboxRoom.prefab`: two south windows with frames and glass, entrance frame and open glass leaves, a 0.05 m threshold, and a minimal exterior ground/backdrop. Compile, entrance validation, greybox validation, and player validation exited `0`; EditMode passed 10/10; PlayMode passed 9/9 including the new entrance enter/leave test. The agent inspected both facade captures.

**Previous run:** `2026-10-20-ll-p2-003` - **complete**
**Report:** `.orchestrator/reports/2026-10-20-ll-p2-003.md`
**Result:** Added the south-east waiting area, west-wall storage shelving, north-west utility alcove, and placeholder storefront/counter/machine/window signage. Public-zone, entrance, greybox, and player validation exited `0`; EditMode passed 10/10; PlayMode passed 10/10 including public-zone presence and central-lane clearance. The agent inspected all three public-zone captures. Human layout review remains part of `LL-P2-006`.

**Previous run:** `2026-10-20-ll-p2-004` - **complete**
**Report:** `.orchestrator/reports/2026-10-20-ll-p2-004.md`
**Result:** Added a shallow east-wall service panel and rolling supply shelf that conceals the secret door while leaving its top edge, outer frame, casters, and a `SERVICE` marker readable. Concealment, entrance, public-zone, greybox, and player validation exited `0`; EditMode passed 10/10; PlayMode passed 11/11 including secret-door concealment. The agent inspected both secret-access captures. Human subtlety review remains part of `LL-P2-006`.

**Previous run:** `2026-10-20-ll-p2-005` - **complete**
**Report:** `.orchestrator/reports/2026-10-20-ll-p2-005.md`
**Result:** Added `ValidatePublicNavigation` and a PlayMode navigation test covering seven public routes, critical-prop colliders, renderer materials, missing references, and 1.5 m/1.7 m camera clearance. Navigation plus all regression validations exited `0`; EditMode passed 10/10; PlayMode passed 12/12.

**Latest run:** `2026-10-30-ll-p3-002` - **complete**
**Report:** `.orchestrator/reports/2026-10-30-ll-p3-002.md`
**Result:** Hardened the interaction contract. `IInteractable` now exposes prompt, eligibility, and interaction; `SimpleInteractable` is eligible by default and can be disabled; `PlayerInteractor` exposes `CurrentInteractable`, `CurrentTarget`, `CurrentPrompt`, `InteractKey`, `InteractKeyLabel`, a testable `TryInteract`, and a `TargetChanged` notification. Compile exited `0`, EditMode passed 15/15, PlayMode passed 18/18, and the log scan was clean. No carry, item, prompt UI, scene, prefab, package, or project-setting change was made.

**Previous run:** `2026-10-30-ll-p3-001` - **complete**
**Report:** `.orchestrator/reports/2026-10-30-phase-3-plan.md`
**Result:** Authored and approved the seven-task Phase 3 plan after two independent `ll-reviewer` reviews, recorded the interaction/carry/item/prompt/machine contracts in decision `0005`, and reconciled stale Phase 0/Phase 2 context in `MASTER_CONTEXT.md`, `ARCHITECTURE.md`, and `ROADMAP.md`. Bootstrap validation passed with 7 JSON documents and 31 tasks; JSON parsed. No Unity compile, test, playtest, or gameplay change was performed.

**Previous run:** `2026-10-20-ll-p2-006` - **complete**
**Report:** `.orchestrator/reports/2026-10-20-ll-p2-006.md`
**Human verification:** `.orchestrator/reports/2026-10-20-ll-p2-006-human-verification.md`
**Result:** Automated acceptance verification passed: bootstrap, compile, six scene validators, EditMode 10/10, PlayMode 12/12, clean log scan, and seven fresh captures. The project owner then reported pass for all six human-review items: traversal, entrance-to-counter path, secret-access subtlety, lighting/readability, signage, and overall layout. No blocking issue was reported. `LL-P2-006` and Phase 2 are complete. This is owner-reported human evidence, not an independent agent walkthrough.

**Review evidence:** `TestResults/phase2-acceptance.html` (local, ignored) and the captures under `TestResults/`.

## 10. Recovery pointer

Read `.orchestrator/recovery/RESUME.md` and `.orchestrator/state.json`, inspect `git status` and the latest commit, and confirm `LL-P3-002` is complete. There is no active task; `LL-P3-003` is the next ready task. Do not replay `LL-P0-*`, `LL-P1-*`, `LL-P2-001`, `LL-P2-002`, `LL-P2-003`, `LL-P2-004`, `LL-P2-005`, `LL-P2-006`, `LL-P3-001`, or `LL-P3-002`. If the tree is not clean, follow the recovery procedure before editing.

## 11. Known technical debt and risks

- Phase 2 is complete. Preserve the validated layout for Phase 3 interaction placement; rerun navigation/camera validation after any scene or prefab change.
- `ProjectSettings/PackageManagerSettings.asset` is untracked and Unity-generated. It was preserved but not committed; review before versioning.
- Unity Personal licensing requires periodic online refresh; batch automation can fail with exit `198` for reasons unrelated to the project.
- Project settings were edited as serialized YAML before an editor settings API existed; Unity re-serialized and preserved them, but future changes should use an editor script or the Unity UI.
- Linear color space and Medium quality are active. Greybox lighting was checked by the project owner in the Game view on 2026-10-20 and reported acceptable; no measured performance data exists.
- The greybox room, props, machines, and secret door are static placeholder primitives; interaction targeting exists, but carrying and machine behavior do not.
- The storefront glazing and open door leaves are static placeholders; there is no door interaction or animation. Phase 2 rendering captures require a graphics-capable batch run because `-nographics` crashed during capture.
- Single-sided placeholder text faces its intended reader: `LAUNDRY LEGENDS` is mirrored from inside and `DROP OFF` is mirrored from the room side. The owner reported the Phase 2 review as passed and did not mark this as blocking; improve it later if presentation quality requires it.
- The secret door is visually foreshadowed and concealed by a static service panel/shelf. The owner reported the Phase 2 subtlety review as passed; secret-room access and reveal logic are not implemented.
- Four real-time point lights are unoptimized; performance has not been measured.
- The pause overlay uses IMGUI as a placeholder; real UI arrives later.
- Cursor lock/release and movement feel were confirmed by the project owner on 2026-10-20; batch tests still skip cursor APIs, so this remains owner-reported rather than independently reproduced.
- `GameSession.Instance` is a static service handle; consumers should prefer explicit references where practical.
- Test coverage is 15 EditMode and 18 PlayMode tests; it does not yet cover carrying, machine cycles, orders, economy transactions, save/load, or UI. Phase 3 must add carry and machine-hook coverage.
- `FailureReportingTests` fails when `LL_FAIL_PROBE` is left set; unset it after probing.
- The stray `C:\Users\Asus\Desktop\AI` log remains pending manual deletion.
- The global OpenCode config points at a local Ollama model; the project config overrides it with DeepSeek. Confirm the provider remains available in future sessions.
- The design contains proposed economy numbers that have never been tuned.
- Asset production is blocked on tool availability or licensed external sources.
- The game is a long multi-session build; no claim of autonomous completion is made.
