# AAA Production Research — What the Look Actually Costs

- **Status:** research and capability assessment. Documentation only. No code, scene, prefab, package, install, or Unity run.
- **Written:** 2026-11-01. **Owner:** `ll-designer` role.
- **Question:** the owner wants a "2026 AAA appearance". What is that made of, what can this project reach, and what is dishonest to promise?
- **Verified against the repo:** `PROJECT_STATE.md`, `.orchestrator/reports/room-realism-brief.md`,
  `.orchestrator/reports/2026-11-01-realism-pass.md`, `.orchestrator/reports/tooling-and-assets-audit.md`,
  `ASSET_PIPELINE.md`, `GAME_DESIGN.md` §12. Every claim about our own state is sourced to one of those.
- **Not verified:** any in-engine look, bake time, or performance number. No asset described here exists.

> **Conflict to resolve, not silently pick.** `MASTER_CONTEXT.md` §3 confirms "small stylized 3D game, **not photoreal**".
> The new request is "2026 AAA appearance". Those are different targets. This report treats "AAA appearance" as
> **presentation quality inside a stylised scope** and states where that stops. Amending the confirmed art scope is an owner decision.

## 1. What "AAA look" decomposes into

Percentages are **estimates**, normalized to 100 for a shipped first-person interior scene. No public study measures this;
the split is reasoned from the sources in §10 and from what is visibly wrong in our own room. Art direction is also a
multiplier on every other row, so treat the numbers as a priority ordering, not physics.

