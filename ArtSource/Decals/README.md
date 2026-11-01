# Decal set - grunge, damp, wear, and paper notices

Overlay decals for the laundromat surfaces. Everything here is **CC0 1.0** and was
downloaded directly (no login, no accounts). Nothing in this folder is imported
into Unity yet; it is staged source art.

- **Total size:** 72.2 MB (25 files, including the three tools)
- **Colour space:** every RGB file is sRGB-encoded data, except masks (`*_opacity`,
  `speckle_mask`) and normal maps, which are non-colour data.
- **Format:** PNG. Decal colour files carry the opacity mask baked into the alpha
  channel, so a single RGB+A texture drives a standard transparent material. Every
  mask is also shipped separately for shader work, blending, or mask sampling.

## Naming

| Suffix | Meaning |
|---|---|
| `_color.png` | RGBA. RGB is the stain/mark colour, **A is the opacity mask**. |
| `_opacity.png` | Grayscale mask on a transparent-safe RGBA canvas (RGB = A = mask). |
| `_normal.png` | 8-bit tangent-space normal map, OpenGL (Y+) convention. |

## Files

### 01 - water stain / damp patch

| File | Size | Alpha | Source | Use |
|---|---|---|---|---|
| `01_water_stain/waterstain_damp_color.png` | 2048x2048 | yes | ambientCG `SurfaceImperfections001` (2K-PNG) | Cloudy damp patch on plaster, ceiling corners, machine backs. |
| `01_water_stain/waterstain_damp_opacity.png` | 2048x2048 | mask | same | Standalone mask for blending or material lerps. |
| `01_water_stain/waterstain_damp_normal.png` | 2048x2048 | no (A=1) | same (`NormalGL`) | Relief for close-up damp patches; re-written 8-bit. |
| `01_water_stain/waterstain_broad_color.png` | 2048x2048 | yes | ambientCG `SurfaceImperfections002` (2K-PNG) | Larger, rounder damp bloom - big wall areas and ceiling. |
| `01_water_stain/waterstain_broad_opacity.png` | 2048x2048 | mask | same | Standalone mask. |

Alpha range: `damp` 0.00-0.83 (mean 0.16), `broad` 0.17-1.00 (mean 0.36).

### 02 - floor grime gradient

| File | Size | Alpha | Source | Use |
|---|---|---|---|---|
| `02_floor_grime/floorgrime_edge_color.png` | 2048x512 | yes | ambientCG `Leaking009` (2K-PNG) | Grime that wicks up and fades out from the floor line; strip along skirting, machine bases, door thresholds. |
| `02_floor_grime/floorgrime_edge_opacity.png` | 2048x512 | mask | same | Standalone mask. |
| `02_floor_grime/floorgrime_edge_normal.png` | 2048x512 | no (A=1) | same (`NormalGL`) | Slight relief along the floor transition. |

Alpha range 0.00-0.96, mean 0.10. The source is already a gradient; nothing was
altered beyond baking the mask into alpha.

### 03 - wall scuff and edge wear

| File | Size | Alpha | Source | Use |
|---|---|---|---|---|
| `03_wall_wear/wallscuff_scratches_color.png` | 2048x2048 | yes | ambientCG `SurfaceImperfections013` (2K-PNG) | Sharp abrasion, grit and light scuff marks on paint, doors and machine panels. |
| `03_wall_wear/wallscuff_scratches_opacity.png` | 2048x2048 | mask | same | Standalone mask. |
| `03_wall_wear/wallscuff_smears_color.png` | 2048x2048 | yes | ambientCG `Smear007` (2K-PNG) | Broad wipe/smear marks: cleaned-but-not-really walls, glass, laminate counters. |
| `03_wall_wear/wallscuff_smears_opacity.png` | 2048x2048 | mask | same | Standalone mask. |
| `03_wall_wear/wallscuff_strip_color.png` | 2048x512 | yes | **authored** from `SurfaceImperfections013` | Edge-wear strip: a 4:1 band that is strongest along its top edge and fades out, for door frames, counter noses and steps. |
| `03_wall_wear/wallscuff_strip_opacity.png` | 2048x512 | mask | same | Standalone mask. |

