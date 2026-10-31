# Bali Street and Interior Comfort Brief

- **Status:** proposal. Nothing described here exists as an asset or a placed object.
- **Written:** 2026-11-01, `ll-designer`. Documentation only. No code, scene, prefab, material, package, Unity, or Blender action was taken.
- **Verified by inspection:** `GreyboxSetupTool.cs` (room shell, counter, table, machine row, light positions), `Phase2SetupTool.cs` (south wall piers, window openings, entrance jambs, waiting area, utility alcove, frontage box sizes), `StorefrontFrontageTool.cs` (sidewalk/road/curb/far-walk dimensions, sign heights), `RoomAtmosphereTool.cs` (tube fixtures, light tuning, ambient), `BasketAssetSetupTool.cs` (existing plant, cart, detergent, signs), `MAT_Sidewalk.mat` / `MAT_Asphalt.mat` (glossiness values), `Laundromat.unity` customer waypoints.
- **Not verified:** on-screen look of any prop, CC0 availability or licence of a scooter/shrine reference, light-count performance at Medium quality, and how the owner judges "believable". Every number below is a proposal.

## 0. Coordinate reference (measured, not guessed)

| Element | Extent | Note |
|---|---|---|
| Room interior | x −8…+8, z −6…+6, ceiling 3.3 | 16 × 12 m |
| South facade line | z = −6.0 (wall), glass at −5.94…−5.98 | entrance opening x −2.0…+2.0, 2.4 m high |
| Window openings | x −6.2…−3.8 and 3.8…6.2, y 0.88…2.42 | solid piers at −8…−6.2, −3.8…−2.1, 2.1…3.8, 6.2…8 |
| Sidewalk | z −6.05…−8.05, top y 0.0 | 20 × 2.0 m, only 2 m deep |
| Curb | z −8.01…−8.19, top y 0.12 | |
| Road | z −8.0…−14.0, top y −0.02 | 6 m, two visual lanes |
| Far walk / far facade | z −13.2…−14.8 / −14.75 | facade 4.4 m tall |
| Customer lane | x = 0, z −10.5 → −5.6 | **must stay clear**; Phase 5 spawns and walks here |
| Counter / folding table | (−4.5, −2.5) 3.2 × 0.9 / (4.5, 1.0) 3.2 × 1.2 | |
| Machine row | z 5.25, x −3.15…+3.15 in 0.9 steps | |
| Existing exterior props | none on the sidewalk | the area is empty today |

Because the sidewalk is 2.0 m, the buildable band is **z −6.15…−7.55** (against the wall, 1.4 m) leaving a ≥1.0 m walking strip along the curb. Anything deeper than 0.7 m belongs on the road, not the sidewalk. This single constraint drives the whole layout.

## 1. What this street is

A narrow Bali side street at dusk, one block back from a main road. The ground is a mix: poured concrete slab against the shopfront, a 0.3 m band of square pavers at the threshold, patched asphalt with a raised curb, and a shallow open gutter running the length of the curb under four loose concrete covers. The shop sits in a 20 m rendered facade that is really four neighbouring lots; two of them have shutters down. Walls are cream plaster weathered to grey at the bottom, a teal-painted low wall with moss on its shaded face, and a strip of moss where the sidewalk meets the wall.

Planting is untidy, not landscaped: three frangipani saplings in chipped terracotta, one banana clump with torn leaves, weeds in the gutter line. A warung awning (faded cream, sagging, one broken rib) and a second over the shop's own window. Four scooters parked at 65° to the curb, two on each side, helmets on mirrors. A small roadside offering shrine on a concrete plinth, and a wall shrine above the shop door — both quiet, undecorated, and never a gameplay target.

Overhead: three sagging cable bundles crossing the street plus a service drop onto the shop, black against a warm-grey sky. Light at dusk is two-temperature: warm 2700–3000 K sodium and fluorescents spilling out of the shop and the warung, cool 6000–7000 K sky filling the road. After rain the gutter runs, the asphalt holds darker wet patches along the tyre tracks, and the sidewalk has two shallow puddles at the low end. The street smells of wet concrete; nothing in the scene should look newly bought.

