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
