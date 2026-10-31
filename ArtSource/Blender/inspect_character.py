"""Headless Blender inspection for candidate rigged characters.

Usage:
  blender.exe -b -P inspect_character.py -- <path-to-fbx-or-glb> [--label NAME]

Reports object hierarchy, armature bone count and first bones, mesh stats,
deformed world-space height, armature modifier / vertex group presence, and a
coarse T-pose vs A-pose classification from the rest-pose arm direction.

The scene is imported into an empty file, then measured through the evaluated
dependency graph so that the numbers reflect what an engine would see.
"""

import sys
import os
import math
import json

import bpy
from mathutils import Vector

argv = sys.argv
args = argv[argv.index("--") + 1:] if "--" in argv else []
if not args:
    raise SystemExit("no input file given")

path = args[0]
label = os.path.basename(path)
if "--label" in args:
    label = args[args.index("--label") + 1]

ext = os.path.splitext(path)[1].lower()

bpy.ops.wm.read_factory_settings(use_empty=True)

if ext == ".fbx":
    bpy.ops.import_scene.fbx(filepath=path)
elif ext in (".glb", ".gltf"):
    bpy.ops.import_scene.gltf(filepath=path)
else:
    raise SystemExit("unsupported extension " + ext)

bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()

out = {"file": path, "label": label, "format": ext}

objs = list(bpy.data.objects)
out["object_count"] = len(objs)

# ---- hierarchy --------------------------------------------------------
def depth(ob):
    d = 0
    p = ob.parent
    while p:
        d += 1
        p = p.parent
    return d

out["hierarchy"] = []
for ob in sorted(objs, key=lambda o: (depth(o), o.name)):
    row = {
        "name": ob.name,
        "type": ob.type,
        "parent": ob.parent.name if ob.parent else None,
        "world_loc": [round(v, 4) for v in ob.matrix_world.translation],
        "world_scale": [round(v, 5) for v in ob.matrix_world.to_scale()],
    }
    if ob.type == "MESH":
        row["verts"] = len(ob.data.vertices)
        row["tris"] = sum(len(p.vertices) - 2 for p in ob.data.polygons)
        row["vertex_groups"] = len(ob.vertex_groups)
        row["vertex_group_names"] = [g.name for g in ob.vertex_groups]
        row["modifiers"] = [m.type for m in ob.modifiers]
        row["materials"] = [m.name if m else None for m in ob.data.materials]
        row["shape_keys"] = bool(ob.data.shape_keys)
        row["armature_modifier"] = any(
            m.type == "ARMATURE" and m.object is not None for m in ob.modifiers
        )
    out["hierarchy"].append(row)

# ---- armature ---------------------------------------------------------
arms = [o for o in objs if o.type == "ARMATURE"]
out["armature_count"] = len(arms)
if arms:
    arm = arms[0]
    names = [b.name for b in arm.data.bones]
    out["armature_name"] = arm.name
    out["armature_world_loc"] = [round(v, 4) for v in arm.matrix_world.translation]
    out["armature_world_scale"] = [round(v, 5) for v in arm.matrix_world.to_scale()]
    out["bone_count"] = len(names)
    out["bones_first10"] = names[:10]
    out["bone_names_all"] = names

    def norm(s):
        return s.lower().replace(".", "").replace("_", "").replace("-", "").replace(" ", "")

    def find(*keys):
        for k in keys:
            for n in names:
                if norm(n) == k:
                    return n
        for k in keys:
            for n in names:
                if norm(n).endswith(k) or norm(n).startswith(k):
                    return n
        for k in keys:
            for n in names:
                if k in norm(n):
                    return n
        return None

    up_arm = find("upperarml", "leftupperarm", "upperarmleft", "arml", "armleft", "shoulderl")
    low_arm = find("lowerarml", "forearml", "leftlowerarm", "leftforearm", "elbowl", "handl", "palml")
    hips = find("hips", "pelvis", "root")
    head = find("head")
    out["probe"] = {"upper_arm": up_arm, "lower_arm": low_arm, "hips": hips, "head": head}

    def wpos(bone_name):
        b = arm.data.bones.get(bone_name)
        if not b:
            return None
        return arm.matrix_world @ b.head_local

    if up_arm and low_arm:
        a, b = wpos(up_arm), wpos(low_arm)
        d = b - a
        horiz = math.hypot(d.x, d.y)
        # Blender world is Z-up after import
        ang = math.degrees(math.atan2(horiz, -d.z)) if (abs(d.z) > 1e-6 or horiz > 1e-6) else None
        out["arm_rest_vector"] = [round(v, 4) for v in d]
        out["arm_angle_from_vertical_deg"] = round(ang, 1) if ang is not None else None
        if ang is not None:
            if ang >= 70:
                pose = "T-pose (arms near horizontal)"
            elif 25 <= ang < 70:
                pose = "A-pose (arms angled down)"
            elif ang < 25:
                pose = "arms down / relaxed rest pose"
            else:
                pose = "unclassified"
            out["rest_pose_estimate"] = pose

