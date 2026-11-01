"""Render the Laundry Legends logo suite from 2D geometry in Blender.

Four deliverables, all built the same way as create_signs.py: a flat stack of
emission-shaded meshes in the XY plane, no lamps, no world light, an orthographic
camera and film transparency, with the Standard view transform so a hex value
written here lands in the PNG unchanged.

The emblem is a front-on washing-machine drum seen through a porthole badge:
  * cream outer ring, navy field
  * cream drum ring, teal glass
  * magenta water with a cream wave crest
  * a five-petal frangipani in cream, floating above the water

Everything is driven from one `build_mark()` so the four files stay identical.

  logo_mark.png          1024x1024  transparent badge, no text
  logo_wordmark.png      2048x512   mark + LAUNDRY LEGENDS / rule / LAUNDROMAT
  logo_shirt.png         1024x1024  single-colour cream print version
  sign_facade_logo.png   2048x384   fascia board, drop-in replacement for sign_facade.png

Usage:
  blender.exe --background --factory-startup --python ArtSource/Signs/create_logo.py
  blender.exe --background --factory-startup --python ArtSource/Signs/create_logo.py -- --only logo_mark
"""

import math
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
FONT_DIR = "C:/Windows/Fonts"
FONT_FILES = {
    "display": os.path.join(FONT_DIR, "impact.ttf"),
    "sans_narrow": os.path.join(FONT_DIR, "ARIALNB.TTF"),
}

# Brand palette (brief). Navy / cream / magenta are the laundromat colours;
# teal is the drum glass. sign_machines.png uses the older darker teal
# #25443F; the brief's #2E7D7A is used here because it survives downscaling.
NAVY = "1C2333"
NAVY_FIELD = "212A3E"
NAVY_MOTTLE = "27324A"
CREAM = "F3E6CB"
CREAM_DIM = "CBB894"
MAGENTA = "E63A81"
TEAL = "2E7D7A"
RIVET = "3A4A6B"

# Single ink for the garment print. Swap to "FFFFFF" for a white print on a dark
# tee, or to NAVY for a light garment - nothing else in the build changes.
SHIRT_INK = CREAM

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def arg_value(flag, default=None):
    if flag in ARGS:
        i = ARGS.index(flag)
        if i + 1 < len(ARGS) and not ARGS[i + 1].startswith("--"):
            return ARGS[i + 1]
        return True
    return default


# --------------------------------------------------------------------------- #
# colour + material
# --------------------------------------------------------------------------- #

def srgb(hex_string, alpha=1.0):
    """sRGB hex -> linear RGBA for Blender node sockets."""
    h = hex_string.lstrip("#")
    rgb = [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]

    def to_linear(u):
        return u / 12.92 if u <= 0.04045 else ((u + 0.055) / 1.055) ** 2.4

    return (to_linear(rgb[0]), to_linear(rgb[1]), to_linear(rgb[2]), alpha)


_MATERIALS = {}


def emission_material(name, base, mottle=None, amount=0.45, strength=1.0,
                      freq=(3.0, 3.0), detail=2.0, seed=0.0):
    key = f"{name}|{base}|{mottle}|{amount}|{freq[0]}|{freq[1]}|{detail}|{seed}|{strength}"
    if key in _MATERIALS:
        return _MATERIALS[key]

    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    tree = mat.node_tree
    tree.nodes.clear()

    out = tree.nodes.new("ShaderNodeOutputMaterial")
    out.location = (600, 0)
    emit = tree.nodes.new("ShaderNodeEmission")
    emit.location = (350, 0)
    emit.inputs["Strength"].default_value = strength
    emit.inputs["Color"].default_value = srgb(base)
    tree.links.new(emit.outputs["Emission"], out.inputs["Surface"])

    if mottle:
        coord = tree.nodes.new("ShaderNodeTexCoord")
        coord.location = (-700, 0)
        mapping = tree.nodes.new("ShaderNodeMapping")
        mapping.location = (-500, 0)
        mapping.inputs["Scale"].default_value = (freq[0], freq[1], 1.0)
        mapping.inputs["Location"].default_value = (seed, seed * 0.7, 0.0)
        noise = tree.nodes.new("ShaderNodeTexNoise")
        noise.location = (-300, 0)
        noise.inputs["Scale"].default_value = 1.0
        noise.inputs["Detail"].default_value = detail
        noise.inputs["Roughness"].default_value = 0.55

        ramp = tree.nodes.new("ShaderNodeValToRGB")
        ramp.location = (-100, -120)
        ramp.color_ramp.elements[0].position = 0.42
        ramp.color_ramp.elements[0].color = (0.0, 0.0, 0.0, 1.0)
        ramp.color_ramp.elements[1].position = 0.78
        ramp.color_ramp.elements[1].color = (amount, amount, amount, 1.0)

        mix = tree.nodes.new("ShaderNodeMixRGB")
        mix.location = (120, 0)
        mix.blend_type = "MIX"
        mix.inputs["Color1"].default_value = srgb(base)
        mix.inputs["Color2"].default_value = srgb(mottle)

        tree.links.new(coord.outputs["Object"], mapping.inputs["Vector"])
        tree.links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
        tree.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
        tree.links.new(ramp.outputs["Color"], mix.inputs["Fac"])
        tree.links.new(mix.outputs["Color"], emit.inputs["Color"])

    _MATERIALS[key] = mat
    return mat


