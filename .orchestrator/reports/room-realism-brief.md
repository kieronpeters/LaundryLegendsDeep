# Room Realism Brief — Bali Laundromat

- **Status:** proposal. Nothing in this document exists as an asset.
- **Written:** 2026-11-01, workstream W4 (ll-designer). Documentation only; no code, scene, prefab, package, or Unity run.
- **Verified by inspection:** `GreyboxRoom.prefab` (YAML object names, light and material values), `RoomRemodelTool.cs`,
  `DirtyEnvironmentTool.cs`, `StorefrontFrontageTool.cs`, `MAT_FloorTile`/`MAT_WallPaint`/`MAT_CounterWood`/
  `MAT_Appliance_Metal`/`Grey_Glass`, `QualitySettings.asset`, `Laundromat.unity` render settings,
  `Packages/manifest.json`, and the captures `TestResults/phase2-waiting.png`, `phase3-machine.png`,
  `frontage-entrance.png`, `frontage-outside.png`.
- **Not verified:** texel density per surface, lightmap bake cost, performance, the look of any prop listed below,
  CC0 availability of the street prop, and how the owner judges "good enough".

## 1. Current state, honestly

The room is a 16 × 12 m box, 3.3 m to the ceiling, built from scaled cubes in `GreyboxSetupTool`. Floor, walls, and
ceiling use three CC0 ambientCG 1K sets (Tiles141, PaintedPlaster017, WoodFloor064) on the Built-In Standard shader.
Smoothness is uniform per material: floor 0.25, walls 0.08, wood 0.20; metallic is 0 everywhere, including
`MAT_Appliance_Metal`, so stainless reads as grey plastic. `RoomRemodelTool` adds about 30 more cubes (baseboards,
crown, wainscot, aprons, cushions, drawers, handles). `DirtyEnvironmentTool` adds three flattened sphere "dirt" piles,
a mop, a bucket, and five wall scuffs. Everything else — counter, table, two chairs, side table, planter (sphere on
cylinder), shelving, utility cabinet and sink, secret-door cover, detergent bottles, cart, signs — is primitive or
simple extruded geometry. Eight 808-triangle appliances are the most detailed objects in the room. The exterior is a
20 m sidewalk, 20 m road, curb, far walk, a 20 × 4.4 m slab, and a backdrop plane. Lighting is four point lights at
intensity 2.6, range 14, warm (1, 0.92, 0.8), two with shadows, plus one directional at 0.55. Ambient is flat 0.24.
Fog is off. There are no reflection probes and no post-processing package; Medium quality allows 2 per-pixel lights.

Three things read as placeholder on screen:

1. **Light does not pool or fall off.** Range-14 lights reach every surface, so the room has no dark corners, while
   the ceiling — lit from below only — renders near black. There are no visible fixtures. Nothing looks like a place
   with light sources; it looks like a model with a default light.
2. **Every surface is a hard-edged box with one uniform response.** Chairs are boxes with thin offset legs, the plant
   is a sphere, floor and wall meet with no contact darkening, and machines sit on the tile with no shadow. Because
   smoothness never varies and there is no probe to reflect, glass, tile, and metal differ only in albedo.
3. **Type and the view outside.** Legacy `TextMesh` signs float, clip, and appear mirrored (`LAUNDRY`, `ONLY`), and
   the window opens onto a flat gradient with plain slabs. That single view tells the player it is a test scene.

Also true but secondary: the floor shows one 1K tile image once across 16 m (material tiling 1,1), so tiles are
mushy at walking distance; the machine wall is a flat grey slab with no shelf, price strip, or wear; the room is
sparse enough that the eye has nothing to rest on between machines and counter.

## 2. Target look

