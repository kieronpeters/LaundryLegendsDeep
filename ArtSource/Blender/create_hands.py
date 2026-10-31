"""Build the first-person hands for Laundry Legends (carry pose).

Run:

  blender --background --python ArtSource/Blender/create_hands.py

Outputs:
  ArtSource/Blender/FirstPersonHands.blend
  ArtSource/Exports/FirstPersonHands.fbx
  ArtSource/Exports/hands_front.png
  ArtSource/Exports/hands_three_quarter.png
  ArtSource/Exports/hands_side.png
  ArtSource/Exports/hands_player_view.png

Authoring space
---------------
The mesh is authored directly in *camera space*: the world origin is the
player's eye, +Y is forward, +Z is up, +X is right. The FBX is exported with
Blender's standard Unity axes (axis_forward="-Z", axis_up="Y"), so this maps
1:1 onto a Unity camera with local position (0,0,0) and identity rotation:
Blender +Y -> Unity +Z, Blender +Z -> Unity +Y, Blender +X -> Unity +X.

Both hands therefore need no re-parent offsets at all; the placement contract
is "parent the model root at the camera with an identity local transform".
See hands.md for the full contract and the measured palm positions.

Anatomy notes (the revision behind this file)
---------------------------------------------
The previous generator resampled each finger polyline with a routine that
dropped the joint control points, so every finger collapsed to a straight
tube. It also lofted the wrist too long, kept the palm a flat paddle, and its
nails floated as white boxes. This version:

* keeps the joint control points (`resample` now emits them),
* rounds the fingertips with a two-ring dome plus a pole instead of a flat cap,
* shrinks the forearm, blends it into the wrist with a monotone radius ramp,
* gives the palm a dorsal/volar asymmetric, rounded cross-section,
* tightens the fingers so their bases overlap into webbing,
* drops the thumb next to the index and lets it curve forward,
* paints the nail into the existing distal faces (flush, no plate, no chips),
* smooth-shades the skin so lofts read as flesh rather than facets.
"""

import math
import os

import bpy
import bmesh
from mathutils import Matrix, Vector


TAU = math.tau
PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(PROJECT_ROOT, "ArtSource", "Blender", "FirstPersonHands.blend")
EXPORT_PATH = os.path.join(PROJECT_ROOT, "ArtSource", "Exports", "FirstPersonHands.fbx")
PREVIEW_DIR = os.path.join(PROJECT_ROOT, "ArtSource", "Exports")

# Unity player camera: 60-70 deg vertical FOV for framing checks (Player.prefab
# currently uses 70).
PLAYER_FOV_Y = 70.0
PLAYER_ASPECT = 16.0 / 9.0

FAILURES = []


def check(condition, message):
    status = "ok  " if condition else "FAIL"
    print(f"[{status}] {message}")
    if not condition:
        FAILURES.append(message)
    return bool(condition)


def require(condition, message):
    """Hard invariant: stop the build instead of exporting broken geometry."""
    if not condition:
        raise AssertionError(message)
    return True


# --------------------------------------------------------------------------
# colours / materials
# --------------------------------------------------------------------------

def srgb_to_linear(channel):
    if channel <= 0.04045:
        return channel / 12.92
    return ((channel + 0.055) / 1.055) ** 2.4


def srgb(r, g, b):
    """Author in sRGB, store linear (Blender Base Color and Unity albedo)."""
    return (srgb_to_linear(r), srgb_to_linear(g), srgb_to_linear(b))


def make_material(name, color, roughness):
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
        bsdf.inputs["Metallic"].default_value = 0.0
    return mat


# --------------------------------------------------------------------------
# mesh builder
# --------------------------------------------------------------------------

