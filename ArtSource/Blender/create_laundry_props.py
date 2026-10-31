"""Build low-poly loose-laundry and bag props for Laundry Legends.

Run:
  blender --background --python ArtSource/Blender/create_laundry_props.py

Outputs:
  ArtSource/Blender/LooseLaundry.blend / .fbx
  ArtSource/Blender/Bag.blend / .fbx
"""

import os

import bpy


PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_DIR = os.path.join(PROJECT_ROOT, "ArtSource", "Blender")
EXPORT_DIR = os.path.join(PROJECT_ROOT, "ArtSource", "Exports")


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


def join(parts, name):
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    joined = bpy.context.active_object
    joined.name = name
    joined.data.name = f"{name}Mesh"
    return joined


def bevel(obj, width):
    modifier = obj.modifiers.new(name="Bevel", type="BEVEL")
    modifier.width = width
    modifier.segments = 2
    modifier.limit_method = "ANGLE"
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def add_material(obj, name, color, roughness):
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = 0.0
    obj.data.materials.clear()
    obj.data.materials.append(material)


def save_and_export(obj, blend_name, fbx_name):
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")
    blend_path = os.path.join(BLEND_DIR, blend_name)
    fbx_path = os.path.join(EXPORT_DIR, fbx_name)
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    os.makedirs(os.path.dirname(fbx_path), exist_ok=True)
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
    triangles = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print(f"[Props] {fbx_name}: {triangles} triangles")


def build_loose_laundry():
    parts = [
        add_box("TowelA", (0.30, 0.22, 0.075), (0.0, 0.0, 0.045), (0.0, 0.0, 0.18)),
        add_box("TowelB", (0.27, 0.20, 0.075), (0.01, 0.01, 0.115), (0.0, 0.0, -0.24)),
        add_box("TowelC", (0.22, 0.18, 0.065), (-0.01, -0.01, 0.18), (0.0, 0.0, 0.5)),
    ]
    loose = join(parts, "LooseLaundry")
    bevel(loose, 0.008)
    add_material(loose, "MAT_LooseLaundry", (0.16, 0.45, 0.47), 0.8)
    save_and_export(loose, "LooseLaundry.blend", "LooseLaundry.fbx")


def build_bag():
    body = add_box("Body", (0.36, 0.24, 0.40), (0.0, 0.0, 0.20))
    fold = add_box("Fold", (0.34, 0.22, 0.035), (0.0, 0.0, 0.405))
    handle_left = add_box("HandleLeft", (0.025, 0.02, 0.10), (-0.10, 0.0, 0.46))
    handle_right = add_box("HandleRight", (0.025, 0.02, 0.10), (0.10, 0.0, 0.46))
    bag = join([body, fold, handle_left, handle_right], "Bag")
    bevel(bag, 0.006)
    add_material(bag, "MAT_Bag", (0.52, 0.38, 0.22), 0.75)
    save_and_export(bag, "Bag.blend", "Bag.fbx")


def main():
    clear_scene()
    build_loose_laundry()
    clear_scene()
    build_bag()


if __name__ == "__main__":
    main()