| Component | Est. share | What it actually is | Where it shows | Basis |
|---|---|---|---|---|
| Art direction & coherence | 18% | One chosen look, palette, value structure, shape language, reference discipline | Everything reads as one place, not a kit | Insomniac environment-art lead: intent and pillars precede "pretty" ([source](https://www.gamedeveloper.com/production/environment-art-isn-t-about-making-pretty-things-so-what-s-it-about-)) |
| Lighting, GI, exposure, tonemap | 20% | Where light comes from, how it falls off, bounce, contrast, practicals | Depth, mood, readability; the first thing the eye judges | Unity: lightmaps carry baked direct+indirect; our own brief calls window light the strongest "real room" cue ([Unity](https://docs.unity3d.com/6000.0/Documentation/Manual/Lightmappers.html)) |
| Animation & character performance | 14% | Idle, walk, carry, reach, weight, timing, reactivity | "Alive" vs "mannequin"; dominant in motion, near-invisible in a still | Faceless/static characters are our current top tell (`2026-11-01-realism-pass.md`) |
| Asset density, silhouette, set dressing | 12% | Unique props, bevels, layered clutter, hero assets, story objects | Close-up credibility at 1–3 m | Interior scenes are environment-budget dominated in the $100 exercise ([source](https://www.gamedeveloper.com/production/environment-art-isn-t-about-making-pretty-things-so-what-s-it-about-)) |
| PBR materials & texture authoring | 10% | Roughness/metal/AO variation, texel density, normals, detail maps | Metal vs plastic vs cloth under the same light | Our materials have colour+normal only, no roughness/metal/AO (`tooling-and-assets-audit.md` §1.5) |
| Post-processing & camera | 8% | ACES tonemap, bloom, grade, vignette, SSAO, AA, DoF | Cohesion and "camera" feel; not a rescue for bad lighting | URP volume stack is scripted but never executed (`tooling-and-assets-audit.md` §1.1) |
| Audio | 8% | Layered ambience, variation, reverb, mix buses, ducking | Perceived production value per minute of play | No `AudioMixer` exists; no buses, reverb or limiting (`tooling-and-assets-audit.md` §1.5) |
| UI/UX & feedback | 4% | Type, spacing, prompts, transitions, state readability | Cheap to fix, obvious when wrong | Placeholder IMGUI + `TextMesh` signage (`room-realism-brief.md` §1) |
| Polish/consistency passes | 6% | Repeated passes against references; removing outliers | The absence of "one thing that looks wrong" | Iteration is the studio advantage we do not have |

## 2. Pipeline reference — technique, payoff, and our real cost

| Technique | What it buys (one sentence) | Cost for us | Verdict |
|---|---|---|---|
| High-poly sculpt → retopo → bake | Micro-detail (folds, seams, dents) stored in normal/AO/curvature maps on a cheap mesh | **High.** Needs sculpting skill, a high-poly source, and a baker (Substance [docs](https://experienceleague.adobe.com/en/docs/substance-3d-designer/using/bakers/bakers)); Blender can bake but we have no sculpts | **Skip.** Our bottleneck is design, not micro-detail |
| Trim sheets | One texture sheet textures many props at consistent texel density and low memory | **Medium.** Needs disciplined UVs in scripted Blender; one 2K sheet | **Adopt.** Best geometry-to-look ratio we can actually execute |
| Modular kit-bashing | Few reusable pieces assemble many believable spaces fast | **Low–Medium.** We already build from cubes; a real kit needs bevels and shared UVs | **Adopt** for walls, fixtures, cabinets |
| Decal systems | Grime, wear, signage, contact darkening, stains without new geometry | **Low.** URP Decal Renderer Feature or plain quads; no image tool, so procedural C# textures or CC0 decal packs | **Adopt.** Highest detail-per-hour available to us |
| PBR texture authoring | Roughness/metal/AO variation is the difference between "grey plastic" and "steel" | **Low.** ambientCG 2K CC0 sets; assign and tune | **Adopt immediately** |
| HDRI lighting | Real ambient + specular from a photographed sky; instantly lifts every existing material | **Low.** Poly Haven CC0, one 4K HDR, skybox + ambient mode | **Adopt immediately** |
| Baked lightmaps vs real-time GI | Baked GI gives high-quality bounce for zero runtime cost, but cannot change at runtime | **Medium.** UV2, bake times on a 4 GB laptop GPU, iteration cost on every scene change | **Adopt** for the room; re-bake is a real workflow tax |
| Adaptive Probe Volumes | Per-pixel baked GI on dynamic objects (characters, props) instead of blotchy light probes | **Low–Medium.** URP only; bake + volume setup | **Adopt after URP**, before characters look lit |
| Reflection probes | Makes metal and glass reflect the room instead of turning black or flat | **Low.** One baked probe; already scripted in `UrpMigrationTool` | **Adopt immediately** — named as missing in our own brief |
| LODs | Cuts vertex/shadow cost at distance so the frame budget can go to near-field detail | **Low–Medium** per asset; Unity LOD Group ([docs](https://docs.unity3d.com/6000.1/Documentation/Manual/class-LODGroup.html)) | **Defer.** A 16×12 m room is not draw-call bound |
| Impostors | Billboard/LOD stand-in for distant heavy objects | **High for us, no benefit.** Needs a bake pipeline ([Pixyz docs](https://docs.unity3d.com/Packages/com.unity.industry.toolkit@3.0/manual/create-impostors.html)) | **Skip.** Wrong tool for one interior |
| GPU instancing / occlusion culling / Addressables | Throughput and memory at scale | **Low effort, no current payoff**; Addressables solves a content-scale problem we do not have | **Defer.** Keep for the arcade expansion, if ever |

## 3. Unity stack for 2025–2026 (and the pipeline reality)

**The strategic fact.** On 2026-02-23 Unity announced that the **Built-in Render Pipeline is deprecated from 6.5**,
that **HDRP receives no new features** (maintenance + Switch 2 only), and that **URP is where all investment goes**
([Unity, "Render Pipelines strategy for 2026"](https://discussions.unity.com/t/render-pipelines-strategy-for-2026/1710004)).
We are still rendering through BIRP (`tooling-and-assets-audit.md` §1.1). BIRP is a dead end for a 2026 look.

**URP vs HDRP for us.** HDRP has SSGI, SSR, volumetric fog, contact shadows, physical light units, subsurface scattering,
DLSS, and ray tracing, but it is maintenance-only, heavy for a 4 GB RTX 3050 laptop, and needs a full rework
([feature comparison](https://docs.unity3d.com/6000.1/Documentation/Manual/render-pipelines-feature-comparison.html);
`tooling-and-assets-audit.md` ranks HDRP "negative"). **URP is correct.** What URP gives us: baked GI + Adaptive Probe
Volumes, decals, SSAO, bloom/ACES/grade, reflection probes, STP upscaling, GPU Resident Drawer, GPU occlusion culling,
Render Graph. What URP does **not** give us (as of the 6.x comparison): SSGI, screen-space reflections, volumetric fog,
contact shadows, physical light units, DLSS. FSR1 is supported; FSR2 is HDRP-only on Windows.

Consequences for the plan: **no real-time GI crutch, no volumetric fog, no contact shadows, no DLSS.** Every one of those
must be replaced by baked lighting, decals, and art direction. That is not a limitation we can shader our way out of.

- **Post stack:** URP Volume — ACES tonemap, bloom, colour adjustments, vignette, SSAO, film grain. Cheap, high payoff, not a rescue.
- **Reflections:** baked reflection probes, not SSGI. One probe is the cheapest fix for our plastic stainless.
- **Decals:** URP Decal Renderer Feature; does not support SRP Batcher by design, batch with GPU instancing and an atlas ([Unity](https://docs.unity3d.com/6000.0/Documentation/Manual/visual-effects/decals/renderer-feature-decal.html)).
- **Scaling:** GPU Resident Drawer and GPU Occlusion Culling need Forward+/Deferred+; irrelevant at our object count.
- **Addressables/LOD/impostors:** real techniques, wrong problem. Do not spend 2026 on them.

## 4. Realistic quality tiers with shipped examples

| Tier | Definition | Shipped examples | What separates it from the tier below |
|---|---|---|---|
| **T0 Prototype** | Readable greybox; placeholders; systems proven, look not authored | **Laundry Legends today** | Nothing to compare; this is pre-art |
| **T1 Stylised indie (shipped)** | One coherent look, modest density, readable silhouettes, clean UI, working audio | *A Short Hike* (one developer; [GDC postmortem](https://gdcvault.com/play/1026613/Independent-Games-Summit-Crafting-A), [IGF interview](https://www.gamedeveloper.com/business/road-to-the-igf-adam-robinson-yu-s-i-a-short-hike-i-)); *Untitled Goose Game* (four people, [House House](https://en.wikipedia.org/wiki/House_House)) | A **chosen** art direction, consistent materials, lighting that supports readability, zero obvious placeholder surfaces |
| **T2 AA-adjacent** | Small team, authored heroes, real characters, full mix, custom shaders, deliberate lighting | *Stray* (~18–20 at release, [Unreal interview](https://www.unrealengine.com/developer-interviews/28-people-and-two-cats-inside-the-making-of-stray)); *Kena: Bridge of Spirits* (Ember Lab's first game, [Unreal interview](https://www.unrealengine.com/developer-interviews/the-magic-of-creating-kena-bridge-of-spirits)); *Hi-Fi Rush* (small internal team with full studio support, [Wikipedia](https://en.wikipedia.org/wiki/Hi-Fi_Rush)) | **Authored character art and animation**, bespoke hero props, technical art, a mixed soundtrack, weeks of iteration per room |
| **T3 AAA-adjacent** | Console-blockbuster presentation at reduced scope, backed by budget and specialists | *Hellblade: Senua's Sacrifice* (~20 people, ~$10M, [GamesIndustry.biz](https://www.gamesindustry.biz/how-ninja-theory-proved-independent-aaa-has-a-future)) | Professional actors and mocap, facial performance, bespoke VFX/audio, a full art department, years of iteration |

Hellblade is the honest proof that twenty people can look AAA — and the honest proof that it took a studio, a budget,
professional performance capture, and specialists we do not have. It is not evidence that free assets plus one developer
can reach that tier.

## 5. The 20% that gives 80% of perceived quality (small interior)

Ranked for our room specifically, by payoff per hour. These are proposals, not measurements.

1. **Baked GI + a designed value structure.** Fewer, narrower lights; window as the key; ceiling bounce; dark corners.
   Nothing else improves a flat room as much.
2. **HDRI ambient + one baked reflection probe.** Fixes "dark ceiling", "grey plastic metal", "glass with no reflection".
3. **Full PBR maps (roughness/metal/AO) and correct tiling.** Same meshes, immediately less like a model.
4. **Contact/grime decals and bevels.** The floor-wall junction and under-machine darkening sell scale.
5. **ACES tonemap + restrained bloom + grade.** Cohesion; makes separate assets look like one game.
6. **A human body with idle/walk/carry.** The single largest "is this a real game?" signal.
7. **Motion everywhere.** Fan, dripping tap, swinging sign, steam, cloth settle, camera weight.
8. **Audio layering and variation.** Machine cycles, room tone, footsteps, coin, door — with a mixer and randomised one-shots.

## 6. What cannot be faked

These read as "not AAA" instantly, and no amount of post-processing hides them:

- **Faceless or unclothed characters.** Our mannequin is the loudest tell in the build (`2026-11-01-realism-pass.md`).
  Attempting eyes/blink shaders on a mannequin makes it worse — that is the uncanny valley, and it is well documented
  as a failure mode ([Eurogamer on Andromeda](https://www.eurogamer.net/mass-effect-andromeda-and-why-facial-animation-is-really-hard)).
- **No facial animation or lip sync.** A talking static face is more jarring than a stylised, deliberately mask-like one.
- **Flat or default lighting.** Uniform ambient + range-14 point lights is the "test scene" signature (`room-realism-brief.md` §1).
- **No motion.** A perfectly still room with a walking player reads as a diorama.
- **Repetitive props at human scale.** The eye finds the third identical chair immediately.
- **No audio variation.** One looping track and one machine hum exposes the budget.
- **Placeholder typography.** Mirrored/clipped `TextMesh` signs are a "prototype" watermark.

## 7. Gap assessment

**Current tier: T0 — prototype, not yet T1.** Evidence, not opinion:

| Dimension | Today | T1 requires |
|---|---|---|
| Characters | Faceless tinted mannequin on the animation-library rig | A clothed human body, humanoid-mapped, with idle/walk/carry |
| Materials | 3 ambientCG sets, 1K, colour+normal only | Roughness/metal/AO, correct texel density, variation |
| Lighting | 4 point lights, range 14, flat ambient 0.24, no probes, no HDRI | Designed key/fill/bounce, HDRI ambient, baked GI, probes |
| Post | None | ACES + bloom + grade + SSAO |
| Audio | 7 music tracks, 54 voice WAVs, no mixer | Buses, reverb, SFX layer, variation |
| Typography | `TextMesh`, mirrored, clipping | TMP, correct faces, consistent hierarchy |
| Rendering | Package resolved, **pipeline not wired**; still BIRP | URP actually rendering |

**What blocks T1:** the unwired URP/post stack (approval gate), missing HDRI/probe/GI, missing PBR maps, the faceless
character, placeholder signage, and no audio mix. All are known, scoped, and cheap-ish.

**What blocks T2:** authored character art and facial performance, bespoke hero props, custom shaders/VFX, directed
animation, a real mix, and a technical artist. These are labour problems, not setting problems.

**The technical standards themselves are a ceiling.** `ASSET_PIPELINE.md` §4 budgets 300–1,500 tris per prop and 512–1,024
textures. Shipped AA props commonly run 5k–50k tris with 2K–4K maps. We can make a *very good stylised* room at our
budget. We cannot make a photoreal one, and raising the budget does not help without an artist to fill it.

## 8. Ranked action list

Effort: **S** ≤ 2 h, **M** 0.5–2 days, **L** > 2 days. Payoff is expected perceived-quality gain, estimated.

| # | Action | Effort | Payoff | Depends on |
|---|---|---|---|---|
| 1 | Finish the URP migration: assign pipeline/renderer, convert materials, build the Volume post stack (ACES, bloom, grade, vignette, SSAO), validate | S | **Very high** | Owner approval (project settings) |
| 2 | Add a CC0 Poly Haven HDRI skybox + skybox ambient + one baked reflection probe | S | **Very high** | #1 for the probe |
| 3 | Relight to a designed value structure: narrow ranges, 4–6 practicals, window key spot, ceiling bounce, cooler ambient | S–M | **Very high** | #2 |
| 4 | Upgrade to full PBR: 2K roughness/metal/AO, fix floor tiling/texel density, differentiate steel/glass/tile | S | **High** | — |
| 5 | Replace the faceless mannequin with a clothed, humanoid-mapped body; fix carry anchor and grip pose | M | **Very high** | Character import must actually pass Unity's humanoid mapper |
| 6 | Decal + bevel pass: contact darkening, grime, wear, window vinyl, TMP signage replacing `TextMesh` | S–M | **High** | URP decals (#1) |
| 7 | `AudioMixer` with Master/Music/SFX/Voice, reverb send, randomised machine/coin/door one-shots | M | **High** | SFX source (CC0) |
| 8 | Baked GI + lightmap UVs + Adaptive Probe Volumes for characters | M | High | #1, static scene locked |
| 9 | Trim sheet + modular kit for walls/fixtures, with bevels and shared UVs | M–L | Medium-high | Blender scripting |
| 10 | Motion pass: fan, steam, swinging sign, cloth settle, camera weight, ambient NPC pathing | M | Medium-high | — |

Actions 1–7 are the T0→T1 jump. 8–10 are T1→"good T1". Nothing here reaches T2.

## 9. Unattainable without artists or budget — stated plainly

- **Photoreal characters with facial animation.** Requires sculptors, facial riggers, performance capture, and iteration.
  Free rigged humans have failed Unity's humanoid mapping twice already.
- **Hand-authored hero props and dense set dressing.** Scripted Blender produces parametric hard-surface forms. It does not
  produce designed objects, worn edges chosen for composition, or a prop that tells a story.
- **Directed, acted animation.** Mocap or skilled keyframing, plus a director deciding what the performance should be.
- **Custom shaders and VFX.** Needs a technical artist; Shader Graph helps, but taste and debugging time are the cost.
- **A bespoke audio mix.** Foley, adaptive music, and a sound designer's ear.
- **AAA density.** Hundreds of unique assets, LODs, and collision-tuned props per room is a team-years problem.
- **The word "AAA" itself.** With faceless characters and flat lighting, calling the result AAA is a credibility risk,
  not a marketing upgrade.

**Three things to stop trying to fake:** (1) photorealism through material sliders alone — silhouette, lighting, and
authored detail carry realism, not smoothness values; (2) facial performance on a mannequin — adding eyes and blinking
to a faceless body makes it more unsettling, not more human; (3) "AAA" via post-processing — bloom and ACES on flat
lighting produce a polished greybox, and the greybox still reads.

## 10. Sources

- Unity, *Render pipeline feature comparison* (6.1) — https://docs.unity3d.com/6000.1/Documentation/Manual/render-pipelines-feature-comparison.html
- Unity, *Render Pipelines strategy for 2026* (BIRP deprecated 6.5; HDRP maintenance; URP focus) — https://discussions.unity.com/t/render-pipelines-strategy-for-2026/1710004
- Unity, *Introduction to lightmaps and baking* (6.0) — https://docs.unity3d.com/6000.0/Documentation/Manual/Lightmappers.html
- Unity, *Introduction to decals in URP* (6.0) — https://docs.unity3d.com/6000.0/Documentation/Manual/visual-effects/decals/renderer-feature-decal.html
- Unity, *What's new in URP 17* (GPU Resident Drawer, Render Graph, STP) — https://docs.unity.com/engine/7000.0/manual/render-pipelines/universal-render-pipeline/urp-whats-new
- Unity, *LOD Group component reference* — https://docs.unity3d.com/6000.1/Documentation/Manual/class-LODGroup.html
- Unity, *Upgrading from the Built-in Render Pipeline to URP* — https://docs.unity3d.com/6000.3/Documentation/Manual/urp/upgrading-from-birp.html
- Unity Pixyz, *Create impostors* — https://docs.unity3d.com/Packages/com.unity.industry.toolkit@3.0/manual/create-impostors.html
- Jason Hickey (Insomniac), *Environment art isn't about "making pretty things"* — the $100 discipline-budget exercise — https://www.gamedeveloper.com/production/environment-art-isn-t-about-making-pretty-things-so-what-s-it-about-
- Polycount wiki, *Modular environments* (trim sheets, kit-bashing, texture atlases) — http://wiki.polycount.com/wiki/Modular_environments
- Tim Simpson, *Trim sheet tutorial* (ArtStation) — https://www.artstation.com/blogs/pixelmasher/eodW/trim-sheet-tutorial-environment-art-101
- Adobe, *Substance 3D bakers* (high→low mesh map baking) — https://experienceleague.adobe.com/en/docs/substance-3d-designer/using/bakers/bakers
- Eurogamer, *Mass Effect Andromeda and why facial animation is really hard* — https://www.eurogamer.net/mass-effect-andromeda-and-why-facial-animation-is-really-hard
- GDC Vault, *Crafting a Tiny Open World: A Short Hike Postmortem* — https://gdcvault.com/play/1026613/Independent-Games-Summit-Crafting-A
- Game Developer, *Road to the IGF: Adam Robinson-Yu's A Short Hike* — https://www.gamedeveloper.com/business/road-to-the-igf-adam-robinson-yu-s-i-a-short-hike-i-
- Wikipedia, *House House* (four-person team) — https://en.wikipedia.org/wiki/House_House
- Unreal Engine, *28 people and two cats: inside the making of Stray* — https://www.unrealengine.com/developer-interviews/28-people-and-two-cats-inside-the-making-of-stray
- Unreal Engine, *The magic of creating Kena: Bridge of Spirits* — https://www.unrealengine.com/developer-interviews/the-magic-of-creating-kena-bridge-of-spirits
- Wikipedia, *Hi-Fi Rush* (small internal team) — https://en.wikipedia.org/wiki/Hi-Fi_Rush
- GamesIndustry.biz, *How Ninja Theory proved independent AAA has a future* (~20 people, ~$10M) — https://www.gamesindustry.biz/how-ninja-theory-proved-independent-aaa-has-a-future

## 11. Change record and limits

- Created `.orchestrator/reports/aaa-production-research.md`. No other file changed.
- No code, scene, prefab, package, project setting, asset, or Unity/Blender run. No asset was produced or integrated.
- Percentages, effort ratings, and payoff ratings are **estimates**, not measurements.
- No claim is made that any technique listed here is implemented in the project.
