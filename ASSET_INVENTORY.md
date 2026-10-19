# Laundry Legends — Asset Inventory

**Last updated:** 2026-10-19
**Status:** No game assets have been created, imported, generated, or integrated. This is the Phase 0 baseline.

## 1. Status definitions

| Status | Meaning |
|---|---|
| Planned | Brief or category identified; no file exists |
| Briefed | Specification exists; no asset file exists |
| Source-created | Editable source exists in `ArtSource/` |
| Imported | File exists in `Assets/` with Unity metadata |
| Integrated | Placed and functioning in a scene/prefab |
| Verified | Visually/behaviorally checked in play mode or build |
| Placeholder | Intentionally temporary |
| Final candidate | Approved for final use, still subject to QA |

A prompt, brief, or idea is **not** an asset. Do not mark anything imported or integrated without a real file and a verification note.

## 2. Current inventory

| Asset ID | File | Category | Purpose | Source / method | Location | License / restrictions | Dependencies | Integration | Verification | Placeholder? |
|---|---|---|---|---|---|---|---|---|---|---|
| — | — | — | No assets yet | — | — | — | — | — | — | — |

## 3. Planned categories

| Category | Examples | First needed | Tool/status |
|---|---|---|---|
| Environment | Room shell, floor, walls, windows, door, shelves, counter | Phase 1–2 | Unity primitives/probuilder-style placeholders; no dedicated modeling tool verified |
| Props | Baskets, laundry piles, bags, chairs, signage, detergent | Phase 2–3 | Procedural/primitive placeholders; briefs later |
| Machines | Washer, dryer, folding table, arcade cabinets | Phase 4/8 | Primitive blockouts; asset briefs |
| Characters | Player hands/body, customers | Phase 5/10 | No character tool verified; external/licensed assets need approval |
| Animations | Idle, walk, carry, load, fold, handoff | Phase 10 | No animation tool verified |
| Materials/textures | Tile, paint, wood, metal, carpet, neon | Phase 1–2/11 | Unity materials; procedural textures |
| Lighting | Sun, fluorescent, neon, emissive signage | Phase 1–2/11 | Unity Built-In RP lighting |
| UI | Prompt, HUD, ticket, menus, icons | Phase 3/13 | UGUI + TextMeshPro |
| Audio SFX | Machines, doors, coins, folding, arcade | Phase 4/8/12 | No audio tool/FFmpeg verified; synthesize or license |
| Music | Laundry ambience, arcade themes | Phase 12 | No music tool verified; license required |
| Voice | Greetings/reactions | Phase 12 | No voice tool verified; optional |
| Video | Promotional captures/trailer | Phase 16 | Browser/capture tools; no editing suite verified |

## 4. Naming and metadata

- Prefixes: `PF_` prefabs, `MAT_` materials, `T_` textures, `SFX_`/`AMB_`/`MUS_`/`VO_` audio.
- Every imported asset gets an inventory row with source, license, integration status, and verification note.
- Keep editable source in `ArtSource/`; keep runtime copies in `Assets/LaundryLegends/`.
- Record poly counts, texture sizes, and audio formats where relevant.
- Do not hand-edit Unity `.meta` GUIDs.
- Use Git LFS for large binary assets once the pipeline is agreed. LFS is installed but no patterns are configured yet.

## 5. Licensing policy

- Prefer original/procedural assets or permissive licenses (CC0, public domain, compatible open licenses).
- Record the exact source URL, author, license, and attribution requirements.
- Do not import assets with unclear licensing.
- Do not commit paid or restricted assets unless the license permits repository use.
- External asset acquisition requires human approval if it involves cost, account access, or unclear terms.

## 6. Tool reality (verified 2026-10-19)

- No Blender on `PATH`.
- No FFmpeg or ImageMagick on `PATH`.
- No GitHub CLI on `PATH`.
- No dedicated image, 3D, audio, or voice-generation tool exposed to this session.
- Unity can create primitives, procedural meshes/materials, and simple effects.
- Python 3.15 is available but Pillow, NumPy, SciPy, soundfile, and trimesh are not installed.
- Do not claim an asset exists because a tool "could" make it. Create a brief or use placeholders, and record the limitation.

## 7. Change log

| Date | Change | Evidence |
|---|---|---|
| 2026-10-19 | Inventory initialized; no assets exist | Phase 0 bootstrap |
