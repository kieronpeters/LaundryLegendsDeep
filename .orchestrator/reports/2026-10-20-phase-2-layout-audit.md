# Phase 2 Layout Audit - LL-P2-001

- **Phase:** 2 - Physical laundromat
- **Task:** LL-P2-001
- **Status:** success (read-only audit; no scene or prefab changes)
- **Date:** 2026-10-20 (UTC+08:00)
- **Owner:** lead (`build`)
- **Inputs:** `Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab`, `Assets/LaundryLegends/Scenes/Laundromat.unity`, `Assets/LaundryLegends/Editor/GreyboxSetupTool.cs`, `TestResults/phase1-greybox.log`, `TestResults/greybox-entrance.png`, `TestResults/greybox-secret.png`

## Result

The existing greybox is a usable 16 x 12 m public room with a clear central aisle, counter, folding table, eight-machine wall, four ceiling lights, and a closed east-wall secret door. It does not yet have an entrance door, storefront windows, waiting chairs, storage/shelving, a utility alcove, or signage. The secret door is fully exposed; Phase 2 must conceal it without making it unreachable or opening the hidden room.

The smallest safe next implementation task is the entrance/window envelope (`LL-P2-002`). It is scoped to the south facade and does not require a room-wide rebuild.

## Actual dimensions

Parsed from the serialized prefab and scene YAML; positions are local to the `GreyboxRoom` root at `(0, 0, 0)`.

| Element | Position / extent | Notes |
|---|---|---|
| Room floor | 16.0 m (X) x 12.0 m (Z), top at `y = 0` | Interior x `-8..8`, z `-6..6` |
| Ceiling | bottom at `y = 3.2` | 3.2 m interior height |
| South wall opening | x `-2..2`, height 2.4 m | 4.0 m wide; header bottom at `y = 2.4` |
| Player spawn | `(0, 0.1, -4.5)`, facing +Z | 1.5 m inside the entrance plane |
| Player capsule | radius 0.3 m, height 1.8 m | Camera local height 1.6 m |
| Counter body | center `(-4.5, -2.5)`, 3.2 x 0.9 m | x `-6.1..-2.9`, z `-2.95..-2.05` |
| Counter top | center `(-4.5, -2.5)`, 3.4 x 1.1 m, top `y = 1.2` | x `-6.2..-2.8`, z `-3.05..-1.95` |
| Folding table body | center `(4.5, 1.0)`, 3.2 x 1.2 m | x `2.9..6.1`, z `0.4..1.6` |
| Folding table top | center `(4.5, 1.0)`, 3.4 x 1.4 m, top `y = 1.1` | x `2.8..6.2`, z `0.3..1.7` |
| Machine wall | 8 machines, x `-3.55..3.55`, z `4.9..5.6` | Back panel z `5.65..5.85` |
| Secret door | x `7.84`, z `2.4..3.6`, height 2.2 m | Frame z `2.25..3.75`; fully exposed |
| Ceiling lights | `(±4, 2.9, ±3)` | Four point lights, range 12, intensity 2.4 |

## Existing zones

| Zone | Current use | Constraint |
|---|---|---|
| Entry / vestibule | Open south gap and spawn | Keep at least 1.2 m clear through the opening |
| Central public aisle | Open floor from entrance toward machines | Primary player path; keep unobstructed |
| Counter / service | Counter on west side | Staff side is open; do not narrow below 1.2 m |
| Machine wall | North wall | Keep 1.5 m working clearance in front of machine doors |
| Folding area | East-center | Keep at least 1.2 m around the table |
| Secret-door approach | East wall at z ~3 | Keep at least 1.2 m clear in front of the closed door |

## Circulation and clearance assessment

- **Entrance to counter:** From `(0, -4.5)` to the counter front near `(-2.8, -2.5)` is approximately 3.5 m across open floor. No collider intersects the direct route.
- **Entrance to machine wall:** Direct route along x ~0 from z `-4.5` to z `4.9` is approximately 9.4 m of open floor. The folding table begins at x `2.8`, so it does not block the center lane.
- **Counter west aisle:** 1.8 m between the counter top and the west wall. Adequate for one player, but shelving deeper than 0.6 m here would reduce it below 1.2 m.
- **Folding table to east wall:** 1.8 m. This is also the approach to the secret door, so a concealment shelf must be shallow (target depth 0.25-0.35 m) and must not protrude into the lane.
- **Folding table to machine wall:** 3.2 m from the table top's north edge (`z = 1.7`) to the machine fronts (`z = 4.9`). Clear.
- **Counter to machine wall:** 6.85 m from counter top north edge (`z = -1.95`) to machine fronts (`z = 4.9`). Clear.
- **Camera clearance:** 1.6 m eye height with 3.2 m ceiling. Door headers at 2.4 m and the secret-door frame top at 2.31 m clear the camera. No low overhead geometry exists in the public path.

