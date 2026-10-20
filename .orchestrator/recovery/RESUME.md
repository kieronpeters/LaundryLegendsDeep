# Resume After Interruption

Use this procedure after a crash, timeout, model change, new session, or unexpected stop.

## 1. Read, do not assume

1. `MASTER_CONTEXT.md`
2. `PROJECT_STATE.md`
3. `.orchestrator/state.json`
4. The active/next phase task file under `.orchestrator/tasks/`
5. The latest report in `.orchestrator/reports/`
6. `.orchestrator/GUARDRAILS.md`
7. `git log --oneline --decorate -10` and `git status --short --branch`

## 2. Inspect the working tree

- Identify tracked modifications, staged files, untracked files, and ignored artifacts.
- Compare the tree with the last checkpoint and the interrupted task's expected file scope.
- Identify partial writes, Unity `.meta` files created without assets, broken scene saves, or half-generated reports.
- Do not run `git reset --hard`, `git clean`, or deletion to "fix" the tree.

## 3. Decide the recovery point

| Situation | Action |
|---|---|
| Last task committed and tree clean | Resume at the next eligible task |
| Task edits present and coherent | Verify/complete the task, then commit if evidence passes |
| Task edits present but incomplete | Finish or safely repair the task; do not replay from scratch |
| Task edits unsafe or conflicting | Preserve them in a named location or report; request a decision before discarding |
| Validation was running | Check logs/processes; re-run if completion is uncertain |
| Unity is open/locked | Close it cleanly before batch mode; avoid duplicate editors on one project |
| State disagrees with Git | Prefer Git + actual files; correct state and record the discrepancy |

## 4. Verify environment if changed

If the model, provider, editor, packages, or tools changed:

- Confirm `opencode --version`, the active model, and subagent availability.
- Confirm the Unity editor path/version still matches `6000.0.64f1`.
- Re-run `tools/validate-bootstrap.ps1`.
- Record changes in `PROJECT_STATE.md` and the run report.

## 5. Update state before continuing

Set the active task, recovery decision, last checkpoint, and next action in `.orchestrator/state.json`. Record what was preserved, repaired, or discarded in a report.

## 6. Resume

Start with `/continue-phase` or the `nextTask` in `.orchestrator/state.json`. Do not hardcode a task ID or replay completed tasks. Do not skip dependencies. Re-run any check whose result was not durable.

If `nextTask` is null because a phase just closed, do not invent work. Author or request the next phase task plan, then start its first eligible task.

## 7. Stop safely if uncertain

If you cannot determine whether a scene, save file, or economy operation completed, stop. Preserve evidence and request a decision. A confused recovery is more dangerous than a pause.
