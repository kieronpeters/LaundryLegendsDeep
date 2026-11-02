# Unreal Engine 5 sample for Laundry Legends — build report

- **Date:** 2026-11-02
- **Type:** Scaffolding task. No engine installed. No Unity run. One repo file
  edited (`.gitignore`); everything else is new files under `UnrealSample/`
  (git-ignored) plus this report.
- **Owner constraint honoured:** nothing under `Assets/`, `Packages/`,
  `ProjectSettings/`, `tools/`, or the existing tests was read for writing,
  modified, or run. Git history untouched. No commit made (subagents do not
  commit).

---

## 1. Hardware measurement

Commands:

```powershell
Get-CimInstance Win32_OperatingSystem | Select-Object Caption,Version,TotalVisibleMemorySize,FreePhysicalMemory
Get-PSDrive -PSProvider FileSystem | Select-Object Name,@{n='UsedGB';e={[math]::Round($_.Used/1GB,1)}},@{n='FreeGB';e={[math]::Round($_.Free/1GB,1)}}
Get-CimInstance Win32_VideoController | Select-Object Name,AdapterRAM,DriverVersion
Get-CimInstance Win32_Processor | Select-Object Name,NumberOfCores,NumberOfLogicalProcessors
Get-CimInstance Win32_ComputerSystem | Select-Object Manufacturer,Model,TotalPhysicalMemory
```

Measured:

| Item | Value |
|---|---|
| OS | Windows 11 Home Single Language 10.0.26300 |
| Machine | ASUS TUF Gaming A15 FA506NCG |
| CPU | AMD Ryzen 7 7445HS, 6 cores / 12 logical processors |
| RAM | 16,011,588 KB visible ≈ 15.3 GB (7.2 GB free at measurement time) |
| GPU 1 | NVIDIA GeForce RTX 3050 Laptop, AdapterRAM 4,293,918,720 B ≈ 4 GB |
| GPU 2 | AMD Radeon 740M iGPU |
| Disk | C: 130.4 GB free of 446.8 GB |
| Shell elevation | **not elevated** (`WindowsPrincipal.IsInRole(Administrator)` = False) |

**Verdict: a UE5 editor can probably run, but the machine is below Epic's
recommended spec, and specifically below the 8 GB VRAM floor.** RAM (15.3 GB
vs 32 GB recommended) and VRAM (4 GB vs 8 GB+) are the binding limits; the
6-core CPU is also below the 12+ core guidance for editor + C++ work. A
greybox scene of ~120 static primitives with movable point lights and no
Nanite/Lumen content is far lighter than the projects those specs describe, so
a sample scene should be viewable — at 1080p with Lumen, Nanite, Virtual Shadow
Maps and hardware ray tracing off, after a cold-start shader/DDC build of
10-30 minutes, with the editor swapping once anything else is open. It is not
a machine to develop an Unreal project on. Disk is sufficient for one binary
5.6 install (~60 GB) plus DDC, and not remotely sufficient for a source build.

The laptop exposes two adapters; if the sample is ever opened the RTX 3050 must
be forced for the editor binary in Windows graphics settings.

---

## 2. Installation feasibility

### 2.1 What `winget` can do

```powershell
winget --version
winget search "Epic Games Launcher" --accept-source-agreements
winget search unreal --accept-source-agreements
winget show EpicGames.EpicGamesLauncher --accept-source-agreements
```

Results:

- `winget` v1.29.380 present.
- `EpicGames.EpicGamesLauncher` 1.3.210.0 is available. Installer: MSI
  `EpicInstaller-20.3.3.msi` from Epic's CDN, machine scope, "Offline
  Distribution Supported: true".
- **No Unreal Engine package exists in any winget source.** `winget search
  unreal` returns the launcher plus unrelated products (Unreal Gold,
  Unreal Commander, EOS SDK, octobuild, …). There is no `EpicGames.UnrealEngine`
  or `UnrealEngine.5.6`.

### 2.2 Whether the engine install can be non-interactive

It cannot, for four independent reasons:

1. **Winget only reaches the launcher.** The launcher is a storefront shell;
   engine builds are a separate 40-60 GB download it manages internally.
2. **Engine download requires an authenticated Epic session.** The launcher
   will not fetch an engine build without an Epic account login. The owner
   approved installs but explicitly withheld credentials, so this step is out
   of bounds for the agent — correctly so, since no credential should ever be
   typed by, stored by, or handed to an agent.
3. **Epic publishes no supported CLI for engine installs.** Deep links
   (`com.epicgames.launcher://ue/5.6`) only foreground the launcher and still
   land in a UI that needs a signed-in session and a click. There is no
   `EpicGamesLauncher.exe --install-engine` equivalent.
4. **The launcher MSI needs elevation.** This shell is not elevated, so even
   `winget install ... --silent` would raise UAC.