class MeshBuilder:
    """Accumulates verts/faces with a per-face material slot."""

    def __init__(self):
        self.verts = []
        self.faces = []
        self.face_mats = []

    def add_points(self, points):
        start = len(self.verts)
        self.verts.extend(tuple(p) for p in points)
        return list(range(start, start + len(points)))

    def add_ring_bridge(self, ring_a, ring_b, mat=0, sides=None):
        """Bridge two equal-length rings.

        `mat` is either one slot index or a list with one slot per column.
        Returns the face indices so callers can repaint columns later.
        """
        n = len(ring_a)
        require(len(ring_b) == n, f"ring bridge length mismatch {n} != {len(ring_b)}")
        if sides is not None:
            require(n == sides, f"ring bridge has {n} verts, expected {sides}")
        mats = mat if isinstance(mat, (list, tuple)) else [mat] * n
        require(len(mats) == n, f"per-column material list has {len(mats)} != {n}")
        first = len(self.faces)
        for i in range(n):
            j = (i + 1) % n
            self.faces.append((ring_a[i], ring_a[j], ring_b[j], ring_b[i]))
            self.face_mats.append(mats[i])
        return list(range(first, first + n))

    def add_cap(self, ring, mat=0):
        self.faces.append(tuple(ring))
        self.face_mats.append(mat)

    def add_fan(self, ring, apex_index, mat=0, reverse=False):
        n = len(ring)
        for i in range(n):
            j = (i + 1) % n
            tri = (ring[i], ring[j], apex_index)
            if reverse:
                tri = (ring[j], ring[i], apex_index)
            self.faces.append(tri)
            self.face_mats.append(mat)

    def to_object(self, name, xform, materials):
        mesh = bpy.data.meshes.new(name + "Mesh")
        points = [xform @ Vector(v) for v in self.verts]
        mesh.from_pydata([tuple(p) for p in points], [], self.faces)
        for poly_mat, poly in zip(self.face_mats, mesh.polygons):
            poly.material_index = poly_mat
            # Skin is smooth-shaded so the loft reads as flesh; the nail slot
            # stays flat so the painted nail plate keeps a crisp edge.
            poly.use_smooth = poly_mat == 0

        bm = bmesh.new()
        bm.from_mesh(mesh)
        if bm.faces:
            bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(mesh)
        bm.free()
        mesh.update()
        mesh.validate(verbose=False)

        obj = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        for material in materials:
            obj.data.materials.append(material)
        return obj


# --------------------------------------------------------------------------
# rings, frames, resampling
# --------------------------------------------------------------------------

def superellipse_ring(center, half_x, half_z_dorsal, half_z_volar, sides,
                      exponent=2.2):
    """Rounded cross-section in the XZ plane.

    The dorsal (+Z) and volar (-Z) half-thicknesses differ so the palm can be
    fleshy on the palm side and flatter across the back of the hand.
    """
    cx, cy, cz = center
    points = []
    for i in range(sides):
        angle = TAU * i / sides
        ca, sa = math.cos(angle), math.sin(angle)
        ux = math.copysign(abs(ca) ** (2.0 / exponent), ca)
        uz = math.copysign(abs(sa) ** (2.0 / exponent), sa)
        half_z = half_z_dorsal if uz >= 0.0 else half_z_volar
        points.append(Vector((cx + ux * half_x, cy, cz + uz * half_z)))
    return points


def tube_frames(points, seed=Vector((1.0, 0.0, 0.0))):
    """Tangents plus parallel-transported side axes for a polyline."""
    pts = [Vector(p) for p in points]
    tangents = []
    for i in range(len(pts)):
        if i == 0:
            t = pts[1] - pts[0]
        elif i == len(pts) - 1:
            t = pts[-1] - pts[-2]
        else:
            t = pts[i + 1] - pts[i - 1]
        if t.length < 1e-9:
            t = Vector((0.0, 1.0, 0.0))
        tangents.append(t.normalized())

    first = tangents[0]
    u = seed - first * seed.dot(first)
    if u.length < 1e-6:
        u = Vector((0.0, 0.0, 1.0)) - first * first.z
    u.normalize()
    sides = [u]
    for i in range(1, len(pts)):
        t = tangents[i]
        prev = sides[-1]
        proj = prev - t * prev.dot(t)
        if proj.length < 1e-6:
            proj = Vector((0.0, 0.0, 1.0)) - t * t.z
        proj.normalize()
        sides.append(proj)
    ups = [t.cross(u).normalized() for t, u in zip(tangents, sides)]
    return pts, tangents, sides, ups


def ellipse_ring(center, u_axis, v_axis, radius_u, radius_v, sides, phase=0.0):
    points = []
    for i in range(sides):
        angle = phase + TAU * i / sides
        points.append(
            Vector(center)
            + u_axis * (math.cos(angle) * radius_u)
            + v_axis * (math.sin(angle) * radius_v)
        )
    return points


def resample(points, per_segment=1):
    """Insert `per_segment` extra points per segment and KEEP the joints.

    The previous version dropped every interior control point, which straight-
    ened the finger chains into pipes. The joints are the anatomy; they stay.
    """
    require(len(points) >= 2, "resample needs at least two points")
    out = [Vector(points[0])]
    for a, b in zip(points[:-1], points[1:]):
        a, b = Vector(a), Vector(b)
        for k in range(1, per_segment + 1):
            out.append(a.lerp(b, k / (per_segment + 1.0)))
        out.append(Vector(b))
    return out


