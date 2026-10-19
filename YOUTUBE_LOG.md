# Laundry Legends — Development Series Log

Potential moments for a YouTube development series. This is not a promise that footage exists. Each entry records why the moment could be interesting and what evidence/capture would be needed.

## Entry format

```markdown
## YYYY-MM-DD — Title
- Phase/task:
- Type: breakthrough | bug | decision | before/after | milestone | limitation
- What happened:
- Why it matters on camera:
- Capture needed:
- Evidence link/path:
- Used in episode:
```

## 2026-10-19 — Building the machine before the game

- **Phase/task:** Phase 0 bootstrap.
- **Type:** decision / milestone.
- **What happened:** Verified the empty workspace, installed Unity versions, OpenCode capabilities, model providers, and missing asset tools. Built a persistent documentation and orchestration system before creating a single gameplay script.
- **Why it matters on camera:** It shows the unglamorous foundation that makes a long AI-assisted project recoverable: task IDs, approval gates, Git checkpoints, honest test reporting, and a roadmap that spans laundry loops to a secret arcade.
- **Capture needed:** Screen recording of the repository tree, roadmap, task state, and validation script; a short explanation of why the game is not being rushed into Unity on day one.
- **Evidence:** `PROJECT_STATE.md`, `.orchestrator/reports/2026-10-19-bootstrap.md`.

## 2026-10-19 — Choosing the boring technical options

- **Phase/task:** Phase 0 decision records.
- **Type:** decision.
- **What happened:** Chose Unity 6.0 LTS `6000.0.64f1` and the Built-In Render Pipeline instead of a newer tech-stream editor or URP. Chose the legacy Input Manager until a package change is justified.
- **Why it matters on camera:** A concrete example of scope discipline: the best tool is the one that reduces unknowns for a small stylized PC game.
- **Capture needed:** Editor version screen, pipeline decision record, package-policy explanation.
- **Evidence:** `.orchestrator/decisions/0001-unity-version-and-render-pipeline.md`.

## Planned moment — The first complete order

- **Phase/task:** Phase 5.
- **Type:** milestone.
- **What happened:** TBD. The first time a customer arrives, hands over laundry, and pays for a completed order.
- **Why it matters on camera:** The first proof that the core loop is real, not a document.
- **Capture needed:** Continuous playthrough from arrival to payment, plus a before/after of the greybox.
- **Evidence:** TBD.

## Planned moment — The wall opens

- **Phase/task:** Phase 7.
- **Type:** milestone / reveal.
- **What happened:** TBD. The secret room is discovered and revealed for the first time.
- **Why it matters on camera:** The game's defining hook lands.
- **Capture needed:** Player-facing reveal, lighting transition, reaction, before/after room state.
- **Evidence:** TBD.

## Planned moment — First arcade machine powers on

- **Phase/task:** Phase 8.
- **Type:** milestone / payoff.
- **What happened:** TBD. A purchased machine activates and the hidden room becomes a business.
- **Why it matters on camera:** The laundry and arcade halves connect.
- **Capture needed:** Purchase, installation, power-on, attract mode, first income.
- **Evidence:** TBD.

## Planned moment — When the test lied

- **Phase/task:** Any QA phase.
- **Type:** bug / lesson.
- **What happened:** TBD. A compile-passing build with a broken gameplay loop, a save that duplicated money, or a scene that only worked in the editor.
- **Why it matters on camera:** Demonstrates why the project distinguishes compiled, tested, and verified.
- **Capture needed:** Failing evidence, diagnosis, fix, regression test.
- **Evidence:** TBD.

## Planned moment — Before and after the lighting pass

- **Phase/task:** Phase 11.
- **Type:** before/after.
- **What happened:** TBD. The laundromat and arcade gain distinct lighting identities.
- **Why it matters on camera:** Visual progression is immediately legible.
- **Capture needed:** Fixed-camera comparison shots of both rooms.
- **Evidence:** TBD.
