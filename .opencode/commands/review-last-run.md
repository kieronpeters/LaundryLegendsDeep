---
description: Review the last development run, its evidence, and its diff
agent: build
---

Review the last development run.

1. Read the latest report in `.orchestrator/reports/` and the corresponding task entries.
2. Inspect the Git log, the checkpoint used by the run, and the current working tree.
3. Review the complete diff for scope creep, unrelated files, generated artifacts, secrets, package/settings changes, and missing tests.
4. Compare claimed results with actual evidence. Flag any unverified or overstated claims.
5. Check whether project state, task status, asset inventory, development log, and YouTube log were updated.
6. Report findings in severity order and recommend whether to accept, repair, or retry the run.

Do not edit, commit, merge, or run destructive commands.
