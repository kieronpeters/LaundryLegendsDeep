"""Repair the Quaternius man rig so Unity can build a humanoid avatar.

Unity's importer and AvatarBuilder both refuse the exported rig with:

    AvatarBuilder 'QuaterniusMan': Transform 'Hips' is not an ancestor of 'LeftUpperLeg'

The source rig parents the legs to an extra `Body` bone and the feet to the
armature root `Bone`, which breaks the humanoid chain Unity requires:

    Hips -> UpperLeg -> LowerLeg -> Foot

This script renames the bones to Unity's standard names (same map as
rename_character_bones.py) and re-parents the four limb bones in place. Blender
rest-pose bone matrices live in armature space, so re-parenting does not move
the bones; the script verifies head/tail stability and reports the deviation.

Run headless:
  blender.exe -b -P ArtSource/Blender/fix_character_hierarchy.py
"""

import json
import os
import sys

import bpy
from mathutils import Vector

PROJECT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SOURCE = os.path.join(
    PROJECT, "ArtSource", "Downloads", "Characters", "poly-pizza-quaternius-man",
    "derived", "Quaternius_Man_1m80.fbx",
)
OUTPUT = os.path.join(PROJECT, "ArtSource", "Exports", "NPC", "QuaterniusMan_UnityRig.fbx")

RENAME = {
    "Hips": "Hips",
    "Abdomen": "Spine",
    "Torso": "Chest",
    "Neck": "Neck",
    "Head": "Head",
    "Shoulder.L": "LeftShoulder",
    "UpperArm.L": "LeftUpperArm",
    "LowerArm.L": "LeftLowerArm",
    "Palm.L": "LeftHand",
    "Shoulder.R": "RightShoulder",
    "UpperArm.R": "RightUpperArm",
    "LowerArm.R": "RightLowerArm",
    "Palm.R": "RightHand",
    "UpperLeg.L": "LeftUpperLeg",
    "LowerLeg.L": "LeftLowerLeg",
    "Foot.L": "LeftFoot",
    "UpperLeg.R": "RightUpperLeg",
    "LowerLeg.R": "RightLowerLeg",
    "Foot.R": "RightFoot",
}

# child -> required parent, expressed with the post-rename names
REPARENT = {
    "LeftUpperLeg": "Hips",
    "RightUpperLeg": "Hips",
    "LeftFoot": "LeftLowerLeg",
    "RightFoot": "RightLowerLeg",
}

report = {"source": SOURCE, "output": OUTPUT}


def measure_height():
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    zs = []
    for ob in [o for o in bpy.data.objects if o.type == "MESH"]:
        ev = ob.evaluated_get(dg)
        me = ev.to_mesh()
        for v in me.vertices:
            zs.append((ob.matrix_world @ v.co).z)
        ev.to_mesh_clear()
    return (min(zs), max(zs)) if zs else (0.0, 0.0)


def main():
    if not os.path.exists(SOURCE):
        print(f"[HIER] missing source {SOURCE}")
        sys.exit(2)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE)

    arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
    if not arms:
        print("[HIER] no armature found")
        sys.exit(3)

    arm = arms[0]
    report["armature_object"] = arm.name
    report["bone_count_before"] = len(arm.data.bones)
    report["mesh_objects"] = [
        (o.name, len(o.data.vertices), [m.name for m in o.data.materials if m])
        for o in bpy.context.scene.objects if o.type == "MESH"
    ]

    zmin, zmax = measure_height()
    report["rest_height_before"] = round(zmax - zmin, 4)

    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    ebs = arm.data.edit_bones

    renamed = 0
    for b in ebs:
        target = RENAME.get(b.name)
        if target and b.name != target:
            b.name = target
            renamed += 1

    heads_before = {b.name: (b.head.copy(), b.tail.copy(), b.matrix.copy()) for b in ebs}

    applied = []
    for child, parent in REPARENT.items():
        cb = ebs.get(child)
        pb = ebs.get(parent)
        if cb is None or pb is None:
            print(f"[HIER] FAIL missing {child} or {parent}")
            sys.exit(4)
        previous = cb.parent.name if cb.parent else None
        # EditBone head/tail/roll are armature-space values; capture and restore
        # them so re-parenting can never move the rest pose.
        head = cb.head.copy()
        tail = cb.tail.copy()
        roll = cb.roll
        cb.parent = pb
        cb.use_connect = False
        cb.head = head
        cb.tail = tail
        cb.roll = roll
        applied.append((child, previous, pb.name))

    max_delta = 0.0
    for b in ebs:
        h0, t0, m0 = heads_before[b.name]
        max_delta = max(
            max_delta,
            (b.head - h0).length,
            (b.tail - t0).length,
        )

    tree = []
    for b in ebs:
        tree.append((b.name, b.parent.name if b.parent else None))

    bpy.ops.object.mode_set(mode="OBJECT")

    report["renamed"] = renamed
    report["reparented"] = applied
    report["max_head_tail_delta"] = round(max_delta, 9)
    report["bone_tree"] = sorted(tree, key=lambda t: (t[1] or "", t[0]))
    report["bone_count_after"] = len(arm.data.bones)

    zmin2, zmax2 = measure_height()
    report["rest_height_after"] = round(zmax2 - zmin2, 4)

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=OUTPUT,
        use_selection=False,
        apply_scale_options="FBX_SCALE_ALL",
        object_types={"ARMATURE", "MESH"},
        bake_anim=False,
        add_leaf_bones=False,
    )
    report["out_bytes"] = os.path.getsize(OUTPUT)

    print("=== HIER_JSON_BEGIN ===")
    print(json.dumps(report, indent=1))
    print("=== HIER_JSON_END ===")


main()
