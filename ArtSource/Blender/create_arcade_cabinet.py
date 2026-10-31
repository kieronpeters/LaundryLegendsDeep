"""Build a low-poly 1980s upright arcade cabinet for Laundry Legends.

Run:
  blender.exe -b -P ArtSource/Blender/create_arcade_cabinet.py

Outputs:
  ArtSource/Blender/ArcadeCabinet.blend
  ArtSource/Exports/ArcadeCabinet.fbx
  ArtSource/Exports/cabinet_front.png
  ArtSource/Exports/cabinet_three_quarter.png
  ArtSource/Exports/cabinet_side.png

Authoring conventions
---------------------
* Metres. Base centre at the world origin, floor at z = 0, max height 1.83 m.
* The cabinet FRONT faces +Y in Blender. In this project's FBX/Unity
  pipeline Blender +Y becomes Unity +Z, which is the orientation the game
  needs (see `arcade_cabinet.md` for the evidence and the exact conversion).
* The screen bay leans BACK: the bezel plane's top edge is 94 mm behind its
  bottom edge, so the glass faces forward and 9.45 degrees upward.

Hierarchy
---------
  ArcadeCabinet (empty, scale 1)
    +- CabinetBody   (joined solid parts, 5 material slots)
    +- ScreenSurface (flat 0.50 x 0.40 quad, local +Z = outward normal)
    +- MarqueePanel  (flat 0.56 x 0.19 quad, local +Z = outward normal)

Everything is deterministic: fixed numbers, no randomness, no add-ons.
"""

import math
import os

import bmesh
import bpy
from mathutils import Vector


PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(PROJECT_ROOT, "ArtSource", "Blender", "ArcadeCabinet.blend")
EXPORT_PATH = os.path.join(PROJECT_ROOT, "ArtSource", "Exports", "ArcadeCabinet.fbx")
PREVIEW_DIR = os.path.join(PROJECT_ROOT, "ArtSource", "Exports")

TRIANGLE_BUDGET = 1500

# ---------------------------------------------------------------------------
# Dimensions (metres). The front of the cabinet is +Y.
# ---------------------------------------------------------------------------
HALF_WIDTH = 0.330
BACK_Y = -0.382
# Front-most point is the control-panel nose; total depth then equals 0.760.
CP_NOSE_Y = 0.378
CP_REAR_Y = 0.240
LOWER_FRONT_Y = 0.290
HEAD_FRONT_Y = 0.235
PLINTH_TOP = 0.075
TOTAL_HEIGHT = 1.830

# Screen bay: bezel front plane runs from (BEZEL_BOTTOM) to (BEZEL_TOP).
BEZEL_BOTTOM_Y, BEZEL_BOTTOM_Z = 0.240, 0.955
BEZEL_TOP_Y, BEZEL_TOP_Z = 0.146, 1.520
BEZEL_TILT = math.atan2(BEZEL_BOTTOM_Y - BEZEL_TOP_Y, BEZEL_TOP_Z - BEZEL_BOTTOM_Z)
BEZEL_NORMAL = Vector((0.0, math.cos(BEZEL_TILT), math.sin(BEZEL_TILT)))
BEZEL_UP = Vector((0.0, -math.sin(BEZEL_TILT), math.cos(BEZEL_TILT)))

SCREEN_WIDTH, SCREEN_HEIGHT = 0.500, 0.400
SCREEN_CENTER_Z = 1.240
SCREEN_PROUD = 0.002        # metres in front of the cabinet face (no z-fighting)
BEZEL_PROUD = 0.022         # bezel frame stands 22 mm proud -> 20 mm recess

MARQUEE_WIDTH, MARQUEE_HEIGHT = 0.560, 0.190
MARQUEE_CENTER_Z = 1.6725

# Side silhouette, counter-clockwise in (y, z).  Front face at +Y.
PROFILE = [
    (LOWER_FRONT_Y, PLINTH_TOP),      # A  front bottom
    (LOWER_FRONT_Y, 0.735),           # B  lower cabinet front top
    (BEZEL_BOTTOM_Y, 0.760),          # C  step back under the control panel
    (BEZEL_BOTTOM_Y, BEZEL_BOTTOM_Z), # D  bezel bottom
    (BEZEL_TOP_Y, BEZEL_TOP_Z),       # E  bezel top (leaning back)
    (HEAD_FRONT_Y, 1.545),            # F  marquee underside
    (HEAD_FRONT_Y, 1.800),            # G  marquee face top
    (0.190, TOTAL_HEIGHT),            # H  top chamfer
    (BACK_Y, TOTAL_HEIGHT),           # I  top back
    (BACK_Y, PLINTH_TOP),             # J  back bottom
]

