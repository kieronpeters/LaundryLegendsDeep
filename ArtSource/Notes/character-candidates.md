# Rigged Humanoid Character — Candidate Comparison

**Date:** 2026-11-01
**Author:** asset subagent
**Task:** find a licence-clean, rigged, skinned humanoid to replace the faceless
100 Avatars NPC mannequins that could not be humanoid-retargeted in Unity.
**Scope:** research + download + inspection only. No Unity launch, no `Assets/`
change, no package change.

---

## 1. Verdict

**Recommended: Quaternius "Man" from Poly Pizza (CC0).**

It is the only candidate that is all five of: rigged and skinned, casual modern
clothing, T-pose rest pose, humanoid-mappable bone names, and shipped with
ready-made locomotion clips. It needs one uniform 0.372 scale bake because the
source GLB is authored 2.67× too large; that bake is already done and verified
(`derived/Quaternius_Man_1m80.fbx`, 1.80 m).

Use `ArtSource/Downloads/Characters/poly-pizza-quaternius-man/derived/Quaternius_Man_1m80.fbx`.

---

## 2. Candidates at a glance

| | **Quaternius "Man"** (Poly Pizza) | **KayKit Rogue** (Adventurers) | **Quaternius Universal Base Characters** |
|---|---|---|---|
| Licence | CC0 (Public Domain) | CC0 | CC0 |
| Commercial / no royalty | yes | yes | yes |
| Format downloaded | GLB (493 KB) | GLB + FBX (3.6 / 19.5 MB) | glTF + .bin + PNGs (~9.3 MB) |
| Theme | casual modern man: t-shirt, shorts, sneakers, hair, face | chibi fantasy rogue: tunic, cape, crossbow, knives | muscular superhero base body in underwear |
| Fits a laundromat | **yes** | no | no (needs a separate outfit pack) |
| Armature bones | 31 | 41 | 65 |
| Bone naming | `Hips`, `Abdomen`, `Torso`, `UpperArm.L`, `UpperLeg.L` | `root`, `hips`, `spine`, `chest`, `upperarm.l` | `pelvis`, `spine_01`, `upperarm_l`, `thigh_l` |
| Height at import scale 1 | **4.84 m** (needs 0.372 bake) | 2.17 m (chibi; head is 1.02 m of it) | **1.82 m** ✔ |
| Triangles | 1,851 | 4,263 (6,377 incl. props) | 14,318 |
| Vertex groups / skin | 31 / armature modifier on all meshes | 41 / armature modifier on 6 meshes | 65 / armature modifier on 3 meshes |
| Rest pose | T-pose (arm 85° from vertical) | T-pose (90°) | T-pose (90°) |
| Animations included | **11** (`Man_Idle`, `Man_Walk`, …) | 76 | 0 |
| Textures | none — 6 flat material colours | 1 palette PNG (17 KB) | 5 PBR PNGs |
| Verdict | **recommended** | rejected (art direction) | rejected (no clothing in free tier) |

All three are CC0 and pass the licence gate. The decision is art direction plus
retarget readiness, not licensing.

---

## 3. Method

Everything was measured, not assumed.

```powershell
$b = "C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe"
$b -b -P ArtSource\Blender\inspect_character.py -- <file> --label <name>
$b -b -P ArtSource\Blender\render_character.py  -- <file> <out-prefix> [--action NAME --frame N]
```

- `inspect_character.py` imports into an empty scene, walks the object
  hierarchy, lists every bone, evaluates the deformed mesh through the
  dependency graph for real world-space bounds, measures the rest-pose arm
  direction for the T/A-pose call, and counts vertex groups and modifiers.
- `render_character.py` produces front and three-quarter previews under plain
  EEVEE lighting so the result can be eyeballed.
- Raw JSON dumps and previews: `ArtSource/Notes/_inspect/`.
- The Blender glTF importer adds a stray 42-vertex `Icosphere` used as a bone
  display shape; it is excluded from every bound and triangle count below.
- Unity was **not** launched (another process owns the editor slot), so the
  humanoid-mapping assessment in §5 is reasoning from Unity's documented
  name-based auto-mapper, not an observed import.

---

## 4. Per-candidate detail

### 4.1 Quaternius "Man" (recommended)

Source: https://poly.pizza/m/HMnuH5geEG → `static.poly.pizza/…glb`.

Object hierarchy as imported:

```
RootNode (empty)
└─ HumanArmature (armature, object scale 100)
   ├─ BaseHuman (mesh, 3967 verts / 1851 tris, 31 vertex groups, ARMATURE modifier)
   └─ 10 × *_end empties (export-time leaf markers)
```

Bones (31):

