# -*- coding: utf-8 -*-
"""
MakeHuman 1.2.0 automation plugin - builds ONE realistic adult male "pilot" and
exports FBX with skeleton + weights.

Why this shape: MakeHuman 1.2.0 has NO --nogui / --script CLI options (see
`parse_arguments()` in makehuman.py - only --version/--license/--noshaders/...).
The supported automation hook is the plugin API: any module in the *user*
plugins folder that is listed in settings.ini -> "activeUserPlugins" gets its
`load(app)` called at startup inside the running application. We use that, then
quit the app when the export is finished.

Install (done by ArtSource/Blender/makehuman_pilot_run.ps1):
  copy this file to  <MH_HOME>/makehuman/v1py3/plugins/pilot_export.py
  set  "activeUserPlugins": ["pilot_export"]  in <MH_HOME>/makehuman/v1py3/settings.ini
"""

import os
import sys
import traceback

import gui3d
import mh
import log

# --------------------------------------------------------------------------
# Paths are injected by the runner via environment variables so the tracked
# copy of this file stays machine independent.
# --------------------------------------------------------------------------
REPO = os.environ.get("LL_REPO_ROOT", "").replace("\\", "/")
MH_ROOT = os.environ.get("LL_MH_ROOT", "").replace("\\", "/")

OUT_FBX = (os.environ.get("LL_PILOT_FBX") or (REPO + "/ArtSource/Exports/Characters/Pilot_Human.fbx")).replace("\\", "/")
RUN_LOG = (os.environ.get("LL_PILOT_LOG") or (REPO + "/ArtSource/Notes/makehuman_pilot_run.log")).replace("\\", "/")

_lines = []


def say(msg):
    _lines.append(str(msg))
    try:
        log.message(str(msg))
    except Exception:
        pass
    try:
        print(msg)
    except Exception:
        pass


def flush():
    try:
        os.makedirs(os.path.dirname(RUN_LOG), exist_ok=True)
        with open(RUN_LOG, "w", encoding="utf-8") as f:
            f.write("\n".join(_lines) + "\n")
    except Exception:
        pass


# --------------------------------------------------------------------------
# load() is called by MakeHuman during startup for every active user plugin.
# --------------------------------------------------------------------------
def load(app):
    from PyQt5.QtCore import QTimer
    # Give the application time to finish building the default human/scene
    # before we start replacing everything.
    QTimer.singleShot(2500, lambda: _guarded(app))


def unload(app):
    pass


def _guarded(app):
    try:
        _run(app)
    except Exception:
        say("FATAL\n" + traceback.format_exc())
    finally:
        flush()
        try:
            app.quit()
        except Exception:
            os._exit(0)


