# Decision 0009 — Real humanoid animation for NPCs

- **Date:** 2026-11-01
- **Status:** accepted
- **Phase:** 4 (realism pass)

## Problem

NPCs are rigged 100 Avatars humanoids, but all motion was procedural:
`NpcPose` rotated arm/leg bones directly, and `LaundryStaff` moved laundry by
teleporting items between slots. Result: T-pose-ish flailing and staff who never
touched the clothes.

## Source assets

| Asset | Author | License | Location |
| --- | --- | --- | --- |
| Universal Animation Library (Standard) | Quaternius | CC0 1.0 | `Assets/LaundryLegends/Art/Animations/UAL1_Standard.fbx` |
| 100 Avatars humanoids | 100 Avatars | CC0 | `Assets/LaundryLegends/Art/Models/NPC/*.fbx` |

The animation pack was published on itch.io; the itch download endpoint failed
in this environment, so the same CC0 standard build was retrieved from a public
GitHub mirror (`IAFahim/quaternius.universalAnimationLibrary.standard`,
`UAL1_Standard.fbx`, 24.9 MB, root motion disabled). The mirror's `LICENSE` is
the CC0 1.0 legal code. Provenance is recorded in `ART_CREDITS.md`.

## Import contract

- The animation FBX is imported as **Humanoid** with
  `avatarSetup = CreateFromThisModel`.
- Clips are taken directly as sub-assets of the FBX; no clip duplication into
  separate assets. This keeps the library upgradeable and avoids drift.
- NPC models stay **Humanoid** so Unity retargets the shared clips onto each
  character's proportions.

## Runtime contract

Two components replace `NpcPose`:

1. **`NpcLocomotion`** — samples world-space movement each frame and drives
   Animator parameters:
   - `Speed` (float, 0..1, normalized against a reference walk speed)
   - `Carrying` (bool; switches the locomotion set to the carry variant)
   - `Grounded` is not modelled yet (no jumping in Phase 4).
   It also owns facing so animation and transform rotation agree.

2. **`CarrySocket`** — a named hand/both-hands transform that becomes the parent
   of a carried `LaundryItem`. Enables “staff visibly carries laundry”:
   the item follows the hands from counter → washer → dryer → folding table.

`NpcPose` is retained only as a fallback when no Animator/controller is present
(e.g. tests that instantiate a bare prefab); it disables itself when an Animator
with a controller exists.

## Carry pose

The animation library is a general locomotion set and does not ship a dedicated
“carry basket” clip. Options considered:

- Additive upper-body layer authored in Blender — deferred; needs a Blender
  session and mask authoring.
- Avatar-mask override clip — needs a hand-authored clip, same cost.
- **Chosen:** hold the item at a two-handed carry anchor and bias the upper-arm
  bones toward a carry pose in `LateUpdate` after the Animator evaluates,
  blending in only while `Carrying` is true. Cheap, reversible, and keeps the
  real walk cycle for legs, hips, and spine.

## Rules

- No root motion: the FBX is the root-motion-disabled build; transform movement
  remains authoritative so nav/waypoints keep working.
- Animation must never gate gameplay: the wash/dry/fold state machine and its
  timers stay independent of Animator state.
- Tests keep working headless: `NpcLocomotion` degrades gracefully when the
  Animator is missing.

## Verification

See `.orchestrator/reports/animation-verification-plan.md` (QA-owned) and the
PlayMode loop test. Acceptance requires:
- humanoid avatar valid on the animation FBX,
- Idle/Walk/PickUp clips present and non-zero length,
- staff item parented to the carry socket while walking,
- the wash/dry/fold/wrap loop still completing,
- a captured screenshot reviewed by a human for “no flailing”.

## Verified implementation facts (2026-11-01, W1 investigation)

- Clip sub-asset names are `UAL1_Standard|Armature|<Take>`. Match on the last
  segment; `EndsWith("Idle_Loop")` would also hit `Sitting_Idle_Loop`.
- Imported takes default to **non-looping**, so `Idle_Loop` freezes after 3.125 s
  unless `AnimationClipSettings.loopTime` is enabled on the take.
- NPC skeletons use `mixamorig:*` bone names, which the suffix-based lookup in
  `CarryPose`/`NpcPose` handles.
- Machines `Machine_1` (-3.15, 0.55, 5.25) and `Machine_2` (-2.25, 0.55, 5.25)
  are **unrotated (+Z forward)** while sitting on the +Z wall, so
  `station.forward` points into the wall. Stand points must be derived from the
  room side, not from the station's forward vector.
- Counter at (-4.5, 0.55, -2.5); folding table at (4.5, 0.5, 1.0) with footprint
  x ∈ [2.9, 6.1], z ∈ [0.4, 1.6]. The serialized `PickupCustomer._tableStand`
  of (3.4, 0, 1) sits **inside** the table and was corrected to (2.55, 0, 1.0).
