"""Simple first-person forearms/hands for Laundry Legends."""
import os
import bpy

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND = os.path.join(ROOT, "ArtSource", "Blender", "FirstPersonHands.blend")
FBX = os.path.join(ROOT, "ArtSource", "Exports", "FirstPersonHands.fbx")


def box(name, size, loc, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    o.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return o


def main():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

    parts = []
    for side, x in (("Left", -0.18), ("Right", 0.18)):
        forearm = box(f"{side}Forearm", (0.12, 0.42, 0.12), (x, 0.0, 0.0), (0.25, 0, 0))
        hand = box(f"{side}Hand", (0.13, 0.16, 0.10), (x, 0.24, -0.04), (0.25, 0, 0))
        parts += [forearm, hand]

    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    hands = bpy.context.active_object
    hands.name = "FirstPersonHands"

    mat = bpy.data.materials.new("MAT_Hands")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0.55, 0.38, 0.28, 1)
    bsdf.inputs["Roughness"].default_value = 0.8
    hands.data.materials.append(mat)

    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")
    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    os.makedirs(os.path.dirname(FBX), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    bpy.ops.object.select_all(action="DESELECT")
    hands.select_set(True)
    bpy.context.view_layer.objects.active = hands
    bpy.ops.export_scene.fbx(
        filepath=FBX, use_selection=True, object_types={"MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", use_mesh_modifiers=True,
        bake_space_transform=False, add_leaf_bones=False)
    print(f"[Hands] {sum(len(p.vertices) - 2 for p in hands.data.polygons)} triangles")


if __name__ == "__main__":
    main()