A neighborhood laundromat on a humid coastal street at dusk. Air is warm and slightly heavy; the glass is not clean
and there is a faint haze near the ceiling. The floor is worn cream tile, darker where feet and machines meet it, with
one or two cracked tiles and a rubber mat at the door. Walls are faded cream plaster above a scuffed teal-green
wainscot, hung with a hand-written price board, a "no smoking" sign, and a notice for a lost umbrella. Stainless
machine fronts catch the window and the tubes; the machines are mismatched but tidy, with powder boxes and a plastic
bottle on a shelf above them. Two plastic stack chairs wait by the window, a laundry rack of white sheets hangs near
the utility corner, and a fan turns slowly. The mop sink is stained, the bucket is never quite empty. Outside, a parked
scooter, a warm street lamp, a neighboring shutter, and a wet road. Light comes from two sources that disagree: cool
blue-green dusk through the windows, and warm 3000K tubes overhead. That disagreement is the look.

## 3. Prioritized prop/asset list

Ordered by visual impact per hour of work, highest first. Sizes are metres (W × D × H). "Blender" means a scripted
Blender mesh like `create_basket.py`, not a hand-sculpted hero asset. Triangle budgets are proposals, not measurements.

| # | Item | Size (m) | Material notes | Tris | Route |
|---|---|---|---|---|---|
| 1 | Fluorescent batten fixture + diffuser | 1.25 × 0.16 × 0.10 | White powder-coated steel housing; milky acrylic diffuser, emission 1.6, 3000K | 80–140 | Unity primitives + emissive material |
| 2 | Contact/grime decal set (6–10 quads) | 0.4–3.0 spans | Alpha-blended, dark warm grey; generated gradient + noise, not painted | 2 each | Unity quads; texture from a C# generator |
| 3 | Window vinyl + price/hours lettering | 0.2–1.2 wide | White vinyl on glass, 90% opacity, slight dirt at edges | 2 per quad | Unity quads + TextMeshPro |
| 4 | Detergent shelf above machines | 2.6 × 0.35 × 0.30 | Powder-coated steel, small lip, 3 brackets; smoothness 0.35 | 200–350 | Blender or box primitives with bevels |
| 5 | Plastic stack chair | 0.45 × 0.50 × 0.80 | Single-color polypropylene, smoothness 0.55–0.65, no metallic; 3 colors | 350–600 | Blender (curved seat and back) |
| 6 | Hanging laundry rack + 2 sheets/towels | 1.8 × 0.60 × 1.70 | Thin chrome tube (metallic 0.8) + cloth planes, smoothness 0.05, double-sided | 250 + 200 | Blender |
| 7 | Detergent/softener bottle cluster (8–12) | 0.10–0.30 tall | 3 shared meshes; coloured HDPE, smoothness 0.45; paper labels | 60–180 each | Blender, one bottle, instanced |
| 8 | Entrance mat | 1.4 × 0.02 × 0.90 | Dark coir, smoothness 0.05, worn centre | 4–12 | Unity quad + generated texture |
| 9 | Janitor / mop sink | 0.60 × 0.55 × 0.90 | Glazed ceramic, smoothness 0.35, grime at base; chrome tap | 250–400 | Blender |
| 10 | Coin/soap vending machine | 0.90 × 1.55 × 0.40 | Painted steel (metallic 0.2), glass front, chrome slot | 500–800 | Blender |
| 11 | Rolling laundry cart | 0.90 × 0.60 × 1.00 | Chrome tube frame, canvas bag, black casters | 400–650 | Blender (reuse basket proportions) |
| 12 | Wall-mounted fan | 0.45 dia × 0.25 deep | Off-white plastic shell, dark grille, smoothness 0.5 | 350–550 | Blender |
| 13 | Scooter, parked outside | 1.85 × 0.70 × 1.10 | Matte paint, chrome, dark vinyl seat, rubber; smoothness 0.3/0.7 | 1,800–2,800 | CC0 pack preferred; license unverified |
| 14 | Street lamp / wall sconce | 2.8 tall or 0.35 projection | Warm 2700K emissive bulb + one point light; dark metal | 120–250 | Primitives acceptable |
| 15 | Notice board, price list, small signs | 0.60 × 0.80 | Paper, tape, cork; text as TMP-rendered texture | 20 | Unity quads + TMP |
| 16 | Conduit, plumbing run, water heater | 0.03 dia pipes; heater 0.45 × 0.45 × 1.20 | Grey PVC, tarnished copper, painted steel | 250–450 | Blender curves, or cylinder primitives |

