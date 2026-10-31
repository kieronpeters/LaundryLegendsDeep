# Wrapped bundle — authoring notes

- **Author:** workstream W2 (original asset, no external licence)
- **Script:** `ArtSource/Blender/create_wrapped_bundle.py`
- **Blend:** `ArtSource/Blender/WrappedBundle.blend`
- **Export:** `ArtSource/Exports/WrappedBundle.fbx`
- **Previews:** `ArtSource/Exports/bundle_front.png`, `bundle_three_quarter.png`, `bundle_top.png`

## Re-run

```powershell
& "C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe" -b -P "ArtSource\Blender\create_wrapped_bundle.py"
```

The script asserts its own budgets, so read the log — an earlier run reported
exit 0 while an `AssertionError` had fired, so the exit code alone is not a gate.

## Verified facts (final run)

| Item | Value |
| --- | --- |
| Total triangles | **1140** (budget 1200) — cloth 216, film 924 |
| Non-manifold edges | 0 on both meshes |
| Size | 0.2969 × 0.2206 × 0.1810 m |
| Pivot | base centre at world origin, min Z = 0.0000 |
| Hierarchy | `WrappedBundle` (empty, scale 1) → `BundleCloth`, `BundleFilm` |
| Film material | `MAT_BundleFilm`, base (0.88, 0.93, 0.96), roughness 0.06, metallic 0, **alpha 0.24**, IOR 1.45, coat 0.35 |
| Cloth material | `MAT_BundleCloth`, (0.11, 0.28, 0.34), roughness 0.85 |

## Honest limitations

- The shoulder reads as a smooth cone/tent rather than radiating gather folds.
- The knot is small — a twisted nub with two short tails, not a detailed tie.
- Flat shading produces large diagonal planes on the film.
- Flat colours only; no textures, no crease normal map.
- **Unity import not done.** The FBX preserves material *names*, but the film will
  need a transparent Standard-material setup (alpha ~0.25, `_Mode = 3`) before it
  reads correctly in game.

This is a solid placeholder-grade asset, not production art. The highest-value
next improvement is replacing the shoulder loft with real gather folds and
enlarging the knot, which needs the triangle budget raised.
