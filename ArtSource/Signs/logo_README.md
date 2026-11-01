# Laundry Legends logo suite

Four logo files built the same way as the sign set: flat 2D geometry rendered by
Blender with emission shading, an orthographic camera and a transparent film. No
lamps, no shadows, no compositing tricks. Three of the four are transparent PNGs;
the fourth is the storefront fascia.

Nothing here has been imported into `Assets/`. Copying these into Unity stays with
the Unity owner.

## Files

| File | Pixels | World size (1 u = 256 px) | Background | Used for |
| --- | --- | --- | --- | --- |
| `logo_mark.png` | 1024 x 1024 | 4.0 x 4.0 u | transparent | The emblem alone: app icon, sticker, machine decal, social avatar, any square lockup |
| `logo_wordmark.png` | 2048 x 512 | 8.0 x 2.0 u | transparent | Horizontal lockup for dark grounds: menus, invoices, uniforms, signage where a board already exists |
| `logo_shirt.png` | 1024 x 1024 | 4.0 x 4.0 u | transparent | Single-colour garment print (polo, tee, apron, tote) |
| `sign_facade_logo.png` | 2048 x 384 | 8.0 x 1.5 u | opaque board | Storefront fascia revision - same frame, board and rivets as `sign_facade.png`, so it drops straight onto the existing quad |

All four are 8-bit RGBA PNG (`color type 6`). `create_logo.py` generates them,
`verify_logo.py` reads them back and writes the downscaled previews.

## The mark

A front-on washing-machine drum read as a porthole badge:

- cream outer ring around a navy field,
- cream drum ring around a teal glass,
- magenta water with a cream wave crest across the bottom of the glass,
- a five-petal cream frangipani floating above the water.

Every shape is a polygon or an annulus built from arithmetic - there is no
boolean, no image texture and no traced artwork. The water is the region of the
circle below a sine curve, clipped by solving where the curve meets the drum;
the crest is the band between that curve and the same curve raised 24 px, so the
two share an edge and there is no seam. The frangipani is five overlapping
obovate ellipses plus a centre disc.

Geometry as a fraction of the badge radius `R`, and in pixels at 1024:

| Element | Fraction of R | Pixels (1024 file) |
| --- | --- | --- |
| Badge (cream ring outside) | 1.000 R | 992 dia |
| Outer ring thickness | 0.089 R | 44 |
| Navy field | 0.911 R | 904 dia |
| Drum ring outside / inside | 0.645 R / 0.516 R | 640 / 512 dia |
| Drum ring thickness | 0.129 R | 64 |
| Glass | 0.516 R | 512 dia |
| Water mean line | -0.40 glass | -102 from centre |
| Wave amplitude | 0.12 glass | 31 |
| Crest thickness | 0.092 glass | 24 |
| Frangipani | 0.36 glass, centred +0.38 glass | 184 dia |

Sizes were chosen so the badge still reads when it is 64 px across, where 1 px
equals 16 design pixels: outer ring 2.8 px, drum ring 4 px, crest 1.5 px,
frangipani 12 px. See `_qa/logo_mark_64.png`.

The single-colour version drops the field and the glass, so the sheet is cream
where the rings, the water and the flower are and completely clear everywhere
else. It is the minimum that still says "washing machine".

## Layout

| File | Element | Ink box (world units) | Pixels |
| --- | --- | --- | --- |
| wordmark | mark | x -3.790..-2.450, y -0.650..0.690 | 343 dia |
| wordmark | LAUNDRY LEGENDS | x -2.305..3.745, y -0.077..0.557 | 1549 x 162 |
| wordmark | rule | x -1.030..2.470, y -0.275..-0.245 | 896 x 7.7 |
| wordmark | LAUNDROMAT | x -0.193..1.633, y -0.578..-0.422 | 467 x 40 |
| shirt | mark | x -1.210..1.210, y -0.945..1.475 | 620 dia |
| shirt | LAUNDRY LEGENDS | x -1.422..1.422, y -1.475..-1.185 | 728 x 74 |
| facade | mark | x -3.580..-2.560, y -0.510..0.510 | 261 dia |
| facade | LAUNDRY LEGENDS | x -2.280..3.580, y -0.097..0.517 | 1500 x 157 |
| facade | rule | x -0.950..2.250, y -0.214..-0.186 | 819 x 7.2 |
| facade | LAUNDROMAT | x -0.204..1.504, y -0.513..-0.368 | 437 x 37 |