def flat(name, colour):
    """Cached flat emission material - used for every logo shape."""
    return emission_material(f"M_{name}_{colour}", colour)


# --------------------------------------------------------------------------- #
# geometry primitives (2D, all in the XY plane)
# --------------------------------------------------------------------------- #

def link(ob):
    bpy.context.collection.objects.link(ob)
    return ob


def mesh_object(name, verts, faces, z, material, cx=0.0, cy=0.0):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([(v[0] + cx, v[1] + cy, z) for v in verts], [], faces)
    mesh.validate()
    mesh.update()
    ob = link(bpy.data.objects.new(name, mesh))
    if material:
        mesh.materials.append(material)
    return ob


def disc(name, r, z, material, cx=0.0, cy=0.0, segments=160):
    verts = [(r * math.cos(2 * math.pi * i / segments),
              r * math.sin(2 * math.pi * i / segments)) for i in range(segments)]
    return mesh_object(name, verts, [list(range(segments))], z, material, cx, cy)


def annulus(name, r_out, r_in, z, material, cx=0.0, cy=0.0, segments=160):
    """Ring built as a quad strip so no ngon has to bridge a hole."""
    verts = []
    for r in (r_out, r_in):
        for i in range(segments):
            a = 2 * math.pi * i / segments
            verts.append((r * math.cos(a), r * math.sin(a)))
    faces = [(i, segments + i, segments + (i + 1) % segments, (i + 1) % segments)
             for i in range(segments)]
    return mesh_object(name, verts, faces, z, material, cx, cy)


def rounded_rect_points(width, height, radius, segments=8):
    hw, hh = width / 2.0, height / 2.0
    r = max(0.0, min(radius, hw - 1e-4, hh - 1e-4))
    if r <= 0.0:
        return [(-hw, -hh), (hw, -hh), (hw, hh), (-hw, hh)]
    centres = ((hw - r, hh - r, 0.0), (-(hw - r), hh - r, 90.0),
               (-(hw - r), -(hh - r), 180.0), (hw - r, -(hh - r), 270.0))
    pts = []
    for cx, cy, start in centres:
        for i in range(segments + 1):
            a = math.radians(start + 90.0 * i / segments)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def board(name, width, height, z, material, radius=0.0, cx=0.0, cy=0.0):
    pts = rounded_rect_points(width, height, radius)
    ob = mesh_object(name, pts, [list(range(len(pts)))], z, material)
    ob.location = (cx, cy, 0.0)
    return ob


# --------------------------------------------------------------------------- #
# wave + water
# --------------------------------------------------------------------------- #

WAVE_FREQ = 0.85          # cycles per glass radius
WAVE_LEVEL = -0.40        # mean water line, as a fraction of the glass radius
WAVE_AMP = 0.12           # wave amplitude, same units
WAVE_PHASE = 0.12         # breaks the left/right symmetry
CREST = 0.092             # cream crest thickness, same units


def _wave(x, gr, level=WAVE_LEVEL, amp=WAVE_AMP, phase=WAVE_PHASE):
    return gr * (level + amp * math.sin(2.0 * math.pi * (WAVE_FREQ * x / gr + phase)))


def _arc_low(x, r):
    return -math.sqrt(max(r * r - x * x, 0.0))


def _arc_high(x, r):
    return math.sqrt(max(r * r - x * x, 0.0))


def _cross(r, level, amp, phase, thickness, side):
    """x where the wave (+thickness) meets the bottom of the circle."""
    def f(x):
        return _wave(x, r, level, amp, phase) + thickness * r - _arc_low(x, r)

    lo, hi = (0.0, r * 0.999) if side > 0 else (-r * 0.999, 0.0)
    flo = f(lo)
    if abs(flo) < 1e-12:
        return lo
    if flo * f(hi) > 0:
        return hi
    for _ in range(80):
        mid = (lo + hi) / 2.0
        fm = f(mid)
        if (fm > 0) == (flo > 0):
            lo, flo = mid, fm
        else:
            hi = mid
    return (lo + hi) / 2.0


