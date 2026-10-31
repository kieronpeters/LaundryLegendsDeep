# Paid Asset Budget Study — Laundry Legends

**Date checked:** 2026-11-01. **Type:** research only. Nothing bought, downloaded, or installed.
**Scope:** one-off paid assets for a stylised low-poly Unity 6 Bali laundromat + secret arcade.
**Prices:** USD, excluding VAT/regional tax, from Unity Asset Store listings, Synty Store, itch.io, and third-party price trackers. Sales are live as of the date above and will end.
**Repo impact:** this file is the only change. Uncommitted work elsewhere in the tree belongs to the owner and was not touched.

## 0. Two corrections to the brief (read first)

1. **The brief says URP; the pipeline is mid-migration — check before buying.** Committed truth (`PROJECT_STATE.md` §1) is Built-In Render Pipeline: no SRP asset, Standard-shader materials. But the **uncommitted working tree** already carries `com.unity.render-pipelines.universal 17.0.4` in `Packages/manifest.json` plus a one-shot `Assets/LaundryLegends/Editor/UrpMigrationTool.cs` (BiRP → URP converter). So URP-only assets are a *conditional* buy: fine if the migration lands, a pipeline-rewrite bill if it is abandoned or reverted. Do not buy URP-only art until the migration is committed and validated.
2. **Editor version is the harder blocker.** `ProjectSettings/ProjectVersion.txt` is `6000.0.64f1`. Several 2026 laundry/SE-Asian kits are built for **6000.3.x / URP 17.3.0** and will not import cleanly here regardless of pipeline.
3. **The visual target is deliberately low-poly, not realistic.** Props are 300–1,500 tris, machines 808 tris, NPC budget 5,000–12,000. "Realistic-ish" character packs are the wrong style at this size of game, and their 2K–4K texture sets fight the flat-shaded look. Stylised low-poly is the correct purchase class.

Also verified in `ASSET_INVENTORY.md`: animation is already solved for free — 45 CC0 Quaternius clips on a working humanoid avatar (`ANIM-NPC-001`). The real visible gap is **not clips, it is the faceless mannequin body** (`MANNEQUIN-NPC-001`, "no clothing").

## 1. Characters

| Asset | Vendor | Price | Licence | Pipeline / Unity | Size / polys | Why it helps | What it will not fix |
|---|---|---|---|---|---|---|---|
| **Low Poly Rigged Character Pack** (40+ rigged casual humans, colour-swap materials) | Floreswa, Asset Store | **$4.99** (all-time low, stable) | Single Entity, standard EULA | Built-in friendly, no shader deps; original 6000.0.32 | 2.1 MB; exact tri count **unverified** (listing: "mobile-friendly low poly") | Directly replaces the faceless mannequin; humanoid-compatible so existing Quaternius clips retarget; no textures to restyle | No facial rig, no clothing layer system, no animations of its own |
| **City Characters — Modular Animated Pack** (300 characters, 1,100 clothing pieces, 150 anims) | ithappy, Asset Store | **$259.00** (was $129.50 on 2026-08-26; low $20.72 in 2026-03) | Single Entity; 5 seats per quantity on ithappy.com | Ships Built-in + URP + HDRP scenes; humanoid; Mixamo compatible | ~6,000 tris/char (fits our 5–12k budget) | The "one purchase, never touch NPC art again" option: body types, ages, outfits, shop/attendant variants | Not worth it at list price for three NPC archetypes; bloat; needs art-direction pass to stop looking like a stock city pack |
| Simple City Characters (low-poly cartoon) | Graham Roberts, Asset Store | $14.99 | Single Entity | **URP only — not Built-in compatible** | 1.4 MB class | — | **Incompatible.** Excluded. |
| Realistic / AAA humanoid packs (TurboSquid, CGTrader, Fab) | various | $40–$200 | varies, often per-seat | Usually PBR/UE-first, 20k–80k tris | — | — | **Style mismatch.** Wrong silhouette, wrong texel density, hours of retopo |

## 2. Animations

| Asset | Vendor | Price | Licence | Pipeline / Unity | Size | Why it helps | What it will not fix |
|---|---|---|---|---|---|---|---|
| **Movement Animset Pro** (180+ mocap: walk, run, crouch, jump, sit, pickup, button-press) | Kubold, Asset Store | **$65.00** list; **$32.50** at 50% off; low $19.50 (2026-04-15) | Single Entity; may be used in any engine, but **not redistributable** as an asset pack | Humanoid/Mecanim; original 2020.1.0; pipeline-agnostic | 13.2 MB | Adds the "acting" our CC0 locomotion lacks: pick up basket, load drum, fold at table, sit and wait | Does not add a body mesh, does not include a controller, does not fix our socket/carry offsets on its own |
| **Human Mega Animations Pack** (1,200+ humanoid clips: basic, crafting, throwing, dance, melee) | Kevin Iglesias, Asset Store | **$130.00** list; **$65.00** at 50% off; low $27.00 | Single Entity | Listing verifies Unity 6000.0.59f2, Built-in + URP + HDRP, humanoid | size **unverified** | "Crafting" and "throwing" clips map well to folding, sorting, and stock handling | Mostly fantasy-adjacent (archer, mage, soldier) — ~70% of the pack is wasted on a laundromat |
| Human Basic Motions only | Kevin Iglesias | $18.00 | Single Entity | Humanoid | small | Cheapest slice if only locomotion + jump is wanted | Largely duplicates the free Quaternius set already imported |
| Mixamo | Adobe | Free | Adobe terms, account required | Humanoid | — | 2,600 clips, zero cost | Needs an Adobe ID and re-download discipline; no batch licence archive |

