"""Flatten the alpha channel of a PNG to 1.0.

Used for the pilot's shoe texture: MakeHuman exports every texture with an
alpha channel wired into the material, and shoes01_diffuse.png carries alpha=0
over most of the shoe body, so Cycles/EEVEE (and any engine that honours the
alpha link) renders the shoes see-through.

Run:
  blender.exe -b --factory-startup --python makehuman_flatten_alpha.py -- --png <file>
"""
import bpy
import sys
import os
import numpy as np

argv = sys.argv
argv = argv[argv.index("--") + 1:] if "--" in argv else []
path = os.path.abspath(argv[argv.index("--png") + 1])

img = bpy.data.images.load(path)
w, h = img.size
alpha = np.empty(w * h * 4, dtype=np.float32)
img.pixels.foreach_get(alpha)
before = alpha[3::4]
alpha[3::4] = 1.0
img.pixels.foreach_set(alpha)
img.filepath_raw = path
img.file_format = 'PNG'
img.save()
print("flattened alpha: %s (%dx%d) min_before=%.3f max_before=%.3f"
      % (path, w, h, float(before.min()), float(before.max())))
