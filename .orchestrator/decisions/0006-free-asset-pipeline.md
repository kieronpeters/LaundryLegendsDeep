# Decision 0006 - Free hybrid asset and NPC pipeline

- **Date:** 2026-10-31
- **Status:** Approved by the project owner
- **Phase:** 3 extension (after core acceptance)
- **Related tasks:** `LL-P3-010`, `LL-P3-011`
- **Supersedes:** none

## Context

Phases 1-3 use Unity primitives and flat greybox materials. The owner wants to polish the existing assets and create NPC characters without spending money, and asked whether Blender or free asset sources should be used. The owner approved Blender installation and chose to polish Phases 1-3 before Phase 4.

## Decision

Adopt a hybrid, zero-cost pipeline:

1. Blender 4.5 LTS (or 5.2 LTS if 4.5 fails) for bespoke low-poly meshes and simple NPC bodies.
2. CC0 packs for props, textures, and character bases: Kenney, Quaternius, Poly Haven, Poly Pizza, 100 Avatars.
3. Mixamo for humanoid animations where its terms are accepted at download time.
4. Sketchfab CC0/CC-BY only for a specific missing asset, with per-model license evidence.

## Constraints

- No paid assets, subscriptions, or AI-generated assets.
- No new Unity packages or project-settings changes without separate approval.
- Every imported asset gets a source URL, author, license, download date, and attribution note in `ASSET_INVENTORY.md`.
- Raw third-party assets are not redistributed as standalone packs; Mixamo/Quaternius files stay in `ArtSource/` and are baked into prefabs for builds.
- The first prototype is a custom laundry basket that preserves the existing template ID, collider, pickup, carry, and machine contracts.
- No customer AI, queueing, orders, or payment in the asset phase.

## Consequences

- The project gains an `ArtSource/` source tree and a documented `ASSET_PIPELINE.md`.
- Blender becomes an approved local tool; its GPL does not affect project ownership of exported art.
- Asset imports must be validated with the existing suites and `Phase3SetupTool.ValidateItems` where laundry items change.
- Phase 4 starts after the asset extension is reviewed.

## Rollback

- Remove imported assets and restore placeholder prefabs from Git.
- Keep Blender installed or uninstall it manually; no Unity project setting depends on it.
- Delete `ArtSource/` only after confirming no prefab references its exports.
