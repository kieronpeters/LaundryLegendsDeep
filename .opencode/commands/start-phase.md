---
description: Start or resume a roadmap phase and execute eligible tasks
agent: build
---

Start roadmap phase **$1**.

1. Read `MASTER_CONTEXT.md`, `PROJECT_STATE.md`, `ROADMAP.md`, `.orchestrator/GUARDRAILS.md`, and `.orchestrator/state.json`.
2. Confirm the phase number and that all dependencies are complete. If it is complete, report that and stop.
3. Read `.orchestrator/tasks/phase-$1.json`. If it does not exist, create it from the phase specification in `ROADMAP.md` with stable `LL-P$1-###` task IDs, dependencies, file scope, acceptance criteria, and validation.
4. Select the next eligible task. Record it as active in `.orchestrator/state.json`.
5. Inspect the Git tree and establish a safe checkpoint before editing.
6. Execute the task within its scope. Use appropriate subagents for independent work, but keep shared scenes, prefabs, settings, and source files sequential.
7. Run the required checks. Record failures accurately and retry within the repair budget.
8. Review the diff, update task status, run report, project state, development log, asset inventory, and YouTube log when meaningful.
9. Continue automatically through eligible tasks. Stop only for an approval gate, genuine blocker, failed validation beyond the retry limit, or budget exhaustion.
10. At the phase boundary, verify every acceptance criterion and report the phase outcome and next recommended action.

Do not skip dependencies, fabricate checks, install tools, publish, or make unapproved package/settings changes.