def water(name, r, z, material, cx=0.0, cy=0.0, thickness=0.0, samples=240):
    """Region of the circle below the wave. A band of `thickness` sits between
    the wave and the wave raised by that amount, so two of these stack into
    water + crest with a shared edge and no seam."""
    t = thickness * r
    xl = _cross(r, WAVE_LEVEL, WAVE_AMP, WAVE_PHASE, thickness, -1)
    xr = _cross(r, WAVE_LEVEL, WAVE_AMP, WAVE_PHASE, thickness, +1)

    top, bot = [], []
    for i in range(samples + 1):
        x = xl + (xr - xl) * i / samples
        y = _wave(x, r)
        if t:
            top.append((x, max(y, min(y + t, _arc_high(x, r)))))
            bot.append((x, y))
        else:
            top.append((x, y))
            bot.append((x, _arc_low(x, r)))

    verts = top + list(reversed(bot))
    return mesh_object(name, verts, [list(range(len(verts)))], z, material, cx, cy)


def flower(name, r, z, material, cx=0.0, cy=0.0, petals=5, rotation=math.radians(90)):
    """Five overlapping obovate petals - a frangipani silhouette. Petals are
    wide enough to merge at the centre, which keeps the shape reading as a
    blossom (not a star) when it is only 13 px across."""
    objs = []
    a_len, b_len, offset = 0.46 * r, 0.350 * r, 0.50 * r
    for i in range(petals):
        a = rotation + 2.0 * math.pi * i / petals + (0.14 if i % 2 else -0.05)
        px = cx + offset * math.cos(a)
        py = cy + offset * math.sin(a)
        ca, sa = math.cos(a), math.sin(a)
        pts = []
        for k in range(44):
            t = 2.0 * math.pi * k / 44
            ex, ey = a_len * math.cos(t), b_len * math.sin(t)
            pts.append((px + ex * ca - ey * sa, py + ex * sa + ey * ca))
        objs.append(mesh_object(f"{name}_p{i}", pts, [list(range(len(pts)))], z, material))
    objs.append(disc(f"{name}_eye", 0.16 * r, z, material, cx, cy))
    return objs


# --------------------------------------------------------------------------- #
# the emblem
# --------------------------------------------------------------------------- #

# Radii as fractions of the badge radius. Chosen so the ring, the drum ring and
# the crest all survive a 64 px downsample (see logo_README.md).
R_RING_IN = 0.911
R_DRUM_OUT = 0.645
R_DRUM_IN = 0.516
R_FLOWER = 0.360      # of the glass radius
FLOWER_CY = 0.380     # of the glass radius, above centre


def build_mark(diameter, cx=0.0, cy=0.0, z=0.0, style="colour", label="Mark", ink=None):
    """One emblem. style='colour' or 'single' (one-colour print version)."""
    R = diameter / 2.0
    ring_in = R_RING_IN * R
    drum_out = R_DRUM_OUT * R
    drum_in = R_DRUM_IN * R
    glass = drum_in

    out = []
    cream = flat("Cream", ink or CREAM)
    navy = flat("Navy", NAVY_FIELD)
    teal = flat("Teal", TEAL)
    magenta = flat("Magenta", MAGENTA)

    if style == "colour":
        out.append(disc(f"{label}_Field", ring_in, z + 0.000, navy, cx, cy))
        out.append(annulus(f"{label}_Ring", R, ring_in, z + 0.004, cream, cx, cy))
        out.append(annulus(f"{label}_Drum", drum_out, drum_in, z + 0.008, cream, cx, cy))
        out.append(disc(f"{label}_Glass", glass, z + 0.012, teal, cx, cy))
        out.append(water(f"{label}_Water", glass, z + 0.016, magenta, cx, cy))
        out.append(water(f"{label}_Crest", glass, z + 0.020, cream, cx, cy,
                         thickness=CREST))
        out.extend(flower(f"{label}_Flower", R_FLOWER * glass, z + 0.024, cream,
                          cx, cy + FLOWER_CY * glass))
    else:
        out.append(annulus(f"{label}_Ring", R, ring_in, z + 0.000, cream, cx, cy))
        out.append(annulus(f"{label}_Drum", drum_out, drum_in, z + 0.004, cream, cx, cy))
        out.append(water(f"{label}_Water", glass, z + 0.008, cream, cx, cy))
        out.extend(flower(f"{label}_Flower", R_FLOWER * glass, z + 0.012, cream,
                          cx, cy + FLOWER_CY * glass))
    return out


