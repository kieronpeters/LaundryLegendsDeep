# Laundry Legends — Asset Inventory

**Last updated:** 2026-10-20
**Status:** Procedural and primitive placeholder assets exist in the Unity project. No production art, models, textures, animations, or audio have been created or imported. No external asset licenses are involved yet.

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
| SCN-FOUNDATION-001 | `Bootstrap.unity`, `MainMenu.unity`, `Laundromat.unity` | Scenes | Boot flow, main menu, and playable laundromat | Generated in-repo by `SceneSetupTool` | `Assets/LaundryLegends/Scenes/` | Original project asset; no external license | Runtime scripts, room/player prefabs | Registered in build settings; Bootstrap at index 0 | Build-settings EditMode test, PlayMode routing test, owner-reported Phase 1 walkthrough | Yes |
| ENV-ROOM-001 | `GreyboxRoom.prefab` | Environment | Walkable room shell, counter, machines, storefront, exterior backdrop, waiting area, storage shelving, utility alcove, signage, and secret-door foreshadowing | Generated in-repo by `GreyboxSetupTool` and `Phase2SetupTool` using Unity primitives and built-in font signage | `Assets/LaundryLegends/Prefabs/Environment/` | Original procedural placeholder; no external license | Unity primitives, Standard shader, eleven greybox materials, `LegacyRuntime.ttf` | Placed in `Laundromat.unity` | Phase 1 validation + owner-reported walkthrough; Phase 2 facade/public-zone validation and agent-inspected captures; Phase 2 human review pending | Yes |
| GAMEPLAY-PLAYER-001 | `Player.prefab` | Player | First-person movement, camera, pause, and interaction raycast | Generated in-repo by `PlayerSetupTool` | `Assets/LaundryLegends/Prefabs/Gameplay/` | Original procedural placeholder; no external license | CharacterController, camera, runtime player scripts | Placed in `Laundromat.unity` | Automated validation/tests and owner-reported Phase 1 movement/camera check | Yes |
| MAT-GREYBOX-001 | `Grey_Floor.mat`, `Grey_Wall.mat`, `Grey_Washer.mat`, `Grey_Dryer.mat`, `Grey_Counter.mat`, `Grey_Metal.mat`, `Grey_Secret.mat`, `Grey_Glass.mat`, `Grey_Sky.mat`, `Grey_Chair.mat`, `Grey_Sign.mat` | Materials | Flat placeholder surfaces for the room, machines, glazing, exterior backdrop, waiting chairs, and signage | Unity Standard materials created in-repo | `Assets/LaundryLegends/Art/Materials/` | Original procedural placeholder; no external license | Built-In Render Pipeline Standard shader | Assigned in room/player prefabs | Integrated; seen in Phase 2 agent captures; no final material art review | Yes |

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
| 2026-10-20 | Registered the procedural placeholder scenes, room and player prefabs, and nine-material greybox set; added the Phase 2 storefront materials | `TestResults/phase1-*.log`, `TestResults/phase2-*.log`, `TestResults/phase2-entrance.png`, `TestResults/phase2-storefront.png` |
| 2026-10-20 | Extended the room prefab with waiting chairs, side table, planter, storage shelving and bins, utility alcove props, and built-in-font signage; added `Grey_Chair` and `Grey_Sign` for eleven materials total | `TestResults/phase2-p3-build.log`, `TestResults/phase2-p3-public-validate.log`, `TestResults/phase2-waiting.png`, `TestResults/phase2-machine-signage.png`, `TestResults/phase2-utility.png` |
