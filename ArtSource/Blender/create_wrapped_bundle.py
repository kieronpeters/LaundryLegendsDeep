"""Build a low-poly clear plastic-wrapped laundry bundle for Laundry Legends.

Run:
  blender.exe -b -P ArtSource/Blender/create_wrapped_bundle.py

Outputs:
  ArtSource/Blender/WrappedBundle.blend
  ArtSource/Exports/WrappedBundle.fbx
  ArtSource/Exports/bundle_front.png
  ArtSource/Exports/bundle_three_quarter.png
  ArtSource/Exports/bundle_top.png

The bundle is authored in metres with its pivot at the base centre (world
origin). It exports as one root empty named WrappedBundle with two mesh
children: BundleCloth (opaque folded clothes) and BundleFilm (thin clear
plastic film gathered into a twisted knot with two tails).

Design notes for the 2026 rework:
  * The cloth is one lofted "stack of folds" whose radius pinches at three
    fold lines, so the stacked silhouette reads through the film.
  * The film body is an open-bottomed loft (the open rim is the film pooling
    on the floor). Radially the shoulder uses a rotating 4-lobe gather so the
    cinch reads as radiating folds rather than a smooth cone.
  * The film body is smooth shaded (dense rings keep the creases) so the
    triangulated loft no longer shows large diagonal planes; the knot and
    tails stay flat shaded because small facets read as crumpled plastic.
  * The film material is a Fresnel driven Transparent/Glossy mix, which is
    what a 2 mm PVC film actually does: almost all light passes through, and
    the reflections concentrate at grazing angles. That is the reason the
    film reads as clear instead of as a pale slab.

Everything is deterministic: fixed numbers and sine-based wrinkles, no
randomness, no external addons.
"""

import math
import os

import bmesh
import bpy
from mathutils import Vector


PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(PROJECT_ROOT, "ArtSource", "Blender", "WrappedBundle.blend")
EXPORT_PATH = os.path.join(PROJECT_ROOT, "ArtSource", "Exports", "WrappedBundle.fbx")
PREVIEW_DIR = os.path.join(PROJECT_ROOT, "ArtSource", "Exports")

SEGMENTS = 18             # radial segments of the film body
CLOTH_SEGMENTS = 14       # radial segments of the cloth stack
FILM_THICKNESS = 0.002    # metres, inward from the authored outer surface
TRIANGLE_BUDGET = 1600
TRIANGLE_WARN = 1560