Because of (2) and (3), a 40-60 GB download could not be completed by this
session on 130 GB of free disk. **No download was started and nothing was
installed.** That is the deliberate decision, not an omission.

### 2.3 Engine presence check

```powershell
Test-Path 'C:\Program Files\Epic Games'          # False
Test-Path 'C:\Program Files (x86)\Epic Games'    # False
Test-Path 'C:\ProgramData\Epic'                  # False
Test-Path 'C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat'  # False
Get-Command UnrealEditor*,UnrealVersionSelector* # nothing
Get-ItemProperty 'HKLM:\SOFTWARE\EpicGames\Unreal Engine\*'  # nothing
```

No engine. `UnrealEditor-Cmd.exe` does not exist on this machine, so step 4 of
the task ("if and only if the engine is installed … run headlessly and report
with a screenshot") could not be reached. No screenshot exists; fabricating one
is not an option.

---

## 3. Deliverables (all new, all under the ignored `UnrealSample/`)

| File | Bytes | Purpose |
|---|---|---|
| `UnrealSample/LaundryLegendsSample.uproject` | 340 | FileVersion 3, `EngineAssociation "5.6"`, no C++ modules, enables `PythonScriptPlugin` + `EditorScriptingUtilities` |
| `UnrealSample/Content/.gitkeep` | 209 | placeholder so `Content/` exists before first open |
| `UnrealSample/build_laundromat_scene.py` | 33,289 (649 lines) | the scene generator |
| `UnrealSample/README.md` | 8,637 | hardware verdict, install blockers, exact commands, what the script builds, verification status |
| `UnrealSample/CONTROLS.md` | 7,920 | how to run the script in-editor and headlessly, PIE controls, troubleshooting table |

Edited: `.gitignore` (one hunk, appended):

```
# ---------------------------------------------------------------------------
# Unreal Engine 5 sample (local-only experiment, never opened on this machine)
# Not part of the Unity project; nothing under Assets/ or Packages/ uses it.
# Delete UnrealSample/ to remove the sample. See UnrealSample/README.md.
# ---------------------------------------------------------------------------
UnrealSample/
```

`git check-ignore -v UnrealSample/README.md` →
`.gitignore:130:UnrealSample/  UnrealSample/README.md`. Confirmed ignored.

`git status --porcelain` after the work shows exactly one new tracked-file
change (`.gitignore`); `UnrealSample/` does not appear.

### 3.1 What the generator builds

Target level `/Game/Maps/LaundryLegendsSample`:

- 16 x 12 m main room, 3.2 m high, 20 cm walls, floor, ceiling, storefront
  glass band, wall shelving.
- 5.2 m counter with a gold-trimmed drop-off slot, conveyor and staff table.
- 8 machines on the west wall: 4 blue washers, 4 orange dryers, each with drum
  door, control panel and coin slot.
- 2 m folding table with stacked linen, waiting bench.
- East wall door opening (1 x 2.2 m) with a door leaf tagged `SecretDoor`,
  concealed behind a 2 m shelf unit tagged `SecretShelf`, leading to an
  8 x 8 m back room.
- Back room: black casino carpet with a gold diamond lattice, 100% procedural
  material; gold baseboards; round felt table and three stools.
- Arcade cabinet with emissive cyan screen, pink marquee, gold buttons.
- 4 warm (2800 K) main-room point lights, 1 over the counter, 2 warmer
  (2400 K) back-room lights, cyan arcade glow, warm light at the secret door.
- One `PlayerStart` at the south-west corner, yaw 25°.

14 materials under `/Game/LaundryLegendsSample/Materials`. Zero imported
textures, meshes or audio: engine `BasicShapes` primitives only.

Idempotency: every actor is labelled `LL_...` and lives in level folder
`LaundryLegendsSample`; every asset lives under `/Game/LaundryLegendsSample`.
A re-run removes exactly those actors and rebuilds. The script logs
`STEP n/8` for each stage and a `BUILD COMPLETE` block with the actor inventory.

### 3.2 One API correction worth recording

The first draft used `MaterialExpressionChecker` for the carpet. Fetching the
UE 5.6 Python API pages showed that class **does not exist** — the alphabetical
chain runs `MaterialExpressionChannelMaskParameter` → `MaterialExpressionClamp`
— so the node would have thrown `AttributeError` on the first run. The carpet
is now computed with expressions verified to exist in 5.6:

`TextureCoordinate` → 2x `DotProduct` (45° rotation) → `AppendVector` →
`Multiply` (9x9 cells) → `Subtract` → `Abs` → `DotProduct` (|x|+|y|) →
`Multiply` (1/0.38 radius) → `OneMinus` → `Clamp` → `LinearInterpolate`
(black → gold).

If any node or pin name still differs in the installed build, the function
catches it, logs `diamond lattice failed (<error>)`, and falls back to a flat
black carpet rather than aborting the whole build.

---

## 4. Checks performed

| Check | Command | Result |
|---|---|---|
| Hardware measurement | see §1 commands | Recorded above |
| winget availability | `winget --version` | 1.29.380 |
| Launcher package exists | `winget show EpicGames.EpicGamesLauncher` | Found, v1.3.210.0, MSI |
| Engine package exists | `winget search unreal` | **No** engine package in any source |
| Engine already installed | path/registry/PATH probes | **Not installed** |
| Shell elevation | `WindowsPrincipal.IsInRole` | Not elevated (UAC would block silent MSI) |
| Python syntax | `python -m py_compile UnrealSample/build_laundromat_scene.py` | Exit 0, syntax OK |
| API class existence | UE 5.6 Python API pages for `MaterialExpressionConstant2Vector`, `TextureCoordinate`, `DotProduct`, `AppendVector`, `Frac`, `Abs`, `Subtract`, `Clamp`, `OneMinus`, `LinearInterpolate`, `LevelEditorSubsystem`, `EditorActorSubsystem` | All exist; `MaterialExpressionChecker` 404 |
| Subsystem method signatures | `LevelEditorSubsystem.new_level/load_level/save_current_level`, `EditorActorSubsystem.spawn_actor_from_class/destroy_actor/get_all_level_actors` | Confirmed against docs |
| Ignore rule works | `git check-ignore -v UnrealSample/README.md` | Matches `.gitignore:130` |
| Working tree scoped | `git status --porcelain` | Only ` M .gitignore` added by this task |

## 5. Checks not performed (and why)

- **Project has never been opened.** No engine on the machine.
- **Script has never executed.** Same reason. Pin names
  (`"UVs"`, `"Alpha"`, `"Input"`, `""`), `MaterialExpressionClamp`'s input
  naming, `actor.tags` append behaviour and commandlet-vs-editor subsystem
  availability are reasoned from documentation, not observed.
- **No screenshot / no PIE run / no lighting build.** Nothing exists to
  capture.
- **Headless `UnrealEditor-Cmd.exe -run=pythonscript`** not run: binary absent.
- Unity: not run, by instruction.

## 6. Blockers

1. **Epic account login (human only).** Engine download is gated behind an
   authenticated Epic session. Cannot be automated, and must not be.
2. **Launcher-only automation.** `winget` installs the launcher (and needs
   UAC), not the engine; Epic publishes no engine-install CLI.
3. **Hardware below spec.** 15.3 GB RAM / 4 GB VRAM / 6 cores. The sample will
   open but will not be a comfortable editor session. This does not block the
   deliverable, only the "verify it looks right" step.
4. **Stray `UnrealSample/__pycache__/`** remains from the syntax check. It is
   inside the ignored folder and was not deletable from this session
   (`Remove-Item` was blocked by the shell permission gate). Delete it with the
   folder; it has no effect.

## 7. Next action for the lead

Either: (a) leave the sample dormant — it costs nothing, is ignored, and the
decision memo already concluded "stay on Unity 6"; or (b) have the owner
install the launcher + UE 5.6 with their own account, then run
`UnrealSample/CONTROLS.md` §2 and capture a screenshot so the sample stops being
unverified. Option (b) is the only path to real evidence, and it is a
human-in-the-loop step.

---

## 8. Summary (10 lines)

1. Measured hardware: Ryzen 7 7445HS (6C/12T), 15.3 GB RAM, RTX 3050 Laptop 4 GB VRAM, 130.4 GB free.
2. That is below Epic's UE5 recommendations and below the 8 GB VRAM floor; a greybox scene should open, a real UE project would not be pleasant.
3. `winget` has the Epic Games Launcher (1.3.210.0) but **no** Unreal Engine package in any source.
4. Engine installs are launcher-UI driven and need an Epic login; no supported CLI exists and the shell is not elevated, so nothing was downloaded.
5. Therefore no engine, no headless run, no screenshot — and no fabricated evidence.
6. Instead: a ready-to-open blueprint-only project at `UnrealSample/LaundryLegendsSample.uproject` (FileVersion 3, EngineAssociation 5.6, no C++ modules).
7. `UnrealSample/build_laundromat_scene.py` (649 lines, syntax-checked) builds the 16x12 m room, drop-off counter, 8 blue/orange machines, folding table, concealed shelf + secret door, 8x8 m back room with a procedural gold-diamond casino carpet, arcade cabinet, warm lights and a PlayerStart; it is idempotent and logs every step.
8. Docs: `UnrealSample/README.md` (hardware verdict, blockers, exact install/run commands) and `UnrealSample/CONTROLS.md` (in-editor and headless runs, PIE controls, troubleshooting).
9. Caught one real defect before shipping: UE 5.6 has no `MaterialExpressionChecker`; the carpet is now built from documented nodes with a logged flat-black fallback.
10. Only repo change is `UnrealSample/` added to `.gitignore`; Unity untouched, nothing committed, sample unverified until a human installs the engine.
