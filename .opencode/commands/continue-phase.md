---
description: Continue the active phase from the persisted task queue
agent: build
---

Continue the current phase.

1. Read `PROJECT_STATE.md`, `.orchestrator/state.json`, the active phase task file, and the latest run report.
2. Inspect `git status`, the last checkpoint, and any uncommitted changes before editing.
3. If a task was interrupted, determine whether its partial output is safe to keep. Do not blindly replay it.
4. Resume the active task or select the next eligible one.
5. Follow the same implementation, validation, review, reporting, and state-update workflow as `/start-phase`.
6. Continue through eligible tasks within the configured budgets. Pause only at a mandatory gate or genuine blocker.
7. Finish with a concise status report and the next recommended task.