Tightest clearance from the artwork to the edge of its own canvas: `logo_mark`
16 px, `logo_wordmark` 54-65 px, `logo_shirt` 134 px, `sign_facade_logo` 60 px.
Nothing is clipped; the audit that proves it runs on every build.

## Palette

| Hex | Name | Where |
| --- | --- | --- |
| `#1C2333` | navy | facade board, shirt/polo ground reference |
| `#212A3E` | navy field | inside the badge - a hair lighter than the board so the mark reads as a disc, not a hole |
| `#27324A` | navy mottle | weathering on the facade board only |
| `#F3E6CB` | cream | rings, type, crest, frangipani, and the only ink in `logo_shirt.png` |
| `#CBB894` | dim cream | `LAUNDROMAT` in both lockups |
| `#E63A81` | magenta | water, rule |
| `#2E7D7A` | teal | drum glass |
| `#3A4A6B` | rivet | facade board corners |

The brief's teal `#2E7D7A` is used rather than the darker `#25443F` that
`sign_machines.png` uses. The brighter teal holds its colour when the glass is
only 30 px across; `#25443F` greys out into the navy. That is a deliberate,
visible difference from the older sign.

Contrast: cream on the badge field is 58.7:1, cream on the facade board 90.8:1.
The mark and the type are not relying on subtlety for legibility.

## Typography

| Role | Font | File |
| --- | --- | --- |
| `LAUNDRY LEGENDS` | **Impact Regular** | `C:/Windows/Fonts/impact.ttf` |
| `LAUNDROMAT` | **Arial Narrow Bold** | `C:/Windows/Fonts/ARIALNB.TTF` |

Exactly the two faces used by `sign_facade.png`, at the same positive tracking
(Impact `space_character` 1.08, Arial Narrow 1.46), so the two facades set the
same type. Both are Microsoft Windows system fonts used only to rasterise glyphs
into a bitmap; no font file is copied into the repository or ships with the game.
If the project ever needs redistributable font files, switch to an OFL face such
as Barlow Condensed.

Text objects are converted to meshes before rendering, so kerning and tracking
are baked into the pixels.

## How it is rendered

Blender 4.5.15 LTS, Cycles on CPU, 128 samples, adaptive sampling off, denoise
off, filter width 1.5, max bounces 1, dither 0, PNG compression 60, film
transparent, Standard view transform. A hex value written in the script lands in
the PNG unchanged. One orthographic camera on -Z with `ortho_scale` equal to the
canvas width; every sign is authored at 256 px per world unit, matching the sign
set.

Layers sit on separate Z planes 0.004 apart (field 0.000, outer ring 0.004,
drum ring 0.008, glass 0.012, water 0.016, crest 0.020, flower 0.024, type 0.03
to 0.06) so nothing z-fights.

```powershell
# render all four
& "C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe" --background --factory-startup --python "ArtSource\Signs\create_logo.py"

# render one while iterating
& "...\blender.exe" --background --factory-startup --python "ArtSource\Signs\create_logo.py" -- --only logo_mark

# verify + write previews into ArtSource/Signs/_qa
& "...\blender.exe" --background --factory-startup --python "ArtSource\Signs\verify_logo.py"
```

Each run is a full rebuild from an empty scene and overwrites only the files it
owns.

## Verification (2026-11-01)