```
Bone, Foot.L, Body, Hips, Abdomen, Torso, Neck, Head, Shoulder.L, UpperArm.L,
LowerArm.L, Palm.L, MiddleHand.L, Fingers.L, Thumb1.L, Thumb2.L, Shoulder.R,
UpperArm.R, LowerArm.R, Palm.R, MiddleHand.R, Thumb1.R, Thumb2.R, Fingers.R,
UpperLeg.L, LowerLeg.L, UpperLeg.R, LowerLeg.R, PoleTarget.L, Foot.R, PoleTarget.R
```

- Materials: `Shirt`, `Skin`, `Pants`, `Eyes`, `Socks`, `Hair`. No image
  textures — flat Principled colours, which suits the project's stylised tone
  and keeps import light.
- Animations: `Man_Idle`, `Man_Walk`, `Man_Run`, `Man_Standing`, `Man_Sitting`,
  `Man_Jump`, `Man_RunningJump`, `Man_Clapping`, `Man_Punch`, `Man_Death`,
  `Man_SwordSlash`.
- **Height problem:** the raw GLB evaluates to **4.8413 m** in rest pose and
  4.8094 m posed. The armature node carries a 100× scale over
  centimetre-sized vertices, so this is a pure authoring-scale artefact, not a
  giant character.
- **Fix applied:** `ArtSource/Blender/convert_to_fbx.py` unparents with
  `CLEAR_KEEP_TRANSFORM`, bakes world transforms, applies a single uniform
  factor 0.371805 about the origin (1.80 / 4.8413), multiplies the 180
  bone-local `.location` F-curves by the same factor so the animation channels
  stay consistent with the rescaled rest bones, and exports FBX.

Verified after the bake, by re-importing the FBX:

| Check | Result |
|---|---|
| Height, rest pose | **1.800 m** (posed 1.8052 m) |
| Object world scale | 1.0 on armature, mesh and root — no importer scale needed |
| Bones | 31, unchanged names |
| Vertex groups | 28 with weights (`Bone`, `PoleTarget.L`, `PoleTarget.R` carry none) |
| Armature modifier | present, binding intact — walk/idle renders deform correctly |
| Animations | 11, names preserved (`HumanArmature|Man_Idle`, …) |
| Triangles | 1,851 |

Preview: `ArtSource/Notes/_inspect/man_front.png`,
`man_threequarter.png`, and `derived-walk_threequarter.png` (mid-walk frame from
the baked FBX — proves the skin follows the rescale).

### 4.2 KayKit — Adventurers, Rogue

Source: `KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0`, CC0.

- `Rig` armature, 41 bones: `root, hips, spine, chest, upperarm.l, lowerarm.l,
  wrist.l, hand.l, handslot.l, upperarm.r, …`
- 6 skinned meshes (`Rogue_Body/ArmLeft/ArmRight/Head/LegLeft/LegRight`, 41
  vertex groups each, armature modifier) plus unskinned props parented to the
  rig (cape, knife, two crossbows, throwable).
- 76 animation clips — the richest animation set of the three.
- Height 2.1732 m to the top of the hood; the head alone spans 1.02 m, i.e.
  genuine chibi proportions. With the held crossbow the bounding box reaches
  down to −0.40 m.
- Rejected on art direction: a medieval rogue with a cape and crossbow does not
  belong in a Bali laundromat, and the chibi proportions would look wrong even
  if re-clothed.
- Preview: `_inspect/kaykit_front.png`.

### 4.3 Quaternius — Universal Base Characters (Superhero Male)

Source: official page (CC0) via the `olccamy/CanvasPro` GitHub mirror.

- 65 bones: `root, pelvis, spine_01, spine_02, spine_03, neck_01, Head,
  clavicle_l, upperarm_l, lowerarm_l, …` — Unreal-style names, present on all
  three skinned meshes (`SuperHero_Male` 12,566 tris, `Eyes` 768,
  `Eyebrows` 984), 65 vertex groups each, armature modifier on all.
- Height **1.8196 m** at import scale 1 — the only candidate that satisfies the
  size requirement with no maths at all.
- T-pose, 0 animations, 5 PBR textures, ~9.3 MB.
- Rejected because the free Standard edition only contains the **Superhero**
  bodies: a muscular adult in underwear. There is no clothing in the free
  download, the Regular/Teen bodies are Source-only, and the outfits live in a
  separate `Modular Character Outfits` pack. Shipping this as an NPC would
  replace one faceless mannequin with another.
- Preview: `_inspect/ubc_front.png`.

---

## 5. Unity humanoid auto-mapper risk

Unity's auto-mapper is name-based. Reasoning per candidate, **not yet verified
in Unity**:

