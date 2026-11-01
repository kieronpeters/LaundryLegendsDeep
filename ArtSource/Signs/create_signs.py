"""Render Laundry Legends sign artwork as flat, self-lit sign boards.

Every sign is a stack of 2D meshes (board, frame, rules, rivets, converted text)
shaded with pure emission inside an empty world, filmed with a transparent
background and an orthographic camera. Nothing is lit by a lamp, nothing casts a
shadow, and no floor or studio backdrop exists, so the PNG is exactly the sign.

Colour management is forced to the Standard view transform, which means a hex
value written here lands in the PNG as that same hex value.

Text is real Blender text objects using
  * Impact (C:/Windows/Fonts/impact.ttf)         - heavy condensed display type
  * Arial Bold (C:/Windows/Fonts/arialbd.ttf)    - secondary lines
  * Arial Narrow Bold (C:/Windows/Fonts/ARIALNB.TTF) - wide-tracked small caps
converted to meshes before rendering so kerning and spacing are baked.

Usage:
  blender.exe --background --factory-startup --python ArtSource/Signs/create_signs.py
  blender.exe --background --factory-startup --python ArtSource/Signs/create_signs.py -- --only sign_open
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

# Palette. Cream, dark navy and magenta are the laundromat brand colours;
# cyan/indigo belong to the arcade room.
NAVY = "1C2333"
NAVY_MOTTLE = "27324A"
CREAM = "F3E6CB"
CREAM_MOTTLE = "E2CFA9"
CREAM_DIM = "CBB894"
MAGENTA = "E63A81"
NEON = "FF2E88"
CYAN = "3FD4DF"
AMBER = "EDA94F"
AMBER_MOTTLE = "D8913A"
TEAL = "25443F"
TEAL_MOTTLE = "2F554E"
MUTED_BOARD = "B7B1A2"
MUTED_MOTTLE = "A49D8D"
GREY_FRAME = "4C4740"
INK_GREY = "35312B"
ARCADE_BLACK = "0C0B16"
ARCADE_MOTTLE = "161427"

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
    """Flat emission material, optionally broken up by a soft noise mottle
    so painted boards read as weathered rather than as a solid swatch."""
    key = f"{name}|{base}|{mottle}|{amount}|{freq}|{detail}|{seed}|{strength}"
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


# --------------------------------------------------------------------------- #
# geometry
# --------------------------------------------------------------------------- #

def link(ob):
    bpy.context.collection.objects.link(ob)
    return ob


def rounded_rect_points(width, height, radius, segments=8):
    """Counter-clockwise outline of a rounded rectangle centred on the origin."""
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


def face_object(name, points, z, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([(p[0], p[1], z) for p in points], [], [list(range(len(points)))])
    mesh.validate()
    mesh.update()
    ob = link(bpy.data.objects.new(name, mesh))
    if material:
        mesh.materials.append(material)
    return ob


def board(name, width, height, z, material, radius=0.0, cx=0.0, cy=0.0):
    pts = rounded_rect_points(width, height, radius)
    ob = face_object(name, pts, z, material)
    ob.location = (cx, cy, 0.0)
    return ob


def disc(name, radius, z, material, cx=0.0, cy=0.0, segments=40):
    pts = [(radius * math.cos(2 * math.pi * i / segments),
            radius * math.sin(2 * math.pi * i / segments)) for i in range(segments)]
    ob = face_object(name, pts, z, material)
    ob.location = (cx, cy, 0.0)
    return ob


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


def text(name, body, colour, font_key="display", tracking=1.05, z=0.02,
         target_w=None, target_h=None, cx=0.0, cy=0.0, mottle=None,
         material_name=None):
    """Build a text object, convert it to a mesh, fit it to a width and/or
    height budget, then centre it on its own ink bounding box."""
    cu = bpy.data.curves.new(name=f"{name}Curve", type="FONT")
    cu.body = body
    cu.font = font(font_key)
    cu.align_x = "CENTER"
    cu.align_y = "CENTER"
    cu.space_character = tracking
    cu.resolution_u = 24
    cu.materials.append(emission_material(material_name or f"M_{name}", colour, mottle))

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
    """World-space ink box of a built object."""
    x0, y0, x1, y1 = _mesh_bbox(ob)
    ox, oy = ob.location.x, ob.location.y
    return (ox + x0, oy + y0, ox + x1, oy + y1)


def row(items, cy, gap, z):
    """Lay text objects out left-to-right around cx=0, vertically centred."""
    widths = [measure(o)[2] - measure(o)[0] for o in items]
    total = sum(widths) + gap * (len(items) - 1)
    x = -total / 2.0
    for ob, w in zip(items, widths):
        x0, y0, x1, y1 = measure(ob)
        # keep the existing optical centre of each part
        centre_y = (y0 + y1) / 2.0
        ob.location = (ob.location.x + (x + w / 2.0 - (x0 + x1) / 2.0),
                       ob.location.y + (cy - centre_y), z)
        x += w + gap
    bpy.context.view_layer.update()


# --------------------------------------------------------------------------- #
# render rig
# --------------------------------------------------------------------------- #

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _MATERIALS.clear()
    _FONTS.clear()


def render(name, width_px, height_px, world_width, glare=None):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 96
    scene.cycles.use_adaptive_sampling = False
    scene.cycles.use_denoising = False
    scene.cycles.filter_width = 1.6
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

    cam_data = bpy.data.cameras.new("SignCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = world_width
    cam_data.clip_start = 0.1
    cam_data.clip_end = 50.0
    cam = link(bpy.data.objects.new("SignCam", cam_data))
    cam.location = (0.0, 0.0, 10.0)
    scene.camera = cam

    if glare:
        scene.use_nodes = True
        tree = scene.node_tree
        tree.nodes.clear()
        layers = tree.nodes.new("CompositorNodeRLayers")
        glow = tree.nodes.new("CompositorNodeGlare")
        glow.glare_type = "FOG_GLOW"
        glow.quality = "HIGH"
        glow.threshold = glare.get("threshold", 0.55)
        glow.size = glare.get("size", 8)
        glow.mix = glare.get("mix", -0.35)
        comp = tree.nodes.new("CompositorNodeComposite")
        tree.links.new(layers.outputs["Image"], glow.inputs["Image"])
        tree.links.new(glow.outputs["Image"], comp.inputs["Image"])

    out_path = os.path.join(HERE, f"{name}.png")
    scene.render.filepath = out_path
    bpy.ops.render.render(write_still=True)
    print(f"WROTE {out_path}  {width_px}x{height_px}")
    return out_path


def audit(name, world_width, world_height, objects):
    """Prove every text block sits fully inside the sign frame."""
    half_w, half_h = world_width / 2.0, world_height / 2.0
    worst = 9.9
    for ob in objects:
        x0, y0, x1, y1 = measure(ob)
        margin = min(half_w - abs(x0), half_w - abs(x1), half_h - abs(y0), half_h - abs(y1))
        worst = min(worst, margin)
        flag = "OK " if margin > 0.02 else "!! "
        print(f"  {flag}{ob.name:<26} x[{x0:+.3f},{x1:+.3f}] y[{y0:+.3f},{y1:+.3f}] "
              f"margin={margin:+.3f}u")
    print(f"AUDIT {name}: tightest text margin {worst:+.3f} world units")
    if worst <= 0.0:
        raise SystemExit(f"AUDIT FAIL {name}: text leaves the frame")
    return worst


# --------------------------------------------------------------------------- #
# sign builds   (world unit = full board width / ortho width)
# --------------------------------------------------------------------------- #

def sign_facade():
    """2048x384 shopfront fascia: dark navy board, cream frame, magenta accent."""
    W, H = 8.0, 1.5
    board("Facade_frame", W + 0.04, H + 0.04, 0.000, emission_material("M_CreamFrame", CREAM))
    board("Facade_board", W - 0.12, H - 0.12, 0.002,
          emission_material("M_NavyBoard", NAVY, NAVY_MOTTLE, amount=0.5, freq=(2.4, 2.4)), radius=0.05)

    title = text("Facade_title", "LAUNDRY LEGENDS", CREAM, "display", tracking=1.08,
                 z=0.02, target_w=7.05, target_h=0.70, cy=0.235,
                 mottle=CREAM_MOTTLE, material_name="M_CreamInk")
    board("Facade_rule", 2.60, 0.030, 0.012,
          emission_material("M_Magenta", MAGENTA), cy=-0.225)
    left = text("Facade_sub_a", "LAUNDROMAT", CREAM_DIM, "sans_narrow", tracking=1.34,
                z=0.02, target_h=0.150)
    dot = disc("Facade_sub_dot", 0.022, 0.02, emission_material("M_Magenta", MAGENTA))
    right = text("Facade_sub_b", "OPEN 7–22", MAGENTA, "sans_narrow", tracking=1.30,
                 z=0.02, target_h=0.150)
    row([left, dot, right], -0.470, 0.13, 0.02)

    for sx in (-1, 1):
        for sy in (-1, 1):
            disc(f"Facade_rivet_{sx}{sy}", 0.026, 0.014,
                 emission_material("M_Rivet", "3A4A6B"), cx=sx * 3.72, cy=sy * 0.575)
    audit("sign_facade", W, H, [title, left, right])
    return render("sign_facade", 2048, 384, W)


def sign_dropoff():
    """1024x256 counter sign: cream board, navy frame, maximum contrast."""
    W, H = 4.0, 1.0
    board("Dropoff_frame", W + 0.04, H + 0.04, 0.000, emission_material("M_NavyFrame", NAVY))
    board("Dropoff_board", W - 0.14, H - 0.14, 0.002,
          emission_material("M_CreamBoard", CREAM, CREAM_MOTTLE, amount=0.55, freq=(3.0, 3.0)),
          radius=0.03)
    title = text("Dropoff_title", "DROP OFF", NAVY, "display", tracking=1.22,
                 z=0.02, target_w=3.20, target_h=0.46, cy=0.11)
    board("Dropoff_rule", 1.55, 0.028, 0.012, emission_material("M_Magenta", MAGENTA), cy=-0.288)
    audit("sign_dropoff", W, H, [title])
    return render("sign_dropoff", 1024, 256, W)


def sign_machines():
    """2048x256 wall band: WASH · DRY · FOLD in cream on faded teal."""
    W, H = 8.0, 1.0
    board("Machines_frame", W + 0.04, H + 0.04, 0.000, emission_material("M_CreamFrame", CREAM))
    board("Machines_board", W - 0.10, H - 0.10, 0.002,
          emission_material("M_TealBoard", TEAL, TEAL_MOTTLE, amount=0.5, freq=(2.6, 2.2)),
          radius=0.02)

    words = [text(f"Machines_w{i}", word, CREAM, "display", tracking=1.10, z=0.02,
                  target_h=0.54, target_w=2.30, mottle=CREAM_MOTTLE,
                  material_name="M_CreamInk")
             for i, word in enumerate(("WASH", "DRY", "FOLD"))]
    dots = [disc(f"Machines_dot{i}", 0.050, 0.02, emission_material("M_Magenta", MAGENTA))
            for i in range(2)]
    row([words[0], dots[0], words[1], dots[1], words[2]], 0.0, 0.58, 0.02)
    audit("sign_machines", W, H, words)
    return render("sign_machines", 2048, 256, W)


def sign_open():
    """512x256 hanging door sign: warm amber board, navy type."""
    W, H = 2.0, 1.0
    board("Open_frame", W + 0.04, H + 0.04, 0.000, emission_material("M_NavyFrame", NAVY))
    board("Open_board", W - 0.13, H - 0.13, 0.002,
          emission_material("M_AmberBoard", AMBER, AMBER_MOTTLE, amount=0.5, freq=(3.6, 3.2)),
          radius=0.05)
    headline = text("Open_title", "OPEN", NAVY, "display", tracking=1.10, z=0.02,
                    target_w=1.34, target_h=0.37, cy=0.1775)
    board("Open_rule", 0.66, 0.024, 0.012, emission_material("M_NavyRule", NAVY), cy=-0.092)
    hours = text("Open_hours", "7–22", NAVY, "display", tracking=1.16, z=0.02,
                 target_h=0.186, cy=-0.257)
    for sx in (-1, 1):
        disc(f"Open_rivet_{sx}", 0.030, 0.014,
             emission_material("M_RivetDark", "25304A"), cx=sx * 0.72, cy=0.395)
    audit("sign_open", W, H, [headline, hours])
    return render("sign_open", 512, 256, W)


def sign_staff():
    """512x256 service-door sign: muted, low-key, deliberately uninviting."""
    W, H = 2.0, 1.0
    board("Staff_frame", W + 0.04, H + 0.04, 0.000, emission_material("M_GreyFrame", GREY_FRAME))
    board("Staff_board", W - 0.14, H - 0.14, 0.002,
          emission_material("M_MutedBoard", MUTED_BOARD, MUTED_MOTTLE, amount=0.5, freq=(3.2, 3.2)),
          radius=0.03)
    title = text("Staff_title", "STAFF ONLY", INK_GREY, "display", tracking=1.10,
                 z=0.02, target_w=1.26, target_h=0.30, cy=0.176)
    board("Staff_rule", 0.66, 0.020, 0.012, emission_material("M_InkGrey", INK_GREY), cy=-0.093)
    note = text("Staff_note", "NO ENTRY", INK_GREY, "sans_narrow", tracking=1.45,
                z=0.02, target_h=0.115, cy=-0.2695)
    audit("sign_staff", W, H, [title, note])
    return render("sign_staff", 512, 256, W)


def sign_arcade():
    """1024x256 neon sign for the hidden arcade: magenta tube, cyan keyline."""
    W, H = 4.0, 1.0
    board("Arcade_base", W + 0.04, H + 0.04, 0.000,
          emission_material("M_ArcadeBlack", ARCADE_BLACK, ARCADE_MOTTLE, amount=0.5, freq=(2.2, 2.2)))
    board("Arcade_cyan_line", W - 0.11, H - 0.11, 0.002, emission_material("M_Cyan", CYAN), radius=0.03)
    board("Arcade_inner", W - 0.17, H - 0.17, 0.004,
          emission_material("M_ArcadeFace", ARCADE_BLACK, ARCADE_MOTTLE, amount=0.4, freq=(2.2, 2.2)),
          radius=0.02)
    title = text("Arcade_title", "ARCADE", NEON, "display", tracking=1.30, z=0.02,
                 target_w=2.85, target_h=0.45, cy=0.075)
    board("Arcade_bar", 1.55, 0.026, 0.012, emission_material("M_Cyan", CYAN), cy=-0.315)
    audit("sign_arcade", W, H, [title])
    return render("sign_arcade", 1024, 256, W, glare={"threshold": 0.32, "size": 9, "mix": -0.15})


SIGNS = {
    "sign_facade": sign_facade,
    "sign_dropoff": sign_dropoff,
    "sign_machines": sign_machines,
    "sign_open": sign_open,
    "sign_staff": sign_staff,
    "sign_arcade": sign_arcade,
}


def main():
    only = arg_value("--only")
    todo = [only] if only else list(SIGNS)
    for name in todo:
        if name not in SIGNS:
            raise SystemExit(f"unknown sign {name}; pick from {sorted(SIGNS)}")
        print(f"=== BUILD {name} ===")
        reset_scene()
        SIGNS[name]()


main()
