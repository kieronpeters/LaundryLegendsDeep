# 0015 - Save format and restore scope

**Status:** Accepted 2026-11-04
**Phase:** 14

## Decision

Use a single versioned JSON document at
`Application.persistentDataPath/laundry-legends.save.json`, written atomically
through a `.tmp` file. `JsonUtility` serializes a flat DTO; additions are
version-gated and missing fields fall back to documented defaults.

Persisted fields:

| Field | Restore behaviour |
|---|---|
| `Money` | `EconomyService.RestoreBalance` |
| `Day`, `MinuteOfDay` | `BusinessClock.State.Set` |
| `Detergent`, `Fragrance`, `Plastic` | shelf `SetCount`, clamped |
| `Reputation` | `LaundryReputation.SetValue` |
| `RoomUnlocked` | `SecretRoomController.UnlockByPurchase` if not already revealed |
| `ArcadeExpanded` | `ArcadeService.RestoreExpansion` (no charge) |
| `Master/Music/SfxVolume` | `GameAudioSettings` and PlayerPrefs |

Not persisted: live customers, in-flight orders, machine cycles, carried items
and per-machine arcade install/upgrade state. Those are transient and are
recreated by the normal spawner/staff loop on load. Per-machine arcade ownership
is a known follow-up because `ArcadeState` has no restore API yet.

## Recovery

- Missing file: new game.
- Corrupt or unreadable JSON: log a warning and start a new game; the existing
  file is left in place for diagnostics rather than deleted.
- Write failure: log and keep playing without a save.

## Testing

- EditMode round-trip and corrupt-file rejection with a temporary path.
- `SaveBootstrap` is skipped in batch mode so tests never touch the player file.
- Autosave every 60 s and on application quit.
