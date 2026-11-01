# -*- coding: utf-8 -*-
"""
Texture post-processing for the MakeHuman NPC exports.

Runs on the MakeHuman bundled Python (3.7.6 + numpy + PyQt5) so it uses exact
8-bit QImage PNG IO - no colour-management guesswork.

Two operations, both idempotent-ish and driven by the character spec:

  --flatten <png> [...]
      Force alpha to 255. MakeHuman writes every texture with an alpha channel
      and the shoe atlases are alpha=0 outside their UV islands, which makes
      shoes render see-through in Cycles/EEVEE and any engine honouring the
      alpha link.

  --tint <png> --neutral r,g,b --blue-hue h --blue-sat s --blue-val v
      Recolour a garment atlas into a warm palette:
        * low-saturation (white shirt) pixels are multiplied by `--neutral`
        * blue/cyan pixels (denim) are hue-shifted to a warm brown, keeping
          their shading detail
      Pixels outside those two buckets are left byte-identical.

Usage example (paths relative to the repo root):
  Tools\\MakeHuman\\Python\\python.exe ArtSource\\Blender\\makehuman_postprocess_textures.py ^
      --flatten ArtSource/Exports/Characters/textures/shoes04_diffuse.png ^
      --tint    ArtSource/Exports/Characters/textures/male_casualsuit06_diffuse.png ^
      --neutral 1.0,0.72,0.50 --blue-hue 0.075
"""

import os
import sys

import numpy as np
from PyQt5.QtGui import QImage


def _args():
    argv = sys.argv[1:]
    return argv


def log(msg):
    print(msg)
    sys.stdout.flush()


def load_rgba(path):
    img = QImage(path)
    if img.isNull():
        raise RuntimeError("cannot load image: %s" % path)
    if img.format() != QImage.Format_RGBA8888:
        img = img.convertToFormat(QImage.Format_RGBA8888)
    w, h = img.width(), img.height()
    bpl = img.bytesPerLine()
    raw = np.frombuffer(img.constBits(), np.uint8).reshape(h, bpl)
    # bytesPerLine can be padded; keep only the w*4 real bytes of each row.
    pix = raw[:, : w * 4].reshape(h, w, 4).copy()
    return pix


def save_rgba(path, pix):
    h, w = pix.shape[0], pix.shape[1]
    buf = np.ascontiguousarray(pix).tobytes()
    out = QImage(buf, w, h, w * 4, QImage.Format_RGBA8888)
    if not out.save(path, "PNG"):
        raise RuntimeError("cannot save image: %s" % path)


def rgb_to_hsv(rgb):
    """rgb float 0..1 (N,3) -> hue 0..1, sat 0..1, val 0..1 (all (N,))"""
    mx = rgb.max(axis=1)
    mn = rgb.min(axis=1)
    d = mx - mn
    sat = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0.0)
    hue = np.zeros(rgb.shape[0], dtype=np.float32)

    m = d > 1e-6
    r, g, b = rgb[:, 0], rgb[:, 1], rgb[:, 2]
    idx = m & (mx == r)
    hue[idx] = np.mod((g[idx] - b[idx]) / d[idx], 6.0)
    idx = m & (mx == g)
    hue[idx] = ((b[idx] - r[idx]) / d[idx]) + 2.0
    idx = m & (mx == b)
    hue[idx] = ((r[idx] - g[idx]) / d[idx]) + 4.0
    hue = (hue / 6.0) % 1.0
    return hue, sat, mx


def hsv_to_rgb(hue, sat, val):
    h6 = (hue % 1.0) * 6.0
    i = np.floor(h6).astype(np.int32) % 6
    f = h6 - np.floor(h6)
    p = val * (1.0 - sat)
    q = val * (1.0 - sat * f)
    t = val * (1.0 - sat * (1.0 - f))
    out = np.zeros((len(hue), 3), dtype=np.float32)
    for k, (rr, gg, bb) in enumerate([
            (val, t, p), (q, val, p), (p, val, t),
            (p, q, val), (t, p, val), (val, p, q)]):
        sel = i == k
        out[sel, 0] = rr[sel]
        out[sel, 1] = gg[sel]
        out[sel, 2] = bb[sel]
    return out


def flatten_alpha(path):
    pix = load_rgba(path)
    before = pix[..., 3]
    changed = int(np.count_nonzero(before != 255))
    pix[..., 3] = 255
    save_rgba(path, pix)
    log("flattened alpha: %s (%dx%d, %d px changed, min_before=%d)"
        % (path, pix.shape[1], pix.shape[0], changed, int(before.min())))


def warm_tint(path, neutral, blue_hue, blue_sat, blue_val):
    pix = load_rgba(path)
    flat = pix.reshape(-1, 4)
    rgb = flat[:, :3].astype(np.float32) / 255.0

    hue, sat, val = rgb_to_hsv(rgb)
    blue = (sat > 0.10) & (hue > 0.50) & (hue < 0.78) & (val > 0.05)
    neutral_px = (sat <= 0.10) & (val > 0.10)

    out = rgb.copy()
    # denim / cool blues -> warm brown, preserving relative shading
    if np.any(blue):
        h = blue_hue + (hue[blue] - 0.50) * 0.04
        s = np.clip(sat[blue] * blue_sat + 0.12, 0.0, 0.62)
        v = val[blue] * blue_val
        out[blue] = hsv_to_rgb(h, s, v)
    # whites / near-neutrals -> warm cream/tan
    if np.any(neutral_px):
        out[neutral_px] = out[neutral_px] * np.array(neutral, dtype=np.float32)

    flat[:, :3] = np.clip(out * 255.0 + 0.5, 0, 255).astype(np.uint8)
    save_rgba(path, pix.reshape(pix.shape))
    log("warm tint: %s (%dx%d, blue px=%d, neutral px=%d, neutral multiplier=%s, blue hue=%.3f)"
        % (path, pix.shape[1], pix.shape[0], int(blue.sum()), int(neutral_px.sum()),
           tuple(neutral), blue_hue))


def main():
    argv = _args()
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--flatten":
            flatten_alpha(argv[i + 1])
            i += 2
        elif a == "--tint":
            path = argv[i + 1]
            neutral = (1.0, 0.72, 0.50)
            blue_hue = 0.075
            blue_sat = 0.55
            blue_val = 0.80
            if "--neutral" in argv:
                neutral = tuple(float(x) for x in argv[argv.index("--neutral") + 1].split(","))
            if "--blue-hue" in argv:
                blue_hue = float(argv[argv.index("--blue-hue") + 1])
            if "--blue-sat" in argv:
                blue_sat = float(argv[argv.index("--blue-sat") + 1])
            if "--blue-val" in argv:
                blue_val = float(argv[argv.index("--blue-val") + 1])
            if not os.path.isfile(path):
                raise RuntimeError("missing texture: %s" % path)
            warm_tint(path, neutral, blue_hue, blue_sat, blue_val)
            i += 2
        else:
            i += 1
    log("postprocess done")


if __name__ == "__main__":
    main()
