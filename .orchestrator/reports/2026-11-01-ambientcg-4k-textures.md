# Worker report — 4K CC0 PBR texture sets (ambientCG)

**Date:** 2026-11-01
**Task:** Download production-grade CC0 PBR texture sets for the laundromat interior and Bali street
**Scope:** `ArtSource/` only. `Assets/` untouched. Unity not launched.
**Worker:** subagent (asset acquisition)

## Objective

Replace/extend the 1K ambientCG maps with a consistent 4K PBR library covering
floor tile, painted plaster, wood, brushed metal, concrete, asphalt, and rubber,
with verified provenance, licences, resolutions, and Unity import guidance.

## Assets created

Seven sets, 31 map files, **356,175,469 bytes (339.68 MiB)** under
`ArtSource/Textures/`:

| Folder | Asset | Resolution | Maps | MiB |
|---|---|---|---|---|
| `FloorTile_Tiles141` | Tiles141 | 4096x4096 | Color, NormalGL, Roughness, Displacement | 49.78 |
| `WallPlaster_PaintedPlaster017` | PaintedPlaster017 | 4096x2048 | Color, NormalGL, Roughness, Displacement | 35.51 |
| `Wood_Wood092` | Wood092 | 4096x4096 | Color, NormalGL, Roughness, Displacement | 39.14 |
| `Metal_Metal009` | Metal009 | 4096x4096 | Color, NormalGL, Roughness, Metalness, Displacement | 45.85 |
| `Concrete_Concrete034` | Concrete034 | 4096x2048 | Color, NormalGL, Roughness, Displacement | 23.22 |
| `Asphalt_Asphalt012` | Asphalt012 | 4096x4096 | Color, NormalGL, Roughness, Opacity, Displacement | 74.54 |
| `Rubber_Rubber001` | Rubber001 | 4096x4096 | Color, NormalGL, Roughness, Displacement | 71.64 |

All CC0 1.0, no account, no cost.

## Files created / modified

Created:
- `ArtSource/Textures/README.md` — provenance, hashes, verification, import guidance
- `ArtSource/Textures/<7 sets>/*.jpg` — 31 maps
- `ArtSource/Textures/_tools/verify_textures.py` — header/dimension/hash validator
- `ArtSource/Textures/_tools/sample_map_stats.ps1` — mean roughness sampler

Modified:
- `ArtSource/Licenses/ambientcg.md` — added the seven 4K sets and their URLs
- `ArtSource/README.md` — added `Textures/` to the layout list

Deleted: none inside the repository. The seven source ZIPs were moved to
`%TEMP%\acg_texture_zips\` (hashes recorded in the README) after extraction,
because the shell policy blocks delete commands and keeping both copies would
have doubled the footprint to ~847 MiB. An empty
`ArtSource/Downloads/_texture_zips/` folder remains.

## Checks performed

| Check | Result |
|---|---|
| ambientCG API `full_json` lookup per asset ID; every `/get?file=` URL returned HTTP 200 | PASS — 7/7 IDs existed, no substitution needed |
| `zipfile.testzip()` on all 7 archives | PASS — no CRC errors |
| `python ArtSource/Textures/_tools/verify_textures.py` | PASS, exit 0 — 31 maps, all valid JPEGs, 4096 px long edge, per-map SHA-256 printed |
| Visual suitability (512 px ambientCG previews inspected) | PASS — tile grid, plaster mottle, pine grain, brushed streaks, concrete tone, asphalt cracks, rubber speckle all match the brief |
| Licence terms (ambientcg.com/license fetched) | PASS — "All ambientCG assets are provided under the CC0 1.0 Universal License", commercial use and raw-file redistribution allowed |
| Repository footprint vs ~500 MB budget | PASS — 339.68 MiB |

## Checks not performed

- **No Unity import, no material assignment, no render.** `Assets/` was not
  touched and Unity was not launched, per the task boundary.
- **No gameplay or visual acceptance.** The README explicitly makes no claim
  that these textures look correct in the game.
- **No AO map.** Not published by ambientCG for any of the seven assets (verified
  from archive contents, not assumed).
- **No packed MetallicSmoothness map.** No verified channel-packing tool was
  available; the README documents the packing recipe and interim flat
  Smoothness values instead.

## Git

No commit, no stage, no branch change (subagent rule). `git status` shows
`?? ArtSource/Textures/` plus the two modified ArtSource docs. The tree also
contains unrelated pre-existing modifications from other work (e.g.
`Assets/**/*.mat`, `.gitignore`, `.opencode/opencode.json`) that this task did
not touch.

## Notes for the lead

1. `Tiles141` and `PaintedPlaster017` are the same assets already shipped at 1K,
   so they can be upgraded in place without changing UVs or tiling.
2. `Wood092` **replaces** `WoodFloor064` as the intended wood: the brief asked
   for warm medium tone and `WoodFloor064` is a dark parquet. The shipped
   material still references the old 1K map.
3. `Metal009` is the only metallic surface; all other sets should use Metallic
   `0`.
4. Copying these into `Assets/` at full 4K would be a significant VRAM commitment
   (normal maps alone are 13–36 MB JPEG each). Recommend Max Size 2048 for
   general surfaces and 4K only for the floor.
5. `ASSET_INVENTORY.md` has not been updated — out of scope here.

## Recommended next step

Assign the sets to the existing Standard materials in an `Assets/` task, importing
at Max Size 2048 with the correct sRGB/normal-map settings from
`ArtSource/Textures/README.md` §6, then capture the interior and street in the
Game view for owner review. Pack MetallicSmoothness maps at the same time if the
roughness variation is wanted.