# ---- bone extents (proves the rig is not anchored kilometres away) ----
if arms:
    arm = arms[0]
    bheads = [arm.matrix_world @ b.head_local for b in arm.data.bones]
    if bheads:
        bxs = [p.x for p in bheads]
        bys = [p.y for p in bheads]
        bzs = [p.z for p in bheads]
        out["bone_bounds_min"] = [round(min(bxs), 4), round(min(bys), 4), round(min(bzs), 4)]
        out["bone_bounds_max"] = [round(max(bxs), 4), round(max(bys), 4), round(max(bzs), 4)]
        out["bone_max_distance_from_origin"] = round(
            max(math.sqrt(p.x ** 2 + p.y ** 2 + p.z ** 2) for p in bheads), 4
        )

# ---- deformed world bounds -------------------------------------------
# The Blender glTF importer adds a stray "Icosphere" used as a bone display
# shape. It is not part of the asset, so exclude it from all measurements.
STRAY = {"Icosphere", "Icosphere.001", "Icosphere.002"}


def is_stray(ob):
    return (
        ob.name in STRAY
        and ob.parent is None
        and len(ob.data.materials) == 0
        and len(ob.data.vertices) <= 64
    )


out["stray_meshes_excluded"] = [o.name for o in objs if o.type == "MESH" and is_stray(o)]

pts = []
per_mesh = []
for ob in [o for o in objs if o.type == "MESH" and not is_stray(o)]:
    ob_eval = ob.evaluated_get(dg)
    try:
        me = ob_eval.to_mesh()
    except Exception:
        continue
    mw = ob.matrix_world
    local = [mw @ v.co for v in me.vertices]
    ob_eval.to_mesh_clear()
    if not local:
        continue
    xs = [p.x for p in local]
    ys = [p.y for p in local]
    zs = [p.z for p in local]
    per_mesh.append({
        "name": ob.name,
        "bbox_min": [round(min(xs), 4), round(min(ys), 4), round(min(zs), 4)],
        "bbox_max": [round(max(xs), 4), round(max(ys), 4), round(max(zs), 4)],
        "height_z": round(max(zs) - min(zs), 4),
        "verts": len(ob.data.vertices),
    })
    pts.extend(local)

out["per_mesh_bounds"] = per_mesh

if pts:
    xs = [p.x for p in pts]
    ys = [p.y for p in pts]
    zs = [p.z for p in pts]
    out["bbox_min"] = [round(min(xs), 4), round(min(ys), 4), round(min(zs), 4)]
    out["bbox_max"] = [round(max(xs), 4), round(max(ys), 4), round(max(zs), 4)]
    out["height_m_world_z"] = round(max(zs) - min(zs), 4)
    out["width_m_world_x"] = round(max(xs) - min(xs), 4)
    out["depth_m_world_y"] = round(max(ys) - min(ys), 4)

out["total_verts"] = sum(
    len(o.data.vertices) for o in objs if o.type == "MESH" and not is_stray(o)
)
out["total_tris"] = sum(
    sum(len(p.vertices) - 2 for p in o.data.polygons)
    for o in objs
    if o.type == "MESH" and not is_stray(o)
)

out["actions"] = [a.name for a in bpy.data.actions]
out["action_count"] = len(bpy.data.actions)
out["images"] = [(i.name, i.size[0], i.size[1]) for i in bpy.data.images if i.size[0] > 0]

print("=== INSPECT_JSON_BEGIN ===")
print(json.dumps(out, indent=1))
print("=== INSPECT_JSON_END ===")