# --------------------------------------------------------------------------- #
# text
# --------------------------------------------------------------------------- #

_FONTS = {}


def font(key):
    if key not in _FONTS:
        path = FONT_FILES[key]
        _FONTS[key] = bpy.data.fonts.load(path, check_existing=True)
        print(f"FONT {key}: {path}")
    return _FONTS[key]


def _mesh_bbox(ob):
    xs = [v.co.x for v in ob.data.vertices]
    ys = [v.co.y for v in ob.data.vertices]
    return min(xs), min(ys), max(xs), max(ys)


def text(name, body, colour, font_key="display", tracking=1.05, z=0.03,
         target_w=None, target_h=None, cx=0.0, cy=0.0):
    """Text object -> mesh, fitted to a width/height budget, centred on its ink."""
    cu = bpy.data.curves.new(name=f"{name}Curve", type="FONT")
    cu.body = body
    cu.font = font(font_key)
    cu.align_x = "CENTER"
    cu.align_y = "CENTER"
    cu.space_character = tracking
    cu.resolution_u = 24
    cu.materials.append(flat(name, colour))

    ob = link(bpy.data.objects.new(name, cu))
    ob.data.size = 1.0
    ob.location = (0.0, 0.0, z)

    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target="MESH")
    ob = bpy.context.view_layer.objects.active

    x0, y0, x1, y1 = _mesh_bbox(ob)
    w, h = max(x1 - x0, 1e-6), max(y1 - y0, 1e-6)
    limits = []
    if target_w:
        limits.append(target_w / w)
    if target_h:
        limits.append(target_h / h)
    scale = min(limits) if limits else 1.0
    if abs(scale - 1.0) > 1e-9:
        ob.scale = (scale, scale, 1.0)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    x0, y0, x1, y1 = _mesh_bbox(ob)
    ob.location = (cx - (x0 + x1) / 2.0, cy - (y0 + y1) / 2.0, z)
    bpy.context.view_layer.update()
    return ob


def measure(ob):
    x0, y0, x1, y1 = _mesh_bbox(ob)
    return (ob.location.x + x0, ob.location.y + y0,
            ob.location.x + x1, ob.location.y + y1)


# --------------------------------------------------------------------------- #
# render rig
# --------------------------------------------------------------------------- #

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _MATERIALS.clear()
    _FONTS.clear()