def _run(app):
    import importlib
    import numpy as np

    from shared import skeleton as mhskeleton
    from shared import proxy as mhproxy
    from shared import material as mhmaterial

    human = app.selectedHuman
    data = MH_ROOT + "/data"

    human.setSubdivided(False)
    say("human object: %s" % human)
    say("subdivided: %s" % human.isSubdivided())

    # ---------------------------------------------------------------- body
    # Gender: 0 = female, 1 = male.  Age: value/1.0 == years/70.
    human.setGender(1.0)
    human.setAgeYears(31)
    human.setMuscle(0.38)
    human.setWeight(0.46)
    human.setBodyProportions(0.55)
    human.setHeight(0.55)
    human.setCaucasian(1.0)
    human.applyAllTargets()

    say("gender=%.3f age=%.3f (~%.1f yr) muscle=%.2f weight=%.2f height=%.2f -> %.3f m"
        % (human.getGender(), human.getAge(), human.getAgeYears(),
           human.getMuscle(), human.getWeight(), human.getHeight(), human.getHeightCm() / 100.0))
    say("weight kg = %.1f" % human.getWeightKg())

    # ---------------------------------------------------------------- skin
    skin = data + "/skins/young_caucasian_male/young_caucasian_male.mhmat"
    if not os.path.isfile(skin):
        say("WARNING: skin material not found: %s" % skin)
    mat = mhmaterial.fromFile(skin)
    human.setMaterial(mat)
    say("skin material: %s (name=%s)" % (skin, getattr(mat, "name", "?")))

    # ---------------------------------------------------------------- rig
    base = human.getBaseSkeleton()
    if base is None:
        say("no base skeleton at startup - loading default.mhskel as base")
        base = mhskeleton.load(data + "/rigs/default.mhskel", human.meshData)
        human.setBaseSkeleton(base)

    rig_path = data + "/rigs/default.mhskel"
    skel = mhskeleton.load(rig_path, human.meshData)
    skel.autoBuildWeightReferences(base)
    vw = skel.getVertexWeights(base.getVertexWeights(), force_remap=False)
    skel.addReferencePlanes(base)
    human.setSkeleton(skel)

    skel = human.getSkeleton()
    say("rig: %s bones=%d maxWeightsPerVertex=%s"
        % (rig_path, skel.getBoneCount(), vw.getMaxNumberVertexWeights()))
    say("bone names: %s" % ", ".join(b.name for b in skel.getBones()))

    # ---------------------------------------------------------------- clothes
    # Casual modern clothing from the stock asset set (CC0).
    # The GUI's ProxyChooser does four things per proxy; we must replicate them
    # or human.addClothesProxy() dies on proxy.object == None:
    #   loadProxy -> loadMeshAndObject -> app.addObject -> pxy.update(seedMesh)
    def attach(path, ptype):
        pxy = mhproxy.loadProxy(human, path, ptype)
        if pxy is None:
            say("FAILED to load proxy: %s" % path)
            return None
        mesh, obj = pxy.loadMeshAndObject(human)
        if not mesh:
            say("FAILED to build proxy object: %s" % path)
            return None
        mesh.setPickable(True)
        app.addObject(obj)
        seed = obj.getSeedMesh()
        pxy.update(seed, False)
        seed.update()
        say("attached proxy: %s (name=%s)" % (os.path.basename(path), pxy.name))
        return pxy

    for path in [data + "/clothes/male_casualsuit01/male_casualsuit01.mhclo",
                 data + "/clothes/shoes01/shoes01.mhclo"]:
        pxy = attach(path, "Clothes")
        if pxy is not None:
            human.addClothesProxy(pxy)

    hair_pxy = attach(data + "/hair/short01/short01.mhclo", "Hair")
    if hair_pxy is not None:
        human.setHairProxy(hair_pxy)

    say("clothes proxies: %d" % len(human.clothesProxies))
    say("total proxies: %d" % len(human.getProxies(includeHumanProxy=False)))

    # Clothing proxies carry a face mask that hides the body surface they cover.
    # The GUI recomputes it on every proxy change; doing it by hand is required
    # or the bare feet render straight through the shoes.
    try:
        human.maskFaces()
        masked = int(sum(1 for m in human.meshData.face_mask if not m))
        say("face mask recomputed: %d body faces hidden" % masked)
    except Exception:
        say("maskFaces failed:\n" + traceback.format_exc())

    # ---------------------------------------------------------------- export
    # The FBX exporter writes TransparencyFactor = mat.transparencyMapIntensity,
    # which defaults to 1.0. Blender (and Unity) then treat every material as
    # fully transparent/alpha-hashed, which makes the shoes vanish entirely.
    # These garments are opaque, so neutralise the transparency fields.
    for obj in human.getObjects():
        m = obj.material
        if m is None:
            continue
        m.transparent = False
        m.alphaToCoverage = False
        m.opacity = 1.0
        m.transparencyMapIntensity = 0.0
        m.transparencyMapTexture = None
    say("materials forced opaque: %d"
        % len([o.material for o in human.getObjects() if o.material is not None]))

    fbx = importlib.import_module("9_export_fbx")
    cfg = fbx.FbxConfig()
    cfg.setHuman(human)
    cfg.feetOnGround = True
    cfg.scale = 0.1          # 'meter' in the GUI scale table
    cfg.unit = "m"
    cfg.scale *= 10.0        # exporter applies this itself in getConfig()
    cfg.binary = True
    cfg.useMaterials = True
    cfg.useRelPaths = False  # absolute texture paths in the FBX
    cfg.hiddenGeom = False

    os.makedirs(os.path.dirname(OUT_FBX), exist_ok=True)
    say("exporting FBX -> %s (scale=%.3f unit=%s)" % (OUT_FBX, cfg.scale, cfg.unit))
    mh2fbx = importlib.import_module("9_export_fbx.mh2fbx")
    mh2fbx.exportFbx(OUT_FBX, cfg)
    say("FBX written: %s (%d bytes)" % (OUT_FBX, os.path.getsize(OUT_FBX) if os.path.isfile(OUT_FBX) else -1))

    texdir = os.path.join(os.path.dirname(OUT_FBX), "textures")
    if os.path.isdir(texdir):
        say("textures copied: %s" % sorted(os.listdir(texdir)))
    else:
        say("WARNING: no textures folder produced")

    # save the .mhm so the character can be reopened/re-tweaked in the GUI
    try:
        mhm = os.path.join(os.path.dirname(OUT_FBX), "Pilot_Human.mhm")
        human.save(mhm)
        say("saved mhm: %s" % mhm)
    except Exception:
        say("could not save mhm:\n" + traceback.format_exc())