Total added geometry if all 16 land: roughly 7,000–11,000 triangles. That is small next to the existing scene.

## 4. Material and lighting plan

Built-In Standard only. No post-processing package, no render-pipeline change; either would be an approval gate.
Standard uses Smoothness = 1 − roughness; values below are Smoothness.

| Surface | Smoothness | Metallic | Note |
|---|---|---|---|
| Floor tile | 0.30–0.40 | 0 | Keep some sheen so window light streaks across it; add roughness variation by decal |
| Wall plaster | 0.05–0.12 | 0 | 0.18–0.24 in the utility alcove where humidity sits |
| Wainscot paint | 0.12–0.18 | 0 | Slightly glossier than the plaster above it |
| Counter wood | 0.22–0.32 | 0 | Normal map at 0.6–0.8 strength |
| Stainless | 0.40–0.55 | 0.85 | Needs a reflection probe or it turns black |
| Painted machine body | 0.35–0.45 | 0.15 | Current pastel bodies read as toys; desaturate |
| Plastic chairs | 0.55–0.65 | 0 | Low metallic, broad highlight |
| Window glass | 0.92–0.96 | 0 | Alpha 0.12–0.25; thin dirt band at frame edges |
| Rubber, mats, cloth | 0.02–0.08 | 0 | Never let cloth look wet unless it is |

- **Fix texel density first.** The floor currently maps one 1K image across 16 m. Re-UV so one texture repeat covers
  1.5–2 m, and confirm the visible tile size against a 0.3 m module. Walls need a second, quieter repeat so the
  plaster stops banding across the large empty faces.
- **Grunge by decal, not by repaint.** Six to ten alpha-blended quads: wall/floor junction, under each machine row,
  door threshold, behind the bucket, and a hand-smudge patch at waist height on the glass. Textures generated by a
  C# `Texture2D` gradient-and-noise script; no image tool exists, and a "generation prompt" is not an asset.
- **Ambient occlusion.** Standard has no AO map. Options in order: decal darkening now; baked lightmap AO later
  (Built-In, no package, bake cost unverified); vertex-color AO only if a shader that reads vertex colour is written.
  Painted contact shadows are static — they will not follow a moved prop.
- **Reflections.** Add one baked reflection probe covering the room. This is the cheapest fix for the plastic look on
  stainless and glass, and it is a scene change for the Unity owner, not a documentation change.
- **Lighting numbers (proposed).** Cut the four existing point lights from range 14 to 5–6 m and 1.6–2.0 intensity.
  Add 4–6 warm practicals at 2.6–2.9 m: over the counter, the machine wall, the waiting chairs, the utility corner.
  Keep shadows on two only. Add two shadowless upward bounce lights (range 3, intensity 0.3–0.5) so the ceiling
  reads at 0.10–0.15 instead of near black.
- **Window light.** One spot at each window plane aimed into the room: 65–75° cone, range 9, intensity 2.5–4, colour
  (1.0, 0.87, 0.72). This paints a bright patch on the floor and is the single strongest "this is a real room" cue.
- **Value structure.** Exterior 1.5–2 stops brighter than the interior; darkest interior 0.05–0.08. Drop flat ambient
  from 0.24 to 0.14–0.18 and tint it cool (roughly 0.55, 0.62, 0.72) so warm practicals dominate. Verify in captures;
  Medium quality may make this too dark.
- **Emissives.** Signs and tubes at emission 1.2–2.0. Without post-processing there is no bloom, so pair each emissive
  with a small point light (range 1.5–2.5, intensity 0.5–0.9) to fake spill.
- **Fog.** Optional linear fog at 0.006–0.010, warm grey, to suggest humidity. Currently off; test that the exterior
  still reads before keeping it.

## 5. Scoped work packages