def render(name, width_px, height_px, world_width, samples=128):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_adaptive_sampling = False
    scene.cycles.use_denoising = False
    scene.cycles.filter_width = 1.5
    scene.cycles.max_bounces = 1
    scene.cycles.seed = 1

    scene.render.resolution_x = width_px
    scene.render.resolution_y = height_px
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.dither_intensity = 0.0
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.image_settings.compression = 60

    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0

    cam_data = bpy.data.cameras.new("LogoCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = world_width
    cam_data.clip_start = 0.1
    cam_data.clip_end = 50.0
    cam = link(bpy.data.objects.new("LogoCam", cam_data))
    cam.location = (0.0, 0.0, 10.0)
    scene.camera = cam

    out_path = os.path.join(HERE, f"{name}.png")
    scene.render.filepath = out_path
    bpy.ops.render.render(write_still=True)
    print(f"WROTE {out_path}  {width_px}x{height_px}")
    return out_path


def audit(name, world_width, world_height, texts, boxes=()):
    """Prove every text block and every emblem box sits inside the canvas."""
    half_w, half_h = world_width / 2.0, world_height / 2.0
    worst = 9.9
    for ob in texts:
        x0, y0, x1, y1 = measure(ob)
        margin = min(half_w - abs(x0), half_w - abs(x1), half_h - abs(y0), half_h - abs(y1))
        worst = min(worst, margin)
        flag = "OK " if margin > 0.02 else "!! "
        print(f"  {flag}{ob.name:<22} x[{x0:+.3f},{x1:+.3f}] y[{y0:+.3f},{y1:+.3f}] "
              f"margin={margin:+.3f}u")
    for label, (x0, y0, x1, y1) in boxes:
        margin = min(half_w - abs(x0), half_w - abs(x1), half_h - abs(y0), half_h - abs(y1))
        worst = min(worst, margin)
        flag = "OK " if margin > 0.02 else "!! "
        print(f"  {flag}{label:<22} x[{x0:+.3f},{x1:+.3f}] y[{y0:+.3f},{y1:+.3f}] "
              f"margin={margin:+.3f}u")
    print(f"AUDIT {name}: tightest margin {worst:+.3f} world units")
    if worst <= 0.0:
        raise SystemExit(f"AUDIT FAIL {name}: artwork leaves the canvas")
    return worst


# --------------------------------------------------------------------------- #
# builds
# --------------------------------------------------------------------------- #

def logo_mark():
    """1024x1024 transparent badge, no text. Fills 96.9 % of the canvas."""
    W, H = 4.0, 4.0
    build_mark(3.875, 0.0, 0.0, 0.0, "colour", "Mark")
    audit("logo_mark", W, H, [], [("mark", (-1.9375, -1.9375, 1.9375, 1.9375))])
    return render("logo_mark", 1024, 1024, W)


def logo_wordmark():
    """2048x512: mark at left, LAUNDRY LEGENDS, rule, LAUNDROMAT."""
    W, H = 8.0, 2.0
    build_mark(1.34, -3.12, 0.02, 0.0, "colour", "WM")
    title = text("Wordmark_title", "LAUNDRY LEGENDS", CREAM, "display",
                 tracking=1.08, z=0.03, target_w=6.05, cx=0.72, cy=0.24)
    rule = board("Wordmark_rule", 3.50, 0.030, 0.008,
                 flat("Magenta", MAGENTA), cx=0.72, cy=-0.26)
    sub = text("Wordmark_sub", "LAUNDROMAT", CREAM_DIM, "sans_narrow",
               tracking=1.46, z=0.03, target_h=0.155, cx=0.72, cy=-0.50)
    audit("logo_wordmark", W, H, [title, sub],
          [("mark", (-3.79, -0.65, -2.45, 0.69))])
    return render("logo_wordmark", 2048, 512, W)


def logo_shirt():
    """1024x1024 single-colour cream print. No gradients, one ink."""
    W, H = 4.0, 4.0
    build_mark(2.42, 0.0, 0.265, 0.0, "single", "Shirt", ink=SHIRT_INK)
    title = text("Shirt_title", "LAUNDRY LEGENDS", SHIRT_INK, "display",
                 tracking=1.10, z=0.03, target_w=3.40, target_h=0.29,
                 cx=0.0, cy=-1.33)
    audit("logo_shirt", W, H, [title],
          [("mark", (-1.21, -0.945, 1.21, 1.475))])
    return render("logo_shirt", 1024, 1024, W)


def sign_facade_logo():
    """2048x384 fascia: same board, frame and rivets as sign_facade.png, with
    the lockup left-aligned so the file swaps in without re-authoring."""
    W, H = 8.0, 1.5
    board("Facade_frame", W + 0.04, H + 0.04, 0.000, flat("CreamFrame", CREAM))
    board("Facade_board", W - 0.12, H - 0.12, 0.002,
          emission_material("M_NavyBoard", NAVY, NAVY_MOTTLE, amount=0.5, freq=(2.4, 2.4)),
          radius=0.05)

    build_mark(1.02, -3.07, 0.0, 0.02, "colour", "FacadeMark")

    title = text("Facade_title", "LAUNDRY LEGENDS", CREAM, "display",
                 tracking=1.08, z=0.06, target_w=5.86, target_h=0.62,
                 cx=0.65, cy=0.21)
    board("Facade_rule", 3.20, 0.028, 0.012, flat("Magenta", MAGENTA),
          cx=0.65, cy=-0.20)
    sub = text("Facade_sub", "LAUNDROMAT", CREAM_DIM, "sans_narrow",
               tracking=1.46, z=0.06, target_h=0.145, cx=0.65, cy=-0.44)

    for sx in (-1, 1):
        for sy in (-1, 1):
            disc(f"Facade_rivet_{sx}{sy}", 0.026, 0.014,
                 emission_material("M_Rivet", RIVET), cx=sx * 3.72, cy=sy * 0.575)

    audit("sign_facade_logo", W, H, [title, sub],
          [("mark", (-3.58, -0.51, -2.56, 0.51))])
    return render("sign_facade_logo", 2048, 384, W)


LOGOS = {
    "logo_mark": logo_mark,
    "logo_wordmark": logo_wordmark,
    "logo_shirt": logo_shirt,
    "sign_facade_logo": sign_facade_logo,
}


def main():
    only = arg_value("--only")
    if only and only != "all":
        todo = only.split(",")
    else:
        todo = list(LOGOS)
    for name in todo:
        if name not in LOGOS:
            raise SystemExit(f"unknown logo {name}; pick from {sorted(LOGOS)}")
        print(f"=== BUILD {name} ===")
        reset_scene()
        LOGOS[name]()


main()
