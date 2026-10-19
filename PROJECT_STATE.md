# Laundry Legends — Project State

**Last updated:** 2026-10-19 21:37 (UTC+08:00)
**Current milestone:** M1 — Playable laundry, **in progress**
**Current phase:** Phase 1 — Unity foundation, **in progress**
**Next phase:** Phase 2 — Physical laundromat, planned
**Active task:** none
**Next recommended task:** `LL-P1-007` — Build the greybox laundromat environment and lighting

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

- **Scenes:** `Bootstrap.unity`, `MainMenu.unity`, and `Laundromat.unity`. All are registered in build settings with Bootstrap at index 0. Bootstrap contains `GameSession` + `BootstrapController`; MainMenu contains `MainMenuController`; Laundromat contains a Player prefab instance, a temporary floor, and a directional light.
- **Scripts:** runtime `GameState`, `GameSession`, `SceneRouter`, `BootstrapController`, `MainMenuController`, `FirstPersonController`, `PauseController`, `PlayerInteractor`, `IInteractable`, `SimpleInteractable`, and an assembly marker. Editor tools: `SceneSetupTool`, `PlayerSetupTool`.
- **Assemblies:** `LaundryLegends.Runtime`, `LaundryLegends.Editor`, `LaundryLegends.Tests.EditMode`, `LaundryLegends.Tests.PlayMode`. Runtime, editor, and PlayMode test assemblies compile.
- **Prefabs:** `Prefabs/Gameplay/Player.prefab` with CharacterController, first-person controller, pause controller, interaction raycast, child camera, and AudioListener.
- **Tests:** PlayMode tests for scene routing and player movement/look/pause/interaction. No EditMode tests yet.
- **Folders:** full `Assets/LaundryLegends` feature tree (33 directories) for art, audio, prefabs, scenes, scripts, settings, ScriptableObjects, UI, editor, and tests.
- **Materials/textures/models/audio:** none; placeholder primitives only.
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
- PlayMode suite: 4/4 tests passed on 2026-10-19.
- No greybox environment, production assets, save system, economy, customers, machines, UI, or build exists yet.

## 4. Incomplete and unverified

- Greybox laundromat room, machine wall, counter, folding area, entrance, and final lighting.
- Manual cursor-lock/release and subjective movement-feel check.
- EditMode tests and batch validation commands/documentation (`LL-P1-008`).
- Production art, audio, UI, animations, and models.
- Save system, economy, customers, laundry machines, orders, secret room, and arcade.
- Player build and performance validation.
- Git LFS configuration for large future assets.

## 5. Blockers and dependencies

- **No current hard blocker.** Unity batch-mode licensing was resolved on 2026-10-19 by a Unity Hub refresh.
- Unity Personal offline licensing expires periodically. If batch mode fails with exit `198`, refresh the license in Unity Hub before retrying; do not treat it as a project defect.
- Package additions and project-wide settings changes are treated as approval gates. Phase 1 uses only the installed editor baseline plus UGUI/TextMeshPro and Test Framework.
- No image, 3D, or audio generation tool is verified. Placeholder and procedural assets are the default.

## 6. Approval status

- The human granted a standing, full access approval on 2026-10-19 for execution requests. Routine in-repository work continues without prompts.
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
- Player/movement commit: recorded in the task-scoped commit and `git log`.
- Remote: none. No push performed or authorized.
- Note: `Library/`, `Logs/`, `UserSettings/`, and `TestResults/` are ignored and not committed.

## 8. Run budget defaults

- 25 model calls per task
- 3 repair attempts per failing check
- 120 minutes wall-clock per run
- 8 queued tasks per run
- 30 changed files per task
- $0 estimated API spend while using a free model; paid-model use requires approval

See `.orchestrator/budgets.json`.

## 9. Last successful development run

**Run:** `2026-10-19-ll-p1-006`
**Report:** `.orchestrator/reports/2026-10-19-ll-p1-006.md`
**Result:** `LL-P1-006` complete. Implemented and validated a first-person player prefab with CharacterController movement, mouse look, Esc pause overlay, cursor lock/release, and a camera-raycast interaction stub. Laundromat has the player plus a temporary floor and directional light. PlayMode suite passed 4/4: routing, movement/look, pause/resume, and interaction raycast. No manual cursor/feel check, playtest, or player build was performed.

## 10. Recovery pointer

Read `.orchestrator/recovery/RESUME.md` and `.orchestrator/state.json`, inspect `git status` and the latest commit, then continue with `LL-P1-007`. Do not replay `LL-P0-*` or `LL-P1-001` through `LL-P1-006`. If the tree is not clean, follow the recovery procedure before editing.

## 11. Known technical debt and risks

- Unity Personal licensing requires periodic online refresh; batch automation can fail with exit `198` for reasons unrelated to the project.
- Project settings were edited as serialized YAML before an editor settings API existed; Unity re-serialized and preserved them, but future changes should use an editor script or the Unity UI.
- Linear color space and Medium quality are active; greybox lighting must be authored and checked in Linear space.
- The pause overlay uses IMGUI as a placeholder; real UI arrives later.
- Cursor lock/release and movement feel were not manually verified; batch tests skip cursor APIs.
- `GameSession.Instance` is a static service handle; consumers should prefer explicit references where practical.
- Test coverage is small (4 PlayMode tests); EditMode tests and documented batch commands are pending.
- The stray `C:\Users\Asus\Desktop\AI` log remains pending manual deletion.
- The global OpenCode config points at a local Ollama model; the project config overrides it with DeepSeek. Confirm the provider remains available in future sessions.
- The design contains proposed economy numbers that have never been tuned.
- Asset production is blocked on tool availability or licensed external sources.
- The game is a long multi-session build; no claim of autonomous completion is made.
