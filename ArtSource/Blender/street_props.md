# Street props — authoring notes

- **Author:** original work by this project (no external licence).
- **Script:** `ArtSource/Blender/create_street_props.py`
- **Build:** `blender.exe -b -P "ArtSource\Blender\create_street_props.py"`
  Read `RESULT:`/`[check]` lines from the log — exit code alone is not a gate.

## Verified after the final colour pass

All numbers are from a re-import of the exported FBX, not from the authoring scene.

| Prop | File | Tris | Dimensions (m) | Non-manifold | Base |
| --- | --- | --- | --- | --- | --- |
| Street lamp | `StreetLamp.fbx` | 368 | 0.470 × 1.059 × 4.501 | 0 | z = 0 |
| Bench | `Bench.fbx` | 428 | 1.600 × 0.499 × 0.844 | 0 | z = 0 |
| Potted plant | `PottedPlant.fbx` | 664 | 0.870 × 0.826 × 1.065 | 0 | z = 0 |
| Stack chair | `StackChair.fbx` | 440 | 0.535 × 0.503 × 0.838 | 0 | z = 0 |
| Curb strip | `CurbStrip.fbx` | 64 | 3.965 × 0.220 × 0.150 | 0 | z = 0 |
| Road marking | `RoadMarking.fbx` | 48 | 3.700 × 0.150 × 0.010 | 0 | z = 0 |

Every prop passes: under the 800-triangle budget, zero non-manifold edges, base at
z = 0, object origin at world zero.

**Materials:** `MAT_StreetLampConcrete/Metal/Glass`, `MAT_BenchWood/Frame`,
`MAT_PlantPot/Soil/Stem/Leaf`, `MAT_StackChairPlastic`, `MAT_CurbConcrete`,
`MAT_RoadPaint`.

**Previews:** `StreetLamp.png`, `Bench.png`, `PottedPlant.png`, `StackChair.png`,
`CurbStrip.png`, `RoadMarking.png` in `ArtSource/Exports/`.

## Honest limitations

- Flat-shaded low-poly with no textures; placeholder-grade, not final art.
- The lamp's lens emission uses a Blender emission shader and **will not survive**
  import into Unity's Standard shader — the lens needs a manual emissive material
  and a separate Light component to actually cast light.
- The lamp arm overhangs ~0.89 m beyond its 0.34 m ground footprint by design;
  place it so the head reaches over the pavement, not the wall.
- Plant leaves are flat folded planes, not modelled foliage.
- No scooter, shrine or awning models yet — the street brief lists them for
  Wave 2 and they would need a licensed import or a longer blockout pass.