**Quaternius "Man" — medium risk, expect 2–3 manual dropdowns.**
- Will map cleanly: `Hips`, `Neck`, `Head`, `Shoulder.L/R`, `UpperArm.L/R`,
  `LowerArm.L/R`, `UpperLeg.L/R`, `LowerLeg.L/R`, `Foot.L/R`. The `.L`/`.R`
  suffix and the plain English bone names are well covered.
- Uncertain: `Abdomen` → Spine and `Torso` → Chest may not auto-match, because
  Unity's dictionary keys on "Spine" and "Chest". If they miss, assign them by
  hand in the Avatar inspector (two dropdowns). Do not trust the auto-map here;
  check the Avatar configuration screen before building prefabs.
- Uncertain: `Palm.L/R` → Hand (Unity looks for "Hand"). Same treatment.
- Extra bones (`Bone`, `Body`, `PoleTarget.L/R`) and the four-way finger chain
  do not block mapping — unmapped extras are allowed.
- **The failure that killed the 100 Avatars rigs does not apply here:** every
  bone head sits within 4.49 m of the origin in raw units (≈1.67 m after the
  bake), so the skeleton is compact and origin-anchored.

**KayKit Rogue — low risk.** `hips`/`spine`/`chest`/`upperarm.l`/`lowerarm.l`
are textbook names; expect a clean auto-map. Irrelevant, since it is rejected.

**Universal Base Characters — medium risk.** `pelvis`, `spine_01…03`,
`clavicle_l`, `upperarm_l`, `thigh_l`, `calf_l`, `ball_l` are the Unreal
convention; Unity generally recognises them, but the numbered spine chain and
`ball_l` toe naming are the parts most likely to need a manual pick.

---

## 6. Files produced

Downloaded (untracked — `ArtSource/Downloads/` is gitignored):

```
ArtSource/Downloads/Characters/
├─ poly-pizza-quaternius-man/
│  ├─ Quaternius_Man.glb                       493,196 B  dad8fa3c…a37fc58
│  └─ derived/Quaternius_Man_1m80.fbx        2,021,052 B  31d56275…980c28c
├─ kaykit-adventurers/
│  ├─ Rogue.glb                              3,616,284 B  e825437c…998fbb7d
│  ├─ Rogue.fbx                             19,529,164 B  93a9ee0a…195eb71
│  ├─ rogue_texture.png                         16,670 B
│  └─ LICENSE.txt                                  891 B
└─ quaternius-universal-base-characters/
   ├─ Superhero_Male_FullBody.gltf              30,989 B  e7fcea21…52dafeb4
   ├─ Superhero_Male_FullBody.bin              720,076 B  459003f9…3e4cd92f
   ├─ T_Superhero_Male_{Dark,Normal,Roughness}.png
   ├─ T_Eye_Brown.png, T_Hair_1_BaseColor.png
   └─ SOURCE.md, CanvasPro_LICENSE.txt
```

Tracked in the repository:

```
ArtSource/Licenses/poly-pizza-quaternius-man.md
ArtSource/Licenses/kaykit-adventurers.md
ArtSource/Licenses/quaternius-universal-base-characters.md
ArtSource/Notes/character-candidates.md          (this file)
ArtSource/Notes/_inspect/*.txt, *.png            (raw JSON dumps + previews)
ArtSource/Blender/inspect_character.py           (reusable inspector)
ArtSource/Blender/render_character.py            (reusable preview renderer)
ArtSource/Blender/convert_to_fbx.py              (scale bake + FBX export)
```

The three scripts are generic: they take a path argument and work on any
FBX/GLB, so they stay useful for the next character.

---

## 7. Not done / limits

- **Unity was not launched**, so humanoid mapping, actual import height, and
  animation retargeting in-engine are unverified. The §5 risk notes are
  predictions.
- Nothing was copied into `Assets/`. The existing `NPC_Customer`,
  `NPC_Attendant`, `NPC_Regular` prefabs still point at the 100 Avatars meshes.
- The Unity project has **no glTF importer package** (`Packages/manifest.json`
  contains no gltfast/glTF entry), so the GLB cannot be dropped into `Assets/`
  as-is. The derived **FBX** is the path that works with the current Built-In
  pipeline and package set. Adding a glTF package would be a package change and
  therefore an approval gate.
- No female or child variant was fetched. Poly Pizza hosts the rest of the same
  CC0 Quaternius character line; a `Woman` counterpart is the obvious next
  download for NPC variety.
- Clip names survive the export as `HumanArmature|Man_Idle` etc. Unity will use
  those take names; renaming clips happens in the model importer.
- Seven redundant preview PNGs from the first render pass could not be deleted
  (the shell policy blocks delete commands). Harmless; remove manually if the
  `_inspect` folder size matters.
