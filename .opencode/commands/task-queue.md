---
description: Show the persistent task queue and dependency state
agent: build
---

Show the current task queue.

Read `.orchestrator/state.json` and the task files under `.orchestrator/tasks/`.

Present a table with task ID, phase, title, status, dependencies, owner, blockers, and acceptance evidence. Clearly identify:

- Tasks ready to run now.
- Tasks blocked by unfinished dependencies.
- Tasks awaiting human approval.
- Failed tasks and their retry count.
- The next eligible task.

Do not modify the queue unless explicitly asked.
