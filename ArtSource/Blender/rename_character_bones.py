"""
Rename the Quaternius character's bones to Unity's standard humanoid names.

Unity's humanoid auto-mapper keys on names like Hips/Spine/Chest/LeftUpperArm.
The source rig uses Abdomen/Torso/UpperArm.L, so Unity imports it as a generic
avatar (human=False). Renaming the bones lets Unity build a humanoid avatar
without hand-editing the mapping.

Run headless:
  blender.exe -b -P ArtSource/Blender/rename_character_bones.py
"""

import bpy
import os
import sys

SOURCE = os.path.join(
    os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))),
    "ArtSource", "Downloads", "Characters", "poly-pizza-quaternius-man", "derived", "Quaternius_Man_1m80.fbx",
)
OUTPUT = os.path.join(
    os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))),
    "ArtSource", "Exports", "NPC", "QuaterniusMan_UnityRig.fbx",
)

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


def main():
    if not os.path.exists(SOURCE):
        print(f"[RENAME] missing {SOURCE}")
        sys.exit(2)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE)

    renamed = 0
    for obj in bpy.context.scene.objects:
        if obj.type != "ARMATURE":
            continue
        for bone in obj.data.bones:
            target = RENAME.get(bone.name)
            if target and bone.name != target:
                print(f"[RENAME] {bone.name} -> {target}")
                bone.name = target
                renamed += 1

    arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
    if not arms:
        print("[RENAME] no armature found")
        sys.exit(3)

    print(f"[RENAME] renamed {renamed} bones; armature bones={len(arms[0].data.bones)}")

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=OUTPUT,
        use_selection=False,
        apply_scale_options="FBX_SCALE_ALL",
        object_types={"ARMATURE", "MESH"},
        bake_anim=False,
        add_leaf_bones=False,
    )
    print(f"[RENAME] exported {OUTPUT} ({os.path.getsize(OUTPUT)} bytes)")


main()
