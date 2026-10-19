# Task Schema and Worker Reports

Task IDs are stable: `LL-P<phase>-<three-digit sequence>`, for example `LL-P1-001`.

## Task file shape

```json
{
  "schemaVersion": 1,
  "phase": 1,
  "name": "Unity foundation",
  "status": "ready",
  "updatedAt": "2026-10-19T19:30:00+08:00",
  "tasks": [
    {
      "id": "LL-P1-001",
      "title": "Create Unity project",
      "status": "ready",
      "priority": 1,
      "owner": "build",
      "dependsOn": ["LL-P0-008"],
      "objective": "Why this task exists.",
      "scope": ["Files/directories the task may change"],
      "mustNotTouch": ["Shared/high-risk files"],
      "acceptance": ["Observable criterion"],
      "checks": [
        {
          "name": "Check name",
          "status": "pending",
          "command": "Exact command",
          "evidence": "Path or note"
        }
      ],
      "result": null,
      "retryCount": 0,
      "startedAt": null,
      "completedAt": null
    }
  ]
}
```

## Status values

`planned`, `ready`, `in_progress`, `complete`, `failed`, `blocked`, `paused`, `pending`, `approval_pending`.

## Check status values

`pending`, `passed`, `failed`, `skipped`, `blocked`, `not_run`.

## Worker report

Every task returns:

```markdown
# Worker Report — LL-P1-001

- Phase:
- Objective:
- Status: success | partial | failed | blocked
- Files created:
- Files modified:
- Files deleted:
- Assets created/integrated:
- Checks performed (command + result):
- Tests passed:
- Tests failed:
- Tests not performed:
- Git checkpoint/commit:
- Review outcome:
- Known risks/limitations:
- Approval required:
- Recommended next task:
```

Durable reports go in `.orchestrator/reports/`. A report is evidence, not a substitute for it.