# --------------------------------------------------------------------------
# skeleton numbers
# --------------------------------------------------------------------------
# Hand-local frame: +X = thumb side, +Y = finger direction, +Z = dorsal
# (back of the hand). The palm faces -Z. Left hand as authored; the right hand
# is the same mesh reflected through X.
#
# Palm / forearm loft rings. One smooth monotone ramp wrist -> palm:
# (y, x_center, z_center, half_x, half_z_dorsal, half_z_volar, exponent)
PALM_RINGS = [
    (-0.0480, -0.0050, -0.0125, 0.0296, 0.0222, 0.0232, 2.05),  # forearm cut
    (-0.0360, -0.0037, -0.0094, 0.0284, 0.0210, 0.0219, 2.05),
    (-0.0245, -0.0023, -0.0062, 0.0268, 0.0194, 0.0202, 2.04),
    (-0.0140, -0.0012, -0.0032, 0.0257, 0.0182, 0.0189, 2.02),  # wrist crease
    (-0.0020, -0.0003, -0.0006, 0.0255, 0.0179, 0.0187, 2.03),
    (0.0090, 0.0005, 0.0002, 0.0272, 0.0180, 0.0195, 2.08),
    (0.0200, 0.0010, 0.0004, 0.0298, 0.0182, 0.0204, 2.15),     # thenar begins
    (0.0310, 0.0013, 0.0003, 0.0326, 0.0183, 0.0212, 2.20),
    (0.0420, 0.0014, 0.0000, 0.0352, 0.0182, 0.0214, 2.23),
    (0.0530, 0.0013, -0.0004, 0.0370, 0.0179, 0.0207, 2.25),
    (0.0640, 0.0010, -0.0008, 0.0381, 0.0174, 0.0196, 2.25),
    (0.0740, 0.0007, -0.0012, 0.0386, 0.0167, 0.0183, 2.25),
    (0.0825, 0.0004, -0.0015, 0.0385, 0.0158, 0.0169, 2.24),    # knuckle line
    (0.0890, 0.0002, -0.0018, 0.0368, 0.0146, 0.0153, 2.21),
    (0.0945, 0.0001, -0.0021, 0.0322, 0.0125, 0.0130, 2.16),
    (0.0985, 0.0000, -0.0024, 0.0242, 0.0092, 0.0095, 2.09),
]
PALM_DISTAL_POLE = Vector((0.0, 0.1012, -0.0027))
PALM_SIDES = 12
PALM_EXPONENT = 2.2

# Fingers: (name, x, mcp_y, splay rad, (proximal, middle, distal), base radius,
# z offset, curl scale). Spacing 19.2 mm with overlapping base rings, so the
# bases merge into webbing at the knuckle line instead of reading as pipes.
FINGERS = [
    ("Index", 0.0288, 0.0870, -0.055, (0.042, 0.025, 0.020), 0.0092, -0.0016, 1.00),
    ("Middle", 0.0096, 0.0900, 0.000, (0.046, 0.028, 0.021), 0.0096, -0.0012, 0.94),
    ("Ring", -0.0096, 0.0875, 0.055, (0.043, 0.026, 0.020), 0.0090, -0.0014, 1.06),
    ("Pinky", -0.0288, 0.0825, 0.125, (0.033, 0.021, 0.017), 0.0078, -0.0010, 1.16),
]
# Flexion at MCP, PIP, DIP. Positive => curl toward the palm (-Z).
FINGER_CURL = (0.24, 0.38, 0.30)
# One entry per loft ring (8 rings = base, mid-prox, PIP, mid-mid, DIP,
# mid-distal, near-tip, tip). The list length is asserted against the ring
# count in add_digit(). Joints swell, the segments between them pinch.
FINGER_PROFILE = (1.10, 0.96, 1.06, 0.92, 1.00, 0.86, 0.76, 0.58)
DIGIT_SIDES = 8
TIP_DOME = ((0.30, 0.78), (0.58, 0.48))   # (advance in tip radii, radius scale)
TIP_POLE_ADVANCE = 0.72

# Thumb: the metacarpal starts inside the thenar mass and the digit leaves the
# palm through its radial edge, so it does not hang under the volar surface as
# a separate bulb. It curves forward and rests beside the index.
THUMB_CHAIN = [
    Vector((0.0140, 0.0225, 0.0000)),   # CMC, buried in the thenar
    Vector((0.0330, 0.0440, -0.0060)),  # MCP, at the radial edge
    Vector((0.0420, 0.0630, -0.0085)),  # IP
    Vector((0.0430, 0.0860, -0.0070)),  # tip, beside the index base
]
THUMB_PROFILE = (1.12, 1.00, 1.04, 0.92, 0.98, 0.86, 0.74, 0.56)
THUMB_BASE_RADIUS = 0.0135

