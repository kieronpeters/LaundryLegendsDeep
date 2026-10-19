---
description: Stop at a safe checkpoint and produce a resumable report
agent: build
---

Stop safely.

1. Finish or explicitly pause the current atomic operation.
2. Do not start new tasks.
3. Leave the working tree in a documented state. Prefer a checkpoint commit only if the current changes are coherent and verified; otherwise record them as uncommitted work.
4. Update `.orchestrator/state.json`, `PROJECT_STATE.md`, task status, and the run report with the exact stopping point.
5. Record running processes, unrun checks, known risks, and recovery instructions.
6. Report the last checkpoint, files modified, active task, and the command needed to resume.

Never use destructive cleanup to make the tree look clean.
