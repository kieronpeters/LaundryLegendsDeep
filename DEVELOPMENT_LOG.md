# Laundry Legends — Development Log

Chronological record of meaningful events, decisions, bugs, fixes, checks, and milestones. A feature is marked **verified** only when the evidence exists.

## 2026-10-19 — Phase 0 bootstrap

**Phase:** 0 — Project bootstrap
**Run:** `2026-10-19-bootstrap`
**Status:** Complete (machine-verified; human review recommended)

### Environment verified

- Workspace was empty before bootstrap; no user files were present.
- OpenCode CLI `2.0.28`; background server reported `2.0.32`.
- Current primary model: `opencode/deepseek-v4.1-flash-free`; provider catalog verified through the model tool.
- Git `2.55.0.windows.3`; Git LFS `3.7.1`; .NET SDK `10.0.401`; Node `24.21.0`; npm `11.19.0`; Python `3.15.0`.
- Unity Hub `43.5.0`.
- Unity editors installed: `6000.0.0f1`, `6000.0.64f1`, `6000.5.7f1`.
- Selected editor: `6000.0.64f1` (Unity 6.0 LTS), with Windows standalone playback engine installed.
- Unity entitlement license file exists; batch-mode launch not yet tested.
- No Blender, FFmpeg, ImageMagick, or GitHub CLI on `PATH`.
- No dedicated image, 3D, audio, or voice-generation tool verified.
- No MCP servers or plugins configured in the global OpenCode config.
- The global OpenCode config defaults to `ollama/qwen3.5:4b`; the project config selects the verified DeepSeek model. No credentials were read or exposed.

### Decisions

- **Unity 6.0 LTS / Built-In Render Pipeline.** Rationale: installed, supported, Windows module present, simpler for a small stylized 3D game, no new pipeline dependency. See `.orchestrator/decisions/0001-unity-version-and-render-pipeline.md`.
- **Legacy Input Manager initially.** Avoid a package change before it is justified.
- **Laundry loop before arcade systems.** The secret arcade remains the central progression hook, but the first playable must be the laundry loop.
- **Single `main` branch, local checkpoints, no remote yet.** Destructive Git operations and pushes are blocked/approval-gated.
- **Project-local OpenCode agents and commands.** Lead is built-in `build`; specialists are `ll-unity-dev`, `ll-designer`, `ll-reviewer`, and `ll-qa`.
- **Persistent state is split between human-readable Markdown and machine-readable JSON.** No database or service.

### Created

- Root documentation: `README.md`, `AGENTS.md`, `PROJECT_STATE.md`, `MASTER_CONTEXT.md`, `GAME_DESIGN.md`, `ARCHITECTURE.md`, `ROADMAP.md`, `ASSET_INVENTORY.md`, `AGENT_GUIDE.md`, `TESTING_GUIDE.md`, `YOUTUBE_LOG.md`, `DEVELOPMENT_LOG.md`.
- `.opencode/opencode.json`, four subagents, and ten slash commands.
- `.orchestrator/` state, budgets, task definitions, decision records, reports, and recovery guide.
- `.gitignore`, `tools/validate-bootstrap.ps1`, `tools/status.ps1`.
- Empty `ArtSource/`, `Builds/`, `TestResults/` directories with markers.

### Checks performed

- `git init -b main`; local `core.autocrlf=false` and `core.safecrlf=false`.
- JSON/frontmatter/command/document structural validation via `tools/validate-bootstrap.ps1` (result recorded in the bootstrap report).
- Git status/ignore audit before checkpoint.
- No Unity compile, Unity test, scene, gameplay, build, or playtest was run because no Unity project exists.

### Not performed / limitations

- Unity batch-mode licensing and project creation.
- Any C# compilation or Unity test.
- Any visual, audio, gameplay, performance, or build verification.
- Any asset creation or integration.
- Any external publishing or remote push.

### Git

- Initial branch: `main`.
- Baseline commit: `b78218cccfeea741d3d227ffa95e679e9ba92a72` (`chore: bootstrap Laundry Legends development system`).
- Bootstrap checkpoint tag: `bootstrap-v0.1` on the state-record commit.
- Post-checkpoint validation passed with exit code 0 at 2026-10-19 20:34 (UTC+08:00); log: `TestResults/bootstrap-validation-2026-10-19.log`. The tag is reachable from `HEAD` and the working tree is clean after the documentation correction commit.

### Independent review

A read-only subagent reviewed the bootstrap before the checkpoint. It found pre-checkpoint completion overclaims, Git-write exposure for built-in subagents, a designer permission that was too broad, a QA task scope mismatch, a test-folder inconsistency, and two documentation contradictions. These were corrected before the checkpoint. The final post-checkpoint validation is recorded in the bootstrap run report.

### Next

`LL-P1-001` — create and validate the Unity 6.0 LTS project at the repository root, preserving bootstrap files, then verify the editor log before relying on batch-mode automation.

## 2026-10-19 — LL-P1-001 Unity project creation

**Phase:** 1 — Unity foundation
**Task:** `LL-P1-001`
**Status:** Complete (license refreshed by the human; batch launch and compile verified)

### What happened

- Initial batch probe failed with exit `198`: the Unity Personal offline license validity window had expired (`LicenseGroupOfflineValidityPeriodIsExpired`, token cache empty).
- Unity Hub was launched; the human signed in and refreshed the license.
- The license probe retry exited `0` and resolved entitlements.
- Unity project created at the repository root with `6000.0.64f1`; all bootstrap files remained intact.
- Added only the pre-approved baseline packages `com.unity.ugui` 2.0.0 (includes TextMeshPro in Unity 6) and `com.unity.test-framework` 1.6.0. Both resolved from the editor's local built-in packages; no remote download.
- Compile smoke check exited `0` with no compiler errors.

### Verified baseline

- `ProjectSettings/ProjectVersion.txt`: `6000.0.64f1 (5360b7cd7953)`.
- Built-In Render Pipeline: `m_CustomRenderPipeline: {fileID: 0}`; no URP/HDRP package.
- Legacy Input Manager: `activeInputHandler: 0`.
- 35 direct package dependencies; 38 resolved packages, all `builtin`.
- `Assets/` exists but is empty.

### Deviations deferred for approval

- Gamma color space (planned: Linear).
- Ultra quality default (planned: one modest preset).
- Full-screen mode (planned: windowed).
- `DefaultCompany` company name and `LaundryLegends` product name.
- Unused `com.unity.multiplayer.center` editor-default package.

### Checks and evidence

- `TestResults/unity-create.log` — exit `0`.
- `TestResults/unity-compile-smoke.log` — exit `0`, no errors.
- `TestResults/unity-license-probe-2026-10-19.log` — evidence of the expired-license failure before refresh.
- `git status` — only `Packages/` and `ProjectSettings/` added plus intended state/doc edits; bootstrap intact.

### Incident

An early `-createProject` attempt passed an unquoted path containing spaces. Unity truncated the project path and wrote a 995-byte log to `C:\Users\Asus\Desktop\AI`. No repository files were affected. The stray file is outside the repository; deletion was blocked by project permissions and requires cleanup approval.

### Not performed

- Unity tests, scene validation, playtest, and player build.

### Next

`LL-P1-002` — review and approve the baseline manifest and project settings.

## 2026-10-19 — LL-P1-002 baseline approval

**Phase:** 1 — Unity foundation
**Task:** `LL-P1-002`
**Status:** Complete (human approved all recommendations)

### Approved and applied

- Color space: Gamma → **Linear**.
- Company/product: `DefaultCompany` / `LaundryLegends` → **`LaundryLegends` / `Laundry Legends`**.
- Quality: Ultra (index 5) → **Medium (index 2)**.
- Display: full-screen window → **windowed 1920×1080, resizable**; `defaultIsNativeResolution` set to `0` so the window size applies.
- Removed the unused `com.unity.multiplayer.center` editor-default package.
- Kept Built-In Render Pipeline and Legacy Input Manager.

### Checks and evidence

- `TestResults/unity-baseline-verify.log` — Unity reopen exit `0`; lock file updated; 37 packages; no compile/exception matches.
- Re-read settings confirmed all approved values persisted.
- `Packages/manifest.json` now has 34 direct dependencies; no URP/HDRP or third-party runtime package.

### Notes

- Settings were changed by editing serialized YAML while Unity was closed, then verified by a batch reopen. Future project-settings changes should use an editor script or the Unity UI.
- The stray Desktop log could not be deleted because the shell policy hard-denies delete commands; manual cleanup remains.

### Next

`LL-P1-003` — create the `Assets/LaundryLegends` folder structure and runtime/editor/test assembly definitions.

## 2026-10-19 — LL-P1-003 folder structure and assemblies

**Phase:** 1 — Unity foundation
**Task:** `LL-P1-003`
**Status:** Complete

### Created

- 33 directories under `Assets/LaundryLegends`: Art (characters/environment/props/materials/textures), Audio (ambience/music/SFX/UI), Prefabs (gameplay/environment/UI), Scenes, Scripts (Core/Player/Interaction/Laundry/Customers/Economy/Arcade/UI/Save), Settings, ScriptableObjects, UI, Editor, Tests (EditMode/PlayMode).
- Runtime assembly: `Scripts/LaundryLegends.Runtime.asmdef` with no references.
- Editor assembly: `Editor/LaundryLegends.Editor.asmdef`, editor-only, references runtime.
- Test assemblies: EditMode and PlayMode, both referencing runtime, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, and `nunit.framework.dll`, gated by `UNITY_INCLUDE_TESTS`.
- `Scripts/Core/RuntimeAssemblyInfo.cs` marker type.

### Checks and evidence

- `TestResults/unity-asmdefs.log` — Unity import exit `0`; `LaundryLegends.Runtime.dll` built; no compiler errors.
- Unity reported that the editor and test assemblies “will not be compiled” because they have no scripts. This is expected at this stage.

### Next

`LL-P1-004` — create the Bootstrap, MainMenu, and Laundromat scenes and register them in build settings.

## 2026-10-19 — LL-P1-004 foundation scenes

**Phase:** 1 — Unity foundation
**Task:** `LL-P1-004`
**Status:** Complete

### Created

