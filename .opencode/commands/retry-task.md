---
description: Retry a failed task after inspecting the failure and working tree
agent: build
---

Retry failed task **$1**.

1. Read the task definition, failure report, error details, and retry count.
2. Inspect the Git tree and any partial output before changing anything. Preserve safe work and identify what must be reverted or repaired.
3. Confirm the retry is inside the configured repair limit and file-scope budget.
4. Diagnose the root cause before retrying. Do not repeat the same failing operation blindly.
5. Apply the smallest safe fix, run the required checks, and update the task status and report.
6. If the retry limit is reached, mark the task blocked, record the evidence, and stop with a resumable summary.

Do not use destructive Git operations or overwrite user work without explicit approval.
