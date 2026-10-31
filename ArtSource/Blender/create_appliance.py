"""Build a low-poly washer/dryer appliance for Laundry Legends.

Run:
  blender --background --python ArtSource/Blender/create_appliance.py
"""

import os

import bpy


PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(PROJECT_ROOT, "ArtSource", "Blender", "Appliance.blend")
EXPORT_PATH = os.path.join(PROJECT_ROOT, "ArtSource", "Exports", "Appliance.fbx")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def add_box(name, size, location, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def add_cylinder(name, radius, depth, location, rotation=(0.0, 0.0, 0.0), vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    return obj


def add_torus(name, major, minor, location, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, location=location, rotation=rotation, major_segments=24, minor_segments=8)
    obj = bpy.context.active_object
    obj.name = name
    return obj


def material(name, color, roughness, metallic=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    return mat


def assign(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def main():
    clear_scene()
    body_mat = material("MAT_ApplianceBody", (0.35, 0.55, 0.75), 0.45, 0.15)
    metal_mat = material("MAT_ApplianceMetal", (0.35, 0.35, 0.37), 0.35, 0.7)
    glass_mat = material("MAT_ApplianceGlass", (0.10, 0.14, 0.16), 0.15, 0.2)
    dark_mat = material("MAT_ApplianceDark", (0.06, 0.06, 0.07), 0.5, 0.0)

    parts = []

    body = add_box("Body", (0.80, 0.70, 1.10), (0.0, 0.0, 0.55))
    bevel = body.modifiers.new("Bevel", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 2
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.modifier_apply(modifier="Bevel")
    assign(body, body_mat)
    parts.append(body)

    # Door ring and glass on the room-facing (-Y) side.
    ring = add_torus("DoorRing", 0.235, 0.028, (0.0, -0.355, 0.62), (1.5708, 0.0, 0.0))
    assign(ring, metal_mat)
    parts.append(ring)

    glass = add_cylinder("DoorGlass", 0.215, 0.018, (0.0, -0.36, 0.62), (1.5708, 0.0, 0.0))
    assign(glass, glass_mat)
    parts.append(glass)

    panel = add_box("ControlPanel", (0.52, 0.035, 0.19), (0.0, -0.36, 0.94))
    assign(panel, metal_mat)
    parts.append(panel)

    display = add_box("Display", (0.22, 0.012, 0.06), (-0.08, -0.38, 0.95))
    assign(display, dark_mat)
    parts.append(display)

    coin = add_box("CoinSlot", (0.045, 0.012, 0.045), (0.18, -0.38, 0.95))
    assign(coin, dark_mat)
    parts.append(coin)

    drawer = add_box("DetergentDrawer", (0.30, 0.035, 0.09), (-0.10, -0.36, 0.80))
    assign(drawer, dark_mat)
    parts.append(drawer)

    for x in (-0.32, 0.32):
        for y in (-0.26, 0.26):
            foot = add_cylinder("Foot", 0.035, 0.05, (x, y, 0.025), vertices=12)
            assign(foot, dark_mat)
            parts.append(foot)

    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()
    appliance = bpy.context.active_object
    appliance.name = "Appliance"
    appliance.data.name = "ApplianceMesh"

    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")

    os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
    os.makedirs(os.path.dirname(EXPORT_PATH), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    bpy.ops.object.select_all(action="DESELECT")
    appliance.select_set(True)
    bpy.context.view_layer.objects.active = appliance
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
    print(f"[Appliance] {sum(len(p.vertices) - 2 for p in appliance.data.polygons)} triangles")


if __name__ == "__main__":
    main()
