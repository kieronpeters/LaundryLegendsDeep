"""Verify the Laundry Legends decal set.

For every expected file this checks that it opens, has the expected pixel size,
carries an alpha channel where alpha is required, and that the alpha actually
varies (a decal whose alpha is all 1.0 or all 0.0 is unusable). Masks get an
extra tile-seam check.

  blender.exe --background --factory-startup \
      --python ArtSource/Decals/tools/verify_decals.py

Optional: pass `-- --preview <file.png>` to also write a contact sheet of every
decal composited over mid grey, in the order printed at the end.
"""

import os
import sys

import bpy
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, ".."))

# file, (width, height), alpha requirement
EXPECTED = [
    ("01_water_stain/waterstain_damp_color.png", (2048, 2048), "alpha"),
    ("01_water_stain/waterstain_damp_opacity.png", (2048, 2048), "mask"),
    ("01_water_stain/waterstain_damp_normal.png", (2048, 2048), "opaque"),
    ("01_water_stain/waterstain_broad_color.png", (2048, 2048), "alpha"),
    ("01_water_stain/waterstain_broad_opacity.png", (2048, 2048), "mask"),
    ("02_floor_grime/floorgrime_edge_color.png", (2048, 512), "alpha"),
    ("02_floor_grime/floorgrime_edge_opacity.png", (2048, 512), "mask"),
    ("02_floor_grime/floorgrime_edge_normal.png", (2048, 512), "opaque"),
    ("03_wall_wear/wallscuff_scratches_color.png", (2048, 2048), "alpha"),
    ("03_wall_wear/wallscuff_scratches_opacity.png", (2048, 2048), "mask"),
    ("03_wall_wear/wallscuff_smears_color.png", (2048, 2048), "alpha"),
    ("03_wall_wear/wallscuff_smears_opacity.png", (2048, 2048), "mask"),
    ("03_wall_wear/wallscuff_strip_color.png", (2048, 512), "alpha"),
    ("03_wall_wear/wallscuff_strip_opacity.png", (2048, 512), "mask"),
    ("04_rust_leak/leakstreak_color.png", (2048, 1024), "alpha"),
    ("04_rust_leak/leakstreak_opacity.png", (2048, 1024), "mask"),
    ("04_rust_leak/leakstreak_normal.png", (2048, 1024), "opaque"),
    ("04_rust_leak/ruststreak_color.png", (2048, 1024), "alpha"),
    ("04_rust_leak/ruststreak_opacity.png", (2048, 1024), "mask"),
    ("05_speckle/speckle_mask_tileable_1k.png", (1024, 1024), "alpha"),
    ("06_notices/notice_lost_umbrella_1024x1408.png", (1024, 1408), "alpha"),
    ("06_notices/notice_price_list_1024x1408.png", (1024, 1408), "alpha"),
]

TILEABLE = {"05_speckle/speckle_mask_tileable_1k.png"}


def load(path):
    img = bpy.data.images.load(path)
    img.colorspace_settings.name = "Non-Color"
    w, h = img.size
    buf = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(buf)
    arr = buf.reshape(h, w, 4)[::-1].copy()
    bpy.data.images.remove(img)
    return arr


def tile_seam(arr, channel):
    """Ratio of the wrap seam error to the mean neighbour-column error."""
    a = arr[:, :, channel]
    seam = np.abs(a[:, 0] - a[:, -1]).mean()
    inner = np.abs(np.diff(a, axis=1)).mean()
    return seam, inner


