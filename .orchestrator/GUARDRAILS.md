# Laundry Legends — Guardrails

These rules are mandatory for every agent and session. They cannot be overridden by a subagent, a task prompt, or a desire to finish faster.

## 1. Scope

- Work only inside `C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends`.
- Do not access or modify another Laundry Legends project.
- Do not install software, change machine-wide settings, or alter external repositories without explicit human approval.
- Read `PROJECT_STATE.md` before acting. Do not trust conversation history alone.

## 2. Human approval gates

Stop and request explicit approval before:

1. Destructive filesystem or Git operations: `reset --hard`, `clean`, forced checkout/restore, forced deletion, history rewriting, branch deletion, or overwriting user work.
2. Software installation, package-manager use, or machine-wide configuration.
3. Adding, removing, or upgrading packages/dependencies (Unity packages, npm, pip, Unity modules).
4. Broad project-wide settings changes (`ProjectSettings/**`, `Packages/**`) outside an approved baseline.
5. Changes to render pipeline, input system, save format, scene architecture, or other major architecture decisions.
6. External publishing: `git push`, remote creation, store submission, uploads, social posting, release distribution.
7. Accessing another Laundry Legends project or external user data.
8. Acquiring assets with cost, account access, unclear licensing, or restricted redistribution.
9. Any operation whose consequences are not understood.

**Silence is not approval.** Routine task execution, local tests, documentation, and local commits inside a task scope do not require repeated approval.

## 3. Destructive operations

- Never use destructive Git commands to make the tree look clean.
- Never delete or overwrite an existing user file merely because it is not mentioned in the current task.
- If partial work is unsafe, preserve it in a clearly named location or report it; do not silently discard it.
- When a destructive action is approved, record the exact command, reason, and rollback/checkpoint before executing it.

## 4. Package and settings policy

- Baseline: installed Unity 6.0 LTS editor, Built-In Render Pipeline, built-in modules, UGUI, TextMeshPro, Unity Test Framework.
- No third-party runtime packages.
- Any dependency change requires a decision record, version, license, reason, and rollback plan, plus human approval.
- Batch project-settings changes into a reviewed task; do not scatter approvals.
- Do not add DI frameworks, ECS/DOTS, behavior trees, tweening libraries, networking, or analytics without a demonstrated blocker.

## 5. Secrets and privacy

- Never write API keys, tokens, passwords, cookies, license files, personal data, or credentials into the repository or reports.
- Redact secrets from logs and screenshots.
- Avoid reading `.env` files unless explicitly necessary; approval/permission rules apply.
- Do not expose machine-wide configuration values that are not needed for the task.
- Git commits must not contain credentials, local approval caches, or private logs.

## 6. Git safety

- Branch: `main`.
- Local commits are allowed for scoped, reviewed work.
- Subagents never commit or merge.
- The lead inspects `git status` and the complete diff before committing.
- Do not commit `Library/`, `Temp/`, `Logs/`, `Builds/*`, `TestResults/*`, local worktrees, or credentials.
- Do not push, create remotes, or publish without approval.
- Tag major milestones; keep checkpoints recoverable.
- If a merge conflict is unsafe, stop and document it.

## 7. Scene and prefab safety

Unity scenes and prefabs are shared serialized assets and high-risk merge surfaces.

- One owner per scene/prefab at a time.
- Parallel agents must not edit the same scene or prefab.
- Prefer prefab instances and code over repeated manual scene setup.
- Broad scene restructuring requires approval.
- Make a checkpoint before risky scene work.
- Verify the scene opens and critical references are intact after changes.
- Do not hand-edit `.meta` GUIDs.
- If a scene change cannot be verified, mark it unverified, not done.

## 8. Validation

- Run the checks required by the task.
- Fix root causes; do not weaken tests to make them pass.
- Maximum 3 repair attempts per failing check unless a human extends the budget.
- Never report a failed or unrun check as passed.
- Compilation is not gameplay verification.
- A scene opening is not a playable feature.
- A generated prompt or brief is not an asset.

## 9. Budgets and stopping

Read `.orchestrator/budgets.json` before long runs.

Stop safely when:

- A mandatory approval gate is reached.
- A genuine dependency or blocker prevents progress.
- Validation fails beyond the repair limit.
- The wall-clock, call, file, or task budget is exhausted.
- The next action would be destructive or unapproved.
- Evidence is insufficient to continue honestly.

On stop: save state, record the exact reason, leave a resumable report, and do not start new work.

## 10. Concurrency

- Use `.orchestrator/worktrees/` for isolated code work when supported.
- Assign explicit file ownership.
- Keep shared scenes, prefabs, settings, packages, and interfaces sequential.
- Integrate only after review and checks.
- Never let two agents independently merge conflicting work.

## 11. Recovery

After a crash, timeout, model change, or new session:

1. Read `PROJECT_STATE.md`, `.orchestrator/state.json`, and the latest report.
2. Inspect `git status`, the last checkpoint, and untracked files.
3. Determine the last completed task and whether partial output is safe.
4. Update state with the recovery decision.
5. Resume from the earliest safe incomplete task.
6. Do not blindly replay completed or partially completed operations.

## 12. Reporting

Every task produces the worker report defined in `AGENT_GUIDE.md` and `.orchestrator/tasks/README.md`. Include exact files, checks, evidence, unrun checks, risks, approvals, and the next task.

## 13. Authority

If instructions conflict:

1. Human instruction and these guardrails.
2. `PROJECT_STATE.md` for current facts.
3. `ROADMAP.md` for scope/sequence.
4. `GAME_DESIGN.md` for product intent.
5. `ARCHITECTURE.md` for technical rules.
6. Task contract.

Record material conflicts in `.orchestrator/decisions/`.
