"""Build low-poly Bali side-street props for Laundry Legends.

Run (from the repository root):

  & "C:\\Users\\Asus\\AppData\\Local\\Programs\\BlenderFoundation\\Blender-4.5.15\\blender-4.5.15-windows-x64\\blender.exe" -b -P "ArtSource\\Blender\\create_street_props.py"

Outputs, one .blend plus one .fbx plus one 800x600 preview PNG per prop:

  ArtSource/Blender/StreetLamp.blend   -> ArtSource/Exports/StreetLamp.fbx    StreetLamp.png
  ArtSource/Blender/Bench.blend       -> ArtSource/Exports/Bench.fbx         Bench.png
  ArtSource/Blender/PottedPlant.blend -> ArtSource/Exports/PottedPlant.fbx   PottedPlant.png
  ArtSource/Blender/StackChair.blend  -> ArtSource/Exports/StackChair.fbx    StackChair.png
  ArtSource/Blender/CurbStrip.blend   -> ArtSource/Exports/CurbStrip.fbx     CurbStrip.png
  ArtSource/Blender/RoadMarking.blend -> ArtSource/Exports/RoadMarking.fbx   RoadMarking.png

Conventions (matches create_laundry_props.py / create_appliance.py):
  - Metres, 1 Blender unit = 1 m, Z up in Blender.
  - Pivot at the base centre: object origin at world (0, 0, 0), min Z = 0.
  - One joined mesh per prop, several material slots, flat shading.
  - FBX exported with the same axis/scale options as the earlier props.

Every prop is checked before export: triangle budget, pivot height, and
non-manifold edges. The same numbers are re-measured from the exported FBX by
re-importing it into a clean scene, so the report is not taken on trust.

The process exit code is only 0 when every check passed; the script also prints
"RESULT: PASS" or "RESULT: FAIL (n)" as a second, greppable gate.
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

TAU = math.tau
DEG = math.radians

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_DIR = os.path.join(PROJECT_ROOT, "ArtSource", "Blender")
EXPORT_DIR = os.path.join(PROJECT_ROOT, "ArtSource", "Exports")

TRI_BUDGET = 800
PREVIEW_SIZE = (800, 600)

FAILURES = []


# --------------------------------------------------------------------------
# checks
# --------------------------------------------------------------------------

def check(condition, message):
    if condition:
        print(f"[check] PASS  {message}")
    else:
        print(f"[check] FAIL  {message}")
        FAILURES.append(message)
    return bool(condition)


# --------------------------------------------------------------------------
# scene helpers
# --------------------------------------------------------------------------

def purge_orphans():
    """Drop datablocks left behind by the previous prop so names stay clean."""
    for collection in (
        bpy.data.meshes,
        bpy.data.materials,
        bpy.data.lights,
        bpy.data.cameras,
        bpy.data.curves,
        bpy.data.images,
        bpy.data.worlds,
    ):
        for block in list(collection):
            if block.users == 0:
                try:
                    collection.remove(block)
                except RuntimeError:
                    pass


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    purge_orphans()


# --------------------------------------------------------------------------
# materials
# --------------------------------------------------------------------------

def srgb_to_linear(channel):
    if channel <= 0.04045:
        return channel / 12.92
    return ((channel + 0.055) / 1.055) ** 2.4


def srgb(r, g, b):
    """Author colours in sRGB (what a colour picker shows), store them linear.

    Base Color in Blender, and the FBX -> Unity Standard albedo, are both
    linear values, so an sRGB intent has to be converted or every prop renders
    several stops too pale.
    """
    return (srgb_to_linear(r), srgb_to_linear(g), srgb_to_linear(b))


def make_material(name, color, roughness, metallic=0.0, emission=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    mat.diffuse_color = (*color, 1.0)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is None:
        for node in mat.node_tree.nodes:
            if node.type == "BSDF_PRINCIPLED":
                bsdf = node
                break
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
        if emission > 0.0:
            # Blender 4.x renamed the socket; fall back for safety.
            socket = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
            if socket is not None:
                socket.default_value = (*color, 1.0)
            strength = bsdf.inputs.get("Emission Strength")
            if strength is not None:
                strength.default_value = emission
    return mat


# --------------------------------------------------------------------------
# geometry helpers
# --------------------------------------------------------------------------

def create_mesh(name, verts, faces, material=None):
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.validate(verbose=False)

    bm = bmesh.new()
    bm.from_mesh(mesh)
    if bm.faces:
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    if material is not None:
        obj.data.materials.append(material)
    return obj


def loft(name, rings, material, cap_start=True, cap_end=True):
    """Skin a list of equal-length vertex rings into a closed tube."""
    sides = len(rings[0])
    verts = [v for ring in rings for v in ring]
    faces = []
    for i in range(len(rings) - 1):
        a, b = i * sides, (i + 1) * sides
        for j in range(sides):
            k = (j + 1) % sides
            faces.append((a + j, a + k, b + k, b + j))
    if cap_start:
        faces.append(tuple(range(sides - 1, -1, -1)))
    if cap_end:
        base = (len(rings) - 1) * sides
        faces.append(tuple(range(base + sides - 1, base - 1, -1)))
    return create_mesh(name, verts, faces, material)


def ring_z(cx, cy, z, radius, sides, phase=0.0):
    return [
        (cx + radius * math.cos(phase + TAU * i / sides),
         cy + radius * math.sin(phase + TAU * i / sides),
         z)
        for i in range(sides)
    ]


def square_ring(cx, cy, z, half):
    """Axis-aligned square ring, 4 points, counter-clockwise seen from +Z."""
    return [
        (cx - half, cy - half, z),
        (cx + half, cy - half, z),
        (cx + half, cy + half, z),
        (cx - half, cy + half, z),
    ]


def chamfered_square_ring(cx, cy, z, half, chamfer):
    """Octagon: a square with the four corners cut, for cast-concrete shapes."""
    c = half * chamfer
    pts = [
        (half, half - c), (half - c, half), (-(half - c), half), (-half, half - c),
        (-half, -(half - c)), (-(half - c), -half), (half - c, -half), (half, -(half - c)),
    ]
    return [(cx + x, cy + y, z) for x, y in pts]


def add_box(name, size, location, rotation=(0.0, 0.0, 0.0), material=None):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if material is not None:
        obj.data.materials.append(material)
    return obj


def extrude_profile_x(name, profile_yz, x0, x1, material):
    """Extrude a closed 2D profile given in the YZ plane along the X axis."""
    rings = [[(x, y, z) for (y, z) in profile_yz] for x in (x0, x1)]
    return loft(name, rings, material)


def arc_tube(name, center_y, center_z, radius, ang0_deg, ang1_deg,
             tube_r0, tube_r1, segments, sides, material):
    """Sweep a circular cross-section along an arc in the YZ plane.

    Angle 0 is straight up from the arc centre, 90 deg points towards -Y, so the
    arc rises out of a vertical post and bends over the street.
    """
    rings = []
    for i in range(segments + 1):
        t = i / segments
        a = DEG(ang0_deg + (ang1_deg - ang0_deg) * t)
        cy = center_y + radius * math.cos(a)
        cz = center_z + radius * math.sin(a)
        tr = tube_r0 + (tube_r1 - tube_r0) * t
        # In-plane normal, perpendicular to the tangent and pointing at the arc centre.
        ny, nz = -math.cos(a), -math.sin(a)
        ring = []
        for j in range(sides):
            phi = TAU * j / sides
            s = tr * math.sin(phi)
            ring.append((tr * math.cos(phi), cy + s * ny, cz + s * nz))
        rings.append(ring)
    return loft(name, rings, material, cap_start=True, cap_end=True)


def build_leaf(name, sections, thickness, material):
    """A flat leaf plane with thickness, a folded midrib and a tapered outline.

    `sections` is a list of (y, half_width, z, fold) sampled base -> tip. Each
    section contributes three top and three bottom vertices, so the surface is a
    quad strip either side of the midrib and the whole thing stays a closed,
    manifold solid rather than a single-sided plane.
    """
    verts = []
    top_idx, bot_idx = [], []
    for (y, half_w, z, fold) in sections:
        top_idx.append(len(verts))
        verts.extend([(-half_w, y, z), (0.0, y, z + fold), (half_w, y, z)])
    for (y, half_w, z, fold) in sections:
        bot_idx.append(len(verts))
        verts.extend([
            (-half_w, y, z - thickness),
            (0.0, y, z + fold - thickness),
            (half_w, y, z - thickness),
        ])

    faces = []
    last = len(sections) - 1
    for i in range(last):
        a, b = top_idx[i], top_idx[i + 1]
        faces.append((a, a + 1, b + 1, b))
        faces.append((a + 1, a + 2, b + 2, b + 1))
        a, b = bot_idx[i], bot_idx[i + 1]
        faces.append((a, a + 1, b + 1, b))
        faces.append((a + 1, a + 2, b + 2, b + 1))
        fa, fb = top_idx[i], top_idx[i + 1]
        ga, gb = bot_idx[i], bot_idx[i + 1]
        faces.append((fa, fb, gb, ga))
        faces.append((fa + 2, fb + 2, gb + 2, ga + 2))

    t0, b0 = top_idx[0], bot_idx[0]
    faces.append((t0, t0 + 1, t0 + 2, b0 + 2, b0 + 1, b0))
    tn, bn = top_idx[last], bot_idx[last]
    faces.append((tn, tn + 1, tn + 2, bn + 2, bn + 1, bn))

    return create_mesh(name, verts, faces, material)


def bevel(obj, width, segments=1):
    modifier = obj.modifiers.new(name="Bevel", type="BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    modifier.angle_limit = DEG(35.0)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


# --------------------------------------------------------------------------
# assembly
# --------------------------------------------------------------------------

def join_objects(parts, name):
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    joined = bpy.context.active_object
    joined.name = name
    joined.data.name = name + "Mesh"
    # Bake everything so the object origin sits exactly on world (0, 0, 0)
    # and the mesh holds world-space metres. That is the pivot contract.
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for poly in joined.data.polygons:
        poly.use_smooth = False
    return joined


def triangle_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def non_manifold_edges(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    count = sum(1 for edge in bm.edges if not edge.is_manifold)
    bm.free()
    return count


def bounds(obj):
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    lo = Vector((min(p.x for p in corners), min(p.y for p in corners), min(p.z for p in corners)))
    hi = Vector((max(p.x for p in corners), max(p.y for p in corners), max(p.z for p in corners)))
    return lo, hi


# --------------------------------------------------------------------------
# 1. street lamp
# --------------------------------------------------------------------------

def build_street_lamp():
    concrete = make_material("MAT_StreetLampConcrete", srgb(0.62, 0.61, 0.58), 0.90)
    metal = make_material("MAT_StreetLampMetal", srgb(0.26, 0.28, 0.26), 0.45, 0.35)
    glass = make_material("MAT_StreetLampGlass", srgb(1.00, 0.85, 0.55), 0.30, 0.0, emission=0.8)

    parts = []

    # Cast concrete plinth: 0.34 m square at the ground, tapering slightly.
    parts.append(loft("Plinth", [
        chamfered_square_ring(0.0, 0.0, 0.000, 0.170, 0.30),
        chamfered_square_ring(0.0, 0.0, 0.180, 0.155, 0.30),
        chamfered_square_ring(0.0, 0.0, 0.320, 0.136, 0.30),
    ], concrete))

    # Cast collar where the steel post enters the concrete.
    parts.append(loft("Collar", [
        ring_z(0.0, 0.0, 0.300, 0.104, 10),
        ring_z(0.0, 0.0, 0.400, 0.086, 10),
    ], metal))

    # Post: a 3.6 m tapered steel tube, 0.15 m at the foot, 0.11 m at the top.
    parts.append(loft("Post", [
        ring_z(0.0, 0.0, 0.360, 0.075, 10),
        ring_z(0.0, 0.0, 4.000, 0.055, 10),
    ], metal))

    # Curved arm: rises out of the post and sweeps 115 deg towards -Y,
    # ending just past horizontal so the shade hangs down at the end.
    arm_radius = 0.46
    arm_end_deg = 115.0
    parts.append(arc_tube(
        "Arm",
        center_y=-arm_radius,
        center_z=4.000,
        radius=arm_radius,
        ang0_deg=0.0,
        ang1_deg=arm_end_deg,
        tube_r0=0.050,
        tube_r1=0.040,
        segments=8,
        sides=8,
        material=metal,
    ))

    end_y = -arm_radius + arm_radius * math.cos(DEG(arm_end_deg))
    end_z = 4.000 + arm_radius * math.sin(DEG(arm_end_deg))

    # Hanging bell shade below the arm tip.
    parts.append(loft("Shade", [
        ring_z(0.0, end_y, end_z + 0.030, 0.056, 12),
        ring_z(0.0, end_y, end_z - 0.060, 0.120, 12),
        ring_z(0.0, end_y, end_z - 0.240, 0.235, 12),
    ], metal))

    # Warm lens, inset just inside the shade mouth.
    parts.append(loft("Lens", [
        ring_z(0.0, end_y, end_z - 0.248, 0.216, 12),
        ring_z(0.0, end_y, end_z - 0.236, 0.216, 12),
    ], glass))

    return join_objects(parts, "StreetLamp")


# --------------------------------------------------------------------------
# 2. wooden bench
# --------------------------------------------------------------------------

def build_bench():
    wood = make_material("MAT_BenchWood", srgb(0.56, 0.37, 0.21), 0.72)
    frame = make_material("MAT_BenchFrame", srgb(0.25, 0.26, 0.25), 0.50, 0.45)

    wood_parts = []
    steel_parts = []

    # Slatted seat, 1.60 m long, top face at 0.450 m.
    for i, y in enumerate((-0.165, -0.055, 0.055, 0.165)):
        wood_parts.append(add_box(
            f"SeatSlat{i}", (1.600, 0.085, 0.032), (0.0, y, 0.434), material=wood,
        ))

    # Back rake: 9.46 deg off vertical, rising from the seat at y = 0.205.
    # The back slats are shorter than the seat so they finish flush with the
    # back posts instead of sticking out past the frame.
    back_angle = math.atan2(0.070, 0.420)
    dy, dz = math.sin(back_angle), math.cos(back_angle)
    for i, t in enumerate((0.11, 0.235, 0.36)):
        wood_parts.append(add_box(
            f"BackSlat{i}",
            (1.460, 0.028, 0.090),
            (0.0, 0.205 + t * dy, 0.440 + t * dz),
            rotation=(-back_angle, 0.0, 0.0),
            material=wood,
        ))

    # Steel under-seat rails.
    for side, y in (("Front", -0.150), ("Rear", 0.150)):
        steel_parts.append(add_box(
            f"Rail{side}", (1.500, 0.032, 0.045), (0.0, y, 0.392), material=frame,
        ))

    # Legs and side stretchers.
    for sx, x in (("L", -0.640), ("R", 0.640)):
        for sy, y in (("F", -0.175), ("B", 0.175)):
            steel_parts.append(add_box(
                f"Leg{sx}{sy}", (0.042, 0.042, 0.420), (x, y, 0.210), material=frame,
            ))
        steel_parts.append(add_box(
            f"Stretcher{sx}", (0.036, 0.350, 0.036), (x, 0.0, 0.140), material=frame,
        ))
        steel_parts.append(add_box(
            f"BackPost{sx}",
            (0.040, 0.040, 0.4126),
            (x, 0.205 + 0.2 * dy, 0.440 + 0.2 * dz),
            rotation=(-back_angle, 0.0, 0.0),
            material=frame,
        ))

    # Round the timber edges only; the steel stays crisp.
    merged_wood = join_objects(wood_parts, "BenchWood")
    bevel(merged_wood, 0.004, segments=1)

    return join_objects([merged_wood] + steel_parts, "Bench")


# --------------------------------------------------------------------------
# 3. potted plant
# --------------------------------------------------------------------------

def build_potted_plant():
    terracotta = make_material("MAT_PlantPot", srgb(0.67, 0.37, 0.25), 0.85)
    soil = make_material("MAT_PlantSoil", srgb(0.23, 0.18, 0.14), 1.00)
    stem_mat = make_material("MAT_PlantStem", srgb(0.42, 0.47, 0.24), 0.80)
    leaf_mat = make_material("MAT_PlantLeaf", srgb(0.24, 0.47, 0.22), 0.65)

    parts = []

    # Terracotta pot: 0.45 m tall, 0.39 m across the rim, 0.22 m at the foot.
    parts.append(loft("Pot", [
        ring_z(0.0, 0.0, 0.000, 0.112, 12),
        ring_z(0.0, 0.0, 0.022, 0.126, 12),
        ring_z(0.0, 0.0, 0.372, 0.170, 12),
        ring_z(0.0, 0.0, 0.392, 0.196, 12),
        ring_z(0.0, 0.0, 0.450, 0.196, 12),
    ], terracotta))

    # Soil mound, proud of the rim by ~1.5 cm.
    parts.append(loft("Soil", [
        ring_z(0.0, 0.0, 0.430, 0.174, 12),
        ring_z(0.0, 0.0, 0.452, 0.170, 12),
        ring_z(0.0, 0.0, 0.466, 0.138, 12),
    ], soil))

    # Stem.
    parts.append(loft("Stem", [
        ring_z(0.0, 0.0, 0.440, 0.030, 6),
        ring_z(0.0, 0.0, 0.640, 0.022, 6),
        ring_z(0.0, 0.0, 0.860, 0.014, 6),
    ], stem_mat))

    # Broad leaves: flat planes with a folded midrib, fanning out and drooping.
    sections = [
        (0.000, 0.018, 0.000, 0.004),
        (0.100, 0.062, 0.026, 0.013),
        (0.240, 0.098, 0.042, 0.015),
        (0.380, 0.082, 0.026, 0.011),
        (0.500, 0.012, -0.026, 0.004),
    ]
    leaves = [
        # (yaw deg, base z, pitch deg, scale)
        (6.0, 0.585, 38.0, 0.94),
        (48.0, 0.640, 34.0, 1.00),
        (92.0, 0.690, 30.0, 0.97),
        (136.0, 0.730, 35.0, 1.03),
        (180.0, 0.765, 29.0, 0.95),
        (224.0, 0.795, 33.0, 1.01),
        (270.0, 0.824, 31.0, 0.96),
        (316.0, 0.852, 30.0, 0.92),
    ]
    for i, (yaw, base_z, pitch, scale) in enumerate(leaves):
        leaf = build_leaf(
            f"Leaf{i}",
            [(y * scale, w * scale, z * scale, f * scale) for (y, w, z, f) in sections],
            thickness=0.012,
            material=leaf_mat,
        )
        leaf.location = (0.012 * math.cos(DEG(yaw)), 0.012 * math.sin(DEG(yaw)), base_z)
        leaf.rotation_euler = (DEG(pitch), 0.0, DEG(yaw))
        parts.append(leaf)

    return join_objects(parts, "PottedPlant")


# --------------------------------------------------------------------------
# 4. plastic stack chair
# --------------------------------------------------------------------------

def build_stack_chair():
    plastic = make_material("MAT_StackChairPlastic", srgb(0.74, 0.23, 0.18), 0.45)

    parts = []

    # Seat: a moulded slab, wider at the front than the back, top face at 0.450 m.
    parts.append(loft("Seat", [
        [(-0.250, -0.225, 0.405), (0.250, -0.225, 0.405),
         (0.220, 0.225, 0.405), (-0.220, 0.225, 0.405)],
        [(-0.270, -0.225, 0.450), (0.270, -0.225, 0.450),
         (0.238, 0.225, 0.450), (-0.238, 0.225, 0.450)],
    ], plastic))

    # Four legs, set close to the seat edge so the chair does not look tippy.
    for sx, x in (("L", -0.245), ("R", 0.245)):
        for sy, y in (("F", -0.195), ("B", 0.195)):
            parts.append(loft(f"Leg{sx}{sy}", [
                square_ring(x, y, 0.000, 0.0155),
                square_ring(x, y, 0.405, 0.0195),
            ], plastic))

    # Raked back: 8 deg off vertical.
    back_angle = DEG(8.0)
    dy, dz = math.sin(back_angle), math.cos(back_angle)
    for sx, x in (("L", -0.238), ("R", 0.238)):
        parts.append(add_box(
            f"BackPost{sx}", (0.036, 0.036, 0.400),
            (x, 0.205 + 0.2 * dy, 0.440 + 0.2 * dz),
            rotation=(-back_angle, 0.0, 0.0),
            material=plastic,
        ))

    for i, t in enumerate((0.085, 0.200, 0.315)):
        parts.append(add_box(
            f"BackSlat{i}", (0.452, 0.026, 0.082),
            (0.0, 0.205 + t * dy, 0.440 + t * dz),
            rotation=(-back_angle, 0.0, 0.0),
            material=plastic,
        ))

    joined = join_objects(parts, "StackChair")
    bevel(joined, 0.009, segments=1)
    return joined


# --------------------------------------------------------------------------
# 5a. curb strip
# --------------------------------------------------------------------------

def build_curb_strip():
    concrete = make_material("MAT_CurbConcrete", srgb(0.57, 0.57, 0.54), 0.92)

    # Cross-section: flat bottom, vertical face, chamfered road-side top edge.
    profile = [
        (-0.110, 0.000),
        (0.110, 0.000),
        (0.110, 0.132),
        (0.086, 0.150),
        (-0.110, 0.150),
    ]

    segment_length = 0.980
    gap = 0.015
    total = 4 * segment_length + 3 * gap
    x = -total / 2.0
    parts = []
    for i in range(4):
        parts.append(extrude_profile_x(f"Curb{i}", profile, x, x + segment_length, concrete))
        x += segment_length + gap

    return join_objects(parts, "CurbStrip")


# --------------------------------------------------------------------------
# 5b. road marking
# --------------------------------------------------------------------------

def build_road_marking():
    paint = make_material("MAT_RoadPaint", srgb(0.88, 0.88, 0.83), 0.60)

    dash_length = 0.700
    gap = 0.300
    count = 4
    total = count * dash_length + (count - 1) * gap
    x = -total / 2.0
    parts = []
    for i in range(count):
        parts.append(add_box(
            f"Dash{i}",
            (dash_length, 0.150, 0.010),
            (x + dash_length / 2.0, 0.0, 0.005),
            material=paint,
        ))
        x += dash_length + gap

    return join_objects(parts, "RoadMarking")


# --------------------------------------------------------------------------
# export
# --------------------------------------------------------------------------

def save_and_export(obj, name):
    blend_path = os.path.join(BLEND_DIR, name + ".blend")
    fbx_path = os.path.join(EXPORT_DIR, name + ".fbx")
    os.makedirs(BLEND_DIR, exist_ok=True)
    os.makedirs(EXPORT_DIR, exist_ok=True)

    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        bake_space_transform=False,
        add_leaf_bones=False,
    )
    print(f"[export] {fbx_path}")
    return fbx_path


# --------------------------------------------------------------------------
# preview render
# --------------------------------------------------------------------------

def fit_distance(corners, center, look, tan_h, tan_v):
    """Smallest camera distance along -look that keeps every corner in frame."""
    world_up = Vector((0.0, 0.0, 1.0))
    right = look.cross(world_up)
    if right.length < 1e-6:
        right = Vector((1.0, 0.0, 0.0))
    right.normalize()
    up = right.cross(look).normalized()

    need = 0.0
    for p in corners:
        v = p - center
        depth = v.dot(look)
        need = max(
            need,
            abs(v.dot(right)) / tan_h - depth,
            abs(v.dot(up)) / tan_v - depth,
        )
    return need


def render_preview(obj, png_path, elevation=0.45, azimuth=(0.80, -1.05), margin=1.10):
    scene = bpy.context.scene

    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    lo = Vector((min(p.x for p in corners), min(p.y for p in corners), min(p.z for p in corners)))
    hi = Vector((max(p.x for p in corners), max(p.y for p in corners), max(p.z for p in corners)))
    center = (lo + hi) * 0.5
    radius = (hi - lo).length * 0.5

    # Ground plane, dropped a hair so it never z-fights the prop's base.
    bpy.ops.mesh.primitive_plane_add(size=max(24.0, radius * 24.0), location=(0.0, 0.0, -0.006))
    ground = bpy.context.active_object
    ground.name = "PreviewGround"
    ground_mat = make_material("MAT_PreviewGround", srgb(0.38, 0.39, 0.37), 0.95)
    ground.data.materials.append(ground_mat)

    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.film_transparent = False
    scene.render.resolution_x = PREVIEW_SIZE[0]
    scene.render.resolution_y = PREVIEW_SIZE[1]
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    try:
        scene.eevee.taa_render_samples = 48
    except AttributeError:
        pass

    world = bpy.data.worlds.new("PreviewWorld")
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (*srgb(0.60, 0.68, 0.80), 1.0)
    bg.inputs[1].default_value = 0.55
    scene.world = world

    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 2.4
    sun_data.angle = DEG(6.0)
    sun = bpy.data.objects.new("Sun", sun_data)
    sun.rotation_euler = Vector((0.5, 0.7, -1.0)).to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)

    # Fill is sized by distance so its irradiance stays constant across props
    # of very different scales; a fixed wattage blew out the small ones.
    fill_dist = max(2.0, radius * 3.0)
    fill_data = bpy.data.lights.new("Fill", "AREA")
    fill_data.energy = 0.45 * 4.0 * math.pi * fill_dist * fill_dist
    fill_data.size = max(1.5, radius * 2.0)
    fill = bpy.data.objects.new("Fill", fill_data)
    fill.location = center + Vector((-1.2, -1.4, 1.0)).normalized() * fill_dist
    fill.rotation_euler = (center - fill.location).to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(fill)

    # Camera framing: fit the real bounding box to both axes of the frame.
    aspect = PREVIEW_SIZE[0] / PREVIEW_SIZE[1]
    lens, sensor = 50.0, 36.0
    tan_h = (sensor * 0.5) / lens
    tan_v = tan_h / aspect

    direction = Vector((azimuth[0], azimuth[1], elevation)).normalized()
    look = -direction
    distance = fit_distance(corners, center, look, tan_h, tan_v) * margin

    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.lens = lens
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam

    cam.location = center + direction * distance
    cam.rotation_euler = look.to_track_quat("-Z", "Y").to_euler()

    scene.render.filepath = png_path
    bpy.ops.render.render(write_still=True)
    print(f"[render] {png_path}")


# --------------------------------------------------------------------------
# re-import verification
# --------------------------------------------------------------------------

def verify_export(name, expected_height_tolerance=0.02):
    fbx_path = os.path.join(EXPORT_DIR, name + ".fbx")
    check(os.path.isfile(fbx_path), f"{name}: FBX written")

    reset_scene()
    bpy.ops.import_scene.fbx(filepath=fbx_path)
    bpy.context.view_layer.update()

    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    check(len(meshes) >= 1, f"{name}: re-import produced a mesh object")

    tris = 0
    total_nm = 0
    welded_nm = 0
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for obj in meshes:
        evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        tris += sum(len(p.vertices) - 2 for p in mesh.polygons)
        evaluated.to_mesh_clear()

        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bm.transform(obj.matrix_world)
        total_nm += sum(1 for e in bm.edges if not e.is_manifold)
        bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-5)
        welded_nm += sum(1 for e in bm.edges if not e.is_manifold)
        bm.free()

        for corner in obj.bound_box:
            p = obj.matrix_world @ Vector(corner)
            lo = Vector((min(lo.x, p.x), min(lo.y, p.y), min(lo.z, p.z)))
            hi = Vector((max(hi.x, p.x), max(hi.y, p.y), max(hi.z, p.z)))

        mats = [m.name for m in obj.data.materials if m]
        print(f"[verify] {name}: materials {mats}")

    size = hi - lo
    print(f"[verify] {name}: tris={tris} dims={size.x:.4f}x{size.y:.4f}x{size.z:.4f} "
          f"minz={lo.z:.4f} nonmanifold={welded_nm} (raw {total_nm})")

    check(tris <= TRI_BUDGET, f"{name}: {tris} triangles <= {TRI_BUDGET}")
    check(abs(lo.z) <= 0.002, f"{name}: base sits on z=0 (min z {lo.z:.4f})")
    check(welded_nm == 0, f"{name}: 0 non-manifold edges (got {welded_nm})")

    return {
        "tris": tris,
        "dims": (size.x, size.y, size.z),
        "min_z": lo.z,
        "non_manifold": welded_nm,
    }


# --------------------------------------------------------------------------
# main
# --------------------------------------------------------------------------

PROPS = [
    # (name, builder, camera elevation, camera azimuth)
    ("StreetLamp", build_street_lamp, 0.16, (0.95, -1.05)),
    ("Bench", build_bench, 0.40, (0.95, -1.15)),
    ("PottedPlant", build_potted_plant, 0.30, (0.90, -1.05)),
    ("StackChair", build_stack_chair, 0.36, (0.95, -1.05)),
    ("CurbStrip", build_curb_strip, 0.80, (0.55, -1.00)),
    ("RoadMarking", build_road_marking, 1.15, (0.35, -1.00)),
]


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    summary = {}

    for name, builder, elevation, azimuth in PROPS:
        print(f"\n=== building {name} ===")
        reset_scene()
        obj = builder()

        tris = triangle_count(obj)
        nm = non_manifold_edges(obj)
        lo, hi = bounds(obj)
        size = hi - lo
        print(f"[build] {name}: tris={tris} nonmanifold={nm} "
              f"dims={size.x:.4f}x{size.y:.4f}x{size.z:.4f} minz={lo.z:.4f}")

        check(tris <= TRI_BUDGET, f"{name}: authored {tris} triangles <= {TRI_BUDGET}")
        check(nm == 0, f"{name}: authored mesh has 0 non-manifold edges (got {nm})")
        check(abs(lo.z) <= 0.002, f"{name}: authored base at z=0 (min z {lo.z:.3f})")
        check(obj.location.length <= 1e-6, f"{name}: object origin at world 0")

        save_and_export(obj, name)
        render_preview(obj, os.path.join(EXPORT_DIR, name + ".png"),
                       elevation=elevation, azimuth=azimuth)

        summary[name] = {"tris": tris, "dims": (size.x, size.y, size.z)}

    print("\n=== re-import verification ===")
    for name, _builder, _elev, _azi in PROPS:
        verified = verify_export(name)
        summary[name]["verified"] = verified

    print("\n=== summary ===")
    for name, _b, _e, _a in PROPS:
        v = summary[name]["verified"]
        print(f"{name:14s} tris={v['tris']:4d}  "
              f"{v['dims'][0]:.3f} x {v['dims'][1]:.3f} x {v['dims'][2]:.3f} m  "
              f"nonmanifold={v['non_manifold']}")

    print("")
    if FAILURES:
        print(f"RESULT: FAIL ({len(FAILURES)})")
        for failure in FAILURES:
            print("  - " + failure)
        sys.stdout.flush()
        sys.stderr.flush()
        os._exit(1)
    print("RESULT: PASS")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
