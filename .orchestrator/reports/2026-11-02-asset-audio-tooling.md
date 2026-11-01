# 3D, Texture, Character and Audio Tooling — Verified Assessment

- **Date:** 2026-11-02
- **Type:** Research + isolated verification. No Unity run, no game code or `Assets/**` change, no package change.
- **Repo state observed:** `main` at `79693f4` (`phase-8-4-g79693f4`), dirty working tree owned by the lead agent (13 modified + ~30 untracked files, all user/lead work; untouched by this task).
- **Evidence:** `.orchestrator/reports/evidence/2026-11-02-asset-audio-tooling/`
- **Installed by me:** **nothing.** The assumed missing tool (Blender) is already installed; the actual blocker was a 55-byte config file.

---

## 0. Headline corrections to the task brief

| Brief said | Verified reality |
|---|---|
| "Blender is NOT installed" | **Blender 4.5.15 LTS is installed** (build 2026-10-22) at `C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe`. `blender --version` returns exactly that. `ASSET_PIPELINE.md` was right; the stale line is `AGENTS.md` → "Tool reality". |
| "`userhome` was deleted" | The directory tree exists but is **empty**: `ArtSource/Downloads/MakeHuman/userhome/makehuman/v1py3/{data,plugins,backgrounds,plugin_logs}` contain **0 files** and there is **no `settings.ini`**. That is the only thing blocking the script. |
| "MakeHuman data may be missing" | MakeHuman's real asset library is intact at `Tools/MakeHuman/makehuman/data` (clothes, hair, skins, rigs, 163-bone `default.mhskel`, FBX exporter plugin). **The userhome data folder is not needed**: the NPC plugin resolves every asset against `LL_MH_ROOT`. |
| "Blender + minimal userhome may re-export" | **Confirmed — done and verified end-to-end in ~28 s per character**, in an isolated temp home, without touching the repo. Result below. |

---

## 1. Task 1 — What is actually installed

`winget list` + direct path/`--version` probes:

| Tool | Version | Path | Licence | Automatable? |
|---|---|---|---|---|
| Blender | **4.5.15 LTS** | `%LOCALAPPDATA%\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe` | GPL-2.0-or-later (app); `.blend`/exports are ours | **Yes** — full CLI (`-b --python`), extension CLI |
| MakeHuman | **1.2.0 final** (Python 3.7.6, 507.6 MB) | `Tools/MakeHuman/` (git-ignored) | AGPLv3 app; bundled assets **CC0** | **Yes** — plugin API only (no `--nogui`) |
| Unity | 6000.0.64f1 (also 6000.0.0f1, 6000.5.7f1) | Unity Hub | — | Batch mode exists (not run here) |
| ffmpeg / ffprobe | **9.0.2** (Gyan full build) | `%LOCALAPPDATA%\Microsoft\WinGet\Packages\Gyan.FFmpeg_...\bin\ffmpeg.exe`, **on PATH** | LGPL-2.1+ core; this build is GPL-enabled | **Yes** — best single batch audio/image tool |
| Krita | **6.0.4** | `C:\Program Files\Krita (x64)\bin\krita.exe` | GPL-3.0 | **Yes** — `krita --export --export-filename out.png` |
| LMMS | **1.2.2** | `C:\Program Files\LMMS\lmms.exe` | GPL-2.0 | **Yes** — `lmms render <mmp> -o out.wav -f wav`, also `rendertracks` (verified in `--help`) |
| Audacity | **4.0.1** | `C:\Program Files\Audacity 4\bin\Audacity4.exe` | GPL-2.0+ | **No** — `--help` exposes no export/processing/CLI; GUI + macros only |
| SoX | not installed (winget has `ChrisBagwell.SoX 14.4.2`) | — | GPL-2.0 / LGPL-2.1 | Yes (CLI) if added |
| Material Maker | not installed (winget `RodZill4.MaterialMaker 1.7`) | — | MIT | GUI only |
| MPFB2 (Blender character add-on) | not installed; on `extensions.blender.org` | — | **GPLv3 code / CC0 assets** | **Yes** (`blender --command extension install -s -e mpfb`) |
| ImageMagick, Git LFS, GitHub CLI | not installed | — | — | ffmpeg covers the image jobs |

