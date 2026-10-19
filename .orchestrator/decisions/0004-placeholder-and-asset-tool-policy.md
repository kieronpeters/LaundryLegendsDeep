# Decision 0004 — Placeholder and Asset-Tool Policy

**Date:** 2026-10-19
**Status:** Accepted
**Decision owner:** Lead agent

## Context

No dedicated image, 3D modeling, animation, audio, or voice-generation tool is verified in this environment. Blender, FFmpeg, ImageMagick, and GitHub CLI are not on `PATH`. Python has no graphics/audio packages installed. Installing tools requires approval.

## Decision

- Build gameplay with Unity primitives, procedural materials/meshes, and simple code-generated audio first.
- Treat every such asset as a **placeholder** until deliberately replaced and verified.
- Prepare asset briefs for categories that cannot be produced with available tools.
- Use licensed external assets only with documented provenance and approval.
- Never install an asset tool or acquire paid assets without human approval.
- Record every asset in `ASSET_INVENTORY.md`; a brief is not an asset.

## Rationale

- Unblocks gameplay and integration now.
- Avoids fake asset claims and unnecessary installs.
- Keeps licensing and provenance explicit.
- Allows the project to improve visuals/audio deliberately later.

## Consequences

- Early builds will look and sound rough.
- Some visual/audio goals wait for Phase 10–12 or approved tools.
- The project must avoid pretending placeholder content is final.
- Asset production capacity is a major release risk.

## Revisit triggers

- Approval to install/use a modeling, image, audio, or voice tool.
- A budget/license decision for external assets.
- A visual/audio quality gate that placeholders cannot satisfy.
