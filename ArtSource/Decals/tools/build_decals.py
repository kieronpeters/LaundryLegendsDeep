"""Build the Laundry Legends decal set from CC0 source maps.

Input  : raw ambientCG maps (Color / Opacity / NormalGL) in a source cache folder.
Output : ArtSource/Decals/**  - decals with the opacity mask baked into the PNG
         alpha channel, plus the untouched opacity mask for shader work.

Most outputs are straight copies of the CC0 maps with alpha baked in. Three are
authored here (rust tint, edge-wear strip, tileable speckle) because no CC0
download matches the brief; every authored asset is derived only from the CC0
sources listed in ArtSource/Decals/README.md.

Usage:
  blender.exe --background --factory-startup \
      --python ArtSource/Decals/tools/build_decals.py \
      -- --sources <folder with ambientCG maps>
"""

import argparse
import os
import sys

import bpy
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_ROOT = os.path.abspath(os.path.join(HERE, ".."))


# ---------------------------------------------------------------- image helpers


def load_rgba(path):
    """Load a PNG as float RGBA, raw values, top-down."""
    img = bpy.data.images.load(path)
    img.colorspace_settings.name = "Non-Color"
    w, h = img.size
    buf = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(buf)
    arr = buf.reshape(h, w, 4)[::-1].copy()
    bpy.data.images.remove(img)
    return arr


def save_rgba(path, arr):
    """Write float RGBA (top-down) as an 8-bit PNG."""
    h, w, _ = arr.shape
    img = bpy.data.images.new(os.path.basename(path), width=w, height=h,
                              alpha=True, float_buffer=False)
    img.colorspace_settings.name = "Non-Color"
    img.pixels.foreach_set(np.ascontiguousarray(arr[::-1]).ravel())
    img.file_format = "PNG"
    img.filepath_raw = path
    img.save()
    bpy.data.images.remove(img)


def luma(rgb):
    return 0.299 * rgb[:, :, 0] + 0.587 * rgb[:, :, 1] + 0.114 * rgb[:, :, 2]


def mask_from_opacity(rgb):
    """Grayscale opacity map -> 0..1 mask."""
    return np.clip(luma(rgb), 0.0, 1.0)


def bake_alpha(color, mask):
    out = np.ones(color.shape[:2] + (4,), dtype=np.float32)
    out[:, :, :3] = color[:, :, :3]
    out[:, :, 3] = np.clip(mask, 0.0, 1.0)
    return out


def save_mask(path, mask):
    save_rgba(path, bake_alpha(np.repeat(mask[:, :, None], 3, axis=2), mask))


def copy_file(src, dst):
    # shutil.copyfile raises WinError 6 inside Blender background mode, so the
    # bytes are streamed by hand; the result is still a byte-identical copy.
    with open(src, "rb") as fh:
        data = fh.read()
    with open(dst, "wb") as fh:
        fh.write(data)
    return len(data)


def ensure(path):
    os.makedirs(path, exist_ok=True)
    return path


# ------------------------------------------------------------- authored assets


def build_rust_tint(streak_color, rust_color):
    """Tint a whitish leak streak with the colour distribution of CC0 rust."""
    # Sample only the brighter half of the rust map so dark gaps do not drag the
    # average toward black, then use that average colour as a plain multiply so
    # the streak keeps its own brightness variation.
    flat = rust_color[:, :, :3].reshape(-1, 3)
    lum = flat @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    keep = flat[lum >= np.percentile(lum, 60)]
    tint = keep.mean(axis=0) * 1.25
    tint = np.clip(tint, 0.0, 1.0)
    print("  rust tint (sRGB bytes):", np.round(tint * 255).astype(int).tolist())
    tinted = np.clip(streak_color[:, :, :3] * tint[None, None, :], 0.0, 1.0)
    return tinted


