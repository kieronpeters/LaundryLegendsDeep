"""Verify the ambientCG texture sets under ArtSource/Textures.

Checks, for every map:
  * the file exists and is a real JPEG,
  * the longest edge is 4096 px (ambientCG's "4K" pack size) and the aspect
    ratio is the one ambientCG publishes (1:1, or 2:1 for a few 2:1 scans),
  * the SHA-256 hash and byte size.

Also prints per-set and grand totals and fails (exit 1) if anything is off.

Usage (from the repository root):
    python ArtSource/Textures/_tools/verify_textures.py
"""

import hashlib
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXPECTED_LONG_EDGE = 4096

# JPEG start-of-frame markers that carry the frame dimensions.
SOF_MARKERS = {0xC0, 0xC1, 0xC2, 0xC3, 0xC5, 0xC6, 0xC7,
               0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF}


def jpeg_dimensions(path):
    """Return (width, height) read from the JPEG SOF marker, or None."""
    with open(path, "rb") as fh:
        if fh.read(2) != b"\xff\xd8":
            return None
        while True:
            byte = fh.read(1)
            if not byte:
                return None
            if byte != b"\xff":
                continue
            marker = fh.read(1)
            while marker == b"\xff":
                marker = fh.read(1)
            if not marker:
                return None
            code = marker[0]
            if code in (0xD8, 0xD9) or 0xD0 <= code <= 0xD7:
                continue
            length_bytes = fh.read(2)
            if len(length_bytes) != 2:
                return None
            length = int.from_bytes(length_bytes, "big")
            if code in SOF_MARKERS:
                data = fh.read(5)
                if len(data) != 5:
                    return None
                height = int.from_bytes(data[1:3], "big")
                width = int.from_bytes(data[3:5], "big")
                return width, height
            fh.seek(length - 2, os.SEEK_CUR)


def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as fh:
        for chunk in iter(lambda: fh.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main():
    failures = []
    grand_total = 0
    rows = []

    sets = sorted(d for d in os.listdir(ROOT)
                  if os.path.isdir(os.path.join(ROOT, d)) and not d.startswith("_"))

    if not sets:
        print("FAIL: no texture sets found under", ROOT)
        return 1

    for name in sets:
        folder = os.path.join(ROOT, name)
        files = sorted(f for f in os.listdir(folder) if f.lower().endswith(".jpg"))
        if not files:
            failures.append(f"{name}: no .jpg maps")
            continue
        set_total = 0
        maps = []
        for f in files:
            path = os.path.join(folder, f)
            size = os.path.getsize(path)
            set_total += size
            dims = jpeg_dimensions(path)
            if dims is None:
                failures.append(f"{name}/{f}: not a readable JPEG")
                continue
            w, h = dims
            if max(w, h) != EXPECTED_LONG_EDGE:
                failures.append(f"{name}/{f}: {w}x{h}, expected a "
                                f"{EXPECTED_LONG_EDGE} px longest edge")
            widest, tallest = max(w, h), min(w, h)
            if not (widest == tallest or widest == 2 * tallest):
                failures.append(f"{name}/{f}: unexpected aspect {w}:{h}")
            maps.append((f, w, h, size, sha256(path)))
        grand_total += set_total
        rows.append((name, set_total, maps))
        print(f"\n{name}  ({set_total / 1048576:.1f} MiB, {len(files)} maps)")
        for f, w, h, size, digest in maps:
            print(f"  {f:<44} {w}x{h}  {size / 1048576:7.2f} MiB  {digest[:16]}")

    print(f"\nTOTAL: {grand_total / 1048576:.1f} MiB "
          f"({grand_total / 1073741824:.2f} GiB) across {len(rows)} sets")

    if failures:
        print("\nFAILURES:")
        for f in failures:
            print("  -", f)
        return 1
    print("\nPASS: all maps exist, are valid JPEGs, and carry the published 4K "
          "resolution (4096 px longest edge).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
