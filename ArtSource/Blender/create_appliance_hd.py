"""Build a high-fidelity commercial washer / dryer for Laundry Legends.

Run:
  blender.exe -b -P ArtSource/Blender/create_appliance_hd.py
  blender.exe -b -P ArtSource/Blender/create_appliance_hd.py -- --fast
  blender.exe -b -P ArtSource/Blender/create_appliance_hd.py -- --notex --fast

Outputs
-------
  ArtSource/Blender/ApplianceHD.blend          (washer, editable source)
  ArtSource/Exports/ApplianceHD.fbx            (washer)
  ArtSource/Exports/ApplianceHD_Dryer.fbx      (dryer variant)
  ArtSource/Exports/appliance_albedo.png       2048 x 2048, sRGB
  ArtSource/Exports/appliance_roughness.png    2048 x 2048, linear
  ArtSource/Exports/appliance_normal.png       2048 x 2048, linear (OpenGL / +Y)
  ArtSource/Exports/appliance_front.png        1000 x 800
  ArtSource/Exports/appliance_three_quarter.png
  ArtSource/Exports/appliance_door_open.png
  ArtSource/Exports/appliance_dryer_three_quarter.png

Authoring conventions
---------------------
* Metres, Z up, origin on the floor at the cabinet centre.
* The ROOM-FACING side is -Y. Blender -Y becomes Unity -Z, which is what the
  existing `Appliance.fbx` does (`Machine_1`'s DrumSlot sits at local z = -0.36).
  Flipping this would spin all eight machines in the room around.
* Overall width and height match the old prop (0.800 / 1.100) so this is a
  drop-in replacement. Depth grows from ~0.73 m to ~0.78 m because the door now
  carries a real glass bowl that stands proud of the cabinet.

Hierarchy
---------
  ApplianceHD (root empty)
    +- Cabinet        painted shell, plinth, feet, kick plate, access cap
    +- ControlPanel   recessed fascia, drawer, 5 buttons, dial, display, vents
    +- CoinSlot       vend plate, slot bezel, coin-return flap
    +- Drum           perforated drum shell + 3 lifters
    +- Gasket         rubber lip around the drum mouth
    +- DoorPivot      empty AT THE HINGE (rotate about local Z to open)
         +- DoorRing  chromed bezel ring, hinge arm, latch
         +- DoorGlass tinted glass bowl

Everything is deterministic: fixed numbers, fixed RNG seed, no add-ons.
"""

import math
import os
import struct
import sys
import zlib

import bmesh
import bpy
import numpy as np
from mathutils import Vector, Matrix

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(PROJECT_ROOT, "ArtSource", "Blender", "ApplianceHD.blend")
EXPORT_DIR = os.path.join(PROJECT_ROOT, "ArtSource", "Exports")

ALBEDO_PATH = os.path.join(EXPORT_DIR, "appliance_albedo.png")
ROUGH_PATH = os.path.join(EXPORT_DIR, "appliance_roughness.png")
NORMAL_PATH = os.path.join(EXPORT_DIR, "appliance_normal.png")

TEX_SIZE = 2048
TRI_MIN, TRI_MAX = 3500, 6000

# ---------------------------------------------------------------------------
# Dimensions (metres). The room-facing side is -Y.
# ---------------------------------------------------------------------------
HALF_W = 0.400
Y_FACE = -0.347          # cabinet front plane
Y_BACK = 0.365
Z_BOTTOM = 0.058         # underside of the cabinet box
Z_TOP = 1.100
CORNER_CHAMFER = 0.014

DOOR_X, DOOR_Z = 0.0, 0.560
BORE_R = 0.196
RING_OUTER = 0.228
RING_INNER = 0.184
GLASS_R = 0.185
GLASS_Y = -0.396
DRUM_R = 0.186
DRUM_LIP_R = 0.194
DRUM_Y_FRONT = -0.326
DRUM_Y_BACK = 0.040

HINGE = (-(HALF_W - 0.014), -0.372, DOOR_Z)

CONSOLE_Z0, CONSOLE_Z1 = 0.892, 1.058
CONSOLE_X = 0.372
CONSOLE_FACE_Y = -0.374
FASCIA_Y = -0.363

# UV zones: (u0, u1, v0, v1); v = 0 at the bottom of the map.
ZONES = {
    "paint":   (0.000, 0.500, 0.000, 0.500),
    "metal":   (0.500, 0.750, 0.000, 0.500),
    "plastic": (0.750, 1.000, 0.000, 0.500),
    "glass":   (0.000, 0.500, 0.500, 1.000),
    "drum":    (0.500, 1.000, 0.500, 1.000),
}


# ===========================================================================
# 1. Byte-exact PNG writing
# ===========================================================================

def _png_chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def write_png(path, rgb_u8):
    """Write an (h, w, 3) uint8 array as an 8-bit RGB PNG, byte-exact.

    Blender's own image saving applies colour-management transforms, which makes
    "did I write 0.5 or 0.73?" ambiguous. Writing the container by hand keeps the
    bytes identical to the authored arrays, which is what Unity will import.
    """
    h, w, _ = rgb_u8.shape
    raw = np.concatenate([np.zeros((h, 1), np.uint8),
                          rgb_u8.reshape(h, w * 3)], axis=1).tobytes()
    blob = b"\x89PNG\r\n\x1a\n"
    blob += _png_chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
    blob += _png_chunk(b"IDAT", zlib.compress(raw, 9))
    blob += _png_chunk(b"IEND", b"")
    with open(path, "wb") as handle:
        handle.write(blob)
    print(f"[Tex] wrote {os.path.basename(path)} ({len(blob) / 1024.0:.0f} KB)")


# ===========================================================================
# 2. Procedural noise
# ===========================================================================

def _upsample(grid, h, w):
    """Quintic bilinear upsample of a small lattice, wrapping at the edges."""
    cy, cx = grid.shape
    ys = (np.arange(h, dtype=np.float32) + 0.5) * (cy / float(h))
    xs = (np.arange(w, dtype=np.float32) + 0.5) * (cx / float(w))
    y0 = np.floor(ys).astype(np.int32)
    x0 = np.floor(xs).astype(np.int32)
    fy = ys - y0
    fx = xs - x0
    fy = fy * fy * fy * (fy * (fy * 6.0 - 15.0) + 10.0)
    fx = fx * fx * fx * (fx * (fx * 6.0 - 15.0) + 10.0)
    y0m, x0m = y0 % cy, x0 % cx
    y1m, x1m = (y0m + 1) % cy, (x0m + 1) % cx
    v00 = grid[np.ix_(y0m, x0m)]
    v10 = grid[np.ix_(y1m, x0m)]
    v01 = grid[np.ix_(y0m, x1m)]
    v11 = grid[np.ix_(y1m, x1m)]
    a = v00 + (v10 - v00) * fy[:, None]
    b = v01 + (v11 - v01) * fy[:, None]
    return a + (b - a) * fx[None, :]