**What it is not:** a resort street, a temple complex, or a tourist postcard. No carved gates at 3 m tall, no rattan lanterns, no neon, no tropical-print anything.

## 2. Placement map — exterior (all coordinates ×, y, z in metres; y is object base unless noted)

| # | Object | Position | Size (W × D × H) | Rotation / clearance |
|---|---|---|---|---|
| 1 | Street lamp A | (−5.2, 0, −7.85) | 0.16Ø pole, 3.2 H, 0.5 arm toward road | arm faces −z; 0.55 m from curb |
| 2 | Street lamp B | (5.6, 0, −7.85) | same | same; 11 m from A |
| 3 | Street lamp C | (7.4, 0, −13.6) | same | on far walk, lights the far facade |
| 4 | Bench 1 (warung) | (−7.6, 0, −6.55) | 1.7 × 0.55 × 0.82 | back to wall, faces street; inside pier x −8…−6.2 |
| 5 | Bench 2 (shop) | (3.2, 0, −6.55) | 1.5 × 0.55 × 0.82 | back to wall; right of the door, clear of door leaf at x 2.05 |
| 6 | Planter 1 | (−2.85, 0, −6.4) | 0.45Ø × 1.25 H | flanks door west; jamb at −2.1 so no pinch |
| 7 | Planter 2 | (2.85, 0, −6.4) | 0.45Ø × 0.95 H | flanks door east, shorter to keep the window clear |
| 8 | Planter 3 (banana) | (−7.9, 0, −7.45) | 0.55Ø × 1.6 H | west corner, in front of the low wall |
| 9 | Scooter 1 | (−3.3, 0, −9.25) | 1.85 × 0.70 × 1.10 | yaw −65° (nose to sidewalk); 1.5 m from lane |
| 10 | Scooter 2 | (−5.2, 0, −9.25) | same | yaw −65°, 1.9 m spacing |
| 11 | Scooter 3 | (2.7, 0, −12.85) | same | yaw +65°, far lane, mirrors 1–2 |
| 12 | Scooter 4 | (4.6, 0, −12.85) | same | yaw +65° |
| 13 | Awning left (shop window) | mount (−5.0, 2.62, −6.02) | 3.0 W × 1.15 D × 0.32 drop | slopes to y 2.30 at the tip |
| 14 | Awning right (warung) | mount (5.4, 2.62, −6.02) | 2.4 W × 1.15 D × 0.32 drop | shortened to clear the blade sign at x 3.55, y ≤2.30 |
| 15 | Roadside shrine | (7.55, 0, −7.5) | 0.8 × 0.6 × 0.7 plinth + 0.5 × 0.45 × 1.0 shrine | faces street; east end, no through traffic |
| 16 | Wall shrine | (2.62, 2.42, −6.02) | 0.38 × 0.18 × 0.5 | on pier, clear of fascia sign (x ≤2.3) and awning (x ≥4.2) |
| 17 | Drainage strip | z −8.20…−8.50, x −10…+10 | 0.30 W × 0.12 deep | 4 cover slabs at x −6.0, −1.5, 3.0, 7.5; 0.9 × 0.32 |
| 18 | Low boundary wall | x −9.25, z −6.1…−8.0 | 0.20 thick × 0.55 H, 1.9 long | perpendicular to facade, capped, moss on north face |
| 19 | Cables | z −8.6, −11.2, −13.6, y 3.4 → 3.5 | 20 m span, 0.35 sag | + service drop at x −6.0 to y 2.9 |

Notes: the customer lane (x 0, z −10.5…−5.6) is clear in every row. Bench, lamp and planter bases total 0.9 m of the 1.4 m wall band, leaving 1.0–1.1 m to walk along the curb. Scooters sit on the road by necessity — a 1.85 m scooter cannot park at 65° on a 2.0 m sidewalk without blocking it. That is also how they park in reality.

## 3. Placement map — interior comfort pass

Placed against existing dead zones. Nothing overlaps counter (−4.5/−2.5), table (4.5/1.0), machine row (z 5.25), waiting chairs (4.4–5.3/−4.9), shelving (x −7.6), utility alcove (−5.05…−7.2, z 4.4–5.0), secret cover (z 2.4–4.2).