def build_edge_wear_strip(color, mask, band=512, gain=1.5, edge_power=1.8):
    """Crop a wall-scuff band and fade it away from the wear edge."""
    h, w = mask.shape
    top = (h - band) // 2
    color = color[top:top + band, :, :3]
    mask = mask[top:top + band, :]
    # Strongest at the top long edge (v = 0), gone by the bottom of the band.
    v = np.linspace(0.0, 1.0, band, dtype=np.float32)[:, None]
    fade = np.clip(1.0 - v, 0.0, 1.0) ** edge_power
    alpha = np.clip(mask * fade * gain, 0.0, 1.0)
    # Dust the strip slightly: worn paint is cooler and grubbier than the base.
    color = np.clip(color * 0.94, 0.0, 1.0)
    return color, alpha


def _periodic_value_noise(size, cells, rng):
    """Tileable bilinear value noise."""
    lattice = rng.random((cells, cells)).astype(np.float32)
    ys = np.linspace(0, cells, size, endpoint=False, dtype=np.float32)
    xs = np.linspace(0, cells, size, endpoint=False, dtype=np.float32)
    y0 = np.floor(ys).astype(int) % cells
    x0 = np.floor(xs).astype(int) % cells
    y1 = (y0 + 1) % cells
    x1 = (x0 + 1) % cells
    fy = (ys - np.floor(ys))[:, None]
    fx = (xs - np.floor(xs))[None, :]
    fy = fy * fy * (3 - 2 * fy)
    fx = fx * fx * (3 - 2 * fx)
    v00 = lattice[np.ix_(y0, x0)]
    v01 = lattice[np.ix_(y0, x1)]
    v10 = lattice[np.ix_(y1, x0)]
    v11 = lattice[np.ix_(y1, x1)]
    top = v00 * (1 - fx) + v01 * fx
    bottom = v10 * (1 - fx) + v11 * fx
    return top * (1 - fy) + bottom * fy


