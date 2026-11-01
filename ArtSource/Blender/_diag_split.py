"""Diagnostic: render BundleCloth alone and BundleFilm alone from the saved blend."""
import bpy
import os

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND = os.path.join(PROJECT_ROOT, "ArtSource", "Blender", "WrappedBundle.blend")
OUT = os.path.join(PROJECT_ROOT, "ArtSource", "Exports", "_diag")

bpy.ops.wm.open_mainfile(filepath=BLEND)
os.makedirs(OUT, exist_ok=True)
scene = bpy.context.scene
scene.cycles.samples = 64
scene.render.resolution_x = 700
scene.render.resolution_y = 560

cam = bpy.data.objects["Cam_bundle_front"]
scene.camera = cam

cloth = bpy.data.objects["BundleCloth"]
film = bpy.data.objects["BundleFilm"]

# 1. cloth only
film.hide_render = True
cloth.hide_render = False
scene.render.filepath = os.path.join(OUT, "diag_cloth_only.png")
bpy.ops.render.render(write_still=True)

# 2. film only
film.hide_render = False
cloth.hide_render = True
scene.render.filepath = os.path.join(OUT, "diag_film_only.png")
bpy.ops.render.render(write_still=True)

# 3. report material node graph of the film for the record
mat = bpy.data.materials["MAT_BundleFilm"]
for node in mat.node_tree.nodes:
    if node.type == "MIX_SHADER":
        fac = node.inputs["Fac"].default_value
        linked = node.inputs["Fac"].is_linked
        print(f"[Diag] MixShader fac={fac:.3f} linked={linked}")

print("[Diag] done")