**Wave 1 — Light and dirt.** Lighting rebalance, visible fixtures, contact/grime decals, floor UV fix, machine-wall
dressing (shelf, price strips, soap stains), signage rewritten in TMP, window vinyl, entrance mat.
Acceptance: ceiling is no longer near-black in captures A and H; a visible dark line exists where floor meets wall at
0.5 m; machines have contact darkening; the window is the brightest interior surface; no mirrored or clipped text is
readable from street or interior; glass, stainless, and tile respond differently to the same light. No new package.
Evidence: fixed 1280×720 captures A–H, before/after pairs, plus one 0.5 m close-up per changed material.

**Wave 2 — Props that carry the story.** Chairs, hanging rack, bottles, mop sink, vending machine, cart, wall fan,
notice board.
Acceptance: each prop within ±10% of the listed dimensions when measured in-editor; each under its triangle budget;
total added under ~12k triangles; every prop casts a shadow and is lit by at least one practical; decorative props
carry no collider unless navigation needs one; navigation validation reruns clean; each prop is identifiable at 3 m.
Evidence: captures C, D, E at eye height, one 1.5 m close-up per new prop, and the rerun navigation validator log.

**Wave 3 — Outside and dusk.** Scooter, street lamp, far-facade detail, road markings, cables, window dirt and
condensation, dusk colour tuning, optional fog.
Acceptance: from capture A the window shows three depth layers (sidewalk, road, far building); the scooter silhouette
is recognizable at 8 m in capture G; exterior reads 1.5–2 stops brighter than interior; the interaction prompt and
machine state stay legible at dusk.
Evidence: captures G and A at dusk, one dusk/night pair, and an owner read of the street.

Capture angles use the existing scene coordinates (floor y = 0, eye 1.7 m):
A entrance `(0,1.7,-5.4) → (0,1.2,4.5)`; B counter `(0,1.7,-1.5) → (-4.5,1.2,-2.9)`; C machine wall
`(0,1.7,1.2) → (0,1.0,5.2)`; D waiting `(1.0,1.7,0.5) → (5.0,1.1,-4.6)`; E utility `(-4.5,1.7,1.5) → (-7.6,1.2,3.4)`;
F secret door `(4.5,1.7,2.8) → (7.8,1.1,3.0)`; G street `(0,1.7,-11.5) → (0,1.7,-6.0)`; H ceiling
`(0,1.7,-3.0) → (0,3.0,2.0)`. A, C, F, and G match angles already used in `TestResults/`, so the comparison is honest.

## 6. Risks

- **The "dirty" pass can read as low-poly, not worn.** Decal grime on hard-edged boxes still looks like dirt painted
  on a box. Bevels and contact shadow matter more than the grime texture.
- **Painted contact shadows are static.** They will detach from props that move and will be visible as floating bands
  on close inspection.
- **Medium quality allows 2 per-pixel lights.** Many practicals may pop or flatten; fixing that means a Quality
  setting change, which is an approval gate, not an art decision.
- **1K textures at 16 × 12 m are already under-resolved.** No image tool exists to author larger maps; a CC0
  download needs approval and an inventory row. The floor will stay soft at close range.
- **Blender by script without an artist.** Each prop needs a real UV unwrap and deliberate bevels; otherwise the new
  props will read no better than the primitives they replace.
- **Street prop licensing is unverified.** The scooter is the highest-risk import; do not commit one without a
  recorded source, author, and licence.
- **Dusk contrast can hurt playability.** A dark interior makes machine state and prompts hard to read. Lighting must
  be judged on gameplay legibility, not just a beauty shot.
- **Human art review is required** for: palette and wainscot colour, chair and machine colours, sign typography,
  the amount of grime before the shop looks abandoned, and whether the dusk look is too dark for the loop.

## 7. Change record

- Created `.orchestrator/reports/room-realism-brief.md`. No other file was created, modified, or deleted.
- No asset was produced, imported, or integrated. No Unity, Blender, git, or package action was taken.