Other useful facts: Python 3.15 (numpy only), Node 24.21, git 2.55, **130.4 GB free on C:**.

### 1.1 What the repo scripts expect (read, not modified)

`ArtSource/Blender/makehuman_npc_run.ps1`:

- `Tools\MakeHuman\Python\python.exe` (embedded Python 3.7.6) — present.
- `Tools\MakeHuman\makehuman\makehuman.py` as the entry point — present; **there is no `makehuman.exe`**.
- `ArtSource\Downloads\MakeHuman\userhome\makehuman\v1py3\plugins` — exists, empty.
- `...\v1py3\settings.ini` — **missing → the script throws at line 18 before doing anything.**
- `ArtSource\Blender\makehuman_characters.json` — 2 characters, `Attendant_Human` (male, `male_casualsuit06` + `shoes04` + `short04`) and `Customer_Human` (female, `female_casualsuit01` + `shoes06` + `ponytail01`).
- Outputs `ArtSource\Exports\Characters\<name>.fbx` + a `textures\` folder; log to `TestResults\makehuman-<name>.log`.
- **It does not apply the FBX exporter patch** (only `makehuman_pilot_run.ps1` does). The patch is currently applied on disk (`PremultiplyAlpha=False`, `Texture alpha=0.0`, `AlphaSource=0`, `ReflectionFactor=0.0` in both `fbx_material.py` and `fbx_binary.py`) — verified by grep. A fresh extract of MakeHuman would silently regress this.

`makehuman_npc_plugin.py`: uses `MH_ROOT/data` for skin/clothes/rig, falls back `.mhclo → .mhpxy` (the installed library only ships `.mhpxy`), forces every material opaque, exports binary FBX with `scale=0.1`, unit `m`, then writes a run log.

---

## 2. Task 2 — Can a minimal userhome re-export the characters? **Yes. Verified.**

### 2.1 What was actually done (isolated, no repo writes)

Temp home: `%TEMP%\opencode\lh-mh-verify\userhome`, containing only:

```
makehuman\v1py3\plugins\npc_export.py      (copy of makehuman_npc_plugin.py)
makehuman\v1py3\settings.ini               (55 bytes: {"version":"1.2.0","activeUserPlugins":["npc_export"]})
```

Run: `MH_HOME_LOCATION=<temp>`, `python.exe makehuman.py`, cwd `Tools/MakeHuman/makehuman`.

| Run | Result | Time | Output |
|---|---|---|---|
| Customer, `female_casualsuit01` + `shoes06` + `ponytail01` | exit 0, all 3 proxies attached, 9 980 body faces hidden after fix | **28 s** | `Verify_Customer.fbx` 1.95 MB → 1.56 MB with fix; 6 textures |
| Same, `female_sportsuit01` (control) | exit 0 | ~28 s | 1.92 MB FBX |
| Blender round-trip (repo's `makehuman_verify_render.py`, Cycles) | 163 bones, height **1.6298 m**, humanoid root `root`, all meshes skinned | ~60 s | report + renders in evidence folder |

MakeHuman rewrote `settings.ini` with the full default schema on exit, so the file self-heals after the first bootstrap.

### 2.2 A real defect found — and a verified fix

`human.maskFaces()` (what the plugin calls) only applies the **static helper/joint face mask**. It does **not** apply the garments' `deleteVerts` vertex mask, which is what the GUI's clothes chooser does (`3_libraries_clothes_chooser.updateFaceMasks`). Consequence: the exporter still ships the **whole naked body**, and on close-fitting garments the skin z-fights through the clothing.

Evidence (all in `evidence/`):

| Image | What it shows |
|---|---|
| `customer-before-maskfix.png` | Skin poking through the tee and jeans; garment looks torn |
| `customer-garment-only.png` | Same FBX with the body mesh hidden → **garment is intact and fully covers** |
| `customer-after-maskfix.png` | With the vertex mask applied → clean blue tee + full-length jeans, no poke-through |
| `sportsuit01-crop-top.png` | `female_sportsuit01` is a **crop top + leggings** → not modest; do not use for the customer |

Verified patch (replaces the `human.maskFaces()` block in `makehuman_npc_plugin.py`):

```python
import numpy as _np
human.maskFaces()
vertsMask = _np.ones(human.meshData.getVertexCount(), dtype=bool)
for pxy in human.clothesProxies.values():
    dv = getattr(pxy, "deleteVerts", None)
    if dv is not None and _np.any(dv):
        vertsMask[dv] = False          # hide body surface under the garment
