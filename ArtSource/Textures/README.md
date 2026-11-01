# Laundry Legends — Production PBR texture set (4K, CC0)

High-resolution physically based texture sets for the laundromat interior and the
Bali street frontage. Everything here is **CC0 1.0**, downloaded without an
account, and unzipped ready for import.

- **Source:** <https://ambientcg.com>
- **Licence:** Creative Commons CC0 1.0 Universal — commercial use allowed,
  modification allowed, redistribution allowed, attribution not required
  (<https://ambientcg.com/license>, checked 2026-11-01).
- **Downloaded:** 2026-11-01
- **Pack format:** ambientCG `_4K-JPG` ZIP (the PNG packs are 3–4× larger with no
  visible gain for game import).
- **Location:** `ArtSource/Textures/` — an art-source folder, **not** a Unity
  runtime folder. Nothing here is imported automatically; copy what a material
  needs into `Assets/LaundryLegends/Art/Textures/` deliberately.

## 1. Summary

| # | Folder | Asset ID | Resolution | Maps | Size | Use |
|---|---|---|---|---|---|---|
| 1 | `FloorTile_Tiles141/` | `Tiles141` | 4096×4096 | 4 | 49.78 MiB | Worn cream ceramic floor tile for the laundromat |
| 2 | `WallPlaster_PaintedPlaster017/` | `PaintedPlaster017` | 4096×2048 | 4 | 35.51 MiB | Cream painted plaster for walls and ceiling |
| 3 | `Wood_Wood092/` | `Wood092` | 4096×4096 | 4 | 39.14 MiB | Warm medium pine for counters and the folding table |
| 4 | `Metal_Metal009/` | `Metal009` | 4096×4096 | 5 | 45.85 MiB | Brushed steel for washer/dryer fronts |
| 5 | `Concrete_Concrete034/` | `Concrete034` | 4096×2048 | 4 | 23.22 MiB | Light grey concrete sidewalk and kerb |
| 6 | `Asphalt_Asphalt012/` | `Asphalt012` | 4096×4096 | 5 | 74.54 MiB | Cracked dark asphalt road |
| 7 | `Rubber_Rubber001/` | `Rubber001` | 4096×4096 | 4 | 71.64 MiB | Black rubber door mats and machine gaskets |

**Total: 356,175,469 bytes = 339.68 MiB across 31 map files** (well under the
~500 MB budget). No ZIP archives are retained in the repository, so this is the
complete on-disk footprint. See §4 for the archive hashes.

Two sets are 4096×2048 rather than square: ambientCG publishes those source
scans at 2:1, and 4096 px on the long edge is the resolution sold as "4K" for
those assets. They are not downscaled copies of a 4096² asset.

## 2. Per-set detail

### 1. Floor tile — `Tiles141`

- **Page:** <https://ambientcg.com/a/Tiles141>
- **Download:** <https://ambientcg.com/get?file=Tiles141_4K-JPG.zip> (200 OK, 72,783,974 bytes)
- **Licence:** CC0 1.0
- **Resolution:** 4096×4096
- **What it is:** off-white/beige ceramic tiles, ~150 mm grid, heavy grout
  darkening, scuffs and rust flecks. Reads as a small-tile commercial floor
  that has been mopped for ten years, which is exactly the laundromat note.
- **Continues the existing 1K choice:** the shipped `MAT_FloorTile` already uses
  `Tiles141` at 1K, so swapping in 4K needs no tiling or UV changes.
- **Maps:**

| File | Bytes | SHA-256 (first 16) |
|---|---|---|
| `Tiles141_4K_Color.jpg` | 19,910,777 | `aac39f8a734b1e6c` |
| `Tiles141_4K_NormalGL.jpg` | 18,865,226 | `dd5edfe09a02fcea` |
| `Tiles141_4K_Roughness.jpg` | 10,188,962 | `704f2be15edf13ca` |
| `Tiles141_4K_Displacement.jpg` | 3,232,040 | `2166983df256d10d` |

### 2. Painted plaster wall — `PaintedPlaster017`

- **Page:** <https://ambientcg.com/a/PaintedPlaster017>
- **Download:** <https://ambientcg.com/get?file=PaintedPlaster017_4K-JPG.zip> (200 OK, 58,386,608 bytes)
- **Licence:** CC0 1.0
- **Resolution:** 4096×2048
- **What it is:** white emulsion over hand-floated plaster — fine pitting,
  trowel drag and one hairline crack. Neutral enough to tint cream in the
  material, which is how the laundromat wall colour should be applied (keep the
  texture white, tint in the shader).
- **Maps:**

| File | Bytes | SHA-256 (first 16) |
|---|---|---|
| `PaintedPlaster017_4K_Color.jpg` | 7,420,337 | `87f8a4c71422605c` |
| `PaintedPlaster017_4K_NormalGL.jpg` | 19,618,831 | `2dfed1d0769e379f` |
| `PaintedPlaster017_4K_Roughness.jpg` | 5,123,983 | `0644d3151f1f9100` |
| `PaintedPlaster017_4K_Displacement.jpg` | 5,072,472 | `c83f8c1db141fe47` |

### 3. Wood — `Wood092`

- **Page:** <https://ambientcg.com/a/Wood092>
- **Download:** <https://ambientcg.com/get?file=Wood092_4K-JPG.zip> (200 OK, 59,980,885 bytes)
- **Licence:** CC0 1.0
- **Resolution:** 4096×4096
- **What it is:** clean, warm honey/orange pine with straight grain and a few
  knots. Reads as a varnished counter top or folding table, not a floor.
- **Deviation from the brief:** the previously used `WoodFloor064` is a dark
  parquet and the brief asked for a *warm medium* tone, so this set is new
  rather than an upgrade of the existing 1K map.
- **Maps:**

| File | Bytes | SHA-256 (first 16) |
|---|---|---|
| `Wood092_4K_Color.jpg` | 12,612,942 | `210419ce58dbb327` |
| `Wood092_4K_NormalGL.jpg` | 17,490,606 | `78092e3db3dd8f34` |
| `Wood092_4K_Roughness.jpg` | 5,128,571 | `6d2517f5c73a8205` |
| `Wood092_4K_Displacement.jpg` | 5,809,837 | `a30179b64b8df54a` |

### 4. Brushed metal — `Metal009`

- **Page:** <https://ambientcg.com/a/Metal009>
- **Download:** <https://ambientcg.com/get?file=Metal009_4K-JPG.zip> (200 OK, 70,046,122 bytes)
- **Licence:** CC0 1.0
- **Resolution:** 4096×4096
- **What it is:** brushed stainless steel with directional streaks and light
  scuffing. This is the only set that ships a Metalness map, which is correct
  because it is the only genuinely metallic surface.
- **Maps:**

| File | Bytes | SHA-256 (first 16) |
|---|---|---|
| `Metal009_4K_Color.jpg` | 9,536,319 | `3f6c8c6ca8289db1` |
| `Metal009_4K_NormalGL.jpg` | 20,580,492 | `61bfbe2ee019745f` |
| `Metal009_4K_Roughness.jpg` | 7,257,558 | `2abdd853150082b1` |
| `Metal009_4K_Metalness.jpg` | 65,697 | `087447bd30ce1ea5` |
| `Metal009_4K_Displacement.jpg` | 10,632,533 | `f67b4e092dd11482` |

### 5. Concrete — `Concrete034`

- **Page:** <https://ambientcg.com/a/Concrete034>
- **Download:** <https://ambientcg.com/get?file=Concrete034_4K-JPG.zip> (200 OK, 39,352,652 bytes)
- **Licence:** CC0 1.0
- **Resolution:** 4096×2048
- **What it is:** smooth light-grey poured concrete with faint formwork
  mottling and a small tie hole. Suitable for the sidewalk, kerb strip and the
  loading apron; tint slightly warm for tropical sun.
- **Maps:**

| File | Bytes | SHA-256 (first 16) |
|---|---|---|
| `Concrete034_4K_Color.jpg` | 4,946,238 | `ed84a6139a440a36` |
| `Concrete034_4K_NormalGL.jpg` | 13,651,639 | `0d2f42309380a115` |
| `Concrete034_4K_Roughness.jpg` | 4,050,854 | `b14bfff7971ded08` |
| `Concrete034_4K_Displacement.jpg` | 1,703,015 | `f152036e46ad33e7` |

### 6. Asphalt — `Asphalt012`

- **Page:** <https://ambientcg.com/a/Asphalt012>
- **Download:** <https://ambientcg.com/get?file=Asphalt012_4K-JPG.zip> (200 OK, 117,017,810 bytes)
- **Licence:** CC0 1.0
- **Resolution:** 4096×4096
- **What it is:** dark, coarse, alligator-cracked asphalt. The crack lines carry
  a faint rust-red staining; at road scale this reads as weathering, but drop
  the texture's saturation slightly in the material if it reads as red.
- **Extra map:** this asset also publishes an `Opacity` map (crack mask). It is
  kept because it is small and is the cleanest crack/edge mask for blending the
  road into the sidewalk. It is **not** wired into any Unity slot (Standard
  shader is opaque here); it is a source map for later decal/vertex work.
- **Maps:**

| File | Bytes | SHA-256 (first 16) |
|---|---|---|
| `Asphalt012_4K_Color.jpg` | 17,580,499 | `dd241be864c1736b` |
| `Asphalt012_4K_NormalGL.jpg` | 37,254,799 | `078cc5686f0203c3` |
| `Asphalt012_4K_Roughness.jpg` | 13,229,654 | `f75f96a458a71476` |
| `Asphalt012_4K_Displacement.jpg` | 8,593,994 | `c0746c89d7bddc91` |
| `Asphalt012_4K_Opacity.jpg` | 1,498,484 | `9333984025296a8f` |

### 7. Rubber — `Rubber001`

- **Page:** <https://ambientcg.com/a/Rubber001>
- **Download:** <https://ambientcg.com/get?file=Rubber001_4K-JPG.zip> (200 OK, 114,197,239 bytes)
- **Licence:** CC0 1.0
- **Resolution:** 4096×4096
- **What it is:** black crumb-rubber matting with fine white speckle. Use at a
  small tile scale for entrance mats, and at a large tile scale with the colour
  darkened for washer door gaskets.
- **Maps:**

| File | Bytes | SHA-256 (first 16) |
|---|---|---|
| `Rubber001_4K_Color.jpg` | 16,352,314 | `fe3f625392f054d1` |
| `Rubber001_4K_NormalGL.jpg` | 37,598,372 | `84ed8000963b2843` |
| `Rubber001_4K_Roughness.jpg` | 11,152,986 | `d12bbcbaddcaed92` |
| `Rubber001_4K_Displacement.jpg` | 10,015,438 | `0eeb882200e5cb8d` |

## 3. What was kept from each archive, and what was not

Each ambientCG ZIP contains more than the game needs. Extraction kept a
documented subset and discarded the rest; nothing else was altered — the JPEGs
are byte-for-byte the archive contents, only renamed from
`<ID>_4K-JPG_<Map>.jpg` to `<ID>_4K_<Map>.jpg`.

**Kept:** `Color`, `NormalGL`, `Roughness`, plus `Displacement` on every set and
`Metalness`/`Opacity` where published.

**Dropped, with reason:**

| Dropped file | Reason |
|---|---|
| `*_NormalDX.jpg` | DirectX (+Y down) duplicate of the normal map. Unity and Blender both use OpenGL (+Y up). Keeping both cost ~152 MB. Regenerate from `NormalGL` by inverting green if a DirectX consumer ever appears. |
| `*_4K-JPG.blend` | Per-set Blender material setup (~1 MB each). Not used by this project's material pipeline. |
| `*.mtlx`, `*.tres`, `*.usdc` | MaterialX / Godot / USD helper files. |
| `*.png` | ambientCG's page thumbnail render, not a PBR map. |

If any dropped file is needed later, re-download the ZIP from the URL in §2 and
confirm the SHA-256 in §4 before extracting.

## 4. Archive provenance (not retained)

The seven ZIPs were verified with `zipfile.testzip()` (all clean) and then moved
out of the repository to keep the art-source footprint under budget. They now
live in `%TEMP%\acg_texture_zips\` on the authoring machine and are **not**
versioned. To re-fetch:

| Archive | Bytes | SHA-256 |
|---|---|---|
| `Tiles141_4K-JPG.zip` | 72,783,974 | `7189714de3cea8e8d2dd3c7f72d724fc7f22423e045df7ff4e47c6e806979489` |
| `PaintedPlaster017_4K-JPG.zip` | 58,386,608 | `7843456207e8b5cac4b44d0583e46fead88c46c28a615895dd79341b24083f9e` |
| `Wood092_4K-JPG.zip` | 59,980,885 | `c54d3dd34323251195ab16a08e24fe894cb4eb45f7dd2c7975b56601a3a0a34a` |
| `Metal009_4K-JPG.zip` | 70,046,122 | `fe6a6d1ea178a5f5987dc258af8faea71f0357397ae7a89eca6fc4e2d678b03b` |
| `Concrete034_4K-JPG.zip` | 39,352,652 | `eb6cea24b345a11b3b6e74d6d76e3fa28bdcbe1904642292732d4eddf80d0577` |
| `Asphalt012_4K-JPG.zip` | 117,017,810 | `1d3816deefee4df5e1d63be8e3c1e144f9d7be380d1dfe23ed5a005f11b82638` |
| `Rubber001_4K-JPG.zip` | 114,197,239 | `40e39f8fac66558acf8e3c8e3c8acde1cded035cee8bcf3269c75bb437a112a8` |

Full per-map SHA-256 values, if ever needed, are printed by
`_tools/verify_textures.py`.

## 5. Verification performed

| Check | Command | Result |
|---|---|---|
| Files exist, are real JPEGs, carry the published 4K resolution, hash + size | `python ArtSource/Textures/_tools/verify_textures.py` | **PASS**, exit `0`, 31 maps, 7 sets. Every map is 4096 px on the long edge; the two 2:1 sets are 4096×2048 as published. |
| Archives are valid ZIPs | `zipfile.testzip()` over all 7 | **PASS**, all returned `None` (no CRC errors) |
| Each asset exists and its 4K-JPG link resolves | ambientCG `full_json` API per asset ID, then HTTP 200 from every `/get?file=` URL | **PASS** — all 7 IDs existed, no substitution was needed |
| Visual suitability of each pick | 512 px ambientCG material previews inspected before downloading | **PASS** — confirmed tile grid, plaster roughness, pine grain, brushed streaks, concrete tone, asphalt cracks, rubber speckle |
| Mean roughness per map (for the Smoothness fallback below) | `powershell -File ArtSource/Textures/_tools/sample_map_stats.ps1` | See §6.1 |
| Import into Unity | — | **NOT RUN.** Out of scope for this task and no `Assets/` change was requested. Unity was not launched. |
| Any check of the texture in-engine or in a render | — | **NOT RUN.** No claim is made that these look correct in the game yet. |

## 6. Unity import guidance

The project uses the **Built-In Render Pipeline with the Standard shader**, so the
slot mapping below is exact. All seven materials should use the same settings.

### 6.1 Map → Standard shader slot

| ambientCG map | Standard shader slot | Importer settings |
|---|---|---|
| `*_Color.jpg` | **Albedo** | Texture Type `Default`, **sRGB (Color Texture) ON**, Generate Mip Maps ON, Max Size 2048 or 4096, Compression `Normal Quality`. Tint cream here for the plaster wall. |
| `*_NormalGL.jpg` | **Normal Map** | Texture Type **`Normal map`**, **sRGB OFF**, Generate Mip Maps ON, Compression `Normal Quality`. **Uncheck "Create from Grayscale"** — the file is already an RGB tangent-space normal. Leave the material's normal strength at `1.0`. |
| `*_Roughness.jpg` | *no direct slot* (see below) | Texture Type `Default`, **sRGB OFF**, no mip maps needed if packed. |
| `*_Metalness.jpg` (`Metal009` only) | *no direct slot* (see below) | Texture Type `Default`, **sRGB OFF**. |
| `*_Displacement.jpg` | *no Standard slot* | Keep as source. Texture Type `Default`, sRGB OFF. Enables parallax/tessellation or Blender height, and is the input for baking AO. |
| `*_Opacity.jpg` (`Asphalt012` only) | *no slot* | Crack mask for decals/blending only. |

**Why roughness and metalness have no direct slot.** The Standard shader packs
metallic into **R** and smoothness into **A** of one *Metallic* texture, and
`Smoothness = 1 - Roughness`. Assigning either ambientCG grayscale map straight
into the Metallic slot would be wrong (a grey roughness map would be read as
partially metallic).

Three ways to handle it, in order of fidelity:

1. **Correct: pack a MetallicSmoothness map** (best). Create one texture per
   set where `R = Metalness` (0 for every non-metal set), `A = 255 - Roughness`,
   `G`/`B` unused (fill with 0 or copy R). Assign it to the **Metallic** slot
   and set the Metallic slider to `1.0` and Smoothness to `1.0` so the texture
   is not overridden. This is the only path that preserves the roughness
   variation that makes the 4K maps worth having.
   *Not done:* no verified channel-packing tool was available in this task.
   Windows PowerShell `System.Drawing` can do it without installing anything —
   see `_tools/` for the pattern — but that script does not exist yet and this
   guidance has not been proven in Unity.
2. **Acceptable interim: flat Smoothness slider.** Leave the Metallic slot
   empty, set Metallic `0` (except `Metal009`: `1`), and type the mean-roughness
   complement into Smoothness. Materials will read as one roughness level with
   the normal map still carrying the surface detail. Measured means:

   | Set | Mean roughness | Set the Smoothness slider to |
   |---|---|---|
   | `Asphalt012` | 0.805 | **0.20** |
   | `Rubber001` | 0.590 | **0.41** |
   | `Concrete034` | 0.516 | **0.48** |
   | `PaintedPlaster017` | 0.515 | **0.49** |
   | `Tiles141` | 0.512 | **0.49** |
   | `Wood092` | 0.482 | **0.52** |
   | `Metal009` | 0.469 | **0.53** (metallic `1.0`) |

3. **Never:** put the roughness JPEG in the Metallic slot. It will look like
   dirty chrome.

### 6.2 Import budget and warnings

- Import at **Max Size 2048** for game use unless a hero surface needs 4K. The
  4096² normal maps are 13–36 MB each as JPEG and become ~180 MB of VRAM at
  full resolution if imported uncompressed; the floor and machine fronts are
  the only defensible 4K imports. Keep the source 4K files here as the master.
- Set **Aniso Level 4–8** on the floor tile and the asphalt; they are viewed at
  grazing angles.
- Non-power-of-two is not an issue (all are powers of two); the two 2:1 maps
  import as 4096×2048 without warning.
- **AO:** none of these seven assets publishes an `AmbientOcclusion` map
  (verified against the archive contents, not assumed). The Standard shader's
  Occlusion slot should stay empty for now, or be filled with AO baked in Unity
  from `Displacement`/geometry. Do not invent an AO map from the roughness map.
- **Tiling:** the laundromat floor is 16×12 m. `Tiles141` is a dense small-tile
  grid, so set the material tiling so a single tile lands at roughly 150 mm, then
  check it in the Game view — the exact count was not measured here, only the
  relative scale. Set tiling on the material, never by resizing the texture.
- **Licence hygiene:** these are CC0, so no attribution is required in-game. The
  credit line ambientCG suggests, if credit is ever added:
  `Created using <asset name> from ambientCG.com, licensed under CC0 1.0.`

## 7. Known gaps

- No AO map is published for these assets; occlusion is not available yet.
- Roughness/Metalness are not packed into a Unity-ready MetallicSmoothness map.
  Until they are, materials use the flat Smoothness values in §6.1.
- The textures have not been imported into Unity or seen in a render. This
  document contains no claim of visual acceptance.
- `Assets/` was not modified by this task. The existing 1K sets shipped under
  `Assets/LaundryLegends/Art/Textures/Environment/` (`Tiles141`,
  `PaintedPlaster017`, `WoodFloor064`) are untouched and still what the game
  renders today.
- The empty `ArtSource/Downloads/_texture_zips/` folder remains (the ZIPs were
  moved out of it); it can be removed by a human or the lead agent.

## 8. Tooling in this folder

- `_tools/verify_textures.py` — reads each JPEG header, asserts the 4K
  resolution, prints size and SHA-256, exits `1` on any failure.
  Run: `python ArtSource/Textures/_tools/verify_textures.py`
- `_tools/sample_map_stats.ps1` — mean roughness per set, used for the
  Smoothness fallback table.
  Run: `powershell -ExecutionPolicy Bypass -File ArtSource/Textures/_tools/sample_map_stats.ps1`