The strip is the only authored file in this folder: a 512 px horizontal band of
the CC0 scratch texture multiplied by a linear edge falloff (`gain 1.9`,
`edge_power 1.35`). Reproducible with `tools/build_decals.py`.

### 04 - rust and leak streaks

| File | Size | Alpha | Source | Use |
|---|---|---|---|---|
| `04_rust_leak/leakstreak_color.png` | 2048x1024 | yes | ambientCG `Leaking001` (2K-PNG) | Neutral water/detergent drips running down walls, glass and machine fronts. |
| `04_rust_leak/leakstreak_opacity.png` | 2048x1024 | mask | same | Standalone mask. |
| `04_rust_leak/leakstreak_normal.png` | 2048x1024 | no (A=1) | same (`NormalGL`) | Drip relief. |
| `04_rust_leak/ruststreak_color.png` | 2048x1024 | yes | **authored** from `Leaking004` + `Rust009` | Same idea tinted with the colour distribution of CC0 rust; for pipes, brackets, vent grilles, old bolts. |
| `04_rust_leak/ruststreak_opacity.png` | 2048x1024 | mask | same | Standalone mask. |

The rust tint is a plain multiply by the average colour of `Rust009`'s brighter
half (`[255, 150, 73]` in sRGB bytes), so the streak keeps its own brightness
variation. Alpha range 0.00-1.00.

### 05 - tileable speckle / dirt noise

| File | Size | Alpha | Source | Use |
|---|---|---|---|---|
| `05_speckle/speckle_mask_tileable_1k.png` | 1024x1024 | yes | **authored**, generated in `tools/build_decals.py` | Crumbs, lint, sand and dust for floors, benches and filter grilles. Alpha mirrors the mask so it works as a mask or as a transparent overlay. |

Seamless by construction (periodic value noise octaves plus wrap-stamped specks).
Measured wrap seam error 0.0115 against a 0.0128 mean neighbour difference, i.e.
indistinguishable from an interior edge.

### 06 - paper notices

| File | Size | Alpha | Source | Use |
|---|---|---|---|---|
| `06_notices/notice_lost_umbrella_1024x1408.png` | 1024x1408 | yes | **authored** over CC0 `Paper002` paper | Lost-property note taped to the wall near the waiting chairs. |
| `06_notices/notice_price_list_1024x1408.png` | 1024x1408 | yes | **authored** over CC0 `Paper002` paper | Price list near the counter or the entrance. |

Both are drawn by `tools/create_notices.ps1`: 11 price rows, taped sheet, aged
edges, photocopier ink bleed. Transparent outside the sheet and around the tape
strip, so they can sit as a flat quad 2-4 mm off a wall.

## Sources and licence

| Asset | Page | Download | Licence |
|---|---|---|---|
| `SurfaceImperfections001` | https://ambientcg.com/a/SurfaceImperfections001 | `https://ambientcg.com/get?file=SurfaceImperfections001_2K-PNG.zip` | CC0 1.0 |
| `SurfaceImperfections002` | https://ambientcg.com/a/SurfaceImperfections002 | `https://ambientcg.com/get?file=SurfaceImperfections002_2K-PNG.zip` | CC0 1.0 |
| `SurfaceImperfections013` | https://ambientcg.com/a/SurfaceImperfections013 | `https://ambientcg.com/get?file=SurfaceImperfections013_2K-PNG.zip` | CC0 1.0 |
| `Smear007` | https://ambientcg.com/a/Smear007 | `https://ambientcg.com/get?file=Smear007_2K-PNG.zip` | CC0 1.0 |
| `Leaking001` | https://ambientcg.com/a/Leaking001 | `https://ambientcg.com/get?file=Leaking001_2K-PNG.zip` | CC0 1.0 |
| `Leaking004` | https://ambientcg.com/a/Leaking004 | `https://ambientcg.com/get?file=Leaking004_2K-PNG.zip` | CC0 1.0 |
| `Leaking009` | https://ambientcg.com/a/Leaking009 | `https://ambientcg.com/get?file=Leaking009_2K-PNG.zip` | CC0 1.0 |
| `Rust009` (colour only) | https://ambientcg.com/a/Rust009 | `https://ambientcg.com/get?file=Rust009_1K-PNG.zip` | CC0 1.0 |
| `Paper002` (colour only) | https://ambientcg.com/a/Paper002 | `https://ambientcg.com/get?file=Paper002_1K-JPG.zip` | CC0 1.0 |

