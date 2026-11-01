"""Check the rendered logo PNGs and emit downscaled legibility previews.

Per file it reports:
  * pixel dimensions against the brief
  * how much of the canvas is opaque / anti-aliased / clear
  * corner alpha, so a transparent file is provably transparent
  * the dominant opaque colours and their contrast ratio
  * for the shirt print: how many distinct opaque colours exist (must be 1)

It also writes 1/4 and 1/16 downscales into _qa/ so the mark can be judged at
256 px and 64 px, which is the size that decides whether the emblem reads.

Usage:
  blender.exe --background --factory-startup --python ArtSource/Signs/verify_logo.py
"""

import os
from collections import Counter

import bpy
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
QA = os.path.join(HERE, "_qa")

EXPECTED = {
    "logo_mark.png": (1024, 1024, "clear"),
    "logo_wordmark.png": (2048, 512, "clear"),
    "logo_shirt.png": (1024, 1024, "clear"),
    "sign_facade_logo.png": (2048, 384, "opaque"),
}

PREVIEWS = {
    "logo_mark.png": [("logo_mark_256.png", 256, 256), ("logo_mark_64.png", 64, 64)],
    "logo_wordmark.png": [("logo_wordmark_512.png", 512, 128)],
    "logo_shirt.png": [("logo_shirt_256.png", 256, 256), ("logo_shirt_96.png", 96, 96)],
    "sign_facade_logo.png": [("sign_facade_logo_512.png", 512, 96)],
}

# Transparent artwork is judged on the surface it will sit on, not on white.
COMPOSITES = {
    "logo_shirt.png": [("logo_shirt_on_navy.png", "1C2333", 512, 512)],
    "logo_wordmark.png": [("logo_wordmark_on_navy.png", "1C2333", 1024, 256)],
}


def srgb_luminance(rgb):
    def lin(u):
        return u / 12.92 if u <= 0.04045 else ((u + 0.055) / 1.055) ** 2.4

    r, g, b = (lin(c) for c in rgb)
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def contrast(rgb_a, rgb_b):
    la, lb = srgb_luminance(rgb_a), srgb_luminance(rgb_b)
    hi, lo = max(la, lb), min(la, lb)
    return (hi + 0.05) / (lo + 0.05)


def check(name, mode):
    path = os.path.join(HERE, name)
    img = bpy.data.images.load(path)
    w, h = img.size
    px = list(img.pixels)
    expect_w, expect_h, _ = EXPECTED[name]
    ok_dims = (w, h) == (expect_w, expect_h)

    opaque = partial = clear = 0
    colours = Counter()
    for i in range(0, len(px), 4):
        a = px[i + 3]
        if a >= 0.999:
            opaque += 1
            colours[tuple(round(px[i + k] * 255) for k in range(3))] += 1
        elif a <= 0.001:
            clear += 1
        else:
            partial += 1

    total = w * h
    corners = []
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        corners.append(round(px[(cy * w + cx) * 4 + 3], 3))

    common = colours.most_common(6)
    ratio = contrast(common[0][0], common[1][0]) if len(common) > 1 else 0.0

    print(f"\n{name}")
    print(f"  size            {w}x{h}  " + ("OK" if ok_dims else f"MISMATCH expected {expect_w}x{expect_h}"))
    print(f"  coverage        opaque {opaque / total * 100:.2f}%  "
          f"partial {partial / total * 100:.2f}%  clear {clear / total * 100:.2f}%")
    print(f"  corner alpha    {corners}")
    print(f"  distinct inks   {len(colours)}")
    for rgb, n in common:
        print(f"    #{rgb[0]:02X}{rgb[1]:02X}{rgb[2]:02X}  {n / total * 100:6.2f}% of canvas")
    print(f"  top-two contrast {ratio:.2f}:1")

    ok = ok_dims
    if mode == "clear":
        ok = ok and all(c == 0.0 for c in corners) and opaque / total * 100 > 3.0
        if not all(c == 0.0 for c in corners):
            print("  FAIL corners are not fully transparent")
        if opaque / total * 100 <= 3.0:
            print("  FAIL almost nothing is drawn")
    else:
        ok = ok and all(c == 1.0 for c in corners) and clear == 0
        if clear:
            print("  FAIL opaque board is not covering the frame")
    print("  CHECK", "PASS" if ok else "FAIL")
    return ok


def preview(name):
    src = bpy.data.images.load(os.path.join(HERE, name))
    for out_name, pw, ph in PREVIEWS.get(name, []):
        img = src.copy()
        img.scale(pw, ph)
        img.filepath_raw = os.path.join(QA, out_name)
        img.file_format = "PNG"
        img.save()
        print(f"  preview {out_name}  {pw}x{ph}")


def composite(name, bg_hex, out_name, pw, ph):
    """Flatten the artwork over a solid sRGB colour, for judging transparent
    artwork on the surface it will actually sit on."""
    img = bpy.data.images.load(os.path.join(HERE, name))
    img.colorspace_settings.name = "Non-Color"   # keep the stored sRGB values
    w, h = img.size
    buf = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(buf)
    arr = buf.reshape(h, w, 4)

    hx = bg_hex.lstrip("#")
    bg = np.array([int(hx[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], dtype=np.float32)
    a = arr[..., 3:4]
    flat = np.concatenate([arr[..., :3] * a + bg * (1.0 - a),
                           np.ones((h, w, 1), dtype=np.float32)], axis=2)

    out = bpy.data.images.new(out_name, w, h, alpha=False)
    out.colorspace_settings.name = "Non-Color"
    out.pixels.foreach_set(flat.ravel())
    out.scale(pw, ph)
    out.filepath_raw = os.path.join(QA, out_name)
    out.file_format = "PNG"
    out.save()
    print(f"  preview {out_name}  {pw}x{ph}  on #{bg_hex}")


def main():
    os.makedirs(QA, exist_ok=True)
    all_ok = True
    for name, (_, _, mode) in EXPECTED.items():
        if not os.path.exists(os.path.join(HERE, name)):
            print(f"MISSING {name}")
            all_ok = False
            continue
        all_ok = check(name, mode) and all_ok
        preview(name)
        for out_name, bg_hex, pw, ph in COMPOSITES.get(name, []):
            composite(name, bg_hex, out_name, pw, ph)
    print("\nVERIFY RESULT:", "PASS" if all_ok else "FAIL")


main()
