# Laundry Legends — Project State

**Last updated:** 2026-10-19 21:00 (UTC+08:00)
**Current milestone:** M1 — Playable laundry, **in progress**
**Current phase:** Phase 1 — Unity foundation, **in progress**
**Next phase:** Phase 2 — Physical laundromat, planned
**Active task:** none
**Next recommended task:** `LL-P1-002` — Review and approve the baseline package manifest and project settings

## 1. Verified configuration

| Item | Value | Status |
|---|---|---|
| Unity editor | `6000.0.64f1` Unity 6.0 LTS | Project created and batch-verified |
| Render pipeline | Built-In Render Pipeline | Verified: no custom SRP asset, no URP/HDRP package |
| Platform | Windows 64-bit | Windows standalone module installed |
| Input | Legacy Input Manager (`activeInputHandler: 0`) | Verified in generated settings |
| Primary model | `opencode/deepseek-v4.1-flash-free` | Verified in model catalog |
| OpenCode | CLI `2.0.28`, server `2.0.32` | Verified |
| Git | `2.55.0.windows.3`, branch `main` | Repository initialized; bootstrap checkpoint recorded |
| Git LFS | `3.7.1` | Available; not configured for asset patterns yet |
| .NET SDK | `10.0.401` | Verified |
| Python | `3.15.0`, no graphics/audio packages | Verified |
| Blender / FFmpeg / ImageMagick / GitHub CLI | Not found on `PATH` | Unavailable; documented |

**Unity licensing:** the Unity Personal offline license validity window expired on 2026-10-19 and caused batch exit code `198`. The human refreshed the license through Unity Hub; the retry resolved entitlements and exited `0`. Unity may require another online refresh when the offline window expires.

## 2. Existing project content

- **Scenes:** none.
- **Scripts:** none.
- **Prefabs:** none.
- **Materials/textures/models/audio:** none.
- **Unity project files:** exist at the repository root. `Packages/manifest.json` (35 direct dependencies), `Packages/packages-lock.json` (38 resolved built-in packages), and 20 generated files under `ProjectSettings/`. `Assets/` exists but is empty.
- **Bootstrap content:** documentation, `.opencode/` configuration, `.orchestrator/` state, validation tooling, and empty source/build/test directories.

The workspace was empty before bootstrap. No user work was overwritten. Project creation left all bootstrap files intact.

## 3. Implemented and verified

- Git repository initialized on `main` with Unity-aware `.gitignore`.
- Required project documentation created.
- Phase roadmap covering Phases 0–16.
- Persistent orchestration state, task queue, budgets, recovery procedure, decision records, and run report.
- Project-local OpenCode model, permissions, agents, and slash commands.
- Bootstrap validation tooling.
- Environment/tool inventory recorded without secrets.
- Unity 6.0 LTS project created at the repository root with `6000.0.64f1`, Built-In Render Pipeline, and a minimal built-in package baseline plus UGUI/TextMeshPro and Unity Test Framework.
- Unity batch-mode launch and compile smoke check both exited `0` on 2026-10-19; logs under `TestResults/` (local-only).
- No gameplay features, scenes, assets, or builds exist.

## 4. Incomplete and unverified

- Scenes, scripts, prefabs, scriptable objects, assemblies, and tests.
- Baseline project-settings approval and tuning (color space, naming, quality, display mode).
- Any gameplay feature.
- Any asset integration.
- Any Unity test run or player build.
- First-person camera/player implementation.
- Save system, UI, economy, customers, laundry machines, secret room, or arcade.
- Git LFS configuration for large future assets.

## 5. Blockers and dependencies

- **No current hard blocker.** Unity batch-mode licensing was resolved on 2026-10-19 by a Unity Hub refresh.
- Unity Personal offline licensing expires periodically. If batch mode fails with exit `198`, refresh the license in Unity Hub before retrying; do not treat it as a project defect.
- Package additions and risky project-wide settings require human approval. Phase 1 uses only the installed editor's built-in modules plus the approved baseline UGUI/TextMeshPro and Test Framework packages.
- No image, 3D, or audio generation tool is currently verified. Placeholder and procedural assets are the default.

## 6. Approval status

- Pending approvals:
  - `LL-P1-002`: review the baseline manifest and project settings before tuning. Specific items: Gamma→Linear color space, company/product naming, quality preset, full-screen→windowed mode, and whether to remove the unused `com.unity.multiplayer.center` package.
  - Cleanup: delete the stray file `C:\Users\Asus\Desktop\AI` (995-byte Unity log created by an unquoted command). Project permissions blocked deletion.
- Human review task: `LL-P0-009` (review the bootstrap) remains pending and non-blocking.
- Standing approvals: none beyond routine in-repository work.

## 7. Git state

- Branch: `main`
- Bootstrap checkpoint tag: `bootstrap-v0.1` (`da130ddad18b633fb9bad82a3df5107168c91dd0`)
- Baseline commit: `b78218cccfeea741d3d227ffa95e679e9ba92a72` (`chore: bootstrap Laundry Legends development system`)
- Bootstrap close-out commit: `ab511d7d945307d55308f0e880f695ddecece7fc`
- Unity project commit: the task-scoped commit containing `Packages/`, `ProjectSettings/`, this report, and the state updates. The exact hash is recorded in the follow-up state update and `git log`.
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

**Run:** `2026-10-19-ll-p1-001`
**Report:** `.orchestrator/reports/2026-10-19-ll-p1-001.md`
**Result:** `LL-P1-001` complete. Unity project created at the repository root with `6000.0.64f1`, Built-In RP, 35 direct package dependencies (all built-in/editor-bundled), and 38 resolved packages. Batch launch and compile smoke check both exited `0`. The first launch failed with exit `198` because the Unity Personal offline license had expired; the human refreshed it through Unity Hub and the retry passed. No tests, scenes, playtest, or build were run.
**Human verification pending:** baseline manifest/settings approval (`LL-P1-002`) and cleanup of a stray Desktop log file.

## 10. Recovery pointer

Read `.orchestrator/recovery/RESUME.md` and `.orchestrator/state.json`, inspect `git status` and the latest commit, then continue with `LL-P1-002`. Do not replay `LL-P0-*` or `LL-P1-001` project creation. If the tree is not clean, follow the recovery procedure before editing.

## 11. Known technical debt and risks

- Unity Personal licensing requires periodic online refresh; batch automation can fail with exit `198` for reasons unrelated to the project.
- Generated project settings are first-open defaults: Gamma color space, Ultra quality, full-screen, and `DefaultCompany`; these conflict with the planned architecture until approved and changed.
- The unused `com.unity.multiplayer.center` editor package is present from the empty-project default.
- The global OpenCode config points at a local Ollama model; the project config overrides it with DeepSeek. Confirm the provider remains available in future sessions.
- No test harness exists yet; compile success is not gameplay verification.
- The design contains proposed economy numbers that have never been tuned.
- Asset production is blocked on tool availability or licensed external sources.
- The game is a long multi-session build; no claim of autonomous completion is made.
