"""Render turntable previews of an imported character for visual verification.

Usage:
  blender.exe -b -P render_character.py -- <path> <out-prefix>

Writes <out-prefix>_front.png and <out-prefix>_threequarter.png.
Uses workbench-style EEVEE lighting so it needs no external HDRI.
"""

import sys
import os
import math

import bpy
from mathutils import Vector

argv = sys.argv
args = argv[argv.index("--") + 1:]
path, prefix = args[0], args[1]
want_action = None
want_frame = 1
if "--action" in args:
    want_action = args[args.index("--action") + 1]
if "--frame" in args:
    want_frame = int(args[args.index("--frame") + 1])
ext = os.path.splitext(path)[1].lower()

bpy.ops.wm.read_factory_settings(use_empty=True)
if ext == ".fbx":
    bpy.ops.import_scene.fbx(filepath=path)
else:
    bpy.ops.import_scene.gltf(filepath=path)
bpy.context.view_layer.update()

if want_action:
    for ob in bpy.data.objects:
        if ob.type == "ARMATURE" and ob.animation_data:
            act = bpy.data.actions.get(want_action)
            if act is None:
                for cand in bpy.data.actions:
                    if cand.name.split("|")[-1] == want_action:
                        act = cand
                        break
            if act:
                ad = ob.animation_data
                for tr in ad.nla_tracks:
                    tr.mute = True
                ad.action = act
                # Blender 4.4+ slotted actions need an explicit slot binding
                try:
                    if ad.action_slot is None and getattr(act, "slots", None):
                        ad.action_slot = act.slots[0]
                except Exception as exc:
                    print("slot assign failed", exc)
                bpy.context.scene.frame_set(want_frame)
                print("ACTION SET", act.name, "frame", want_frame,
                      "slot", getattr(ad, "action_slot", None))
            else:
                print("ACTION NOT FOUND", want_action,
                      [a.name for a in bpy.data.actions])

# remove the glTF importer bone-shape helper
for ob in list(bpy.data.objects):
    if ob.type == "MESH" and ob.name.startswith("Icosphere") and ob.parent is None \
            and not ob.data.materials and len(ob.data.vertices) <= 64:
        bpy.data.objects.remove(ob, do_unlink=True)

dg = bpy.context.evaluated_depsgraph_get()
pts = []
for ob in [o for o in bpy.data.objects if o.type == "MESH"]:
    ob_eval = ob.evaluated_get(dg)
    me = ob_eval.to_mesh()
    pts.extend([ob.matrix_world @ v.co for v in me.vertices])
    ob_eval.to_mesh_clear()

xs = [p.x for p in pts]
ys = [p.y for p in pts]
zs = [p.z for p in pts]
center = Vector(((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, (min(zs) + max(zs)) / 2))
height = max(zs) - min(zs)

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.film_transparent = False
scene.render.resolution_x = 640
scene.render.resolution_y = 960
scene.render.resolution_percentage = 100
scene.view_settings.view_transform = "Standard"

world = bpy.data.worlds.new("W")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.35, 0.35, 0.38, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
scene.world = world

# key + fill lights
for name, loc, energy in (
    ("Key", (3.0, -3.0, 2.5), 900),
    ("Fill", (-3.0, -2.0, 1.5), 400),
    ("Rim", (0.0, 3.5, 2.0), 500),
):
    ld = bpy.data.lights.new(name, "POINT")
    ld.energy = energy * max(1.0, height)
    ld.shadow_soft_size = 1.5
    lo = bpy.data.objects.new(name, ld)
    lo.location = (center.x + loc[0], center.y + loc[1], center.z + loc[2])
    scene.collection.objects.link(lo)

cam_data = bpy.data.cameras.new("Cam")
cam_data.lens = 60
cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam


def aim(obj, target):
    d = target - obj.location
    obj.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()


dist = height * 1.9
for suffix, (dx, dy) in (("front", (0.0, -1.0)), ("threequarter", (0.75, -0.75))):
    cam.location = Vector((center.x + dx * dist, center.y + dy * dist, center.z + height * 0.06))
    aim(cam, center)
    scene.render.filepath = f"{prefix}_{suffix}.png"
    bpy.ops.render.render(write_still=True)
    print("WROTE", scene.render.filepath)
