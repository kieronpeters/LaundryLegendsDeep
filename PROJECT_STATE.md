# Laundry Legends — Project State

**Last updated:** 2026-10-19 20:35 (UTC+08:00)
**Current milestone:** M0 — Bootstrap, **complete**
**Current phase:** Phase 0 — Project bootstrap, **complete (machine-verified; human review task pending, non-blocking)**
**Next phase:** Phase 1 — Unity foundation, **ready**
**Active task:** none
**Next recommended task:** `LL-P1-001` — Create and validate the Unity 6.0 LTS project at the repository root

## 1. Verified configuration

| Item | Value | Status |
|---|---|---|
| Unity editor | `6000.0.64f1` Unity 6.0 LTS | Installed; selected |
| Render pipeline | Built-In Render Pipeline | Decision recorded; project not created |
| Platform | Windows 64-bit | Windows standalone module installed |
| Input | Legacy Input Manager | Decision recorded |
| Primary model | `opencode/deepseek-v4.1-flash-free` | Verified in model catalog |
| OpenCode | CLI `2.0.28`, server `2.0.32` | Verified |
| Git | `2.55.0.windows.3`, branch `main` | Repository initialized; bootstrap checkpoint recorded |
| Git LFS | `3.7.1` | Available; not configured for asset patterns yet |
| .NET SDK | `10.0.401` | Verified |
| Python | `3.15.0`, no graphics/audio packages | Verified |
| Blender / FFmpeg / ImageMagick / GitHub CLI | Not found on `PATH` | Unavailable; documented |

**Unity licensing:** the entitlement file is present at `C:\Users\Asus\AppData\Local\Unity\licenses\UnityEntitlementLicense.xml`, but a batch-mode editor launch has not yet been performed. Treat Unity automation as unverified until `LL-P1-001` records a successful log.

## 2. Existing project content

- **Scenes:** none.
- **Scripts:** none.
- **Prefabs:** none.
- **Materials/textures/models/audio:** none.
- **Unity project files:** none yet. No `Assets/`, `Packages/`, or `ProjectSettings/` Unity project exists.
- **Bootstrap content:** documentation, `.opencode/` configuration, `.orchestrator/` state, validation tooling, and empty source/build/test directories.

The workspace was empty before bootstrap. No user work was overwritten.

## 3. Implemented and verified

- Git repository initialized on `main` with Unity-aware `.gitignore`.
- Required project documentation created.
- Phase roadmap covering Phases 0–16.
- Persistent orchestration state, task queue, budgets, recovery procedure, decision records, and run report.
- Project-local OpenCode model, permissions, agents, and slash commands.
- Bootstrap validation tooling.
- Environment/tool inventory recorded without secrets.
- No gameplay features, assets, or builds exist.

## 4. Incomplete and unverified

- Unity project creation, project settings, scenes, scripts, prefabs, and packages.
- Unity batch-mode licensing and command-line execution.
- Any gameplay feature.
- Any asset integration.
- Any Unity test or build.
- First-person camera/player implementation.
- Save system, UI, economy, customers, laundry machines, secret room, or arcade.
- Git LFS configuration for large future assets.

## 5. Blockers and dependencies

- **No hard blocker for Phase 1.**
- Phase 1 must verify Unity batch-mode licensing early; if activation is unavailable, manual editor confirmation is required before relying on automated Unity commands.
- Package additions and risky project-wide settings require human approval. Phase 1 should use only the installed editor's built-in modules and a minimal package manifest.
- No image, 3D, or audio generation tool is currently verified. Placeholder and procedural assets are the default.

## 6. Approval status

- Pending approvals: none for routine Phase 1 preparation.
- Human review task: `LL-P0-009` (review the bootstrap and confirm Phase 1 start). Non-blocking for preparation, but required before relying on unverified Unity automation.
- Standing approvals: none beyond routine in-repository work.
- Required before Phase 1 continues past project creation: review of the generated Unity package manifest and project settings if they introduce anything beyond the baseline built-in configuration.

## 7. Git state

- Branch: `main`
- Bootstrap checkpoint tag: `bootstrap-v0.1`
- Baseline commit: `b78218cccfeea741d3d227ffa95e679e9ba92a72` (`chore: bootstrap Laundry Legends development system`)
- Checkpoint commit: `da130ddad18b633fb9bad82a3df5107168c91dd0` (`docs: record bootstrap checkpoint and completion state`)
- Final documentation correction commit follows the checkpoint; the tag remains reachable from `HEAD`
- Working tree: clean after the final bootstrap commit
- Remote: none. No push performed or authorized.

## 8. Run budget defaults

- 25 model calls per task
- 3 repair attempts per failing check
- 120 minutes wall-clock per run
- 8 queued tasks per run
- 30 changed files per task
- $0 estimated API spend while using a free model; paid-model use requires approval

See `.orchestrator/budgets.json`.

## 9. Last successful development run

**Run:** `2026-10-19-bootstrap`
**Report:** `.orchestrator/reports/2026-10-19-bootstrap.md`
**Result:** Phase 0 bootstrap complete. Structural validation passed with exit code 0 (log: `TestResults/bootstrap-validation-2026-10-19.log`); two independent read-only reviews found pre-checkpoint status overclaims, permission/scope issues, and evidence gaps, which were corrected. Unity/gameplay checks were not run because no Unity project exists.
**Human verification pending:** review of the bootstrap docs, command behavior, and the decision to begin Phase 1 (`LL-P0-009`, non-blocking).

## 10. Recovery pointer

Read `.orchestrator/recovery/RESUME.md` and `.orchestrator/state.json`, inspect `git status` and the latest commit, then start `LL-P1-001`. Do not replay bootstrap file creation or checkpoint work. If the tree is not clean, follow the recovery procedure before editing.

## 11. Known technical debt and risks

- Unity batch-mode behavior is unknown on this machine.
- The global OpenCode config points at a local Ollama model; the project config overrides it with DeepSeek. Confirm the provider remains available in future sessions.
- No test harness exists until Phase 1.
- The design contains proposed economy numbers that have never been tuned.
- Asset production is blocked on tool availability or licensed external sources.
- The game is a long multi-session build; no claim of autonomous completion is made.
