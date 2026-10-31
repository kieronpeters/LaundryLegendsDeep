# Laundry Legends — Asset Inventory

**Last updated:** 2026-10-31
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
| ENV-ROOM-001 | `GreyboxRoom.prefab` | Environment | Walkable room shell, counter, machines with additive front panels/drawers/coin slots, one washer load/unload hook, storefront, exterior backdrop, waiting area, storage shelving, utility alcove, signage, secret-door concealment, and secret-door foreshadowing | Generated in-repo by `GreyboxSetupTool`, `Phase2SetupTool`, `Phase3SetupTool`, and `BasketAssetSetupTool` using Unity primitives and built-in font signage | `Assets/LaundryLegends/Prefabs/Environment/` | Original procedural placeholder; no external license | Unity primitives, Standard shader, eleven greybox materials, `LegacyRuntime.ttf`, `MachineLoadHook` | Placed in `Laundromat.unity` | Phase 1 validation + owner-reported walkthrough; Phase 2 facade/public-zone/concealment validation and agent-inspected captures; Phase 2 human review passed; Phase 3 machine-hook and machine-front validation | Yes |
| GAMEPLAY-PLAYER-001 | `Player.prefab` | Player | First-person movement, camera, pause, interaction raycast, carry controller, hold anchor, and IMGUI interaction prompt view | Generated in-repo by `PlayerSetupTool` | `Assets/LaundryLegends/Prefabs/Gameplay/` | Original procedural placeholder; no external license | CharacterController, camera, runtime player scripts | Placed in `Laundromat.unity` | Automated validation/tests and owner-reported Phase 1 movement/camera check | Yes |
| GAMEPLAY-LOOSE-001 | `LooseLaundry.prefab`, `LooseLaundry.fbx`, `MAT_LooseLaundry.mat` | Gameplay prop | Custom low-poly folded-laundry bundle, 324 triangles, 0.32 x 0.29 x 0.21 m, base-center pivot | Authored in Blender 4.5.15 with `ArtSource/Blender/create_laundry_props.py`; imported by `BasketAssetSetupTool` | `ArtSource/Blender/LooseLaundry.blend`, `ArtSource/Exports/LooseLaundry.fbx` | Original project asset; no external license | Blender mesh, `MAT_LooseLaundry` Standard material, `LaundryItem`, `LaundryItemInteractable`, SphereCollider, Rigidbody | `LooseLaundry.prefab` under `Phase3Items`; validators and tests pass | Prop validation, item validation, EditMode/PlayMode, capture inspected | Yes |
| GAMEPLAY-BAG-001 | `Bag.prefab`, `Bag.fbx`, `MAT_Bag.mat` | Gameplay prop | Custom low-poly kraft bag, 432 triangles, 0.36 x 0.51 x 0.24 m, base-center pivot | Authored in Blender 4.5.15 with `ArtSource/Blender/create_laundry_props.py`; imported by `BasketAssetSetupTool` | `ArtSource/Blender/Bag.blend`, `ArtSource/Exports/Bag.fbx` | Original project asset; no external license | Blender mesh, `MAT_Bag` Standard material, `LaundryItem`, `LaundryItemInteractable`, BoxCollider, Rigidbody | `Bag.prefab` under `Phase3Items`; validators and tests pass | Prop validation, item validation, EditMode/PlayMode, capture inspected | Yes |
| GAMEPLAY-BASKET-001 | `Basket.prefab`, `LaundryBasket.fbx`, `MAT_Basket.mat` | Gameplay prop | Custom low-poly open laundry basket, 756 triangles, 0.50 x 0.35 x 0.33 m, base-center pivot | Authored in Blender 4.5.15 with `ArtSource/Blender/create_basket.py`; exported FBX and imported by `BasketAssetSetupTool` | `ArtSource/Blender/LaundryBasket.blend`, `ArtSource/Exports/LaundryBasket.fbx`, `Assets/LaundryLegends/Art/Models/LaundryBasket.fbx` | Original project asset; no external license | Blender mesh, `MAT_Basket` Standard material, `LaundryItem`, `LaundryItemInteractable`, BoxCollider, Rigidbody | `Basket.prefab` in `Phase3Items`; validators and tests pass | Basket validation, EditMode 32/32, PlayMode 37 passed plus one capture test, capture inspected | Yes |
| NPC-AVATARS-001 | `NPC_Customer.prefab`, `NPC_Attendant.prefab`, `NPC_Regular.prefab` | Characters | Three stylized humanoid NPC archetypes with capsule colliders, humanoid avatars, texture materials, and static relaxed poses | CC0 100 Avatars R2 bases (BizDude, SunflowerPerson, CosmicPerson) imported by `NpcAssetSetupTool` | `Assets/LaundryLegends/Prefabs/Gameplay/NPC/`, `ArtSource/Licenses/100-avatars.md` | CC0 1.0; Polygonal Mind; no attribution required, credit appreciated | 100 Avatars FBX + PNG albedo, Standard materials, Unity humanoid avatar | Prefabs only; not yet placed in a scene | NPC validation, EditMode/PlayMode suites, agent-inspected `TestResults/npc-archetypes.png` | Yes — **superseded 2026-11-01**: the avatars' humanoid skeletons place the hips kilometres from the origin, so retargeted animation renders them off-screen. Prefabs now use the animation library's mannequin rig (`MANNEQUIN-NPC-001`); the 100 Avatars meshes remain in `Art/Models/NPC/` unused. |
| MANNEQUIN-NPC-001 | `NPC_Attendant.prefab`, `NPC_Customer.prefab`, `NPC_Regular.prefab` (rebuilt) | Characters | Animated NPC bodies on the animation pack's own mannequin, tinted per archetype | Quaternius Universal Animation Library rig (CC0); `NpcMannequinTool` | `Assets/LaundryLegends/Prefabs/Gameplay/NPC/` | CC0 1.0 | `UAL1_Standard.fbx` Mannequin mesh, `NPC_Locomotion.controller`, `NpcLocomotion`, `CarrySocket`, `CarryPose` | Renderer bounds centre y=0.86, extents ~0.97 (correct scale and floor contact) | Captures `TestResults/captures/05..07`; EditMode 43/43, PlayMode 45 passed / 1 skipped | Yes — faceless mannequin, no clothing |
| ANIM-NPC-001 | `UAL1_Standard.fbx` (+ `NPC_Locomotion.controller`, generated) | Animation | 45 humanoid takes used for NPC idle/walk/pickup; retargeted onto the 100 Avatars rigs | Quaternius Universal Animation Library (Standard), CC0 1.0; retrieved 2026-11-01 from a public GitHub mirror after the itch.io download failed | `Assets/LaundryLegends/Art/Animations/`, `ArtSource/Licenses/quaternius-universal-animation-library.md` | CC0 1.0; no attribution required | Unity humanoid avatar `UAL1_StandardAvatar` (isHuman/isValid verified), NPC humanoid avatars, `NpcLocomotion`, `CarrySocket`, `CarryPose` | Import verified by batch run `TestResults/anim-import3.log`: avatar valid, 45 clips listed | Import + clip enumeration; runtime wiring and captures tracked under workstream W1 | No |
| AUDIO-VOICE-002 | 54 dialogue WAVs (`<archetype>_<trigger>_<n>.wav`) | Audio | Spoken lines for attendant, phone regular, and drop-off customer | Piper 2023.11.14-2 (engine, MIT-era release) with public-domain from-scratch voices: `en_GB-cori-high`, `en_US-ljspeech-high`, `en_US-norman-medium` | `ArtSource/Voice/Draft/`, `ArtSource/Licenses/piper-tts.md` | Engine MIT-era; voices public domain (LibriVox / LJ Speech) | Piper binary `Tools/Piper/` (git-ignored), `Tools/Voice/render_dialogue.py` | Mechanical validation only: 54/54 files, correct sample rate, non-silent, 3.66–6.08 s | Mechanical checks passed; **no human listening pass, not yet imported into Unity** | No |
| AUDIO-MUSIC-002 | `ThatZenMoment.mp3`, `Ripples.mp3` | Audio | Longer, percussion-free ambient beds to replace the 84-second loop | Kevin MacLeod (incompetech.com), CC BY 4.0; fetched 2026-11-01 | staged in `ArtSource/Downloads/Music/`, target `Assets/LaundryLegends/Audio/Music/` | **CC BY 4.0**; credit required (see `ArtSource/Licenses/kevin-macleod.md`) | Recorded MP3, `MusicPlaylist` | Download verified (12,038,401 / 8,220,237 bytes); not yet imported into Unity | Download only; import and playlist run tracked under workstream W5 | No |
| MAT-GREYBOX-001 | `Grey_Floor.mat`, `Grey_Wall.mat`, `Grey_Washer.mat`, `Grey_Dryer.mat`, `Grey_Counter.mat`, `Grey_Metal.mat`, `Grey_Secret.mat`, `Grey_Glass.mat`, `Grey_Sky.mat`, `Grey_Chair.mat`, `Grey_Sign.mat` | Materials | Flat placeholder surfaces for the room, machines, glazing, exterior backdrop, waiting chairs, and signage | Unity Standard materials created in-repo | `Assets/LaundryLegends/Art/Materials/` | Original procedural placeholder; no external license | Built-In Render Pipeline Standard shader | Assigned in room/player prefabs | Integrated; seen in Phase 2 agent captures; no final material art review | Yes |
| MAT-ENV-001 | `MAT_FloorTile.mat`, `MAT_WallPaint.mat`, `MAT_CounterWood.mat`, six texture maps | Materials | CC0 PBR tile, painted plaster, and wood materials for floor/walls/counters | ambientCG Tiles141, PaintedPlaster017, WoodFloor064 (1K JPG) | `Assets/LaundryLegends/Art/Textures/Environment/`, `ArtSource/Licenses/ambientcg.md` | CC0 1.0; no attribution required | Standard shader, color + normal maps, tiling | Applied in `GreyboxRoom.prefab` | Environment validation, navigation, EditMode/PlayMode, captures | Yes |
| PROP-ENV-001 | Open/hours signs, detergent bottles, plant, cart | Gameplay props | Additive signage and decor details | Generated in-repo by `BasketAssetSetupTool` using Unity primitives and text meshes | `Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab` | Original project assets; no external license | Unity primitives, standard materials, built-in font | Applied in `GreyboxRoom.prefab` | Signage/prop validation, navigation, captures | Yes |
| PROP-APPLIANCE-001 | `Appliance.fbx`, `MAT_Appliance_*` | Machines | Custom 808-triangle washer/dryer with door ring/glass, control panel, coin slot, and drawer | Authored in Blender 4.5.15 with `ArtSource/Blender/create_appliance.py` | `ArtSource/Blender/Appliance.blend`, `Assets/LaundryLegends/Art/Models/Appliance.fbx` | Original project asset; no external license | Blender mesh, five Standard materials | Eight instances in `GreyboxRoom.prefab` | Appliance validation, machine-hook validation, captures | Yes |
| AUDIO-MUSIC-001 | `BalinesePrelude.mp3` | Audio | Real Balinese-inspired game/ambience track | Geoff Harvey - Purple Planet Music; Free Music Archive | `Assets/LaundryLegends/Audio/Music/` | **CC BY 4.0**; credit required | Recorded MP3, Unity AudioSource | One looping source in `Laundromat.unity` | Music validation, PlayMode music test | Yes |

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
- The approved zero-cost pipeline is documented in `ASSET_PIPELINE.md`: Blender LTS plus CC0 packs (Kenney, Quaternius, Poly Haven, Poly Pizza, 100 Avatars) and Mixamo animations. Every imported asset still gets its own source, author, license, and attribution row here before it is committed.
- AI-generated assets are not approved. Quaternius and Mixamo raw files must not be redistributed as standalone packs.

