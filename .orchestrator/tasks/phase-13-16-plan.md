# Phases 13-16 combined plan

**Branch:** `phase-13-16` (from `main` at `5deb402`)
**Started:** 2026-11-04
**Status:** In progress

Phases 13-16 are developed on one branch but landed in reviewed slices. The
orchestrator budget (8 tasks / 120 minutes per run) and the human gates in
`GUARDRAILS.md` mean they cannot honestly be completed in a single automated
run. This file is the queue; `.orchestrator/state.json` is the live truth.

## Slice A - Phase 13 audio and presentation (no approval gate)

| ID | Task | Acceptance |
|---|---|---|
| LL-P13-001 | Audio map, bus volumes and settings contract | Decision 0014 records buses, clip list and persistence |
| LL-P13-002 | Procedural SFX library and `GameAudio` director | Every essential action has a distinct clip; no null/stacking |
| LL-P13-003 | Laundry/arcade ambience and music ducking | Loops are seamless; arcade entry changes the bed |
| LL-P13-004 | Pause-menu volume controls | Master/music/SFX sliders persist across sessions |
| LL-P13-005 | Audio tests, validation and inventory | EditMode settings test, PlayMode audio smoke, inventory rows |

## Slice B - Phase 14 save/load and reliability (APPROVAL GATE)

Save format is a major architecture decision (`GUARDRAILS` 2.5). The format is
proposed in `.orchestrator/decisions/0015-save-format.md`; implementation starts
only after the owner approves it.

| ID | Task | Acceptance |
|---|---|---|
| LL-P14-001 | Approve save schema/versioning | Owner approves decision 0015 |
| LL-P14-002 | Save DTO and file store | Round-trip of wallet/orders/clock/stock/arcade/room |
| LL-P14-003 | Capture and restore hook-up | New game -> save -> quit -> continue keeps progression |
| LL-P14-004 | Corrupt/missing save recovery | Bad file falls back safely and records diagnostics |
| LL-P14-005 | Settings/accessibility persistence | Volume and display settings survive restart |
| LL-P14-006 | UI/domain desync guards | HUD/prompt tests still green after load |

## Slice C - Phase 15 balancing and usability (needs playtest data)

| ID | Task | Acceptance |
|---|---|---|
| LL-P15-001 | Balance data pass | Pay/cost/cycle tables documented and applied |
| LL-P15-002 | Onboarding prompts | First order completable from guidance |
| LL-P15-003 | Low-cash/missed-order recovery | Documented and tested failure states |
| LL-P15-004 | Human pacing playtests | Owner records timings and fun rating |

## Slice D - Phase 16 QA, optimization and polish (release gate)

| ID | Task | Acceptance |
|---|---|---|
| LL-P16-001 | Full regression + defect log | No P0/P1 open |
| LL-P16-002 | Performance profiling | Target met on the development machine |
| LL-P16-003 | Backlog burn-down | Every P2 fixed or owner-accepted; P3 deferred in writing |
| LL-P16-004 | Accessibility and known-issues pass | Settings work; release notes drafted |
| LL-P16-005 | Release candidate | Reproducible build and smoke test |

## Run order

1. Slice A (this run if budget allows).
2. Request save-format approval, then Slice B.
3. Slice C with owner playtest data.
4. Slice D and release candidate.

## Out of scope for this branch

- New Unity packages or render-pipeline changes (approval gate).
- Unreal Engine work (`LL-F-004`/`LL-F-007`).
- External publishing.
