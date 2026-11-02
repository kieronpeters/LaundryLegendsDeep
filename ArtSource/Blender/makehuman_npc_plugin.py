# -*- coding: utf-8 -*-
"""
MakeHuman 1.2.0 automation plugin - builds ONE realistic NPC from a JSON spec
and exports FBX with skeleton + weights + textures.

This is the same harness shape as `makehuman_pilot_plugin.py` (which produced the
verified Pilot_Human.fbx), but the body / face / asset choices come from a spec
file so the same code can produce several distinct characters.

Why a plugin at all: MakeHuman 1.2.0 has NO --nogui / --script CLI options. The
supported automation hook is the plugin API - any module in the *user* plugins
folder listed in settings.ini -> "activeUserPlugins" gets `load(app)` called at
startup inside the running application. We use that, then quit the app.

Installed by ArtSource/Blender/makehuman_npc_run.ps1 as:
  <MH_HOME>/makehuman/v1py3/plugins/npc_export.py

Environment contract (all injected by the runner):
  LL_REPO_ROOT  repo root
  LL_MH_ROOT    Tools/MakeHuman/makehuman
  LL_NPC_SPEC   path to the character spec JSON
  LL_NPC_FBX    output FBX path
  LL_NPC_LOG    output run log path
"""

import os
import json
import traceback

import gui3d
import mh
import log

REPO = os.environ.get("LL_REPO_ROOT", "").replace("\\", "/")
MH_ROOT = os.environ.get("LL_MH_ROOT", "").replace("\\", "/")

SPEC_PATH = (os.environ.get("LL_NPC_SPEC") or "").replace("\\", "/")
OUT_FBX = (os.environ.get("LL_NPC_FBX") or "").replace("\\", "/")
RUN_LOG = (os.environ.get("LL_NPC_LOG") or (REPO + "/ArtSource/Notes/makehuman_npc_run.log")).replace("\\", "/")

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


# --------------------------------------------------------------------------
# helpers
# --------------------------------------------------------------------------
def _resolve(path):
    """Accept a repo-relative or MH-data-relative asset path; return absolute."""
    p = (path or "").replace("\\", "/")
    if os.path.isabs(p):
        return p
    if p.startswith("data/"):
        return MH_ROOT + "/" + p
    return REPO + "/" + p


def _set_modifier(human, name, value):
    """Set one modeling modifier by full name, tolerating unknown names."""
    try:
        if name not in human.modifierNames:
            say("WARNING: unknown modifier '%s' - skipped" % name)
            return False
        human.getModifier(name).setValue(float(value))
        return True
    except Exception:
        say("WARNING: could not set modifier '%s':\n%s" % (name, traceback.format_exc()))
        return False


def _solve_height(human, target_cm):
    """Drive the macrodetails-height/Height slider until the mesh is target_cm tall.

    getHeightCm() measures the actual basemesh bounding box, so a two-point
    linear calibration plus Newton refinement lands within a millimetre-ish
    without assuming MakeHuman's slider-to-metre mapping.
    """
    sample_lo, sample_hi = 0.15, 0.90

    def at(v):
        human.setHeight(v)
        return human.getHeightCm()

    h_lo, h_hi = at(sample_lo), at(sample_hi)
    slope = (h_hi - h_lo) / (sample_hi - sample_lo)
    if abs(slope) < 1e-6:
        say("WARNING: height slider has no effect (%.1f .. %.1f cm); leaving as-is" % (h_lo, h_hi))
        return human.getHeight(), human.getHeightCm()

    v = sample_lo + (target_cm - h_lo) / slope
    v = max(0.0, min(1.0, v))
    for _ in range(4):
        cm = at(v)
        if abs(cm - target_cm) <= 0.3:
            break
        v = max(0.0, min(1.0, v + (target_cm - cm) / slope))

    human.setHeight(v)
    return v, human.getHeightCm()


