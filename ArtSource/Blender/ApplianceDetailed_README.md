# ApplianceDetailed — washer + dryer source

Detailed front-load washer and dryer for Laundry Legends, built with Blender
4.5.15 and exported to FBX. **Asset generation only — nothing in this folder is
wired into a Unity scene, prefab, or script yet.**

## Files

| File | Purpose |
| --- | --- |
| `ArtSource/Blender/ApplianceDetailed.blend` | Editable source. Both machines, each in its own collection, plus a preview studio (camera, 3 lights, floor/wall). |
| `ArtSource/Blender/ApplianceDetailed_preview.png` | 1400 x 1050 Cycles preview of both machines. |
| `Assets/LaundryLegends/Art/Models/Appliances/WasherDetailed.fbx` | Washer, 356 KB. |
| `Assets/LaundryLegends/Art/Models/Appliances/DryerDetailed.fbx` | Dryer, 370 KB. |

The `.blend` stores the two machines side by side (washer at x = −0.565, dryer at
x = +0.565) so they can be inspected together. **The exported FBXs are each at
the origin**: root empty at (0, 0, 0), floor at z = 0, geometry centred on X.

## Conventions (matches the existing appliance pipeline)

* Units: metres. Width 0.800, height 1.100, depth 0.719 (washer) / 0.722 (dryer,
  because of the rear vent hose).
* Origin: floor centre. Feet touch z = 0; the body top is z = 1.100.
* Front of the machine faces **−Y in Blender**, exactly like
  `ApplianceHD.blend` and the in-game `Assets/.../Appliance.fbx`.
* FBX export used the same settings as `create_appliance_hd.py`:
  `axis_forward="-Z"`, `axis_up="Y"`, `FBX_SCALE_ALL`, `apply_unit_scale=True`,
  `use_mesh_modifiers=True`, `add_leaf_bones=False`, `bake_space_transform=False`.
  Transforms are applied (verified: every imported mesh has identity
  location/rotation/scale), so the asset drops into an existing `Machine_i`
  visual slot without changing rotation, scale, or pivot.
* One root empty per machine (`WasherDetailed`, `DryerDetailed`) with one child
  mesh per material group (11–12 meshes).

## Geometry

| | Washer | Dryer |
| --- | --- | --- |
| Triangles | 13,300 | 14,144 |
| Meshes | 12 | 11 |
| Materials | 12 | 11 |

Both models contain: a rounded/bevelled cabinet shell with a bored front face
(the bore is lined with the dark drum material), a beveled top lid with a seam,
a dark plinth band, four adjustable feet with rubber pads, a recessed circular
door with a domed translucent glass, a rubber bellows gasket, a dark drum with
three lifters, 18 rim ribs and five two-lobed cloth bundles, a dark control
panel with a display, four buttons, a 28-segment knob with index marks, plus a
rear service panel with vent slots and screws.

Washer only: detergent drawer with grip and softener cap, drain filter cap,
front intake slots, two rear inlet bibs, a drain hose with clamp, and a power
cord.

Dryer only: lint-screen module (slatted screen with accent pull) instead of the
drawer, a 100 mm rear vent stub with corrugated collar, a flexible duct run, and
a rear lint exhaust hatch.

Accent colour: washer cool blue, dryer warm orange.

## Materials

Each model ships its own material set (no shared dependencies):

| Washer | Dryer | Notes |
| --- | --- | --- |
| `MAT_Washer_Body` | `MAT_Dryer_Body` | Off-white painted steel, rough 0.34 |
| `MAT_Washer_Accent` | `MAT_Dryer_Accent` | Blue / orange trim, door collar, badge |
| `MAT_Washer_AccentDark` | — | Washer drawer cap only (dryer set has 11 materials) |
| `MAT_Washer_ControlPanel` | `MAT_Dryer_ControlPanel` | Near-black plastic |
| `MAT_Washer_DarkMetal` | `MAT_Dryer_DarkMetal` | Vent slats, hinges, chassis parts |
| `MAT_Washer_Chrome` | `MAT_Dryer_Chrome` | Door bezel, handle, filter cap |
| `MAT_Washer_Glass` | `MAT_Dryer_Glass` | Alpha 0.30, roughness 0.045 |
| `MAT_Washer_Drum` | `MAT_Dryer_Drum` | Dark drum + cavity liner |
| `MAT_Washer_Rubber` | `MAT_Dryer_Rubber` | Gasket, pads, hoses |
| `MAT_Washer_LED` | `MAT_Dryer_LED` | Emissive display bars (strength 1.8) |
| `MAT_Washer_Cloth` | `MAT_Dryer_Cloth` | Cloth chunk A |
| `MAT_Washer_ClothAlt` | `MAT_Dryer_ClothAlt` | Cloth chunk B |

All materials are Principled BSDF with plain colour values (no texture files).
Every mesh has a UV map (Smart UV Project) so textures can be added later.

## Verification performed

* `blender -b -P build_appliances.py` — builds both machines, saves the blend and
  exports both FBX files (Blender 4.5.15 LTS).
* Re-import each FBX in a clean Blender scene: 13,300 / 14,144 triangles,
  bounds 0.800 x 0.719/0.722 x 1.100 m, z from 0.000 to 1.100, root empty at the
  origin, 11–12 materials, 100 % of meshes carry a UV map, no non-identity mesh
  transforms.
* Rendered `ApplianceDetailed_preview.png` (Cycles CPU, 64 samples, denoise,
  1400 x 1050) and reviewed front, 3/4, close-up, top and rear test renders.

## Limitations

* Not imported into, referenced by, or tested inside Unity. No prefab, scene,
  `.meta` import settings, collider, animation, or LOD work was done. Unity
  generated default `.meta` files for the two FBX files on its own.
* No textures; materials are flat colours. Surface detail comes from geometry
  only.
* No animation or rig. The drum is static geometry, so adding rotation later
  means rotating (or splitting) `*_Drum` and `*_Cloth*`.
* Glass relies on material alpha; transparency must be set up in the Unity
  material after import.
* The preview uses a three-point Cycles studio; it is not an in-game screenshot.
* The generator script was run from a temporary path (session write scope
  covered only the deliverables), so it is not stored in the repo. The `.blend`
  is the editable source of truth; re-run the export from it if needed.
* A first preview render was accidentally written to `C:\ArtSource\Blender\` by
  a relative-path mistake. That stray file is outside the repo and can be
  deleted.
* Blender also wrote `ApplianceDetailed.blend1` (its automatic backup) next to
  the `.blend`.