- `Assets/LaundryLegends/Scenes/Bootstrap.unity`
- `Assets/LaundryLegends/Scenes/MainMenu.unity`
- `Assets/LaundryLegends/Scenes/Laundromat.unity`
- `Assets/LaundryLegends/Editor/SceneSetupTool.cs` — idempotent scene creation and validation tool with menu items and batch-callable methods.

### Build settings

- Index 0: Bootstrap
- Index 1: MainMenu
- Index 2: Laundromat
- All enabled.

### Checks and evidence

- `TestResults/unity-scenes-create.log` — exit `0`; three scenes created.
- `TestResults/unity-scenes-validate.log` — exit `0`; every scene opened; no missing scripts; build order verified.
- Only `ProjectSettings/EditorBuildSettings.asset` changed among project settings.

### Notes

The scenes are intentionally empty shells. Camera, player, UI, greybox, and lighting arrive in later Phase 1 tasks.

### Next

`LL-P1-005` — implement the minimal `GameState` foundation and scene routing.

## 2026-10-19 - LL-P1-005 GameState and routing

**Phase:** 1 - Unity foundation
**Task:** LL-P1-005
**Status:** Complete (PlayMode routing test passed)

### Implemented

- GameState: instance-based money, day/time, and progression flags. Money has no public mutator; EconomyService will be the single writer later.
- GameSession: one persistent session created in Bootstrap, marked DontDestroyOnLoad, duplicate-safe. Instance is a service handle, not gameplay state.
- SceneRouter: single owner of foundation scene names and transitions.
- BootstrapController: verifies GameState, then routes to MainMenu.
- MainMenuController: routes to Laundromat; temporary Return-key trigger.
- SceneSetupTool extended to add and validate the scene components.
- ARCHITECTURE.md documents runtime state lifetime.

### Checks and evidence

- TestResults/unity-session-setup.log - exit 0; session components added.
- TestResults/unity-session-validate.log - exit 0; components present, no missing scripts.
- TestResults/playmode.xml - BootstrapRoutesThroughMainMenuToLaundromat passed.
- TestResults/playmode.log - no error/exception lines.

### Next

LL-P1-006 - first-person player controller and camera.

## 2026-10-19 - LL-P1-006 first-person player

**Phase:** 1 - Unity foundation
**Task:** LL-P1-006
**Status:** Complete (automated checks; manual cursor/feel check unrun)

### Implemented

- FirstPersonController: CharacterController movement, gravity, mouse look, clamped pitch; legacy Input Manager only.
- PauseController: Esc toggles pause, releases/locks the cursor, and sets Time.timeScale 0/1. OnGUI overlay is a placeholder.
- PlayerInteractor: 3 m camera raycast, CurrentTarget/CurrentPrompt, E interaction.
- IInteractable + SimpleInteractable placeholder contract.
- Player.prefab with CharacterController, camera, AudioListener, and all player components.
- PlayerSetupTool creates the prefab and adds the player, temporary floor, and directional light to Laundromat.
- PlayerControllerTests covers movement/look, pause/resume, and interaction.

### Checks and evidence

- TestResults/unity-player-setup.log - exit 0; prefab and scene content created.
- TestResults/unity-player-validate.log - exit 0; required components present; no Input System package.
- TestResults/playmode.xml - 4/4 PlayMode tests passed.
- TestResults/playmode.log - no error/exception lines.
- Initial interaction test failed due to one-frame physics sync; fixed with Physics.SyncTransforms plus a two-frame wait. Retry passed.

### Not performed

- Manual cursor lock/release and movement-feel check.
- Manual pause overlay button check.
- Player build.

### Next

LL-P1-007 - greybox laundromat environment and lighting.

## 2026-10-19 - LL-P1-007 greybox environment and lighting

**Phase:** 1 - Unity foundation
**Task:** LL-P1-007
**Status:** Complete (automated and captured-image checks; human visual check unrun)

### Implemented

- GreyboxRoom prefab: 16x12 m room, 3.2 m walls, entrance gap, ceiling, counter, folding table, eight-machine wall, and a closed foreshadowed secret door.
- Seven flat Standard materials under Art/Materials for floor, walls, washer, dryer, counter, metal, and secret panel.
- Four warm ceiling point lights (range 12, intensity 2.4) and a 0.55-intensity directional fill. Flat ambient lighting, fog off.
- Player spawn moved to (0, 0.1, -4.5) inside the entrance; temporary floor removed.
- GreyboxSetupTool: reproducible build, validation, and preview capture commands.
- PlayerSetupTool now treats GreyboxRoom as the walkable floor.
- GreyboxTraversalTests: room traversal, east-wall containment, secret-door closure, scale dimensions, and lighting presence.

### Checks and evidence

- TestResults/compile-greybox.log - exit 0; no compiler errors.
- TestResults/unity-greybox-build.log - exit 0; room prefab built and placed.
- TestResults/unity-greybox-validate.log - exit 0; floor, walls, secret door, lights, and spawn valid.
- TestResults/unity-player-validate.log - exit 0; player still valid with the greybox floor.
- TestResults/playmode.xml - 8/8 PlayMode tests passed.
- TestResults/greybox-entrance.png and greybox-secret.png - captured and inspected by the agent.

### Repair

- Initial traversal test used 200 frames, which moved only 0.55 m under uncapped batch frame rate. Replaced with a 2.6-second Time.deltaTime loop.
- Lighting was tuned from 1.8 (too dark) to 3.2 (clipped walls) to 2.4 with slightly darker walls.

### Not performed

- Human visual check of scale, readability, and lighting comfort.
- Manual playtest.
- Player build and performance measurement.

### Next

LL-P1-008 - test scaffolding and batch validation commands.

## 2026-10-20 - LL-P1-008 test scaffolding and batch commands

**Phase:** 1 - Unity foundation
**Task:** LL-P1-008
**Status:** Complete (automated checks; no playtest or build)

### Implemented

- EditMode GameStateTests: money storage/rejection, no public money setter, flag rules, and day advance.
- EditMode BuildSettingsTests: Bootstrap -> MainMenu -> Laundromat order and enabled scenes.
- EditMode FailureReportingTests: opt-in LL_FAIL_PROBE=1 probe that deliberately fails.
- Hardened PlayMode PlayerMovesAndLooks: accumulates 0.1 seconds of Time.deltaTime instead of relying on one frame.
- TESTING_GUIDE.md: runnable Start-Process commands, exit-code guidance, current suite inventory, failure probe, batch timing, and test isolation.

### Checks and evidence

- TestResults/editmode.xml - exit 0; 10/10 passed.
- TestResults/editmode-probe.xml - exit 2; deliberate probe failed with result="Failed".
- TestResults/editmode.log - normal rerun after the probe; 10/10 passed.
- TestResults/playmode.xml - exit 0; 8/8 passed.
- TestResults/playmode-repeat.xml - exit 0; 8/8 passed again.
- tools/validate-bootstrap.ps1 - passed.

### Repair

- One PlayMode run failed because PlayerMovesAndLooks moved for only one uncapped batch frame. Fixed with a simulated-duration loop; two repeat runs passed.

### Not performed

- Human playtest.
- Player build and performance measurement.

### Next

LL-P1-009 - Phase 1 acceptance verification and evidence record.
## 2026-10-20 - LL-P1-009 Phase 1 verification

**Phase:** 1 - Unity foundation
**Task:** LL-P1-009
**Status:** Blocked on the required human verification; all automated checks passed

### Checks and evidence

- tools/validate-bootstrap.ps1 - PASSED.
- TestResults/phase1-compile.log - exit 0; no compile/import errors.
- TestResults/phase1-scenes.log - exit 0; three scenes load, Bootstrap index 0, no missing scripts.
- TestResults/phase1-player.log - exit 0; player/camera/pause/interaction/floor/light valid; no Input System.
- TestResults/phase1-greybox.log - exit 0; floor, walls, secret door, lights, spawn valid.
- TestResults/phase1-editmode.xml - exit 0; 10/10 passed.
- TestResults/phase1-playmode.xml - exit 0; 8/8 passed.
- Package/pipeline check - no Input System, URP, or HDRP; no manifest change since baseline.

### Blocker

The roadmap requires a human to verify movement feel, camera, cursor release, and greybox readability. That check has not been performed, so Phase 1 is not complete. The checklist is in .orchestrator/reports/2026-10-20-ll-p1-009.md.

### Not performed

- Human Game-view verification.
- Player build and performance measurement.

### Next

Resume LL-P1-009 with the human check. If it passes, close Phase 1 and start Phase 2; if it fails, fix the scoped issue and rerun the affected checks.
## 2026-10-20 - Phase 1 close and Phase 2 layout audit

**Phase:** 1 complete; Phase 2 in progress
**Tasks:** LL-P1-009 closeout, LL-P2-001
**Status:** Phase 1 complete with owner-reported human verification; LL-P2-001 complete

### Human verification

- The project owner confirmed all six Phase 1 Game-view checks on 2026-10-20: movement/speed, camera/comfort, pause/cursor release, room layout, the closed east-wall secret door, and lighting/readability.
- Recorded as owner-reported evidence in `.orchestrator/reports/2026-10-20-ll-p1-009-human-verification.md` and appended as a clearly separated addendum to the automated report.
- The agent did not independently observe the Unity session, capture new screenshots, or rerun tests for this confirmation.

### Phase 1 closeout

- All Phase 1 acceptance criteria now have automated evidence plus the human verification.
- Original automated results, report, and `TestResults/greybox-*.png` captures were preserved.
- Phase 1 marked complete in the task file, roadmap, project state, and orchestration state.
- Phase 2 task plan created from the roadmap: layout audit, entrance/windows, waiting/storage/signage, secret-door concealment, collision/path validation, and phase verification.

### LL-P2-001 audit

- Parsed the actual `GreyboxRoom.prefab` and `Laundromat.unity` serialization rather than assuming the generator output.
- Confirmed a 16 x 12 m room, 3.2 m ceiling, 4 x 2.4 m south opening, player spawn at `(0, 0.1, -4.5)`, counter, folding table, eight-machine wall, four point lights, and the exposed east-wall secret door.
- Mapped circulation and clearances. Recorded that entrance door/windows, waiting area, storage, utility alcove, signage, and secret-door concealment are missing or partial.
- No `Assets/`, `Packages/`, or `ProjectSettings/` change was made. No compile or test rerun was needed.
- Report: `.orchestrator/reports/2026-10-20-phase-2-layout-audit.md`.