| # | Object | Position | Size (W × D × H) | Why it helps |
|---|---|---|---|---|
| 20 | Interior bench 1 | (−7.52, 0, −3.4), faces +x | 1.8 long × 0.6 × 0.85 | Turns the west strip into a waiting nook; gives the eye a mid-ground object between window and counter |
| 21 | Interior bench 2 | (7.52, 0, −1.4), faces −x | 1.6 × 0.6 × 0.85 | Breaks the long empty east run before the secret cover; a person sitting explains why the cover is unnoticed |
| 22 | Potted plant 1 | (7.35, 0, −5.45) | 0.5Ø × 1.3 H | Softens the hard south-east corner; frame edge for the waiting capture |
| 23 | Potted plant 2 | (−6.55, 0, −3.35) | 0.4Ø × 0.9 H | Fills the dead counter end; blocks the eye from reading the whole floor at once |
| 24 | Potted plant 3 | (6.85, 0, 1.95) | 0.45Ø × 1.0 H | Separates folding from circulation; 0.28 m clear of the existing cart |
| 25 | Notice board | west wall x −7.88, z −5.0, centre y 1.5 | 1.0 × 0.05 × 0.7 | The only dense small detail on a 4.8 m blank wall; reads as a neighbourhood, not a box |
| 26 | Water cooler | (2.7, 0, −5.68) | 0.36 × 0.36 × 1.05 + 0.4 bottle | Marks the entry threshold as a real shop; sits on solid pier, clear of door leaf at 2.05 |
| 27 | Magazine rack | (6.05, 0, −5.65) | 0.75 × 0.35 × 0.72 | Under the right-window sill (0.88) so it never covers glass; gives the waiting chairs a reason to exist |

Each new floor object is 0.35–0.6 m deep, leaving the 2.5 m centre lane and the entrance-to-counter route intact. Verify with the navigation validator, not by eye.

## 4. Lighting direction

**Diagnosis of the flat read (evidence-based).** Eight warm lights exist: four original points (range 7.5, intensity 2.2) and four tube practicals (range 7, intensity 1.5), all near-identical colour, plus flat ambient 0.36. Equal-intensity light from every direction with no AO, no reflection probe, and no post-processing flattens everything. The exterior is lit by one 0.55 directional and the same ambient, so it has no dusk identity at all.

**Interior (3000 K practicals).** Proposed: ambient 0.36 → 0.16–0.20, tinted cool (0.55, 0.62, 0.72). Keep the four tubes as the key, but separate them: entry tube (0, 3.15, −2.8) → 2.6 / range 6.0; counter tube (−3.4, 3.15, 0.4) → 2.2 / 5.5; machine-wall tube (0, 3.15, 3.6) → 1.9 / 5.0; waiting tube (3.4, 3.15, 0.4) → 1.4 / 4.5. The four original points drop to fill at 0.9–1.2 with range 6, shadows off; keep exactly two shadow casters in the room. That produces four different pools instead of one wash.

**Cool dusk from outside.** Add two spots outside the windows aimed in — (−5.0, 2.2, −6.3) and (5.0, 2.2, −6.3), 70° cone, range 7, intensity 2.0, colour (0.62, 0.70, 0.85). This is the single strongest realism cue: a hard cool patch on the tile that disagrees with the warm tubes. Directional light 0.55 → 0.35, colour (0.55, 0.63, 0.80), low angle from the west (x rotation ~10°).

**Street (2700 K sodium).** Lamps A/B/C: point light at head height (y 3.0), range 8, intensity 2.2, colour (1.0, 0.70, 0.40), shadows off on all three; emissive head at emission 2.4–3.0. A fourth, shadowless lamp is not needed. Total exterior lights: 3 lamps + 2 window spots + 1 directional.

**Making the storefront glow read from the street.** Three cheap parts, no post-processing package:
1. Spill point at (0, 2.05, −6.75), range 5.5, intensity 1.4, colour (1.0, 0.88, 0.72), no shadows — a warm pool on the sidewalk in the door cone.
2. Raise the entry tube to 2.6 so light physically falls through the opening (already counted above).
3. Emissive fascia: duplicate the shared sign material as `MAT_SignLit` (emission 1.2) and apply only to `Facade_SignPanel`, so "LAUNDRY LEGENDS" holds value at 15 m while the interior stays readable. No bloom exists, so the panel will look printed-but-lit, not neon. Do not claim a glow effect that the pipeline cannot produce.

