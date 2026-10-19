---
description: Show current project status, phase, task, blockers, and Git state
agent: build
---

Show the current project status.

Read `PROJECT_STATE.md`, `.orchestrator/state.json`, the active phase task file, the latest report in `.orchestrator/reports/`, and run `git status --short --branch`.

Report:

- Current milestone, phase, and phase status.
- Active task and next recommended task.
- Completed, failed, blocked, and approval-pending tasks.
- Verified features versus unverified features.
- Last checkpoint/commit and working-tree state.
- Known blockers, risks, unrun checks, and budget status.
- One clear recommended action.

Do not edit files or start work unless explicitly asked.