# Camera-space placement of each wrist (Blender axes: +Y forward, +Z up).
# 0.30 m forward and 0.175 m down keeps the hands in the lower third of a 70
# degree view and stops the forearm from filling the frame.
WRIST_LEFT = Vector((-0.150, 0.300, -0.175))
HAND_PITCH = math.radians(28.0)     # fingers rise; tips the palm toward the camera
HAND_INWARD = math.radians(20.0)    # palms tilt toward each other
HAND_SPLAY = math.radians(3.0)      # fingers fan slightly outward


def hand_frame(pitch=HAND_PITCH, inward=HAND_INWARD, splay=HAND_SPLAY):
    """Return (forward, dorsal normal) for the canonical left hand."""
    forward = Vector((
        math.sin(splay),
        math.cos(splay) * math.cos(pitch),
        math.cos(splay) * math.sin(pitch),
    )).normalized()
    normal = Vector((0.0, -math.sin(pitch), math.cos(pitch)))
    normal = (Matrix.Rotation(inward, 3, forward) @ normal).normalized()
    return forward, normal


def hand_basis(forward, normal):
    """Local X = Y x Z (thumb side for the left hand), Y = fingers, Z = dorsal."""
    y = Vector(forward).normalized()
    z = Vector(normal).normalized()
    x = y.cross(z).normalized()
    z = x.cross(y).normalized()
    return Matrix(
        (
            (x.x, y.x, z.x, 0.0),
            (x.y, y.y, z.y, 0.0),
            (x.z, y.z, z.z, 0.0),
            (0.0, 0.0, 0.0, 1.0),
        )
    )


def mirror_x(vector):
    return Vector((-vector.x, vector.y, vector.z))


def finger_chain(x, mcp_y, splay, lengths, z0, curl_scale=1.0):
    """Joint polyline for one finger: MCP, PIP, DIP, tip."""
    direction = Matrix.Rotation(splay, 3, "Z") @ Vector((0.0, 1.0, 0.0))
    points = [Vector((x, mcp_y, z0))]
    curl = 0.0
    for length, joint in zip(lengths, FINGER_CURL):
        curl += joint * curl_scale
        # Rotation about -X flexes toward the palm (-Z); +X would extend.
        step = (Matrix.Rotation(-curl, 3, "X") @ direction).normalized()
        points.append(points[-1] + step * length)
    return points


# --------------------------------------------------------------------------
# digits
# --------------------------------------------------------------------------

def add_digit(builder, points, profile, base_radius, mat_skin,
              sides=DIGIT_SIDES):
    """Loft one digit, ending in a rounded dome instead of a flat cap.

    Returns a record with the *profile* rings only (joint rings + tip ring),
    so nail painting always indexes a list whose length equals len(profile).
    """
    centers = resample(points, per_segment=1)
    require(len(centers) + 1 == len(profile),
            f"resampled chain has {len(centers)} joints but profile has "
            f"{len(profile)} entries; they must differ by exactly 1")

    tip_radius = profile[-2] * base_radius
    tangent_tip = (centers[-1] - centers[-2]).normalized()
    centers.append(centers[-1] + tangent_tip * tip_radius * 0.55)

    pts, tangents, us, vs = tube_frames(centers)

    records = []
    for i, center in enumerate(pts):
        r = base_radius * profile[i]
        radius_u, radius_v = r * 1.06, r * 0.90
        ring_points = ellipse_ring(center, us[i], vs[i], radius_u, radius_v, sides)
        records.append({
            "center": Vector(center),
            "u": Vector(us[i]),
            "v": Vector(vs[i]),
            "tangent": Vector(tangents[i]),
            "radii": (radius_u, radius_v),
            "points": ring_points,
            "indices": builder.add_points(ring_points),
        })

    require(len(records) == len(profile),
            f"{len(records)} rings built for a {len(profile)}-entry profile")

    bridge_faces = []
    for a, b in zip(records[:-1], records[1:]):
        bridge_faces.append(builder.add_ring_bridge(a["indices"], b["indices"], mat_skin))
    builder.add_cap(records[0]["indices"], mat_skin)

    # Rounded fingertip: two shrinking rings plus a pole.
    last = records[-1]
    dome_rings = []
    for advance, scale in TIP_DOME:
        center = last["center"] + last["tangent"] * (last["radii"][1] * advance)
        ring_points = ellipse_ring(
            center, last["u"], last["v"],
            last["radii"][0] * scale, last["radii"][1] * scale, sides,
        )
        dome_rings.append({"indices": builder.add_points(ring_points)})
    pole = last["center"] + last["tangent"] * (last["radii"][1] * TIP_POLE_ADVANCE)
    pole_index = builder.add_points([pole])[0]

    prev = last["indices"]
    for ring in dome_rings:
        builder.add_ring_bridge(prev, ring["indices"], mat_skin)
        prev = ring["indices"]
    builder.add_fan(prev, pole_index, mat_skin)

    require(all(len(rec["indices"]) == sides for rec in records),
            "every profile ring must have `sides` vertices")
    require(all(len(faces) == sides for faces in bridge_faces),
            "every bridge must have `sides` faces")

    return {
        "rings": records,
        "centers": [rec["center"] for rec in records],
        "us": [rec["u"] for rec in records],
        "vs": [rec["v"] for rec in records],
        "radii": [rec["radii"] for rec in records],
        "bridge_faces": bridge_faces,
        "sides": sides,
    }