# Film outer profile:
#   (z, half_x, half_y, centre_x, centre_y, wobble, wobble_phase_deg,
#    gather_amp, gather_phase_deg)
# Height 0 -> 0.154 m on the body; the knot carries it to ~0.20 m.
# Widest ring is 0.152 x 0.111 half extent (~0.304 x 0.222 m).
# The three rings with a low `wobble` and a slightly smaller extent are the
# deliberate crease loops (z = 0.0095, 0.0570, 0.0890).
FILM_RINGS = [
    (0.0000, 0.124, 0.089, 0.000, 0.000, 0.026,   0.0, 0.000,  0.0),
    (0.0035, 0.140, 0.101, 0.000, 0.000, 0.016,  12.0, 0.000,  0.0),
    (0.0090, 0.148, 0.108, 0.000, 0.000, 0.011,  24.0, 0.000,  0.0),
    (0.0190, 0.151, 0.110, 0.001, 0.000, 0.014,  34.0, 0.000,  0.0),
    (0.0420, 0.153, 0.112, 0.001, 0.000, 0.017,  50.0, 0.000,  0.0),
    (0.0555, 0.146, 0.106, 0.001, 0.001, 0.010,  62.0, 0.000,  0.0),
    (0.0620, 0.153, 0.111, 0.001, 0.001, 0.017,  74.0, 0.000,  0.0),
    (0.0885, 0.145, 0.105, 0.002, 0.001, 0.009,  96.0, 0.000,  0.0),
    (0.0950, 0.150, 0.109, 0.002, 0.002, 0.018, 108.0, 0.000,  0.0),
    (0.1100, 0.134, 0.097, 0.004, 0.003, 0.020, 128.0, 0.050,  0.0),
    (0.1200, 0.116, 0.084, 0.006, 0.004, 0.024, 148.0, 0.130, 22.0),
    (0.1290, 0.090, 0.065, 0.009, 0.006, 0.026, 172.0, 0.180, 40.0),
    (0.1370, 0.063, 0.046, 0.012, 0.008, 0.028, 196.0, 0.185, 58.0),
    (0.1450, 0.036, 0.028, 0.014, 0.010, 0.024, 222.0, 0.150, 76.0),
    (0.1500, 0.023, 0.018, 0.015, 0.011, 0.016, 244.0, 0.080, 92.0),
]
# Cloth stack profile: (z, half_x, half_y, centre_x, centre_y, twist_deg).
# The rings where the extent drops and then grows again are the fold edges:
# z = 0.0560, 0.0940, 0.1200. Those three grooves are what makes the bundle
# read as folded clothes rather than as a pillow.
CLOTH_RINGS = [
    (0.0040, 0.106, 0.078, 0.000, 0.000, 0.0),
    (0.0180, 0.121, 0.090, 0.001, 0.000, 0.6),
    (0.0380, 0.124, 0.093, 0.002, 0.001, 1.2),   # fold layer 1 peak
    (0.0475, 0.110, 0.082, 0.002, 0.001, 1.6),   # fold edge 1 (pinch in)
    (0.0525, 0.118, 0.088, 0.003, 0.002, 1.9),   # fold edge 1 (pinch out)
    (0.0700, 0.120, 0.090, 0.004, 0.002, 2.4),   # fold layer 2 peak
    (0.0820, 0.107, 0.080, 0.004, 0.003, 2.8),   # fold edge 2 (pinch in)
    (0.0885, 0.113, 0.085, 0.005, 0.003, 3.1),   # fold edge 2 (pinch out)
    (0.1040, 0.112, 0.084, 0.006, 0.004, 3.6),   # fold layer 3 peak
    (0.1150, 0.096, 0.072, 0.007, 0.005, 4.0),   # top fold edge
    (0.1255, 0.062, 0.047, 0.009, 0.007, 4.5),
]

# Knot sections: (z, radius_x, radius_y, centre_x, centre_y, twist_deg, lobe_amp)
# Two lobes with a steep per-section twist reads as a twisted band of film
# rather than the faceted crystal a 3-lobe star produced at this poly count.
KNOT_SECTIONS = [
    (0.1410, 0.026, 0.021, 0.013, 0.010,   0.0, 0.10),
    (0.1470, 0.034, 0.028, 0.015, 0.011,  30.0, 0.20),
    (0.1535, 0.038, 0.031, 0.017, 0.012,  70.0, 0.24),
    (0.1605, 0.036, 0.029, 0.019, 0.013, 110.0, 0.24),
    (0.1665, 0.030, 0.024, 0.021, 0.014, 145.0, 0.20),
    (0.1735, 0.031, 0.025, 0.023, 0.016, 185.0, 0.22),
    (0.1810, 0.024, 0.019, 0.024, 0.017, 225.0, 0.16),
    (0.1890, 0.011, 0.009, 0.025, 0.018, 265.0, 0.06),
]
KNOT_SIDES = 8


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.meshes, bpy.data.materials, bpy.data.lights,
                       bpy.data.cameras, bpy.data.images):
        for block in list(collection):
            if block.users == 0:
                collection.remove(block)


def smoothstep(edge0, edge1, value):
    if edge1 == edge0:
        return 0.0
    t = (value - edge0) / (edge1 - edge0)
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def set_bsdf_input(bsdf, key, value):
    if key in bsdf.inputs:
        bsdf.inputs[key].default_value = value


def make_material(name, base_color, roughness, metallic=0.0, alpha=1.0,
                  ior=1.45, transmission=0.0, coat=0.0):
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf is None:
        return material

    set_bsdf_input(bsdf, "Base Color", (*base_color, 1.0))
    set_bsdf_input(bsdf, "Roughness", roughness)
    set_bsdf_input(bsdf, "Metallic", metallic)
    set_bsdf_input(bsdf, "Alpha", alpha)
    set_bsdf_input(bsdf, "IOR", ior)
    set_bsdf_input(bsdf, "Transmission Weight", transmission)
    set_bsdf_input(bsdf, "Coat Weight", coat)

    if alpha < 1.0:
        material.use_backface_culling = False
        try:
            material.surface_render_method = "BLENDED"
        except AttributeError:
            pass
    return material


