"""Check the rendered sign PNGs against the delivery requirements.

Reads the PNGs back and reports, per sign:
  * pixel dimensions
  * whether the frame is fully covered (no transparent pixels leaking in, and
    the four corners are opaque)
  * the share of pixels that are anti-aliased / transparent
  * the WCAG relative-luminance contrast ratio between the two dominant
    colours (board vs type)

Usage:
  blender.exe --background --factory-startup --python ArtSource/Signs/verify_signs.py
"""

import os
from collections import Counter

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))

EXPECTED = {
    "sign_facade.png": (2048, 384),
    "sign_dropoff.png": (1024, 256),
    "sign_machines.png": (2048, 256),
    "sign_open.png": (512, 256),
    "sign_staff.png": (512, 256),
    "sign_arcade.png": (1024, 256),
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


def check(name):
    path = os.path.join(HERE, name)
    img = bpy.data.images.load(path)
    w, h = img.size
    px = list(img.pixels)  # RGBA floats, already sRGB-encoded for PNG loads
    expect_w, expect_h = EXPECTED[name]
    ok_dims = (w, h) == (expect_w, expect_h)

    opaque = 0
    partial = 0
    clear = 0
    colours = Counter()
    for i in range(0, len(px), 4):
        a = px[i + 3]
        if a >= 0.999:
            opaque += 1
            key = tuple(round(px[i + k] * 255) for k in range(3))
            colours[key] += 1
        elif a <= 0.001:
            clear += 1
        else:
            partial += 1

    total = w * h
    corners = []
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        idx = (cy * w + cx) * 4
        corners.append(round(px[idx + 3], 3))

    top = colours.most_common(2)
    ratio = contrast(top[0][0], top[1][0]) if len(top) > 1 else 0.0
    board_share = top[0][1] / total * 100.0

    print(f"\n{name}")
    print(f"  size            {w}x{h}  {'OK' if ok_dims else 'MISMATCH expected %dx%d' % (expect_w, expect_h)}")
    print(f"  frame coverage  opaque {opaque / total * 100:.2f}%  partial {partial / total * 100:.2f}%  "
          f"transparent {clear / total * 100:.2f}%")
    print(f"  corner alpha    {corners}")
    print(f"  board colour    {top[0][0]}  ({board_share:.1f}% of pixels)")
    print(f"  type colour     {top[1][0] if len(top) > 1 else None}")
    print(f"  contrast ratio  {ratio:.2f}:1")
    return ok_dims, clear == 0, ratio


def main():
    all_ok = True
    for name in EXPECTED:
        if not os.path.exists(os.path.join(HERE, name)):
            print(f"MISSING {name}")
            all_ok = False
            continue
        ok_dims, filled, ratio = check(name)
        if not (ok_dims and filled):
            all_ok = False
        if ratio < 4.5:
            print(f"  WARNING low contrast on {name}")
    print("\nVERIFY RESULT:", "PASS" if all_ok else "FAIL")


main()
