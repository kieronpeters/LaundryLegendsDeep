"""Blender headless: re-import the MakeHuman pilot FBX, report structure, render previews.

Usage:
  blender.exe -b --factory-startup --python makehuman_verify_render.py -- ^
      --fbx "ArtSource/Exports/Characters/Pilot_Human.fbx" ^
      --outdir "ArtSource/Exports/Characters" ^
      --report "ArtSource/Notes/makehuman_pilot_verify.txt" ^
      --yaw 0

Writes report_verify (text) and two 900x900 PNGs (front, three-quarter).
"""

import bpy
import sys
import os
import math
import json

# ---------------------------------------------------------------- arguments
argv = sys.argv
argv = argv[argv.index("--") + 1:] if "--" in argv else []


def arg(name, default=None):
    if name in argv:
        return argv[argv.index(name) + 1]
    return default


FBX = os.path.abspath(arg("--fbx", "ArtSource/Exports/Characters/Pilot_Human.fbx"))
OUTDIR = os.path.abspath(arg("--outdir", "ArtSource/Exports/Characters"))
REPORT = os.path.abspath(arg("--report", "ArtSource/Notes/makehuman_pilot_verify.txt"))
YAW = float(arg("--yaw", "0"))
SAMPLES = int(arg("--samples", "48"))

os.makedirs(OUTDIR, exist_ok=True)
os.makedirs(os.path.dirname(REPORT), exist_ok=True)

lines = []


def say(msg=""):
    lines.append(str(msg))
    print(msg)


# ---------------------------------------------------------------- clean scene
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.unit_settings.system = 'METRIC'
bpy.context.scene.unit_settings.scale_length = 1.0

say("=== MakeHuman pilot FBX round-trip verify ===")
say("blender      : %s" % bpy.app.version_string)
say("fbx          : %s (%d bytes)" % (FBX, os.path.getsize(FBX) if os.path.exists(FBX) else -1))

bpy.ops.import_scene.fbx(filepath=FBX)

objs = list(bpy.context.scene.objects)
meshes = [o for o in objs if o.type == 'MESH']
arms = [o for o in objs if o.type == 'ARMATURE']

say("")
say("--- objects ---")
say("total objects: %d" % len(objs))
for o in objs:
    say("  %-28s type=%-8s parent=%s" % (o.name, o.type, o.parent.name if o.parent else "-"))

say("")
say("--- armature ---")
if not arms:
    say("NO ARMATURE FOUND")
for a in arms:
    bones = a.data.bones
    say("armature '%s': %d bones" % (a.name, len(bones)))
    say("first 15 bone names: %s" % ", ".join(b.name for b in bones[:15]))
    say("all bone names (%d): %s" % (len(bones), ", ".join(b.name for b in bones)))
    say("is humanoid-ish root bones: %s" % ", ".join(
        b.name for b in bones if b.parent is None))

say("")
say("--- mesh ---")
total_v = 0
total_t = 0
vgroup_report = []
mod_report = []
for m in meshes:
    me = m.data
    me.calc_loop_triangles()
    total_v += len(me.vertices)
    total_t += len(me.loop_triangles)
    vg = len(m.vertex_groups)
    weighted = 0
    for v in me.vertices:
        if any(g.weight > 0.0 for g in v.groups):
            weighted += 1
    arm_mods = [mod for mod in m.modifiers if mod.type == 'ARMATURE']
    mod_report.append((m.name, [(mod.name, mod.object.name if mod.object else None) for mod in arm_mods]))
    vgroup_report.append((m.name, vg, weighted, len(me.vertices)))
    say("mesh '%s': verts=%d tris=%d vgroups=%d weighted_verts=%d shape_keys=%s"
        % (m.name, len(me.vertices), len(me.loop_triangles), vg, weighted,
           bool(me.shape_keys)))

say("total verts=%d  total tris=%d" % (total_v, total_t))
for name, vg, w, tv in vgroup_report:
    say("  vertex groups on '%s': %d (%d/%d verts have weight)" % (name, vg, w, tv))
for name, mods in mod_report:
    say("  armature modifiers on '%s': %s" % (name, mods))

