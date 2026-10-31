"""Normalize a Quaternius/Poly Pizza GLB to a Unity-ready FBX at real-world scale.

The source GLB carries a 100x node scale (world height ~4.81 m). This script
bakes a uniform factor so the rest pose measures exactly TARGET_HEIGHT, keeps
the armature, the skin weights and every animation action, then writes an FBX.

Bone-local translation channels are multiplied by the same factor, because pose
bone locations are expressed in rest-bone space and the rest bones are being
scaled. Rotation/scale channels are unitless and are left alone.

Usage:
  blender.exe -b -P convert_to_fbx.py -- <in.glb> <out.fbx>
"""

import sys
import os
import json

import bpy
from mathutils import Vector

TARGET_HEIGHT = 1.80

argv = sys.argv
args = argv[argv.index("--") + 1:]
src, dst = args[0], args[1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
bpy.context.view_layer.update()

# drop the importer's bone-display icosphere and stray *_end helper empties
for ob in list(bpy.data.objects):
    if ob.type == "MESH" and ob.name.startswith("Icosphere") and ob.parent is None \
            and not ob.data.materials and len(ob.data.vertices) <= 64:
        bpy.data.objects.remove(ob, do_unlink=True)
    elif ob.type == "EMPTY" and ob.name.endswith("_end"):
        bpy.data.objects.remove(ob, do_unlink=True)

arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
if not arms:
    raise SystemExit("no armature found")
arm = arms[0]


def measure():
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    zs = []
    for ob in [o for o in bpy.data.objects if o.type == "MESH"]:
        ev = ob.evaluated_get(dg)
        me = ev.to_mesh()
        for v in me.vertices:
            zs.append((ob.matrix_world @ v.co).z)
        ev.to_mesh_clear()
    return min(zs), max(zs)


arm.data.pose_position = "REST"
zmin, zmax = measure()
rest_h = zmax - zmin
factor = TARGET_HEIGHT / rest_h

report = {
    "source": src,
    "rest_height_before": round(rest_h, 4),
    "target_height": TARGET_HEIGHT,
    "uniform_factor": round(factor, 6),
}

# ---------------------------------------------------------------- unparent
for ob in bpy.data.objects:
    ob.select_set(False)
for ob in bpy.data.objects:
    if ob.parent is not None:
        ob.select_set(True)
bpy.context.view_layer.objects.active = bpy.data.objects[0]
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")

# ---------------------------------------------------- bake world transforms
for ob in bpy.data.objects:
    ob.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

# scale everything about the world origin
import bmesh  # noqa: E402

for ob in bpy.data.objects:
    ob.scale = (factor, factor, factor)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# ------------------------------------------- rescale bone-local translations
loc_fixed = 0
for action in bpy.data.actions:
    for fc in action.fcurves:
        if fc.data_path.endswith(".location"):
            for kp in fc.keyframe_points:
                kp.co[1] *= factor
                kp.handle_left[1] *= factor
                kp.handle_right[1] *= factor
            fc.update()
            loc_fixed += 1
report["location_fcurves_rescaled"] = loc_fixed

arm.data.pose_position = "POSE"
bpy.context.view_layer.update()

# Give the actions clean names so Unity's FBX take names read "Man_Idle" etc.
# glTF import names them "HumanArmature|Man_Idle"; chained exports re-prefix.
renamed = []
for action in bpy.data.actions:
    clean = action.name.split("|")[-1]
    renamed.append(clean)
    action.name = clean
report["action_names"] = sorted(renamed)

zmin2, zmax2 = measure()
report["rest_height_after"] = round(zmax2 - zmin2, 4)
report["bones"] = len(arm.data.bones)
report["actions"] = len(bpy.data.actions)
report["meshes"] = [
    (o.name, len(o.data.vertices), sum(len(p.vertices) - 2 for p in o.data.polygons))
    for o in bpy.data.objects
    if o.type == "MESH"
]

os.makedirs(os.path.dirname(dst), exist_ok=True)
kwargs = dict(
    filepath=dst,
    use_selection=False,
    apply_scale_options="FBX_SCALE_NONE",
    apply_unit_scale=False,
    add_leaf_bones=False,
    mesh_smooth_type="FACE",
    bake_space_transform=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    armature_nodetype="NULL",
    export_animations=True,
    export_animation_mode="ACTIONS",
    export_bake_animation=False,
    export_optimize_animation_size=False,
    export_path_mode="AUTO",
    path_mode="AUTO",
    embed_textures=False,
)
try:
    bpy.ops.export_scene.fbx(**kwargs)
except TypeError as exc:
    print("FALLBACK export (kwarg rejected:", exc, ")")
    bpy.ops.export_scene.fbx(
        filepath=dst,
        use_selection=False,
        apply_scale_options="FBX_SCALE_NONE",
        apply_unit_scale=False,
        add_leaf_bones=False,
    )

report["out_bytes"] = os.path.getsize(dst)
print("=== CONVERT_JSON_BEGIN ===")
print(json.dumps(report, indent=1))
print("=== CONVERT_JSON_END ===")