human.changeVertexMask(vertsMask)
```

Effect: 5 108 → **9 980** hidden body faces; 41 448 → **31 704** exported tris; FBX 1.95 → 1.56 MB. Height, rig (163 bones), hair, eyes and shoe materials unchanged.

### 2.3 Outfit answer (the actual question)

`female_casualsuit01` is a **blue short-sleeve T-shirt + full-length denim jeans** (verified from the 2048² diffuse atlas and the render). That is a modest, readable retail-customer outfit and the existing JSON change is correct. Alternatives in the installed library:

| Garment | Coverage | Verdict |
|---|---|---|
| `female_casualsuit01` | tee + full jeans | **Use** (current JSON) |
| `female_sportsuit01` | crop top + leggings | Not modest |
| `female_casualsuit02` | the old revealing suit | Do not use |
| `female_elegantsuit01` | long-sleeve striped blouse only (no bottoms in the atlas) | Needs a second garment; not drop-in |
| `male_casualsuit06` | attendant's current male outfit | Keep for attendant |
| `male_worksuit01` | denim overalls/apron, no top | Male cut; not for the customer |

### 2.4 Exact steps to unblock (copy-paste)

```powershell
# 1) one-time userhome bootstrap (UTF-8 WITHOUT BOM — MakeHuman chokes on a BOM)
$v = 'ArtSource\Downloads\MakeHuman\userhome\makehuman\v1py3'
New-Item -ItemType Directory -Force "$v\plugins" | Out-Null
[System.IO.File]::WriteAllText("$v\settings.ini",
  '{"version":"1.2.0","activeUserPlugins":["npc_export"]}',
  (New-Object System.Text.UTF8Encoding($false)))

# 2) apply the mask fix to ArtSource/Blender/makehuman_npc_plugin.py (snippet in §2.2)

# 3) re-run the verified harness (28 s/character, needs an OpenGL context)
powershell -ExecutionPolicy Bypass -File ArtSource\Blender\makehuman_npc_run.ps1

# 4) sanity-check the export in Blender (no Unity needed)
blender.exe -b --factory-startup --python ArtSource\Blender\makehuman_verify_render.py -- `
  --fbx ArtSource\Exports\Characters\Customer_Human.fbx `
  --outdir ArtSource\Exports\Characters --report ArtSource\Notes\customer_verify.txt
