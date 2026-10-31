# Laundry Legends - Free Asset Pipeline

**Status:** Approved direction, research complete; first prototype pending `LL-P3-011`
**Owner decision:** 2026-10-31
**Scope:** Free (zero-cost) environment, prop, and NPC assets. No paid assets, no AI-generated assets, no new Unity packages without separate approval.

## 1. Decision

Use a **hybrid pipeline**:

1. **Blender LTS** for bespoke low-poly props, laundry items, machine details, signage, and simple NPC bodies.
2. **CC0 packs** (Kenney, Quaternius, Poly Haven, Poly Pizza, 100 Avatars) for bulk props, materials, and character bases.
3. **Mixamo** for free humanoid idle/walk/carry animations when its terms are accepted at download time.
4. **Sketchfab CC0/CC-BY filter** only for a specific missing asset, with the author, URL, and license recorded before download.

The intent is to polish Phases 1-3 and create a small NPC set **before Phase 4 gameplay**, while keeping every asset free and license-auditable.

## 2. Tooling

| Tool | Recommended version | Source | License | Notes |
|---|---|---|---|---|
| Blender | **4.5.15 LTS** (supported to July 2027) | https://www.blender.org/download/lts/ | GPL v2+ for the software; artwork created is owned by us | Conservative choice for Unity 6 FBX compatibility. Installed 2026-10-31 at `C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15`. SHA-256 verified against the official checksum. |
| Blender alternative | 5.2.3 LTS (supported to July 2028) | https://www.blender.org/download/ | Same | Newer, but the FBX/Unity path is less battle-tested for this project. Use only if 4.5 fails. |
| Unity | 6000.0.64f1 (already installed) | Unity Hub | Existing | No new packages. |

Blender installation is approved by the project owner and was completed on 2026-10-31 using the official portable Windows x64 ZIP. The GPL applies to Blender itself, not to `.blend` files or exported meshes; those remain ours.

### Current installation

- Version: Blender 4.5.15 LTS, build 2026-10-22
- Path: `C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe`
- Source: `https://download.blender.org/release/Blender4.5/blender-4.5.15-windows-x64.zip`
- SHA-256: `52dd02fd74877fa08e74b67d7854cf32468a232c02aaa19b6844c91285574f8d` (verified)
- Portable install: no machine-wide PATH change; invoke by full path.

## 3. Source and license matrix