ambientCG licence terms: https://docs.ambientcg.com/license/ - CC0 1.0, commercial
use allowed, attribution not required. Attribution is still recorded here as good
practice. No CC BY-NC, no share-alike, no account-only sources were used.

Derived files (`wallscuff_strip`, `ruststreak`, `speckle_mask`, both notices) are
derivatives of the CC0 assets above and of original work by this project, so they
carry no additional restrictions.

Source zips and intermediate maps are **not** committed: about 274 MB of
downloadable 2K archives would only duplicate public files. Re-download from the
URLs above and regenerate with the commands below.

## Regeneration

The tools take the raw ambientCG maps as input, so download the zips, unpack the
`_Color` / `_Opacity` / `_NormalGL` PNGs into one folder, then:

```powershell
# decals: alpha baked in, rust tint, edge-wear strip, tileable speckle
& "C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe" `
    --background --factory-startup `
    --python ArtSource/Decals/tools/build_decals.py `
    -- --sources <folder-with-ambientCG-maps>

# paper notices (needs Paper002_1K-JPG_Color.jpg)
powershell -ExecutionPolicy Bypass -File ArtSource/Decals/tools/create_notices.ps1 `
    -Paper <folder>\Paper002_1K-JPG_Color.jpg

# verification: dimensions, alpha presence, tileability, plus optional contact sheet
& "C:\...\blender.exe" --background --factory-startup `
    --python ArtSource/Decals/tools/verify_decals.py -- `
    --preview <folder>\decal_contact_sheet.png
```

`verify_decals.py` currently reports **PASS (22/22 files ok)** on this set. It
exits non-zero on any size, alpha, or seam failure.

## Verified

Checked on 2026-11-01 with `tools/verify_decals.py` (Blender 4.5.15 background):

- All 22 image files open, decode, and match their expected pixel size.
- Every `_color` file has a real alpha channel with meaningful coverage; no file
  is fully transparent or fully opaque by accident.
- `_opacity` masks carry the same coverage as the alpha they were baked from.
- `_normal` maps are opaque and contain no NaN/inf values.
- `speckle_mask_tileable_1k.png` passes the wrap-seam test.

Not verified: appearance inside Unity, mip/compression settings, and in-scene
placement. No Unity or scene file was touched.

## Unity import notes (for whoever integrates this)

- `*_color.png`: Texture Type **Default**, sRGB **on**, Alpha Is Transparency **on**,
  then use Standard (Fade/Transparent) or a decal/unlit transparent shader.
- `*_opacity.png`, `speckle_mask_tileable_1k.png`: sRGB **off** (linear data).
- `*_normal.png`: Texture Type **Normal map**, sRGB **off**.
- Not tested for premultiplied alpha; the files are straight (non-premultiplied).
- Place decals 2-5 mm off the surface and scale UVs so the stain never repeats
  visibly at player eye height (1.7 m).

## Start with these two

1. `02_floor_grime/floorgrime_edge_color.png` - the floor is the largest uninterrupted
   surface in the room, so grime along the skirting instantly removes the "clean
   box" read.
2. `01_water_stain/waterstain_damp_color.png` - a few 0.6-1.2 m damp patches above and
   behind the machine wall sell the "these machines leak" story with no geometry.

`04_rust_leak/ruststreak_color.png` is the next biggest win once there is any
metalwork to stain.