def fbm(h, w, rows, cols, octaves, rng, gain=0.5):
    total = np.zeros((h, w), np.float32)
    amp, norm = 1.0, 0.0
    r, c = float(rows), float(cols)
    for _ in range(octaves):
        grid = rng.random((max(2, int(round(r))),
                           max(2, int(round(c))))).astype(np.float32)
        total += amp * _upsample(grid, h, w)
        norm += amp
        amp *= gain
        r *= 2.0
        c *= 2.0
    return total / norm


def smoothstep(edge0, edge1, x):
    t = np.clip((x - edge0) / (edge1 - edge0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def zone_slice(zone):
    """(row_slice, col_slice) in PNG top-down row space for a UV zone."""
    u0, u1, v0, v1 = zone
    x0, x1 = int(u0 * TEX_SIZE), int(u1 * TEX_SIZE)
    y0 = int((1.0 - v1) * TEX_SIZE)
    y1 = int((1.0 - v0) * TEX_SIZE)
    return slice(y0, y1), slice(x0, x1)


def shape(h, w):
    return np.ones((h, w), np.float32)


def gradient_v(h, w):
    """1.0 at the top row of the zone, 0.0 at the bottom (UV v)."""
    return np.linspace(1.0, 0.0, h, dtype=np.float32)[:, None] * shape(h, w)


def gradient_u(h, w):
    return shape(h, w) * np.linspace(0.0, 1.0, w, dtype=np.float32)[None, :]


def scratches(h, w, count, length_px, width_px, rng, strength=1.0):
    """Soft elongated blobs: abrasion marks and fingerprints."""
    field = np.zeros((h, w), np.float32)
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    for _ in range(count):
        cy = rng.uniform(0.0, 1.0) * h
        cx = rng.uniform(0.0, 1.0) * w
        if rng.random() < 0.72:
            angle = rng.uniform(-0.28, 0.28)
        else:
            angle = rng.uniform(1.2, 1.9)
        length = rng.uniform(0.45, 1.0) * length_px
        width = rng.uniform(0.3, 1.0) * width_px
        ca, sa = math.cos(angle), math.sin(angle)
        du = (xs - cx) * ca + (ys - cy) * sa
        dv = -(xs - cx) * sa + (ys - cy) * ca
        field += (np.exp(-((du / length) ** 2 + (dv / width) ** 2) * 1.6)
                  * rng.uniform(0.35, 1.0) * strength)
    return np.clip(field, 0.0, 1.0)


def stamps(h, w, centres, radius, softness, value):
    """Circular soft stamps, evaluated locally so it stays cheap."""
    field = np.zeros((h, w), np.float32)
    pad = int(radius * softness) + 2
    for (cy, cx) in centres:
        y0 = max(0, int(cy) - pad)
        y1 = min(h, int(cy) + pad + 1)
        x0 = max(0, int(cx) - pad)
        x1 = min(w, int(cx) + pad + 1)
        if y0 >= y1 or x0 >= x1:
            continue
        ys, xs = np.mgrid[y0:y1, x0:x1].astype(np.float32)
        rad = np.sqrt((xs - cx) ** 2 + (ys - cy) ** 2)
        field[y0:y1, x0:x1] = np.maximum(
            field[y0:y1, x0:x1],
            smoothstep(radius * softness, radius, rad) * value)
    return field


def height_to_normal(height, strength):
    """OpenGL-convention tangent normal map (+Y up in UV)."""
    dcol = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5
    drow = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5
    nx = -dcol * strength
    ny = drow * strength
    nz = np.ones_like(height)
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    return (np.stack([nx / length, ny / length, nz / length], axis=-1) * 0.5 + 0.5)


# ===========================================================================
# 3. Texture synthesis
# ===========================================================================

PAINT_RGB = np.array([0.560, 0.618, 0.678], np.float32)
METAL_RGB = np.array([0.658, 0.668, 0.688], np.float32)
PLASTIC_RGB = np.array([0.105, 0.114, 0.128], np.float32)
DRUM_RGB = np.array([0.600, 0.612, 0.632], np.float32)
GLASS_RGB = np.array([0.300, 0.345, 0.375], np.float32)


def _fill(rgb, h, w):
    return np.broadcast_to(rgb, (h, w, 3)).copy()


def build_textures():
    rng = np.random.default_rng(20261101)
    albedo = np.zeros((TEX_SIZE, TEX_SIZE, 3), np.float32)
    rough = np.zeros((TEX_SIZE, TEX_SIZE), np.float32)
    height = np.zeros((TEX_SIZE, TEX_SIZE), np.float32)

    # ---- painted enamel --------------------------------------------------
    rows, cols = zone_slice(ZONES["paint"])
    h = rows.stop - rows.start
    w = cols.stop - cols.start
    v = gradient_v(h, w)
    u = gradient_u(h, w)

    mottle = fbm(h, w, 5, 5, 5, rng) - 0.5
    fine = fbm(h, w, 96, 96, 3, rng) - 0.5
    orange = fbm(h, w, 210, 210, 2, rng) - 0.5

    base = _fill(PAINT_RGB, h, w)
    base += mottle[..., None] * 0.034 + fine[..., None] * 0.010
    base *= (0.962 + 0.046 * v)[..., None]

    grime = smoothstep(0.16, 0.0, v) * (0.55 + 0.45 * fine)
    base *= (1.0 - 0.17 * grime)[..., None]

    # Scuffs cluster near the base and around the door opening.
    wear = scratches(h, w, 22, 95.0, 8.0, rng) * 0.75
    wear += scratches(h, w, 8, 40.0, 26.0, rng) * 0.30
    du = (u - 0.44)
    dv = (v - 0.66) * 0.55
    ring = np.exp(-((np.sqrt(du * du + dv * dv) - 0.30) ** 2) / 0.0011)
    wear = np.clip(wear + ring * 0.55, 0.0, 1.0)
    base = base * (1.0 - wear[..., None] * 0.15) + 0.145 * wear[..., None]

    albedo[rows, cols] = np.clip(base, 0.0, 1.0)
    rough[rows, cols] = np.clip(0.415 + mottle * 0.07 + fine * 0.035
                                + orange * 0.02 + wear * 0.17 + grime * 0.09,
                                0.0, 1.0)

    paint_h = fine * 0.55 + orange * 0.45
    seam_v = np.exp(-((u - 0.930) ** 2) / 0.0000020)
    seam_h = np.exp(-((v - 0.905) ** 2) / 0.0000020)
    paint_h = paint_h - np.maximum(seam_v, seam_h) * 0.85
    height[rows, cols] = paint_h

    # ---- brushed metal ---------------------------------------------------
    rows, cols = zone_slice(ZONES["metal"])
    h = rows.stop - rows.start
    w = cols.stop - cols.start
    brush = fbm(h, w, 5, 2, 4, rng) - 0.5
    grain = fbm(h, w, 260, 2, 2, rng) - 0.5
    mottle = fbm(h, w, 4, 4, 4, rng) - 0.5

    base = _fill(METAL_RGB, h, w)
    base += brush[..., None] * 0.052 + grain[..., None] * 0.028
    base += mottle[..., None] * 0.018
    base *= (0.972 + 0.028 * gradient_v(h, w))[..., None]
    albedo[rows, cols] = np.clip(base, 0.0, 1.0)
    rough[rows, cols] = np.clip(0.225 + brush * 0.075 + grain * 0.05, 0.05, 1.0)
    height[rows, cols] = grain * 0.4 + brush * 0.15

    # ---- dark plastic fascia --------------------------------------------
    rows, cols = zone_slice(ZONES["plastic"])
    h = rows.stop - rows.start
    w = cols.stop - cols.start
    mottle = fbm(h, w, 6, 6, 4, rng) - 0.5
    fine = fbm(h, w, 170, 170, 2, rng) - 0.5
    base = _fill(PLASTIC_RGB, h, w)
    base += mottle[..., None] * 0.012 + fine[..., None] * 0.006
    smudge = scratches(h, w, 9, 140.0, 44.0, rng) * 0.35
    base += smudge[..., None] * 0.030
    albedo[rows, cols] = np.clip(base, 0.0, 1.0)
    rough[rows, cols] = np.clip(0.445 + mottle * 0.05 - smudge * 0.15, 0.0, 1.0)
    height[rows, cols] = fine * 0.5

    # ---- perforated stainless drum --------------------------------------
    rows, cols = zone_slice(ZONES["drum"])
    h = rows.stop - rows.start
    w = cols.stop - cols.start
    brush = fbm(h, w, 900, 4, 2, rng) - 0.5
    base = _fill(DRUM_RGB, h, w) + brush[..., None] * 0.065

    holes_x, holes_y = 24, 40
    centres = []
    for iy in range(holes_y):
        for ix in range(holes_x):
            cx = (ix + 0.5) * (w / holes_x) + ((w / holes_x) * 0.5 if iy % 2 else 0.0)
            cy = (iy + 0.5) * (h / holes_y)
            centres.append((cy % h, cx % w))
    hole_mask = stamps(h, w, centres, (w / holes_x) * 0.30, 2.1, 1.0)
    base *= (1.0 - hole_mask * 0.80)[..., None]
    albedo[rows, cols] = np.clip(base, 0.0, 1.0)
    rough[rows, cols] = np.clip(0.300 + brush * 0.06 + hole_mask * 0.18, 0.05, 1.0)
    height[rows, cols] = brush * 0.3 - hole_mask * 1.1

    # ---- glass tint / smeared film --------------------------------------
    rows, cols = zone_slice(ZONES["glass"])
    h = rows.stop - rows.start
    w = cols.stop - cols.start
    smear = scratches(h, w, 24, 165.0, 58.0, rng)
    film = fbm(h, w, 4, 4, 4, rng) - 0.5
    base = _fill(GLASS_RGB, h, w) + film[..., None] * 0.02 + smear[..., None] * 0.045
    albedo[rows, cols] = np.clip(base, 0.0, 1.0)
    rough[rows, cols] = np.clip(0.060 + smear * 0.26 + film * 0.02, 0.0, 1.0)
    height[rows, cols] = smear * 0.05

    write_png(ALBEDO_PATH, (np.clip(albedo, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8))
    grey = np.clip(rough, 0.0, 1.0)[..., None]
    write_png(ROUGH_PATH, (np.repeat(grey, 3, axis=2) * 255.0 + 0.5).astype(np.uint8))
    normal = height_to_normal(height, 26.0)
    write_png(NORMAL_PATH, (normal * 255.0 + 0.5).astype(np.uint8))


# ===========================================================================
# 4. Mesh helpers
# ===========================================================================

def recalc(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def mesh_object(name, verts, faces, mat):
    mesh = bpy.data.meshes.new(f"{name}Mesh")
    mesh.from_pydata([tuple(v) for v in verts], [], [tuple(f) for f in faces])
    mesh.validate(verbose=False)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    mesh.materials.append(mat)
    return obj


def bevel(obj, width=0.006, segments=1, angle=32.0):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    edges = [e for e in bm.edges
             if e.is_manifold and e.calc_face_angle(0.0) > math.radians(angle)]
    if edges:
        bmesh.ops.bevel(bm, geom=edges, offset=width, offset_type="OFFSET",
                        segments=segments, profile=0.5, affect="EDGES",
                        clamp_overlap=True, loop_slide=True)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return obj


def chamfer_loop(x0, x1, z0, z1, c):
    """Chamfered rectangle in XZ, counter-clockwise."""
    return [(x0 + c, z0), (x1 - c, z0), (x1, z0 + c), (x1, z1 - c),
            (x1 - c, z1), (x0 + c, z1), (x0, z1 - c), (x0, z0 + c)]


def radial_loop(cx, cz, loop, count):
    """Intersect `count` rays from (cx, cz) with a closed polygon.

    The front plate is an annulus between this loop and a circle. Both rings
    must be sampled at the SAME angle or the bridging quads come out twisted
    (self-intersecting bow-ties that render as black slivers).
    """
    out = []
    for k in range(count):
        ang = 2.0 * math.pi * k / count
        dx, dz = math.cos(ang), math.sin(ang)
        best = None
        for i in range(len(loop)):
            x0, z0 = loop[i]
            x1, z1 = loop[(i + 1) % len(loop)]
            ex, ez = x1 - x0, z1 - z0
            det = -dx * ez + ex * dz
            if abs(det) < 1e-12:
                continue
            ax, az = x0 - cx, z0 - cz
            t = (-ax * ez + ex * az) / det
            s = (dx * az - dz * ax) / det
            if t > 1e-6 and -1e-6 <= s <= 1.0 + 1e-6:
                if best is None or t < best:
                    best = t
        if best is None:
            best = 1.0
        out.append((cx + dx * best, cz + dz * best))
    return out


def revolve(name, profile, cx, cz, segments, mat, closed_profile=False):
    """Revolve a (radius, y) profile around the Y axis at (cx, cz)."""
    verts, rings = [], []
    for radius, y in profile:
        if radius < 1e-6:
            rings.append([len(verts)])
            verts.append((cx, y, cz))
        else:
            ring = []
            for j in range(segments):
                a = 2.0 * math.pi * j / segments
                ring.append(len(verts))
                verts.append((cx + radius * math.cos(a), y,
                              cz + radius * math.sin(a)))
            rings.append(ring)
    faces = []
    pairs = [(i, i + 1) for i in range(len(rings) - 1)]
    if closed_profile:
        pairs.append((len(rings) - 1, 0))
    for i, k in pairs:
        A, B = rings[i], rings[k]
        if len(A) == 1 and len(B) == 1:
            continue
        if len(A) == 1:
            for j in range(segments):
                faces.append((A[0], B[j], B[(j + 1) % segments]))
        elif len(B) == 1:
            for j in range(segments):
                faces.append((A[j], A[(j + 1) % segments], B[0]))
        else:
            for j in range(segments):
                faces.append((A[j], A[(j + 1) % segments],
                              B[(j + 1) % segments], B[j]))
    return mesh_object(name, verts, faces, mat)


def chamfered_box(name, x0, x1, y0, y1, z0, z1, ch, mat, segments=1):
    verts = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
             (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    obj = mesh_object(name, verts, faces, mat)
    if ch > 0.0:
        bevel(obj, width=ch, segments=segments, angle=25.0)
    else:
        recalc(obj)
    return obj


def rect_ring_prism(name, outer, inner, y0, y1, mat):
    """Rectangular frame prism. outer/inner are (x0, x1, z0, z1)."""
    def loop(rect, y):
        x0, x1, z0, z1 = rect
        return [(x0, y, z0), (x1, y, z0), (x1, y, z1), (x0, y, z1)]

    O0, O1 = loop(outer, y0), loop(outer, y1)
    I0, I1 = loop(inner, y0), loop(inner, y1)
    verts = O0 + O1 + I0 + I1
    faces = []
    for i in range(4):
        j = (i + 1) % 4
        faces.append((i, j, 4 + j, 4 + i))
        faces.append((8 + i, 12 + i, 12 + j, 8 + j))
        faces.append((8 + j, 8 + i, i, j))
        faces.append((4 + i, 4 + j, 12 + j, 12 + i))
    obj = mesh_object(name, verts, faces, mat)
    recalc(obj)
    return obj


def join_objects(objects, name):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    joined = bpy.context.active_object
    joined.name = name
    joined.data.name = f"{name}Mesh"
    return joined


def shade_smooth(obj, smooth=True):
    for poly in obj.data.polygons:
        poly.use_smooth = smooth


def triangles(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def screw(name, x, z, y, mat, radius=0.0045):
    return revolve(name, [(0.000, y), (radius, y), (radius, y - 0.0018),
                          (0.000, y - 0.0022)],
                   x, z, 8, mat, closed_profile=True)


# ===========================================================================
# 5. Materials
# ===========================================================================

def _bsdf(mat):
    return mat.node_tree.nodes.get("Principled BSDF")


def set_input(bsdf, key, value):
    if key in bsdf.inputs:
        bsdf.inputs[key].default_value = value


def _image(path, non_color):
    img = bpy.data.images.load(path, check_existing=True)
    img.colorspace_settings.name = "Non-Color" if non_color else "sRGB"
    return img


def pbr_material(name, metallic, roughness_value, tint=None, alpha=1.0,
                 emission=None, normal_strength=1.0, maps=True):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = _bsdf(mat)
    set_input(bsdf, "Metallic", metallic)
    set_input(bsdf, "Roughness", roughness_value)
    set_input(bsdf, "Alpha", alpha)
    if tint is not None:
        set_input(bsdf, "Base Color", (*tint, 1.0))
    if emission is not None:
        set_input(bsdf, "Emission Color", (*emission, 1.0))
        set_input(bsdf, "Emission Strength", 4.0)

    tree = mat.node_tree
    if maps:
        tex = tree.nodes.new("ShaderNodeTexImage")
        tex.location = (-840, 320)
        tex.image = _image(ALBEDO_PATH, non_color=False)
        tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])

        rgh = tree.nodes.new("ShaderNodeTexImage")
        rgh.location = (-840, 20)
        rgh.image = _image(ROUGH_PATH, non_color=True)
        tree.links.new(rgh.outputs["Color"], bsdf.inputs["Roughness"])

    nrm = tree.nodes.new("ShaderNodeTexImage")
    nrm.location = (-840, -300)
    nrm.image = _image(NORMAL_PATH, non_color=True)
    nmap = tree.nodes.new("ShaderNodeNormalMap")
    nmap.location = (-540, -300)
    nmap.inputs["Strength"].default_value = normal_strength
    tree.links.new(nrm.outputs["Color"], nmap.inputs["Color"])
    tree.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def build_materials():
    return {
        "paint": pbr_material("MAT_ApplianceHD", 0.0, 0.42),
        "metal": pbr_material("MAT_ApplianceHD_Metal", 0.92, 0.24),
        "glass": pbr_material("MAT_ApplianceHD_Glass", 0.0, 0.06,
                              alpha=0.34, normal_strength=0.3),
        "rubber": pbr_material("MAT_ApplianceHD_Rubber", 0.0, 0.86,
                               tint=(0.050, 0.050, 0.054), maps=False,
                               normal_strength=0.55),
        "led": pbr_material("MAT_ApplianceHD_LED", 0.0, 0.20,
                            tint=(0.02, 0.05, 0.03),
                            emission=(0.12, 1.0, 0.34), maps=False),
    }


# ===========================================================================
# 6. Geometry
# ===========================================================================

def build_cabinet(mat, mat_metal):
    parts = []

    outer = chamfer_loop(-HALF_W, HALF_W, Z_BOTTOM, Z_TOP, CORNER_CHAMFER)
    ring_n = 48
    outer_rs = resample_loop(outer, ring_n)
    circle = [(DOOR_X + BORE_R * math.cos(2.0 * math.pi * i / ring_n),
               DOOR_Z + BORE_R * math.sin(2.0 * math.pi * i / ring_n))
              for i in range(ring_n)]

    verts = []
    for (x, z) in outer_rs:
        verts.append((x, Y_FACE, z))
    for (x, z) in outer_rs:
        verts.append((x, Y_BACK, z))
    for (x, z) in circle:
        verts.append((x, Y_FACE, z))
    for (x, z) in circle:
        verts.append((x, -0.262, z))
    centre_back = len(verts)
    verts.append((0.0, Y_BACK, (Z_BOTTOM + Z_TOP) * 0.5))

    faces = []
    n = ring_n
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))                 # outer wall
        faces.append((2 * n + i, 2 * n + j, j, i))         # front annulus
        faces.append((3 * n + i, 3 * n + j, 2 * n + j, 2 * n + i))  # bore
        faces.append((centre_back, n + j, n + i))          # back cap fan

    cabinet = mesh_object("CabinetShell", verts, faces, mat)
    recalc(cabinet)
    bevel(cabinet, width=0.009, segments=1, angle=30.0)
    parts.append(cabinet)

    # Recessed plinth / kick plate.
    parts.append(chamfered_box("KickPlate", -0.352, 0.352, -0.322, 0.348,
                               0.006, 0.058, 0.005, mat))

    # Adjustable feet. The revolve axis is Y, so rotate each foot 90 degrees
    # about X to stand it on the floor.
    foot_profile = [(0.000, 0.000), (0.026, 0.000), (0.026, 0.044),
                    (0.021, 0.056), (0.000, 0.056)]
    for sx in (-1, 1):
        for sy in (-1, 1):
            foot = revolve(f"Foot_{sx}_{sy}", foot_profile, 0.0, 0.0, 10,
                           mat_metal, closed_profile=True)
            offset_x = sx * 0.360
            offset_y = -0.312 if sy < 0 else 0.300
            for vert in foot.data.vertices:
                x, y, z = vert.co
                vert.co = Vector((x + offset_x, -z + offset_y, y))
            foot.data.update()
            shade_smooth(foot, True)
            parts.append(foot)

    # Drain filter access cap on the lower right of the front plate.
    parts.append(revolve("AccessCap",
                         [(0.000, Y_FACE), (0.046, Y_FACE),
                          (0.046, Y_FACE - 0.005), (0.040, Y_FACE - 0.008),
                          (0.000, Y_FACE - 0.009)],
                         0.292, 0.170, 18, mat, closed_profile=True))
    cap_screw = screw("CapScrew", 0.292, 0.170, Y_FACE - 0.009, mat_metal, 0.005)

    cabinet = join_objects(parts + [cap_screw], "Cabinet")
    shade_smooth(cabinet, False)
    for poly in cabinet.data.polygons:
        centre = poly.center
        if (centre.x - 0.292) ** 2 + (centre.z - 0.170) ** 2 < 0.047 ** 2:
            poly.use_smooth = True
    return cabinet


def build_dryer_louvres(mat, mat_metal):
    parts = []
    x0, x1, z0, z1 = 0.055, 0.338, 0.098, 0.352
    parts.append(rect_ring_prism("VentFrame",
                                 (x0, x1, z0, z1),
                                 (x0 + 0.020, x1 - 0.020, z0 + 0.020, z1 - 0.020),
                                 Y_FACE, Y_FACE - 0.011, mat))
    parts.append(chamfered_box("VentThroat", x0 + 0.019, x1 - 0.019,
                               Y_FACE + 0.026, Y_FACE + 0.032,
                               z0 + 0.019, z1 - 0.019, 0.0, mat))
    slat_count = 7
    span = (z1 - 0.030) - (z0 + 0.030)
    for i in range(slat_count):
        cz = z0 + 0.030 + span * (i + 0.5) / slat_count
        slat = chamfered_box(f"Slat_{i}", x0 + 0.020, x1 - 0.020,
                             Y_FACE - 0.002, Y_FACE + 0.018,
                             cz - 0.007, cz + 0.007, 0.0, mat_metal)
        pivot_y, pivot_z = Y_FACE + 0.008, cz
        rot = 0.58
        for vert in slat.data.vertices:
            dy = vert.co.y - pivot_y
            dz = vert.co.z - pivot_z
            vert.co.y = pivot_y + dy * math.cos(rot) - dz * math.sin(rot)
            vert.co.z = pivot_z + dy * math.sin(rot) + dz * math.cos(rot)
        slat.data.update()
        parts.append(slat)
    return parts


def build_control_panel(mat, mat_led):
    parts = []
    outer = (-CONSOLE_X, CONSOLE_X, CONSOLE_Z0, CONSOLE_Z1)
    inner = (-0.352, 0.352, 0.906, 1.044)
    parts.append(rect_ring_prism("ConsoleShell", outer, inner,
                                 Y_FACE, CONSOLE_FACE_Y, mat))
    parts.append(chamfered_box("Fascia", -0.356, 0.356,
                               FASCIA_Y, FASCIA_Y + 0.004,
                               0.902, 1.048, 0.0, mat))

    # -- detergent drawer --------------------------------------------------
    dz0, dz1 = 0.914, 1.036
    dx0, dx1 = -0.344, -0.152
    parts.append(chamfered_box("DrawerFront", dx0, dx1, FASCIA_Y - 0.008,
                               FASCIA_Y + 0.002, dz0, dz1, 0.003, mat))
    parts.append(chamfered_box("DrawerHandleBar", dx0 + 0.022, dx1 - 0.022,
                               FASCIA_Y - 0.020, FASCIA_Y - 0.007,
                               dz1 - 0.028, dz1 - 0.013, 0.003, mat))
    for sx in (-1, 1):
        hx = (dx0 + dx1) * 0.5 + sx * 0.085
        parts.append(chamfered_box(f"HandleRoot_{sx}",
                                   hx - 0.010, hx + 0.010,
                                   FASCIA_Y - 0.011, FASCIA_Y - 0.006,
                                   dz1 - 0.025, dz1 - 0.015, 0.002, mat))
    parts.append(chamfered_box("DrawerSeam", dx0 + 0.003, dx1 - 0.003,
                               FASCIA_Y - 0.0012, FASCIA_Y + 0.0012,
                               dz0 - 0.005, dz0 - 0.002, 0.0, mat))

    # -- rotary dial -------------------------------------------------------
    dial_cx, dial_cz = -0.078, 0.974
    parts.append(revolve("Dial",
                         [(0.000, FASCIA_Y), (0.036, FASCIA_Y),
                          (0.040, FASCIA_Y - 0.012), (0.040, FASCIA_Y - 0.020),
                          (0.032, FASCIA_Y - 0.026), (0.000, FASCIA_Y - 0.027)],
                         dial_cx, dial_cz, 22, mat))
    parts.append(chamfered_box("DialPointer", dial_cx - 0.004, dial_cx + 0.004,
                               FASCIA_Y - 0.030, FASCIA_Y - 0.024,
                               dial_cz + 0.012, dial_cz + 0.030, 0.0, mat_led))

    # -- display -----------------------------------------------------------
    bx0, bx1 = -0.030, 0.086
    bz0, bz1 = 0.990, 1.032
    parts.append(chamfered_box("DisplayBezel", bx0 - 0.008, bx1 + 0.008,
                               FASCIA_Y - 0.006, FASCIA_Y + 0.001,
                               bz0 - 0.007, bz1 + 0.007, 0.002, mat))
    parts.append(chamfered_box("DisplayGlass", bx0, bx1,
                               FASCIA_Y - 0.0040, FASCIA_Y - 0.0025,
                               bz0, bz1, 0.0, mat_led))
    parts.append(chamfered_box("DisplayBar", bx0 + 0.008, bx1 - 0.026,
                               FASCIA_Y - 0.0050, FASCIA_Y - 0.0040,
                               1.018, 1.023, 0.0, mat_led))
    for i in range(3):
        px = bx0 + 0.012 + i * 0.013
        parts.append(chamfered_box(f"StatusLed_{i}", px - 0.0035, px + 0.0035,
                                   FASCIA_Y - 0.0050, FASCIA_Y - 0.0040,
                                   0.998, 1.004, 0.0, mat_led))

    # -- five buttons ------------------------------------------------------
    for i in range(5):
        col, row = i % 3, i // 3
        bxx = 0.112 + col * 0.046
        bzz = 0.998 - row * 0.050
        parts.append(revolve(f"ButtonWell_{i}",
                             [(0.000, FASCIA_Y), (0.019, FASCIA_Y),
                              (0.019, FASCIA_Y - 0.0022),
                              (0.000, FASCIA_Y - 0.0022)],
                             bxx, bzz, 14, mat, closed_profile=True))
        parts.append(revolve(f"Button_{i}",
                             [(0.000, FASCIA_Y - 0.002), (0.014, FASCIA_Y - 0.002),
                              (0.014, FASCIA_Y - 0.009), (0.011, FASCIA_Y - 0.012),
                              (0.000, FASCIA_Y - 0.013)],
                             bxx, bzz, 16, mat))

    panel = join_objects(parts, "ControlPanel")
    shade_smooth(panel, False)
    return panel


def build_coin_slot(mat_metal, mat_dark):
    parts = []
    px0, px1, pz0, pz1 = 0.238, 0.336, 0.908, 1.046
    parts.append(chamfered_box("CoinPlate", px0, px1, FASCIA_Y - 0.005,
                               FASCIA_Y + 0.002, pz0, pz1, 0.0025, mat_metal))
    sx0, sx1 = 0.278, 0.296
    parts.append(rect_ring_prism("SlotBezel",
                                 (sx0 - 0.013, sx1 + 0.013, 0.998, 1.042),
                                 (sx0, sx1, 1.010, 1.032),
                                 FASCIA_Y - 0.005, FASCIA_Y - 0.013, mat_metal))
    parts.append(chamfered_box("SlotThroat", sx0 + 0.002, sx1 - 0.002,
                               FASCIA_Y - 0.0030, FASCIA_Y + 0.0005,
                               1.014, 1.028, 0.0, mat_dark))

    rx0, rx1 = 0.260, 0.314
    parts.append(chamfered_box("ReturnThroat", rx0, rx1, FASCIA_Y - 0.0012,
                               FASCIA_Y + 0.0012, 0.928, 0.966, 0.0, mat_dark))
    flap = chamfered_box("ReturnFlap", rx0 + 0.002, rx1 - 0.002,
                         FASCIA_Y - 0.011, FASCIA_Y - 0.005,
                         0.932, 0.962, 0.0015, mat_metal)
    for vert in flap.data.vertices:
        dz = vert.co.z - 0.962
        dy = vert.co.y - FASCIA_Y
        rot = -0.32
        vert.co.z = 0.962 + dz * math.cos(rot) - dy * math.sin(rot)
        vert.co.y = FASCIA_Y + dz * math.sin(rot) + dy * math.cos(rot)
    flap.data.update()
    parts.append(flap)

    for zz in (0.918, 1.036):
        parts.append(screw(f"CoinScrew_{zz:.3f}", px1 - 0.012, zz,
                           FASCIA_Y - 0.005, mat_metal, 0.004))

    coin = join_objects(parts, "CoinSlot")
    shade_smooth(coin, False)
    return coin


def build_drum(mat):
    profile = [
        (0.000, DRUM_Y_BACK + 0.030),
        (0.055, DRUM_Y_BACK + 0.026),
        (0.120, DRUM_Y_BACK + 0.010),
        (DRUM_R, DRUM_Y_BACK),
        (DRUM_R, DRUM_Y_FRONT + 0.014),
        (DRUM_LIP_R, DRUM_Y_FRONT),
        (DRUM_LIP_R, DRUM_Y_FRONT - 0.007),
        (DRUM_R - 0.002, DRUM_Y_FRONT - 0.003),
    ]
    drum = revolve("DrumShell", profile, DOOR_X, DOOR_Z, 36, mat)
    recalc(drum)
    mod = drum.modifiers.new("Wall", "SOLIDIFY")
    mod.thickness = 0.005
    mod.offset = -1.0
    bpy.context.view_layer.objects.active = drum
    bpy.ops.object.select_all(action="DESELECT")
    drum.select_set(True)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    shade_smooth(drum, True)

    parts = [drum]
    for index, angle in enumerate((math.radians(90), math.radians(210),
                                   math.radians(330))):
        ca, sa = math.cos(angle), math.sin(angle)
        tx, tz = -sa, ca
        y_front, y_back = -0.268, -0.132
        r_out, r_in = DRUM_R - 0.002, DRUM_R - 0.054
        thick = 0.006
        verts = []
        for sign in (-1.0, 1.0):
            for (yy, rr) in ((y_front, r_out), (y_back, r_out + 0.008),
                             (y_back + 0.014, r_in + 0.006),
                             (y_front + 0.018, r_in)):
                verts.append((DOOR_X + rr * ca + sign * thick * tx,
                              yy,
                              DOOR_Z + rr * sa + sign * thick * tz))
        faces = [(i, (i + 1) % 4, 4 + (i + 1) % 4, 4 + i) for i in range(4)]
        faces.append((0, 1, 2, 3))
        faces.append((7, 6, 5, 4))
        paddle = mesh_object(f"Lifter_{index}", verts, faces, mat)
        recalc(paddle)
        parts.append(paddle)

    drum = join_objects(parts, "Drum")
    shade_smooth(drum, True)
    return drum


def build_gasket(mat):
    profile = [
        (0.196, Y_FACE + 0.010),
        (0.206, Y_FACE - 0.002),
        (0.206, Y_FACE - 0.011),
        (0.197, Y_FACE - 0.015),
        (0.188, Y_FACE - 0.008),
        (0.186, Y_FACE + 0.022),
        (0.190, -0.300),
        (0.196, -0.292),
    ]
    gasket = revolve("GasketShell", profile, DOOR_X, DOOR_Z, 36, mat,
                     closed_profile=True)
    recalc(gasket)
    shade_smooth(gasket, True)
    gasket.name = "Gasket"
    gasket.data.name = "GasketMesh"
    return gasket


def build_door(mat_metal, mat_glass):
    parts = []
    profile = [
        (RING_INNER, -0.362),
        (RING_OUTER, -0.364),
        (RING_OUTER - 0.002, -0.378),
        (RING_OUTER - 0.011, -0.393),
        (RING_OUTER - 0.026, -0.401),
        (0.196, -0.401),
        (0.189, -0.395),
        (RING_INNER - 0.002, -0.372),
    ]
    ring = revolve("DoorRingShell", profile, DOOR_X, DOOR_Z, 40, mat_metal,
                   closed_profile=True)
    recalc(ring)
    shade_smooth(ring, True)
    parts.append(ring)

    parts.append(chamfered_box("HingeArm", -0.394, -0.198, -0.383, -0.361,
                               0.502, 0.618, 0.004, mat_metal))
    parts.append(chamfered_box("HingeBarrel", -0.396, -0.378, -0.385, -0.363,
                               0.462, 0.658, 0.007, mat_metal))
    parts.append(chamfered_box("LatchGrab", 0.198, 0.224, -0.388, -0.372,
                               0.516, 0.604, 0.005, mat_metal))
    parts.append(revolve("LatchPin", [(0.000, -0.392), (0.008, -0.392),
                                      (0.008, -0.383), (0.000, -0.383)],
                         0.211, 0.560, 10, mat_metal))

    ring = join_objects(parts, "DoorRing")
    shade_smooth(ring, False)
    return ring


def build_glass(mat_glass):
    profile = []
    rings = 5
    for i in range(rings + 1):
        t = i / float(rings)
        radius = GLASS_R * math.cos(t * math.pi * 0.5)
        bulge = 0.014 * math.sin(t * math.pi * 0.5)
        profile.append((radius, GLASS_Y - bulge))
    glass = revolve("DoorGlassMeshSrc", profile, DOOR_X, DOOR_Z, 40, mat_glass)
    recalc(glass)
    glass.name = "DoorGlass"
    glass.data.name = "DoorGlassMesh"
    shade_smooth(glass, True)
    return glass


# ===========================================================================
# 7. UVs
# ===========================================================================

def _zone_rect(zone):
    u0, u1, v0, v1 = zone
    pad_u = (u1 - u0) * 0.02
    pad_v = (v1 - v0) * 0.02
    return u0 + pad_u, u1 - pad_u, v0 + pad_v, v1 - pad_v


def box_uv(obj, zone, metres_per_zone=None):
    """Per-part box projection into one atlas zone.

    `metres_per_zone=None` fits the part's own bounds to the zone (right for
    large panels). A number uses a fixed world scale centred on the part, so
    small parts (buttons, screws) sample a small patch at consistent texel
    density instead of stretching the whole zone across 20 mm of plastic.
    """
    mesh = obj.data
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    layer = mesh.uv_layers[0]

    coords = [v.co for v in mesh.vertices]
    lo = [min(c[i] for c in coords) for i in range(3)]
    hi = [max(c[i] for c in coords) for i in range(3)]
    centre = [(lo[i] + hi[i]) * 0.5 for i in range(3)]

    u0, u1, v0, v1 = _zone_rect(zone)
    if metres_per_zone is None:
        span = max(hi[0] - lo[0], hi[1] - lo[1], hi[2] - lo[2], 1e-4)
        scale = 1.0 / span
        offset = lo
    else:
        scale = 1.0 / metres_per_zone
        offset = centre

    for poly in mesh.polygons:
        n = poly.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        tu, tv = ((1, 2) if ax == 0 else ((0, 2) if ax == 1 else (0, 1)))
        for li in poly.loop_indices:
            co = mesh.vertices[mesh.loops[li].vertex_index].co
            nu = (co[tu] - offset[tu]) * scale
            nv = (co[tv] - offset[tv]) * scale
            if metres_per_zone is not None:
                nu += 0.5
                nv += 0.5
            layer.data[li].uv = (u0 + nu * (u1 - u0), v0 + nv * (v1 - v0))


def cylindrical_uv(obj, zone, cx, cz, y0, y1):
    mesh = obj.data
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    layer = mesh.uv_layers[0]
    u0, u1, v0, v1 = _zone_rect(zone)

    per_loop = {}
    for poly in mesh.polygons:
        us = []
        for li in poly.loop_indices:
            co = mesh.vertices[mesh.loops[li].vertex_index].co
            dx, dz = co.x - cx, co.z - cz
            angle = (math.atan2(dz, dx) + math.pi) / (2.0 * math.pi)
            if math.hypot(dx, dz) < 1e-5:
                angle = 0.5
            axial = (co.y - y0) / max(1e-5, (y1 - y0))
            uv = (u0 + angle * (u1 - u0),
                  v0 + min(1.0, max(0.0, axial)) * (v1 - v0))
            per_loop[li] = uv
            us.append(uv[0])
        # Kill the wrap column so one quad does not smear across the whole zone.
        if max(us) - min(us) > (u1 - u0) * 0.5:
            for li in poly.loop_indices:
                uu, vv = per_loop[li]
                if uu < (u0 + u1) * 0.5:
                    per_loop[li] = (max(us), vv)
    for li, uv in per_loop.items():
        layer.data[li].uv = uv


# ===========================================================================
# 8. Assembly
# ===========================================================================

def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.objects, bpy.data.meshes, bpy.data.materials,
                       bpy.data.lights, bpy.data.cameras, bpy.data.images):
        for block in list(collection):
            try:
                collection.remove(block)
            except RuntimeError:
                pass


def build_appliance(variant):
    mats = build_materials()

    cabinet = build_cabinet(mats["paint"], mats["metal"])
    box_uv(cabinet, ZONES["paint"])

    panel = build_control_panel(mats["paint"], mats["led"])
    box_uv(panel, ZONES["plastic"])

    coin = build_coin_slot(mats["metal"], mats["paint"])
    box_uv(coin, ZONES["metal"])

    drum = build_drum(mats["metal"])
    cylindrical_uv(drum, ZONES["drum"], DOOR_X, DOOR_Z,
                   DRUM_Y_BACK, DRUM_Y_FRONT)

    gasket = build_gasket(mats["rubber"])
    box_uv(gasket, ZONES["plastic"])

    ring = build_door(mats["metal"], mats["glass"])
    box_uv(ring, ZONES["metal"])
    glass = build_glass(mats["glass"])
    box_uv(glass, ZONES["glass"])

    movers = [cabinet, panel, coin, drum, gasket]
    if variant == "dryer":
        vent = join_objects(build_dryer_louvres(mats["paint"], mats["metal"]),
                            "VentBank")
        shade_smooth(vent, False)
        box_uv(vent, ZONES["metal"])
        movers.append(vent)

    root = bpy.data.objects.new("ApplianceHD", None)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.15
    bpy.context.scene.collection.objects.link(root)
    root.location = (0.0, 0.0, 0.0)

    pivot = bpy.data.objects.new("DoorPivot", None)
    pivot.empty_display_type = "ARROWS"
    pivot.empty_display_size = 0.10
    bpy.context.scene.collection.objects.link(pivot)
    pivot.location = HINGE

    for obj in movers:
        obj.parent = root
        obj.matrix_parent_inverse = Matrix.Identity(4)

    # Bake the hinge offset into the door mesh so the exported FBX carries an
    # identity local transform. Using matrix_parent_inverse here would be lost
    # by the FBX exporter and the door would land a hinge-length away.
    hinge_vec = Vector(HINGE)
    for obj in (ring, glass):
        for vert in obj.data.vertices:
            vert.co = vert.co - hinge_vec
        obj.data.update()
        obj.location = (0.0, 0.0, 0.0)
        obj.parent = pivot
        obj.matrix_parent_inverse = Matrix.Identity(4)

    total = sum(triangles(o) for o in movers + [ring, glass])
    for obj in movers + [ring, glass]:
        print(f"[ApplianceHD] {variant:6s} {obj.name:14s} tris={triangles(obj):5d} "
              f"mats={[m.name for m in obj.data.materials]}")
    print(f"[ApplianceHD] {variant} total triangles: {total} "
          f"(target {TRI_MIN}-{TRI_MAX})")
    if not (TRI_MIN <= total <= TRI_MAX):
        print("[ApplianceHD] WARNING: triangle count outside the target band")
    return root, pivot, movers + [ring, glass], total


def export_fbx(path, objects):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        bake_space_transform=False,
        add_leaf_bones=False,
    )
    print(f"[ApplianceHD] exported {path}")


