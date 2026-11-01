import bpy, sys, os, math, mathutils, numpy as np

argv = sys.argv
argv = argv[argv.index("--") + 1:] if "--" in argv else []
FBX = os.path.abspath(argv[argv.index("--fbx") + 1])
OUT = os.path.abspath(argv[argv.index("--out") + 1])
PNG = os.path.abspath(argv[argv.index("--png") + 1])

img = bpy.data.images.load(PNG)
w, h = img.size
a = np.empty(w * h * 4, dtype=np.float32)
img.pixels.foreach_get(a)
al = a[3::4]
print("TEXTURE %s %dx%d alpha min=%.3f max=%.3f mean=%.3f" % (os.path.basename(PNG), w, h, al.min(), al.max(), al.mean()))

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)

deps = bpy.context.evaluated_depsgraph_get()
shoe = bpy.data.objects.get("shoes01Mesh")
print("SHOE object:", shoe, "verts", len(shoe.data.vertices), "polys", len(shoe.data.polygons))
ev = shoe.evaluated_get(deps)
me = ev.to_mesh()
lo = mathutils.Vector((1e9,) * 3); hi = mathutils.Vector((-1e9,) * 3)
for v in me.vertices:
    wv = ev.matrix_world @ v.co
    lo = mathutils.Vector((min(lo[i], wv[i]) for i in range(3)))
    hi = mathutils.Vector((max(hi[i], wv[i]) for i in range(3)))
print("SHOE bounds", tuple(round(x, 3) for x in lo), tuple(round(x, 3) for x in hi))
# normal direction sanity: average normal z of shoe sole
zs = [p.normal.z for p in shoe.data.polygons]
for _m in bpy.data.materials:
    if False:
        continue
    print("MAT:", _m.name)
    print("SHOE MATERIAL nodes:", [n.type for n in _m.node_tree.nodes])
    for _n in _m.node_tree.nodes:
        if _n.type == "BSDF_PRINCIPLED":
            for _k in ("Base Color", "Metallic", "Roughness", "IOR", "Alpha", "Transmission Weight", "Specular IOR Level", "Coat Weight"):
                _i = _n.inputs.get(_k)
                if _i is not None:
                    _v = _i.default_value
                    try:
                        _v = tuple(round(float(x), 3) for x in _v)
                    except TypeError:
                        _v = round(float(_v), 3)
                    print("   %-22s %s linked=%s" % (_k, _v, _i.is_linked))
print("SHOE polygon normal z mean=%.3f min=%.2f max=%.2f" % (sum(zs) / len(zs), min(zs), max(zs)))
ev.to_mesh_clear()

# hide everything except the shoe, render it alone
for o in bpy.context.scene.objects:
    if o.type == 'MESH' and o.name != "shoes01Mesh":
        o.hide_render = True

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.cycles.device = 'CPU'
scene.render.resolution_x = 600
scene.render.resolution_y = 600
scene.view_settings.view_transform = 'AgX'
world = bpy.data.worlds.new("W"); scene.world = world; world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.5, 0.5, 0.55, 1.0)

cam_data = bpy.data.cameras.new("C"); cam_data.lens = 60
cam = bpy.data.objects.new("C", cam_data); scene.collection.objects.link(cam); scene.camera = cam
t = (lo + hi) * 0.5
cam.location = t + mathutils.Vector((0.45, -0.55, 0.30))
cam.rotation_euler = (t - cam.location).to_track_quat('-Z', 'Y').to_euler()
ld = bpy.data.lights.new("K", 'AREA'); ld.energy = 260; ld.size = 0.7
lo_obj = bpy.data.objects.new("K", ld); scene.collection.objects.link(lo_obj)
lo_obj.location = t + mathutils.Vector((0.7, -0.9, 1.1))
lo_obj.rotation_euler = (t - lo_obj.location).to_track_quat('-Z', 'Y').to_euler()
scene.render.filepath = os.path.join(OUT, "_diag_pilot_shoe_only.png")
bpy.ops.render.render(write_still=True)
print("rendered shoe only")
