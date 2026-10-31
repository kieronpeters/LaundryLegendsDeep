# Laundry Legends — Master Context

> **Read this before any work.** This is the durable contract for every future model, agent, and session.
> If documents conflict, use this order: `PROJECT_STATE.md` (current truth) → `ROADMAP.md` (scope/sequence) → `GAME_DESIGN.md` (product intent) → `ARCHITECTURE.md` (technical rules) → `AGENTS.md` and `.orchestrator/GUARDRAILS.md` (safety). Resolve and record genuine contradictions rather than silently choosing one.

## 1. Mission

Build **Laundry Legends** from an empty directory into a finished, polished, playable 3D PC game through a persistent, phase-based development system. The system must survive new sessions, model changes, crashes, and long gaps without losing state or repeating completed work.

The project is currently in **Phase 3 (laundry interaction foundation)**. Phase 0 bootstrap, Phase 1 foundation, and Phase 2 physical laundromat are complete. Phase 2's human acceptance is recorded as owner-reported. Phase 3's seven-task plan is approved and `LL-P3-001` (context reconciliation and interaction contracts) is complete; implementation begins with `LL-P3-002`.

## 2. The game in one paragraph

Laundry Legends is a single-player 3D laundry-business simulation. The player runs a small neighborhood laundromat: receiving orders, washing, drying, folding, returning clean laundry, and reinvesting the earnings. The defining long-term hook is a concealed back room that becomes an arcade. The player discovers it, clears and repairs it, installs the first machine, attracts customers, and grows it into a second business whose income and identity are intertwined with the laundry.

The laundry is the beginning. The secret arcade is the reason the game is remembered.

## 3. Confirmed decisions

These are locked unless a documented decision changes them:

- **Engine:** Unity 6.0 LTS, editor `6000.0.64f1` (installed and verified).
- **Render pipeline:** Built-In Render Pipeline. Revisit only with a demonstrated requirement and approval.
- **Platform:** Windows 64-bit PC first. Other platforms are out of scope until release planning.
- **Mode:** Single-player, first-person (provisional; see open decisions).
- **Scope:** Small stylized 3D game, not photoreal or open-world.
- **Starting loop:** Laundry order fulfillment. No arcade before the laundry loop is playable.
- **Central progression:** Secret room → arcade discovery → first machine → expansion.
- **Asset strategy:** Placeholder-first; improve deliberately. Never fake integration.
- **Architecture:** Simple Unity components, explicit ownership, ScriptableObject configuration, no custom framework without need.
- **Input:** Legacy Input Manager initially, to avoid adding a package before it is justified.
- **Testing:** Automated checks plus human playtesting; compilation alone is not gameplay verification.
- **Version control:** Git, `main` branch, task-scoped commits, bootstrap tag `bootstrap-v0.1`.

## 4. Core gameplay loop

The first playable loop must be satisfying before breadth is added:

1. Customer arrives with laundry.
2. Customer hands over an order or basket.
3. Player collects and carries the laundry.
4. Player loads a washing machine.
5. Laundry washes.
6. Player transfers laundry to a dryer.
7. Laundry dries.
8. Player folds the laundry.
9. Completed order is returned to the customer.
10. Customer pays.
11. Earnings fund equipment, upgrades, and progression.

The vertical slice is complete when this loop can be repeated without console commands or manual scene repair, with clear prompts, visible progress, payment, and a reason to continue.

## 5. Secret arcade progression

Long-term progression, in order:

1. Clues that something is behind the back wall or locked door.
2. Discovery triggered by laundry progress (for example completed orders or revenue threshold).
3. Access/reveal sequence: clear, repair, power, and unlock the hidden space.
4. Prepare the room and install the first arcade machine.
5. Attract customers and generate arcade income.
6. Buy additional machines, decorations, and upgrades.
7. Expand the room and connect laundry reputation/footfall to arcade traffic.
8. Unlock later milestones and a coherent dual-business endgame.

The arcade must not be a disconnected optional minigame. It should change the building, the customer flow, the economy, and the player's daily priorities.

## 6. Design principles