The existing `GreyboxSetupTool.ValidateGreyboxRoom` passed floor support at eight sample points, five wall rays, secret-door collider, four point lights, and player spawn. It does **not** test the full path, prop colliders, or camera clearance.

## Phase 2 deliverable inventory

| Deliverable | State | Gap |
|---|---|---|
| Playable layout with circulation | Partial | Open shell works; waiting/storage/utility zones are undefined |
| Entrance door | Missing | Only a 4 x 2.4 m opening exists |
| Windows | Missing | No glazing or daylight openings |
| Counter | Present (placeholder) | No service divider, queue marker, or staff boundary |
| Waiting chairs | Missing | No waiting area |
| Shelves / storage | Missing | No storage or clutter |
| Signage | Missing | No interior or storefront signage |
| Utility area | Missing | No staff-only utility alcove |
| Collision on critical paths | Partial | Floor/walls/props collide; no path-specific checks |
| Navigation surfaces | Missing | No customer navigation; no NavMesh package is installed |
| Lighting zones | Partial | Four point lights; no window/daylight contribution |
| Secret door/shelf access | Partial | Door and collider exist; no shelf or service-panel concealment |
| Scene validation | Partial | Greybox validator exists; no Phase 2 prop/path validation |

## Constraints for the remaining tasks

1. Preserve the 4.0 m south opening and the player spawn at `(0, 0.1, -4.5)`. A door assembly may narrow the clear opening but not below 1.2 m.
2. Do not add a threshold step higher than 0.05 m; the CharacterController must cross it without jumping.
3. Keep the central entrance-to-machine lane and the entrance-to-counter route at least 1.2 m wide.
4. Place waiting chairs along the south-east wall, outside the spawn-to-counter route.
5. Keep storage shelving on the west/north-west side, out of the central lane; limit depth to 0.6 m where it faces the counter aisle.
6. Keep the folding table and its 1.2 m working perimeter; do not place props between it and the east-wall secret approach.
7. Conceal the secret door with a shallow shelf/service panel, but preserve a visible seam or handle clue and the existing closed collider. No gap may allow entry into the hidden room.
8. Keep overhead signage above 2.1 m in circulation; the camera eye is at 1.6 m.
9. Do not add a navigation package. Until customers exist, represent navigation with deterministic path/clearance tests rather than NavMesh.
10. Re-run compile, greybox validation, EditMode, PlayMode, and a visual capture after each scene-affecting task.

## Proposed placement plan

- **Entrance:** double glass door centered in the south opening, with a frame and flat threshold; door leaves swing inward but stay clear of the spawn.
- **Windows:** two storefront windows in the south wall flanks, approximately 2.2 x 1.5 m, sill near 0.9 m; preserve the central entrance.
- **Waiting:** two or three chairs and a small side table along the south-east wall, facing the room.
- **Storage:** shallow shelving along the west wall north of the counter; no obstruction of the counter aisle.
- **Utility:** a small staff alcove in the north-west corner near the machine wall, marked by a half-height divider or floor material.
- **Signage:** a counter sign and a machine-wall sign, plus a small window decal; placeholder text only.
- **Secret concealment:** a shallow utility shelf/service panel over the east-wall door, offset enough to show a seam and handle.

These are constraints and a proposed plan, not built geometry. Only `LL-P2-002` should begin next.

## Scope audit

No file under `Assets/`, `Packages/`, or `ProjectSettings/` was changed by this audit. The only task outputs are this report and the orchestration/state documentation updates. The pre-existing Unity-generated untracked `ProjectSettings/PackageManagerSettings.asset` was not touched.

## Tests not performed

- No compile or test rerun was needed because no code or scene changed.
- No new screenshots were captured; the existing Phase 1 captures were reviewed.
- Human layout verification is deferred to `LL-P2-006`.

## Recommended next task

`LL-P2-002` - Add entrance door, storefront windows, and threshold. It is scoped to the south facade, preserves the existing room, and requires no package or project-setting change.
