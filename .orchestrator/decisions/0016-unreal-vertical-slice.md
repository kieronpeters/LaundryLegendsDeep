# 0016 - Post-1.0 Unreal vertical slice scope

**Status:** Deferred by design (post-1.0)
**Date:** 2026-11-03
**Tickets:** LL-F-004 (sample parity), LL-F-007 (vertical slice)

## Context

Unreal Engine 5.8 is installed at `C:\Program Files\Epic Games\UE_5.8`.
`UnrealSample/` contains a standalone UE project and a headless Python scene
builder. The shipping game remains the Unity project; the Unreal sample is not
part of the build.

## Decision

- **LL-F-004:** use the installed UE 5.8 commandlet to verify that the sample
  project and `build_laundromat_scene.py` run. Parity is judged on the greybox
  layout, not on visual fidelity. The Unity build remains the reference.
- **LL-F-007:** the vertical slice is explicitly post-1.0. It is a remake/sample,
  not a code port.

## Slice scope (when it starts)

1. One laundromat room, machine wall, counter, folding table, player start.
2. One customer/order loop and the arcade cabinet interaction.
3. Save/load for wallet, clock, stock and arcade state.
4. Capture and compare the room layout with the Unity reference.

## Non-goals

- Full feature parity, networking, console builds, or shipping on Unreal.
- Porting Unity C# or UI verbatim.
- Replacing the Unity release.

## Prerequisites

- Owner approval to spend time on a non-shipping engine port.
- A machine that meets UE5 editor recommendations (the current laptop is below
  the 8 GB VRAM guidance; the sample greybox is expected to be usable).
- A frozen Unity release branch and asset export set.

## Evidence

- UE 5.8 installation verified on 2026-11-03.
- Headless sample run recorded in `Logs/unreal-sample.log` (if the commandlet
  succeeds).
