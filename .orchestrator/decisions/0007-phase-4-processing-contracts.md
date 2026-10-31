# Decision 0007 - Phase 4 machine, timer, transfer, and audio contracts

- **Date:** 2026-10-31
- **Status:** Approved for implementation
- **Phase:** 4 - Laundry processing
- **Related tasks:** `LL-P4-001` through `LL-P4-007`

## Machine states

`Empty -> Loaded -> Running -> Complete -> Empty`

- `Empty`: no item; accepts one `LooseLaundry`.
- `Loaded`: item held; player may start the cycle or unload.
- `Running`: timer advances; load/unload rejected.
- `Complete`: timer finished; item can be unloaded or transferred.
- Invalid transitions no-op and do not change the stored item.

## Timer

- Uses `Time.deltaTime`, so `Time.timeScale == 0` freezes the cycle.
- Placeholder durations: washer 20 s, dryer 25 s.
- Progress is `elapsed / duration`, clamped 0-1.
- A later save phase must persist elapsed state; Phase 4 only keeps it in memory.

## Item and transfer rules

- Only `LaundryItemKind.LooseLaundry` can enter a washer or dryer.
- The exact stored instance is returned; no cloning.
- Washer completion does not auto-transfer; the player unloads and loads the dryer.
- A dryer rejects wet/unwashed items.
- Folding accepts only dried items and sets a simple `IsReady`/folded flag.
- No customers, payment, economy, or order lifecycle in Phase 4 beyond the ready flag.

## Feedback

- Machine prompt/status exposes state and 0-1 progress.
- Folding/ready feedback must be visible in the Game view, not only the console.
- IMGUI placeholders remain temporary.

## Audio fixes

- Replace the 84-second clap-heavy loop with a longer seamless playlist or a longer track.
- Voice lines must be non-spatial or clearly audible and triggered on entry, counter wait, and exit.
- Keep licenses/credits in `ASSET_INVENTORY.md` and `ArtSource/Licenses/`.

## Boundaries

- No customer AI, queue, payment, or economy in Phase 4.
- No new Unity packages or project-settings changes.
- Scene changes are additive and must preserve Phase 3 navigation/colliders.
