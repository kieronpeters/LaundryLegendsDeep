# Tools and Free 4K Assets — Decision-Ready Research

- **Date:** 2026-11-01
- **Type:** Research only. Nothing installed, nothing downloaded, no `Assets/`, `Packages/`, or `ProjectSettings/` change. One file written (this report).
- **Question:** for a Unity 6 (URP 17) laundromat/arcade sim on a 4 GB RTX 3050 laptop, which free tools cover 3D modelling, music, storyboard/animatics and general asset creation, which CC0 sources supply 4K arcade/buildings/walls/floors/roofs/textures, what is actually achievable at 4K, and what should be installed first.
- **Method:** live HTTP fetch of every vendor/download/licence page listed in §7; GitHub API for release asset sizes and winget manifest IDs; Sketchfab public search API for real CC0 counts; local filesystem probes for Blender add-ons, disk and GPU.
- **Evidence standard:** every URL below returned HTTP 200 unless §7 says otherwise. Sizes are **measured from the vendor file** (Content-Length or GitHub release asset) unless marked *est.*

> **Read §0 first.** It contains the four decisions that matter; the rest is evidence.

---

## 0. Verdict in one page

| # | Decision | Why (one line) |
|---|---|---|
| 1 | **Do not install a "blockbuster toolbox."** Install **5 small tools** (~456 MB total) on top of the Blender you already have. | The project's look is blocked by lighting, characters and PBR maps — not by missing DCC software (§3). |
| 2 | **Target "good T1", not 4K-native photorealism.** Render 1440p → upscale to 4K; 2K textures by default, 4K only on the floor and one hero surface. | A 4 GB laptop GPU cannot hold a 4K-native, 4K-textured interior with post; the maths is in §3.1. |
| 3 | **Skip photogrammetry, ArmorPaint, Ardour, Quixel, GameTextures and Textures.com entirely.** | CUDA/VRAM/account/paid-tier/licence problems respectively; each is a dead end *for this project* (§1.3, §1.2, §2.1). |
| 4 | **The arcade cabinet should be built in Blender**, kitbashed from Kenney *Mini Arcade* proportions, reusing the existing `create_appliance.py` parametric pipeline. | There is no good free arcade-cabinet model library: Sketchfab CC0 returns **0** results for "arcade cabinet" (§2.2). |

