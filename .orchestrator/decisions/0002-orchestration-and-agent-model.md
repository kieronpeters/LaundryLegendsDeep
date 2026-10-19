# Decision 0002 — Orchestration and Agent Model

**Date:** 2026-10-19
**Status:** Accepted
**Decision owner:** Lead agent

## Context

The project needs a persistent, low-maintenance development system that can survive sessions, model changes, and long gaps without building a custom multi-agent framework.

OpenCode already provides:

- A primary `build` agent and built-in `explore`/`general` subagents.
- Project-local agent, command, permission, and model configuration.
- Worktrees for isolated parallel work.
- Session persistence and file-based project instructions.

## Decision

- Use the built-in **`build`** agent as the single lead/orchestrator, governed by `AGENTS.md`.
- Add four focused project subagents: `ll-unity-dev`, `ll-designer`, `ll-reviewer`, `ll-qa`.
- Persist state in Markdown + JSON under `.orchestrator/`; no database or external service.
- Use stable task IDs (`LL-P<phase>-<sequence>`) and phase task files.
- Use `.opencode/commands/` for reusable phase commands.
- Use `.orchestrator/worktrees/` for isolated parallel code work.
- Keep final commit/merge authority with the lead.
- Use the verified free model `opencode/deepseek-v4.1-flash-free` by default.

## Rationale

- Reuses OpenCode's existing capabilities instead of building a custom framework.
- Keeps orchestration understandable, inspectable, and recoverable.
- Markdown is readable by humans; JSON is precise for state and queues.
- Specialist agents are useful but not so numerous that coordination dominates.
- The built-in lead retains its base tool/system behavior rather than replacing it with a custom primary prompt.

## Consequences

- State updates are a manual discipline; the validator catches structural mistakes but not every semantic one.
- Custom subagents become available to new sessions after discovery/reload.
- Parallelism must be conservative because Unity scenes/prefabs are merge-sensitive.
- Free-model spend cannot be measured precisely; process limits are required.

## Revisit triggers

- A specialist workload that genuinely justifies another role.
- A need for automated state transitions or a database.
- Worktree or subagent capabilities changing in OpenCode.
- A paid model or external service being approved.
