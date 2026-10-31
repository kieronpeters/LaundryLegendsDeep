# Tooling and Free-Asset Audit

- **Date:** 2026-11-01
- **Type:** Research only. Nothing installed, no `Assets/`/`Packages/` change, no Unity/Blender build run.
- **Scope:** what this machine and project already have, and which free additions would most raise visual/audio quality.
- **Method:** filesystem inspection, `--version` probes, `Get-CimInstance Win32_VideoController`, licence pages fetched live (Poly Haven, Quaternius, ArmorPaint).

> **Context correction.** `AGENTS.md` still says "no Unity project exists / Phase 1 begins with LL-P1-001". That is stale. The project is at **Phase 8** (53 tasks complete, EditMode 115/115, PlayMode 51/0/1). This audit uses the real state.

## Part 1 — Inventory

### 1.1 Engine and modules

| Item | Finding | Evidence |
|---|---|---|
| Unity editors installed | `6000.0.0f1`, `6000.0.64f1`, `6000.5.7f1` | `C:\Program Files\Unity\Hub\Editor\` |
| Editor in use | `6000.0.64f1` (Unity 6.0 LTS) | `ProjectSettings/ProjectVersion.txt`, `state.json` |
| PlaybackEngines (6000.0.64f1) | **`windowsstandalonesupport` only** — no Android/iOS/WebGL module | `...\6000.0.64f1\Editor\Data\PlaybackEngines` |
| PlaybackEngines (6000.5.7f1) | `WebGLSupport` + `windowsstandalonesupport` | same tree |
| Blender | **4.5.15 LTS**, build date 2026-10-22 | `C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe` |

**The render pipeline migrated from Built-In to URP *during this audit*.** The project changed under me between checks, so both states are recorded:

| Source | At 03:09 (first read) | At 03:16 (re-check) |
|---|---|---|
| `Packages/manifest.json` | URP `17.0.4` requested | unchanged |
| `Packages/packages-lock.json` | no URP (stale, dated 2026-10-19) | **URP 17.0.4 resolved** (rewritten 03:14:20) |
| `Library/PackageCache` | 3 packages | **+ universal, universal-config, core, shadergraph, burst, collections, mathematics, light-transport, searcher, test-framework.performance** |
| `ProjectSettings/GraphicsSettings.asset` | `m_CustomRenderPipeline: {fileID: 0}` | **still `fileID: 0`** |
| `ProjectSettings/QualitySettings.asset` | — | **all levels `customRenderPipeline: {fileID: 0}`** |
| `Assets/**` URP assets | none | **still none** — no `UniversalRenderPipelineAsset` / `UniversalRendererData` / `VolumeProfile` |
| `Assets/.../Editor/UrpMigrationTool.cs` | untracked, 313 lines | unchanged (creates pipeline + renderer + volume + reflection probe) |

**Net state: the package is installed, but nothing is wired up.** The renderer is still rendering through Built-In because no SRP asset is assigned. `UrpMigrationTool.BuildPipeline` / `ConvertMaterials` / `BuildPostProcessing` have not been run. Note that `GameCaptureTool.cs` already references `UniversalAdditionalCameraData`, so the migration is committed to in code but not in settings.

**Concurrency warning:** new `.meta` files (`UrpMigrationTool.cs.meta`, `SignageArtTool.cs.meta`, `Signs.meta`) and the lock rewrite appeared mid-audit with no action from me. A Unity editor or another session is active on this project — respect the one-editor-at-a-time rule before running any batch command.

### 1.2 Package presence

| Need | Present? | Note |
|---|---|---|
| URP + Shader Graph | **Yes** (built-in pkg folder; manifest entry unresolved) | universal-config also bundled |
| Post Processing (URP Volume) | **Yes** — via URP; already scripted in `UrpMigrationTool.BuildPostProcessing` (ACES, bloom, colour, vignette) | no `Volume` asset on disk yet |
| TextMeshPro | **Yes** — `com.unity.ugui 2.0.0` (Unity 6 merged TMP) + bundled `com.unity.textmeshpro` | **zero TMP usage in code** (grep: 0 hits) |
| Cinemachine | **No** | not in manifest, not bundled — registry download only |
| Input System | **No** — `activeInputHandler: 0` (legacy) | `ProjectSettings.asset:679` |
| ProBuilder / Polybrush | **No** | registry download |
| Recorder / Addressables | **No** | registry download |
| Test Framework | Yes `1.6.0` | 115 EditMode tests live |
| Other bundled-but-unused | HDRP, VFX Graph, Terrain tools, Sentis, Path Tracing, Light Transport | free to enable per-package |

### 1.3 Toolchain

| Tool | Status | Evidence |
|---|---|---|
| Blender 4.5.15 | **Installed**, not on `PATH` | version probe returned `Blender 4.5.15 LTS` |
| Blender addons | Bundled only: `rigify`, `cycles`, `io_scene_fbx`, `io_scene_gltf2`, `node_wrangler`, `pose_library`, `hydra_storm`. User `addons/` folder **empty**; no extensions installed | `4.5\scripts\addons_core`, `%APPDATA%\Blender Foundation\Blender\4.5\extensions` |
| Python | **3.15.0** | `python --version` |
| Python libs | `numpy 2.5.4` ✅, `requests 2.34.2` ✅; **PIL, scipy, soundfile, trimesh, matplotlib, imageio, bpy all MISSING** | import probe |
| ffmpeg | **Not on PATH**, but usable: CapCut `21.7.0` and Tracktion Waveform 14 bundle one | `C:\Users\Asus\AppData\Local\CapCut\Apps\9.1.0.3879\ffmpeg.exe -version` |
| ImageMagick / sox | **Not installed** (`convert.exe` is Windows' disk tool, not IM) | `Get-Command` |
| git | `2.55.0.windows.3` ✅ | — |
| curl | Present (`curl.exe`; PowerShell alias shadows it) | — |
| dotnet | `10.0.401` ✅ | — |
| node / npm | `24.21.0` / `11.19.0` | — |
| GitHub CLI | **Not installed** | — |
| OBS Studio | **Installed** | `C:\Program Files\obs-studio` |
| Piper TTS | Installed under `Tools/Piper` (git-ignored) — 54 voice WAVs already rendered | `ArtSource/Voice/` |
| Disk free | **63.8 GB** on C: | `Get-PSDrive C` |

### 1.4 GPU / machine

| Component | Value | Rendering implication |
|---|---|---|
| GPU 0 | **NVIDIA RTX 3050 Laptop, 4 GB VRAM**, driver 32.0.16.1088 | capable of EEVEE, Cycles GPU, URP; **4 GB is the binding constraint** (ArmorPaint wants 6 GB) |
| GPU 1 | AMD Radeon 740M iGPU, 512 MB | ignore for rendering |
| CPU | Ryzen 7 7445HS, 6C/12T | fine for Blender bakes |
| RAM | 15.3 GB | adequate; 4K Cycles bakes will swap |

### 1.5 Current asset state (what the quality gap actually is)

| Area | Reality today |
|---|---|
| Characters | **Faceless tinted mannequin** on the UAL rig. 100 Avatars and Quaternius Man both failed Unity humanoid mapping (`realism-pass.md`) |
| Materials | 3 ambientCG sets, **1K, colour + normal only** — no roughness/metal/AO maps |
| Lighting | 4 point lights + 1 directional; **no HDRI, no skybox, no cubemap, no reflection probe asset** (grep for `*.exr`/`*.cubemap`: 0 hits) |
| Audio | 7 music tracks, 54 voice WAVs, a few SFX WAVs — but **no `AudioMixer`** (0 `.mixer` files): no buses, ducking, reverb, or master limiting |
| Post | None. No tonemapping/bloom/colour grading |
| Scripts | 51 runtime/editor `.cs` files |

## Part 2 — Free upgrade candidates

Ranked by **impact per hour**. "Acct" = needs an account. "Rule" = fits decision 0006 (no paid, no subscription, no AI assets).

| # | Candidate | Licence | Acct | Size | Impact here | Rule | Hours |
|---|---|---|---|---|---|---|---|
| 1 | **Finish URP + Volume post** | Unity | – | 0 (already cached) | **Very high.** ACES tonemap, bloom, grading, vignette, reflection probe — turns flat greybox into a lit room. Package resolved; tooling written; **never executed** | Needs settings approval | 1–2 |
| 2 | **Poly Haven HDRI** | **CC0** (verified) | No | 30–120 MB | **Very high.** Fixes "flat materials, dark ceiling". Gives real ambient + specular; makes every existing material better | ✅ | 0.5 |
| 3 | **Kenney / freesound SFX + AudioMixer** | CC0 / CC0-or-BY | No / freesound: yes | 20–80 MB | **High.** Machines, coins, doors, arcade. Zero SFX today | ✅ (mixer is in-engine) | 2–4 |
| 4 | **Quaternius Animated Men/Women or Downtown City** | **QAL v1.0** (verified 2026-08-28) | No | 10–60 MB | **High.** Only realistic fix for the faceless NPC; also street dressing for Bali | ✅ (already matrixed) | 4–8 |
| 5 | **ambientCG full PBR sets (2K: rough/metal/AO)** | CC0 | No | 100–200 MB | **High.** Roughness maps are the single biggest realism gain per material | ✅ | 2 |
| 6 | Cinemachine 3.x | Unity | – | ~10 MB | Medium. Arcade cam + reveal shots. Not needed for FPS core | approval | 2 |
| 7 | Unity Recorder | Unity | – | ~5 MB | Medium. Deterministic 4K trailer captures vs OBS | approval | 1 |
| 8 | ProBuilder + Polybrush | Unity | – | ~10 MB | Medium. In-editor iteration beats round-tripping Blender | approval | 2 |
| 9 | Material Maker | MIT | No | ~120 MB (Godot-based) | Medium. Procedural tile/neon/grunge without downloads | ✅ | 3 |
| 10 | Mixamo animations | Adobe terms | **Yes** | – | Low. UAL already supplies 45 clips; only needed for specials | ✅ per matrix | 1 |
| 11 | Unity Input System | Unity | – | ~5 MB | **Low/negative now** — `activeInputHandler: 0`; migrating touches every input call | approval | 6+ |
| 12 | Addressables | Unity | – | ~15 MB | Low. No content scale problem yet | approval | 4 |
| 13 | GIMP / Krita | GPL | No | 300–500 MB | Low. Need is maps, not painting | ✅ | – |
| 14 | Blender **Rigify** | GPL (bundled) | No | 0 | Medium **later**. Real fix for a bespoke CC0 body, not a quick win | ✅ | 8+ |
| 15 | MakeHuman (offline) | CC0 output | No | ~500 MB | Low. Slow, poor clothing, weak faces | ✅ | 6+ |
| 16 | VRoid / Ready Player Me | free, proprietary | **Yes** | ~200 MB | **Negative.** Anime/stylized, clashes with Bali realism | ✅ but off-brief | 4 |
| 17 | Quixel Megascans | Fab/Epic terms | **Yes (Epic)** | multi-GB | **Negative.** Account-gated, licence changed post-Fab; 4 GB VRAM | ⚠️ needs review | – |
| 18 | HDRP | Unity | – | 0 | **Negative.** Overkill for RTX 3050 laptop, full rework, no upside at this scope | approval | 20+ |
| 19 | ArmorPaint | zlib; **paid binaries** | Yes (buy) | 10 MB | **Negative.** 6 GB VRAM recommended vs 4 GB; paid | ❌ paid | – |
| 20 | LMMS / Sonic Pi | GPL | No | 150–300 MB | **Negative.** Music pipeline already works via CC-BY tracks | ✅ | – |

**Unverified:** exact registry versions/install sizes for Cinemachine, Recorder, ProBuilder, Addressables (Package Manager not queryable offline without opening Unity); Mixamo terms page is a JS app and returned no text; Megascans/Fab account requirement inferred, not confirmed this session; Synty free samples and Unity Asset Store free packs were **not** assessed (account + per-asset licence, low value vs the CC0 sources already in the matrix).

## Part 3 — Concrete recommendations

### 3.1 Top 5 additions

| Rank | Addition | Exact steps (not executed) | Licence | Approval needed |
|---|---|---|---|---|
| 1 | **Finish the URP migration** | Package is **already resolved** (done 03:14). Remaining: open `6000.0.64f1` → menu `Laundry Legends/Rendering/Build URP` → `Convert Materials` → `Build Post Processing` → `Validate URP` → run EditMode + PlayMode | Unity package terms | **Yes** — `GraphicsSettings`/`QualitySettings` are gated project settings. Commit the URP assets + `UrpMigrationTool.cs` + `GameCaptureTool.cs` together |
| 2 | **Poly Haven HDRI** (e.g. Bali/dusk or subtropical street) | Download 4K HDR from polyhaven.com → `Assets/LaundryLegends/Art/Textures/Environment/HDRI/` → Texture importer: shape=Cube, 2048 → Skybox/Panoramic material → set `RenderSettings.skybox`, `ambientMode=Skybox`, realtime GI off for now → add reflection probe (already scripted) | **CC0** | No (asset download already allowed; record inventory row) |
| 3 | **SFX pack + AudioMixer** | Get machine/coin/door/arcade SFX (Kenney Audio, or freesound CC0) → `Audio/SFX/` → create `LL_Master.mixer` with Master → Music / SFX / Voice groups → route existing sources → add a light reverb on SFX | Kenney **CC0**; freesound per-file | No for CC0; freesound needs a registered account → then **yes** |
| 4 | **Replace the mannequin** | Try Quaternius *Animated Men/Women* (already-rigged, animated) via the existing `render_character.py` / `inspect_character.py` harness → if Unity humanoid mapping fails again, fall back to `Mixamo` auto-rig or Blender Rigify on the Quaternius Man mesh | **QAL v1.0** (no resale of raw assets) | No (matrix already approved); Mixamo route needs **yes** (Adobe account) |
| 5 | **Upgrade materials to full PBR** | Download ambientCG 2K **Roughness/Metalness/AO** alongside existing colour/normal → assign in `MAT_FloorTile`/`MAT_WallPaint`/`MAT_CounterWood` → raise tiling on floor | **CC0** | No |

### 3.2 Top 5 free packs for a 2026 Bali laundromat + arcade

| Pack | Source / link | Licence | Why |
|---|---|---|---|
| **Poly Haven HDRI + texture library** | polyhaven.com | **CC0** | Bali sky, dusk, tropical streets; also neon/grime/metal PBR |
| **ambientCG material sets** | ambientcg.com | **CC0** | Already in use; add rough/metal/AO — highest realism per MB |
| **Quaternius Downtown City MegaKit** | quaternius.com | **QAL v1.0** | Modular street/shopfront for the Bali frontage, matches existing UAL art |
| **Quaternius Sushi Restaurant Kit / Ultimate House Interior** | quaternius.com | **QAL v1.0** | Counters, shelving, seating, plants — laundromat interior dressing |
| **Kenney furniture + audio + arcade-ish UI kits** | kenney.nl | **CC0** | Props, coin/machine SFX, UI frames for the arcade cabinet |

Also viable, unassessed this session: Poly Pizza (mixed CC0/CC-BY, per-model), KayKit (CC0, already downloaded), Sketchfab CC0 filter, Unity Asset Store free packs (per-asset licence).

### 3.3 What would NOT help (avoid the noise)

- **HDRP** — full rework, heavy for a 4 GB laptop GPU, no benefit at this fidelity target.
- **Input System migration** — legacy input already works and is tested; pure risk right now.
- **Addressables** — solving a scale problem the project does not have.
- **Megascans / Fab** — account-gated, licence drift, multi-GB, VRAM-heavy.
- **VRoid / Ready Player Me** — anime/stylized characters against a Bali-realism brief.
- **ArmorPaint** — paid binaries and wants 6 GB VRAM (machine has 4).
- **LMMS / Sonic Pi / Audacity** — music and voice pipelines already deliver; Audacity only if you need to trim the existing WAVs.
- **ImageMagick / sox** — ffmpeg 21.7 is already available for both image and audio conversion.
- **Blender addon shopping** — `rigify`, `io_scene_fbx`, `io_scene_gltf2`, `node_wrangler` are bundled; nothing missing for this workflow.
- **4K textures** — the project already ships 1K and the room is small; 4K wastes VRAM on a 4 GB card.

## Honest limits of this audit

- No Unity or Blender session was started *by me*, so nothing here is verified *in-engine*.
- The project mutated during the audit (URP resolved 03:14, new `.meta` files). The pipeline-assignment rows were re-verified at 03:16 and are current **as of that time**.
- Package Manager registry contents were not queried; versions/sizes for Cinemachine, Recorder, ProBuilder and Addressables are indicative **unverified**.
- Character replacement remains the project's hardest open problem; the previous two attempts failed at Unity's humanoid mapper, and this audit offers candidates, not a proven fix.