def _run(app):
    import importlib

    from shared import skeleton as mhskeleton
    from shared import proxy as mhproxy
    from shared import material as mhmaterial

    with open(SPEC_PATH, "r", encoding="utf-8") as f:
        spec = json.load(f)

    name = spec["name"]
    human = app.selectedHuman
    data = MH_ROOT + "/data"

    human.setSubdivided(False)
    say("=== %s ===" % name)
    say("spec: %s" % SPEC_PATH)
    say("human object: %s  subdivided=%s" % (human, human.isSubdivided()))

    # ---------------------------------------------------------------- body
    human.setGender(float(spec.get("gender", 1.0)))
    human.setAgeYears(int(spec.get("ageYears", 30)))
    human.setMuscle(float(spec.get("muscle", 0.4)))
    human.setWeight(float(spec.get("weight", 0.45)))
    human.setBodyProportions(float(spec.get("bodyProportions", 0.5)))

    eth = spec.get("ethnicity", {})
    human.setAfrican(float(eth.get("african", 0.0)))
    human.setAsian(float(eth.get("asian", 0.0)))
    human.setCaucasian(float(eth.get("caucasian", 0.0)))
    human.applyAllTargets()

    height_slider, height_cm = _solve_height(human, float(spec["heightCm"]))
    say("body: gender=%.2f age=%.2f (~%.0f yr) muscle=%.2f weight=%.2f props=%.2f"
        % (human.getGender(), human.getAge(), human.getAgeYears(),
           human.getMuscle(), human.getWeight(), human.getBodyProportions()))
    say("ethnicity: african=%.2f asian=%.2f caucasian=%.2f"
        % (human.getAfrican(), human.getAsian(), human.getCaucasian()))
    say("height: slider=%.4f -> %.2f cm (target %.1f)  weight=%.1f kg"
        % (height_slider, height_cm, float(spec["heightCm"]), human.getWeightKg()))

    # ---------------------------------------------------------------- face
    # Modeling modifiers are applied BEFORE the rig is built, so the skeleton
    # and the vertex weights are generated from the final face shape.
    applied = 0
    for mod_name, mod_value in (spec.get("modifiers") or {}).items():
        if _set_modifier(human, mod_name, mod_value):
            applied += 1
    human.applyAllTargets()
    say("face/torso modifiers applied: %d/%d"
        % (applied, len(spec.get("modifiers") or {})))

    # ---------------------------------------------------------------- skin
    skin = _resolve(spec["skin"])
    if not os.path.isfile(skin):
        say("WARNING: skin material not found: %s" % skin)
    mat = mhmaterial.fromFile(skin)
    human.setMaterial(mat)
    say("skin material: %s (name=%s)" % (skin, getattr(mat, "name", "?")))

    # ---------------------------------------------------------------- rig
    # Identical to the verified pilot rig: default.mhskel, weights built against
    # the base skeleton, reference planes added. Do not change - this is the rig
    # that already imported into Unity as a valid humanoid.
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

    # ---------------------------------------------------------------- clothes
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
        say("attached proxy: %s (type=%s name=%s)"
            % (os.path.basename(path), ptype, pxy.name))
        return pxy

    for rel in spec.get("clothes", []):
        path = _resolve(rel)
        if not os.path.isfile(path):
            alt = path[:-6] + ".mhpxy" if path.endswith(".mhclo") else path
            if os.path.isfile(alt):
                path = alt
        if not os.path.isfile(path):
            say("MISSING clothes asset: %s" % path)
            continue
        pxy = attach(path, "Clothes")
        if pxy is not None:
            human.addClothesProxy(pxy)

    hair_pxy = None
    if spec.get("hair"):
        hair_path = _resolve(spec["hair"])
        if not os.path.isfile(hair_path) and hair_path.endswith(".mhclo"):
            alt = hair_path[:-6] + ".mhpxy"
            if os.path.isfile(alt):
                hair_path = alt
        if os.path.isfile(hair_path):
            hair_pxy = attach(hair_path, "Hair")
            if hair_pxy is not None:
                human.setHairProxy(hair_pxy)
        else:
            say("MISSING hair asset: %s" % hair_path)

    say("clothes proxies: %d" % len(human.clothesProxies))
    say("total proxies: %d" % len(human.getProxies(includeHumanProxy=False)))

    # Clothing proxies carry a face mask that hides the body surface they cover.
    # The GUI recomputes it on every proxy change; doing it by hand is required
    # or the bare feet render straight through the shoes.
    try:
        human.maskFaces()
        # maskFaces only hides the static helper/joint faces. Apply each garment's
        # own deleteVerts mask too, or the naked body keeps rendering under the
        # clothes and z-fights through close-fitting garments (skin "tears").
        import numpy as _np
        vertsMask = _np.ones(human.meshData.getVertexCount(), dtype=bool)
        for pxy in human.clothesProxies.values():
            dv = getattr(pxy, "deleteVerts", None)
            if dv is not None and _np.any(dv):
                vertsMask[dv] = False
        human.changeVertexMask(vertsMask)
        masked = int(sum(1 for m in human.meshData.face_mask if not m))
        say("face mask recomputed: %d body faces hidden" % masked)
    except Exception:
        say("maskFaces failed:\n" + traceback.format_exc())

    # ---------------------------------------------------------------- export
    # The FBX exporter writes TransparencyFactor = mat.transparencyMapIntensity,
    # which defaults to 1.0. Any engine that honours it treats the garment as
    # fully transparent. These garments are opaque, so neutralise the fields.
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
    say("FBX written: %s (%d bytes)"
        % (OUT_FBX, os.path.getsize(OUT_FBX) if os.path.isfile(OUT_FBX) else -1))

    texdir = os.path.join(os.path.dirname(OUT_FBX), "textures")
    if os.path.isdir(texdir):
        say("textures present in export dir: %s" % sorted(os.listdir(texdir)))
    else:
        say("WARNING: no textures folder produced")
