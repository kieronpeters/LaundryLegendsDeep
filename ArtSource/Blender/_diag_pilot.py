"""Diagnostic close-ups + material dump for the pilot FBX."""
import bpy, sys, os, math, mathutils

argv = sys.argv
argv = argv[argv.index("--") + 1:] if "--" in argv else []
FBX = os.path.abspath(argv[argv.index("--fbx") + 1])
OUT = os.path.abspath(argv[argv.index("--out") + 1])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)

print("\n=== MATERIAL DUMP ===")
for mat in bpy.data.materials:
    print("MATERIAL", mat.name, "blend=", getattr(mat, "blend_method", "?"),
          "alpha=", getattr(mat, "alpha_threshold", "?"))
    if mat.use_nodes:
        for n in mat.node_tree.nodes:
            if n.type == 'BSDF_PRINCIPLED':
                print("   alpha_linked=", n.inputs["Alpha"].is_linked)
                bc = n.inputs["Base Color"]
                alpha = n.inputs["Alpha"].default_value
                print("   base_color", tuple(round(x, 3) for x in bc.default_value),
                      "linked=", bc.is_linked, "alpha=", round(alpha, 3))
            if n.type == 'TEX_IMAGE' and n.image:
                print("   tex", os.path.basename(bpy.path.abspath(n.image.filepath)),
                      n.image.size[0], "x", n.image.size[1])

print("\n=== MESH DUMP ===")
for o in bpy.context.scene.objects:
    if o.type != 'MESH':
        continue
    o.data.calc_loop_triangles()
    print("MESH", o.name, "verts", len(o.data.vertices), "tris", len(o.data.loop_triangles),
          "polys", len(o.data.polygons), "mats", [s.material.name if s.material else None for s in o.material_slots],
          "uv", [u.name for u in o.data.uv_layers], "attrs", [a.name for a in o.data.color_attributes])
    print("   world matrix translation", tuple(round(v, 4) for v in o.matrix_world.translation),
          "scale", tuple(round(v, 4) for v in o.matrix_world.to_scale()))

# ---- render close-ups on evaluated geometry ----
deps = bpy.context.evaluated_depsgraph_get()
lo = mathutils.Vector((1e9,) * 3)
hi = mathutils.Vector((-1e9,) * 3)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
for m in meshes:
    ev = m.evaluated_get(deps)
    me = ev.to_mesh()
    for v in me.vertices:
        w = ev.matrix_world @ v.co
        lo = mathutils.Vector((min(lo[i], w[i]) for i in range(3)))
        hi = mathutils.Vector((max(hi[i], w[i]) for i in range(3)))
    ev.to_mesh_clear()
H = hi.z - lo.z

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 40
scene.cycles.use_denoising = True
scene.render.resolution_x = 700
scene.render.resolution_y = 700
scene.view_settings.view_transform = 'AgX'

world = bpy.data.worlds.new("W"); scene.world = world; world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.45, 0.47, 0.5, 1.0)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.4

cam_data = bpy.data.cameras.new("C"); cam_data.lens = 85.0
cam = bpy.data.objects.new("C", cam_data); scene.collection.objects.link(cam); scene.camera = cam

def add_area(name, loc, energy, size, target):
    ld = bpy.data.lights.new(name, 'AREA'); ld.energy = energy; ld.size = size
    ob = bpy.data.objects.new(name, ld); scene.collection.objects.link(ob)
    ob.location = loc
    ob.rotation_euler = (mathutils.Vector(target) - mathutils.Vector(loc)).to_track_quat('-Z', 'Y').to_euler()

def shot(name, target, dist, azimuth=25.0):
    t = mathutils.Vector(target)
    for o in [o for o in scene.objects if o.type == 'LIGHT']:
        bpy.data.objects.remove(o, do_unlink=True)
    a = math.radians(azimuth)
    cam.location = t + mathutils.Vector((dist * math.sin(a), -dist * math.cos(a), dist * 0.25))
    cam.rotation_euler = (t - cam.location).to_track_quat('-Z', 'Y').to_euler()
    add_area("K", (t.x - dist, t.y - dist * 1.5, t.z + dist), 220 * dist * dist, dist, t)
    add_area("F", (t.x + dist * 1.6, t.y - dist, t.z + dist * 0.4), 120 * dist * dist, dist, t)
    add_area("R", (t.x, t.y + dist * 1.8, t.z + dist), 160 * dist * dist, dist, t)
    scene.render.filepath = os.path.join(OUT, name)
    bpy.ops.render.render(write_still=True)
    print("rendered", scene.render.filepath)

shot("_diag_pilot_head.png", (hi.x * 0.0, 0.0, lo.z + H - 0.10), 0.55, 18.0)
shot("_diag_pilot_feet.png", (0.16, 0.0, lo.z + 0.10), 0.45, 30.0)
for mat in bpy.data.materials:
    try:
        mat.blend_method = 'OPAQUE'
    except Exception:
        pass
shot("_diag_pilot_feet_opaque.png", (0.16, 0.0, lo.z + 0.10), 0.45, 30.0)
shot("_diag_pilot_torso.png", (0.0, 0.0, lo.z + H * 0.62), 0.9, 15.0)
