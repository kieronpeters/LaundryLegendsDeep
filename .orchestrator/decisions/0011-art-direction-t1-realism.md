# Decision 0011 — Art direction target: T1, realistic-leaning

- **Date:** 2026-11-01
- **Status:** accepted (owner decision)
- **Supersedes:** the "not photoreal" line in `MASTER_CONTEXT.md`

## Decision

The owner asked for a 2026 AAA appearance and then accepted that the realistic
ceiling for this team is **T1 — high-quality stylised, realistic-leaning**.
Photoreal *materials and lighting* are welcome; we do not chase T2/T3 tech we
cannot support.

## What T1 means concretely

| Dimension | Target |
| --- | --- |
| Rendering | URP + ACES tonemapping, bloom, grading, reflection probes |
| Lighting | HDRI sky, warm/cool practicals, contact shadows, baked or probe GI |
| Materials | PBR albedo/normal/roughness at 1–2K, decals, grime |
| Models | 2–8k tris props, bevels, real sub-parts; characters still a known gap |
| Animation | Humanoid clips, no procedural flailing, believable idle/walk |
| Audio | Mixer buses, machine SFX, footsteps, ducking |
| Density | Lived-in dressing, not empty rooms |

## What is explicitly out of scope

- HDRP, SSGI/SSR, volumetric fog, DLSS/FSR.
- 4K texture sets on a 4 GB VRAM GPU (2K max, floor may keep 4K).
- Photoreal human characters authored from scratch.
- T2/T3 production scale (15+ artists, $10M budgets).

## Consequences

- `MASTER_CONTEXT.md` must be amended so the "not photoreal" line does not
  contradict this decision.
- Every future art review is judged against T1, not against AAA.
- Character fidelity remains the weakest axis and the clearest reason we cannot
  honestly claim T2.
