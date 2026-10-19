# Laundry Legends — OpenCode project instructions

These instructions apply to every OpenCode session in this repository.

## Mandatory reading order

Before acting:

1. Read `MASTER_CONTEXT.md`.
2. Read `PROJECT_STATE.md`.
3. Read the active phase in `ROADMAP.md` and `.orchestrator/state.json`.
4. Read the phase task file under `.orchestrator/tasks/`.
5. Read `.orchestrator/GUARDRAILS.md`.
6. For code work, read `ARCHITECTURE.md`; for feature work, read `GAME_DESIGN.md`; for verification, read `TESTING_GUIDE.md`.

The current phase and next task are in `PROJECT_STATE.md`. Do not rely on conversation history alone.

## Current boundary

Bootstrap (Phase 0) is complete. **No Unity project exists yet.** Phase 1 begins with `LL-P1-001`. Do not skip ahead to gameplay, customers, arcade, art, or audio before their phase dependencies and acceptance criteria are satisfied.

## Operating rules

- Work only inside `C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends` unless a documented, approved operation requires otherwise.
- Never access or modify another Laundry Legends project.
- Never install software, change machine-wide settings, publish externally, or change hosting without explicit human approval.
- Never use destructive Git or filesystem operations (`reset --hard`, `clean`, forced deletion, overwriting user work) without explicit human approval.
- Package and dependency changes require approval. This includes Unity packages, npm/pip packages, and Unity modules. Built-in modules already installed with the chosen editor are not new installs.
- Project-wide settings changes (`ProjectSettings/**`, `Packages/**`) are approval gates. Batch them into a reviewed change instead of prompting repeatedly.
- Keep one script, one clear responsibility. Prefer simple components, explicit references, and ScriptableObject data over frameworks.
- Preserve working systems. Do not rewrite completed work merely to match a preference.
- Use placeholder assets deliberately; never describe an asset as integrated or final unless it exists in the repository and has been verified in Unity.
- Never claim a test, build, playtest, or visual check passed unless the evidence exists. Record unrun checks as unrun.

## Phase workflow

1. Confirm dependencies and acceptance criteria.
2. Select the next eligible task and record it as active in `.orchestrator/state.json`.
3. Establish a Git checkpoint and inspect the working tree.
4. Execute within the task's file scope.
5. Run available automated checks.
6. Review the diff and integrate only compatible changes.
7. Update the run report, task status, asset inventory, development log, and project state.
8. Continue to the next eligible task unless a gate, blocker, validation failure, or budget stops the run.
9. Mark a phase complete only when every acceptance criterion has evidence.

Do not pause after routine coding tasks. Pause for the approval gates in `.orchestrator/GUARDRAILS.md`, genuine blockers, repeated validation failure, or exhausted budgets.

## Verification rules

- `compile succeeded` is not the same as `gameplay verified`.
- A Unity scene opening is not the same as a playable feature.
- A generated prompt or asset brief is not an asset.
- Record every check in the worker report: command, result, date, and evidence location.
- Use `tools/validate-bootstrap.ps1` for framework checks until Unity validation exists.
- Once the Unity project exists, use the batch-mode commands in `TESTING_GUIDE.md` and inspect logs for compile/test failures.

## Git rules

- The repository uses `main`; bootstrap tag is `bootstrap-v0.1`.
- Keep commits small and task-scoped.
- The lead agent owns commits and merges.
- Subagents must not merge into `main` or push.
- Inspect `git status`, the staged diff, and ignored files before every commit.
- Never commit generated caches, builds, credentials, or local approval state.
- Parallel code work must use isolated worktrees or separate files with explicit ownership. Integrate only after review.

## Budget defaults

Read `.orchestrator/budgets.json` before a long run. Defaults are conservative:

- 25 model calls per task
- 3 repair attempts per failing check
- 120 minutes wall-clock per run
- 8 queued tasks per run
- 30 changed files per task
- $0 estimated API spend (free model); any paid model requires approval

Stop safely when a budget is reached. Save state and produce a resumable report.

## Reporting

Every task ends with:

- Task ID and phase
- Objective
- Files created, modified, or deleted
- Assets created or integrated
- Checks performed and results
- Checks not performed
- Git checkpoint or commit
- Review outcome
- Known risks and limitations
- Approval required, if any
- Recommended next task

Use `.orchestrator/reports/` for the durable report and summarize it in the response.

## Tool reality

Verified: shell, file editing, Git, web search/fetch, browser preview, OpenCode subagents, and Unity Editor installation.

Not verified/available: dedicated image generation, 3D modeling, audio generation, voice synthesis, Blender, FFmpeg, ImageMagick, or GitHub CLI. Do not pretend these exist. Use procedural Unity primitives, code-generated placeholders, or documented asset briefs until a tool is approved and verified.

## Current next action

Start `LL-P1-001` only when asked to continue development. It must preserve all bootstrap files and verify Unity batch-mode licensing before relying on Unity automation.
