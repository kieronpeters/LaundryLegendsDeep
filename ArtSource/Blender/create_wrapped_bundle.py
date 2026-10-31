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
plastic film, gathered into a twisted knot with two small tails).

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

SEGMENTS = 16          # radial segments of the film body
FILM_THICKNESS = 0.002  # metres, inward from the authored outer surface
TRIANGLE_BUDGET = 1200

# Film outer profile: (z, half_x, half_y, centre_x, centre_y, wobble, phase_deg)
# Height 0 -> ~0.184 m; widest ring is 0.147 x 0.107 half extent (~0.294 x 0.214).
FILM_RINGS = [
    (0.000, 0.136, 0.096, 0.000, 0.000, 0.004, 0.0),
    (0.016, 0.145, 0.105, 0.000, 0.000, 0.007, 25.0),
    (0.036, 0.148, 0.108, 0.001, 0.000, 0.013, 55.0),
    (0.054, 0.148, 0.108, 0.001, 0.001, 0.014, 95.0),
    (0.072, 0.148, 0.108, 0.002, 0.001, 0.014, 140.0),
    (0.106, 0.140, 0.102, 0.004, 0.003, 0.012, 220.0),
    (0.120, 0.137, 0.100, 0.005, 0.004, 0.012, 255.0),
    (0.132, 0.132, 0.096, 0.007, 0.005, 0.020, 290.0),
    (0.141, 0.112, 0.082, 0.009, 0.007, 0.032, 320.0),
    (0.149, 0.075, 0.054, 0.013, 0.010, 0.045, 350.0),
    (0.156, 0.042, 0.031, 0.017, 0.012, 0.034, 15.0),
    (0.162, 0.024, 0.018, 0.019, 0.013, 0.018, 45.0),
    (0.167, 0.019, 0.014, 0.020, 0.014, 0.000, 70.0),
]


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.meshes, bpy.data.materials, bpy.data.lights,
                       bpy.data.cameras, bpy.data.images):
        for block in list(collection):
            if block.users == 0:
                collection.remove(block)


def make_material(name, base_color, roughness, metallic=0.0, alpha=1.0,
                  ior=1.45, transmission=0.0, coat=0.0):
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf is None:
        return material

    def set_input(key, value):
        if key in bsdf.inputs:
            bsdf.inputs[key].default_value = value

    set_input("Base Color", (*base_color, 1.0))
    set_input("Roughness", roughness)
    set_input("Metallic", metallic)
    set_input("Alpha", alpha)
    set_input("IOR", ior)
    set_input("Transmission Weight", transmission)
    set_input("Coat Weight", coat)

    if alpha < 1.0:
        material.use_backface_culling = False
        try:
            material.surface_render_method = "BLENDED"
        except AttributeError:
            pass
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
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def shade_smooth(obj):
    for polygon in obj.data.polygons:
        polygon.use_smooth = True


def stats(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    non_manifold = sum(1 for edge in bm.edges if len(edge.link_faces) != 2)
    triangles = sum(len(face.verts) - 2 for face in bm.faces)
    bm.free()
    return triangles, non_manifold


# ---------------------------------------------------------------------------
# Folded clothes core
# ---------------------------------------------------------------------------

def add_beveled_box(name, size, location, rotation_z, bevel_width, segments):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location,
                                    rotation=(0.0, 0.0, math.radians(rotation_z)))
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    modifier = obj.modifiers.new(name="Bevel", type="BEVEL")
    modifier.width = bevel_width
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return obj


def soften(obj, taper, twist_degrees):
    """Taper and lightly twist a box vertically so it reads as folded cloth."""
    mesh = obj.data
    zs = [vertex.co.z for vertex in mesh.vertices]
    z_min, z_max = min(zs), max(zs)
    span = max(z_max - z_min, 1e-6)
    centre_x = sum(vertex.co.x for vertex in mesh.vertices) / len(mesh.vertices)
    centre_y = sum(vertex.co.y for vertex in mesh.vertices) / len(mesh.vertices)
    for vertex in mesh.vertices:
        t = (vertex.co.z - z_min) / span
        shrink = 1.0 - taper * t
        x = (vertex.co.x - centre_x) * shrink
        y = (vertex.co.y - centre_y) * shrink
        angle = math.radians(twist_degrees) * t
        cos_a, sin_a = math.cos(angle), math.sin(angle)
        vertex.co.x = centre_x + x * cos_a - y * sin_a
        vertex.co.y = centre_y + x * sin_a + y * cos_a
    mesh.update()