### Not performed

- No new Unity playtest or capture for the audit.
- No player build.
- Human Phase 2 layout verification remains deferred to LL-P2-006.

### Next

`LL-P2-002` - add the entrance door, storefront windows, and threshold, then run compile, validation, EditMode/PlayMode, and capture the facade.

## 2026-10-20 - LL-P2-002 storefront entrance and windows

**Phase:** 2 - Physical laundromat
**Task:** LL-P2-002
**Status:** Complete (automated checks and agent-inspected captures; human facade review pending)

### Implemented

- Added `Phase2SetupTool` as an additive, idempotent facade builder, validator, and capture command.
- Split the solid south wall into two window openings with piers, sills, headers, metal frames, mullions, and transparent glass.
- Added entrance jambs, a header, open glass door leaves, and a 0.05 m threshold around the existing 4 m opening.
- Added a minimal exterior ground strip and backdrop so the doorway and windows do not open onto empty space.
- Added `Grey_Glass` (transparent) and `Grey_Sky` (emissive) placeholder materials.
- Added a PlayMode test that walks the player into the room through the storefront and back out.

### Checks and evidence

- `TestResults/phase2-compile.log` - exit 0; no compiler errors.
- `TestResults/phase2-build.log` - exit 0; facade built into the room prefab.
- `TestResults/phase2-validate.log` - exit 0; doorway clear, window glass colliders hit, threshold low, secret door still closed.
- `TestResults/phase2-greybox-validate.log` and `TestResults/phase2-player-validate.log` - exit 0.
- `TestResults/phase2-editmode.xml` - 10/10 passed.
- `TestResults/phase2-playmode.xml` - 9/9 passed, including the new entrance enter/leave test.
- `TestResults/phase2-entrance.png` and `TestResults/phase2-storefront.png` - captured and inspected by the agent.

### Repairs

- The first entrance validation ray began inside the player capsule and hit the player. The ray now starts inside the doorway past the spawn capsule.
- A `-nographics` capture crashed with `0xC0000005`. The capture succeeded without `-nographics`; the testing guide now marks graphics as required.

### Not performed

- Human Phase 2 facade/layout review; deferred to `LL-P2-006`.
- Player build and performance measurement.
- No package, project-setting, or runtime-script change.

### Next

`LL-P2-003` - add the waiting area, storage shelving, utility alcove, and placeholder signage.

## 2026-10-20 - LL-P2-003 public zones and signage

**Phase:** 2 - Physical laundromat
**Task:** LL-P2-003
**Status:** Complete (automated checks and agent-inspected captures; human public-zone review pending)

### Implemented

- Added a south-east waiting area with two chairs, a side table, and a planter.
- Added two shallow west-wall storage shelving units with bins.
- Added a north-west utility alcove with a half-height divider, cabinet/sink, mop bucket, and `STAFF ONLY` sign.
- Added placeholder signage on the storefront, machine wall (`WASH   DRY   FOLD`), counter (`DROP OFF`), and right window (`OPEN`).
- Added `Grey_Chair` and `Grey_Sign` placeholder materials.
- Extended `Phase2SetupTool` with idempotent `BuildPublicZones`, `ValidatePublicZones`, and `CapturePublicZones` commands.
- Added a PlayMode test for public-zone presence and central-lane clearance.

### Checks and evidence

- `TestResults/phase2-p3-build.log` - exit 0; public zones built into the room prefab.
- `TestResults/phase2-p3-public-validate.log` - exit 0; props present and central aisle/customer path clear.
- `TestResults/phase2-p3-entrance-validate.log`, `phase2-p3-greybox-validate.log`, and `phase2-p3-player-validate.log` - exit 0.
- `TestResults/phase2-p3-editmode.xml` - 10/10 passed.
- `TestResults/phase2-p3-playmode.xml` - 10/10 passed, including `PublicZonesArePresentAndClear`.
- `TestResults/phase2-waiting.png`, `phase2-machine-signage.png`, and `phase2-utility.png` - captured and inspected by the agent.

### Repairs

- The first public-zone validation linecast began inside the player capsule and reported the aisle blocked by `Player` at 0.46 m. The linecasts now start at `z = -4.1`, past the capsule.
- The first full PlayMode run failed the entrance leave assertion at `-4.475` after teleporting the player. The test now calls `Physics.SyncTransforms()` after teleports; the rerun passed 10/10.
- A temporary debug method used during the repair was removed.

### Not performed

- Human Phase 2 layout, signage, and clearance review; deferred to `LL-P2-006`.
- Player build and performance measurement.
- No package or project-setting change.

### Next

`LL-P2-004` - conceal the secret door behind a shelf/service panel.

## 2026-10-20 - LL-P2-004 secret-door concealment

**Phase:** 2 - Physical laundromat
**Task:** LL-P2-004
**Status:** Complete (automated checks and agent-inspected captures; human subtlety review pending)

### Implemented

- Added a shallow `SecretAccess_ServicePanel` across the east-wall secret door.
- Added a rolling `SecretAccess_Shelf` in front: four boards, two side panels, four casters, and a supply bin.
- Left the top of the dark door and the outer frame edges readable so the access is foreshadowed rather than fully hidden.
- Added a `SERVICE` marker above the cover using the built-in `LegacyRuntime.ttf`.
- Added `BuildSecretConcealment`, `ValidateSecretConcealment`, and `CaptureSecretApproach` to `Phase2SetupTool`.
- Added a PlayMode test for cover placement, door-width coverage, visible top edge, marker text, and aisle-to-door occlusion.
- No new material, package, or project-setting change.

### Checks and evidence

- `TestResults/phase2-p4-compile.log` - exit 0; no compiler errors.
- `TestResults/phase2-p4-build.log` - exit 0; concealment built into the room prefab.
- `TestResults/phase2-p4-validate.log` - exit 0; cover, marker, colliders, clearance, and closed door valid.
- `TestResults/phase2-p4-entrance-validate.log`, `phase2-p4-public-validate.log`, `phase2-p4-greybox-validate.log`, and `phase2-p4-player-validate.log` - exit 0.
- `TestResults/phase2-p4-editmode.xml` - 10/10 passed.
- `TestResults/phase2-p4-playmode.xml` - 11/11 passed, including `SecretDoorIsConcealedButForeshadowed`.
- `TestResults/phase2-secret-approach.png` and `phase2-secret-close.png` - captured and inspected by the agent.

### Repairs

- The initial validation measured the whole `SecretAccess` root, including the marker above the door, and would have failed the visible-top-edge check. Validation now measures the shelf body only.
- The first close capture cropped the marker and door edge; the final three-quarter view includes both.

### Not performed

- Human Phase 2 concealment/subtlety review; deferred to `LL-P2-006`.
- Player build and performance measurement.
- No secret-room reveal or interaction logic.

### Next

`LL-P2-005` - add Phase 2 collision, navigation, and camera-clearance validation.

## 2026-10-20 - LL-P2-005 public navigation and camera clearance

**Phase:** 2 - Physical laundromat
**Task:** LL-P2-005
**Status:** Complete (automated checks; human walkthrough pending)

### Implemented

- Added `Phase2SetupTool.ValidatePublicNavigation` with seven public-route linecasts, critical-prop collider checks, material/missing-reference checks, and 1.5 m/1.7 m camera-sphere clearance samples.
- Added `PublicNavigationAndCameraClearanceAreValid` to the PlayMode suite with the same route and camera checks.
- Added route diagnostics that report the blocking collider and distance.
- No prefab, package, project-setting, or runtime-script change.

### Checks and evidence

- `TestResults/phase2-p5-compile.log` - exit 0; no compiler errors.
- `TestResults/phase2-p5-navigation-validate.log` - exit 0; routes, colliders, materials, and camera clearance valid.
- `TestResults/phase2-p5-entrance-validate.log`, `phase2-p5-public-validate.log`, `phase2-p5-secret-validate.log`, `phase2-p5-greybox-validate.log`, and `phase2-p5-player-validate.log` - exit 0.
- `TestResults/phase2-p5-editmode.xml` - 10/10 passed.
- `TestResults/phase2-p5-playmode.xml` - 12/12 passed, including `PublicNavigationAndCameraClearanceAreValid`.
- Log scan found no compiler errors, exceptions, missing references, shader errors, or aborts.

### Repairs

- The first route set crossed `FoldingTableTop` on the east side and `Entrance_Door_Right` on the waiting-to-entrance leg. Routes now use the 6.4-6.7 m east lane and a z = -3.6 m cross-room lane.
- Camera sample `(2.0, -3.9)` hit the open right door leaf; it moved to `(2.0, -3.6)`.

### Not performed

- Human Phase 2 walkthrough and subtlety review; deferred to `LL-P2-006`.
- Player build and performance measurement.
- No runtime navigation, NavMesh, or collision change.

### Next

`LL-P2-006` - run the complete Phase 2 verification, capture the layout, and obtain human review.

## 2026-10-20 - LL-P2-006 automated acceptance verification

**Phase:** 2 - Physical laundromat
**Task:** LL-P2-006
**Status:** Complete - automated checks and owner-reported human acceptance passed

### Implemented / performed

- Ran bootstrap validation, compile, entrance/public-zone/secret/navigation/greybox/player validation, EditMode, and PlayMode.
- Captured seven final Phase 2 views without `-nographics`.
- Inspected all seven captures and prepared `TestResults/phase2-acceptance.html` plus a walkthrough checklist.
- No scene, prefab, runtime-script, package, or project-setting change.

### Checks and evidence

- `TestResults/phase2-p6-compile.log` - exit 0.
- `TestResults/phase2-p6-entrance-validate.log`, `phase2-p6-public-zones-validate.log`, `phase2-p6-secret-validate.log`, `phase2-p6-navigation-validate.log`, `phase2-p6-greybox-validate.log`, and `phase2-p6-player-validate.log` - all exit 0.
- `TestResults/phase2-p6-editmode.xml` - 10/10 passed.
- `TestResults/phase2-p6-playmode.xml` - 12/12 passed.
- Log scan found no compiler errors, exceptions, missing references, shader errors, or aborts.
- Fresh captures: `phase2-entrance.png`, `phase2-storefront.png`, `phase2-waiting.png`, `phase2-machine-signage.png`, `phase2-utility.png`, `phase2-secret-approach.png`, `phase2-secret-close.png`.
- Agent observation: the exterior-facing `LAUNDRY LEGENDS` sign is mirrored from inside, and `DROP OFF` is mirrored from the room side. The owner did not report this as blocking.
- Owner-reported human review: the project owner replied "pass" for all six checklist items on 2026-10-20. Recorded separately in `.orchestrator/reports/2026-10-20-ll-p2-006-human-verification.md`. No independent agent walkthrough was performed.