TRIM_INSET = 0.012       # T-molding border starts 12 mm inside the edge
TRIM_WIDTH = 0.012


# ---------------------------------------------------------------------------
# Small helpers
# ---------------------------------------------------------------------------

def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.meshes, bpy.data.materials, bpy.data.lights,
                       bpy.data.cameras, bpy.data.images):
        for block in list(collection):
            if block.users == 0:
                collection.remove(block)


def set_bsdf(bsdf, key, value):
    if key in bsdf.inputs:
        bsdf.inputs[key].default_value = value


def material(name, color, roughness, metallic=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    set_bsdf(bsdf, "Base Color", (*color, 1.0))
    set_bsdf(bsdf, "Roughness", roughness)
    set_bsdf(bsdf, "Metallic", metallic)
    return mat


def mesh_object(name, verts, faces, mat):
    mesh = bpy.data.meshes.new(f"{name}Mesh")
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    mesh.validate(verbose=False)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    mesh.materials.append(mat)
    return obj


def face_normal(points):
    """Newell normal of an ordered polygon (Blender's own winding rule)."""
    normal = Vector((0.0, 0.0, 0.0))
    count = len(points)
    for index in range(count):
        current = Vector(points[index])
        following = Vector(points[(index + 1) % count])
        normal.x += (current.y - following.y) * (current.z + following.z)
        normal.y += (current.z - following.z) * (current.x + following.x)
        normal.z += (current.x - following.x) * (current.y + following.y)
    return normal.normalized()


def oriented_face(points, out_normal):
    """Return the point list wound so that its normal points along out_normal."""
    if face_normal(points).dot(Vector(out_normal)) < 0.0:
        return list(reversed(points))
    return list(points)


def recalc_outward(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def prism(name, profile, x0, x1, mat):
    """Extrude a closed (y, z) profile between x0 and x1."""
    count = len(profile)
    verts = [(x0, y, z) for (y, z) in profile] + [(x1, y, z) for (y, z) in profile]
    faces = []
    for index in range(count):
        a = index
        b = (index + 1) % count
        faces.append((a, b, b + count, a + count))
    faces.append(tuple(range(count)))                       # x0 cap
    faces.append(tuple(range(2 * count - 1, count - 1, -1)))  # x1 cap
    obj = mesh_object(name, verts, faces, mat)
    recalc_outward(obj)
    return obj


def box(name, x0, x1, y0, y1, z0, z1, mat):
    return prism(name, [(y0, z0), (y1, z0), (y1, z1), (y0, z1)], x0, x1, mat)


def cylinder(name, radius, depth, center, axis, mat, segments=12):
    """Cylinder built directly so it stays deterministic and cheap."""
    axis = Vector(axis).normalized()
    if abs(axis.z) > 0.99:
        u = Vector((1.0, 0.0, 0.0))
    else:
        u = axis.cross(Vector((0.0, 0.0, 1.0))).normalized()
    v = axis.cross(u).normalized()
    center = Vector(center)
    half = axis * (depth * 0.5)
    rings = []
    for sign in (-1.0, 1.0):
        ring = []
        for index in range(segments):
            angle = 2.0 * math.pi * index / segments
            ring.append(center + sign * half + u * (radius * math.cos(angle))
                        + v * (radius * math.sin(angle)))
        rings.append(ring)
    verts = rings[0] + rings[1]
    faces = []
    for index in range(segments):
        a = index
        b = (index + 1) % segments
        faces.append((a, b, b + segments, a + segments))
    faces.append(tuple(range(segments - 1, -1, -1)))
    faces.append(tuple(range(segments, 2 * segments)))
    obj = mesh_object(name, verts, faces, mat)
    recalc_outward(obj)
    return obj


def uv_sphere(name, radius, center, mat, segments=10, rings=5):
    """Cheap UV sphere; enough for a joystick ball at this scale."""
    center = Vector(center)
    verts = [center + Vector((0.0, 0.0, radius))]
    for ring in range(1, rings):
        phi = math.pi * ring / rings
        z = radius * math.cos(phi)
        r = radius * math.sin(phi)
        for index in range(segments):
            angle = 2.0 * math.pi * index / segments
            verts.append(center + Vector((r * math.cos(angle), r * math.sin(angle), z)))
    verts.append(center - Vector((0.0, 0.0, radius)))
    bottom = len(verts) - 1
    faces = []
    for index in range(segments):
        faces.append((0, 1 + index, 1 + (index + 1) % segments))
    for ring in range(rings - 2):
        start = 1 + ring * segments
        following = start + segments
        for index in range(segments):
            a = start + index
            b = start + (index + 1) % segments
            faces.append((a, b, following + (index + 1) % segments, following + index))
    start = 1 + (rings - 2) * segments
    for index in range(segments):
        faces.append((bottom, start + (index + 1) % segments, start + index))
    obj = mesh_object(name, verts, faces, mat)
    recalc_outward(obj)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def ring_frame(name, origin, right, up, normal, outer, inner, depth, mat):
    """Rectangular frame standing `depth` proud of a plane (bezel, marquee)."""
    uo, vo = outer
    ui, vi = inner
    origin = Vector(origin)
    right = Vector(right)
    up = Vector(up)
    normal = Vector(normal)

    def point(u, v, w):
        return origin + right * u + up * v + normal * w

    verts = [
        point(-uo, -vo, 0.0), point(uo, -vo, 0.0), point(uo, vo, 0.0), point(-uo, vo, 0.0),
        point(-uo, -vo, depth), point(uo, -vo, depth), point(uo, vo, depth), point(-uo, vo, depth),
        point(-ui, -vi, 0.0), point(ui, -vi, 0.0), point(ui, vi, 0.0), point(-ui, vi, 0.0),
        point(-ui, -vi, depth), point(ui, -vi, depth), point(ui, vi, depth), point(-ui, vi, depth),
    ]
    faces = []

    def quad(a, b, c, d, expected):
        order = [a, b, c, d]
        if face_normal([verts[i] for i in order]).dot(Vector(expected)) < 0.0:
            order.reverse()
        faces.append(tuple(order))

    # Front face ring.
    quad(4, 5, 13, 12, normal)    # bottom band
    quad(5, 6, 14, 13, normal)    # right band
    quad(6, 7, 15, 14, normal)    # top band
    quad(7, 4, 12, 15, normal)    # left band
    # Outer walls (back -> front).
    quad(0, 1, 5, 4, -up)
    quad(1, 2, 6, 5, right)
    quad(2, 3, 7, 6, up)
    quad(3, 0, 4, 7, -right)
    # Inner walls (front -> back, facing the opening).
    quad(12, 13, 9, 8, up)
    quad(13, 14, 10, 9, -right)
    quad(14, 15, 11, 10, -up)
    quad(15, 12, 8, 11, right)

    obj = mesh_object(name, verts, faces, mat)
    return obj


def flat_plane(name, width, height, location, rotation, mat):
    """Flat quad in local XY; local +Z is the outward normal."""
    hw, hh = width * 0.5, height * 0.5
    verts = [(-hw, -hh, 0.0), (hw, -hh, 0.0), (hw, hh, 0.0), (-hw, hh, 0.0)]
    obj = mesh_object(name, verts, [(0, 1, 2, 3)], mat)
    obj.location = Vector(location)
    obj.rotation_euler = rotation
    return obj


def join(objects, name):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    joined = bpy.context.active_object
    joined.name = name
    joined.data.name = f"{name}Mesh"
    return joined


def triangles(obj):
    return sum(len(poly.vertices) - 2 for poly in obj.data.polygons)


# ---------------------------------------------------------------------------
# Geometry
# ---------------------------------------------------------------------------

def inward_normals(profile):
    """Per-vertex mitered inward normal (y, z) for a CCW profile."""
    count = len(profile)
    edge_normals = []
    for index in range(count):
        current = Vector(profile[index])
        following = Vector(profile[(index + 1) % count])
        direction = (following - current).normalized()
        outward = Vector((direction.y, -direction.x))  # (dz, -dy) for CCW
        edge_normals.append(-outward)

    normals = []
    for index in range(count):
        previous = edge_normals[index - 1]
        following = edge_normals[index]
        miter = (previous + following)
        if miter.length < 1e-6:
            normals.append(previous)
            continue
        miter.normalize()
        scale = 1.0 / max(0.35, miter.dot(previous))
        normals.append(miter * scale)
    return normals


def build_body(mat_body, mat_panel):
    parts = []
    body = prism("Body", PROFILE, -HALF_WIDTH, HALF_WIDTH, mat_body)
    bevel = body.modifiers.new(name="EdgeBevel", type="BEVEL")
    bevel.width = 0.006
    bevel.segments = 1
    bevel.limit_method = "ANGLE"
    bevel.angle_limit = math.radians(35.0)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    parts.append(body)

    # Recessed kick plinth.
    parts.append(box("Plinth", -0.300, 0.300, -0.352, 0.270, 0.0, PLINTH_TOP, mat_panel))

    # Control panel wedge: nose -> sloped deck -> tucked behind the bezel step.
    cp_profile = [
        (CP_NOSE_Y, 0.725),
        (CP_NOSE_Y, 0.800),
        (CP_REAR_Y, 0.825),
        (CP_REAR_Y, 0.760),
    ]
    panel = prism("ControlPanel", cp_profile, -HALF_WIDTH, HALF_WIDTH, mat_panel)
    parts.append(panel)
    return parts, body


def build_trim(mat_trim):
    """Thin inset border standing in for T-molding on both side panels."""
    sides = []
    front_count = 8  # A .. H
    points = [Vector((y, z)) for (y, z) in PROFILE]
    miter = inward_normals(PROFILE)

    inner = []
    outer = []
    for index in range(front_count):
        inner.append(points[index] + miter[index] * TRIM_INSET)
        outer.append(points[index] + miter[index] * (TRIM_INSET + TRIM_WIDTH))

    for side, x in (("L", -HALF_WIDTH - 0.0004), ("R", HALF_WIDTH + 0.0004)):
        verts = []
        for index in range(front_count):
            verts.append(Vector((x, inner[index].x, inner[index].y)))
            verts.append(Vector((x, outer[index].x, outer[index].y)))
        faces = []
        outward = Vector((1.0 if x > 0 else -1.0, 0.0, 0.0))
        for index in range(front_count - 1):
            a = index * 2
            order = [a, a + 2, a + 3, a + 1]
            if face_normal([verts[i] for i in order]).dot(outward) < 0.0:
                order.reverse()
            faces.append(tuple(order))
        obj = mesh_object(f"Trim_{side}", verts, faces, mat_trim)
        sides.append(obj)
    return sides


def build_bezel(mat_panel, mat_screen):
    center = Vector((0.0, BEZEL_BOTTOM_Y + (SCREEN_CENTER_Z - BEZEL_BOTTOM_Z)
                     * (BEZEL_TOP_Y - BEZEL_BOTTOM_Y) / (BEZEL_TOP_Z - BEZEL_BOTTOM_Z),
                     SCREEN_CENTER_Z))
    bezel = ring_frame("Bezel", center, Vector((1.0, 0.0, 0.0)), BEZEL_UP, BEZEL_NORMAL,
                       outer=(0.300, 0.250), inner=(0.260, 0.210),
                       depth=BEZEL_PROUD, mat=mat_panel)
    screen_center = center + BEZEL_NORMAL * SCREEN_PROUD
    return bezel, center, screen_center


def build_marquee(mat_panel, mat_trim):
    origin = Vector((0.0, HEAD_FRONT_Y, MARQUEE_CENTER_Z))
    frame = ring_frame("MarqueeFrame", origin, Vector((1.0, 0.0, 0.0)),
                       Vector((0.0, 0.0, 1.0)), Vector((0.0, 1.0, 0.0)),
                       outer=(0.310, 0.245), inner=(MARQUEE_WIDTH * 0.5,
                                                     MARQUEE_HEIGHT * 0.5),
                       depth=0.008, mat=mat_trim)
    return frame, origin


def build_controls(mats):
    parts = []
    slope = (0.825 - 0.800) / (CP_REAR_Y - CP_NOSE_Y)  # dz/dy, the deck tilts back

    def deck_z(y):
        return 0.800 + (y - CP_NOSE_Y) * slope

    def deck_normal():
        direction = Vector((0.0, 1.0, slope)).normalized()
        return Vector((0.0, -direction.z, direction.y))

    normal = deck_normal()
    joystick_x, joystick_y = -0.190, 0.334
    base = Vector((joystick_x, joystick_y, deck_z(joystick_y)))
    parts.append(cylinder("JoystickBase", 0.042, 0.012, base + normal * 0.004,
                          normal, mats["coin"], segments=14))
    shaft_bottom = base + normal * 0.008
    shaft_top = base + normal * 0.070
    parts.append(cylinder("JoystickShaft", 0.011, (shaft_top - shaft_bottom).length,
                          (shaft_bottom + shaft_top) * 0.5, normal, mats["coin"], segments=10))
    parts.append(uv_sphere("JoystickBall", 0.031, base + normal * 0.086, mats["trim"]))

    for index, x in enumerate((0.105, 0.180, 0.255)):
        y = 0.300
        center = Vector((x, y, deck_z(y)))
        parts.append(cylinder(f"ButtonRing{index + 1}", 0.023, 0.010,
                              center + normal * 0.004, normal, mats["coin"], segments=12))
        parts.append(cylinder(f"ButtonCap{index + 1}", 0.017, 0.014,
                              center + normal * 0.011, normal, mats["trim"], segments=12))
    return parts


def build_coin_door(mats):
    parts = []
    front = LOWER_FRONT_Y
    center_z = 0.430
    outer_w, outer_h = 0.342, 0.424
    inner_w, inner_h = 0.298, 0.380

    # Raised frame around an inset door plate.
    parts.append(ring_frame("CoinFrame", Vector((0.0, front, center_z)),
                            Vector((1.0, 0.0, 0.0)), Vector((0.0, 0.0, 1.0)),
                            Vector((0.0, 1.0, 0.0)),
                            outer=(outer_w * 0.5, outer_h * 0.5),
                            inner=(inner_w * 0.5, inner_h * 0.5),
                            depth=0.008, mat=mats["coin"]))
    parts.append(box("CoinDoorPlate", -inner_w * 0.5, inner_w * 0.5,
                     front + 0.004, front + 0.011,
                     center_z - inner_h * 0.5, center_z + inner_h * 0.5, mats["coin"]))

    for x in (-0.075, 0.075):
        parts.append(box(f"CoinEntry_{'L' if x < 0 else 'R'}", x - 0.045, x + 0.045,
                         front + 0.011, front + 0.019,
                         0.498, 0.598, mats["coin"]))
        parts.append(box(f"CoinSlot_{'L' if x < 0 else 'R'}", x - 0.006, x + 0.006,
                         front + 0.019, front + 0.0205,
                         0.528, 0.578, mats["panel"]))
        parts.append(box(f"CoinReturn_{'L' if x < 0 else 'R'}", x - 0.040, x + 0.040,
                         front + 0.011, front + 0.013,
                         0.300, 0.352, mats["panel"]))

    parts.append(cylinder("CoinLock", 0.012, 0.014,
                          Vector((0.0, front + 0.017, 0.548)),
                          Vector((0.0, 1.0, 0.0)), mats["panel"], segments=12))
    return parts


def build_vent_slots(mat_panel):
    """Four short vent slots on the lower front sides, a period detail."""
    parts = []
    for x in (-0.285, 0.285):
        for index in range(3):
            z = 0.180 + index * 0.030
            parts.append(box(f"Vent_{x:+.2f}_{index}", x - 0.022, x + 0.022,
                             LOWER_FRONT_Y, LOWER_FRONT_Y + 0.0025,
                             z, z + 0.012, mat_panel))
    return parts


def build_pinstripe(mat_trim):
    """Thin horizontal accent across the lower front and the panel nose."""
    parts = []
    parts.append(box("Pinstripe_Lower", -0.285, 0.285,
                     LOWER_FRONT_Y, LOWER_FRONT_Y + 0.0018,
                     0.672, 0.684, mat_trim))
    parts.append(box("Pinstripe_Panel", -0.310, 0.310,
                     CP_NOSE_Y, CP_NOSE_Y + 0.0018,
                     0.786, 0.795, mat_trim))
    return parts


# ---------------------------------------------------------------------------
# Assembly, export, verification
# ---------------------------------------------------------------------------

def aim(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_plane(name, location, size, rotation, mat):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size[0], size[1], 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    return obj


def add_area_light(name, location, target, size, energy, color=(1.0, 1.0, 1.0)):
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
    floor_mat = material("MAT_StudioFloor", (0.055, 0.060, 0.070), 0.55)
    back_mat = material("MAT_StudioBack", (0.030, 0.034, 0.042), 0.7)
    add_plane("StudioFloor", (0.0, 0.0, -0.0015), (12.0, 12.0), (0.0, 0.0, 0.0), floor_mat)
    add_plane("StudioBack", (0.0, -1.60, 1.6), (12.0, 4.0),
              (math.radians(90.0), 0.0, 0.0), back_mat)

    # Key from the player's upper left, cool fill from the right,
    # a magenta kicker to pick out the trim, and a soft top.
    add_area_light("Key", (-1.9, 2.9, 3.0), (0.0, 0.0, 1.05), 1.6, 420.0, (1.0, 0.96, 0.90))
    add_area_light("Fill", (2.6, 2.2, 1.5), (0.0, 0.0, 0.95), 1.8, 130.0, (0.80, 0.88, 1.0))
    add_area_light("Kicker", (1.3, -1.4, 2.2), (0.0, 0.0, 1.30), 0.9, 220.0, (1.0, 0.45, 0.75))
    add_area_light("Top", (0.0, 0.4, 3.4), (0.0, 0.0, 1.2), 1.4, 110.0, (1.0, 1.0, 1.0))

    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    if background is not None:
        background.inputs["Color"].default_value = (0.020, 0.024, 0.034, 1.0)
        background.inputs["Strength"].default_value = 0.9


def make_camera(name, location, target, lens=50.0, ortho=None):
    data = bpy.data.cameras.new(name=name)
    data.lens = lens
    data.sensor_width = 36.0
    if ortho is not None:
        data.type = "ORTHO"
        data.ortho_scale = ortho
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj


def configure_render():
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 96
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 8
    scene.cycles.use_adaptive_sampling = True
    scene.render.resolution_x = 900
    scene.render.resolution_y = 700
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.color_depth = "8"
    scene.render.film_transparent = False
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass


def render_previews():
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    scene = bpy.context.scene
    views = [
        ("cabinet_front", (0.0, 4.30, 1.20), (0.0, 0.0, 0.95), 50.0, None),
        ("cabinet_three_quarter", (2.55, 3.35, 1.85), (0.0, 0.0, 0.95), 50.0, None),
        ("cabinet_side", (4.20, 0.0, 0.95), (0.0, 0.0, 0.95), 50.0, 2.45),
    ]
    for name, location, target, lens, ortho in views:
        camera = make_camera(f"Cam_{name}", location, target, lens, ortho)
        scene.camera = camera
        path = os.path.join(PREVIEW_DIR, f"{name}.png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        print(f"[Cabinet] Rendered {path}")


def verify_export():
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=EXPORT_PATH)
    print("[Verify] Re-imported", EXPORT_PATH)
    total = 0
    overall_min = Vector((1e9, 1e9, 1e9))
    overall_max = Vector((-1e9, -1e9, -1e9))
    for obj in bpy.context.scene.objects:
        parent = obj.parent.name if obj.parent else "-"
        if obj.type == "MESH":
            tris = triangles(obj)
            total += tris
            dims = obj.dimensions
            print(f"[Verify] {obj.name} tris={tris} parent={parent} "
                  f"loc=({obj.location.x:.4f}, {obj.location.y:.4f}, {obj.location.z:.4f}) "
                  f"dims=({dims.x:.4f}, {dims.y:.4f}, {dims.z:.4f}) "
                  f"mats={[m.name for m in obj.data.materials]}")
            for corner in obj.bound_box:
                world = obj.matrix_world @ Vector(corner)
                overall_min.x = min(overall_min.x, world.x)
                overall_min.y = min(overall_min.y, world.y)
                overall_min.z = min(overall_min.z, world.z)
                overall_max.x = max(overall_max.x, world.x)
                overall_max.y = max(overall_max.y, world.y)
                overall_max.z = max(overall_max.z, world.z)
        else:
            print(f"[Verify] {obj.name} type={obj.type} parent={parent} "
                  f"scale={tuple(round(v, 4) for v in obj.scale)}")
    size = overall_max - overall_min
    print(f"[Verify] Total triangles: {total}")
    print(f"[Verify] Bounds min=({overall_min.x:.4f}, {overall_min.y:.4f}, "
          f"{overall_min.z:.4f}) max=({overall_max.x:.4f}, {overall_max.y:.4f}, "
          f"{overall_max.z:.4f})")
    print(f"[Verify] Size: {size.x:.4f} x {size.y:.4f} x {size.z:.4f} m")


def export(root, meshes):
    os.makedirs(os.path.dirname(EXPORT_PATH), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in [root] + meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=EXPORT_PATH,
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
    print(f"[Cabinet] Exported {EXPORT_PATH}")


def main():
    clear_scene()

    mats = {
        "body": material("MAT_CabinetBody", (0.020, 0.036, 0.086), 0.52),
        "trim": material("MAT_CabinetTrim", (0.580, 0.022, 0.235), 0.38),
        "screen": material("MAT_CabinetScreen", (0.003, 0.004, 0.008), 0.065),
        "panel": material("MAT_ControlPanel", (0.013, 0.014, 0.018), 0.32, 0.05),
        "coin": material("MAT_CoinDoor", (0.050, 0.053, 0.060), 0.40, 0.85),
    }

    solid_parts, body = build_body(mats["body"], mats["panel"])
    solid_parts += build_trim(mats["trim"])
    bezel, bezel_center, screen_center = build_bezel(mats["panel"], mats["screen"])
    solid_parts.append(bezel)
    marquee_frame, marquee_center = build_marquee(mats["panel"], mats["trim"])
    solid_parts.append(marquee_frame)
    solid_parts += build_controls(mats)
    solid_parts += build_coin_door(mats)
    solid_parts += build_vent_slots(mats["panel"])
    solid_parts += build_pinstripe(mats["trim"])

    cabinet = join(solid_parts, "CabinetBody")

    screen = flat_plane("ScreenSurface", SCREEN_WIDTH, SCREEN_HEIGHT, screen_center,
                        (BEZEL_TILT - math.pi * 0.5, 0.0, 0.0), mats["screen"])
    marquee = flat_plane("MarqueePanel", MARQUEE_WIDTH, MARQUEE_HEIGHT,
                         marquee_center + Vector((0.0, 0.003, 0.0)),
                         (-math.pi * 0.5, 0.0, 0.0), mats["panel"])

    root = bpy.data.objects.new("ArcadeCabinet", None)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.12
    bpy.context.scene.collection.objects.link(root)
    for obj in (cabinet, screen, marquee):
        obj.parent = root
        obj.matrix_parent_inverse = root.matrix_world.inverted()

    # Report.
    total = triangles(cabinet) + triangles(screen) + triangles(marquee)
    print(f"[Cabinet] CabinetBody triangles: {triangles(cabinet)}")
    print(f"[Cabinet] ScreenSurface triangles: {triangles(screen)}")
    print(f"[Cabinet] MarqueePanel triangles: {triangles(marquee)}")
    print(f"[Cabinet] Total triangles: {total} (budget {TRIANGLE_BUDGET})")
    assert total <= TRIANGLE_BUDGET, f"Triangle budget exceeded: {total}"

    normal = (screen.rotation_euler.to_matrix() @ Vector((0.0, 0.0, 1.0))).normalized()
    print(f"[Cabinet] ScreenSurface blender location = "
          f"({screen.location.x:.4f}, {screen.location.y:.4f}, {screen.location.z:.4f})")
    print(f"[Cabinet] ScreenSurface blender rotation = "
          f"({math.degrees(screen.rotation_euler.x):.4f}, 0, 0) deg")
    print(f"[Cabinet] ScreenSurface blender normal = "
          f"({normal.x:.4f}, {normal.y:.4f}, {normal.z:.4f})")
    print(f"[Cabinet] ScreenSurface unity  location = "
          f"({-screen.location.x:.4f}, {screen.location.z:.4f}, {screen.location.y:.4f})")
    print(f"[Cabinet] ScreenSurface unity  normal   = "
          f"({-normal.x:.4f}, {normal.z:.4f}, {normal.y:.4f})")

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print(f"[Cabinet] Saved {BLEND_PATH}")

    export(root, [cabinet, screen, marquee])

    configure_render()
    build_studio()
    render_previews()

    verify_export()


if __name__ == "__main__":
    main()