def add_nail(builder, digit, mat_nail, from_ring=5, to_ring=7):
    """Paint the dorsal distal faces as the nail plate.

    This replaces the old floating box: the nail is now the existing skin
    faces of the distal phalanx, so it is flush by construction and cannot
    read as a white chip. The skin and nail albedos stay within a few percent.
    """
    rings = digit["rings"]
    us, radii, centers = digit["us"], digit["radii"], digit["centers"]
    require(len(rings) == len(us) == len(radii) == len(centers),
            f"add_nail ring lists disagree: rings={len(rings)} us={len(us)} "
            f"radii={len(radii)} centres={len(centers)}")
    require(0 <= from_ring < to_ring < len(rings),
            f"nail ring range {from_ring}..{to_ring} outside 0..{len(rings) - 1}")

    sides = digit["sides"]
    reference = rings[from_ring]
    dorsal = Vector((0.0, 0.0, 1.0))          # hand-local back-of-hand axis
    dorsal = dorsal - reference["tangent"] * dorsal.dot(reference["tangent"])
    if dorsal.length < 1e-6:
        dorsal = -reference["v"]
    dorsal.normalize()

    angle = math.atan2(dorsal.dot(reference["v"]), dorsal.dot(reference["u"]))
    dorsal_index = int(round((angle % TAU) / TAU * sides)) % sides
    columns = {(dorsal_index - 1) % sides, dorsal_index}

    painted = 0
    for bridge in range(from_ring, to_ring):
        faces = digit["bridge_faces"][bridge]
        require(len(faces) == sides, "bridge face list out of sync with ring count")
        for column in columns:
            builder.face_mats[faces[column]] = mat_nail
            painted += 1
    require(painted > 0, "nail painted no faces")
    return painted


# --------------------------------------------------------------------------
# palm
# --------------------------------------------------------------------------

def build_palm(builder, mat_skin):
    """Loft forearm -> wrist -> palm, then dome the palm over the knuckles."""
    ring_sets = []
    for (y, cx, cz, half_x, half_z_d, half_z_v, exponent) in PALM_RINGS:
        points = superellipse_ring(
            (cx, y, cz), half_x, half_z_d, half_z_v, PALM_SIDES, exponent
        )
        ring_sets.append(builder.add_points(points))

    for a, b in zip(ring_sets[:-1], ring_sets[1:]):
        builder.add_ring_bridge(a, b, mat_skin, PALM_SIDES)

    # Rounded forearm stump (usually below the frame, never a flat disc).
    first = Vector((PALM_RINGS[0][1], PALM_RINGS[0][0], PALM_RINGS[0][2]))
    stump_center = first + Vector((-0.0035, -0.0045, -0.0025))
    stump = superellipse_ring(
        tuple(stump_center), PALM_RINGS[0][3] * 0.62, PALM_RINGS[0][4] * 0.62,
        PALM_RINGS[0][5] * 0.62, PALM_SIDES, PALM_RINGS[0][6],
    )
    stump_index = builder.add_points(stump)
    builder.add_ring_bridge(ring_sets[0], stump_index, mat_skin, PALM_SIDES)
    stump_pole = builder.add_points([
        stump_center + Vector((-0.0025, -0.0040, -0.0015))
    ])[0]
    builder.add_fan(stump_index, stump_pole, mat_skin, reverse=True)

    # Rounded distal edge over the knuckles.
    last = PALM_RINGS[-1]
    edge = superellipse_ring(
        (last[1], last[0] + 0.0012, last[2] - 0.0004),
        last[3] * 0.55, last[4] * 0.55, last[5] * 0.55, PALM_SIDES, 2.02,
    )
    edge_index = builder.add_points(edge)
    builder.add_ring_bridge(ring_sets[-1], edge_index, mat_skin, PALM_SIDES)
    pole_index = builder.add_points([PALM_DISTAL_POLE])[0]
    builder.add_fan(edge_index, pole_index, mat_skin, reverse=True)

    return ring_sets


