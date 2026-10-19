# Orchestration State

This directory is the durable, machine-readable memory of the Laundry Legends development system.

## Files

| Path | Purpose |
|---|---|
| `state.json` | Current phase, active task, next task, checkpoint, blockers, budgets reference, last run |
| `budgets.json` | Run limits and paid-model policy |
| `tasks/phase-NN.json` | Task definitions, dependencies, statuses, checks, and results |
| `tasks/README.md` | Task schema and worker-report contract |
| `reports/` | Durable run and worker reports |
| `decisions/` | Architecture/design decision records |
| `recovery/RESUME.md` | Recovery procedure after interruption or model change |
| `worktrees/` | Local isolated worktrees (ignored) |

## Rules

- Update `state.json` when phase, task, checkpoint, blockers, or next action changes.
- Never mark a task complete without evidence.
- Keep task IDs stable: `LL-P<phase>-<three-digit sequence>`.
- Record failed attempts and retry counts.
- Record approvals that are pending, granted, or rejected.
- Use Markdown reports for narrative and JSON for state, not a database.
- Commit state changes with the work they describe.

## Reading order for a new session

1. `MASTER_CONTEXT.md`
2. `PROJECT_STATE.md`
3. `state.json`
4. The active phase task file
5. The latest report
6. `GUARDRAILS.md`

Then inspect `git status` and resume safely.
