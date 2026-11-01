# Signage artwork

Finished sign panels for the laundromat and the hidden arcade. These replace the
legacy `TextMesh` strings that read as placeholder text floating in space: each
file is a complete board (painted panel, frame, rules, rivets and set type) that
can be dropped straight onto a quad.

Nothing here has been imported into `Assets/`; that stays with the Unity owner.

## Files

| File | Pixels | Aspect | World size if 1 unit = 1 m | Used for |
| --- | --- | --- | --- | --- |
| `sign_facade.png` | 2048 x 384 | 16:3 | 8.0 x 1.5 m | Exterior fascia above the storefront: **LAUNDRY LEGENDS**, subtitle **LAUNDROMAT · OPEN 7–22** |
| `sign_dropoff.png` | 1024 x 256 | 4:1 | 4.0 x 1.0 m | Drop-off counter sign, sits above the counter at eye level |
| `sign_machines.png` | 2048 x 256 | 8:1 | 8.0 x 1.0 m | Wall band over the machine wall: **WASH · DRY · FOLD** |
| `sign_open.png` | 512 x 256 | 2:1 | 2.0 x 1.0 m | Hanging door sign on the entrance glass: **OPEN 7–22** |
| `sign_staff.png` | 512 x 256 | 2:1 | 2.0 x 1.0 m | Service / utility door: **STAFF ONLY**, secondary **NO ENTRY** |
| `sign_arcade.png` | 1024 x 256 | 4:1 | 4.0 x 1.0 m | Neon sign for the secret arcade room: **ARCADE** |

All six are 8-bit RGBA PNG (`color type 6`, `bit depth 8`). The board fills the
frame exactly, so the PNG can also be used as an opaque texture with no alpha cut.

`create_signs.py` generates them, `verify_signs.py` reads them back and checks
size, frame coverage and contrast.

The brand logo suite (`logo_mark.png`, `logo_wordmark.png`, `logo_shirt.png`,
`sign_facade_logo.png`) lives in this folder too and is documented separately in
`logo_README.md`; it reuses this palette and the same two fonts.

## Typography

| Role | Font | File | Used in |
| --- | --- | --- | --- |
| Display | **Impact Regular** | `C:/Windows/Fonts/impact.ttf` | Every headline |
| Secondary | **Arial Narrow Bold** | `C:/Windows/Fonts/ARIALNB.TTF` | Facade subtitle, `NO ENTRY` |

Both are Microsoft Windows system fonts, used here only to rasterise glyphs into
a bitmap; no font file is copied into the repository and none ships with the
game. If the project ever needs redistributable *font* files (localisation,
in-game text, UI), do not reuse these - switch to Blender's built-in Bfont or an
OFL face such as Archivo Narrow / Barlow Condensed.

Type is set all-caps with positive tracking (`space_character` 1.05-1.45) and is
converted from a text object to a mesh before rendering, so kerning and tracking
are baked into the texture. Cap heights on the 256 px-per-unit grid:

| Sign | Headline cap height | Smallest secondary type |
| --- | --- | --- |
| facade | 179 px | 38 px subtitle |
| dropoff | 118 px | - |
| machines | 138 px | - |
| open | 95 px | 48 px hours |
| staff | 53 px | 29 px `NO ENTRY` |
| arcade | 115 px | - |

Thinnest painted element anywhere is a 5 px rule; no element is a 1 px hairline,
and Impact's stems are 25 px or wider at the smallest size.

## Palette

| Hex | Name | Where |
| --- | --- | --- |
| `#1C2333` | navy | facade board, dropoff frame, open frame, all display ink |
| `#27324A` | navy mottle | weathering on navy boards |
| `#F3E6CB` | cream | facade frame, machines type, dropoff board |
| `#E2CFA9` | cream mottle | weathering on cream |
| `#CBB894` | dim cream | facade subtitle |
| `#E63A81` | magenta | accent rules, separators, `OPEN 7–22` |
| `#FF2E88` | neon magenta | arcade tube |
| `#3FD4DF` | cyan | arcade keyline and tube |
| `#EDA94F` | amber | open-sign board |
| `#25443F` | teal | machines board |
| `#B7B1A2` / `#4C4740` / `#35312B` | muted board / frame / ink | staff sign |
| `#0C0B16` | near-black indigo | arcade board |