def build_cloth(material):
    lower = add_beveled_box("ClothLower", (0.242, 0.168, 0.086),
                            (0.000, 0.000, 0.048), 2.0, 0.022, 2)
    soften(lower, taper=0.035, twist_degrees=1.8)
    upper = add_beveled_box("ClothUpper", (0.200, 0.134, 0.050),
                            (-0.010, 0.006, 0.102), -6.0, 0.011, 2)
    soften(upper, taper=0.055, twist_degrees=-2.5)

    cloth = join_objects([lower, upper], "BundleCloth")
    cloth.data.materials.clear()
    cloth.data.materials.append(material)
    recalc_normals(cloth)
    triangulate(cloth)
    shade_smooth(cloth)
    return cloth


# ---------------------------------------------------------------------------
# Plastic film
# ---------------------------------------------------------------------------

def superellipse_point(angle, half_x, half_y, cx, cy, z, wobble, phase, twist):
    cos_a, sin_a = math.cos(angle), math.sin(angle)
    exponent = 3.4
    x = half_x * math.copysign(abs(cos_a) ** (2.0 / exponent), cos_a)
    y = half_y * math.copysign(abs(sin_a) ** (2.0 / exponent), sin_a)
    if z > 0.125:
        # Tighter, higher-frequency gathers where the film is cinched.
        radius = 1.0 + wobble * math.sin(4.0 * angle + phase) \
            + 0.6 * wobble * math.sin(7.0 * angle + phase * 1.9)
    else:
        radius = 1.0 + wobble * math.sin(3.0 * angle + phase) \
            + 0.55 * wobble * math.sin(5.0 * angle + phase * 1.7)
    x *= radius
    y *= radius
    cos_t, sin_t = math.cos(twist), math.sin(twist)
    return (cx + x * cos_t - y * sin_t, cy + x * sin_t + y * cos_t, z)


def build_film_body(material):
    verts = []
    for z, half_x, half_y, cx, cy, wobble, phase_deg in FILM_RINGS:
        phase = math.radians(phase_deg)
        twist = math.radians(14.0) * (z / 0.167) ** 1.5
        for index in range(SEGMENTS):
            angle = 2.0 * math.pi * index / SEGMENTS
            verts.append(superellipse_point(angle, half_x, half_y, cx, cy, z,
                                            wobble, phase, twist))

    faces = []
    for ring in range(len(FILM_RINGS) - 1):
        for index in range(SEGMENTS):
            a = ring * SEGMENTS + index
            b = ring * SEGMENTS + (index + 1) % SEGMENTS
            c = (ring + 1) * SEGMENTS + (index + 1) % SEGMENTS
            d = (ring + 1) * SEGMENTS + index
            faces.append((a, b, c, d))

    # Bottom and top caps (winding fixed by recalc_normals afterwards).
    faces.append(tuple(range(SEGMENTS - 1, -1, -1)))
    top_start = (len(FILM_RINGS) - 1) * SEGMENTS
    faces.append(tuple(range(top_start, top_start + SEGMENTS)))

    body = link_mesh("FilmBody", verts, faces, material)
    recalc_normals(body)

    solidify = body.modifiers.new(name="FilmThickness", type="SOLIDIFY")
    solidify.thickness = FILM_THICKNESS
    solidify.offset = -1.0
    solidify.use_even_offset = True
    solidify.use_rim = True
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
    centres = [
        (Vector((0.019, 0.013, 0.162)), 0.020, 0.016, 0.0),
        (Vector((0.021, 0.014, 0.169)), 0.016, 0.013, 55.0),
        (Vector((0.025, 0.015, 0.176)), 0.012, 0.010, 125.0),
        (Vector((0.030, 0.017, 0.181)), 0.005, 0.004, 215.0),
    ]
    sides = 8
    verts = []
    for centre, radius_x, radius_y, twist_deg in centres:
        twist = math.radians(twist_deg)
        for index in range(sides):
            angle = 2.0 * math.pi * index / sides
            x = radius_x * math.cos(angle)
            y = radius_y * math.sin(angle)
            cos_t, sin_t = math.cos(twist), math.sin(twist)
            verts.append((centre.x + x * cos_t - y * sin_t,
                          centre.y + x * sin_t + y * cos_t,
                          centre.z))
    faces = []
    for ring in range(len(centres) - 1):
        for index in range(sides):
            a = ring * sides + index
            b = ring * sides + (index + 1) % sides
            c = (ring + 1) * sides + (index + 1) % sides
            d = (ring + 1) * sides + index
            faces.append((a, b, c, d))
    faces.append(tuple(range(sides - 1, -1, -1)))
    last = (len(centres) - 1) * sides
    faces.append(tuple(range(last, last + sides)))
    knot = link_mesh("FilmKnot", verts, faces, material)
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
        [Vector((0.026, 0.015, 0.168)), Vector((0.036, 0.009, 0.166)),
         Vector((0.043, 0.003, 0.160))],
        [0.013, 0.008, 0.003], 0.0018, material)
    flap_b = build_flap(
        "FilmTailB",
        [Vector((0.023, 0.014, 0.166)), Vector((0.014, 0.024, 0.165)),
         Vector((0.006, 0.031, 0.159))],
        [0.012, 0.007, 0.003], 0.0018, material)

    film = join_objects([body, knot, flap_a, flap_b], "BundleFilm")
    recalc_normals(film)
    triangulate(film)
    # Flat shading keeps the low-poly facets and makes the film wrinkles read.
    for polygon in film.data.polygons:
        polygon.use_smooth = False
    return film


