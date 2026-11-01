# Phase 8 — playtest blockers handoff (2026-11-01)

Written at the end of a long session so a fresh session can start fixing immediately.

## Repository state

- Branch `phase-8`, merged to `main`; both at **`bfafdd5`**, pushed to `github.com/kieronpeters/LaundryLegendsDeep`.
- Tag `phase-8` (annotated) at the same commit. Default branch is `main`.
- Working tree clean. `gh` CLI installed and authenticated as `kieronpeters`.
- Tests at that commit: **EditMode 145/145**, **PlayMode 53 passed / 0 failed / 1 skipped**.
- Build: `Build\LaundryLegends.exe`, 196 MB, scenes `Bootstrap → MainMenu → Laundromat`.
- Backup bundle: `Desktop\LaundryLegends-backup-20261101-1837.bundle` (582 MB, pre-history-rewrite).

## Owner playtest verdict — NOT passed

The owner could not complete the Phase 8 loop. Reported blockers, with what has been verified so far.

### 1. HUD / interaction prompt missing
- `InteractionPromptView` has **0 references** in `Laundromat.unity`. It normally lives on `Player.prefab`, so verify the component exists there and is enabled, and that `PlayerInteractor` has its `_camera` and `_interactKey` wired.
- Earlier captures (19:24 build) did show the ORDER TICKET panel, so the HUD existed at some point. Check `OrderTicketView` too (1 scene ref).

### 2. Only one order, then nothing
- `CustomerSpawner` in scene: `_respawnDelay: 4`, `_autoStart: 1`, `_price: 40`, `_customerPrefab` assigned.
- Player log from the final session shows a complete machine cycle:
  `[Machine] Loaded loose laundry → Wash started → Unloaded → Loaded → Dry started → Unloaded`
  …then nothing: no collect, no payment, no respawn.
- So the failure is after the dry/wrap stage — investigate `CustomerAgent` state machine (`Arrive → Waiting → … → Collect → Leave`), `OrderLedger` state transitions, and whether `CustomerSpawner.ActiveCustomer` is ever cleared.

### 3. Cannot reach $200
- Each order is `$40`; the arcade requires `$200` → five completed orders.
- This is unreachable while blocker 2 exists. Verify the wallet actually credits on payment (`EconomyService`), not just that the order closes.

### 4. Back room purchase not reachable
- `HiddenRoomPurchasePoint` is placed on the concealed shelf, cost `$200`, requires `_spawner.Economy`.
- Confirm the shelf has a collider and sits within `PlayerInteractor` range (3 m), and that `_room`/`_spawner` are assigned in the scene.

### 5. Appliances do not read as washers/dryers
- The appliance is an **808-triangle prototype**; the high-detail generator was abandoned mid-refactor (`ArtSource/Blender/create_appliance_hd.py`, `ApplianceHD.blend` exist but the script does not run).
- Needs either a working rebuild or a better authored primitive; keep the existing materials.

### 6. Character fidelity
- Missing **eyes, teeth, tongue, inner mouth**: MakeHuman has the assets (`Tools/MakeHuman/makehuman/data/{eyes,teeth,tongue}`: 34 and 21 files) but `ArtSource/Blender/makehuman_npc_plugin.py` only exports body + clothing proxies.
- Customer clothing was changed in `ArtSource/Blender/makehuman_characters.json` from `female_casualsuit02` (revealing) to **`female_casualsuit01`** (modest). This change is **not yet applied to any asset** — the re-export has not run.
- **Re-export is blocked**: `makehuman_npc_run.ps1` depends on `ArtSource\Downloads\MakeHuman\userhome`, which was deleted during disk cleanup. It must be recreated (plugins folder + `settings.ini` with `activeUserPlugins: ["npc_export"]`) or the script repointed to another userhome.

## Hard-won constraints — read before changing NPC movement

Three separate attempts to stop NPCs clipping furniture each broke the customer loop and were caught by PlayMode tests:

1. Adding `CharacterController` to NPC prefabs → customers deadlocked in the doorway.
2. Penetration push-out inside `Update` → blocked the loop.
3. Sphere-cast slide in `MoveTowards` → stalled the loop.

What ships now: overlap correction in **`LateUpdate`**, only for overlaps deeper than 70% of body radius, capped at 5 cm/frame (`NpcLocomotion.ResolveOverlaps`). Tests pass with it; do not move it back into `Update`.

Related: `CustomerSpawner.SpawnNow` must not set `isTrigger` on a `CharacterController` (throws "A Character Controller cannot be a trigger").

## Suggested order of work

1. Restore the HUD/prompt so interaction is visible and reliable.
2. Fix the order lifecycle so a customer returns, pays, leaves and the next spawns — gate on the PlayMode customer tests plus a new test asserting two consecutive paid orders.
3. Confirm $200 is reachable end-to-end (five paid orders) with an automated wallet assertion.
4. Verify the shelf purchase and back-room entry in a build.
5. Only then: appliance models and character fidelity (eyes/teeth/clothing re-export).

## Useful commands

```powershell
# tests
& 'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe' -batchmode -projectPath '<repo>' `
  -runTests -testPlatform EditMode -testResults '<repo>\TestResults\edit.xml' -logFile '<repo>\TestResults\edit.log'

# build (overwrites Build\LaundryLegends.exe)
& 'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe' -batchmode -quit -projectPath '<repo>' `
  -executeMethod LaundryLegends.Editor.BuildGameTool.BuildDirect -logFile '<repo>\TestResults\build.log'
```

Player log: `%USERPROFILE%\AppData\LocalLow\LaundryLegends\Laundry Legends\Player.log`

## Environment notes

- Unity 6000.0.64f1 only (two unused editors were removed to free ~22 GB).
- Installed: Audacity 4.0.1, Krita 6.0.4, LMMS 1.2.2, FFmpeg 9.0.2, GitHub CLI 2.103.3.
- Free disk ~130 GB.
- Permissions: asks are all set to allow; destructive git/disk rules still deny.