- Playable before polished.
- Small vertical slices before broad systems.
- One script, one clear responsibility.
- Verify dependencies before implementing.
- Preserve working systems.
- Avoid unnecessary rewrites and premature frameworks.
- Use placeholder assets, then improve them deliberately.
- Keep mechanics, visuals, audio, and progression consistent.
- Distinguish planned, implemented, tested, and verified.
- Record meaningful milestones for a possible development-series video.

## 7. Architecture rules

- Unity project lives at the repository root; bootstrap documentation stays alongside it.
- Runtime code under `Assets/LaundryLegends/Scripts/`, organized by feature.
- Static configuration in ScriptableObjects; runtime state in components/services, not static mutable globals.
- `GameState` owns money, day/time, and progression flags.
- `EconomyService` owns transactions; machines and customers request transactions instead of mutating money directly.
- `OrderStateMachine` owns an order's lifecycle.
- Machine controllers own their own timers and expose state/progress events.
- Customer agents own arrival, waiting, handoff, collection, and payment behavior.
- UI subscribes to events and never mutates domain state directly.
- Save data is a versioned DTO; do not serialize scene objects directly.
- One scene owner at a time. Parallel agents must not edit the same scene or prefab.
- No third-party package, framework, or render-pipeline change without approval.

See `ARCHITECTURE.md` for the full conventions.

## 8. Roadmap summary

| Phase | Focus | Status |
|---|---|---|
| 0 | Bootstrap, Git, docs, environment, orchestration | Complete |
| 1 | Unity foundation, scene flow, player, greybox | Complete |
| 2 | Physical laundromat and concealed access | Complete |
| 3 | Interaction and laundry handling | In progress |
| 4 | Washer, dryer, folding, order state machine | Planned |
| 5 | Customers, handoff, payment, first full loop | Planned |
| 6 | Business simulation and upgrades | Planned |
| 7 | Secret room discovery and preparation | Planned |
| 8 | First arcade machine and income | Planned |
| 9 | Arcade expansion and laundry/arcade integration | Planned |
| 10 | Characters and animation | Planned |
| 11 | Visual art and environment | Planned |
| 12 | Audio and presentation | Planned |
| 13 | UI, saving, settings, progression reliability | Planned |
| 14 | Balancing, usability, tutorial | Planned |
| 15 | QA, optimization, polish | Planned |
| 16 | Build and release preparation | Planned |

`ROADMAP.md` is authoritative for tasks, dependencies, deliverables, acceptance criteria, validation, risks, and status.

## 9. Verified environment (2026-10-19)

- OpenCode CLI `2.0.28`; background server `2.0.32`.
- Current primary model: `opencode/deepseek-v4.1-flash-free`.
- Git `2.55.0.windows.3`; Git LFS `3.7.1`.
- Unity Hub `43.5.0`.
- Unity editors installed: `6000.0.0f1`, `6000.0.64f1`, `6000.5.7f1`. Selected: `6000.0.64f1`.
- Windows standalone playback engine installed for `6000.0.64f1`. No other platform modules verified.
- .NET SDK `10.0.401`; Node `24.21.0`; npm `11.19.0`; Python `3.15.0`.
- Unity entitlement license file exists. Batch-mode compile, validation, and test runs have repeatedly exited `0`; the offline license can expire and may require a Unity Hub refresh.
- No Blender, FFmpeg, ImageMagick, or GitHub CLI on `PATH`. No dedicated image, 3D, audio, or voice-generation tool is verified.
- OpenCode tools verified: shell, read/write/edit, Git, web search/fetch, browser preview, subagents, worktrees.
- The global OpenCode config currently defaults to `ollama/qwen3.5:4b`; this project explicitly selects the verified DeepSeek model. Do not expose credentials or keys.

## 10. Agent model

- **Primary/lead:** built-in `build` agent, governed by `AGENTS.md`. Owns direction, task queue, dependencies, Git, reviews, and final reporting.
- **`ll-unity-dev`:** scoped Unity/C# implementation. No package/settings/commit authority.
- **`ll-designer`:** design, UX, economy, asset briefs, documentation. No game code.
- **`ll-reviewer`:** read-only diff, safety, architecture, and evidence review.
- **`ll-qa`:** tests and validation evidence. No runtime/scene edits.
- **Built-in `explore` / `general`:** research and independent multi-step work when useful.