# ===========================================================================
# 9. Preview scene
# ===========================================================================

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def flat_plane(name, size, location, rotation, mat):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size[0], size[1], 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    return obj


def area_light(name, location, target, size, energy, color=(1.0, 1.0, 1.0)):
    data = bpy.data.lights.new(name=name, type="AREA")
    data.shape = "SQUARE"
    data.size = size
    data.energy = energy
    data.color = color
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj


def build_studio():
    def simple(name, rgb, rough):
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        bsdf = _bsdf(mat)
        set_input(bsdf, "Base Color", (*rgb, 1.0))
        set_input(bsdf, "Roughness", rough)
        return mat

    floor_mat = simple("MAT_StudioFloor", (0.34, 0.345, 0.355), 0.60)
    back_mat = simple("MAT_StudioBack", (0.285, 0.292, 0.305), 0.82)
    flat_plane("StudioFloor", (14.0, 14.0), (0.0, 0.0, -0.0015),
               (0.0, 0.0, 0.0), floor_mat)
    flat_plane("StudioBack", (14.0, 6.0), (0.0, 1.95, 2.6),
               (math.radians(90.0), 0.0, 0.0), back_mat)

    area_light("Key", (-2.10, -2.55, 3.10), (0.0, 0.0, 0.72), 2.2, 950.0,
               (1.0, 0.96, 0.90))
    area_light("Fill", (2.55, -2.05, 1.35), (0.0, 0.0, 0.62), 2.4, 300.0,
               (0.86, 0.92, 1.0))
    area_light("Rim", (1.35, 1.95, 2.45), (0.0, 0.0, 0.95), 1.4, 520.0,
               (1.0, 0.98, 0.96))
    area_light("Top", (0.0, -0.35, 3.35), (0.0, 0.0, 0.90), 1.8, 240.0)

    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg is not None:
        bg.inputs["Color"].default_value = (0.38, 0.40, 0.44, 1.0)
        bg.inputs["Strength"].default_value = 0.6