def build_speckle_mask(size=1024, seed=7):
    """Tileable speckle / dirt-noise mask (grain, crumbs, lint)."""
    rng = np.random.default_rng(seed)
    noise = np.zeros((size, size), dtype=np.float32)
    for cells, weight in ((8, 0.30), (16, 0.22), (32, 0.18), (64, 0.14), (128, 0.10)):
        noise += weight * _periodic_value_noise(size, cells, rng)
    noise /= 0.94
    # Bias low so the mask is mostly transparent and only specks read.
    noise = np.clip((noise - 0.36) / 0.64, 0.0, 1.0) ** 1.7

    # Hard specks: crumbs and lint, stamped with wrap-around so edges tile.
    specks = np.zeros((size, size), dtype=np.float32)
    yy, xx = np.mgrid[0:size, 0:size]
    for count, radius, value in ((900, 1.6, 0.95), (320, 3.2, 0.75), (90, 6.5, 0.5)):
        cy = rng.integers(0, size, count)
        cx = rng.integers(0, size, count)
        for y, x in zip(cy, cx):
            d2 = ((yy - y + size // 2) % size - size // 2) ** 2 + \
                 ((xx - x + size // 2) % size - size // 2) ** 2
            specks = np.maximum(specks, value * np.exp(-d2 / (2 * radius ** 2)).astype(np.float32))
    mask = np.clip(noise * 0.55 + specks, 0.0, 1.0)
    return mask


# ------------------------------------------------------------------- the build


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--sources", required=True)
    args = parser.parse_args(argv)
    src = os.path.abspath(args.sources)

    def s(name):
        path = os.path.join(src, name)
        if not os.path.exists(path):
            raise SystemExit("missing source map: " + path)
        return path

    report = []

    def out(folder, name):
        return os.path.join(ensure(os.path.join(OUT_ROOT, folder)), name)

    def emit(folder, stem, color, mask=None, normal=None, raw_color=None,
             raw_mask=None):
        """Write <stem>_color / _opacity / _normal into the subject folder."""
        if raw_color is not None:
            report.append((folder, stem + "_color", copy_file(raw_color,
                                                             out(folder, stem + "_color.png"))))
        else:
            p = out(folder, stem + "_color.png")
            save_rgba(p, bake_alpha(color, mask))
            report.append((folder, stem + "_color", os.path.getsize(p)))
        if raw_mask is not None:
            report.append((folder, stem + "_opacity", copy_file(raw_mask,
                                                              out(folder, stem + "_opacity.png"))))
        elif mask is not None:
            p = out(folder, stem + "_opacity.png")
            save_mask(p, mask)
            report.append((folder, stem + "_opacity", os.path.getsize(p)))
        if normal is not None:
            # ambientCG ships 16-bit normals; they are re-written as 8-bit so the
            # set stays game-ready and compressible.
            p = out(folder, stem + "_normal.png")
            arr = load_rgba(normal)
            save_rgba(p, bake_alpha(arr[:, :, :3],
                                    np.ones(arr.shape[:2], dtype=np.float32)))
            report.append((folder, stem + "_normal", os.path.getsize(p)))

    # 01 - water stain / damp patch -------------------------------------------
    for asset, stem in (("SurfaceImperfections001", "waterstain_damp"),
                        ("SurfaceImperfections002", "waterstain_broad")):
        color = load_rgba(s(asset + "_2K-PNG_Color.png"))
        mask = mask_from_opacity(load_rgba(s(asset + "_2K-PNG_Opacity.png")))
        normal = s(asset + "_2K-PNG_NormalGL.png") if asset.endswith("001") else None
        emit("01_water_stain", stem, color, mask, normal)
        print(f"  {stem}: alpha max {mask.max():.2f} mean {mask.mean():.2f}")

    # 02 - floor grime gradient -----------------------------------------------
    color = load_rgba(s("Leaking009_2K-PNG_Color.png"))
    mask = mask_from_opacity(load_rgba(s("Leaking009_2K-PNG_Opacity.png")))
    emit("02_floor_grime", "floorgrime_edge", color, mask,
         s("Leaking009_2K-PNG_NormalGL.png"))

    # 03 - wall scuff / edge wear ---------------------------------------------
    scuff_color = load_rgba(s("SurfaceImperfections013_2K-PNG_Color.png"))
    scuff_mask = mask_from_opacity(load_rgba(s("SurfaceImperfections013_2K-PNG_Opacity.png")))
    emit("03_wall_wear", "wallscuff_scratches", scuff_color, scuff_mask)

    smear_color = load_rgba(s("Smear007_2K-PNG_Color.png"))
    smear_mask = mask_from_opacity(load_rgba(s("Smear007_2K-PNG_Opacity.png")))
    emit("03_wall_wear", "wallscuff_smears", smear_color, smear_mask)
    strip_color, strip_alpha = build_edge_wear_strip(scuff_color, scuff_mask,
                                                     gain=1.9, edge_power=1.35)
    save_rgba(out("03_wall_wear", "wallscuff_strip_color.png"),
              bake_alpha(strip_color, strip_alpha))
    save_mask(out("03_wall_wear", "wallscuff_strip_opacity.png"), strip_alpha)
    report.append(("03_wall_wear", "wallscuff_strip", 0))

    # 04 - leak and rust streaks ----------------------------------------------
    leak_color = load_rgba(s("Leaking001_2K-PNG_Color.png"))
    leak_mask = mask_from_opacity(load_rgba(s("Leaking001_2K-PNG_Opacity.png")))
    emit("04_rust_leak", "leakstreak", leak_color, leak_mask,
         s("Leaking001_2K-PNG_NormalGL.png"))

    rust_color_src = load_rgba(s("Leaking004_2K-PNG_Color.png"))
    rust_mask = mask_from_opacity(load_rgba(s("Leaking004_2K-PNG_Opacity.png")))
    rust_palette = load_rgba(s("Rust009_1K-PNG_Color.png"))
    rust_tinted = build_rust_tint(rust_color_src, rust_palette)
    emit("04_rust_leak", "ruststreak", rust_tinted, rust_mask)

    # 05 - tileable speckle ---------------------------------------------------
    speckle = build_speckle_mask()
    save_rgba(out("05_speckle", "speckle_mask_tileable_1k.png"),
              bake_alpha(np.repeat(speckle[:, :, None], 3, axis=2), speckle))
    print(f"  speckle: mean {speckle.mean():.3f} max {speckle.max():.2f}")

    print("\nWROTE")
    for folder, stem, size in report:
        print(f"  {folder}/{stem}  {size/1024:,.0f} KB")


main()
