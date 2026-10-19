---
description: Recover and resume safely after a crash, timeout, model change, or new session
agent: build
---

Resume Laundry Legends after an interruption.

1. Read `MASTER_CONTEXT.md`, `PROJECT_STATE.md`, `.orchestrator/state.json`, `.orchestrator/recovery/RESUME.md`, and the latest report.
2. Inspect `git status`, the last checkpoint, `git log`, and any untracked or partially written files.
3. Determine the last completed task, the interrupted task, and whether partial output is safe to keep.
4. Verify the environment and current model/tool capabilities if they may have changed.
5. Update state to reflect the recovery decision before continuing.
6. Resume from the earliest safe incomplete task. Do not replay completed work or assume a partial operation succeeded.
7. Report what was recovered, what was discarded, and the next action.

Do not run destructive cleanup or rewrite history without explicit approval.
