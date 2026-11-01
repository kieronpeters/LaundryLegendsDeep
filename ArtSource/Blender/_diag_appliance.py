"""Diagnostic: render ApplianceHD.blend with flat, untextured materials.

Run:
  blender.exe -b ArtSource/Blender/ApplianceHD.blend -P ArtSource/Blender/_diag_appliance.py
"""

import math
import os

import bpy
from mathutils import Vector

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(PROJECT_ROOT, "ArtSource", "Exports", "_diag")


def main():
    os.makedirs(OUT, exist_ok=True)
    mat = bpy.data.materials.new("DiagFlat")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.62, 0.63, 0.65, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.45
    bsdf.inputs["Metallic"].default_value = 0.0

    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            obj.data.materials.clear()
            obj.data.materials.append(mat)

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 1000
    scene.render.resolution_y = 800

    data = bpy.data.cameras.new("DiagCam")
    data.lens = 55.0
    cam = bpy.data.objects.new("DiagCam", data)
    bpy.context.scene.collection.objects.link(cam)
    cam.location = (0.0, -2.45, 0.66)
    direction = Vector((0.0, 0.0, 0.575)) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam

    light = bpy.data.lights.new("DiagLight", type="AREA")
    light.size = 2.5
    light.energy = 900.0
    lo = bpy.data.objects.new("DiagLight", light)
    bpy.context.scene.collection.objects.link(lo)
    lo.location = (-2.2, -2.6, 3.2)
    lo.rotation_euler = (Vector((0, 0, 0.6)) - lo.location).to_track_quat("-Z", "Y").to_euler()

    bpy.ops.mesh.primitive_plane_add(size=14.0, location=(0, 0, -0.002))
    floor = bpy.context.active_object
    floor.data.materials.append(mat)
    bpy.ops.mesh.primitive_plane_add(size=14.0, location=(0, 1.95, 2.6),
                                     rotation=(math.radians(90), 0, 0))
    bpy.context.active_object.data.materials.append(mat)

    scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.3, 0.32, 0.35, 1)
    scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.6

    scene.render.filepath = os.path.join(OUT, "flat_front.png")
    bpy.ops.render.render(write_still=True)

    # Close-up of the front plate to see whether the wedges are geometry.
    cam.location = (0.0, -1.15, 0.86)
    cam.rotation_euler = (Vector((0.0, 0.0, 0.82)) - cam.location).to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = 70.0
    scene.render.filepath = os.path.join(OUT, "flat_closeup.png")
    bpy.ops.render.render(write_still=True)

    print("[Diag] done")


main()