def make_camera(name, location, target, lens):
    data = bpy.data.cameras.new(name=name)
    data.lens = lens
    data.sensor_width = 36.0
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj


def configure_render(samples):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 8
    scene.cycles.transparent_max_bounces = 16
    scene.cycles.use_adaptive_sampling = True
    scene.cycles.adaptive_threshold = 0.02
    scene.render.resolution_x = 1000
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.color_depth = "8"
    scene.render.film_transparent = False
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass


# ===========================================================================
# 10. Verification
# ===========================================================================

def verify(path):
    for obj in list(bpy.context.scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat, do_unlink=True)
    bpy.ops.import_scene.fbx(filepath=path)
    print(f"[Verify] re-imported {os.path.basename(path)}")
    total = 0
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for obj in bpy.context.scene.objects:
        parent = obj.parent.name if obj.parent else "-"
        if obj.type == "MESH":
            tris = triangles(obj)
            total += tris
            olo = Vector((1e9, 1e9, 1e9))
            ohi = Vector((-1e9, -1e9, -1e9))
            for corner in obj.bound_box:
                world = obj.matrix_world @ Vector(corner)
                for i in range(3):
                    olo[i] = min(olo[i], world[i])
                    ohi[i] = max(ohi[i], world[i])
                    lo[i] = min(lo[i], world[i])
                    hi[i] = max(hi[i], world[i])
            osize = ohi - olo
            print(f"[Verify] {obj.name:14s} tris={tris:5d} parent={parent:10s} "
                  f"world=({olo.x:+.3f}..{ohi.x:+.3f}, {olo.y:+.3f}..{ohi.y:+.3f}, "
                  f"{olo.z:+.3f}..{ohi.z:+.3f}) "
                  f"size=({osize.x:.3f}, {osize.y:.3f}, {osize.z:.3f}) "
                  f"mats={[m.name for m in obj.data.materials]}")
        else:
            print(f"[Verify] {obj.name:14s} type={obj.type} parent={parent} "
                  f"loc=({obj.location.x:.4f}, {obj.location.y:.4f}, "
                  f"{obj.location.z:.4f})")
    size = hi - lo
    print(f"[Verify] total triangles: {total}")
    print(f"[Verify] bounds min=({lo.x:.4f}, {lo.y:.4f}, {lo.z:.4f}) "
          f"max=({hi.x:.4f}, {hi.y:.4f}, {hi.z:.4f})")
    print(f"[Verify] size: {size.x:.4f} x {size.y:.4f} x {size.z:.4f} m")
    return total, size