### Not performed

- Independent agent Game-view walkthrough; the human result is owner-reported only.
- Per-item owner notes, timestamps, or screenshots; the owner gave one overall "pass".
- Player build and performance measurement.
- No interaction or secret-reveal implementation.

### Next

Phase 2 is complete. Author and approve the Phase 3 task plan before starting interaction work; `.orchestrator/tasks/phase-03.json` does not exist yet. Do not replay `LL-P2-006`.

## 2026-10-30 - LL-P3-001 Phase 3 plan and interaction contracts

**Phase:** 3 - Laundry interaction foundation
**Task:** LL-P3-001
**Status:** Complete - documentation and orchestration only

### Implemented / performed

- Authored a seven-task Phase 3 plan (`LL-P3-001` through `LL-P3-007`) in `.orchestrator/tasks/phase-03.json`.
- Obtained two independent read-only `ll-reviewer` reviews. Both returned "approve with changes"; every blocking finding was applied before approval.
- Recorded the accepted interaction, type-ownership, item-identity, carry, prompt, and machine-hook contracts in `.orchestrator/decisions/0005-phase-3-interaction-contracts.md`.
- Reconciled `MASTER_CONTEXT.md` and `ARCHITECTURE.md`, which still described a pre-Unity Phase 0 project.
- Updated `ROADMAP.md` and `PROJECT_STATE.md` for Phase 3 start.
- No script, prefab, scene, material, package, or project-setting change.

### Decisions

- `IInteractable` exposes prompt, `CanInteract(GameObject)`, and `Interact(GameObject)`.
- `PlayerInteractor` owns target resolution, prompt state, the interaction key/label, a testable `TryInteract`, and change notification.
- `CarryController` holds one item on the Player prefab; `LaundryItem` carries `TemplateId`, runtime `InstanceId`, and kind (`Basket`, `LooseLaundry`, `Bag`).
- Only `LooseLaundry` is washer-loadable in Phase 3. Basket contents, machine cycles, economy, orders, and customers remain out of scope.
- The prompt is an IMGUI placeholder with testable `IsVisible`/`CurrentLabel` state; no TextMeshPro import or new package.

### Checks and evidence

- `phase-03.json` and `state.json` parsed with `ConvertFrom-Json`.
- `tools/validate-bootstrap.ps1` passed on 2026-10-30: 7 JSON documents and 31 tasks; only the expected asset-baseline warning.
- `git diff --check` reported no whitespace errors.
- Report: `.orchestrator/reports/2026-10-30-phase-3-plan.md`.

### Not performed

- Unity compile, EditMode, PlayMode, log scan, captures, playtest, or any gameplay change. This task was documentation-only.
- No human review was requested for `LL-P3-001`; the human acceptance gate is `LL-P3-007`.

### Next

`LL-P3-002` - harden the interaction contract and target resolution. Do not start `LL-P3-003` until `LL-P3-002` compiles and its tests pass.

## 2026-10-30 - LL-P3-002 interaction contract and target resolution

**Phase:** 3 - Laundry interaction foundation
**Task:** LL-P3-002
**Status:** Complete - compile, EditMode, PlayMode, and log scan passed

### Implemented / performed

- Extended `IInteractable` with `CanInteract(GameObject)` while preserving `InteractionPrompt` and `Interact`.
- Made `SimpleInteractable` eligible by default, disableable through a serialized flag or component disable, and event-safe when ineligible.
- Added `CurrentInteractable`, `InteractKey`, `InteractKeyLabel`, `TryInteract`, and a `TargetChanged` notification to `PlayerInteractor`.
- Centralized target resolution: out-of-range, blocked, ineligible, disabled, and paused targets are neither reported nor dispatched.
- Added five EditMode eligibility tests and six PlayMode interaction tests.
- No carry, item, prompt UI, scene, prefab, package, or project-setting change.

### Checks and evidence

- Unity compile exited `0`; log clean (`TestResults/p3-002-compile.log`, ignored).
- EditMode: 15/15 passed (`TestResults/p3-002-editmode.xml`, ignored).
- PlayMode: 18/18 passed without `-nographics` (`TestResults/p3-002-playmode.xml`, ignored).
- Log scan found no compiler errors, exceptions, missing references, shader errors, or aborts.
- Bootstrap validation passed with 7 JSON documents and 31 tasks; JSON parsed; `git diff --check` passed.
- Report: `.orchestrator/reports/2026-10-30-ll-p3-002.md`.

### Not performed

- Carry, item identity, item prefabs, prompt UI, and machine load/unload. These are `LL-P3-003` through `LL-P3-006`.
- Human Game-view review, playtest, player build, and performance measurement.
- `ValidatePlayerSetup`; the Player prefab and scene were not modified.

### Next

`LL-P3-003` - implement carry state, item identity, and drop/place rules. Do not start `LL-P3-004` until `LL-P3-003` compiles and its tests pass.

## 2026-10-30/31 - LL-P3-003 carry, item identity, and drop/place rules

**Phase:** 3 - Laundry interaction foundation
**Task:** LL-P3-003
**Status:** Complete - compile, EditMode, PlayMode, player validation, and log scan passed

### Implemented / performed

- Added `LaundryItemKind` (`Basket`, `LooseLaundry`, `Bag`) and `LaundryItem` with authored `TemplateId`, runtime `InstanceId`, carryability, and washer-loadability.
- Added `CarryController` to the Player prefab with an explicit `HoldAnchor`; it holds one item, suspends held colliders/rigidbody, restores them on drop, and resolves a wall-aware drop point.
- Added `CarrySlot` with empty-only, kind-compatible placement and occupant physics stabilization.
- Added no-stuck handling for destroyed/disabled held items, disabling the controller, blocked drops, and double interaction.
- Added idempotent `SetupPlayerCarry` and carry checks in `ValidatePlayerSetup`.
- Added six EditMode rule tests and nine PlayMode carry tests.
- No scene, item prefab, material, package, or project-setting change.

### Checks and evidence

- Unity compile exited `0`; final log clean (`TestResults/p3-003-compile-3.log`, ignored).
- EditMode: 21/21 passed (`TestResults/p3-003-editmode.xml`, ignored).
- PlayMode: 27/27 passed without `-nographics` (`TestResults/p3-003-playmode-2.xml`, ignored).
- Player validation passed (`TestResults/p3-003-player-validate.log`, ignored).
- First PlayMode run logged a kinematic-velocity warning; fixed by zeroing velocity before switching the body to kinematic, then reran compile and PlayMode clean.
- Bootstrap validation passed; JSON parsed; `git diff --check` passed.
- Report: `.orchestrator/reports/2026-10-30-ll-p3-003.md`.

### Not performed

- Item prefabs, scene placement, and template-ID asset validation (`LL-P3-004`).
- Prompt UI (`LL-P3-005`) and machine load/unload (`LL-P3-006`).
- Direct scene-unload-while-holding automation; human carrying review, playtest, player build, and performance measurement.

### Next

`LL-P3-004` - add basket, loose-laundry, and bag placeholder prefabs with a small additive scene placement. Do not start `LL-P3-005` until `LL-P3-004` validates.

## 2026-10-31 - LL-P3-004 placeholder laundry item prefabs

**Phase:** 3 - Laundry interaction foundation
**Task:** LL-P3-004
**Status:** Complete - build, item validation, navigation validation, compile, EditMode, PlayMode, and log scan passed

### Implemented / performed

- Added `Phase3SetupTool` with idempotent `BuildItems` and `ValidateItems` editor entry points.
- Added `Basket.prefab`, `LooseLaundry.prefab`, and `Bag.prefab` under `Prefabs/Gameplay/Laundry/` with unique template IDs, colliders, rigidbodies, and existing greybox materials.
- Added four item instances (one basket, one bag, two loose-laundry) under a new additive `Phase3Items` root in `Laundromat.unity`.
- Validation checks template-ID uniqueness, kind, washer-loadability, colliders, materials, scene instance counts, and clearance from all seven public routes.
- Updated `ASSET_INVENTORY.md` with `GAMEPLAY-LAUNDRY-001` and the carry-capable player description.
- No runtime script, material, package, or project-setting change.

### Checks and evidence

- `Phase3SetupTool.BuildItems` exited `0` (`TestResults/p3-004-build-items.log`, ignored).
- `Phase3SetupTool.ValidateItems` exited `0` (`TestResults/p3-004-validate-items.log`, ignored).
- `Phase2SetupTool.ValidatePublicNavigation` exited `0` (`TestResults/p3-004-navigation.log`, ignored).
- Unity compile exited `0` (`TestResults/p3-004-compile-1.log`, ignored).
- EditMode: 21/21 passed (`TestResults/p3-004-editmode.xml`, ignored).
- PlayMode: 27/27 passed (`TestResults/p3-004-playmode.xml`, ignored).
- All six logs were scanned for errors, exceptions, unsupported physics writes, shader errors, and aborts; clean.
- Bootstrap validation passed; JSON parsed; `git diff --check` passed.
- Report: `.orchestrator/reports/2026-10-31-ll-p3-004.md`.

### Not performed

- Prompt UI (`LL-P3-005`) and machine load/unload (`LL-P3-006`).
- Human visual review of the item placeholders, playtest, player build, and performance measurement.

### Next

`LL-P3-005` - add the interaction prompt UI and controls hint. Do not start `LL-P3-006` until the prompt tests pass.

## 2026-10-31 - LL-P3-005 interaction prompt UI

**Phase:** 3 - Laundry interaction foundation
**Task:** LL-P3-005
**Status:** Complete - compile, prompt setup, player validation, EditMode, PlayMode, and log scan passed

### Implemented / performed

