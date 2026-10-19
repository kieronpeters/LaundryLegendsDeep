---
description: Queue and run eligible consecutive phases within safety and budget limits
agent: build
---

Run roadmap phases **$ARGUMENTS** consecutively.

1. Read the roadmap, state, guardrails, and budgets.
2. Validate the requested phase order and dependencies. Refuse to skip a prerequisite.
3. Execute each phase with `/start-phase` behavior, continuing through eligible tasks without routine manual approval.
4. Preserve all mandatory approval gates, validation requirements, Git review, and file-scope limits.
5. At each phase boundary, record acceptance evidence and report the outcome.
6. Stop when the requested phases are complete, a gate or blocker is reached, validation fails beyond the retry limit, or a budget expires.
7. Produce a final summary with completed phases, remaining tasks, evidence, and the next recommended phase.

Do not interpret this command as permission to bypass tests, dependencies, approvals, or safety rules.
