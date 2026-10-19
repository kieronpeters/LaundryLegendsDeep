# Decision 0003 — Documentation and State Sources of Truth

**Date:** 2026-10-19
**Status:** Accepted
**Decision owner:** Lead agent

## Context

Multiple sessions and models must resume without hidden conversation history. The project needs clear, non-conflicting sources for current status, scope, design, architecture, and safety.

## Decision

Use the following precedence:

1. Human instruction and `.orchestrator/GUARDRAILS.md` for safety.
2. `PROJECT_STATE.md` for current facts.
3. `ROADMAP.md` for scope/sequence/acceptance.
4. `GAME_DESIGN.md` for product intent.
5. `ARCHITECTURE.md` for technical rules.
6. Task contract for the immediate assignment.

Machine-readable state lives in `.orchestrator/state.json` and `.orchestrator/tasks/phase-NN.json`. Durable narrative evidence lives in `.orchestrator/reports/`. Chronological history lives in `DEVELOPMENT_LOG.md`.

## Rules

- Update `PROJECT_STATE.md` and `state.json` together when phase/task/checkpoint status changes.
- Do not duplicate long design or architecture text into state files; link instead.
- Record material contradictions in `.orchestrator/decisions/`.
- Never let a conversation-only decision override the committed documents; write it down.
- Reports are evidence summaries, not substitutes for actual files/tests.
- A task is not complete because its checkbox says so; acceptance evidence must exist.

## Consequences

- A small amount of manual synchronization is required.
- `tools/validate-bootstrap.ps1` checks structure, JSON validity, and basic consistency, not semantic truth.
- Future automation may generate task tables from JSON, but that is not needed yet.