- Added `InteractionPromptView` as an IMGUI placeholder in `Scripts/UI/`.
- Exposes `IsVisible` and `CurrentLabel`; `OnGUI` renders only from that state using the built-in font.
- Subscribes to `PlayerInteractor.TargetChanged` and refreshes each frame to catch pause, eligibility, and destroyed-target changes.
- Combines the interactable prompt with `PlayerInteractor.InteractKeyLabel`; no key literal is hard-coded in the view.
- Hides when there is no target, the target is ineligible or destroyed, or `Time.timeScale == 0`.
- Does not read input or change cursor lock, so it cannot steal focus from `PauseController`.
- Added idempotent `SetupPlayerPrompt`, prompt wiring checks in `ValidatePlayerSetup`, and four PlayMode tests.
- No scene, item prefab, package, or project-setting change.

### Checks and evidence

- Unity compile exited `0` (`TestResults/p3-005-compile-2.log`, ignored).
- Prompt setup and Player validation exited `0` (`TestResults/p3-005-prompt-setup.log`, `p3-005-player-validate.log`, ignored).
- EditMode: 21/21 passed (`TestResults/p3-005-editmode-2.xml`, ignored).
- PlayMode: 31/31 passed (`TestResults/p3-005-playmode-2.xml`, ignored).
- The first PlayMode run failed 5 tests with `MissingReferenceException` because a copied `IsAlive` helper used a tautological Unity-null check; the view now uses `ReferenceEquals(...) || unityObject != null`, and the full suite was rerun clean.
- Log scan clean across the final compile, EditMode, and PlayMode logs.
- Report: `.orchestrator/reports/2026-10-31-ll-p3-005.md`.

### Not performed

- Machine load/unload (`LL-P3-006`).
- Human Game-view review of prompt placement/readability, playtest, player build, and performance measurement.
- Direct cursor-lock assertion; the view does not call cursor APIs, and batch mode skips them.

### Next

`LL-P3-006` - add one machine load/unload interaction hook. Do not start `LL-P3-007` until the machine tests pass.

## 2026-10-31 - LL-P3-006 machine load/unload hook

**Phase:** 3 - Laundry interaction foundation
**Task:** LL-P3-006
**Status:** Complete - machine build/validation, item validation, navigation, compile, EditMode, PlayMode, and log scan passed

### Implemented / performed

- Added `MachineLoadHook` with `TryLoad`/`TryUnload`, `LoadedItem`, `IsLoaded`, `LastResult`, and a load/take prompt.
- Added read-only `CarrySlot.AcceptsKind` for validation and tests.
- Added `Phase3SetupTool.BuildMachineHook` and `ValidateMachineHook`.
- Added one additive `DrumSlot` child and `MachineLoadHook` to `Machine_1` in `GreyboxRoom.prefab`; no machine position, scale, collider, or hierarchy node was removed or moved.
- Loading accepts one loose-laundry item and rejects baskets, bags, an occupied machine, and empty hands without changing ownership.
- Unloading returns the exact stored instance to an empty-handed carrier and rejects full hands.
- Added five EditMode rule tests and four PlayMode load/unload/targeting tests.
- Recorded the pre-edit checkpoint `cc6821a` before changing the room prefab.

### Checks and evidence

- `BuildMachineHook` and `ValidateMachineHook` exited `0` (`TestResults/p3-006-build-machine.log`, `p3-006-validate-machine.log`, ignored).
- `ValidateItems` and `ValidatePublicNavigation` exited `0` (`TestResults/p3-006-validate-items.log`, `p3-006-navigation.log`, ignored).
- Unity compile exited `0` (`TestResults/p3-006-compile-1.log`, ignored).
- EditMode: 26/26 passed (`TestResults/p3-006-editmode.xml`, ignored).
- PlayMode: 35/35 passed (`TestResults/p3-006-playmode.xml`, ignored).
- Log scan clean; the only `Exception` matches in the PlayMode log are test-framework method names containing `Exceptions`.
- Report: `.orchestrator/reports/2026-10-31-ll-p3-006.md`.

### Not performed

- Machine cycles, timers, water/soap, payment, and the other seven machines.
- Human Game-view review of machine interaction, playtest, player build, and performance measurement.

### Next

`LL-P3-007` - run Phase 3 acceptance verification and capture evidence. Do not start Phase 4 until acceptance is recorded.

## 2026-10-31 - LL-P3-008 item pickup repair

**Phase:** 3 - Laundry interaction foundation
**Task:** LL-P3-008 (plan repair after acceptance review)
**Status:** Complete - pickup setup, item validation, compile, EditMode, PlayMode, and log scan passed

### Implemented / performed

- Found during acceptance review: carry logic existed but no interaction target could pick up a scene item.
- Added `LaundryItemInteractable` with kind-specific prompts, free-item eligibility, held/slotted guards, and delegation to `CarryController.TryPickUp`.
- Added the component to all three item prefabs through idempotent `Phase3SetupTool.SetupItemPickup`; `BuildItems` now includes it.
- Extended `ValidateItems` to require exactly one wired pickup component per prefab and scene instance.
- Corrected `PlayerInteractor.IsAlive` to use `ReferenceEquals` so destroyed interactables are rejected.
- Added five EditMode pickup tests and one PlayMode scene-pickup test.
- No scene, player prefab, package, or project-setting change.

### Checks and evidence

- Pickup setup and `ValidateItems` exited `0` (`TestResults/p3-008-item-pickup.log`, `p3-008-validate-items.log`, ignored).
- Unity compile exited `0` (`TestResults/p3-008-compile-2.log`, ignored).
- EditMode: 31/31 passed (`TestResults/p3-008-editmode.xml`, ignored).
- PlayMode: 36/36 passed (`TestResults/p3-008-playmode-2.xml`, ignored).
- Log scan clean across final logs.
- Report: `.orchestrator/reports/2026-10-31-ll-p3-008.md`.

### Not performed

- Captures, Phase 3 acceptance verification, and human review (`LL-P3-007`).

### Next

`LL-P3-007` - run the full acceptance verification and request the owner review.

## 2026-10-31 - LL-P3-009 drop input and hint

**Phase:** 3 - Laundry interaction foundation
**Task:** LL-P3-009 (owner-review repair)
**Status:** Complete - setup, validation, compile, EditMode, PlayMode, and log scan passed

### Implemented / performed

- Owner review found that a carried basket could not be put down; pickup worked, but no input called `TryDrop`.
- Added a serialized `KeyCode.Q` drop key to `CarryController`, ignored while paused, with `DropKey`/`DropKeyLabel`.
- Added an IMGUI `[Q] Drop` hint to `InteractionPromptView`; it appears only while carrying and hides when paused or empty-handed.
- Wired the prompt view's carry reference and serialized the Q key in Player setup; validation checks both.
- Added an EditMode drop-key test and a PlayMode drop-hint/pause test.
- No scene, item prefab, environment prefab, package, or project-setting change.

### Checks and evidence

- Compile exited `0` (`TestResults/p3-009-compile-3.log`, ignored).
- Carry/prompt setup and Player validation exited `0` (`TestResults/p3-009-setup-*.log`, `p3-009-player-validate.log`, ignored).
- EditMode: 32/32 passed (`TestResults/p3-009-editmode.xml`, ignored).
- PlayMode: 37 passed, 1 explicit capture test skipped (`TestResults/p3-009-playmode.xml`, ignored).
- Log scan clean.
- The first compile attempt failed because the project was open in the owner's Unity editor; after it closed, setup and tests ran normally. A missing `using` in the new EditMode test was fixed before the passing run.
- Report: `.orchestrator/reports/2026-10-31-ll-p3-009.md`.

### Not performed

- Owner retest of the Q drop key and hint.
- New screenshots showing the drop hint; the existing capture test can regenerate them.
- Phase 3 acceptance (`LL-P3-007`).

### Next

`LL-P3-007` - owner retest of drop, then acceptance evidence and report.

## 2026-10-31 - LL-P3-007 Phase 3 acceptance

**Phase:** 3 - Laundry interaction foundation
**Task:** LL-P3-007
**Status:** Complete - automated verification and owner-reported review passed

### Implemented / performed

- Ran bootstrap, compile, item/machine/navigation/player validators, EditMode, PlayMode, and log scan.
- Captured prompt, carry (with `[Q] Drop`), and machine evidence in a non-batch PlayMode run.
- Recorded the owner's first review finding: the basket could be picked up but not put down.
- `LL-P3-009` added the `Q` drop key and hint; the owner reopened the project and reported **Pass**.
- Recorded the owner-reported pass for pickup, carrying, drop, prompt clarity, machine load/unload, and non-blocking minor cosmetic clipping.
- Phase 3 core acceptance is complete; the approved asset extension continues in `LL-P3-010`/`011`.

### Checks and evidence

- Compile and four validators exited `0` (`TestResults/p3-007-*`, ignored).
- EditMode: 32/32 passed (`TestResults/p3-007-editmode.xml`, ignored); final drop run 32/32.
- PlayMode: 37 passed, 1 explicit capture test skipped (`TestResults/p3-007-playmode.xml`, ignored).
- Capture filtered run passed; `phase3-prompt.png`, `phase3-carry.png`, `phase3-machine.png` inspected.
- Owner evidence: `.orchestrator/reports/2026-10-31-ll-p3-007-human-verification.md`.
- Report: `.orchestrator/reports/2026-10-31-ll-p3-007.md`.

### Not performed

- Phase 4 machine cycles, orders, economy, save, and final UI.
- Asset/NPC polish, which follows in `LL-P3-010`/`011`.

### Next

`LL-P3-010` - research and document the free hybrid Blender + CC0 asset pipeline and the first asset prototype plan.

## 2026-10-31 - LL-P3-010 free asset pipeline research

**Phase:** 3 extension - free asset/NPC polish
**Task:** LL-P3-010
**Status:** Complete - research and documentation only; no install or download

### Implemented / performed

- Added `ASSET_PIPELINE.md` with the approved hybrid Blender + CC0 + Mixamo pipeline.
- Recorded decision `0006` and the owner's approvals.
- Documented Blender 4.5.15 LTS (5.2.3 LTS fallback), official source, footprint, and GPL/artwork ownership.
- Compared Kenney, Quaternius, Poly Haven, Poly Pizza, 100 Avatars, Mixamo, Sketchfab, and Unity Asset Store terms.
- Defined scale/orientation, poly budgets, export/import settings, folder layout, NPC plan, and Phase 1-3 polish backlog.
- Selected the custom laundry basket as the first prototype and wrote a rollback path.
- Updated `ASSET_INVENTORY.md` with the pipeline policy and a research change-log row.

