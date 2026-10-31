# Arcade cabinet — authoring notes

- **Author:** original work by this project (no external licence).
- **Script:** `ArtSource/Blender/create_arcade_cabinet.py`
- **Export:** `ArtSource/Exports/ArcadeCabinet.fbx`
- **Previews:** `cabinet_front.png`, `cabinet_three_quarter.png`, `cabinet_side.png`

## Verified numbers (re-imported FBX)

| Item | Value |
| --- | --- |
| Total triangles | **892** (budget 1500) — body 888, screen 2, marquee 2 |
| Size | **0.6608 × 0.7618 × 1.9175 m** |
| Hierarchy | `ArcadeCabinet` (empty, scale 1) → `CabinetBody`, `ScreenSurface`, `MarqueePanel` |
| Body materials | `MAT_CabinetBody` (navy), `MAT_ControlPanel` (charcoal), `MAT_CabinetTrim` (magenta), `MAT_CoinDoor` (metal) |
| Screen material | `MAT_CabinetScreen` (near-black, roughness ~0.065) |

## Screen anchor (this is what the Unity integration needs)

The screen bay leans back by **9.45°**. In **Unity** coordinates:

- `ScreenSurface` local position: **(0, 1.2403, 0.1946)**
- Faces **+Z**, tilted back: normal **(0, 0.1641, 0.9864)**, Euler **(-9.45, 0, 0)**
- Panel size **0.50 × 0.40 m** (4:3, fits a 512×384 RenderTexture)

`MarqueePanel`: local **(0, 1.6725, 0.2380)**, faces +Z, 0.56 × 0.19 m.

## Axis decision (documented, not assumed)

Blender **+Y → Unity +Z**. The cabinet is authored facing **+Y in Blender** so it
faces **+Z in Unity**. This was derived from in-repo evidence (the appliance door
is authored at −Y while `Machine_1`'s `DrumSlot` sits at local z = −0.36 facing
the room; the first-person hands extend to +Y and appear in front of the camera).

**If the cabinet appears backwards in Unity, fix the rotation on the prefab — not
the model.**

## Honest limitations

- Flat materials, no textures, no artwork on the marquee yet.
- The screen is a plain dark quad; the RenderTexture supplies all visuals.
- Coin door, vents and T-moulding are suggested with inset geometry rather than
  modelled trim.
- No attract-mode lighting; the cabinet will read as "off" until something drives
  the screen.

## Build

```powershell
& "C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe" -b -P "ArtSource\Blender\create_arcade_cabinet.py"
```

Read `[Cabinet]`/`[Verify]` lines; an earlier generator in this project exited 0
after an assertion, so the log is the gate.