Target value structure: interior floor in the door cone 1.5–2 stops brighter than the sidewalk; sidewalk 1 stop brighter than the road; road darkest at 0.04–0.07. Judge on gameplay legibility (prompt contrast, machine state) first, beauty second.

## 5. Material notes

Built-In Standard; `Smoothness = 1 − roughness`. Metallic stays 0 for everything except lamp arms and awning frames (0.2 max) — with no reflection probe, real metal renders black.

| Surface | Colour (linear-ish sRGB) | Smoothness | Roughness | Notes |
|---|---|---|---|---|
| Sidewalk concrete | (0.55, 0.53, 0.50) | **0.12–0.18** | 0.82–0.88 | `MAT_Sidewalk.mat` is currently **0.75** — the glossiest surface in the scene. Fix this before adding props; it is the cheapest visual win available. |
| Paver band at threshold | (0.50, 0.47, 0.43) | 0.15 | 0.85 | 0.30 m module, 0.02 proud of the slab |
| Asphalt | (0.12, 0.13, 0.14) | 0.10 dry / 0.35–0.50 wet | 0.50–0.90 | `MAT_Asphalt.mat` is 0.65; wet patches are 25 % darker, not shinier-everywhere |
| Gutter / drain | (0.17, 0.18, 0.16) | 0.10 | 0.90 | 30 % green-black algae along the waterline |
| Rendered wall | (0.62, 0.58, 0.50) | 0.06–0.10 | 0.90–0.94 | 40 % darker below y 0.9 (splash zone) |
| Low wall, mossy face | (0.45, 0.43, 0.39) base | 0.08 | 0.92 | moss (0.20, 0.28, 0.14), roughness 0.95, on north face + top 60–80 % |
| Terracotta | (0.60, 0.31, 0.19) | 0.12–0.18 | 0.80–0.88 | white salt bloom ring at the base, chipped rim darker |
| Weathered teal | (0.16, 0.32, 0.31) | 0.35 painted / 0.15 worn | 0.65 / 0.85 | worn-through edges show bare wood albedo, not white |
| Faded cream | (0.78, 0.73, 0.63) | 0.55–0.65 | 0.35–0.45 (painted metal) | awning fabric 0.05–0.10; staining under the drip line |
| Vegetation | (0.10–0.28) green, yellow variance | 0.20–0.35 | 0.65–0.80 | banana leaves slightly glossier (0.35); no subsurface |
| Emissive lamp head | (1.0, 0.96, 0.88) | — | — | emission 2.4–3.0 |
| Street metal | (0.10, 0.11, 0.12) | 0.30–0.40 | 0.60–0.70 | metallic ≤0.2; painted, not chrome |

**Where grime belongs** (alpha quads or vertex-tint, never a repaint): the wall/ground joint outside (5 cm band), the gutter lip and cover edges, the awning drip line onto the facade, under each bench and planter, tyre tracks along z −9.3 and −12.6, the base of both shrines, hand smudges at y 1.0–1.5 on the shop glass, and two kickstand oil dots per scooter. Moss only on north-facing and shaded surfaces — never on the sunny top of the low wall.

## 6. Scoped waves

**Wave 1 — ground, light and static primitives.** Sidewalk/paver/asphalt material fix · drain strip + 4 covers · low boundary wall · 3 lamps with light and emissive heads · 2 shutters on the far facade (flat boxes) · exterior dusk directional + 2 window spots · storefront spill + `MAT_SignLit` · contact-shadow quads under every new base. No imports, no new package, buildable from boxes/cylinders/quads. ≈ 4k tris.
*Acceptance:* sidewalk grey and matte in capture S2, not glossy; a ≥1.0 m clear walking band survives at every x; the door cone reads 1.5–2 stops brighter than the sidewalk in S5; lamps cast no more than one shadow total; customer lane untouched and Phase 5 lifecycle tests still pass.
*Captures:* S1 (0, 1.7, −13.4) → (0, 1.6, −6.0); S2 (−3.6, 1.65, −9.6) → (0.5, 1.8, −6.2); S5 from inside (0, 1.7, −1.2) → (0, 1.3, −6.0).