### Checks and evidence

- Official Blender download/LTS/license pages, Kenney support, Quaternius QAL, Poly Haven license, Poly Pizza, 100 Avatars, and Sketchfab terms reviewed.
- Mixamo's official FAQ returned 403 to automated fetch; its terms are summarized from Adobe Community citations and must be rechecked at download time.
- No Unity compile/test was required for this documentation-only task.
- Report: `.orchestrator/reports/2026-10-31-asset-pipeline-research.md`.

### Not performed

- Blender installation (`LL-P3-011`).
- Asset download/import, NPC creation, and Unity changes.
- Phase 4 gameplay.

### Next

`LL-P3-011` - install Blender 4.5 LTS, create the custom basket, import it, preserve the pickup/carry/machine contracts, and capture the result.

## 2026-10-31 - LL-P3-011 Blender install and custom basket

**Phase:** 3 extension - free asset/NPC polish
**Task:** LL-P3-011
**Status:** Complete - Blender installed, basket imported, validators and suites pass

### Implemented / performed

- Downloaded Blender 4.5.15 LTS from the official Blender release page and verified the SHA-256.
- Installed the portable Windows x64 build under the user profile; no PATH or machine setting changed.
- Added `ArtSource/` with the editable basket, generator script, export, and license notes.
- Replaced the basket primitive with a 756-triangle open basket and `MAT_Basket`; kept `laundry.basket`, collider, rigidbody, pickup, carry, and washer rules.
- Added idempotent apply/validate entry points in `BasketAssetSetupTool`.
- Captured the scene with the new basket and updated the asset inventory.

### Checks and evidence

- Blender 4.5.15 LTS verified; SHA-256 matched the official checksum.
- Basket generation exited `0` with 756 triangles; Unity apply and basket validation exited `0`.
- Item validation exited `0`; EditMode 32/32; PlayMode 37 passed, 1 explicit capture test skipped; capture run 1/1.
- Log scan clean; diff check passed.
- Report: `.orchestrator/reports/2026-10-31-ll-p3-011.md`.

### Not performed

- Loose laundry, bag, and machine visuals (`LL-P3-012`).
- NPCs (`LL-P3-013`), human visual review, player build, and performance measurement.

### Next

`LL-P3-012` - custom loose-laundry/bag meshes and washer/dryer front detail.

## 2026-10-31 - LL-P3-012 prop and machine polish

**Phase:** 3 extension - free asset/NPC polish
**Task:** LL-P3-012
**Status:** Complete - props and machine fronts applied, validators and suites pass

### Implemented / performed

- Added `create_laundry_props.py` and built a 324-triangle folded loose-laundry bundle and a 432-triangle kraft bag.
- Imported both with custom materials; preserved `laundry.loose`/`laundry.bag`, colliders, pickup, and washer rules.
- Added 24 additive front details (panel, drawer, coin slot) to all eight machines; no colliders or geometry changes.
- Added idempotent apply/validate methods to `BasketAssetSetupTool`.
- Inspected the refreshed prompt and machine captures.

### Checks and evidence

- Prop and machine-front validation exited `0`; item, machine-hook, and navigation validation exited `0`.
- EditMode 32/32; PlayMode 37 passed, 1 explicit capture test skipped; capture run 1/1.
- Log scan clean; diff check passed.
- Report: `.orchestrator/reports/2026-10-31-ll-p3-012.md`.

### Not performed

- NPCs (`LL-P3-013`), human visual review, player build, performance measurement.

### Next

`LL-P3-013` - three CC0 NPC archetypes with idle/walk/carry clips.

## 2026-10-31 - LL-P3-013 CC0 NPC archetypes

**Phase:** 3 extension - free asset/NPC polish
**Task:** LL-P3-013
**Status:** Complete - three prefabs, validation, suites, and preview capture passed

### Implemented / performed

- Downloaded the CC0 100 Avatars R2 release and used BizDude, SunflowerPerson, and CosmicPerson.
- Imported three humanoid avatars at 1.5-1.8 m with a 14x global scale, capsule colliders, and texture materials.
- Added `NpcAssetSetupTool` apply/pose/capture/validate utilities.
- Applied a relaxed static arm pose; no usable idle/walk/carry clips exist in the FBX files, so animation is deferred.
- Inspected `TestResults/npc-archetypes.png`.
- Recorded the CC0 license in `ArtSource/Licenses/100-avatars.md`.

### Checks and evidence

- NPC apply and validation exited `0`; humanoid avatars, heights, colliders, and materials verified.
- EditMode 32/32; PlayMode 37 passed, 1 explicit capture test skipped.
- Preview capture inspected; import warned about one self-intersecting Customer polygon.
- Report: `.orchestrator/reports/2026-10-31-ll-p3-013.md`.

### Not performed

- Animation playback, NPC behavior, scene placement, player build, performance measurement.
- Human review of the NPC style.

### Next

Owner review of the NPC capture, then Phase 4 planning. Phase 3 is complete.

## 2026-10-31 - LL-P3-014 to LL-P3-016 final polish

**Phase:** 3 extension - final visual polish
**Tasks:** LL-P3-014 materials/lighting; LL-P3-015 signage/props; LL-P3-016 NPC polish
**Status:** Complete - eight validators, suites, captures, and log scan passed

### Implemented / performed

- Applied CC0 ambientCG tile, painted-plaster, and wood materials to floor, walls/ceiling, and counters; warmed four room lights.
- Added OPEN/HOURS signs, flipped existing storefront text for inside readability, and added three detergent bottles, a plant, and a cart.
- Normalized NPC heights to 1.61-1.78 m, added role tints, and re-applied a relaxed pose (partial due to source rig).
- Captured entrance, waiting area, machine wall, and NPCs for review.

### Checks and evidence

- Environment, signage/prop, item, machine-hook, public-zone, navigation, player, and NPC validators exited `0`.
- EditMode 32/32; PlayMode 37 passed, 1 explicit capture test skipped.
- Log scan clean; captures inspected.
- Report: `.orchestrator/reports/2026-10-31-ll-p3-014-016-polish.md`.

### Not performed

- Full NPC animation/pose retargeting, human review, player build, performance measurement, Phase 4 gameplay.

### Next

Owner review of the final captures, then Phase 4 planning.

## 2026-11-01 — Back room, cabinet fight game, NPC carry and permissions

### Changed

- Built the back room behind the concealed shelf: a 10.0 x 12.9 m shell (half the measured shop footprint) with floor, four walls, ceiling and light, parented under `RoomContent` so it only appears once usable.
- `HiddenRoomPurchasePoint`: paying $200 clears the shelf, reveals the room and prepares it in one step; the clue/order path still works for players who find it themselves.
- The arcade cabinet now lives inside that room, ships already powered, and the player-facing install point was removed.
- Cabinet game replaced with a versus fighter behind a PLAY/QUIT menu: HONDA vs BISON, health bars, round clock, knockout, five-second result hold, then back to the menu. QUIT closes the cabinet and returns the player to the shop. Original pixel font drawn procedurally, so no font asset.
- Map on **M** shows the shop and, once revealed, the back room with the arcade marked.
- NPC carry: `CarrySocket` no longer falls back to the NPC root (the laundry used to sit at the feet) and `CarryPose` now matches MakeHuman bone names as well as Mixamo, so arms fold around the load; a reach-in motion plays when a carry starts.
- Reverted the CharacterController experiment twice: it stalled customers in the doorway and, in its overlap-push form, blocked the customer loop again. The push-out remains in code behind an off-by-default flag.
- Permissions: push and pip installs allowed as approved, but force/tag/delete/mirror pushes denied; `.git/**` edits denied; config, guardrails and `AGENTS.md` edits require approval; remote/merge/pull/clone ask; key and licence files ask.

### Checks and evidence

- EditMode 145/145; PlayMode 53 passed, 1 skipped.
- Player build: `[BUILD] Succeeded ... scenes: Bootstrap, MainMenu, Laundromat`.
- Cabinet screens captured: `TestResults/arcade-menu.png`, `arcade-fight.png` (PLAY/QUIT and HONDA vs BISON verified by eye).

### Not performed

- Owner playtest of the $200 back room and the cabinet in a build.
- NPCs still visually intersect furniture; the fix needs navigation, not colliders.
- Audio not listened to.

### Next

Owner playtest, then Phase 9.

## 2026-11-01 — Tooling, repository size and cleanup

### Changed

- Research report written for tools and assets (`.orchestrator/reports/2026-11-01-tools-and-assets-research.md`): six tools totalling 456 MB recommended, no 4K "blockbuster" target on a 4 GB GPU (2K textures with 4K only on the floor), Sketchfab has no CC0 arcade/laundromat models, GameTextures' free tier is non-commercial.
- Duplicate asset copies removed: 49 tracked files (byte-identical MD5 to a surviving `Assets` copy) plus untracked duplicates under `ArtSource/Downloads`.
- History rewritten with `git-filter-repo` to drop those paths. **Measured outcome: the pack did not shrink (582 MiB before and after)** because Git already stores identical content once. The duplicates were wasting disk, not repository space. The real weight is ~340 MB of unique 4K PBR maps, music, and animation FBX.
- Backup bundle kept at `Desktop/LaundryLegends-backup-20261101-1837.bundle` (582 MB).
- FFmpeg 9.0.2 installed via winget. Audacity, Krita and LMMS installers downloaded to `Downloads` for manual installation; the automated routes failed on a stuck Windows Installer transaction (error 1618) and a truncated KDE download.
- Cleanup: 52 items removed, 1,073 MB freed (failed portable extracts, duplicate source packs, stray test scenes).

### Checks and evidence

- EditMode 145/145; PlayMode 53 passed, 1 skipped.
- Build `[BUILD] Succeeded ... scenes: Bootstrap, MainMenu, Laundromat`.
- `phase-8` pushed to `github.com/kieronpeters/LaundryLegendsDeep`.

### Not performed

- Audacity/Krita/LMMS installation (owner installing from Downloads).
- Playtest of the back room and cabinet.
- Texture downscale to 2K, the change that would actually shrink the repository.



### Changed

