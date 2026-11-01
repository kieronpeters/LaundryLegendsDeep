import bpy, sys, os, math, mathutils

argv = sys.argv
argv = argv[argv.index("--") + 1:] if "--" in argv else []
FBX = os.path.abspath(argv[argv.index("--fbx") + 1])
OUT = os.path.abspath(argv[argv.index("--out") + 1])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)

# keep only the shoe mesh
for o in list(bpy.context.scene.objects):
    if o.type == 'MESH' and o.name != "shoes01Mesh":
        bpy.data.objects.remove(o, do_unlink=True)

shoe = bpy.data.objects["shoes01Mesh"]
me = shoe.data
lo = mathutils.Vector((1e9,) * 3); hi = mathutils.Vector((-1e9,) * 3)
for v in me.vertices:
    w = shoe.matrix_world @ v.co
    lo = mathutils.Vector((min(lo[i], w[i]) for i in range(3)))
    hi = mathutils.Vector((max(hi[i], w[i]) for i in range(3)))

# flat opaque override
flat = bpy.data.materials.new("FLAT")
flat.use_nodes = True
bsdf = flat.node_tree.nodes["Principled BSDF"]
bsdf.inputs["Base Color"].default_value = (0.8, 0.2, 0.2, 1.0)
bsdf.inputs["Roughness"].default_value = 0.6
bsdf.inputs["Metallic"].default_value = 0.0
for o in bpy.context.scene.objects:
    if o.type == 'MESH':
        o.data.materials.clear()
        o.data.materials.append(flat)

# backface culling test: is the sock geometry outside the leather?
print("verts", len(me.vertices), "polys", len(me.polygons),
      "materials-after", [m.name for m in bpy.data.materials])

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 32
scene.cycles.device = 'CPU'
scene.render.resolution_x = 600
scene.render.resolution_y = 600
scene.view_settings.view_transform = 'Standard'
w = bpy.data.worlds.new("W"); scene.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.5, 0.5, 0.55, 1.0)
w.node_tree.nodes["Background"].inputs[1].default_value = 0.8

cam_data = bpy.data.cameras.new("C"); cam_data.lens = 60
cam = bpy.data.objects.new("C", cam_data); scene.collection.objects.link(cam); scene.camera = cam
t = (lo + hi) * 0.5
cam.location = t + mathutils.Vector((0.40, -0.50, 0.28))
cam.rotation_euler = (t - cam.location).to_track_quat('-Z', 'Y').to_euler()
ld = bpy.data.lights.new("K", 'AREA'); ld.energy = 180; ld.size = 0.8
ob = bpy.data.objects.new("K", ld); scene.collection.objects.link(ob)
ob.location = t + mathutils.Vector((0.6, -0.8, 1.0))
ob.rotation_euler = (t - ob.location).to_track_quat('-Z', 'Y').to_euler()
scene.render.filepath = os.path.join(OUT, "_diag_pilot_shoe_flat.png")
bpy.ops.render.render(write_still=True)
print("rendered flat shoe")
