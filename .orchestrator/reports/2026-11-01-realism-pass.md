# Realism Pass — Status

- **Updated:** 2026-11-01
- **Plan:** `.orchestrator/ORCHESTRATION_PLAN.md`

## Verified

| Item | Evidence |
| --- | --- |
| Animation FBX imports as humanoid | `TestResults/anim-import3.log` — 45 clips, valid avatar |
| Loop enabled on Idle/Walk takes | `TestResults/w1-build2.log`, validation pass |
| Locomotion controller + prefab wiring | `Assets/LaundryLegends/Art/Animations/NPC_Locomotion.controller` |
| NPCs render at correct size and place | `NpcMannequinTool.CaptureOne` → bounds center y=0.86, extents ~0.97 |
| Staff walks and carries laundry | `TestResults/captures/06-staff-walking.png`, `07-staff-carrying.png` |
| Counter signage size/duplication fixed | `TestResults/sign-fix.log` (2.5 m → 0.62 m, duplicate removed) |
| Bundle mesh authored | `ArtSource/Exports/WrappedBundle.fbx`, 1140 tris, 0.297 m |
| Bundle imported and swapped | `Assets/LaundryLegends/Art/Models/WrappedBundle.fbx`, prefab `WrappedBundle.prefab`; `LaundryItem` shows it at the Wrapped stage (`TestResults/bundle.log`) |
| Music replaced | `ThatZenMoment.mp3` (10:02) + `Ripples.mp3` (3:25) + `TempleOfHangzhou` in the playlist (`TestResults/music2.log`) |
| Voice lines rendered | 54/54 WAVs, `ArtSource/Voice/rendering-report.csv` |
| Music tracks downloaded | That Zen Moment 10:02, Ripples 3:25 (CC BY 4.0) |
| Test suites | **EditMode 43/43**, **PlayMode 45 passed, 0 failed, 1 skipped** (`TestResults/final2-*.xml`) |
| Voice wired | 15 archetype/trigger entries, 54 clips (`TestResults/voice-setup.log`, `voice-validate.log`) |
| Room atmosphere | 4 fixtures with lights, range 7.5, ambient 0.36, contact shadows, floor/wall tiling (`TestResults/room2.log`) |

## The 100 Avatars rig problem (important)

The Phase 3 NPC models cannot be used with Unity humanoid retargeting. Their
avatars place the hips at **y ≈ 4629** (Attendant) and **y ≈ 89** (Regular) even
though the meshes render at roughly 1.67 m. Because bind poses compensate, the
characters look fine when static, but a humanoid Animator positions them
kilometres from the room, so they disappear.

Evidence: `TestResults/avatar-diag2.log`, `TestResults/diag-retarget.log`
(hips local `(29.46, 4472.17, 26.32)` in play mode).

Importer combinations tested (`TestResults/probe-import.log`):

| Setting | Avatar hips |
| --- | --- |
| fileScale on, globalScale 15.5 (original) | 4629 |
| fileScale off, globalScale 1 | 2.99 |
| fileScale off, globalScale 15.5 | 46.29 |

None is consistent with the rendered size, so the rigs are unusable.

A Blender re-export normalised the geometry correctly (1.75 m, feet at 0,
65 `mixamorig:` bones, skinned) but Unity refused to build a humanoid avatar
from it (`Avatar creation failed`).

**Resolution:** NPC prefabs were rebuilt on the **animation library's own
rigged mannequin** (`UAL1_Standard.fbx` → `Mannequin`), which is the exact rig
the clips were authored for, so retargeting is exact. Each archetype gets a
tinted material.

**Cost of that decision:** the NPCs are now a faceless mannequin rather than the
textured 100 Avatars characters. Character art is a placeholder again and needs
a proper CC0 rigged human that Unity can map.

## Known visual defects

1. NPC bodies are a plain tinted mannequin — no face, clothing, or hair. A CC0
   clothed character (Quaternius "Man", 1,851 tris, 31 bones, baked to 1.80 m)
   is downloaded and staged at `Assets/LaundryLegends/Art/Models/NPC/QuaterniusMan.fbx`,
   but Unity imports it as **non-humanoid** even after renaming bones to Unity
   standard names, and `AvatarBuilder.BuildHumanAvatar` rejects the hand-built
   description (`valid=False`). The mannequin remains until that mapping is solved.
2. The modeled wrapped bundle is in game but renders as an **opaque pale shape**:
   only one of its two renderers keeps its material override, and the film shader
   does not read as transparent in the batch capture. Needs a dedicated material pass.
3. The carried item is parented to a chest anchor and the arm-override pose does
   not visibly grip it, so it reads as floating in front of the body.
4. Room is improved but still prototype quality: flat materials, no
   reflection probes, dark ceiling.

## Corrections made during the pass

1. `FindClip` matched Unity `__preview__` clips; controller bound a non-looping
   preview take. Filter fixed.
2. `FindFirstObjectByType<FoldingStation>(true)` used a bool where Unity 6 wants
   `FindObjectsInactive`. Fixed.
3. `SpeedParameterRisesWhileWalking...` failed because the staff loop kept moving
   the NPC, then again because it measured fixed frames instead of elapsed time.
   Test now disables the staff loop and accumulates `Time.deltaTime`.
4. Counter signage was 2.5 m wide with a mirrored duplicate; resized, duplicate
   removed, and turned to face the room.