- Arcade business loop: install-point purchase through `EconomyService` (single debit), Off/Installing/Active lifecycle, attract mode (emission pulse + generated cue), visitor traffic with exactly-once income, and a serializable `ArcadeState`.
- Fixed the shipped menu: `Button.onClick.AddListener` in the editor tool produced an empty serialized call list, so START GAME and EXIT did nothing in a build. Switched to persistent listeners, added the missing `AudioListener`, and removed a duplicate `MainMenuController` root plus the Enter shortcut that double-fired with UI submit.
- Restored the two grey NPC variants by rebuilding their materials from the existing MakeHuman textures; retired the old skin-tinting pass that stripped them. Normal maps are now imported as normal maps.
- Mounted the first-person hands using measured mesh bounds and made them visible only while carrying.
- `ArcadeCabinet.Stop()` now clears the maze view; arcade income records state before crediting and rolls back on rejection.
- Build tool now builds every enabled scene instead of Laundromat alone, so the player always starts at the menu.

### Checks and evidence

- EditMode 135/135; PlayMode 53 passed, 1 skipped.
- Player build exit 0; the built player starts at the menu and a single mouse click on START GAME clears it.
- Scene GUID check: `ArcadeService`, `ArcadeAttractMode`, `ArcadeTraffic`, `ArcadeInstallPoint` each present once in `Laundromat.unity`.
- Report: `.orchestrator/reports/2026-11-01-phase8-arcade-business-and-menu-fix.md`.

### Not performed

- Owner playtest of the first-machine payoff (phase acceptance criterion).
- Listening check of menu music and attract cue.

### Next

Owner verification, then Phase 9 arcade expansion.

## 2026-11-01 - Phase 8 playtest blocker fix

**Phase:** 8 - First arcade machine
**Run:** `2026-11-01-ll-p8-fix-01`
**Status:** Complete; owner playtest outstanding

### Fixed

- `CustomerAgent` always ends in `Done`: the order body moved to `RunOrder()`, every exit path goes through `LeaveAndFinish()`, and each walk leg has a 40 s timeout, so a blocked path can no longer freeze the shop. Ticket create/accept/collect/payment failures and abandonments now log instead of failing silently. Patience 90 s -> 180 s, tip window 60 s -> 120 s.
- `CustomerSpawner` gained a watchdog: a missing, disabled, or `Done` active customer is cleared and the respawn timer re-armed. Spawn failures back off instead of retrying every frame. Spawn and next-customer events are logged.
- Root cause of the second order abandoning: the decorative ambient street customer dropped its laundry prop into the **real order drop-off**, so the staff washed the decoration ahead of the real order (the drop-off was occupied from t=15 s to t=84 s in the trace). The scene link is cleared, `NpcSceneSetupTool` no longer wires it, and `AmbientNpc` documents the rule.
- `HiddenRoomPurchasePoint` moved from the thin service panel to the `SecretAccess` group root, so every concealment collider resolves to it. `_sealedDoorCover` is the same group, so paying clears the whole doorway instead of leaving the shelf in it.
- `OrderTicketView` self-wires a missing spawner and its box grew from 260x110 to 300x290 (the scene value clipped the ticket text).

### Added

- `PlaytestFixTool` (`Repair`/`Validate`) and `PlaytestAuditTool` (read-only wiring audit). Validate now also rejects ambient NPCs wired to the order drop-off.
- `TwoOrderPaymentTests` (EditMode): two orders credit twice; a duplicate attempt on the first does not block the second.
- `TwoOrderPlayModeTests`: two real customers through the real staff/machines with production durations and a 1/60 s delta.
- `PlaytestHudTests`: HUD ticket and prompt live/wired; ticket tracks the live ledger.
- `ShelfPurchaseTests`: aisle ray resolves to the purchase point; buying clears the doorway.

### Checks and evidence

- EditMode **147/147 passed**, exit 0 (`TestResults/editmode.xml`).
- PlayMode **57 passed, 0 failed, 1 skipped**, exit 0 (`TestResults/playmode.xml`). The two-order test logged `ORD-0001 Paid`, `ORD-0002 Paid`, wallet 100, 2 payouts.
- `PlaytestFixTool.Repair` exit 0; `PlaytestFixTool.Validate` exit 0 with every playtest wiring check.
- No `NullReferenceException`, missing reference, compile error, or assertion-failure lines in either suite log.
- Player build exit 0: `Build/LaundryLegends.exe`, 196 MB, scenes Bootstrap/MainMenu/Laundromat.
- Report: `.orchestrator/reports/2026-11-01-phase8-playtest-blockers-fix.md`.

### Not performed

- Owner playtest of the rebuilt build.
- Manual shelf purchase and back-room entry in the built player.
- Appliance models and character eyes/teeth/clothing re-export (out of scope by instruction).

### Next

Owner playtest, then the deferred arcade-payoff verification and Phase 9 planning.

## 2026-11-02 - Playtest fixes, hidden room, and character re-export

**Phase:** 8 - First arcade machine
**Status:** code/scene/tests/build complete; owner playtest outstanding

### Fixed

- Order loop: customer counter waypoint and staff drop-off/idle points were inside the counter collider, so overlap correction pinned both NPCs. Moved them clear, routed customers through room/doorway waypoints, added a 20 s staff walk timeout, and raised the fold stand to 2 m.
- Hidden room was built in the wrong place (frontage skewed the measured shop bounds; the west-wall gap was centred on the shelf root at the origin). It is now 8x8 m at x=8.2, centred on the door, with a cut in the shop's east wall and a floor threshold.
- Cabinet was lying in the floor: the import rotation was being discarded. It now preserves the prefab root rotation, is grounded, and faces the door.
- Clothing: the MakeHuman export never masked the body under garments. Patched the plugin to apply each garment's deleteVerts mask, re-exported the customer in `female_casualsuit01` (tee + jeans) and the attendant in `male_casualsuit06`, and switched the material mapping.
- Removed the placeholder first-person hands and hid 13 clutter/placeholder objects.
- Added the controls legend to the HUD.

### Added

- Casino back room: generated black/gold carpet, bar, plant, sconces, and two wordplay posters (`tools/make-posters.ps1`).
- `PlaytestFixTool`, `VisualPolishTool`, `VisualAuditTool`, `VisualProbeTool`, `CounterProbeTool`, `ModestClothingPostprocessor` (retired after the source fix).
- `ProgressionPlayModeTests`: six production-timing orders -> $300 -> shelf -> walk into the back room.
- `tools/game-vision/game-vision.ps1`: window capture, clicks, keys, relative mouse; README.
- Subagent reports: computer-use tooling, asset/audio tooling, Unity vs Unreal, Unreal sample.
- `.orchestrator/BACKLOG.md` with self-filed tickets.

### Checks and evidence

- EditMode **147/147**; PlayMode **57 passed / 0 failed / 2 explicit skipped**.
- Progression **1/1 passed** (wallet 0 -> 300, 6 paid orders).
- Repair/Validate exit 0; build exit 0 (198 MB, 0 errors).
- 20 captures, 0 invalid frames; live build captured at the menu and in game.
- Reports: `.orchestrator/reports/2026-11-02-playtest-fixes-and-visual-pass.md` and the four subagent reports.

### Not performed

- Owner playtest of the machine payoff; audio listening checks; Unreal engine install/run.

### Next

Owner playtest, then Phase 9 planning or backlog grooming.

## 2026-11-02 - Phase 8 complete: fullscreen arcade session

**Phase:** 8 - First arcade machine
**Status:** Complete; owner verified the payoff.

### Fixed / added

- The cabinet had no collider, so it could not be interacted with. Added a BoxCollider sized from its render bounds and wired the service.
- Interacting now cuts to a fullscreen fight (Arcade Paradise flow): the player, HUD, prompt and pause are disabled, the cursor is freed, and a paid play starts the fight immediately. Esc/Q leaves; the result hold returns to the shop automatically. Added `ArcadeMode` for control restore and updated the HUD views to hide during the session.
- Updated the cabinet EditMode test and added `ArcadeCabinetInteractTests` (targeting, fight start, player freeze, restore).

### Checks

- EditMode **147/147**; PlayMode **58 passed / 0 failed / 2 skipped**; build **exit 0, 198 MB**.
- Owner playtest: cabinet interaction and fullscreen fight work; payoff confirmed.

### Next

Phase 9 planning; backlog grooming (`LL-F-006` fighter visual upgrade, `LL-B-003` doorway clearance, etc.).

## 2026-11-02 - Phase 9 planning: arcade expansion

**Phase:** 9 - Arcade expansion
**Status:** In progress; LL-P9-001 complete, LL-P9-002 ready.

### Planned

- Decision `0012` locks the contracts: `ArcadeMachineDefinition` (cost, income, attract weight, footprint, prefab), a multi-slot `ArcadeService` whose slot 0 keeps the Phase 8 API, three install anchors, arcade upgrades/decorations with income/traffic multipliers, laundry reputation (paid/tip/abandon, decay) feeding footfall, scaled income plus 120 s upkeep, a visible 6x4 m expansion bay (capacity 3 -> 5), and milestone HUD entries.
- Ten tasks recorded in `.orchestrator/tasks/phase-09.json` with scope, acceptance and checks.

### Branch

- `phase-9` created from `main` (`cf06745`) and pushed. Phase 8 is merged and tagged `phase-8-complete`.

### Next

LL-P9-002: machine catalogue + multi-slot service, keeping all Phase 8 tests green.

## 2026-11-02 - Phase 9 implementation: arcade expansion

**Phase:** 9 - Arcade expansion
**Status:** Implementation and automated checks complete; owner playtest (LL-P9-010) pending.
**Commit:** 5881945 on phase-9.

### Implemented

- ArcadeMachineDefinition catalogue (Fighter/Racer/Puzzle) and a multi-slot ArcadeService; slot 0 keeps every Phase 8 API.
- Four extra anchors: Racer and Puzzle open at start, two more inside a sealed expansion bay. Purchase/placement is spatial; each debit happens once.
- Three arcade upgrade kiosks (neon +10% income, speakers +20% traffic, carpet +10% both) with scene decor that appears on purchase.
- LaundryReputation (0-100): +4 paid, +2 tip, -2 abandon, decay 1 per 120 s; footfall 0.8-1.4 feeds traffic cadence and visit income.
- Scaled visit income and 120 s upkeep through EconomyService; unpayable upkeep is skipped and logged, never negative.
- Visible, walkable 6x4 m expansion bay; capacity 3 -> 5, with two new anchors.
- Five one-time milestones and an IMGUI arcade panel (machines, lifetime income, reputation/footfall, next milestone).
- Scene rebuilt by HiddenRoomSetupTool: five anchors, three kiosks, expansion kiosk, decor, reputation and HUD.

