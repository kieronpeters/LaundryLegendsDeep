# Decision 0001 — Unity Version and Render Pipeline

**Date:** 2026-10-19
**Status:** Accepted
**Decision owner:** Lead agent
**Approval:** Bootstrap-scoped; changing this decision later requires human approval.

## Context

The project needs a stable Unity editor for a small stylized 3D PC game. Three editors are installed:

- `6000.0.0f1`
- `6000.0.64f1`
- `6000.5.7f1`

Unity 6.0 is the LTS line; `6000.0.64f1` is the newest installed 6.0 LTS patch. The Windows standalone playback engine is installed for it. `6000.5.7f1` is a newer tech-stream editor and is larger/newer than the project needs.

The project must choose a render pipeline before creating the Unity project.

## Decision

- Use **Unity 6.0 LTS `6000.0.64f1`**.
- Use the **Built-In Render Pipeline**.
- Target **Windows x86_64 standalone**.
- Use **legacy Input Manager** initially.
- Use built-in UGUI/TextMeshPro and Unity Test Framework; add no third-party runtime package without approval.

## Rationale

- Installed, LTS, and has the required Windows build module.
- Built-In RP is simpler, has fewer package/configuration dependencies, and is sufficient for a small stylized interior game.
- Avoids a URP/HDRP migration and shader/post-processing commitments before the core loop exists.
- Legacy input avoids a package and import migration before input needs are proven.
- Reduces unknowns and keeps the first playable loop cheap to reach.

## Alternatives considered

| Alternative | Why not now |
|---|---|
| Unity 6.5 tech stream | Less conservative; no demonstrated requirement; larger editor install |
| URP | Better lighting/post-processing ceiling, but adds pipeline/shader/asset dependencies and migration risk before the game exists |
| HDRP | Overkill for scope and performance target |
| New Input System | Useful later for rebinding/gamepad, but is a package change and not needed for the first loop |

## Consequences

- Built-In RP lighting/post-processing may become limiting in the art phase; revisit only with evidence, a migration plan, and approval.
- Regional/Windows-only assumptions must be revisited for release platforms.
- The project must not silently drift to another editor version or pipeline.

## Revisit triggers

- A demonstrated visual/performance requirement that Built-In RP cannot meet.
- A required package that only supports another pipeline.
- Input rebinding/gamepad requirements that justify the Input System package.
- A release platform requirement that changes the editor/module set.

## Verification

- Editor version and installed modules verified on 2026-10-19.
- Actual project creation and batch-mode licensing remain unverified until `LL-P1-001`.
