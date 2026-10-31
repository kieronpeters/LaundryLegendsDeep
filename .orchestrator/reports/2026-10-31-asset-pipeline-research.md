# Asset Pipeline Research - LL-P3-010

- **Phase:** 3 extension - free asset/NPC polish
- **Task:** LL-P3-010
- **Objective:** Research a zero-cost Blender + CC0 pipeline, license constraints, and the first asset prototype before installing or downloading anything.
- **Status:** success
- **Date:** 2026-10-31 (UTC+08:00)
- **Checkpoint:** `247e258`
- **Owner:** `ll-designer` role, executed by the lead

## Outcome

The approved hybrid pipeline is documented in `ASSET_PIPELINE.md` and decision `0006`. No software was installed, no asset was downloaded, and no Unity or repository asset file changed.

Recommended tool: **Blender 4.5.15 LTS** (about 344 MB, supported to July 2027), from the official Blender LTS page. Blender 5.2.3 LTS is the fallback. Blender's GPL applies to the application, not to artwork we create.

## Findings

### Free sources

| Source | License finding | Project fit |
|---|---|---|
| Blender | GPL v2+ software; created art is ours | Primary bespoke meshes |
| Kenney | All game assets CC0; no attribution | Props, UI, audio, blocky characters |
| Quaternius | QAL v1.0: commercial, no credit, no raw resale | Rigged characters and animation library |
| Poly Haven | All assets CC0 | PBR textures, HDRIs, some models |
| Poly Pizza | Per-model CC0/CC-BY; 10,700+ models | Low-poly props, 100 Avatars mirror |
| 100 Avatars (Polygonal Mind) | CC0; FBX/VRM rigged humanoids | Three NPC archetypes from one rig |
| Mixamo | Free with Adobe ID; commercial game use; no standalone redistribution; account and region restrictions | Idle/walk/carry clips |
| Sketchfab | Per-model license; CC0/CC-BY subset only | Last-resort specific asset |
| Unity Asset Store free | Per-asset standard license | Not primary |

### License traps

- "Free" is not the same as commercial-use safe; only CC0/CC-BY or an explicit free commercial license qualifies.
- Quaternius and Mixamo forbid redistributing raw assets; ship baked prefabs, not extractable folders.
- CC-BY requires a credit line; record it before import.
- Mixamo's official FAQ returned 403 to automated fetch; recheck it in a browser when the first animation is downloaded.
- Sketchfab changed ownership in 2026 and its free CC downloads are not guaranteed forever; archive the exact asset and license.
- AI 3D generators remain unapproved because their free-tier output rights are unclear.

### Technical standards

- 1 Unity unit = 1 meter; apply transforms before FBX export; scale 1.
- Props 300-1,500 tris; hero items up to 2,500; NPCs 5,000-12,000.
- Textures 512-1,024; one material per asset where practical.
- Author colliders in Unity; do not rely on generated mesh colliders for carried items.
- Use one humanoid rig for NPCs and Mixamo-compatible bone names.
- Keep `.blend` sources in `ArtSource/Blender/`, exports in `ArtSource/Exports/`, licenses in `ArtSource/Licenses/`.

### First prototype

**Custom low-poly laundry basket.** It is visible, simple, and already covered by pickup/carry/machine tests. It proves Blender, export, Unity import, collider placement, material setup, and the prefab contract with low risk. Estimate 2-4 hours. Rollback is restoring `Basket.prefab` from Git.

### Phase 1-3 polish backlog

Basket/loose/bag meshes, washer/dryer fronts and control panels, floor/wall/counter materials, signage, props, and a lighting/color pass. NPCs come after the laundry assets: three archetypes from one CC0 rig, with idle/walk/carry clips. No NPC behavior in the asset phase.

## Checks performed

- Official Blender download/LTS/license pages reviewed.
- Kenney, Quaternius, Poly Haven, Poly Labs/Poly Pizza, 100 Avatars, Mixamo, and Sketchfab terms reviewed.
- Blender version, installer size, and support window recorded.
- `ASSET_PIPELINE.md` and decision `0006` written.
- Bootstrap validation and JSON checks (recorded in the run).
- No Unity compile or test was required for this documentation-only task.

## Not performed

- No Blender install (`LL-P3-011`).
- No asset download or import.
- No NPC creation.
- No Unity or gameplay change.

## Risks and limits

- Mixamo terms could change or be region-restricted; verify at download.
- Blender 4.5 FBX export must be tested on the basket before batch asset work.
- Art time is not yet measured; the 2-4 hour basket estimate is a planning guess.
- The owner's minor cosmetic clipping note may need a hold-offset change during the asset pass.

## Recommended next task

`LL-P3-011` - install Blender 4.5 LTS, create the first custom basket, import it, preserve the pickup/carry/machine contracts, and capture the result. The remaining polish batch (materials, machines, signage, NPCs) should be split into reviewed follow-up tasks after the prototype.