def build_hand(name, matrix, materials):
    skin, nail = 0, 1
    builder = MeshBuilder()

    build_palm(builder, skin)

    for _, x, mcp_y, splay, lengths, radius, z0, curl_scale in FINGERS:
        points = finger_chain(x, mcp_y, splay, lengths, z0, curl_scale)
        digit = add_digit(builder, points, FINGER_PROFILE, radius, skin)
        add_nail(builder, digit, nail, from_ring=5, to_ring=7)

    thumb = add_digit(builder, THUMB_CHAIN, THUMB_PROFILE, THUMB_BASE_RADIUS, skin)
    add_nail(builder, thumb, nail, from_ring=5, to_ring=7)

    return builder.to_object(name, matrix, materials)


def hand_matrix(side):
    forward, normal = hand_frame()
    basis = hand_basis(forward, normal)
    if side == "L":
        wrist = WRIST_LEFT
        reflection = Matrix.Identity(4)
    else:
        # A right hand is the mirror image of the left; a pure rotation cannot
        # produce it, so the placement carries an X reflection and the mesh
        # winding is fixed by the normal recalculation in to_object().
        wrist = mirror_x(WRIST_LEFT)
        reflection = Matrix.Diagonal((-1.0, 1.0, 1.0, 1.0))
    return Matrix.Translation(wrist) @ reflection @ basis


# --------------------------------------------------------------------------
# measurements
# --------------------------------------------------------------------------

def to_unity(point):
    """Blender camera space -> Unity camera space (x, y, z) = (x, z, y)."""
    return Vector((point.x, point.z, point.y))


def palm_center_local():
    """Centroid of the palm-region ring centres (wrist crease to knuckles)."""
    rows = [row for row in PALM_RINGS if 0.006 <= row[0] <= 0.094]
    centre = Vector((0.0, 0.0, 0.0))
    for (y, cx, cz, _hx, _hd, _hv, _e) in rows:
        centre += Vector((cx, y, cz))
    return centre / len(rows)


def palm_center_world(matrix):
    return matrix @ palm_center_local()


def palm_normal_world(matrix):
    """Dorsal (back-of-hand) normal; the palm faces -normal."""
    return (matrix.to_3x3() @ Vector((0.0, 0.0, 1.0))).normalized()


def project_to_view(point, fov_y=PLAYER_FOV_Y, aspect=PLAYER_ASPECT):
    """Normalised view position: (0,0) centre, +-1 at the frame edges."""
    depth = point.y
    if depth <= 1e-6:
        return None
    half_h = math.tan(math.radians(fov_y) * 0.5) * depth
    half_w = half_h * aspect
    return (point.x / half_w, point.z / half_h)


def world_bounds(objects):
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for obj in objects:
        for corner in obj.bound_box:
            p = obj.matrix_world @ Vector(corner)
            lo = Vector((min(lo.x, p.x), min(lo.y, p.y), min(lo.z, p.z)))
            hi = Vector((max(hi.x, p.x), max(hi.y, p.y), max(hi.z, p.z)))
    return lo, hi


def triangle_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def digit_volume_distance(left, right):
    """Closest distance between sampled vertex clouds of two hands."""
    lv = [left.matrix_world @ v.co for v in left.data.vertices]
    rv = [right.matrix_world @ v.co for v in right.data.vertices]
    best = 1e9
    for a in lv:
        for b in rv:
            d = (a - b).length
            if d < best:
                best = d
    return best


# --------------------------------------------------------------------------
# preview renders
# --------------------------------------------------------------------------

def setup_studio():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    try:
        scene.eevee.taa_render_samples = 64
    except AttributeError:
        pass

    world = bpy.data.worlds.new("HandsPreviewWorld")
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (*srgb(0.28, 0.30, 0.33), 1.0)
    bg.inputs[1].default_value = 0.45
    scene.world = world

    def add_area(name, energy, size, location, target=(0.0, 0.06, -0.10)):
        light = bpy.data.lights.new(name, "AREA")
        light.energy = energy
        light.size = size
        obj = bpy.data.objects.new(name, light)
        obj.location = location
        obj.rotation_euler = (
            Vector(target) - obj.location
        ).to_track_quat("-Z", "Y").to_euler()
        scene.collection.objects.link(obj)

    add_area("Key", 30.0, 1.2, (0.62, -0.72, 0.86))
    add_area("Fill", 11.0, 2.0, (-0.85, -0.62, 0.20))
    add_area("Rim", 18.0, 1.0, (-0.15, 0.88, 0.66), (0.0, 0.10, -0.10))


