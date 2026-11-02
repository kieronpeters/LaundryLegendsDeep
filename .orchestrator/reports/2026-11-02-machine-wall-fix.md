# Machine-wall fix and Phase 10 queue update

**Date:** 2026-11-02
**Status:** Complete; awaiting owner playtest
**Commit:** `8d86fef` on `phase-9`

## Defect

Only `Machine_1` and `Machine_2` had `MachineLoadHook` components; the other six
appliances were decorative. `LaundryStaff` also hard-coded those two names, so the
order pipeline could only ever use the left two machines.

## Fix

- `MachineSetupTool` now wires all eight machines:
  - `Machine_1/3/5/7` are washers.
  - `Machine_2/4/6/8` are dryers.
  - Each has a `DrumSlot` or `DryerSlot` with a `CarrySlot`, and a
    `MachineLoadHook` configured to the matching kind.
- `LaundryStaff` now sorts the hooks by name and picks the first **free** machine
  of the required kind. If the player occupies the preferred unit, the staff uses
  the next free one instead of waiting forever.
- The old Phase 3/4 and appliance validators no longer assert the single-hook
  prototype; machine-wall validation is owned by `MachineSetupTool`.
- Phase 10's order queue is now **10 active orders** (was 5); the ready shelf stays
  at 10.

## Verification

| Check | Result |
|---|---|
| Machine wall validation | **4 washers + 4 dryers, each with a load slot** |
| EditMode suite | **158 / 158 passed** |
| PlayMode suite | **59 passed, 0 failed, 2 explicit skipped** |
| Windows build | **198 MB, 0 errors** at `Build/LaundryLegends.exe` |

Evidence: `Logs/machines-build.log`, `Logs/machines-validate.log`,
`TestResults/machines-editmode.xml`, `TestResults/machines-playmode.xml`.

## Not verified

- The owner has not walked the wall and confirmed every machine accepts a load in
  the shipped build. That remains part of the Phase 9/10 playtest.
- Visual realism is unchanged; the eight models are the existing stylised
  front-load units. Filed as `LL-A-005` for Phase 12.

## Notes

- The existing capture `TestResults/captures/03-machine-wall.png` shows all eight
  appliances with round doors and control panels; the missing piece was the
  interaction wiring, not the geometry.
- The build was produced after this fix. The previous running player was closed
  before rebuilding.