**Wave 2 — street depth.** Two awnings · 4 scooters · roadside and wall shrines · 3 cable spans + service drop · far-facade door recesses · 3 exterior planters with frangipani/banana card geometry · optional kerbside warung counter. Scooters require a CC0 model or a beveled Blender blockout; primitives will not survive 3 m inspection.
*Acceptance:* three depth layers visible from S1 (sidewalk, road, far facade); a scooter silhouette is identifiable at 8 m in S3; both awnings shadow the facade without occluding window glass above y 2.2; no prop inside the x 0 lane or within 0.6 m of it; every import has source, author, licence and SHA-256 in `ASSET_INVENTORY.md` before commit.
*Captures:* S3 (7.2, 1.7, −7.3) → (−6.0, 1.4, −6.6); S4 (0, 1.7, −4.6) → (0, 1.5, −11.0); plus one dusk/night pair.

**Wave 3 — interior dressing.** 2 benches · 3 plants · notice board · water cooler · magazine rack · interior light split (four tube intensities, ambient drop, two shadow casters) · contact shadows under new furniture. No new lights beyond the two window spots already added in Wave 1.
*Acceptance:* eight new objects within ±10 % of listed size; each identifiable at 3 m; no new collider blocks a validated route; the room no longer reads as one uniform wash in I1 and I2 (distinct light pools at counter, entry, machine wall); navigation validator and Phase 5 tests rerun clean.
*Captures:* I1 (2.0, 1.7, −1.0) → (−7.5, 1.2, −4.0); I2 (0.5, 1.7, −4.0) → (7.0, 1.15, −5.4); reuse existing capture B for the counter.

All captures use the existing 1280×720 / 60° helper already in the editor tools. Before/after pairs for every material change.

## 7. Risks

- **A 2 m sidewalk cannot hold this much street.** Benches, lamps, planters and shrines together will crowd it unless the 1.0 m walking strip is treated as a hard constraint. If it crowds, delete Planter 3 and Bench 1 before breaking the strip.
- **Scooters are the highest-risk item.** No 3D generation tool exists, and a scooter assembled from 40 boxes looks like a toy at 3 m. Either import a licensed model (approval gate + inventory row) or accept a 900-triangle Blender blockout and put it in the "needs human art pass" column.
- **Light count.** Medium quality allows 2 per-pixel lights. After this brief: 8 interior + 3 lamps + 2 spots + spill + directional = 15. Most will fall back to vertex lighting and pop between surfaces. The real fix is a QualitySettings change, which is an approval gate, not an art decision.
- **No post-processing means no bloom.** Emissive lamp heads and a lit sign will not halo. Do not design the street around glow; design it around light pools and contrast.
- **Painted contact shadows do not move.** Under benches and scooters they will read as floating bands on close inspection.
- **Wet patches without a reflection probe read as dark stains, not water.** Either add one probe (scene change) or keep wetness to two low-gloss patches and call it mud.
- **The shrine is culturally load-bearing.** A Balinese offering shrine done carelessly is worse than none. It must be small, plain, respectful, non-interactive, and reviewed by a human before it ships; no fantasy iconography and no player interaction prompt on it.
- **The customer lane is silently fragile.** Waypoint tests will pass even if a prop blocks the visible walk, because the agent follows transforms. Only the navigation validator and an eye check catch it. Re-run both after every placement wave.
- **Text risk.** Existing `TextMesh` signage is already mirrored from some angles. Do not add readable text to awnings, shutters or the shrine until signage is rewritten in TextMeshPro.
- **Human art pass required for:** moss coverage (too much reads as abandoned), terracotta saturation, whether the dusk look is too dark to read prompts, and whether the street belongs to this specific shop or to a generic asset pack.

## 8. Change record

- Created `.orchestrator/reports/bali-street-brief.md`. No other file was created, modified, or deleted.
- No asset was produced, generated, imported, or integrated. No Unity, Blender, git, or package action was taken. Nothing in this document changes `PROJECT_STATE.md` or `ASSET_INVENTORY.md`.