def render_view(path, location, target, lens, resolution, fov_y=None):
    scene = bpy.context.scene
    scene.render.resolution_x = resolution[0]
    scene.render.resolution_y = resolution[1]
    scene.render.resolution_percentage = 100

    cam_data = bpy.data.cameras.new("PreviewCam")
    if fov_y is not None:
        cam_data.sensor_fit = "VERTICAL"
        cam_data.angle = math.radians(fov_y)
        cam_data.clip_start = 0.01
    else:
        cam_data.lens = lens
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    cam.location = Vector(location)
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam

    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"[render] {path}")
    bpy.data.objects.remove(cam, do_unlink=True)
    bpy.data.cameras.remove(cam_data, do_unlink=True)


# --------------------------------------------------------------------------
# export / verification
# --------------------------------------------------------------------------

def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.meshes, bpy.data.materials, bpy.data.lights,
                       bpy.data.cameras, bpy.data.worlds):
        for block in list(collection):
            if block.users == 0:
                try:
                    collection.remove(block)
                except RuntimeError:
                    pass


def build():
    clear_scene()

    skin_mat = make_material("MAT_HandsSkin", srgb(0.72, 0.50, 0.40), 0.62)
    nail_mat = make_material("MAT_HandsNail", srgb(0.70, 0.49, 0.40), 0.50)
    materials = [skin_mat, nail_mat]

    left = build_hand("Hand_L", hand_matrix("L"), materials)
    right = build_hand("Hand_R", hand_matrix("R"), materials)

    root = bpy.data.objects.new("FirstPersonHands", None)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.05
    bpy.context.scene.collection.objects.link(root)
    left.parent = root
    right.parent = root

    # Reference camera documenting the placement contract inside the .blend.
    cam_data = bpy.data.cameras.new("PlayerViewFOV70_Ref")
    cam_data.sensor_fit = "VERTICAL"
    cam_data.angle = math.radians(PLAYER_FOV_Y)
    cam_data.clip_start = 0.05
    cam_data.clip_end = 100.0
    ref_cam = bpy.data.objects.new("PlayerViewFOV70_Ref", cam_data)
    bpy.context.scene.collection.objects.link(ref_cam)
    ref_cam.location = (0.0, 0.0, 0.0)
    ref_cam.rotation_euler = (math.radians(90.0), 0.0, 0.0)

    bpy.context.view_layer.update()
    return left, right, root, ref_cam, materials


def report_measurements(left, right):
    for obj in (left, right):
        tris = triangle_count(obj)
        check(700 <= tris <= 1400, f"{obj.name}: {tris} triangles within 700-1400")

    lo, hi = world_bounds([left, right])
    print(f"[bounds] both hands lo={tuple(round(v, 4) for v in lo)} "
          f"hi={tuple(round(v, 4) for v in hi)} world size={tuple(round(v, 4) for v in (hi - lo))}")

    local_centre = palm_center_local()
    print(f"[palm] local centre (hand space) = {tuple(round(v, 4) for v in local_centre)}")

    for obj, side in ((left, "L"), (right, "R")):
        m = hand_matrix(side)
        pc = palm_center_world(m)
        n = palm_normal_world(m)
        u_pc = to_unity(pc)
        u_n = to_unity(n)
        view = project_to_view(pc)
        print(f"[{obj.name}] palm centre blender={tuple(round(v, 4) for v in pc)} "
              f"unity={tuple(round(v, 4) for v in u_pc)}")
        print(f"[{obj.name}] dorsal normal blender={tuple(round(v, 3) for v in n)} "
              f"unity={tuple(round(v, 3) for v in u_n)}")
        if view:
            print(f"[{obj.name}] palm centre on 16:9 FOV{PLAYER_FOV_Y:.0f} view "
                  f"= ({view[0]:+.2f}, {view[1]:+.2f}) of half-frame")
        lo_o, hi_o = world_bounds([obj])
        print(f"[{obj.name}] world bbox {tuple(round(v, 4) for v in lo_o)} .. "
              f"{tuple(round(v, 4) for v in hi_o)} "
              f"size={tuple(round(v, 4) for v in (hi_o - lo_o))}")
        print(f"[{obj.name}] triangles={triangle_count(obj)} verts={len(obj.data.vertices)}")

    pc_l = to_unity(palm_center_world(hand_matrix("L")))
    pc_r = to_unity(palm_center_world(hand_matrix("R")))
    separation = (pc_r - pc_l).length
    print(f"[gap] palm centre separation = {separation:.4f} m")
    print(f"[gap] closest vertex-to-vertex distance between hands = "
          f"{digit_volume_distance(left, right):.4f} m")
    check(separation > 0.02, f"palm separation {separation:.3f} m leaves room for a grip")