### Verification

- EditMode **158/158**; PlayMode **59 passed / 0 failed / 2 explicit skipped**; explicit $300 progression **1/1**.
- Windows build **198 MB / 0 errors** at Build/LaundryLegends.exe.
- Long-session EditMode test reconciles laundry income, tips, reputation, arcade income, second-machine cost, expansion cost and the transaction log.
- Not run: visual capture review, build launch smoke test, owner dual-business playtest.

### Deferred

- LL-A-003 unique Racer/Puzzle cabinet visuals (Phase 11).
- LL-A-004 placeholder upgrade decor (Phase 11).
- LL-B-008 1080p capture review of bay/kiosks/HUD (Phase 15).

### Next

LL-P9-010: owner runs Build/LaundryLegends.exe, plays the dual-business loop, and records the result.

## 2026-11-02 - Phase 10 added: business operations and order flow

**Phase:** 10 - Business operations and order flow (new; old phases 10-16 renumbered 11-17).
**Status:** Planned; contracts accepted, implementation waits on the Phase 9 owner playtest.

### Added

- Decision  013: a serializable 24-hour BusinessClock (Day 1+, skip-to-next-day), consumable definitions and one shelf per item (detergent/fragrance/plastic, capacity 10, one unit per purchase), stock consumption in wash/dry/wrap with out-of-stock blocking, five active orders, a ten-slot ready-order shelf, and a 30-day itemized rent/electricity/water bill with a bills menu.
- Balance placeholders: detergent , fragrance , plastic ; rent , electricity , water  per 30 days; 10% late fee after 3 days; 1 real second = 1 game minute.
- phase-10.json with ten tasks; LL-P10-001 complete, LL-P10-002-010 planned.
- Backlog umbrella LL-F-008; future art/audio/save/QA targets renumbered to Phases 12/13/14/16.

### Tooling

- alidate-bootstrap.ps1 now accepts main or a phase-N branch, matching the phase-branch workflow.
- Removed the stale LL-P8-010 dependency from the Phase 9 plan; validation passes.

### Next

Finish the Phase 9 owner playtest (LL-P9-010), then start LL-P10-002 once Phase 10 is the active phase.

## 2026-11-02 - Machine-wall fix and Phase 10 queue size

**Phase:** 9 follow-up / Phase 10 contract
**Status:** Fixed; build rebuilt; owner playtest pending.
**Commit:** 8d86fef.

### Fixed

- Only Machine_1/2 had MachineLoadHook; the other six appliances were decorative, and LaundryStaff hard-coded those two names.
- Wired all eight machines: 4 washers (1/3/5/7) and 4 dryers (2/4/6/8), each with a load slot and hook.
- LaundryStaff now sorts hooks and uses the first free unit of the required kind, so any machine can serve an order and the player can occupy one without stalling the staff.
- Updated the old Phase 3/4/appliance validators so they no longer assert a single-hook prototype.
- Phase 10 active-order queue raised from 5 to 10 (ready shelf remains 10).

### Verification

- Machine validation: 4 washers + 4 dryers, each with a slot.
- EditMode 158/158; PlayMode 59 passed / 0 failed / 2 explicit skipped; build 198 MB / 0 errors.
- Not run: owner walk-the-wall check in the shipped build.

### Deferred

- LL-A-005: washer/dryer realism pass, target Phase 12.

## 2026-11-03 - Phase 9 review checkpoint; Phase 10 paused

**Phase:** 9 - Arcade expansion
**Status:** Implementation and automated checks complete; owner playtest (LL-P9-010) pending. Branch clean for review.

- Re-ran the explicit $300 progression on the clean branch: 1/1 passed.
- Phase 10 implementation was started after the review request and has been moved off `phase-9` to the local `phase-10-wip` branch (commit 97897a2). It is incomplete: no scene wiring, UI, LaundryStaff rack handling, or tests yet. Do not review it as Phase 9.
- `phase-9` remains at the reviewed implementation + machine-wall fix; no Phase 10 gameplay code is present.

## 2026-11-03 - Review round 2 fixes

**Phase:** 9 - review follow-up
**Status:** Implemented and built; owner walkthrough pending.

- U purchase menu replaces the ground pedestals; carpet gated to the expansion and moved into the bay; posters moved/added; secret-door leaf hidden; parcel bundle; rotating drums; hidden-room visitor removed, male order customer, silent staff.
- Machines: 4 washers/4 dryers valid; drums assigned.
- EditMode 159/159; PlayMode 59 passed / 0 failed / 2 explicit skipped; build 199 MB / 0 errors.
- Follow-ups: LL-B-011 (menu bay labels), LL-A-006 (cabinet side art), LL-F-010 (different games per machine), LL-F-009 (male-customer voice).

## 2026-11-03 - Phase 9 owner approval and closeout

**Phase:** 9 - Arcade expansion
**Status:** Complete. Owner approved the round 2 fullscreen build on 2026-11-03.

- Phase 9 acceptance satisfied: 5 arcade slots, reputation-fed traffic/income, walkable expansion bay, combined economy, owner-verified dual-business loop.
- Evidence: EditMode 159/159, PlayMode 59 passed / 0 failed / 2 explicit skipped, machine wall 4 washers/4 dryers, build 199 MB / 0 errors.
- Known follow-ups ticketed: LL-B-009, LL-B-010, LL-A-005, LL-A-006, LL-F-009, LL-F-010, LL-B-011, LL-B-008.
- Branch `phase-9`; tag `phase-9-complete`; merge request to `main` opened.
- Phase 10 (business operations) is next; partial work remains on local `phase-10-wip`.

## 2026-11-03 - Post-approval playtest tickets

**Phase:** 9 post-approval
**Status:** Approved build unchanged; follow-ups ticketed.

- `LL-B-012`: orders stall when entering the back room (target Phase 10).
- `LL-B-013`: phone regular stands in storefront debris (target Phase 11).
- `LL-A-005`: replace the eight block appliances with real washer/dryer models (target Phase 12).
- `LL-B-014`: dryer drum clothes do not tumble (target Phase 12).
- `LL-B-015`: expansion carpet is a small rug; needs a full-bay bar/casino pattern (target Phase 12).
- Experimental audit/drum changes preserved on local `phase-9-postapproval-wip`; no code changes in the approved build.

## 2026-11-03 - Phase 9 merged; Phase 10 branch ready

- PR #2 (`phase-9` -> `main`) merged at `1978451`; `main` fast-forwarded.
- Tag `phase-9-complete` remains on `eb6f5d7`.
- `phase-10` created from `main` and pushed; phase status ready.
- Decision 0013 and phase-10 tasks are present. LL-P10-002 (business clock) is the next ready task.
- Local WIP preserved: `phase-10-wip` (97897a2) and `phase-9-postapproval-wip` (344f942), not merged.
- Post-approval tickets remain: LL-B-012 (P1), LL-B-013, LL-A-005, LL-B-014, LL-B-015.

## 2026-11-04 - Phases 10-12 combined branch

**Branch:** `phase-10-12` (from `phase-10` / `main` at `3cd96f4`)

**Phase 10 - business operations**
- Added `BusinessClock`, `ConsumableDefinition`/`ConsumableShelf`/`ConsumableStockService`, `DropOffRack`, `BillingService`, `BusinessPanelView`, `BillsPanelView` and `BusinessSetupTool`.
- Ten active orders, ten reserved rack slots, three shelves (detergent, fragrance, plastic) at 5/10 starting stock, one unit per purchase, 10 s respawn, 600 s patience.
- Stock blocks wash/dry/wrap and recovers on restock. Bills: rent 250 + electricity 90 + water 60 every 30 days, 3-day grace, one 10% late fee.
- Fixed `LaundryStaff.WaitForWork` exiting before assigning the rack slot (the queue stall), and placed the rack in world space so the counter scale no longer spreads the slots across the shop.
- `U` purchase list and `B` bills menu pause time and release the cursor; `N` skips a day.

**Phase 11 - characters and voice**
- Phone regular moved to `(6.0, 0, -4.2)` and the disabled legacy pickup component removed.
- Ambient route now ends at `Waypoint_Waiting (4.3, 0, -4.2)`; the counter waypoint is gone and each leg has a 20 s timeout.
- Staff dialogue and voice stay removed. `NPC_OrderCustomer` now speaks the male DropOff/Norman clips with subtitles.
- Audit note: the only staff voice set is female Cori, so the literal "male staff clips" request was adapted to the male DropOff set instead of copying the staff voice.

**Phase 12 - visual art**
- Generated `WasherDetailed.fbx` (13,300 tris) and `DryerDetailed.fbx` (14,144 tris) in Blender 4.5.15; replaced all eight visuals and converted 100 material slots to URP/Lit.
- Preserved the FBX 270� X root rotation and added 180� yaw; machines now stand upright with the glass door facing the room.
- Drum clothes rotate while a cycle runs; the synthetic dark drum face was removed.
- Full-bay 6x4 m casino-lattice carpet replaces the small rug; it stays gated to the expansion and carpet purchase and is parented inside `ExpansionBay`.
- Cabinet side art on both faces; Racer Riot poster in the bay; Street Folders poster off the expansion wall; post-unlock doorway verified clear.

**Evidence**
- EditMode 164/164; PlayMode 61 passed / 0 failed / 2 skipped before the new Phase 12 tests, then `Phase12SceneTests` 3/3.
- Build 200 MB / 0 errors.
- Captures: `TestResults/phase12/machine-wall-4.png`, `12-bay-entry.png`, `12-bay-carpet.png`, `12-bay-poster.png`, `12-cabinet-side.png`, `12-secret-door.png`, `12-waiting-area.png`, `menu-u.png`, `menu-b.png`.

**Still open:** authored carry/load/fold/handoff clips, `LL-F-010` per-machine arcade variety (the maze game exists but is not wired to a cabinet), and `LL-A-004` neon/speaker polish.

**Also fixed on this branch:** `LL-B-010` staff now rotates through washers and dryers, and `LL-B-011` duplicate U-menu machine rows now carry a `(#slot)` suffix.