say("")
say("--- object transforms (FBX import orientation) ---")
for o in objs:
    r = o.rotation_euler if o.rotation_mode != 'QUATERNION' else o.rotation_quaternion.to_euler()
    say("  %-26s loc=(%.3f, %.3f, %.3f) rot_deg=(%.1f, %.1f, %.1f) scale=(%.3f, %.3f, %.3f)"
        % (o.name, o.location.x, o.location.y, o.location.z,
           math.degrees(r.x), math.degrees(r.y), math.degrees(r.z),
           o.scale.x, o.scale.y, o.scale.z))

# ---------------------------------------------------------------- world bounds
import mathutils

deps = bpy.context.evaluated_depsgraph_get()

# Measure the *evaluated* (post-armature-modifier) geometry: that is what the
# engine actually receives. Mesh local data is Y-up with the Z-up conversion
# baked into the skin bind matrices, so raw vertex coordinates alone mislead.
lo = mathutils.Vector((1e9, 1e9, 1e9))
hi = mathutils.Vector((-1e9, -1e9, -1e9))
say("")
say("--- per-mesh evaluated world bounds (metres) ---")
for m in meshes:
    ob_eval = m.evaluated_get(deps)
    me = ob_eval.to_mesh()
    mw = ob_eval.matrix_world
    mlo = mathutils.Vector((1e9, 1e9, 1e9))
    mhi = mathutils.Vector((-1e9, -1e9, -1e9))
    for v in me.vertices:
        w = mw @ v.co
        for i in range(3):
            mlo[i] = min(mlo[i], w[i])
            mhi[i] = max(mhi[i], w[i])
    say("  %-26s z %.4f .. %.4f  (h=%.4f)  x %.3f..%.3f  y %.3f..%.3f"
        % (m.name, mlo.z, mhi.z, mhi.z - mlo.z, mlo.x, mhi.x, mlo.y, mhi.y))
    for i in range(3):
        lo[i] = min(lo[i], mlo[i])
        hi[i] = max(hi[i], mhi[i])
    ob_eval.to_mesh_clear()

if lo.x > 1e8:
    lo, hi = mathutils.Vector((0, 0, 0)), mathutils.Vector((0, 0, 0))

height = hi.z - lo.z
say("")
say("--- world bounds (metres, Z-up) ---")
say("min = (%.4f, %.4f, %.4f)" % (lo.x, lo.y, lo.z))
say("max = (%.4f, %.4f, %.4f)" % (hi.x, hi.y, hi.z))
say("HEIGHT = %.4f m" % height)
say("width(X) = %.4f  depth(Y) = %.4f" % (hi.x - lo.x, hi.y - lo.y))
say("height in range 1.6-1.9 m: %s" % ("YES" if 1.6 <= height <= 1.9 else "NO"))

say("")
say("--- key bone positions (world, metres) ---")
if arms:
    arm = arms[0]
    for bname in ("root", "head", "wrist.L", "foot.L", "upperleg01.L", "upperarm01.L"):
        pb = arm.pose.bones.get(bname)
        if pb is None:
            continue
        h = arm.matrix_world @ pb.head
        t = arm.matrix_world @ pb.tail
        say("  %-12s head=(%.3f, %.3f, %.3f) tail=(%.3f, %.3f, %.3f)"
            % (bname, h.x, h.y, h.z, t.x, t.y, t.z))
    # arm angle: how far the arm hangs from horizontal => T-pose vs A-pose
    ua = arm.pose.bones.get("upperarm01.L")
    wr = arm.pose.bones.get("wrist.L")
    if ua and wr:
        import math as _m
        a = arm.matrix_world @ ua.head
        b = arm.matrix_world @ wr.head
        horiz = mathutils.Vector((b.x - a.x, b.y - a.y, 0)).length
        dz = a.z - b.z
        ang = _m.degrees(_m.atan2(dz, abs(horiz))) if horiz > 1e-6 else 0.0
        say("  arm droop below shoulder level: %.1f deg (%s)"
            % (ang, "T-pose" if ang < 12 else "A-pose" if ang < 60 else "arms down"))