# ---------------------------------------------------------------------------
# Assembly, export, studio, renders, verification
# ---------------------------------------------------------------------------

def assemble():
    cloth_material = make_material("MAT_BundleCloth", (0.11, 0.28, 0.34), 0.85)
    film_material = make_material("MAT_BundleFilm", (0.88, 0.93, 0.96), 0.06,
                                  alpha=0.24, ior=1.45, coat=0.35)

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
        bake_space_transform=False,
        add_leaf_bones=False,
    )
    print(f"[Bundle] Exported {EXPORT_PATH}")


def add_studio_material(name, color, roughness):
    material = make_material(name, color, roughness)
    return material


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
    floor_material = add_studio_material("MAT_StudioFloor", (0.40, 0.40, 0.39), 0.75)
    backdrop_material = add_studio_material("MAT_StudioBackdrop", (0.48, 0.49, 0.50), 0.85)
    add_plane("StudioFloor", (0.0, 0.0, -0.0008), (9.0, 9.0, 1.0), (0.0, 0.0, 0.0),
              floor_material)
    add_plane("StudioBackdrop", (0.0, 1.10, 1.10), (9.0, 4.0, 1.0),
              (math.radians(90.0), 0.0, 0.0), backdrop_material)

    add_area_light("Key", (-0.55, -0.70, 0.72), (0.0, 0.0, 0.09), 0.9, 8.0,
                   (1.0, 0.97, 0.92))
    add_area_light("Fill", (0.75, -0.55, 0.38), (0.0, 0.0, 0.07), 1.4, 3.5,
                   (0.88, 0.93, 1.0))
    add_area_light("Rim", (0.20, 0.65, 0.55), (0.0, 0.0, 0.10), 0.6, 5.0,
                   (1.0, 1.0, 1.0))
    add_area_light("Top", (0.0, 0.0, 1.0), (0.0, 0.0, 0.06), 1.2, 4.0,
                   (1.0, 1.0, 1.0))

    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    if background is not None:
        background.inputs["Color"].default_value = (0.62, 0.63, 0.64, 1.0)
        background.inputs["Strength"].default_value = 0.18


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
    scene.cycles.max_bounces = 8
    scene.cycles.transparent_max_bounces = 16
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
        ("bundle_front", (0.0, -0.58, 0.095), (0.0, 0.0, 0.088)),
        ("bundle_three_quarter", (-0.42, -0.47, 0.27), (0.0, 0.0, 0.082)),
        ("bundle_top", (0.06, -0.20, 0.62), (0.0, 0.0, 0.070)),
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
            print(f"[Verify] {obj.name} type={obj.type} parent={parent} matrix_scale={tuple(round(v, 4) for v in obj.scale)}")
    size = overall_max - overall_min
    print(f"[Verify] Total triangles: {total}")
    print(f"[Verify] Overall bounds min=({overall_min.x:.4f}, {overall_min.y:.4f}, "
          f"{overall_min.z:.4f}) max=({overall_max.x:.4f}, {overall_max.y:.4f}, "
          f"{overall_max.z:.4f})")
    print(f"[Verify] Overall size: {size.x:.4f} x {size.y:.4f} x {size.z:.4f} m")


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
