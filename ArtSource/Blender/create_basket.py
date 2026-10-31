"""Build the first low-poly laundry basket for Laundry Legends.

Run with:
  blender --background --python ArtSource/Blender/create_basket.py

Outputs:
  ArtSource/Blender/LaundryBasket.blend
  ArtSource/Exports/LaundryBasket.fbx

The mesh is authored in meters with its origin at the base center. Unity
imports it as a child of the existing Basket prefab; gameplay components stay
on the prefab root.
"""

import os

import bpy


PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(PROJECT_ROOT, "ArtSource", "Blender", "LaundryBasket.blend")
EXPORT_PATH = os.path.join(PROJECT_ROOT, "ArtSource", "Exports", "LaundryBasket.fbx")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def add_box(name, size, location):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def build_basket():
    # Overall footprint 0.50 x 0.35 m, height 0.30 m, base at z=0.
    bottom = add_box("Bottom", (0.50, 0.35, 0.035), (0.0, 0.0, 0.0175))
    front = add_box("Front", (0.50, 0.035, 0.30), (0.0, 0.1575, 0.15))
    back = add_box("Back", (0.50, 0.035, 0.30), (0.0, -0.1575, 0.15))
    left = add_box("Left", (0.035, 0.35, 0.30), (-0.2325, 0.0, 0.15))
    right = add_box("Right", (0.035, 0.35, 0.30), (0.2325, 0.0, 0.15))

    # Simple handles: two short bars above the left and right walls.
    handle_left = add_box("HandleLeft", (0.025, 0.12, 0.025), (-0.2325, 0.0, 0.3125))
    handle_right = add_box("HandleRight", (0.025, 0.12, 0.025), (0.2325, 0.0, 0.3125))

    parts = [bottom, front, back, left, right, handle_left, handle_right]
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = bottom
    bpy.ops.object.join()

    basket = bpy.context.active_object
    basket.name = "LaundryBasket"
    basket.data.name = "LaundryBasketMesh"

    bevel = basket.modifiers.new(name="Bevel", type="BEVEL")
    bevel.width = 0.006
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.ops.object.modifier_apply(modifier=bevel.name)

    # Origin at the base center.
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")

    return basket


def add_material(basket):
    material = bpy.data.materials.new(name="MAT_Basket")
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = (0.42, 0.28, 0.14, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.72
        bsdf.inputs["Metallic"].default_value = 0.0

    basket.data.materials.clear()
    basket.data.materials.append(material)


def export(basket):
    os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
    os.makedirs(os.path.dirname(EXPORT_PATH), exist_ok=True)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    bpy.ops.object.select_all(action="DESELECT")
    basket.select_set(True)
    bpy.context.view_layer.objects.active = basket

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

    print(f"[Basket] Saved {BLEND_PATH}")
    print(f"[Basket] Exported {EXPORT_PATH}")
    print(f"[Basket] Triangles: {sum(len(p.vertices) - 2 for p in basket.data.polygons)}")


def main():
    clear_scene()
    basket = build_basket()
    add_material(basket)
    export(basket)


if __name__ == "__main__":
    main()