Boards use an emission material with a low-frequency noise mottle so painted
surfaces read as weathered rather than as flat colour swatches.

## How it is rendered

Blender 4.5.15 LTS. Each sign is a flat 2D stack of emission-shaded meshes in the
XY plane (board at z=0, frame/board inset z=0.002, rules z=0.012, type z=0.02), a
single orthographic camera on -Z at `ortho_scale = board width`. There are no
lamps, no world light and no floor, so nothing casts a shadow, and film
transparency plus the Standard view transform mean a hex value written in the
script lands in the PNG unchanged.

Render settings: Cycles, CPU, 96 samples, adaptive sampling off, denoise off,
filter width 1.6, max bounces 1, dither 0, PNG compression 60. The arcade sign
adds a compositor Fog Glow node (threshold 0.32, size 9, mix -0.15) for the neon
bloom.

Every sign is authored on the same grid: **1 world unit = 256 px**, so the type
sizes above are identical across all six files.

## Exact commands

Render all six (from the repository root):

```powershell
& "C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe" --background --factory-startup --python "ArtSource\Signs\create_signs.py"
```

Render one sign (useful while iterating):

```powershell
& "C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe" --background --factory-startup --python "ArtSource\Signs\create_signs.py" -- --only sign_open
```

Verify the rendered PNGs (dimensions, frame coverage, contrast):

```powershell
& "C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe" --background --factory-startup --python "ArtSource\Signs\verify_signs.py"
```

Each run is a full rebuild from an empty scene; the script writes only the six
PNGs next to itself and overwrites them.

## Verification (2026-11-01)

Layout audit printed by `create_signs.py` - world units of clearance between the
ink of every text block and the edge of the frame:

| Sign | Tightest text margin | Headline ink width |
| --- | --- | --- |
| facade | 0.165 u (42 px) | 6.68 u (1710 px) |
| dropoff | 0.160 u (41 px) | 2.66 u (681 px) |
| machines | 0.230 u (59 px) | 6.68 u (1710 px) |
| open | 0.138 u (35 px) | 0.97 u (248 px) |
| staff | 0.173 u (44 px) | 1.26 u (323 px) |
| arcade | 0.200 u (51 px) | 2.29 u (586 px) |

`verify_signs.py` result `PASS` for all six: exact pixel dimensions, 100.00 %
opaque pixels, all four corners alpha 1.0, no transparent or stray-shadow pixels.
Contrast between the two dominant colours of each file:

| Sign | Board / ink pair | Contrast |
| --- | --- | --- |
| facade | cream `#F3E6CB` on navy `#1C2333` | 90.8:1 |
| dropoff | navy on cream | 90.8:1 |
| machines | cream on teal `#25443F` | 22.8:1 |
| open | navy on amber `#EDA94F` | 51.7:1 |
| staff | ink `#35312B` on muted `#B7B1A2` | 8.8:1 |
| arcade | neon magenta on indigo `#0C0B16` | (bloom-affected, >100:1) |

Visual check: each PNG was opened and inspected at full size, then downsampled
to 25 % (a distance proxy) and re-inspected. All six read correctly at quarter
size - headline legible, subtitle still resolvable on the facade, `NO ENTRY`
legible but intentionally the quietest line. Centring and margins were confirmed
both by eye and by the numeric audit above.

## Known limitations

- The arcade glow is baked into the texture. If the arcade is lit with emissive
  materials later, the baked bloom will double up; render without `glare` instead.
- Flat texture only - no normal map, roughness or metalness variant. Rivets and
  frames are painted, not modelled.
- Type is baked to pixels; resizing a sign in Unity scales the texture, and the
  words are not localisable without a re-render.
- The facade subtitle is 38 px cap height. It is legible at 2 m on a 3-4 m sign,
  but it is deliberately the smallest element on that board.