# ===========================================================================
# 11. Main
# ===========================================================================

def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    fast = "--fast" in argv
    skip_tex = "--notex" in argv
    skip_render = "--norender" in argv
    samples = 24 if fast else 64

    os.makedirs(EXPORT_DIR, exist_ok=True)
    if not skip_tex:
        build_textures()

    # ---- washer ----------------------------------------------------------
    clear_scene()
    root, pivot, all_objs, total_washer = build_appliance("washer")
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print(f"[ApplianceHD] saved {BLEND_PATH}")
    washer_fbx = os.path.join(EXPORT_DIR, "ApplianceHD.fbx")
    export_fbx(washer_fbx, [root, pivot] + all_objs)

    if not skip_render:
        build_studio()
        configure_render(samples)
        cam_front = make_camera("Cam_front", (0.0, -2.45, 0.66),
                                (0.0, 0.0, 0.575), 55.0)
        bpy.context.scene.camera = cam_front
        bpy.context.scene.render.filepath = os.path.join(
            EXPORT_DIR, "appliance_front.png")
        bpy.ops.render.render(write_still=True)
        print("[ApplianceHD] rendered appliance_front.png")

        cam_3q = make_camera("Cam_3q", (1.42, -2.02, 1.28),
                             (0.0, -0.02, 0.58), 55.0)
        bpy.context.scene.camera = cam_3q
        bpy.context.scene.render.filepath = os.path.join(
            EXPORT_DIR, "appliance_three_quarter.png")
        bpy.ops.render.render(write_still=True)
        print("[ApplianceHD] rendered appliance_three_quarter.png")

        pivot.rotation_euler = (0.0, 0.0, math.radians(-78.0))
        cam_open = make_camera("Cam_open", (0.86, -2.30, 1.08),
                               (0.0, -0.16, 0.58), 48.0)
        bpy.context.scene.camera = cam_open
        bpy.context.scene.render.filepath = os.path.join(
            EXPORT_DIR, "appliance_door_open.png")
        bpy.ops.render.render(write_still=True)
        print("[ApplianceHD] rendered appliance_door_open.png")
        pivot.rotation_euler = (0.0, 0.0, 0.0)

    verify(washer_fbx)

    # ---- dryer -----------------------------------------------------------
    clear_scene()
    root, pivot, all_objs, total_dryer = build_appliance("dryer")
    dryer_fbx = os.path.join(EXPORT_DIR, "ApplianceHD_Dryer.fbx")
    export_fbx(dryer_fbx, [root, pivot] + all_objs)

    if not skip_render:
        build_studio()
        configure_render(samples)
        cam = make_camera("Cam_dryer", (1.42, -2.02, 1.28),
                          (0.0, -0.02, 0.58), 55.0)
        bpy.context.scene.camera = cam
        bpy.context.scene.render.filepath = os.path.join(
            EXPORT_DIR, "appliance_dryer_three_quarter.png")
        bpy.ops.render.render(write_still=True)
        print("[ApplianceHD] rendered appliance_dryer_three_quarter.png")

    verify(dryer_fbx)
    print(f"[ApplianceHD] DONE washer={total_washer} dryer={total_dryer}")


if __name__ == "__main__":
    main()