```

Then, in a Unity step (lead agent, not run here): copy `Customer_Human.fbx`/`Attendant_Human.fbx` into `Assets/LaundryLegends/Art/Models/NPC/Variants/`, copy the new `textures/*.png` into `Assets/LaundryLegends/Art/Textures/NPC/`, and **update `CharacterAppearanceFixTool.cs`** — it still hard-codes `female_casualsuit02_diffuse.png` / `_normal.png` (line ~42). Without that edit the prefab keeps the old suit material.

### 2.5 Risks / limitations of this path

| Risk | Detail | Mitigation |
|---|---|---|
| `settings.ini` bootstrap | Hand-written file must be UTF-8 **without BOM** and carry `"version":"1.2.0"`; wrong version silently loads defaults and ignores `activeUserPlugins` | The 55-byte file above is verified |
| FBX exporter patch not idempotent in `npc_run.ps1` | Fresh MakeHuman extract → transparent garments + metallic skin again | Run the patch block from `makehuman_pilot_run.ps1`, or move it into the NPC script |
| No `--nogui` | Needs an OpenGL context and a desktop session; it is unattended but not headless/CI-safe | Keep on the lead's workstation |
| Face modifiers silently skipped | Both specs' modifiers (`head/head-scale-horiz-decr-incr`, `chin/chin-width-decr-incr`) are **unknown** in this build → 0/2 applied, generic face | Dump `human.modifierNames` and correct the JSON |
| Texture size | 2048² diffuse, 4096² normal per garment; ~14 MB per character | Downscale to 1024² in the Unity importer |
| Triangle count | ~31.7 k tris/character (body 17 k + hair 5.3 k + garment 4.2 k + shoes 3 k) | Acceptable for hero NPCs; not for crowds |
| AGPL / community shutdown | App is AGPLv3 (fine as a build-time tool, not shipped); MakeHuman community services **shut down 2026-10-21** | The local install is self-contained; do not depend on live downloads |
| Unity side unverified | No import, avatar mapping, material or capture check was run | Track as part of the re-export task |

**Effort to finish the re-export: S (≈30–60 min incl. Unity import/capture). No install required.**

---

## 3. Task 3 — Prioritized tool set

Priority = value to *this* game now. "Agent-driven" means an agent can invoke it without a human clicking.

| # | Tool | Purpose | Install | Licence | Agent-driven | Effort |
|---|---|---|---|---|---|---|
| **P0** | **MakeHuman 1.2.0** (installed) | Two realistic NPC bodies + CC0 wardrobe (13 garment items, 6 shoes, hair, skins) | none — `Tools/MakeHuman/` | AGPLv3 app / **CC0 assets** | **Yes** — plugin API (verified) | **S** |
| **P0** | **Blender 4.5.15 LTS** (installed) | Props, appliances, arcade cabinet, rigs, FBX/glTF export, bakes, renders | none | GPL-2.0+ | **Yes** — `-b --python` | S |
| **P0** | **ffmpeg 9.0.2** (installed) | Trim/loop/normalise/loudnorm, wav↔ogg/mp3, sprite/texture conversions, contact sheets | none (winget `Gyan.FFmpeg`) | LGPL/GPL build | **Yes** | S |
| **P0** | **Kenney audio packs** (Interface, Impact, Casino, Foley, UI) | UI clicks, coin drops, thuds, door/drawer Foley — the exact vocabulary this game needs | download ZIP from kenney.nl | **CC0** | Partial (curl + Expand-Archive; no official API) | S |
| **P0** | **Poly Haven API / ambientCG** | CC0 PBR surfaces, HDRIs, some models; ambientCG already supplies 340 MB in-repo | no install; `curl` / `polydown` | **CC0** | **Yes** — documented JSON APIs | S |
| **P1** | **MPFB2** (Blender extension) | Maintained successor to standalone MakeHuman: bodies, CC0 clothes/hair, Rigify/Mixamo rigs, Python API | `blender --command extension install -s -e mpfb` | GPLv3 code / **CC0 assets** | Yes (CLI install; scripted build possible) | **M** |
| **P1** | **Material Maker 1.7** | Procedural PBR authoring: trim sheets, grime, neon, machine panels → Unity maps | `winget install RodZill4.MaterialMaker` (~105 MB) | **MIT** | No CLI (project files are portable `.ptex`) | M |
| **P1** | **LMMS 1.2.2** (installed) | Original music beds/jingles with built-in synths | none | GPL-2.0 | **Yes** — `lmms render` (verified) | M |
| **P1** | **Krita 6.0.4** (installed) | Signage/decal/texture touch-up, normal-map painting, logo cleanup | none | GPL-3.0 | **Yes** — `krita --export` | S |
| **P2** | **SoX 14.4.2** | Batch trim/resample/normalise + simple synthesis for placeholder SFX | `winget install ChrisBagwell.SoX` (~10 MB) | GPL-2.0/LGPL-2.1 | **Yes** | S |
| **P2** | **Audacity 4.0.1** (installed) | Manual mastering, spectral repair, noise reduction | none | GPL-2.0+ | **No** CLI | S |
| **P2** | **Sonniss GDC bundles** | Huge pro SFX library (washer hums, doors, coins, crowd) | no install — direct ZIP | Royalty-free, no attribution; **AI-training prohibited** | Manual download | M |
| **P2** | **Mixamo** | Free humanoid idle/walk/carry/lift clips | web, Adobe account | Adobe free terms; **no standalone redistribution** | No API | M |
| **P3** | **Substance 3D / Marvelous Designer** | Best-in-class texturing / cloth | paid subscription | Paid | Partly (Substance has CLI baking) | L |
| **P3** | **AI 3D/audio generators** (Meshy, Tripo, Suno, ElevenLabs) | Fast asset generation | paid / account | Often unclear on training data & redistribution | API exists | — |

**Not approved by project policy:** AI-generated assets (`ASSET_PIPELINE.md` §10). Do not use them without a new owner decision, regardless of tooling convenience.

### 3.1 By asset class

**3D props / appliances.** Blender scripting is already the project's strongest pipeline: `create_appliance_hd.py`, `create_arcade_cabinet.py`, `create_street_props.py`, `create_laundry_props.py` produced every prop in the game. Continue this; it is deterministic, diff-able and free. Use Kenney/Quaternius kits only for bulk background dressing. Poly Haven models are CC0 but photoreal, which fights the stylised look.

**PBR textures.** ambientCG (CC0, JSON API v3, `downloadLink` redirects) is the workhorse and already in-repo. Poly Haven's API became **free for commercial use on 2026-07-18** and only requires a descriptive `User-Agent`. Material Maker (MIT) is the only free procedural authoring option and is worth installing when the machine fronts need consistent grime/wear rather than photoscan tiles.

**Character clothing.** Three viable routes, in order: (1) the installed MakeHuman CC0 library — zero download, already proven; (2) MPFB2's CC0 asset packs inside Blender — maintained, better rigging; (3) hand-modelled clothing in Blender with a cloth sim — highest effort. Paid cloth tools are not justified at this scope. Avoid the Quaternius licence trap: assets may be used but **not redistributed as raw packs**.

**SFX.** Kenney CC0 for UI/coins/impacts (small, cohesive, no attribution). Sonniss GDC for realistic machine/room sounds (one part, not the 23.5 GB set). Freesound CC0 as a fallback (needs an account + API key). Generate genuinely laundromat-specific layers (washer spin hum, coin slide, door latch) with ffmpeg/SoX synthesis + pitch/loop shaping — cheap, owned, and easy to iterate.

**Music.** Already eight licensed tracks in `Assets/.../Audio/Music` (see §4). If more is needed, Kevin MacLeod (CC BY 4.0, credit) and Pixabay Music (no attribution, no standalone resale) are the low-friction options; FMA's gamelan holdings are mostly **BY-NC** and unusable. LMMS can render original loops from the CLI if a bespoke motif is wanted.

---

## 4. Task 4 — What already exists and should be reused

| Asset | Where | Licence | Reuse note |
|---|---|---|---|
| **8 music tracks** (BalinesePrelude, TempleOfHangzhou, ThatZenMoment, Ripples, Healing, MeditationImpromptu01–03) | `Assets/LaundryLegends/Audio/Music/` | Balinese/CC BY + Kevin MacLeod CC BY 4.0 (recorded in `ArtSource/Licenses/`) | Already shipped; do not buy music before auditioning these |
| **54 dialogue WAVs** + 5 legacy clips | `Assets/LaundryLegends/Audio/Voice/` | Piper outputs (public-domain voices) | Recorded but **never listened to**; a listening pass is cheaper than regenerating |
| **Piper TTS + 4 voices** | `Tools/Piper/` (git-ignored, 375 MB) | MIT-era engine; PD voices | Regenerate any line in seconds via `Tools/Voice/render_dialogue.py` |
| **14 Blender props**: Appliance, ApplianceHD (+dryer), ArcadeCabinet, Basket, Bag, LooseLaundry, WrappedBundle, Bench, CurbStrip, PottedPlant, RoadMarking, StackChair, StreetLamp, FirstPersonHands | `ArtSource/Blender/*.blend` + `ArtSource/Exports/*.fbx` | Original project art | Source scripts are parametric — variant a colour/size instead of modelling anew |
| **HD appliance texture set** (albedo, normal 7.7 MB, roughness) | `ArtSource/Exports/appliance_*` | Original | Already baked; reuse for any white-goods variant |
| **7 ambientCG 4K PBR materials** (~255 MB in `ArtSource/Textures/`; tile, plaster, wood, metal, concrete, asphalt, rubber) | `ArtSource/Textures/`, imported to `Assets/.../Textures/Environment/` | CC0 | Covers every current interior/exterior surface; extend by API rather than buying packs |
| **Decal library** (water stain, floor grime, wall scuff, rust leak, speckle, notices) | `Assets/.../Textures/Decals/` + `build_decals.py` | Original/CC0 | Procedurally generated; extend rather than source new |
| **9 signage textures** | `Assets/.../Textures/Signs/` | Original | Generated text/logo; edit with Krita |
| **MakeHuman clothing/hair/skin library** | `Tools/MakeHuman/makehuman/data/` | **CC0** | 13 garment items, 6 shoes, hair, skins — no download needed |
| **Quaternius UAL mannequin + 45 clips** | `Assets/.../Art/Animations/`, `Models/NPC/` | CC0 | Already wired into `NPC_Locomotion.controller` |
| **3 × 100 Avatars CC0 humanoids** | `Assets/.../Art/Models/NPC/` | CC0 | Unused after the mannequin swap; usable as background extras if re-rigged |
| **Camera/capture tooling** | `Assets/LaundryLegends/Editor/GameCaptureTool.cs`, `tools/game-vision/` | Original | How visual verdicts should be produced |

**Not reusable:** `ArtSource/Downloads/Characters/`, `Music/`, `Music2/`, `_texture_zips/` are **empty directories** — the staged third-party source packs are gone. Re-importing those characters would require fresh downloads (and a licence row each).

---

## 5. Task 5 — Installs and uninstalls

**Installed by this task: none.** Nothing was added to PATH, the registry, `Packages/**`, `ProjectSettings/**` or `Assets/**`. All verification ran in `%TEMP%\opencode\lh-mh-verify`.

If the owner later wants the recommended additions, these are the exact commands and their reversals:

| Tool | Install | Uninstall |
|---|---|---|
| MPFB2 | `blender --command extension install -s -e mpfb` | `blender --command extension remove mpfb` |
| Material Maker | `winget install --id RodZill4.MaterialMaker -e` | `winget uninstall --id RodZill4.MaterialMaker` |
| SoX | `winget install --id ChrisBagwell.SoX -e` | `winget uninstall --id ChrisBagwell.SoX` |
| Kenney / Sonniss packs | unzip into `ArtSource/Downloads/…` (git-ignored) | delete the folder |
| Blender (already) | portable ZIP, no registry | delete `%LOCALAPPDATA%\Programs\BlenderFoundation` |
| MakeHuman (already) | extracted installer payload | delete `Tools/MakeHuman` |
| Audacity / Krita / LMMS / ffmpeg (already) | winget | `winget uninstall --id Audacity.Audacity` / `KDE.Krita` / `LMMS.LMMS` / `Gyan.FFmpeg` |

---

## 6. Checks performed / not performed

**Performed**
- `winget list` + `Get-Command` + path probes for all tools above (2026-11-02).
- Read `makehuman_npc_run.ps1`, `makehuman_pilot_run.ps1`, `makehuman_npc_plugin.py`, `makehuman_characters.json`, `mh2fbx.py`, `guicommon.py`, `3_libraries_clothes_chooser.py`, `getpath.py`, `mhmain.py`.
- Two full MakeHuman exports in an isolated temp home (exit 0, 28 s each) + one Blender round-trip and four Cycles renders.
- Confirmed texture alpha is fully opaque for `female_casualsuit01` (min/mean/max = 1.0) — the see-through look was body interpenetration, not alpha.
- Verified `lmms --help` exposes `render`/`rendertracks`; `krita --help` exposes `--export`; `Audacity4 --help` exposes no batch options.
- Verified the Blender extension repo is configured and `extension install` exists.

**Not performed (recorded as unrun)**
- Unity Editor launch, FBX import, avatar mapping, prefab/material wiring, capture — **none**.
- Listening test of music/SFX/voice.
- In-game visual check of the fixed character.
- Performance measurement of the 31.7 k-tri character.
- No web page was fetched for Mixamo terms this session (earlier report recorded a 403); treat those terms as "recheck at download".

---

## 7. Recommended next 3 actions

1. **Finish the customer re-export (no installs, ~1 h).** Bootstrap `settings.ini` with the verified 55-byte file, apply the `deleteVerts` vertex-mask patch to `makehuman_npc_plugin.py`, run `makehuman_npc_run.ps1`, confirm with `makehuman_verify_render.py`, then — in Unity — import the two FBXs, copy the new textures, and update `CharacterAppearanceFixTool.cs` from `female_casualsuit02_*` to `female_casualsuit01_*`. Capture front/three-quarter shots before accepting.
2. **Give the game an audio pass that costs nothing but downloads (~1 h).** Pull three Kenney CC0 packs (Interface, Impact, Casino) plus one Sonniss part, pick ~8 SFX (coin, drawer, door, washer start/stop, button), normalise with `ffmpeg loudnorm`, and fill the empty `Audio/SFX` and `Audio/UI` folders; record licence rows. Then audition the 8 existing music tracks and the 54 voice lines — both are already paid for and never heard.
3. **Decide the long-term character path (~2 h spike).** Install MPFB2 into Blender and build one character headless to compare quality/effort against the now-working MakeHuman 1.2.0 harness. Keep 1.2.0 as the known-good fallback; do not delete it. Only if MPFB2 wins should the MakeHuman scripts be retired.

---

## 8. Ten-line summary

1. Blender **is** installed (4.5.15 LTS) — the brief's "not installed" is stale; nothing needed to be installed for this task.
2. The real blocker was a missing 55-byte `settings.ini`; the MakeHuman userhome otherwise only needed empty `plugins/` and `data/` folders.
3. A minimal userhome plus the existing plugin re-exported the customer in **28 seconds**, exit 0, 1.95 MB FBX with textures.
4. Blender round-trip confirmed 163 bones, 1.63 m height, complete skinning, and all six textures resolving.
5. The old exported body was never masked under clothing; that is why the customer looked undressed and torn.
6. Applying the clothing `deleteVerts` vertex mask (the clothes chooser's own logic) fixes it: 9 980 faces hidden, 31.7 k tris, clean tee-and-jeans render.
7. `female_casualsuit01` is a blue T-shirt with full-length jeans — modest and correct; `sportsuit01` is a crop top and should not be used.
8. Unity still needs the import, the new textures copied, and `CharacterAppearanceFixTool.cs` switched off `female_casualsuit02_*`.
9. Best free tooling: installed Blender/MakeHuman/ffmpeg/Krita/LMMS, plus CC0 Kenney audio, Poly Haven/ambientCG textures, and optionally MPFB2, Material Maker and SoX.
10. Reuse first: 8 licensed tracks, 54 voice lines, 14 Blender props, 255 MB of CC0 PBR, a decal library and 13 garments already sit in the repo.
