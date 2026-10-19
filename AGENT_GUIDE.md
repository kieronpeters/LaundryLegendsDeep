# Laundry Legends — Agent Guide

This guide defines how the lead agent and specialist subagents work together. It is subordinate to `.orchestrator/GUARDRAILS.md` for safety and to `PROJECT_STATE.md` for current truth.

## 1. Operating model

- One primary **lead** agent: the built-in OpenCode `build` agent, governed by `AGENTS.md`.
- Specialist subagents are launched only when their work is genuinely independent or benefits from focused review.
- The lead owns task selection, dependencies, Git integration, conflict resolution, state updates, and final reporting.
- Subagents work in fresh context and must receive complete task contracts. They do not inherit conversation history.
- No subagent may merge to `main`, push, publish, or perform destructive operations.

## 2. Current agents

| Agent | Mode | Purpose | Authority |
|---|---|---|---|
| `build` (lead) | Primary | Orchestration, implementation, Git, final reporting | In-scope edits and local commits; approval gates apply |
| `explore` | Built-in subagent | Fast read-only codebase/web exploration | Read/search only |
| `general` | Built-in subagent | Independent research or multi-step analysis | No subagent launching; Git write commands denied by project config |
| `ll-unity-dev` | Subagent | Scoped Unity/C# implementation | Edits `Assets/**`; package/settings changes require human approval |
| `ll-designer` | Subagent | Design, UX, economy, asset briefs, docs | Edits Markdown and `.orchestrator/**`; no game code |
| `ll-reviewer` | Subagent | Read-only diff, safety, architecture, evidence review | No edits, commits, merges, or pushes |
| `ll-qa` | Subagent | Tests and validation evidence | Edits tests/TestResults/Markdown only |

Combine roles for small tasks. Do not create a running agent for every specialty. Add a new specialist only when the workload and interface boundaries justify it.

## 3. Permission matrix

| Action | Lead | Unity dev | Designer | Reviewer | QA |
|---|---|---|---|---|---|
| Read project files | Yes | Yes | Yes | Yes | Yes |
| Edit runtime code | Yes | Assigned files | No | No | No |
| Edit scenes/prefabs | Yes, scoped | Assigned files | No | No | No |
| Edit tests | Yes | Assigned tests | No | No | Tests only |
| Edit design/docs | Yes | No | Assigned docs | No | Assigned docs |
| Edit packages/project settings | Approval gate | No | No | No | No |
| Run local tests | Yes | Yes | No | Read-only checks | Yes |
| Commit/merge | Yes | No | No | No | No |
| Push/publish | Human approval | No | No | No | No |
| Destructive operations | Human approval | No | No | No | No |

Global permission rules in `.opencode/opencode.json` enforce the most important denials and prompts. The built-in `general`, `explore`, and `plan` agents are explicitly denied `git add`, `git commit`, `git tag`, and `git branch` writes; only the lead commits. Agent-specific prompts are a second layer, not a substitute for permissions.

## 4. Task contract

Before delegating, the lead must provide:

```markdown
## Task contract
- Task ID:
- Phase:
- Objective:
- Why it matters:
- Dependencies / prerequisites:
- Inputs to read:
- Files you own (create/modify):
- Files you must not touch:
- Expected outputs:
- Acceptance criteria:
- Checks to run:
- Checks that are out of scope:
- Budget (calls/files/minutes):
- Reporting format:
- Approval gates:
```

A task without file ownership and acceptance criteria is not ready to delegate.

## 5. Parallelism rules

Parallel work is allowed only when all of these are true:

- Tasks do not edit the same source file.
- Tasks do not edit the same Unity scene or prefab.
- Tasks do not change shared architecture, packages, project settings, or common serialized assets.
- One task does not depend on the unfinished interface of another.
- Results can be reviewed and integrated independently.

Good candidates:

- Design research and code inspection.
- Separate asset briefs for unrelated categories.
- Tests written against an already-agreed interface.
- Independent documentation updates.
- Review of a completed diff while unrelated documentation proceeds.

Bad candidates:

- Two agents editing `Laundromat.unity`.
- One agent creating an interface while another implements against a guessed version.
- Parallel changes to `Packages/manifest.json` or `ProjectSettings/**`.
- Two agents editing the same prefab or ScriptableObject.
- Parallel economy implementations with no agreed transaction contract.

When in doubt, serialize.

## 6. Worktrees and integration

- Use `.orchestrator/worktrees/` for isolated parallel code work when the tool supports it.
- Worktrees are ignored by Git and local-only.
- Each worktree is task-scoped; do not mix unrelated changes.
- The lead reviews the complete diff before integrating.
- Integration happens through a normal commit on `main` after tests and review.
- Never let a subagent independently merge conflicting work.
- If integration is unsafe, stop and document the conflict rather than forcing it.

## 7. Communication conventions

- Be explicit about what was verified, what was not, and what is blocked.
- Quote exact commands and exit codes for checks.
- Use file paths relative to the repository root.
- Distinguish `implemented`, `compiled`, `tested`, `playtested`, and `verified`.
- Do not use confident language for unrun checks.
- Surface uncertainty early; do not bury it in a summary.
- Escalate conflicting instructions, missing dependencies, or approval gates.

## 8. Approval gates

Human approval is mandatory for:

- Destructive filesystem or Git actions.
- Software installation or machine-wide settings.
- Package/dependency additions, removals, or upgrades.
- Broad project settings changes (`ProjectSettings/**`, `Packages/**`) outside an approved Phase 1 baseline.
- Render pipeline, input system, save format, or scene-architecture changes.
- Overwriting user work.
- External publishing, remote pushes, store submissions, uploads, and public releases.
- Accessing another Laundry Legends project or external user data.
- Any action whose consequences are not understood.

Routine task execution, local tests, documentation, and local commits inside scope do not require repeated approval.

## 9. Worker report template

Every task, successful or failed, must produce:

```markdown
# Worker Report — <TASK ID>

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

The durable copy goes in `.orchestrator/reports/` or the phase task entry. The final response summarizes it.

## 10. Review and merge protocol

1. Lead inspects `git status` and the complete diff.
2. Reviewer checks correctness, scope, safety, tests, and documentation.
3. Lead verifies evidence and addresses findings.
4. Lead runs the required checks again if fixes changed behavior.
5. Lead commits only the intended files.
6. Lead updates state, reports, asset inventory, development log, and YouTube log when meaningful.
7. Tag/checkpoint major milestones.
8. Never force-push or rewrite history without explicit approval.

## 11. Budget enforcement

Read `.orchestrator/budgets.json` before a run. If a limit is reached:

- Stop starting new tasks.
- Save state.
- Record the exact reason and remaining work.
- Leave a resumable report.
- Do not loop indefinitely or silently increase limits.

Estimated spend is $0 while using the free model. If a paid model is selected, the run must stop and request an approved spend budget first.

## 12. Conflict resolution

Priority order:

1. Human instruction and safety rules.
2. `PROJECT_STATE.md` for current facts.
3. `ROADMAP.md` for scope/sequence.
4. `GAME_DESIGN.md` for product intent.
5. `ARCHITECTURE.md` for technical rules.
6. Task contract.

If two documents genuinely conflict, stop, record the conflict in `.orchestrator/decisions/`, and ask for a decision if the consequence is material. Do not silently pick the convenient interpretation.