Subagents receive fresh context. Every task contract must include the objective, dependencies, file ownership, expected outputs, checks, and reporting format.

## 11. Safety and approval gates

Human approval is mandatory before:

- Destructive filesystem or Git operations, including `reset --hard`, `clean`, forced deletion, or overwriting user work.
- Installing software or changing machine-wide settings.
- Adding, removing, or upgrading packages/dependencies (Unity packages, npm, pip, Unity modules).
- Broad project-wide settings or architecture changes, including render pipeline, input system, save format, or scene structure.
- External publishing, pushing to a remote, store submission, or uploading media.
- Accessing a separate Laundry Legends project or external user data.
- Any operation whose consequences are not understood.

Routine coding, documentation, local commits, tests, and phase transitions within scope do **not** require repeated approval. See `.orchestrator/GUARDRAILS.md`.

## 12. Persistent state

| Path | Purpose |
|---|---|
| `PROJECT_STATE.md` | Human-readable authoritative status |
| `.orchestrator/state.json` | Machine-readable phase/task/checkpoint state |
| `.orchestrator/tasks/phase-NN.json` | Task definitions and results for each phase |
| `.orchestrator/budgets.json` | Run limits |
| `.orchestrator/reports/` | Durable run and worker reports |
| `.orchestrator/decisions/` | Architecture/design decision records |
| `.orchestrator/recovery/RESUME.md` | Recovery procedure |
| `DEVELOPMENT_LOG.md` | Chronological history |
| `ASSET_INVENTORY.md` | Asset truth |
| `YOUTUBE_LOG.md` | Development-series moments |

## 13. Resuming and commands

Slash commands:

- `/start-phase 1`
- `/continue-phase`
- `/status`
- `/task-queue`
- `/review-last-run`
- `/retry-task LL-P1-003`
- `/resume`
- `/run-phases 1 2`
- `/stop-safe`
- `/prepare-release`

Natural-language equivalents work too: **START PHASE 1**, **CONTINUE CURRENT PHASE**, **SHOW PROJECT STATUS**, **SHOW CURRENT TASK QUEUE**, **REVIEW LAST RUN**, **RETRY FAILED TASK**, **RESUME AFTER INTERRUPTION**, **RUN NEXT ELIGIBLE PHASES**, **STOP SAFELY**, **PREPARE RELEASE CANDIDATE**.

A new session must read the state documents, inspect Git, and resume from the earliest safe incomplete task. It must not rely on hidden conversation history.

## 14. Definition of done

A task is done only when:

- Its acceptance criteria are met.
- Required automated checks ran and passed, or the failure is documented.
- Human-sensitive checks are explicitly marked as pending or passed.
- The diff was reviewed for scope and safety.
- Project state, task status, run report, and relevant logs were updated.
- No generated caches, secrets, or unrelated files were added.
- The next task is clear.

## 15. Current limitations

- Phase 2 human acceptance is owner-reported; the agent did not independently walk the Game view.
- Unity batch-mode licensing can expire; refresh it in Unity Hub before treating exit `198` as a project defect.
- Only procedural placeholder assets exist; no production art, audio, animation, or models.
- The automated suite is 32 EditMode and 37 PlayMode tests plus one explicit capture test; it does not yet cover machine cycles, orders, economy, customers, save/load, or final UI. Item prefabs, scene placement, pickup wiring, and machine-hook geometry are covered by editor validators.
- Phase 3 core acceptance is complete and owner-reviewed. A Phase 3 extension was approved for free asset/NPC polish: hybrid Blender plus CC0 packs, with `LL-P3-010` research complete and `LL-P3-011` first custom asset next before Phase 4. The pipeline and license matrix are in `ASSET_PIPELINE.md`; no Blender install or asset download has happened yet.
- No image/3D/audio authoring pipeline is verified.
- Spend telemetry is not reliable for free models; budget enforcement is conservative and process-based.
- The game cannot be built autonomously end-to-end without integration, testing, and human feedback.
