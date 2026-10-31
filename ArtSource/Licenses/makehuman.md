# MakeHuman — engine licence and install record

- **Recorded:** 2026-11-01
- **Scope:** MakeHuman is used **locally, on this machine, as an authoring tool** to build humanoid
  base meshes that are then exported to FBX and reworked in Blender. No MakeHuman binary is part of
  the game build, and nothing under `Tools/MakeHuman/` is committed (see `.gitignore` line 113).

## 1. Install record

| Field | Value |
|---|---|
| Project | MakeHuman Community (`makehumancommunity/makehuman`) |
| Version | **1.2.0 final**, basemesh `hm08`, bundled Python 3.7.6 |
| Release page | http://www.makehumancommunity.org/content/makehuman_120.html |
| Download | http://download.tuxfamily.org/makehuman/releases/makehuman-community-1.2.0-windows.zip |
| Mirror | https://sourceforge.net/projects/makehuman.mirror/files/makehuman/v1.2.0/ |
| Size | 351,434,681 bytes (335.1 MiB) |
| SHA-256 | `7f801801f4307a89c8dc91e5713553000a37d1225f25cb1c32afad20f3b0f4ee` |
| Zip contents | 7 entries; all CRC-verified on 2026-11-01 |
| Local zip | `ArtSource/Downloads/makehuman-community-1.2.0-windows.zip` (git-ignored) |
| Installed at | `Tools/MakeHuman/` (git-ignored) |
| Licence | App **AGPLv3**; bundled graphical assets **CC0** (per the binary's own `--license` text) |

The SHA-256 above is independently confirmed: the Chocolatey `MakeHuman` 1.2.0 package pins the same
value for the same URL, so the downloaded archive is the authentic official build.

## 2. This is not a "portable" build — important

MakeHuman's Windows distribution is a **zip that contains an installer**, not a portable tree:

```
makehuman-community-1.2.0-windows.zip
├── makehuman-community-1.2.0-windows.exe   <- NSIS installer (341 MB)
├── README.txt
└── addons_for_blender_28x/                 <- MPFB, MakeClothes2, MakeTarget2, MakeSkin
```

`Tools/MakeHuman/` holds the **extracted installer payload**, which keeps the app self-contained and
avoids touching the registry, Start Menu, or `Program Files`:

```
Tools/MakeHuman/
├── Python/python.exe      <- embedded Python 3.7.6 (the actual executable)
├── makehuman/             <- application source (makehuman.py is the entry point)
├── pkgs/                  <- numpy, PyQt5 5.12.3, PyOpenGL, sip
├── licenses/
└── mhstartwrapper.py
```

**There is no `makehuman.exe`.** Any automation must call `Python\python.exe makehuman.py`.

## 3. Version and launch checks (2026-11-01)

| Check | Command | Result |
|---|---|---|
| Version | `Python\python.exe makehuman.py --version` (cwd `Tools\MakeHuman\makehuman`) | exit 0, banner printed |
| Version value | `python.exe -c "import makehuman as m; print(m.__version__, m.versionSub)"` | `1.2.0 final` |
| GUI start | `Start-Process Python\python.exe makehuman.py` | process alive after 25 s, window `MakeHuman Community 1.2.0 - [Untitled]`, clean `python_err.txt` |
| CLI help | `--help` | lists `-v/--version`, `--license`, `--noshaders`, `--nomultisampling`, `--debugopengl`, `--fullloggingopengl`, `--debugnumpy`, `mhmFile` |

### Pitfall: stdout is redirected

`makehuman.py` calls `get_platform_paths()` + `redirect_standard_streams()` early in `main()`, so on
Windows **all** console output after the initial banner goes to files, not to the terminal:

- `<MH home>\python_out.txt`
- `<MH home>\python_err.txt`

That is why `--version` and `--help` appear to print nothing. Read the files, or evaluate the version
via `python -c` if you need it on stdout.

Default home when `MH_HOME_LOCATION` is unset: `%USERPROFILE%\Documents\makehuman\v1py3`.

## 4. Headless / unattended operation

There is **no `--nogui` or `--script` switch** in 1.2.0. The documented route is the plugin API:

1. Drop a plugin into `<MH home>\makehuman\v1py3\plugins\`.
2. List it in `<MH home>\makehuman\v1py3\settings.ini` as `"activeUserPlugins": ["your_plugin"]`.
3. MakeHuman calls the plugin's `load(app)` at startup; the plugin builds the character, exports
   FBX, and calls `app.exit()`.

The working, verified harness for this repo is:

```
powershell -ExecutionPolicy Bypass -File ArtSource\Blender\makehuman_pilot_run.ps1
```

It sets `MH_HOME_LOCATION` to `ArtSource/Downloads/MakeHuman/userhome` so per-user state stays inside
the repo, activates `makehuman_pilot_plugin.py` as `pilot_export`, exports
`ArtSource/Exports/Characters/Pilot_Human.fbx`, and prints the run log. It requires an OpenGL context
(offscreen/minimised window) — it is unattended, but not GPU-free.

That harness has already produced a verified asset: 163-bone armature, 5 skinned meshes, 52,466 tris,
height 1.837 m, with all textures present (`ArtSource/Notes/makehuman_pilot_verify.txt`).

## 5. FBX exporter patch

`makehuman_pilot_run.ps1` patches `plugins/9_export_fbx/fbx_material.py` and `fbx_binary.py` inside
our extracted copy (never upstream) so exports are opaque: `PremultiplyAlpha` False, `Texture alpha`
0.0, `AlphaSource` 0, `ReflectionFactor` 0.0. Without it, Blender's FBX importer makes garments
transparent and every material metallic.

## 6. Licence note

MakeHuman itself is AGPLv3, but it is used here only as a **build-time authoring tool**; the game
ships exported meshes and textures, not the program. MakeHuman's bundled assets are CC0, and the
exports are our own derivative game art. If the project ever ships the MakeHuman application or its
interface inside a build, that needs a fresh licence review — AGPLv3 obligations would then apply.

## 7. Redistribution caveat

MPFB / MakeClothes2 / MakeTarget2 / MakeSkin in `addons_for_blender_28x/` are Blender add-ons with
their own licences; they were **not** installed as part of this tooling. Use Blender's own exporter
path if one of them is ever needed.