**Context corrections** (the brief's stated machine state is stale — verified this session):

| Claim in brief | Reality now | Evidence |
|---|---|---|
| Blender not installed | **Blender 4.5.15 LTS installed** at `C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\...\blender.exe` | `Test-Path` = True, ProductVersion 4.5 |
| Greybox art, no pipeline | Phase 8; URP 17.0.4 wired; **340 MB of 4K ambientCG maps already on disk** | `PROJECT_STATE.md`, `ArtSource/Textures/` = 339.7 MB |
| — | **58.5 GB free on C:**, not the 63.8 GB recorded in the earlier audit | `Get-PSDrive C` |
| — | GPU confirmed: **RTX 3050 Laptop, 4 GB**, driver 32.0.16.1088; iGPU Radeon 740M 0.5 GB | `Get-CimInstance Win32_VideoController` |

---

## 1. Tools (free / open source)

Column meanings: **Size** = installer/archive download as measured (installed footprint is larger); **Unattended** = safe to install with no human at the keyboard; **Verdict** = scoped to *this* project, not a general judgement.

### 1.1 3D modelling, sculpting, retopo

| Tool | What it does | Source | Licence | Win | Size | Unattended | Verdict |
|---|---|---|---|---|---|---|---|
| **Blender 4.5.15 LTS** | Modelling, sculpt, UV, bake, animation, FBX/glTF export | https://www.blender.org/download/lts/ · licence https://www.blender.org/about/license/ | GPL-2.0-or-later (app); **your .blend and exports are yours** | Yes | 380.2 MB zip | **Yes** — portable ZIP, no installer, no registry, no admin | **Keep. Already installed. Do not upgrade mid-phase.** |
| **Blender Extensions: LoopTools, F2, Auto Mirror** | Loop/spin/relax tools, one-key face fill, mirror+apply | https://extensions.blender.org/add-ons/looptools/ · https://extensions.blender.org/add-ons/f2/ · https://extensions.blender.org/add-ons/auto-mirror/ | GPL | Yes | <1 MB each | Yes (in-app install) | **Optional, free, 2 minutes.** LoopTools is the single most useful free modelling helper. |
| **RetopoFlow** | Dedicated retopology suite inside Blender | https://github.com/CGCookie/retopoflow | GPL-3.0 for **code**; non-code assets are not | Yes | *est.* 5–20 MB | Yes (add-on zip) | **No.** We retopo nothing: our meshes are parametric low-poly (300–1,500 tris). |
| **Blendkit (ex-BlenderKit), free tier** | In-Blender asset library (models/materials/HDRI), and a PBR/asset browser | https://www.blendkit.com/ · licences https://www.blendkit.com/docs/licenses/ | Two licences, both commercial-use; **Royalty Free forbids reselling the asset itself** | Yes (add-on) | *est.* 10–50 MB | Needs **account** | **Optional.** Useful for reference blocks; every asset must still be licence-audited per item. Not needed given Kenney/Poly Haven. |
| HardOps / Boxcutter / MeshMachine | Paid add-on market (no free equivalent of note) | — | Paid | — | — | — | **No.** Paid, and our hard-surface work is scripted. |

> Verified on disk: the installed Blender bundles only `cycles, rigify, node_wrangler, io_scene_fbx, io_scene_gltf2, pose_library, hydra_storm, io_anim_bvh, io_curve_svg, io_mesh_uv_layout, ui_translate, viewport_vr_preview, bl_pkg`. **LoopTools/F2 are not bundled in 4.5** — they come from extensions.blender.org.

### 1.2 PBR texturing

| Tool | What it does | Source | Licence | Win | Size | Unattended | Verdict |
|---|---|---|---|---|---|---|---|
| **Material Maker** | Node-graph procedural PBR authoring; exports Unity/Unreal/Godot maps; community library of materials, brushes, environments | https://materialmaker.org/ · https://github.com/RodZill4/material-maker | **MIT** | Yes (Godot-based) | 104.5 MB zip (v1.8) | **Yes** — portable zip, no admin | **Install.** Only free way to author trim sheets, grime decals and neon panels procedurally. |
| **ArmorPaint** | GPU 3D painting (Substance-Painter-like); textures a mesh by hand | https://armorpaint.org/ · https://github.com/armory3d/armorpaint | Source: zlib-like (GitHub reports NOASSERTION); **official binaries are paid** | Yes | *est.* 50–100 MB source | **No** — binaries must be bought; building needs the Armory SDK + MSVC | **No.** Vendor page states **"a minimum of 6 GB of video memory is recommended"**; we have 4 GB. |
| **Quixel Megascans / Fab** | Huge scanned PBR library | https://www.fab.com/ | Epic/Fab terms, account-bound | Yes | multi-GB | No (account) | **No.** Licence drift post-Fab, account-gated, VRAM-heavy. ambientCG is CC0 and equivalent for our needs. |
| **Substance Painter/Designer** | Industry standard | — | Paid subscription | — | — | — | **No.** Cost gate; Material Maker + ambientCG cover the gap. |

### 1.3 Photogrammetry / scan cleanup

| Tool | What it does | Source | Licence | Win | Size | Unattended | Verdict |
|---|---|---|---|---|---|---|---|
| **Meshroom** | Node-based image→textured-mesh reconstruction (AliceVision) | https://alicevision.org/ · https://github.com/alicevision/Meshroom | **MPL-2.0** | Yes | **not published on the fetchable page — unverified**; commonly multi-GB | No | **No.** No photo sets exist, no scan target in scope, and dense reconstruction is CUDA + VRAM heavy on 4 GB. |
| **COLMAP** | SfM + MVS, CLI and GUI | https://github.com/colmap/colmap | **new BSD** | Yes | **395 MB** (`colmap-x64-windows-cuda.zip`, v4.2.1) | Partly (portable-ish zip) | **No.** Same reason; CLI-first workflow we have no use for. |

*Unverified:* Meshroom's CUDA requirement is documented by AliceVision but the requirements page (`meshroom-manual.readthedocs.io/.../requirements.html`) returned **404** this session, and no page I fetched mentioned CUDA. Treat as unconfirmed.

### 1.4 Music and SFX

| Tool | What it does | Source | Licence | Win | Size | Unattended | Verdict |
|---|---|---|---|---|---|---|---|
| **LMMS** | DAW: step sequencer, built-in synths (TripleOscillator, ZynAddSubFX), MIDI, WAV/OGG/MP3 export | https://lmms.io/download | **GPL-2.0** | Yes | 35.1 MB (1.2.2 win64) | Yes (installer/winget) | **Install only if original music is required.** Caveat: **stable release is 1.2.2 from 2020-06-25** — old, no VST3, but the built-in synths are enough for a loop. |
| **Ardour** | Full multitrack DAW | https://community.ardour.org/download | **GPL-2.0** | Yes, **but prebuilt Windows binaries are paid** ("don't want to or cannot pay → build from source") | *est.* 100 MB | No (payment + build) | **No.** LMMS/Audacity cover the need; Ardour's Windows path is a paid binary or a painful build. |
| **Audacity** | Audio editor: trim, loop, normalise, loudness, noise reduction, batch convert | https://www.audacityteam.org/ | **GPL-2.0-or-later** | Yes | 47.5 MB (4.0.1 x64 msi) | Yes | **Install.** 54 voice WAVs and future SFX need trimming, looping and level-matching; this is the cheapest quality win in audio. |
| **MuseScore Studio** | Notation / score engraving | https://musescore.org/ | GPL-3.0 | Yes | *est.* 100–200 MB | Yes | **No.** Notation is irrelevant to a laundromat sim. No winget ID found (`MuseScore.MuseScore` → 404). |
| **jsfxr (sfxr.me)** | Browser 8-bit SFX generator (coin, laser, jump, powerup, hit) | https://sfxr.me/ | MIT-style (jsfxr, open source) | Browser, **no install** | 0 MB | n/a | **Use.** Exactly the arcade SFX vocabulary; zero install, zero licence friction. |
| **ChipTone** | Browser chiptune/SFX generator by SFB Games | https://sfbgames.itch.io/chiptone (note: `sfbg.com/chiptone` → **404**) | Free download | Browser/no-install | ~few MB | n/a | **Optional.** sfxr.me covers the same ground. |

**Licence-clean sample sources**

| Source | Licence | Account | Size | Note |
|---|---|---|---|---|
| **Sonniss GDC Game Audio Bundles** — https://sonniss.com/gameaudiogdc | Royalty-free, **commercially usable, no attribution**; **AI/ML training prohibited** | No | **23.5 GB** for GDC 2024 alone (9 parts) | Superb quality, absurd size for a 5-SFX need. Download one part, not the set. |
| **Freesound CC0 filter** — https://freesound.org/ | Per-file; CC0 filter available | **Yes** (login to download) | — | Could not verify this session: `freesound.org` returned **403** to automated fetch. |
| **Kenney audio packs** (Casino Audio, Interface Sounds, Impact Sounds) — https://kenney.nl/assets/casino-audio | **CC0** (verified on asset pages) | No | *est.* 5–30 MB each | Arcade coins, buttons, thuds — the on-theme choice. |
| **Kevin MacLeod / incompetech** — https://incompetech.com/ | CC-BY 4.0 (credit required) | No | — | Already in use in this project (`AUDIO-MUSIC-002`). |

### 1.5 Storyboard / animatics

| Tool | What it does | Source | Licence | Win | Size | Unattended | Verdict |
|---|---|---|---|---|---|---|---|
| **Krita** | Raster/vector painting, animation timeline, frame export; also texture touch-up and normal-map hand edits | https://krita.org/ | **GPL-3.0** | Yes | 157.7 MB (5.3.3 setup) | Yes (winget) | **Install.** Doubles as image editor and storyboard tool; replaces GIMP for our needs. |
| **Storyboarder** | Dedicated shot-list storyboarder with animatic playback and FFmpeg export | https://wonderunit.com/storyboarder/ · https://github.com/wonderunit/storyboarder | GitHub reports **no detected licence** (verify `LICENSE` before relying on it); tool licence does not affect your drawings | Yes | 119.5 MB (2.1.0) | Likely (Electron) | **Optional.** Best-in-class for boards, but **last repo activity 2024-03**; Krita + FFmpeg produce an animatic without it. |
| **Pencil2D** | Simple 2D frame-by-frame animation | https://www.pencil2d.org/ | **GPL-2.0** | Yes | 68.9 MB (0.7.2) | Yes | **No.** Krita's timeline covers it. |
| **OpenToonz** | Production 2D animation pipeline (Toonz-derived) | https://opentoonz.github.io/ | BSD-3-Clause per project site (GitHub: NOASSERTION) | Yes | 57.0 MB (1.8.0) | Yes | **No.** A full 2D production suite; wrong scale for a 6-shot animatic. |

### 1.6 Video / encoding

| Tool | What it does | Source | Licence | Win | Size | Unattended | Verdict |
|---|---|---|---|---|---|---|---|
| **FFmpeg** (Gyan essentials build) | Encode/decode, trim, concat, frame extraction, image format conversion, audio loudness | https://ffmpeg.org/download.html · https://www.gyan.dev/ffmpeg/builds/ | LGPL-2.1-or-later core; GPL build flags depend on build | Yes | 109.5 MB zip | **Yes** — portable unzip, no admin | **Install (portable).** Highest utility-per-MB of anything here; also removes the need for ImageMagick and sox. |
| **Shotcut** | Timeline NLE (MLT-based) | https://shotcut.org/ | **GPL-3.0** | Yes | 145.8 MB (26.9.27) | Yes | **No (yet).** No editing need before the Phase 16 trailer; OBS + FFmpeg suffice. |
| **OBS Studio** | Screen/game capture | https://obsproject.com/ | GPL-2.0 | Yes | already installed | — | Use what exists. |

### 1.7 Image

| Tool | What it does | Source | Licence | Win | Size | Unattended | Verdict |
|---|---|---|---|---|---|---|---|
| **Krita** | See §1.5 | https://krita.org/ | GPL-3.0 | Yes | 157.7 MB | Yes | **Install** (counted once). |
| **GIMP** | General raster editor | https://www.gimp.org/ | GPL-3.0-or-later | Yes | *est.* 250–350 MB | Yes | **No.** Krita covers painting and touch-up; a second 300 MB editor adds no capability. |
| **ImageMagick** | CLI image convert/composite/resize | https://imagemagick.org/ | ImageMagick License (Apache-2.0-derived) — **licence page returned no content to fetch; not re-verified** | Yes | *est.* 30–100 MB | Yes | **No.** `ffmpeg -i in.png out.jpg` and `ffmpeg -vf scale` cover the same jobs. |
| **Upscayl** | AI 2×/4× upscaling (Real-ESRGAN/NCNN models) on Vulkan | https://upscayl.org/ · https://github.com/upscayl/upscayl | **AGPL-3.0** (app only; outputs unaffected) | Yes | 248.8 MB (2.15.0) | Yes | **Optional, albedo only.** Never upscale **normal or roughness** maps — AI invents detail that breaks lighting. 2K→4K albedo is defensible; 4K PBR from 2K is not. |

### 1.8 Unity-side helpers (all free with the editor, but **package additions are an approval gate**)

| Package | What it does | Source | Licence | Size | Verdict |
|---|---|---|---|---|---|
| **Cinemachine 3.1.7** | Virtual cameras, blends, dolly/target groups — arcade reveal shot, trailer cameras | https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html | Unity Companion Licence | *est.* 10 MB | **Later.** Earns its place at the arcade-reveal phase, not now. |
| **Recorder 5.1.7** | Deterministic 4K video/image-sequence capture from the Editor | https://docs.unity3d.com/Packages/com.unity.recorder@5.1/manual/index.html | Unity Companion Licence | *est.* 5 MB | **Later.** Useful for the trailer and for repeatable 4K screenshots; OBS works today. |
| **Terrain Tools 5.0** | Sculpt/paint terrain | https://docs.unity3d.com/Packages/com.unity.terrain-tools@5.0/manual/index.html | Unity Companion Licence | *est.* 10 MB | **No.** No terrain in a 16×12 m interior; the street is a flat strip. |
| ProBuilder / Polybrush | In-editor modelling | Unity registry | Unity Companion Licence | *est.* 10 MB | **Optional.** Speeds up blockouts; competes with the Blender pipeline we already have. |
| URP volume post (ACES/bloom/grade/SSAO) | The actual "look" layer | already in URP 17.0.4 | Unity | 0 (cached) | **Already installed and wired** per `PROJECT_STATE.md`; tune, don't add. |

---

## 2. Free 4K asset sources

### 2.1 Source matrix

"Login" = account needed to download. "4K?" = whether the site actually serves 4K maps (not merely "high-res").

| Site | URL | Licence | Login | Format | 4K? | Good for | Verdict |
|---|---|---|---|---|---|---|---|
| **Poly Haven** | https://polyhaven.com/ · licence https://polyhaven.com/license | **CC0** (verified: "You can use our assets for any purpose… redistribute them… in a product you sell") | **No** | JPG/PNG/EXR maps, HDR, glTF/FBX/blend models | **Yes — 1k/2k/4k/8k** (verified via API) | HDRIs, PBR surfaces, a few props/facades | **Primary.** 869 textures, 521 models, 997 HDRIs; ships a packed **ARM** (AO/Rough/Metal) map — 1 texture instead of 3. |
| **ambientCG** | https://ambientcg.com/ · licence https://docs.ambientcg.com/license/ | **CC0** (verified: "You can include the raw files in your project, for example a video game") | **No** | JPG/PNG maps, zip, `full_json` API | **Yes** | Surfaces, decals, HDRIs, some 3D | **Primary.** Already 340 MB of it in-repo; add the **Decal** and 3D categories. |
| **Kenney** | https://kenney.nl/assets · licence https://kenney.nl/support | **CC0** (verified) | **No** | GLB/OBJ/FBX-style kits | No (untextured/low-poly) | Arcade cabinet, furniture, city buildings, UI, audio | **Primary for props/arcade.** |
| **Quaternius** | https://quaternius.com/ · licence https://quaternius.com/license.html | **QAL v1.0** — commercial use, no credit; **no reselling/redistributing raw assets**. *Pack pages themselves display a "CC0" badge → contradictory; obey the stricter QAL.* | **No** | FBX, glTF, .blend | No (stylised, small maps) | Downtown City MegaKit (315 models), interiors, furniture | **Primary for street/buildings.** Never ship the raw pack folder. |
| **Poly Pizza** | https://poly.pizza/ | Per model: **CC0 or CC-BY** (filter on site) | No | **GLB** (single file) | No | Individual props, incl. **3 CC0 arcade cabinets** | **Use for one-off props**, verify licence per model. |
| **Sketchfab (CC0 + downloadable filter)** | https://sketchfab.com/3d-models?features=downloadable&licenses=322a749bcfa841b29dff1e8a1bb74b0b | CC0 / CC-BY / **CC-BY-NC (forbidden for commercial)** | **Yes** (download) | glTF/FBX/OBJ | Sometimes | Scans, hero oddities | **Last resort only** — see the measured counts in §2.2. |
| **OpenGameArt** | https://opengameart.org/ | Per item (CC0 / CC-BY / GPL / OGA-BY) | No | varies | Rarely | 2D/3D oddities, "Arcade Machine Pack" | **Situational.** CC0 filter works; quality is uneven. |
| **itch.io free assets** | https://itch.io/game-assets/free | Per pack | Sometimes | varies | Sometimes | Character/prop packs | **Situational.** Licence text differs per upload; audit each. |
| **Freesound** | https://freesound.org/ | Per file; CC0 filter | **Yes** | WAV/FLAC/MP3 | n/a | Room tone, machines, coins | **Secondary audio.** |
| **ShareTextures** | https://www.sharetextures.com/ | "copyright-free… commercial projects too" (**CC0-equivalent**) | No | JPG/PNG | Yes | 1,700+ textures **and 3D models** | **Good backup** to ambientCG. |
| **CG Bookcase** | https://www.cgbookcase.com/ | "100% free, no restrictions" (**CC0-equivalent**) | No | JPG/PNG | Yes | PBR surfaces + **photo-scanned decals** | **Good backup**; decals are the useful part. |
| **GameTextures free tier** | https://gametextures.com/freebies/ | **Free account = unclear/non-commercial**; **commercial licence starts at the paid "Freelancer" tier ($15.20/mo)** — verified on the pricing page, where the *Hobbyist* tier is explicitly "Non-Commercial License" | **Yes** | Unity/Unreal/Substance packs | Yes (4K) | AAA-ish surfaces | **NO.** A free-tier texture you cannot legally ship is a trap. |
| **Textures.com free tier** | https://www.textures.com/ | Per-licence; account + download credits | **Yes** | JPG/TIF | Yes | Surfaces | **NO / unverified** — the site returned no content to automated fetch twice; do not rely on it until a human reads the licence page. |
| **Sonniss GDC** | https://sonniss.com/gameaudiogdc | Royalty-free, commercial, no attribution; AI training prohibited | No | WAV | n/a | Foley, machines, arcade | **Only if a specific sound is missing** — 23.5 GB per year. |

### 2.2 The hunt: arcade cabinets

Measured with the Sketchfab public search API (CC0 + downloadable), 2026-11-01:

| Query | Results | Note |
|---|---|---|
| `arcade cabinet` + CC0 + downloadable | **0** | — |
| `laundromat` + CC0 + downloadable | **0** | — |
| `washing machine` + CC0 + downloadable | **0** | — |
| `roof kit` + CC0 + downloadable | **0** | — |
| `building facade` + CC0 + downloadable | **3** | Scans of 372k / 603k / 915k triangles — unusable as game assets on 4 GB |
| `arcade cabinet`, no licence filter | 24 | Mix of CC-BY, **CC-BY-NC (cannot ship)**, Free Standard |

**Conclusion: Sketchfab is not a viable arcade/prop source for this project.** The CC0+downloadable intersection is nearly empty, and the unfiltered results are dominated by non-commercial licences.

Real candidates, in order:

| Candidate | Source | Licence | Format | Assessment |
|---|---|---|---|---|
| **Kenney Mini Arcade** (20 files, 3D, animated, variations) | https://kenney.nl/assets/mini-arcade | **CC0** (verified on page) | Kit format (GLB/OBJ/FBX family) | **Best free base.** Low-poly, correct silhouette, no login. |
| **Poly Pizza — "Arcade Machine" (J-Toastie), "Arcade Cabinet 🕹" (Chris Ross)** | https://poly.pizza/search/arcade | **CC0 per model** (verify each) | **GLB** | Good for a quick blockout; single-mesh, no UV discipline. |
| **OpenGameArt — "Arcade Machine Pack", "Mini Arcade", "Lowpoly Arcades"** | https://opengameart.org/art-search-advanced?keys=arcade&field_art_type_tid%5B%5D=10 | Per item | varies | 16 3D hits total; licences mixed. |
| **Bespoke Blender build** | Our own `ArtSource/Blender/create_appliance.py` pattern | Ours | FBX → Unity | **Recommended.** An arcade cabinet is a box with a bezel, a marquee and a control panel — four bevelled slabs. We already script exactly this shape language. |

### 2.3 Buildings / facades, walls, floors, roofs

| Need | Best free source | Licence | Notes |
|---|---|---|---|
| **Building facades / street blocks** | **Quaternius Downtown City MegaKit** — https://quaternius.com/packs/downtowncitymegakit.html | QAL v1.0 (see contradiction note) | **315 models**, modular, FBX/glTF/.blend; 7 example buildings. Free portion is 60–70% of the pack. |
| | Poly Haven `modular_urban_apartments_facade`, `modular_factory_facade` — https://polyhaven.com/models | CC0 | Verified present in the API model list; clean PBR. |
| | Kenney **City Kit (Commercial)** 50 files, **(Suburban)**, **(Industrial)**, **(Roads)** | CC0 | Verified pages; deliberately toy-like — good for background, not for a shopfront hero. |
| **Interior walls** | ambientCG surfaces (plaster, tile, concrete) + **Poly Haven** (869 textures) | CC0 | Use 2K for walls; add a **decal** layer for grime at the dado line. |
| | CG Bookcase / ShareTextures | CC0-equivalent | Backups. |
| **Floors** | ambientCG `Tiles141`, `Concrete034`, `Rubber001` (**already downloaded at 4K**) | CC0 | The one place 4K genuinely pays: a 4 m tile repeat at 4K = 1024 px/m. |
| **Roofs** | **No dedicated free 4K roof kit exists** among the CC0 sources. Best options: Quaternius Downtown City roof pieces (flat/parapet), Kenney City Kit roof caps, or model the parapet in Blender and texture with ambientCG roofing felt/metal. | — | Be blunt: "roof kit" is not a solved free category. We are a ground-floor interior — this is low priority. |

### 2.4 Laundromat-appropriate props

| Prop | Source | Licence | Note |
|---|---|---|---|
| Washer/dryer | **Already bespoke** (`Appliance.fbx`, 808 tris ×8) | Ours | Do not replace; it is better-fitted than any free asset. |
| Baskets | Poly Haven `wicker_basket_01/02` (verified in API) | CC0 | Optional realism upgrade over our 756-tri basket. |
| Furniture, shelves, chairs, tables | Kenney **Furniture Kit** (140 files) — https://kenney.nl/assets/furniture-kit | CC0 | Blockout quality; replace heroes later. |
| Interior dressing | Quaternius **Ultimate House Interior** — https://quaternius.com/packs/ultimatehomeinterior.html | QAL | Counters, shelving, seating, plants. |
| Detergent, carts, plants | Already procedural + ambientCG decals | CC0 | Fine for now. |
| Bags / laundry piles | Already bespoke (324 / 432 tris) | Ours | Nothing free beats them at this fidelity. |

### 2.5 "4K" honesty per source

| Source | Genuinely 4K? | Evidence |
|---|---|---|
| Poly Haven | **Yes** (and 8K) | API file listing shows `8k/4k/2k/1k` for `Diffuse`, `nor_gl`, `ARM`, `Rough`, `AO`, `Displacement` |
| ambientCG | **Yes** | Repo already holds 7 sets at 4096 px (§`ArtSource/Textures/README.md`) |
| ShareTextures / CG Bookcase | Mostly yes | Vendor claims; not measured |
| Kenney / Quaternius / Poly Pizza | **No** — stylised, small or no maps | Nature of the packs |
| GameTextures / Textures.com | Yes, but licence-gated | §2.1 |

---

## 3. Reality check

### 3.1 The numbers behind "4K on 4 GB"

**Texture cost** (Unity compressed, with mips):

| Map | 4K | 2K | 1K |
|---|---|---|---|
| Albedo BC1/BC7 | 8.4 / 16.8 MB | 2.1 / 4.2 MB | 0.5 / 1.0 MB |
| Normal BC5 | **16.8 MB** | 4.2 MB | 1.0 MB |
| Mask/ARM BC7 | **16.8 MB** | 4.2 MB | 1.0 MB |
| **One full PBR set** | **~42 MB** | **~10.5 MB** | **~2.6 MB** |

**Frame cost at 4K:** an HDR RGBA16F backbuffer is 3840×2160×8 B = **66 MB**; depth 33 MB; a normal URP chain (colour + depth + SSAO + bloom mips) lands around **350–500 MB**. Four 2048 shadow cascades add ~67 MB; 4096 cascades add ~268 MB and should not be used on this GPU.

**Budget on 4 GB:** Windows desktop + driver at 4K ≈ 0.7–1.0 GB. That leaves roughly **2.2–2.6 GB** for render targets, shadows and textures. At 42 MB per 4K material, that is **~50 materials total** — including every wall, floor, prop and the whole arcade. A 16×12 m room with an arcade expansion will want 30–60 distinct surfaces. **4K everywhere is not tight; it is impossible.**

At 2K (~10.5 MB/set) the same VRAM holds **200+ materials**. That is the difference between "author freely" and "count every set".

### 3.2 What the RTX 3050 Laptop can actually do

| Target | Feasible? | Conditions |
|---|---|---|
| 4K output, internal 1440p, URP, baked lighting, SSAO+bloom, 2K textures | **Yes** — 40–60 fps plausible | Baked GI, 1 reflection probe, ≤4 shadow-casting lights, no realtime GI |
| 4K native render, 2K textures, same scene | **Marginal** — 20–35 fps | Must drop SSAO or shadows; thermal throttling on a laptop |
| 4K native + 4K PBR everywhere + SSAO + volumetric-ish fog | **No** | Exceeds VRAM before the first prop |
| HDRP at 4K | **No** | 4 GB card, and HDRP is maintenance-only per Unity's 2026 pipeline strategy |

Recommended render config: **internal 2560×1440, URP STP or FSR1 → 4K output**; Texture "Max Size" 2048 global with a per-texture override to 4096 on the floor and one hero wall; BC7 for albedo/masks, BC5 for normals; `Texture Streaming` on.

### 3.3 What "blockbuster" would actually cost

| Item | Real cost | What we have |
|---|---|---|
| Unique PBR materials in a shipped AAA interior | 150–400 sets × 40 MB = **6–16 GB VRAM** | 2.2–2.6 GB available |
| Source texturing files per hero prop | 2–8 GB (PSD/Substance + 8K sources) | 58.5 GB free disk total |
| Environment art team for one room | 6–12 months, 3–8 specialists | 1 agent + owner |
| Character art | sculptors, facial riggers, mocap | faceless mannequin; 2 failed humanoid imports |
| Download volume for a "complete" library | Meshroom ~4 GB + Sonniss 23.5 GB + a scanned library 50–100 GB | 58.5 GB free — **one wrong download ends the project's disk headroom** |

Blunt version: **"T1/blockbuster at 4K" is two requests that fight each other on this hardware.** T1 scope is achievable. "4K" is achievable as an *output resolution*. 4K *asset density* is not, and buying more tools does not change that.

### 3.4 Where to compromise — the rules

1. **4K output, 1440p internal.** Upscale. Nobody counts pixels; everybody notices a 25 fps stutter.
2. **2K default, 4K only for the floor and one hero wall.** Texel density, not resolution, is what reads as "4K".
3. **Trim sheets + atlases.** One 2K sheet textures a whole prop family (machines, cabinets, shelving) at consistent density: 8 MB instead of 8 × 42 MB.
4. **Packed maps.** Poly Haven's `ARM` and ambientCG's roughness-as-greyscale cut map count by a third.
5. **Decals instead of geometry** for grime, wear, signage, contact darkening — the cheapest detail-per-byte available.
6. **Never upscale normal or roughness maps.** AI invents microstructure; lighting then lies.
7. **Bake lighting, keep ≤4 realtime shadow casters**, one 256 reflection probe.
8. **Reject anything that needs an account or a paid tier to be legal** (§6).

---

## 4. Recommended minimum set

Six tools (one already installed) and five asset packs. Total **new download ≈ 1.3 GB**.

### Tools — install in this order

| # | Tool | Size | Why it earns its place | Verify |
|---|---|---|---|---|
| 1 | **Blender 4.5.15 LTS** (already installed) | 0 new (380 MB if re-downloaded) | The only modelling/export path the project has ever validated; builds the arcade cabinet, signs and props | `blender.exe --version` → `Blender 4.5.15 LTS` |
| 2 | **FFmpeg** (Gyan essentials, portable) | 109.5 MB | Encoding, conversion, frame extraction, loudness — replaces ImageMagick *and* sox *and* a video editor for asset work | `ffmpeg -version` |
| 3 | **Audacity** | 47.5 MB | 54 voice WAVs and every future SFX need trimming/looping; directly improves perceived quality | `audacity.exe --version` or Help ▸ About |
| 4 | **Krita** | 157.7 MB | Storyboard frames + animatic stills + texture touch-up + normal-map hand edits, in one tool | launch, check version in splash |
| 5 | **Material Maker** | 104.5 MB | Procedural trim sheets, grime/neon decals and tileable PBR — the only free path to *authored* 4K materials | launch, export a 2048 map |
| 6 | **LMMS** | 35.1 MB | Original music if the licence-clean tracks stop fitting; the owner explicitly asked for composition | launch, export a 30 s WAV |

Not in the set, deliberately: Meshroom, COLMAP, ArmorPaint, Ardour, MuseScore, OpenToonz, Pencil2D, Shotcut, GIMP, ImageMagick, Upscayl, BlenderKit, Storyboarder. Reasons in §1.

### Asset packs — download in this order

| # | Pack | Size | Why | Licence |
|---|---|---|---|---|
| 1 | **Poly Haven — 3 HDRIs + 4 surface sets** (4K) | *est.* 150–250 MB | HDRIs are the single biggest lighting win available; the surfaces fill gaps ambientCG does not cover | CC0, no login |
| 2 | **ambientCG — decal + grime + 3D categories** (4K) | *est.* 150–250 MB | Decals are the cheapest "not a model" detail; we already own the surface sets | CC0, no login |
| 3 | **Kenney — Mini Arcade (20) + Furniture Kit (140)** | *est.* 5–15 MB | The only clean CC0 arcade cabinet source plus interior blockouts | CC0, no login |
| 4 | **Quaternius — Downtown City MegaKit** | *est.* 300–600 MB | 315 modular facade/street pieces for the Bali street and the arcade's exterior identity | QAL v1.0, no login |
| 5 | **Kenney — Casino Audio + Interface Sounds + Impact Sounds** | *est.* 20–40 MB | Coin, button, thud, machine — the arcade's entire SFX vocabulary, no account | CC0, no login |

Sizes marked *est.* are estimates: Kenney and Quaternius serve downloads through a JS/donation flow with no `Content-Length` visible to an automated fetch. **Do not quote these numbers to the owner as measured.**

---

## 5. Install plan

All commands are **proposals, not executed**. `winget` IDs were verified against the `microsoft/winget-pkgs` manifest tree via the GitHub API (200 = manifest exists).

### 5.1 winget (one batch, one approval)

```powershell
winget install --id Gyan.FFmpeg            -e --silent --disable-interactivity --accept-package-agreements --accept-source-agreements
winget install --id Audacity.Audacity      -e --silent --disable-interactivity --accept-package-agreements --accept-source-agreements
winget install --id KDE.Krita              -e --silent --disable-interactivity --accept-package-agreements --accept-source-agreements
winget install --id RodZill4.MaterialMaker -e --silent --disable-interactivity --accept-package-agreements --accept-source-agreements
winget install --id LMMS.LMMS              -e --silent --disable-interactivity --accept-package-agreements --accept-source-agreements
```

| Package ID | Manifest verified | Admin? | Notes |
|---|---|---|---|
| `Gyan.FFmpeg` | **Yes** (versions `9.0`, `Essentials`, `Shared`) | **Yes** (Program Files) | If you prefer no admin, unzip the portable essentials build to `Tools/ffmpeg/` instead. |
| `Audacity.Audacity` | **Yes** (`4.0.0…`, `4.0.1…`) | **Yes** (MSI, machine scope) | `msiexec /i audacity-win-4.0.1-x86_64.msi /qn /norestart` is the direct equivalent. |
| `KDE.Krita` | **Yes** (`5.3.3.0`, `5.3.4.0`, `5.3.4.1`) | **Yes** | `--scope user` may avoid UAC if the manifest supports it; not confirmed. |
| `RodZill4.MaterialMaker` | **Yes** (`1.3`, `1.6`, `1.7`) | No (per-user) | Portable zip alternative: https://github.com/RodZill4/material-maker/releases (v1.8, 104.5 MB). |
| `LMMS.LMMS` | **Yes** (`1.2.0`–`1.2.2`) | **Yes** | Latest stable is still 2020's 1.2.2. |

If the shell refuses `winget` (as it did for this research session), the fallbacks are: extract the portable ZIP/7z to `Tools/<name>/` — no admin, no registry, matching the existing `Tools/Piper` convention.

### 5.2 Direct / portable alternatives (no admin)

| Tool | Command / action | Verify |
|---|---|---|
| FFmpeg | Download https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip → expand to `Tools/ffmpeg/` | `Tools\ffmpeg\bin\ffmpeg.exe -version` |
| Material Maker | Download `material_maker_1_8_windows.zip` → expand to `Tools/MaterialMaker/` | launch `MaterialMaker.exe`, Help ▸ About |
| Blender (already installed) | invoke by full path; **do not** add to machine `PATH` | `& "C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe" --version` |
| Blender add-on | Extensions ▸ LoopTools / F2 (in-app) | Preferences ▸ Add-ons shows enabled |

### 5.3 Post-install checks (all run by a human or the lead agent)

| # | Check | Pass looks like |
|---|---|---|
| 1 | `ffmpeg -version` | banner + `--enable-gpl` build line |
| 2 | `ffmpeg -i <any>.wav -af loudnorm -f null -` | completes without error (proves the audio filter chain) |
| 3 | Audacity opens a 48 kHz WAV and exports MP3/OGG | file plays |
| 4 | Krita opens a PNG, exports PNG+JPG | colour preserved |
| 5 | Material Maker exports a 2048 basecolor+normal pair | both files non-empty |
| 6 | LMMS plays a built-in demo project | audio through the default device |
| 7 | `git status --short` after all installs | **no new tracked files** (tools must not enter the repo) |
| 8 | Record each tool + version in `PROJECT_STATE.md` §1 | table updated |

---

## 6. Risks

### 6.1 Licences that forbid shipping raw assets

| Source | Trap | Rule |
|---|---|---|
| **Quaternius (QAL v1.0)** | "You may not extract, repackage, sublicense, sell, or otherwise redistribute the Assets… regardless of how much they have been modified." The pack pages simultaneously show a **CC0** badge — a genuine contradiction on the vendor's own site. | Obey the stricter reading: **incorporate into prefabs, never ship the raw pack folder.** Keep sources in `ArtSource/`, and document the contradiction in `ArtSource/Licenses/quaternius-*.md`. |
| **Mixamo** | No standalone redistribution; Adobe account required | Bake clips onto our prefabs. |
| **Unity Asset Store free** | Standard Asset Store licence: no raw redistribution, no standalone asset folder | Prefabs only. |
| **Blendkit/BlenderKit** | Royalty Free forbids resale of the asset itself; account required | Fine for blocks; audit each item. |
| **GameTextures free tier** | Commercial licence begins at a **paid** tier; the hobbyist tier is explicitly non-commercial | **Do not use the free tier for a commercial game.** |
| **Sonniss** | Royalty-free for media production; **AI/ML training prohibited**; 23.5 GB | Download one part, not the archive. |
| **Sketchfab** | Many "free" models are **CC-BY-NC** — illegal in a sold game | Filter CC0 only; and §2.2 shows there is almost nothing there we need. |
| **CC-BY anywhere** (Kevin MacLeod, Sketchfab) | Attribution required | Every CC-BY asset needs a credits-screen line **and** an inventory row. |
| **Kenney** | CC0, but the **logo** is not | Credit as text only. |

### 6.2 Accounts and approvals

- Require an account: Sketchfab (download), Freesound, Mixamo, GameTextures, Textures.com, Blendkit, Fab/Quixel.
- Require **human approval under this project's rules**: any Unity package (Cinemachine, Recorder, ProBuilder), any `ProjectSettings/**` or `Packages/**` change, and **any software install** (§`GUARDRAILS.md`). This report is evidence for that decision, not the decision.

### 6.3 Repository bloat — measured today

| Fact | Value | Risk |
|---|---|---|
| `.git` size | **582.9 MB** | Already large; every binary commit is permanent in history |
| `Assets/` + `ArtSource/Textures/` | **339.0 MB + 339.7 MB** | The same 4K maps exist twice |
| `.gitattributes` | **absent** | **No Git LFS filter** — every PNG/JPG/FBX is a raw blob |
| Disk free | **58.5 GB** | One 23.5 GB audio bundle = 40% of headroom |

**Rules:** add LFS patterns for `*.png *.jpg *.exr *.tga *.psd *.fbx *.blend *.wav *.mp3` (a repo-config change → approval); keep 4K *sources* in `ArtSource/` and import into Unity at 2048 max size; never commit a downloaded pack folder wholesale; put portable tools under `Tools/` (883 MB on disk today, **0 tracked files**, `git status` clean — verify that stays true after each install).

### 6.4 Process risks

- **The one-file rule** — any asset entering the project must get a row in `ASSET_INVENTORY.md` (URL, author, licence, resolution, date, integration status) **before** it is committed. This report does not create any row; it only names sources.
- **Tool sprawl** — 20 free tools that nobody masters is worse than 5 that get used. The 6-tool set is a ceiling, not a starting point.
- **LMMS age** — 1.2.2 is from June 2020. If it misbehaves on Windows 11, fall back to licensed CC-BY tracks rather than debugging a DAW.
- **"4K" as a proxy for quality** — the owner's real request is "stop looking like a greybox". 4K textures do not fix flat lighting or a faceless character.

---

## 7. Verification ledger

| URL | Status | What was confirmed |
|---|---|---|
| https://www.blender.org/download/lts/ · /about/license/ | 200 | LTS page + GPL |
| https://download.blender.org/release/Blender4.5/blender-4.5.15-windows-x64.zip | 200, **380.2 MB** | Installer size |
| https://extensions.blender.org/add-ons/{looptools,f2,auto-mirror}/ | 200 | Free official extensions exist |
| https://materialmaker.org/ · https://github.com/RodZill4/material-maker | 200 | MIT (GitHub API), Unity/Unreal/Godot export |
| https://armorpaint.org/ | 200 | "minimum of 6GB of video memory is recommended" |
| https://alicevision.org/ | 200 | Meshroom, MPL-2.0, Windows |
| https://github.com/colmap/colmap | 200 | "licensed under the new BSD license" |
| https://api.github.com/repos/colmap/colmap/releases/latest | 200 | `colmap-x64-windows-cuda.zip` **395 MB** |
| https://lmms.io/download | 200 | Stable 1.2.2, dated 2020-06-25 |
| https://community.ardour.org/download | 200 | GPLv2; prebuilt binaries are paid |
| https://www.audacityteam.org/ + GitHub releases | 200 | GPL; **47.5 MB** msi (4.0.1) |
| https://musescore.org/ | 200 | GPL-3.0 |
| https://sfxr.me/ | 200 | Browser SFX generator |
| https://sfbgames.itch.io/chiptone | 200 | ChipTone download |
| https://www.sfbg.com/chiptone | **404** | Old ChipTone URL is dead — do not cite it |
| https://krita.org/ | 200 | GPL-3.0; **157.7 MB** setup (5.3.3) |
| https://wonderunit.com/storyboarder/ + GitHub | 200 | 2.1.0, **119.5 MB**; repo last activity 2024-03 |
| https://www.pencil2d.org/ | 200 | GPL-2.0, **68.9 MB** |
| https://opentoonz.github.io/ | 200 | **57.0 MB** installer |
| https://ffmpeg.org/download.html · https://www.gyan.dev/ffmpeg/builds/ | 200 | **109.5 MB** essentials zip |
| https://shotcut.org/ | 200 | GPL-3.0, **145.8 MB** |
| https://www.gimp.org/ | 200 | GPL-3.0-or-later |
| https://imagemagick.org/ | 200 | Site up; **licence page returned no content — unverified** |
| https://upscayl.org/ + GitHub | 200 | AGPL-3.0, **248.8 MB** |
| https://polyhaven.com/license · api.polyhaven.com | 200 | CC0 verbatim; **869 textures / 521 models / 997 HDRIs**; `1k–8k`; `ARM` map |
| https://docs.ambientcg.com/license/ | 200 | CC0; "include the raw files in your project, for example a video game" |
| https://kenney.nl/assets/mini-arcade | 200 | 20 files, **CC0** |
| https://kenney.nl/assets/furniture-kit | 200 | 140 files, CC0 |
| https://kenney.nl/assets/city-kit-commercial | 200 | 50 files, CC0 |
| https://kenney.nl/support | 200 | "all game assets… are public domain licensed (CC0)" |
| https://quaternius.com/license.html | 200 | QAL v1.0, no redistribution |
| https://quaternius.com/packs/downtowncitymegakit.html | 200 | 315 models, FBX/glTF/.blend, page shows **CC0** badge |
| https://poly.pizza/search/arcade | 200 | 19 results; 3 CC0 arcade cabinets; GLB |
| https://sketchfab.com/3d-models?features=downloadable&licenses=322a… | 200 | Filter page live; **API counts: 0 CC0 arcade cabinets** |
| https://opengameart.org/ (plain fetch **403**, browser-UA fetch 200) | 200 w/ UA | 16 3D "arcade" results |
| https://freesound.org/ | **403** | **Unverified** — account-gated, assume login required |
| https://gametextures.com/freebies/ + /membership/pricing/ | 200 | Freebies need account; **commercial from $15.20/mo** |
| https://www.textures.com/ | **no content returned (twice)** | **Unverified** |
| https://sonniss.com/gameaudiogdc | 200 | Royalty-free, commercial, no attribution; **23.5 GB** GDC 2024 |
| https://www.sharetextures.com/ | 200 | 1,700+ textures + models, commercial-use |
| https://www.cgbookcase.com/ | 200 | "100% free, no restrictions" |
| https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/… | 200 | v3.1.7, separate install |
| https://docs.unity3d.com/Packages/com.unity.recorder@5.1/… | 200 | v5.1.7, separate install |
| https://docs.unity3d.com/Packages/com.unity.terrain-tools@5.0/… | 200 | Terrain Tools docs |
| api.github.com/repos/microsoft/winget-pkgs/contents/… | 200/404 | IDs in §5.1 verified; `MuseScore.MuseScore`, `Pencil2D.Pencil2D`, `OpenToonz.OpenToonz`, `WonderUnit.Storyboarder`, `AliceVision.Meshroom`, `COLMAP.COLMAP` **do not exist** |
| https://meshroom-manual.readthedocs.io/en/latest/install/requirements.html | **404** | **Meshroom size and CUDA requirement unverified** |

Not verified at all, and not claimed: installed footprints, first-run behaviour, silent-install success for any specific installer, any in-engine visual result, any asset's suitability once imported.

---

## 8. Recommended next actions (max 8)

1. **Approve one batch install** of FFmpeg, Audacity, Krita, Material Maker and LMMS (§5.1) — 456 MB, one UAC pass, then run the §5.3 checks.
2. **Fix the licence contradiction on record**: add a note to `ASSET_PIPELINE.md` that Quaternius pack pages say CC0 while QAL v1.0 forbids redistribution, and that the stricter rule governs.
3. **Measure before authoring**: build a throwaway 4K-output scene with the current room at 2048 textures, `STP/FSR` upscaling, and record GPU frame time and VRAM on the 3050. That number decides every later texture argument.
4. **Download pack #1 and #2 only** (Poly Haven HDRIs + ambientCG decals), record each in `ASSET_INVENTORY.md` with URL/author/licence/date, then wire the HDRI into `RenderSettings` and re-capture the room.
5. **Build the arcade cabinet in Blender** from Kenney Mini Arcade proportions using the `create_appliance.py` pattern — 300–900 tris, one trim sheet, no new packages.
6. **Adopt the texture policy** in §3.4 as a written standard in `ASSET_PIPELINE.md` (2K default, 4K floor/hero only, BC7/BC5, ARM packing, no AI-upscaled normals).
7. **Propose LFS patterns and a `Tools/` exclusion** as a single reviewed repo-config change (approval gate) before the next large download.
8. **Ask the owner to settle the art-scope wording** — `MASTER_CONTEXT.md` says "small stylised, not photoreal"; "T1/blockbuster" is a different target. Record the answer as a decision file so future agents stop re-litigating it.

---

*End of report. Nothing was installed, downloaded, or imported. No repository file other than this one was created or modified.*