`create_logo.py` layout audit, all text and emblem boxes inside their canvas:

| File | Tightest margin |
| --- | --- |
| `logo_mark.png` | +0.062 u (16 px) |
| `logo_wordmark.png` | +0.210 u (54 px) |
| `logo_shirt.png` | +0.525 u (134 px) |
| `sign_facade_logo.png` | +0.233 u (60 px) |

`verify_logo.py` result `PASS` for all four:

| File | Size | Opaque | Partial | Clear | Corner alpha |
| --- | --- | --- | --- | --- | --- |
| `logo_mark.png` | 1024x1024 | 73.36 % | 0.65 % | 25.99 % | 0.0 (all four) |
| `logo_wordmark.png` | 2048x512 | 22.81 % | 2.84 % | 74.36 % | 0.0 (all four) |
| `logo_shirt.png` | 1024x1024 | 13.65 % | 2.15 % | 84.21 % | 0.0 (all four) |
| `sign_facade_logo.png` | 2048x384 | 100.00 % | 0.00 % | 0.00 % | 1.0 (all four) |

The three transparent files are provably transparent - every corner is alpha 0
and no background pixel is opaque. `logo_shirt.png` contains exactly **one**
opaque ink, `#F3E6CB`, which is the single-colour requirement met literally
rather than by eye.

Visual check, all inspected as PNGs at full size and at reduced size:

| Preview | Shows |
| --- | --- |
| `_qa/logo_mark_256.png` | badge at icon size - rings, glass, wave, flower all distinct |
| `_qa/logo_mark_64.png` | badge at 64 px - still reads as a porthole with water; the frangipani is a single cream blob |
| `_qa/logo_wordmark_512.png` | lockup at quarter size - `LAUNDRY LEGENDS` fully legible, `LAUNDROMAT` still readable |
| `_qa/logo_wordmark_on_navy.png` | lockup flattened on `#1C2333` - the way it will actually be used |
| `_qa/logo_shirt_on_navy.png` | shirt artwork flattened on `#1C2333`, because cream on white is not a fair test |
| `_qa/logo_shirt_96.png` | print artwork at 96 px as a distance proxy |
| `_qa/sign_facade_logo_512.png` | fascia at quarter size - the sign still reads across a street |

## Known limitations

- **Raster only.** The mark is not a vector. It is authored at 1024 and will
  soften past roughly 2048 px across; re-render at a larger canvas rather than
  upscaling.
- **Cream on transparent.** `logo_wordmark.png` and `logo_shirt.png` are single
  light inks with no background. They are built for dark grounds. On white paper
  they will nearly disappear; use `sign_facade_logo.png`, or set `SHIRT_INK` in
  `create_logo.py` to `"1C2333"` (light garment) or `"FFFFFF"` (white print) and
  re-render.
- **The frangipani is an approximation.** Five ellipses, not a botanical petal
  silhouette, and it is the first element to disappear below about 48 px. The
  badge still reads as a porthole without it, by design.
- **The mark sits flat on the fascia.** Its field is `#212A3E` over a mottled
  `#1C2333`/`#27324A` board, so under close inspection the badge is a slightly
  lighter, unweathered disc. That is intentional (it makes the badge read), but
  it is a printed-badge look, not a weathered painted circle.
- **The wave is baked.** There is no animation, no shader and no separate water
  layer; if the mark ever needs to look like it is sloshing, this file cannot be
  reused for it.
- **The facade has no `OPEN 7-22` line.** It carries the lockup only, because
  `sign_open.png` already handles opening state. Swapping the boards loses the
  hours from the fascia.
- **Type is pixels.** Words are not localisable, and re-wrapping `LAUNDRY LEGENDS`
  means re-rendering.
- **No print profile.** The shirt file is sRGB, not CMYK; a screen printer will
  want to recolour the ink to their stock anyway, which is why it is one flat
  colour.
- **Not reviewed by the project owner.** All checks above are agent-run.