def export_fbx(objects):
    os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
    os.makedirs(os.path.dirname(EXPORT_PATH), exist_ok=True)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print(f"[save] {BLEND_PATH}")

    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]

    bpy.ops.export_scene.fbx(
        filepath=EXPORT_PATH,
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
    print(f"[export] {EXPORT_PATH}")


def verify_roundtrip(expected):
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=EXPORT_PATH)
    bpy.context.view_layer.update()
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    names = sorted(o.name for o in meshes)
    check(len(meshes) == 2, f"re-import produced 2 meshes (got {len(meshes)}: {names})")
    check(names == ["Hand_L", "Hand_R"], f"re-imported names are Hand_L/Hand_R ({names})")

    total = 0
    counts = {}
    for obj in meshes:
        tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
        counts[obj.name] = tris
        total += tris
        print(f"[verify] {obj.name}: {tris} triangles, {len(obj.data.materials)} material slot(s)")
        lo, hi = world_bounds([obj])
        print(f"[verify] {obj.name} bbox {tuple(round(v, 4) for v in lo)} .. "
              f"{tuple(round(v, 4) for v in hi)}")
        smooth = sum(1 for p in obj.data.polygons if p.use_smooth)
        print(f"[verify] {obj.name}: {smooth}/{len(obj.data.polygons)} polygons smooth-shaded")
    for name, tris in expected.items():
        check(counts.get(name) == tris,
              f"round-trip {name} keeps {tris} triangles (got {counts.get(name)})")

    check(total == sum(expected.values()),
          f"round-trip triangle total {total} == built {sum(expected.values())}")


def main():
    left, right, root, ref_cam, materials = build()

    check(root.name == "FirstPersonHands", "root empty is named FirstPersonHands")
    check(left.parent is root and right.parent is root,
          "Hand_L and Hand_R are parented to FirstPersonHands")
    check(left.name == "Hand_L", f"left object named Hand_L (got {left.name})")
    check(right.name == "Hand_R", f"right object named Hand_R (got {right.name})")
    check([m.name for m in materials] == ["MAT_HandsSkin", "MAT_HandsNail"],
          "materials MAT_HandsSkin and MAT_HandsNail exist")

    expected = {"Hand_L": triangle_count(left), "Hand_R": triangle_count(right)}
    report_measurements(left, right)

    export_fbx([left, right])

    # Previews (the .blend is saved before the studio lights are added).
    setup_studio()
    os.makedirs(PREVIEW_DIR, exist_ok=True)

    # Compose the preview cameras from the hand's own axes so each shot shows
    # the anatomy rather than another view down the finger axis.
    forward, dorsal = hand_frame()
    axes = hand_basis(forward, dorsal).to_3x3()
    ax_x = (axes @ Vector((1.0, 0.0, 0.0))).normalized()   # thumb side (left)
    ax_z = (axes @ Vector((0.0, 0.0, 1.0))).normalized()   # back of the hand
    left_palm = palm_center_world(hand_matrix("L"))
    right_palm = palm_center_world(hand_matrix("R"))
    both = (left_palm + right_palm) * 0.5
    dorsal_avg = (ax_z + mirror_x(ax_z)).normalized()

    front_cam = both + dorsal_avg * 0.46
    render_view(
        os.path.join(PREVIEW_DIR, "hands_front.png"),
        tuple(front_cam), tuple(both), 36.0, (900, 700),
    )
    render_view(
        os.path.join(PREVIEW_DIR, "hands_three_quarter.png"),
        tuple(both + dorsal_avg * 0.34 + Vector((0.30, 0.10, 0.0))),
        tuple(both), 38.0, (900, 700),
    )
    render_view(
        os.path.join(PREVIEW_DIR, "hands_side.png"),
        tuple(left_palm + ax_x * 0.42 + ax_z * 0.08),
        tuple(left_palm + ax_x * 0.02), 50.0, (900, 700),
    )
    render_view(
        os.path.join(PREVIEW_DIR, "hands_player_view.png"),
        (0.0, 0.0, 0.0), (0.0, 1.0, 0.0), 0.0, (1280, 720),
        fov_y=PLAYER_FOV_Y,
    )
    _ = ref_cam

    verify_roundtrip(expected)

    print(f"[result] failures={len(FAILURES)}")
    for failure in FAILURES:
        print(f"  - {failure}")
    if FAILURES:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