| Source | License | Commercial use | Attribution | Redistribution / resale | Account | Use in this project |
|---|---|---|---|---|---|---|
| Blender / our own meshes | Our ownership | Yes | Not required | We own the files | No | Primary bespoke props and NPC bodies |
| Kenney (https://kenney.nl) | CC0 | Yes | Not required | Allowed | No | Props, machines, UI, audio, blocky characters |
| Quaternius (https://quaternius.com) | Quaternius Asset License v1.0 (2026-08-28) | Yes | Not required | **No reselling/redistributing raw assets** | No | Rigged characters, universal animations, stylized props |
| Poly Haven (https://polyhaven.com) | CC0 | Yes | Not required | Allowed | No | PBR textures, HDRIs, some models |
| Poly Pizza (https://poly.pizza) | Per model, CC0 or CC-BY | Yes when CC0/CC-BY | CC-BY requires credit | Check per model | No | Low-poly props and 100 Avatars mirrors |
| 100 Avatars / Polygonal Mind | CC0 | Yes | Not required (credit appreciated) | Allowed | No | Rigged humanoid NPC bases, FBX/VRM |
| Mixamo (https://www.mixamo.com) | Adobe free terms | Yes | Not required | **No standalone redistribution**; bake into the game | Adobe ID required | Humanoid animations and test characters |
| Sketchfab (https://sketchfab.com) | Per model; CC0/CC-BY or paid | Only if the model says so | Per license | Per license | Usually yes | Last resort for one specific missing asset |
| Unity Asset Store free | Standard Asset Store license per asset | Usually yes | Per asset | No raw redistribution | Unity ID | Not primary; only if a specific free asset is approved |
| AI 3D generators | Varies / often paid | Unclear | Unclear | Unclear | Varies | **Not approved** for this project |

**Rules**

- Record source URL, author, license, download date, and any attribution text in `ASSET_INVENTORY.md` before committing an asset.
- Keep the original license file next to the source file under `ArtSource/Licenses/`.
- Never ship a folder of raw third-party assets that a user could extract as a standalone pack. Bake Mixamo/Quaternius assets into prefabs and keep source files under `ArtSource/` (outside the build).
- CC-BY assets require a credit line in the eventual credits screen.
- Do not rely on a free library staying free; download and archive the exact version we approve.

## 4. Art direction and technical standards

| Item | Standard |
|---|---|
| Style | Stylized low-poly, flat or lightly metallic materials, readable silhouettes |
| Scale | 1 Unity unit = 1 meter; Blender scene unit = meters |
| Orientation | Author Y-up/-Z-forward in Unity; export from Blender with the FBX exporter's Unity-friendly axes |
| Pivot | Base-center for floor props; hold point at the object's center for carried items |
| Poly budget | Props 300-1,500 tris; hero laundry item up to 2,500; NPC 5,000-12,000 |
| Textures | 512-1,024 px, power of two; one material per asset where practical |
| Colliders | Authored manually in Unity; do not rely on generated mesh colliders for carried items |
| Rig | Humanoid only for NPCs; Mixamo-compatible bone names where possible |
| Animation | Idle, walk, carry for NPCs; no gameplay behavior in the asset phase |
| Naming | `SM_` mesh, `MAT_` material, `T_` texture, `PF_` prefab only if a new prefab is created; existing prefab names stay unchanged |

### Blender to Unity export

- Apply transforms before export (scale 1, rotation 0).
- Export FBX with `-Z Forward`, `Y Up`, apply scale `1.0`, no leaf bones unless needed.
- Disable cameras/lights in the export.
- In Unity: scale factor `1`, convert units on, bake axis conversion off for FBX, generate colliders off, read/write off, mesh compression off for small props.
- Keep the `.blend` source in `ArtSource/Blender/` and the exported FBX in `ArtSource/Exports/` or the final `Assets/LaundryLegends/Art/` folder.

## 5. Folder layout

```text
ArtSource/
  Blender/            editable .blend files
  Exports/            FBX/glTF exports before Unity import
  Licenses/           downloaded license text and source notes
  References/         style boards, public-domain references
Assets/LaundryLegends/Art/
  Models/             imported meshes
  Materials/          Unity materials
  Textures/           imported textures
  Animations/         humanoid clips
```

`ArtSource/` is source material, not a Unity runtime folder; it stays out of builds.

## 6. NPC plan

Start with three archetypes, all reusing one rig and animation set:

| Archetype | Base | Role | Phase |
|---|---|---|---|
| Customer | Quaternius or 100 Avatars CC0 humanoid | Drops off and collects orders | Phase 5+ behavior, asset now |
| Attendant | Same rig, different outfit/color | Behind the counter | Asset now, behavior later |
| Regular | Same rig, third outfit | Waiting area | Asset now, behavior later |

- Use Mixamo idle/walk/carry clips or Quaternius' universal animation library.
- Keep the NPC as a prefab with a capsule collider, animator, and placeholder dialogue anchor.
- Do not add customer AI, queueing, or payment in the asset phase.
- Faces/hair stay simple; no facial animation requirement for Phase 3.

## 7. Prioritized asset backlog

### Phase 1-3 polish (approved before Phase 4)

1. **Custom laundry basket** (first prototype): replace the primitive cube basket; keep `laundry.basket`, collider, pickup, carry, and washer contracts.
2. Custom loose-laundry bundle and bag: soft shapes, readable color, same contracts.
3. Washer/dryer fronts: door ring, control panel, coin slot; keep the existing machine colliders and `MachineLoadHook` on `Machine_1`.
4. Floor/wall materials: tile floor, painted wall, metal counter, glass; CC0 PBR textures from Poly Haven/ambientCG.
5. Signage: `WASH DRY FOLD`, `DROP OFF`, `STAFF ONLY`, hours/open sign; Blender text or simple meshes.
6. Props: detergent bottles, carts, folding table details, waiting chairs, plants.
7. Lighting/color pass: warmer interior, cooler exterior, readable wayfinding.

### NPC/character pass

8. Three NPC archetypes, one rig, idle/walk/carry clips.
9. Player hands/held-item presentation pass (optional; first-person arms are not required for the vertical slice).

## 8. First prototype: custom basket

**Why:** highest visibility, simplest shape, and the existing tests already cover pickup, carry, drop, and machine eligibility. It proves the whole Blender-to-Unity path with low risk.

**Deliverable**

- `ArtSource/Blender/LaundryBasket.blend` with a low-poly open basket (under 2,500 tris).
- Exported FBX and one `MAT_Basket` material.
- `Basket.prefab` updated in place: same name, `LaundryItem` template `laundry.basket`, `BoxCollider`, `Rigidbody`, `LaundryItemInteractable`, pickup/carry behavior.
- Screenshot/capture and license row.
- Rollback: restore the previous `Basket.prefab` from Git if import or contracts fail.

**Estimate:** 2-4 hours including export, import, collider tuning, and tests.
**Acceptance:** EditMode/PlayMode suites pass; pickup, drop, and washer rejection for baskets still hold; asset validation passes.

## 9. Risks and mitigations

| Risk | Mitigation |
|---|---|
| License drift or library shutdown | Download and archive the exact paid-free version; record license text and URL |
| Mixamo region/account restrictions | Verify at download; fall back to Quaternius animations |
| Quaternius no-resale clause | Never ship raw source models or an extractable asset folder |
| CC-BY attribution missed | Record credit text in `ASSET_INVENTORY.md` and the credits screen |
| Blender FBX scale/axis errors | Apply transforms; test one asset before batch import |
| Rig/retarget mismatch | Use one humanoid rig and Mixamo-compatible bone names |
| Poly count/performance | Budgets above; check the Game view and profiler after each batch |
| Installer or package changes | Blender install approved; any Unity package or ProjectSettings change still needs approval |
| AI asset licensing | Not approved; do not use generated assets |
| Art scope creep | First asset only; review the pipeline before creating a large set |

## 10. Approval gates

- Blender installation: **approved** 2026-10-31.
- Downloading CC0/free assets: allowed after the license row is recorded; no paid or account-restricted asset without approval.
- New Unity packages or project settings: **not approved**.
- AI-generated assets: **not approved**.
- Third-party asset redistribution as a standalone pack: **not allowed** by the source licenses.

## 11. Sources checked

- Blender download and LTS: https://www.blender.org/download/ and https://www.blender.org/download/lts/
- Blender license: https://www.blender.org/about/license/
- Kenney support/license: https://kenney.nl/support
- Quaternius Asset License v1.0: https://quaternius.com/license.html
- Poly Haven license: https://polyhaven.com/license
- Poly Pizza: https://poly.pizza/
- 100 Avatars / Polygonal Mind: https://github.com/PolygonalMind/100Avatars and https://poly.pizza/bundle/100-Avatars-R2-1VwH0yHhlS
- Mixamo FAQ: https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html (the page returned 403 to automated fetch; terms summarized from Adobe Community citations and must be rechecked in the browser at download time)
- Sketchfab licenses: https://sketchfab.com/licenses (per-model terms; CC0/CC-BY downloads are separate from the paid license agreement)