## 3. Environment, props, arcade

| Asset | Vendor | Price | Licence | Pipeline / Unity | Size / polys | Why it helps | What it will not fix |
|---|---|---|---|---|---|---|---|
| **Retro Arcade Machines Pack** (5 meshes, 11 branded variants, separate CRT screens, PSD branding) | Alexander Ivlev, Asset Store | **$8.99** (stable) | Single Entity | Built-in friendly (Standard-style albedo/metal/normal/emission), original 5.4.3 | 106.4 MB; **500–1,600 tris/machine** | The arcade is the game's identity hook and this matches our tri budget exactly; CRT as its own mesh means animated screen content is cheap | Static only — no coin door animation, no screen shader, no cabinet interaction |
| Modern Laundromat — Modular Interior & Laundry Props (90 models, LODs, C# hinge/drum examples) | BytesWeavers, Asset Store | $39.99 | Single Entity | **URP 17.3.0 only, editor 6000.3.21f1** — built-in and HDRP not supplied | 136.9 MB; LOD0 inventory 524,073 tris (12–57,648 per model) | Genuinely good laundry kit, moving parts, machine-cycle samples | **Blocked on the editor version** (see §0); URP need may be covered by the in-progress migration. Also 2–3× our texel/tri density |
| Modular Laundry Interior | Asset Store | price **unverified** | Single Entity | **URP only, 6000.3.9** | 110.7 MB | — | Same incompatibility. Excluded. |
| Realistic Laundromat Asset Package | Aligned Games, Asset Store | **$15.00** | Single Entity | pipeline **unverified** | — | Cheapest dedicated laundromat kit | Realistic prop style clashes with the 808-tri appliances we already built |
| Bandung Heritage Twin Building (Indonesian colonial shophouse) | Nusantara ArtWorld, Asset Store | price **unverified** (sibling "Bamboo House modular" is $18) | Single Entity | **URP only, not Built-in** | 2048² textures, low tri counts | The one genuinely Balinese/SE-Asian facade found | Incompatible pipeline; single building, not a street kit. Brief-reference only |
| POLYGON — City Pack | Synty | **$10.00** (Asset Store, 50% off $20); $19.99 on Synty Store | **Restricted Single Entity**; 5 seats per quantity at syntystore | Built-in + URP variants shipped | 121.8 MB | Cheapest way to get a whole street block and crowd | Synty's smooth, denser polygon style does not match our flat kitbash; also urban-Western |

## 4. Textures / PBR

| Option | Price | Verdict |
|---|---|---|
| ambientCG (already in use: `MAT-ENV-001`) | Free, CC0 | **Keep.** Our 1K tile/plaster/wood path works and is license-clean |
| Quixel Megascans on Fab | Free starter set (1,500+ assets); individual scans from $0.99, kits $4.99, packs from $24.99, bundles $79.99–$89.99 | Photoreal scans are the wrong texel density for a 300–1,500-tri kit. Free tier only, if ever |
| Paid PBR texture bundles (Asset Store, $20–$60) | $20–$60 | **Wasted money at this stage.** We need maybe 6 more tiling materials; ambientCG covers them for $0 |

## 5. Audio

| Asset | Vendor | Price | Licence | Why it helps | What it will not fix |
|---|---|---|---|---|---|
| **Household Ambience** (washing machine, room tones, hallway, kitchen drones) | 344 SFX, Asset Store | **$19.99** | Single Entity | The single most on-theme paid SFX buy: drum hum, spin, drain, room tone for the core loop | No arcade bed, no street/foot-traffic layer, no one-shots for coin/button |
| Washing Machine Sound Effect Pack (37 files) | Debsound, Asset Store | tracker reports **$0.00** — treat as free/unverified | Single Entity | Narrow, cheap | Only one appliance; no room tone |
| Retro Game SFX Pack / Retro Style SFX Pack / Retro SFX | ME Magic / SR Sound / Gravity Sound | **$9.99 / $39.99 / $29.99** | Single Entity | Coin, button, jingle, UI blips for cabinets | 8-bit chiptune clashes with a diegetic arcade room unless deliberately stylised |
| Arcade Blast (228 8-bit SFX) / 8-Bit Arcade SFX (192) | Clutch Assets / yetiancai, itch.io | **$3.49** (sale) / **$2.59** (sale) | Royalty-free, no resale of files | Cheapest credible arcade SFX | Both flagged **AI-assisted** on itch.io — conflicts with our "AI-generated assets not approved" policy |
| Music (gamelan/Balinese) | Kevin MacLeod (in use), AudioJungle | already CC BY on hand | — | — | A paid gamelan/lo-fi track is a $15–$40 want, not a need |

## 6. UI

| Asset | Vendor | Price | Pipeline | Verdict |
|---|---|---|---|---|
| **Modern UI Pack** | Michsky, Asset Store | **$39.99** | Built-in + URP + HDRP | Reasonable if the HUD/menu work is blocking; saves component-wiring time, not art direction |
| GUI Pro — Casual Game (+PSD) | LAYERLAB | **$39.99** | Built-in compatible | Cleanest visual match for a cosy sim; heavier than we need for a prompt + ticket + shop |
| Heat — Complete Modern UI | Michsky | $69.98 | Built-in compatible | Overbuilt for one game |
| Modular Game UI Kit / Cute Kawaii GUI | ricimi | $99.99 / $49.99 | Built-in compatible | Too expensive, too themed |
| 2D Mobile Game UI Kit | 300Mind | **Free** | — | Free option worth trying before paying |

## 7. Recommended baskets

### Tier A — under $50 total
| # | Item | Price |
|---|---|---|
| 1 | Low Poly Rigged Character Pack | $4.99 |
| 2 | Retro Arcade Machines Pack | $8.99 |
| 3 | Household Ambience | $19.99 |
| 4 | Arcade Blast SFX *only if* the AI-assist policy is cleared; else free Chequered Ink arcade pack | $3.49 |
| | **Total** | **≈ $37.46** |

Buys the two things the player actually looks at (a human body, an arcade cabinet) plus the sound of the core loop. Everything else waits.

### Tier B — $50–150
Tier A, **plus** Kubold Movement Animset Pro at $32.50 on sale (or $65 full):
**≈ $69.96 on sale / $102.46 at list.** This is the "the NPCs now act like people" tier — basket pickup, drum load, sitting, waiting.

### Tier C — $150–400
Tier B, **plus** Human Mega Animations Pack at $65 (half-price; $130 list) and Modern UI Pack $39.99:
**≈ $174.95 on sale / $272.44 at list.**

Deliberately **not** in Tier C: City Characters at $259. It is the right pack at the wrong price; three archetypes do not justify 300 characters, and its sale price has hit $20.72. If it appears at ≤$65, buy it and drop everything else.

## 8. Bad purchases for this project

- **Modern Laundromat ($39.99) and Modular Laundry Interior** — URP-only *and* built for editor 6000.3.x while we are on 6000.0.64f1. The URP part may be solved by the in-progress migration; the **editor-version** part is not, and would mean an editor upgrade plus re-validating every scene and PlayMode test. This is the single most expensive "cheap" mistake on the list.
- **Bandung Heritage Twin Building / any URP-only SE-Asian kit** — same trap, and it is one building, not a street.
- **Simple City Characters ($14.99)** — URP-only, listed "Not compatible" for Built-in.
- **Realistic humanoid packs and photoreal Megascans** — style and texel-density mismatch against 300–1,500-tri props.
- **Synty All-Access Pass ($30/mo)** — a subscription, not the one-off the owner asked about, and Synty's smooth polygon look is the wrong neighbour for our flat kitbash.
- **AI-assisted itch packs** (Daily Props "Corner Laundromat" $6.50, Heavy Cycle colourways $7.95) — cheap and tempting, but `ASSET_PIPELINE.md` §10 rules out AI-generated assets, and their licences forbid shipping source files.
- **Human Basic Motions ($18) or Mixamo re-downloads** — duplicates the 45 CC0 clips already imported and verified.
- **Paid texture bundles** — ambientCG already covers the requirement at $0.

### Licence traps to respect
- Asset Store "Single Entity" = one person/legal entity. A second collaborator needs a second seat; ithappy and Synty Store sell "5 seats per quantity", so quantity 1 is fine for the owner alone and wrong for a team.
- Synty's listing says **"Restricted Single Entity"** — read that clause before planning any future contractor.
- No paid pack's EULA permits committing raw FBX/textures to a shared repo. `ASSET_INVENTORY.md` §5 already says so. Keep paid source under a git-ignored `AssetSource-Paid/` and commit only prefabs/scene references, exactly as we do for Mixamo.
- Kubold's FAQ explicitly forbids re-selling his animations inside an asset pack — fine for our game, fatal if this project ever ships a "Laundry Legends toolkit".

## 9. Answers

**Three best value purchases overall**
1. **Low Poly Rigged Character Pack — $4.99.** Removes the faceless mannequin, retargets to the animation rig we already have.
2. **Retro Arcade Machines Pack — $8.99.** The arcade is the reason the game is remembered; 500–1,600 tris matches the budget and the CRT is a separate mesh.
3. **Household Ambience — $19.99.** Gives the core laundry loop its sound for less than a takeaway.

**Do not buy:** any URP-only environment kit, anything realistic, any subscription, and City Characters at $259.

**Approval required before any purchase:** cost, account access, and `ASSET_PIPELINE.md` §10 all make this an owner gate. No asset may be marked imported until it has an inventory row, a licence file, and a PlayMode pass.