## 6. Tool reality (verified 2026-10-19)

- Blender 4.5.15 LTS is installed at `C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe` (portable ZIP, SHA-256 verified 2026-10-31). It is not on `PATH`; invoke by full path.
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
| 2026-10-20 | Added an idempotent secret-access service panel and rolling supply shelf with a `SERVICE` marker; no new material or imported asset | `TestResults/phase2-p4-build.log`, `TestResults/phase2-p4-validate.log`, `TestResults/phase2-secret-approach.png`, `TestResults/phase2-secret-close.png` |
| 2026-10-31 | Registered the approved free hybrid pipeline (Blender + CC0 + Mixamo) and the first basket prototype plan; no asset downloaded or installed yet | `ASSET_PIPELINE.md`, `.orchestrator/decisions/0006-free-asset-pipeline.md`, `.orchestrator/reports/2026-10-31-asset-pipeline-research.md` |
| 2026-10-31 | Installed Blender 4.5.15 LTS (SHA-256 verified) and replaced the basket primitive with a 756-triangle custom mesh, `MAT_Basket`, and updated `Basket.prefab`; loose laundry and bag remain placeholders | `ArtSource/Blender/LaundryBasket.blend`, `ArtSource/Exports/LaundryBasket.fbx`, `TestResults/p3-011-validate-basket.log`, `TestResults/phase3-prompt.png` |
| 2026-10-31 | Replaced loose-laundry and bag primitives with 324- and 432-triangle meshes, added `MAT_LooseLaundry`/`MAT_Bag`, and added 24 additive machine front details; runtime contracts and machine geometry preserved | `ArtSource/Blender/LooseLaundry.blend`, `ArtSource/Blender/Bag.blend`, `TestResults/p3-012-validate-props.log`, `TestResults/p3-012-validate-machines.log`, `TestResults/phase3-machine.png` |
| 2026-10-31 | Added three CC0 100 Avatars NPC prefabs with humanoid avatars, capsule colliders, texture materials, and static relaxed poses; no behavior or scene placement | `ArtSource/Licenses/100-avatars.md`, `TestResults/p3-013-validate-npc-6.log`, `TestResults/npc-archetypes.png` |
| 2026-10-31 | Applied CC0 ambientCG tile/plaster/wood materials, warmed room lights, added open/hours signs, detergent bottles, plant, and cart; polished NPC scale/tint/pose | `ArtSource/Licenses/ambientcg.md`, `TestResults/p3-final-*.log`, `TestResults/phase2-entrance.png`, `TestResults/phase2-waiting.png`, `TestResults/npc-archetypes.png` |
| 2026-10-31 | Added custom 8-machine appliance models, real CC BY Balinese music, and a walkable storefront frontage with mounted signage | `TestResults/frontage-outside.png`, `TestResults/frontage-dropoff.png`, `TestResults/phase3-machine.png`, `ArtSource/Licenses/balinese-laundry-music.md` |