# ---------------------------------------------------------------- materials
say("")
say("--- materials & textures ---")
say("material count: %d" % len(bpy.data.materials))
missing = []
for mat in bpy.data.materials:
    imgs = []
    if mat.use_nodes and mat.node_tree:
        for n in mat.node_tree.nodes:
            if n.type == 'TEX_IMAGE' and n.image:
                p = bpy.path.abspath(n.image.filepath)
                ok = os.path.exists(p)
                imgs.append((p, ok, n.image.size[0], n.image.size[1]))
                if not ok:
                    missing.append(p)
    say("  %-28s images=%s" % (mat.name, imgs if imgs else "NONE"))
say("missing texture files: %s" % (missing if missing else "none"))

say("")
say("--- material slots per mesh ---")
for m in meshes:
    say("  %-28s slots=%d -> %s" % (m.name, len(m.material_slots),
                                    [s.material.name if s.material else None for s in m.material_slots]))

# ---------------------------------------------------------------- render setup
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = SAMPLES
scene.cycles.use_denoising = True
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.film_transparent = False
scene.view_settings.view_transform = 'Filmic' if 'Filmic' in [
    v.name for v in scene.view_settings.bl_rna.properties['view_transform'].enum_items] else 'Standard'
scene.view_settings.view_transform = 'AgX'
scene.view_settings.look = 'AgX - Medium High Contrast'
scene.view_settings.exposure = -0.9

# world: neutral studio grey
world = bpy.data.worlds.new("VerifyWorld")
scene.world = world
world.use_nodes = True
bg = world.node_tree.nodes["Background"]
bg.inputs[0].default_value = (0.45, 0.47, 0.5, 1.0)
bg.inputs[1].default_value = 0.35

center = (lo + hi) * 0.5
# floor
bpy.ops.mesh.primitive_plane_add(size=max(6.0, height * 6), location=(center.x, center.y, lo.z))
floor = bpy.context.active_object
fm = bpy.data.materials.new("FloorMat")
fm.use_nodes = True
bsdf = fm.node_tree.nodes["Principled BSDF"]
bsdf.inputs["Base Color"].default_value = (0.32, 0.33, 0.35, 1.0)
bsdf.inputs["Roughness"].default_value = 0.75
floor.data.materials.append(fm)

# camera
cam_data = bpy.data.cameras.new("VerifyCam")
cam_data.lens = 55.0
cam_data.sensor_width = 36.0
cam = bpy.data.objects.new("VerifyCam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam

target = mathutils.Vector((center.x, center.y, lo.z + height * 0.52))
dist = height * 1.75

# lights (area, scaled with subject size)
def add_area(name, loc, energy, size):
    ld = bpy.data.lights.new(name, 'AREA')
    ld.energy = energy
    ld.size = size
    ob = bpy.data.objects.new(name, ld)
    scene.collection.objects.link(ob)
    ob.location = loc
    d = (target - mathutils.Vector(loc))
    ob.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    return ob

k = height ** 2 * 8.0
add_area("Key",  (center.x - height * 1.4, center.y - height * 1.7, lo.z + height * 1.9), 900 * k / 6.0, height * 1.2)
add_area("Fill", (center.x + height * 1.8, center.y - height * 1.2, lo.z + height * 1.3), 320 * k / 6.0, height * 1.4)
add_area("Rim",  (center.x + height * 0.6, center.y + height * 2.0, lo.z + height * 1.8), 700 * k / 6.0, height * 1.0)


def shoot(name, azimuth_deg):
    """azimuth 0 = straight in front of the model on -Y."""
    a = math.radians(azimuth_deg)
    cam.location = mathutils.Vector((
        center.x + dist * math.sin(a),
        center.y - dist * math.cos(a),
        lo.z + height * 0.55,
    ))
    d = target - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    path = os.path.join(OUTDIR, name)
    scene.render.filepath = path
    scene.render.image_settings.file_format = 'PNG'
    bpy.ops.render.render(write_still=True)
    say("rendered %s" % path)
    return path


say("")
say("--- renders ---")
p1 = shoot("Pilot_Human_front.png", YAW + 0.0)
p2 = shoot("Pilot_Human_three_quarter.png", YAW + 40.0)

# also save a working blend for inspection
blend = os.path.join(OUTDIR, "Pilot_Human_verify.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend)
say("saved %s" % blend)

with open(REPORT, "w", encoding="utf-8") as f:
    f.write("\n".join(lines) + "\n")

print("REPORT WRITTEN:", REPORT)