def thumb(arr, size):
    """Downsample the whole image into a `size` box, keeping the aspect ratio."""
    h, w, _ = arr.shape
    scale = min(size / w, size / h)
    tw = max(1, int(round(w * scale)))
    th = max(1, int(round(h * scale)))
    ys = np.linspace(0, h - 1, th).astype(int)
    xs = np.linspace(0, w - 1, tw).astype(int)
    return arr[ys][:, xs]


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    preview_path = None
    if "--preview" in argv:
        preview_path = argv[argv.index("--preview") + 1]

    failures = 0
    colours = []
    print("")
    for rel, (ew, eh), need in EXPECTED:
        path = os.path.join(ROOT, rel)
        if not os.path.exists(path):
            print(f"FAIL {rel}: missing")
            failures += 1
            continue
        arr = load(path)
        h, w, _ = arr.shape
        a = arr[:, :, 3]
        rgb = arr[:, :, :3]
        problems = []
        if (w, h) != (ew, eh):
            problems.append(f"size {w}x{h} != {ew}x{eh}")
        if not np.isfinite(arr).all():
            problems.append("non-finite pixels")
        alpha_used = float(a.min()) < 0.99 or float(a.max()) > 1.01
        if need in ("alpha", "mask"):
            if not alpha_used:
                problems.append("no alpha channel variation")
            if float(a.max()) <= 0.02:
                problems.append("fully transparent")
        if need == "opaque" and float(a.min()) < 0.99:
            problems.append("unexpected transparency")
        if float(rgb.max()) < 0.02:
            problems.append("all-black colour")
        if rel in TILEABLE:
            seam, inner = tile_seam(arr, 0)
            if seam > max(inner * 2.5, 0.02):
                problems.append(f"tile seam {seam:.3f} vs inner {inner:.3f}")
        else:
            seam, inner = (0.0, 0.0)

        status = "PASS" if not problems else "FAIL"
        failures += bool(problems)
        print(f"{status} {rel}")
        print(f"     {w}x{h}  alpha min {a.min():.3f} max {a.max():.3f} "
              f"mean {a.mean():.3f}  rgb mean {rgb.mean():.3f}")
        if rel in TILEABLE:
            print(f"     tile seam {seam:.4f} vs neighbour mean {inner:.4f}")
        for p in problems:
            print(f"     ! {p}")
        if need == "alpha":
            colours.append((rel, arr))

    if preview_path:
        cell = 288
        cols = 4
        rows = (len(colours) + cols - 1) // cols
        sheet = np.full((rows * cell, cols * cell, 3), 0.62, dtype=np.float32)
        for i, (_, arr) in enumerate(colours):
            t = thumb(arr, cell)
            ah, aw, _ = t.shape
            # Fit wide or tall decals into the square cell, keeping the aspect.
            scale = min(1.0, cell / max(aw, ah))
            if scale < 1.0:
                t = t[::int(round(1 / scale)), ::int(round(1 / scale))]
                ah, aw, _ = t.shape
            bg = np.full((ah, aw, 3), 0.62, dtype=np.float32)
            comp = t[:, :, 3:4] * t[:, :, :3] + (1 - t[:, :, 3:4]) * bg
            y = (i // cols) * cell + (cell - ah) // 2
            x = (i % cols) * cell + (cell - aw) // 2
            sheet[y:y + ah, x:x + aw] = comp
        img = bpy.data.images.new("sheet", width=sheet.shape[1],
                                  height=sheet.shape[0], alpha=False,
                                  float_buffer=False)
        img.colorspace_settings.name = "Non-Color"
        rgba = np.ones((sheet.shape[0], sheet.shape[1], 4), dtype=np.float32)
        rgba[:, :, :3] = sheet
        img.pixels.foreach_set(np.ascontiguousarray(rgba[::-1]).ravel())
        img.file_format = "PNG"
        img.filepath_raw = preview_path
        img.save()
        print("\npreview order (row major):")
        for i, (rel, _) in enumerate(colours):
            print(f"  {i // cols},{i % cols}  {rel}")
        print("preview written:", preview_path)

    print(f"\nDECAL VERIFY RESULT: {'PASS' if failures == 0 else 'FAIL'} "
          f"({len(EXPECTED) - failures}/{len(EXPECTED)} files ok)")
    if failures:
        sys.exit(1)


main()