def make_film_material(name, tint=(0.93, 0.96, 0.97)):
    """Thin clear film: transparent where it faces you, glossy at grazing angles.

    A Principled BSDF with a low Alpha multiplies the specular by the alpha, so
    it can only be milky or invisible. A Fresnel-driven Transparent/Glossy mix
    is the physically correct thin-film model: ~4% reflection head on, ~100%
    at grazing. That is what gives the bright edge lines around a cling-film
    wrapped bundle while letting the clothes behind stay readable.
    """
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    material.use_backface_culling = False
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    for node in list(nodes):
        if node.type != "OUTPUT_MATERIAL":
            nodes.remove(node)

    output = nodes.get("Material Output")
    if output is None:
        output = nodes.new("ShaderNodeOutputMaterial")
    output.location = (620, 0)

    fresnel = nodes.new("ShaderNodeFresnel")
    fresnel.location = (-420, 240)
    fresnel.inputs["IOR"].default_value = 1.52

    # A Glossy BSDF already carries its own Fresnel term, so driving the mix
    # with a Fresnel node counts the reflection twice and turns the film into
    # a pale slab at grazing angles. Halving the factor brings the film back
    # to roughly the reflectance of a real 2 mm PVC film.
    fresnel_scale = nodes.new("ShaderNodeMath")
    fresnel_scale.location = (-250, 300)
    fresnel_scale.operation = "MULTIPLY"
    fresnel_scale.inputs[1].default_value = 0.60

    glossy = nodes.new("ShaderNodeBsdfGlossy")
    glossy.location = (-20, 260)
    glossy.inputs["Color"].default_value = (1.0, 0.995, 0.985, 1.0)
    glossy.inputs["Roughness"].default_value = 0.10
    glossy.distribution = "GGX"

    transparent = nodes.new("ShaderNodeBsdfTransparent")
    transparent.location = (-20, 60)
    # A very slight cool tint: real film dims and cools what is behind it by a
    # couple of percent, and that is what stops it being literally invisible.
    transparent.inputs["Color"].default_value = (*tint, 1.0)

    mix = nodes.new("ShaderNodeMixShader")
    mix.location = (250, 120)
    links.new(fresnel.outputs["Fac"], fresnel_scale.inputs[0])
    links.new(fresnel_scale.outputs["Value"], mix.inputs["Fac"])
    links.new(transparent.outputs["BSDF"], mix.inputs[1])
    links.new(glossy.outputs["BSDF"], mix.inputs[2])
    links.new(mix.outputs["Shader"], output.inputs["Surface"])

    # Small amount of surface scatter so the shoulder gathers catch a soft
    # milky sheen where the film bunches up, like real stretch-wrap.
    # This has to stay tiny: a camera ray crosses four film surfaces (outer
    # and inner front, inner and outer back), so 7.5% diffuse per surface
    # added up to a ~30% white wash and made the film look like a pale slab.
    milky = nodes.new("ShaderNodeBsdfDiffuse")
    milky.location = (250, -180)
    milky.inputs["Color"].default_value = (0.90, 0.94, 0.96, 1.0)
    milky.inputs["Roughness"].default_value = 0.55

    scatter = nodes.new("ShaderNodeMixShader")
    scatter.location = (430, 0)
    scatter.inputs["Fac"].default_value = 0.020
    links.new(mix.outputs["Shader"], scatter.inputs[1])
    links.new(milky.outputs["BSDF"], scatter.inputs[2])
    links.new(scatter.outputs["Shader"], output.inputs["Surface"])
    return material


def link_mesh(name, verts, faces, material):
    mesh = bpy.data.meshes.new(f"{name}Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.validate(verbose=False)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    mesh.materials.append(material)
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
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return joined


def recalc_normals(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def triangulate(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.triangulate(bm, faces=bm.faces[:], quad_method="BEAUTY",
                          ngon_method="BEAUTY")
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def shade(obj, smooth):
    for polygon in obj.data.polygons:
        polygon.use_smooth = smooth


def stats(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    non_manifold = sum(1 for edge in bm.edges if len(edge.link_faces) != 2)
    triangles = sum(len(face.verts) - 2 for face in bm.faces)
    bm.free()
    return triangles, non_manifold


# ---------------------------------------------------------------------------
# Shared lofting helpers
# ---------------------------------------------------------------------------

def superellipse_xy(angle, half_x, half_y, exponent):
    """Point on a superellipse; exponent 2 is an ellipse, 3 a rounded rect."""
    cos_a, sin_a = math.cos(angle), math.sin(angle)
    power = 2.0 / exponent
    x = half_x * math.copysign(abs(cos_a) ** power, cos_a)
    y = half_y * math.copysign(abs(sin_a) ** power, sin_a)
    return x, y


def loft(name, ring_points, material, cap_bottom=True, cap_top=True):
    """Build a quad loft through a list of equally sized rings of points."""
    segments = len(ring_points[0])
    verts = [point for ring in ring_points for point in ring]
    faces = []
    for ring in range(len(ring_points) - 1):
        for index in range(segments):
            a = ring * segments + index
            b = ring * segments + (index + 1) % segments
            c = (ring + 1) * segments + (index + 1) % segments
            d = (ring + 1) * segments + index
            faces.append((a, b, c, d))
    if cap_bottom:
        faces.append(tuple(range(segments - 1, -1, -1)))
    if cap_top:
        start = (len(ring_points) - 1) * segments
        faces.append(tuple(range(start, start + segments)))
    obj = link_mesh(name, verts, faces, material)
    recalc_normals(obj)
    return obj


# ---------------------------------------------------------------------------
# Folded clothes core
# ---------------------------------------------------------------------------

def cloth_point(angle_index, ring):
    z, half_x, half_y, cx, cy, twist_deg = ring
    theta = 2.0 * math.pi * angle_index / CLOTH_SEGMENTS
    # Flatten the stack's vertical walls slightly so the fold grooves bite.
    x, y = superellipse_xy(theta, half_x, half_y, 3.0)
    twist = math.radians(twist_deg)
    cos_t, sin_t = math.cos(twist), math.sin(twist)
    return (cx + x * cos_t - y * sin_t, cy + x * sin_t + y * cos_t, z)


def build_cloth(material):
    rings = [[cloth_point(index, ring) for index in range(CLOTH_SEGMENTS)]
             for ring in CLOTH_RINGS]
    cloth = loft("BundleCloth", rings, material, cap_bottom=True, cap_top=True)
    recalc_normals(cloth)
    triangulate(cloth)
    shade(cloth, smooth=False)
    return cloth


# ---------------------------------------------------------------------------
# Plastic film
# ---------------------------------------------------------------------------

def film_point(angle_index, ring):
    z, half_x, half_y, cx, cy, wobble_amp, phase_deg, gather_amp, gather_deg = ring
    theta = 2.0 * math.pi * angle_index / SEGMENTS
    phase = math.radians(phase_deg)

    # Broad irregularity: two incommensurate low harmonics, never a clean cone.
    wobble = 1.0 + wobble_amp * (0.78 * math.sin(3.0 * theta + phase)
                                + 0.42 * math.sin(5.0 * theta + phase * 1.7 + 0.9))

    # Shoulder gathers: 4 broad lobes with a phase that creeps up the shoulder
    # so the folds spiral toward the cinch, plus a per-segment alternating
    # pleat under a 2-lobe envelope. Without the pleat, 18 segments across a
    # 5-ring shoulder smooth out into the cone this asset was criticised for;
    # with it, the shoulder puckers like gathered stretch-wrap. Sharpening the
    # cosine with |cos|**0.62 gives wide folds and tight creases.
    if gather_amp > 0.0:
        lobes = math.cos(4.0 * theta + math.radians(gather_deg))
        lobes = math.copysign(abs(lobes) ** 0.62, lobes)
        envelope = 0.45 + 0.55 * math.cos(2.0 * theta
                                          + math.radians(gather_deg) * 0.5 + 0.7)
        pleat = 1.0 if (angle_index % 2 == 0) else -1.0
        wobble *= 1.0 + gather_amp * lobes + gather_amp * 0.42 * pleat * envelope

    # Rounded rectangle at the base, near-circular at the neck.
    exponent = 2.95 - 0.95 * smoothstep(0.09, 0.15, z)
    x, y = superellipse_xy(theta, half_x, half_y, exponent)
    x *= wobble
    y *= wobble

    twist = math.radians(11.0) * (z / 0.154) ** 1.6
    cos_t, sin_t = math.cos(twist), math.sin(twist)
    return (cx + x * cos_t - y * sin_t, cy + x * sin_t + y * cos_t, z)


def build_film_body(material):
    rings = [[film_point(index, ring) for index in range(SEGMENTS)]
             for ring in FILM_RINGS]
    # Open bottom: the boundary loop becomes the rim of film pooling on the
    # floor after solidify. Capped at the neck so the knot sits on a closed bag.
    body = loft("FilmBody", rings, material, cap_bottom=False, cap_top=True)
    recalc_normals(body)

    solidify = body.modifiers.new(name="FilmThickness", type="SOLIDIFY")
    solidify.thickness = FILM_THICKNESS
    solidify.offset = -1.0
    solidify.use_even_offset = True
    solidify.use_rim = True
    solidify.use_rim_only = False
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.modifier_apply(modifier=solidify.name)
    return body


def tube_from_sections(name, sections, material):
    """Closed quad tube through 4-corner sections."""
    verts = [corner for section in sections for corner in section]
    faces = []
    count = len(sections)
    for section in range(count - 1):
        base_a = section * 4
        base_b = (section + 1) * 4
        for index in range(4):
            faces.append((base_a + index, base_a + (index + 1) % 4,
                          base_b + (index + 1) % 4, base_b + index))
    faces.append((3, 2, 1, 0))
    last = (count - 1) * 4
    faces.append((last, last + 1, last + 2, last + 3))
    obj = link_mesh(name, verts, faces, material)
    recalc_normals(obj)
    return obj


def build_knot(material):
    rings = []
    for z, radius_x, radius_y, cx, cy, twist_deg, lobe_amp in KNOT_SECTIONS:
        twist = math.radians(twist_deg)
        points = []
        for index in range(KNOT_SIDES):
            theta = 2.0 * math.pi * index / KNOT_SIDES
            # Two lobes, unsharpened: at 8 sides a 3-lobe star aliases into
            # glass-like spikes, a rotating flattened section does not.
            lobes = math.cos(2.0 * theta + twist * 2.0)
            scale = 1.0 + lobe_amp * lobes
            x = radius_x * math.cos(theta) * scale
            y = radius_y * math.sin(theta) * scale
            cos_t, sin_t = math.cos(twist), math.sin(twist)
            points.append((cx + x * cos_t - y * sin_t,
                           cy + x * sin_t + y * cos_t,
                           z))
        rings.append(points)
    knot = loft("FilmKnot", rings, material, cap_bottom=True, cap_top=True)
    recalc_normals(knot)
    return knot


def build_flap(name, points, widths, thickness, material):
    sections = []
    last_index = len(points) - 1
    for index, (point, width) in enumerate(zip(points, widths)):
        if index < last_index:
            direction = (points[index + 1] - point).normalized()
        else:
            direction = (point - points[index - 1]).normalized()
        side = direction.cross(Vector((0.0, 0.0, 1.0)))
        if side.length < 1e-6:
            side = Vector((1.0, 0.0, 0.0))
        side.normalize()
        up = side.cross(direction).normalized()
        half_side = side * (width * 0.5)
        half_up = up * (thickness * 0.5)
        sections.append([
            point - half_side - half_up,
            point + half_side - half_up,
            point + half_side + half_up,
            point - half_side + half_up,
        ])
    return tube_from_sections(name, sections, material)


def build_film(material):
    body = build_film_body(material)
    knot = build_knot(material)
    flap_a = build_flap(
        "FilmTailA",
        [Vector((0.030, 0.014, 0.158)), Vector((0.046, 0.006, 0.152)),
         Vector((0.058, -0.004, 0.145)), Vector((0.068, -0.014, 0.137)),
         Vector((0.075, -0.024, 0.128))],
        [0.026, 0.021, 0.015, 0.009, 0.004], 0.0018, material)
    flap_b = build_flap(
        "FilmTailB",
        [Vector((0.026, 0.016, 0.156)), Vector((0.016, 0.032, 0.150)),
         Vector((0.004, 0.046, 0.142)), Vector((-0.010, 0.058, 0.133)),
         Vector((-0.024, 0.066, 0.124))],
        [0.025, 0.020, 0.014, 0.008, 0.004], 0.0018, material)

    film = join_objects([body, knot, flap_a, flap_b], "BundleFilm")
    recalc_normals(film)
    triangulate(film)
    # Smooth over the body, flat above the gather line. Flat shading on the
    # whole 14 x 18 loft produced the large diagonal planes the previous
    # version was criticised for, so the body stays smooth; but the gathered
    # shoulder, knot and tails read better as small facets, and there the
    # facets are the pleats rather than an artefact.
    for polygon in film.data.polygons:
        polygon.use_smooth = polygon.center.z < 0.116
    return film


# ---------------------------------------------------------------------------
# Assembly, export, studio, renders, verification
# ---------------------------------------------------------------------------

def assemble():
    cloth_material = make_material("MAT_BundleCloth", (0.045, 0.135, 0.185), 0.88)
    film_material = make_film_material("MAT_BundleFilm")

    cloth = build_cloth(cloth_material)
    film = build_film(film_material)

    cloth_tris, cloth_non_manifold = stats(cloth)
    film_tris, film_non_manifold = stats(film)
    total = cloth_tris + film_tris
    print(f"[Bundle] BundleCloth triangles: {cloth_tris}, non-manifold edges: {cloth_non_manifold}")
    print(f"[Bundle] BundleFilm triangles: {film_tris}, non-manifold edges: {film_non_manifold}")
    print(f"[Bundle] Total triangles: {total} (budget {TRIANGLE_BUDGET})")

    assert total <= TRIANGLE_BUDGET, f"Triangle budget exceeded: {total} > {TRIANGLE_BUDGET}"
    assert cloth_non_manifold == 0, f"Cloth is not watertight: {cloth_non_manifold} edges"
    assert film_non_manifold == 0, f"Film is not watertight: {film_non_manifold} edges"
    if total > TRIANGLE_WARN:
        print(f"[Bundle] WARNING: {total} triangles is close to the {TRIANGLE_BUDGET} budget")

    root = bpy.data.objects.new("WrappedBundle", None)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.05
    bpy.context.scene.collection.objects.link(root)
    for obj in (cloth, film):
        obj.parent = root
        obj.matrix_parent_inverse = root.matrix_world.inverted()
    return root, cloth, film


def export_bundle(root, cloth, film):
    os.makedirs(os.path.dirname(EXPORT_PATH), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in (root, cloth, film):
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
    print(f"[Bundle] Exported {EXPORT_PATH}")


def add_studio_material(name, color, roughness):
    return make_material(name, color, roughness)


def add_plane(name, location, scale, rotation, material):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
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
    direction = Vector(target) - Vector(location)
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    return obj


def build_studio():
    """Darker surroundings make clear film legible: a bright studio washes it out."""
    floor_material = add_studio_material("MAT_StudioFloor", (0.20, 0.20, 0.198), 0.72)
    backdrop_material = add_studio_material("MAT_StudioBackdrop", (0.26, 0.275, 0.30), 0.85)
    add_plane("StudioFloor", (0.0, 0.0, -0.0008), (9.0, 9.0, 1.0), (0.0, 0.0, 0.0),
              floor_material)
    add_plane("StudioBackdrop", (0.0, 1.10, 1.10), (9.0, 4.0, 1.0),
              (math.radians(90.0), 0.0, 0.0), backdrop_material)

    # Tight key + tight rim: small sources give the thin bright reflection
    # lines that make clear plastic read as plastic.
    add_area_light("Key", (-0.50, -0.62, 0.66), (0.0, 0.0, 0.10), 0.30, 9.0,
                   (1.0, 0.97, 0.92))
    add_area_light("Fill", (0.70, -0.52, 0.34), (0.0, 0.0, 0.08), 0.55, 1.5,
                   (0.86, 0.92, 1.0))
    add_area_light("Rim", (0.16, 0.62, 0.52), (0.0, 0.0, 0.11), 0.28, 7.0,
                   (1.0, 1.0, 1.0))
    add_area_light("RimLeft", (-0.62, 0.42, 0.40), (0.0, 0.0, 0.10), 0.22, 5.0,
                   (1.0, 1.0, 1.0))
    add_area_light("Top", (0.0, -0.05, 1.00), (0.0, 0.0, 0.07), 0.45, 2.2,
                   (1.0, 1.0, 1.0))

    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    if background is not None:
        background.inputs["Color"].default_value = (0.30, 0.33, 0.38, 1.0)
        background.inputs["Strength"].default_value = 0.30


def make_camera(name, location, target, lens=58.0):
    data = bpy.data.cameras.new(name=name)
    data.lens = lens
    data.sensor_width = 36.0
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = location
    direction = Vector(target) - Vector(location)
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    return obj


def configure_render():
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 128
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 12
    scene.cycles.transparent_max_bounces = 48
    scene.cycles.transmission_bounces = 12
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
        ("bundle_front", (0.0, -0.60, 0.100), (0.0, 0.0, 0.096)),
        ("bundle_three_quarter", (-0.46, -0.50, 0.30), (0.0, 0.0, 0.090)),
        ("bundle_top", (0.06, -0.20, 0.66), (0.0, 0.0, 0.078)),
    ]
    for name, location, target in views:
        camera = make_camera(f"Cam_{name}", location, target)
        scene.camera = camera
        path = os.path.join(PREVIEW_DIR, f"{name}.png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        print(f"[Bundle] Rendered {path}")


def verify_export():
    """Re-import the FBX in a clean scene and print hierarchy/dimensions."""
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=EXPORT_PATH)
    print("[Verify] Re-imported", EXPORT_PATH)
    total = 0
    overall_min = Vector((1e9, 1e9, 1e9))
    overall_max = Vector((-1e9, -1e9, -1e9))
    for obj in bpy.context.scene.objects:
        parent = obj.parent.name if obj.parent else "-"
        if obj.type == "MESH":
            tris = sum(len(poly.vertices) - 2 for poly in obj.data.polygons)
            total += tris
            materials = [mat.name for mat in obj.data.materials]
            dims = obj.dimensions
            print(f"[Verify] {obj.name} type={obj.type} parent={parent} "
                  f"tris={tris} dims=({dims.x:.4f}, {dims.y:.4f}, {dims.z:.4f}) "
                  f"materials={materials}")
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
                  f"matrix_scale={tuple(round(v, 4) for v in obj.scale)}")
    size = overall_max - overall_min
    print(f"[Verify] Total triangles: {total}")
    print(f"[Verify] Overall bounds min=({overall_min.x:.4f}, {overall_min.y:.4f}, "
          f"{overall_min.z:.4f}) max=({overall_max.x:.4f}, {overall_max.y:.4f}, "
          f"{overall_max.z:.4f})")
    print(f"[Verify] Overall size: {size.x:.4f} x {size.y:.4f} x {size.z:.4f} m")
    print(f"[Verify] Footprint centre: ({(overall_min.x + overall_max.x) / 2:.4f}, "
          f"{(overall_min.y + overall_max.y) / 2:.4f}), min Z = {overall_min.z:.4f}")


def main():
    clear_scene()
    root, cloth, film = assemble()
    export_bundle(root, cloth, film)
    configure_render()
    build_studio()
    render_previews()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print(f"[Bundle] Saved {BLEND_PATH}")
    verify_export()


if __name__ == "__main__":
    main()
