# Machine Audit — DESKTOP-HSDVJFU (Windows 11, user `Asus`)

**Date:** 2026-11-01, 19:20–19:35 local
**Mode:** Read-only. No file was deleted, moved, installed, uninstalled, or reconfigured. The only write is this report.
**Host:** `DESKTOP-HSDVJFU`, Windows 11 Home Single Language, build `26300.9818` (26H2)
**Shell:** Windows PowerShell 5.1, **not elevated** (`IsInRole(Administrator) = False`)
**Repo measured:** `C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends` (never modified)

---

## Summary table

| Area | Finding | Size / severity |
|---|---|---|
| System volume `C:` | 60.0 GB free of 446.8 GB | **13.4% free — below the 15% threshold (FLAG)** |
| Disk hardware | WD PC SN5000S 512 GB NVMe, `HealthStatus = Healthy`, `OperationalStatus = OK` | OK |
| Profile total | `C:\Users\Asus` | 163.60 GB |
| Top profile consumer | `C:\Users\Asus\.ollama` (local LLM model blobs) | **40.10 GB** — high |
| Desktop | 41.39 GB, of which 34.9 GB is video + game installs | high, owner's data |
| Downloads | 13.07 GB, of which 13.13 GB is one Terminator collection | high, owner's data |
| AppData | Roaming 7.44 GB, Local\Packages 10.2 GB, Local\Google 10.24 GB, Local\Programs 10.12 GB | medium |
| Program Files (+x86) | 50.65 GB + 46.10 GB; Unity alone is 29.66 GB across 3 editors | medium |
| `C:\Windows` | 41.58 GB, of which WinSxS 16.55 GB, System32 11.87 GB, assembly 7.17 GB | normal for Windows |
| Temp (`%LOCALAPPDATA%\Temp`) | 5.1 GB, 1,812 MB of it older than 7 days | **easy win** |
| `C:\Windows\Temp` | 0 entries, 0 bytes | clean |
| Recycle Bin | 366 MB / 38,290 files | easy win |
| Duplicate files > 100 MB | 11 identical SHA-256 groups | **2,381 MB reclaimable** |
| Installed software | 83 uninstall entries (82 distinct names) | normal |
| Startup entries | 7 | one unnecessary leftover (Java updater) |
| Windows Installer | **Error 1618 observed at 19:02:00**; a single `msiexec` held the mutex from 18:22 to ~19:32 | **high — installer blockage** |
| Audacity 4.0.1 install | Failed; not in registry, not in `winget list` | medium |
| Pending reboot | None (all four indicators clear) | OK |
| Defender | RealTime on, signature age 0 days, quick scan 1 day | OK (full scan never run) |
| RAM | 15.27 GB total, 8.06 GB free | OK |
| Uptime | 5 h 31 m (booted 2026-11-01 13:52) | OK |
| Unity editor crashes today | 3× `Unity.exe` + 4× `Unity.ILPP.Trigger.exe` `Application Error` events | **medium — project relevant** |
| BitLocker state | **Could not measure** (needs elevation) | unknown |
| SMART counters | **Could not measure** (CIM denied without elevation) | unknown |
| Repo `Library` | 1.81 GB (regenerable, git-ignored) | OK |
| Desktop git backup | `LaundryLegends-backup-20261101-1837.bundle`, 582 MB, verified `# v2 git bundle` | keep |

---

## 1. Disk and volumes

`Get-Volume | Where DriveType -eq 'Fixed'`

| Label | FS | Size | Free | % free |
|---|---|---|---|---|
| (C:, system) | NTFS | 446.8 GB | 60.0 GB | **13.4%** |
| RESTORE | NTFS | 28.0 GB | 7.7 GB | 27.5% |
| MYASUS | FAT32 | 0.2 GB | 0.2 GB | 62.3% |
| RECOVERY | NTFS | 0.8 GB | 0.1 GB | 12.4% |
| (unlabeled) | NTFS | 0.9 GB | 0.1 GB | 8.9% |

Only one physical disk exists: `WD PC SN5000S SDEQNSJ-512G-1002`, 476.9 GB, NVMe, `Healthy` / `OK`. All five volumes report `HealthStatus = Healthy`, `OperationalStatus = OK`.

**Flag:** `C:` at 13.4% free is under the 15% threshold. The RECOVERY and 0.9 GB partitions are also under 15%, but they are small system/recovery partitions and are not user-managed space.

C: accounting from measured numbers: 386.8 GB used. `C:\Users\Asus` 163.60 GB + `C:\Windows` 41.58 GB + `C:\Program Files` 50.65 GB + `C:\Program Files (x86)` 46.10 GB + `C:\ProgramData` 11.05 GB ≈ 313 GB, with the remainder in `C:\` root items, other user profiles, and system files.

---

## 2. Biggest consumers

### 2.1 Requested locations

| Path | Size | Notes |
|---|---|---|
| `%LOCALAPPDATA%\Temp` | **5.1 GB** (5,223.7 MB) | 3,635 files; 1,812 MB older than 7 days |
| `C:\Windows\Temp` | **0 bytes** | genuinely empty (0 top-level entries, 0 files recursively) |
| `C:\Users\Asus\Downloads` | **13.07 GB** | 13.13 GB of it is one movie collection |
| `C:\Users\Asus\Desktop` | **41.39 GB** | see breakdown below |
| `C:\Users\Asus\Documents` | 0.38 GB | |
| `C:\Users\Asus\Videos` | 0.81 GB | 784 MB is one OBS recording |
| `C:\Users\Asus\Pictures` | 0.01 GB | |
| `%LOCALAPPDATA%\Microsoft\WinGet` | **0.79 GB** | `Packages` 735 MB, `Links` 0 MB |
| `%APPDATA%` (Roaming) | **7.44 GB** | |
| `%LOCALAPPDATA%\Packages` | **10.2 GB** | top 5 below |
| `%LOCALAPPDATA%\Unity` | **0.85 GB** | `cache` 566 MB, `Caches` 254 MB, `bin` 27 MB |
| `%LOCALAPPDATA%\UnityHub` | 185 MB | includes `pending\UnityHubSetup-3.22.5-x64.exe` |
| `%APPDATA%\UnityHub` | **0.39 GB** | |
| `%PROGRAMDATA%\Unity` | **MISSING** | path does not exist |
| `C:\Program Files` | **50.65 GB** | Unity 29.66 GB, VS 5.64 GB, Office 4.18 GB, Image-Line 2.05 GB, WSL 1.17 GB, dotnet 1.14 GB |
| `C:\Program Files (x86)` | **46.10 GB** | |

**Top 5 `%LOCALAPPDATA%\Packages` by size**

| Size | Package |
|---|---|
| 4,362 MB | `SpotifyAB.SpotifyMusic_zpdnekdrzrea0` |
| 2,086 MB | `OpenAI.Codex_2p2nqsd0c76g0` |
| 1,416 MB | `MSTeams_8wekyb3d8bbwe` |
| 1,260 MB | `5319275A.WhatsAppDesktop_cv1g1gvanyjgm` |
| 598 MB | `MicrosoftWindows.Client.CBS_cw5n1h2txyewy` |

**Desktop breakdown (items over 100 MB)**

| Size | Item |
|---|---|
| 9,120 MB | `Inception.2010.PROPER.Bluray.2160p.AV1.HDR10.OPUS.5.1-UH.mkv` |
| 7,230 MB | `The Matrix Complete 5 Movie Collection ...` (folder) |
| 5,941 MB | `Farm.Factory.Simulator.v2026.07.27` (game) |
| 5,676 MB | `Big Ambitions` (game) |
| 4,550 MB | `AI Coding Test` (contains this repo) |
| 3,043 MB | `LaundryLegends` (**a second folder of that name; not opened — see §8**) |
| 2,078 MB | `Supermarket Simulator (2025)` |
| 1,548 MB | `Ghost 1990 Remastered 1080p BluRay HEVC 265 5.1 BONE.mkv` |
| 1,201 MB | `Mon Bazou` |
| 612 MB | `SELF.MADE.Investment.Life.Sim.v2026.08.27` |
| 582 MB | `LaundryLegends-backup-20261101-1837.bundle` |
| 410 MB | `Startup.Company.v1.24` |
| 238 MB | `drive-download-20260930T144504Z-1-001.zip` |
| 99 MB | `drive-download-20260930T144518Z-1-001.zip` |

### 2.2 Repo

| Path | Size |
|---|---|
| `...\LaundryLegends` (whole) | 4.24 GB |
| `...\LaundryLegends\Library` | **1,857.9 MB** |
| `...\LaundryLegends\Assets` | 0.33 GB |
| `...\LaundryLegends\TestResults` | 149.4 MB |
| `...\LaundryLegends\Logs` | <0.1 MB |
| `...\LaundryLegends\UserSettings` | <0.1 MB |
| `...\LaundryLegends\Builds` | <0.1 MB |
| `Temp`, `obj` | do not exist |

### 2.3 Temp contents over 100 MB

| Size | Path | Modified |
|---|---|---|
| 2,753 MB | `%LOCALAPPDATA%\Temp\jofxdt2q` | 2026-10-30 |
| 507 MB | `%LOCALAPPDATA%\Temp\acg_texture_zips` | 2026-11-01 |
| 500 MB | `%LOCALAPPDATA%\Temp\scoped_dir24512_540937250` | 2026-10-26 |
| 380 MB | `%LOCALAPPDATA%\Temp\opencode` | 2026-11-01 |
| 185 MB | `%LOCALAPPDATA%\Temp\Diagnostics` | 2026-10-17 |
| 116 MB | `%LOCALAPPDATA%\Temp\Cline-0.0.48-updater-zthgxV` | 2026-10-17 |
| 109 MB | `%LOCALAPPDATA%\Temp\cori-meta.onnx` | 2026-10-31 |
| 101 MB | `%LOCALAPPDATA%\Temp\chocolatey` | 2026-10-22 |

### 2.4 Top 10 largest files above 500 MB under `C:\Users\Asus`

Full scan of the profile took 71.7 s; 33 files exceed 500 MB. **Zero** of them are inside the repo's `Library` folder, so no exclusions were needed.

| # | Size | Path |
|---|---|---|
| 1 | 17,697 MB | `C:\Users\Asus\.ollama\models\blobs\sha256-58574f2e94b9...` |
| 2 | 9,120 MB | `C:\Users\Asus\Desktop\Inception.2010.PROPER.Bluray.2160p.AV1.HDR10.OPUS.5.1-UH.mkv` |
| 3 | 8,846 MB | `C:\Users\Asus\.ollama\models\blobs\sha256-a8cc1361f314...` |
| 4 | 8,572 MB | `C:\Users\Asus\.ollama\models\blobs\sha256-ac9bc7a69dab...` |
| 5 | 7,230 MB | `C:\Users\Asus\Desktop\The Matrix Complete 5 Movie Collection ...\05 The Matrix Resurrections....mp4` |
| 6 | 4,983 MB | `C:\Users\Asus\.ollama\models\blobs\sha256-a3de86cd1c13...` |
| 7 | 2,730 MB | `C:\Users\Asus\AppData\Local\Google\Chrome\User Data\OptGuideOnDeviceModel\2025.8.21.1028\weights.bin` |
| 8 | 2,445 MB | `C:\Users\Asus\Downloads\Terminator.Collection...\Terminator.Dark.Fate.2019....mkv` |
| 9 | 2,298 MB | `C:\Users\Asus\Downloads\Terminator.Collection...\Terminator.2.Judgment.Day.1991....mkv` |
| 10 | 2,239 MB | `C:\Users\Asus\Downloads\Terminator.Collection...\Terminator.Genisys.2015....mkv` |

Also notable: `%LOCALAPPDATA%\Docker\wsl\disk\docker_data.vhdx` 1,576 MB, `%LOCALAPPDATA%\NVIDIA\DXCache\fc52a9d814ed1d31.nvph` 1,024 MB, two Docker ISOs ~1.5 GB combined.

---

## 3. Recycle Bin

`C:\$Recycle.Bin` — **383,811,373 bytes = 366 MB, 38,290 files**.

Per-SID: `S-1-5-21-2570633785-717233366-3394990918-1001` (Asus) = 366 MB / 38,290 files; `S-1-5-18` = 0 MB / 0 files.

The file count is high for the size, which means a large number of small deleted files rather than a few big ones.

---

## 4. Duplicate files above 100 MB

Method: enumerate all files > 100 MB under `C:\Users\Asus` (149 files, 54 s), group by exact byte length, then SHA-256 hash every file in a colliding group (26 files). 13 size groups collided; **11 groups were byte-identical**.

| Copies | Size | Equivalent | Paths |
|---|---|---|---|
| 2 | 352 MB | 352 MB | `.codex\plugins\.plugin-appserver\codex.exe` = `AppData\Local\OpenAI\Codex\bin\22cfb1d302613eae\codex.exe` |
| 2 | 257 MB | 257 MB | `.local\bin\claude.exe` = `.local\share\claude\versions\2.1.299` |
| 2 | 252 MB | 252 MB | `AppData\Local\@opencodedesktop-updater\installer.exe` = `...\pending\opencode-desktop-win-x64.exe` |
| 2 | 247 MB | 247 MB | `Local\Programs\ai.opencode.desktop\resources\opencode-cli.exe` = `Roaming\ai.opencode.desktop\cli\2.2.5-x64\opencode-cli.exe` |
| 2 | 242 MB | 242 MB | `.cache\opencode\opencode-service-12140-8de2737a.exe` = `Roaming\npm\node_modules\@opencode\cli\bin\opencode.exe` |
| 2 | 204 MB | 204 MB | `Local\canva-updater\installer.exe` = `...\pending\Canva-Setup-1.126.2.exe` |
| 2 | 200 MB | 200 MB | `.docker\cli-plugins\docker-scout.exe` = `Local\Programs\DockerDesktop\resources\cli-plugins\docker-scout.exe` |
| 2 | 184 MB | 184 MB | `Local\UnityHub\pending\UnityHubSetup-3.22.5-x64.exe` = `Local\unityhub-updater\installer.exe` |
| 2 | 169 MB | 169 MB | `Local\Microsoft\Edge\User Data\ProvenanceData\...\vti-b-p32-visual.quant.ort` = same file under `Local\Temp\scoped_dir24512_540937250` |
| 2 | 155 MB | 155 MB | Edge `component_crx_cache` copy = same file under `Local\Temp\scoped_dir24512_540937250` |
| 2 | 120 MB | 120 MB | `.docker\cli-plugins\docker-agent.exe` = `Local\Programs\DockerDesktop\resources\cli-plugins\docker-agent.exe` |

**Total reclaimable: 2,381 MB.**

Important: 6 of the 11 groups are *installer/pending-update pairs*, not accidents. The `installer.exe` / `pending\*` copies are how Windows updaters stage the next version. Deleting the `pending` copy can break a queued update. See the delete guidance in §9.

---

## 5. Installed software

| Metric | Value |
|---|---|
| Entries with `DisplayName` (HKLM 64-bit + HKLM WOW6432 + HKCU) | **83** |
| Distinct display names | 82 (one duplicate, Office Click-to-Run Extensibility Component) |
| Entries with no `EstimatedSize` | 31 (size unknown, mostly runtime patches) |

**Top 15 by `EstimatedSize`**

| Estimated size | Product | Version |
|---|---|---|
| 44,240 MB | Tempest Rising | — |
| 5,404 MB | Taxi Life: A City Driving Simulator | — |
| 4,846 MB | Final Fantasy I-VI Bundle PR | — |
| 3,354 MB | Docker Desktop | 4.95.0 |
| 3,164 MB | Ollama | 0.40.13 |
| 2,835 MB | Microsoft Edge | 156.0.4314.50 |
| 2,331 MB | Windows Software Development Kit — Windows 10.0.26100.8249 | 10.1.26100.8249 |
| 2,070 MB | Windows Software Development Kit — Windows 10.0.19041.5609 | 10.1.19041.5609 |
| 1,597 MB | Lark | 7.73.11 |
| 1,061 MB | PostgreSQL 18 | 18.6-3 |
| 990 MB | Microsoft Visual Studio Code (User) | 1.142.0 |
| 842 MB | OpenCode | 2.2.5 |
| 705 MB | Canva | 1.126.2 |
| 593 MB | Unity Hub | 3.22.5 |
| 574 MB | Foxit PDF Reader | 2026.2.1.39815 |

Two separate Windows SDK versions are registered (26100 and 19041), ≈4.4 GB combined. Tempest Rising's 44 GB `EstimatedSize` is a registry estimate, not a verified on-disk measurement.

Unity editors are **not** in the uninstall list; measured directly on disk: `6000.0.0f1` 6,932 MB, `6000.0.64f1` (the repo's editor) 7,420 MB, `6000.5.7f1` 15,314 MB.

---

## 6. Startup

`Get-CimInstance Win32_StartupCommand` returned 7 entries.

| Name | Location | Command |
|---|---|---|
| GoogleDriveFS | HKU SYSTEM + HKCU + HKU .DEFAULT (3 copies of one entry) | `"C:\Program Files\Google\Drive File Stream\133.0.2.0\GoogleDriveFS.exe" --startup_mode` |
| Ollama | Startup folder (`Ollama.lnk`) | `%APPDATA%\...\Startup\Ollama.lnk` (2,083 bytes) |
| Docker Desktop | HKCU Run | `C:\Users\Asus\AppData\Local\Programs\DockerDesktop\Docker Desktop.exe` |
| CanvaAutoLaunchAvailabilityCheckAgent | HKCU Run | `Canva.exe --start-availability-check-agent` |
| MicrosoftEdgeAutoLaunch_E27E50BD... | HKCU Run | `msedge.exe --no-startup-window --win-session-start` |
| SecurityHealth | HKLM Run | `%windir%\system32\SecurityHealthSystray.exe` |
| SunJavaUpdateSched | HKLM WOW6432 Run | `"C:\Program Files (x86)\Common Files\Java\Java Update\jusched.exe"` |

Nothing looks malicious. All paths resolve to legitimate signed vendor locations. Notes:

- **SunJavaUpdateSched** is a legacy Java updater stub and is not needed on a machine with no Java development work in this project; it is a scheduled-task leftover pattern.
- **CanvaAutoLaunchAvailabilityCheckAgent** is an availability poller that starts at every login.
- **Edge `--win-session-start`** pre-launches Edge at login.
- **Ollama** starts a background model server at every login. This machine holds 40.1 GB of Ollama models, so it also holds RAM and disk.
- **Docker Desktop** auto-start keeps `docker_data.vhdx` (1,576 MB) mounted.
- Nothing points outside `C:\Program Files`, `C:\Program Files (x86)`, or `%LOCALAPPDATA%\Programs`.

---

## 7. Health signals

### 7.1 Disk hardware
- `WD PC SN5000S SDEQNSJ-512G-1002`, 476.9 GB, NVMe, `MediaType = SSD`, `HealthStatus = Healthy`, `OperationalStatus = OK`, `Status = OK`.
- **SMART / reliability counters unavailable:** `Get-StorageReliabilityCounter` returned *"Access to a CIM resource was not available to the client"* — this requires elevation. Temperature, power-on hours, and wear are therefore **not measured**.

### 7.2 Memory and uptime
- RAM total **15.27 GB**, free **8.06 GB** (≈53% free).
- Last boot 2026-11-01 13:52:36; uptime **5 h 31 m**.
- Pagefile: WMI reports `C:\pagefile.sys`, 17,408 MB allocated, 67 MB in use; automatic pagefile management is `True`.

### 7.3 Pending reboot
All clear — no reboot is pending:

| Indicator | Value |
|---|---|
| `Component Based Servicing\RebootPending` | False |
| `Component Based Servicing\RebootRequired` | False |
| `WindowsUpdate\Auto Update\RebootRequired` | False |
| `Session Manager\PendingFileRenameOperations` | 0 entries |

### 7.4 Windows version and updates
- `ProductName` = Windows 10 Home Single Language, `DisplayVersion` = **26H2**, build **26300.9818**, edition `CoreSingleLanguage`. (ProductName still reports "Windows 10" on Windows 11; the build number is the reliable indicator.)
- Last 5 hotfixes: **KB5127936** and **KB5132402** on 2026-10-29, **KB5127694** and **KB5121794** on 2026-10-14, KB5054156 on 2026-06-06.
- Most recent update history: Defender intelligence 1.463.68.0 on 2026-11-01 06:03, plus Realtek network and Bluetooth driver updates on 2026-10-30. `SoftwareDistribution\Download` is 2 MB — no queued update payload.

### 7.5 Defender
| Field | Value |
|---|---|
| `RealTimeProtectionEnabled` | True |
| `AntivirusEnabled` | True |
| `AntivirusSignatureAge` | **0 days** (last updated 2026-11-01 05:26) |
| `QuickScanAge` | 1 day (ended 2026-10-31 13:30) |
| `FullScanAge` | **4,294,967,295 = never scanned** |
| `IsTamperProtected` | True |
| `BehaviorMonitorEnabled` | True |
| Engine / product | 1.1.26100.4 / 4.18.26100.4 |

Defender exclusions could not be read: `Get-MpPreference` returned *"N/A: Must be an administrator to view exclusions"*. Whether Unity folders or `Desktop\AI Coding Test` are excluded is **unknown**.

### 7.6 Top processes by working set (right now)

| Working set | Process | PID |
|---|---|---|
| 557 MB | `opencode-cli` | 18652 |
| 432 MB | `MsMpEng` (Defender) | 6576 |
| 408 MB | Memory Compression | 4148 |
| 381 MB | `Spotify` | 9528 |
| 378 MB | `OpenCode` | 2200 |
| 349 MB | `Spotify` (second process) | 5944 |
| 301 MB | **`LaundryLegends`** (the game build is running) | 12268 |
| 285 MB | `explorer` | 9728 |

### 7.7 Crashes recorded in the Application log (last 3 days)

28 `Application Error` events, 8 `CodexSandboxService` errors, 8 `.NET Runtime` errors. The Unity-relevant ones:

| Time | Faulting app | Module |
|---|---|---|
| 2026-11-01 15:36:10 | `Unity.exe` 6000.0.64.24759 | KERNELBASE.dll |
| 2026-11-01 14:36:20 | `Unity.exe` 6000.0.64.24759 | KERNELBASE.dll |
| 2026-11-01 03:18:45 | `Unity.exe` 6000.0.64.24759 | KERNELBASE.dll |
| 2026-11-01 02:17:51 | `Unity.ILPP.Trigger.exe` 2022.2.0.1 | self |
| 2026-11-01 01:24:42 | `Unity.ILPP.Trigger.exe` | self |
| 2026-11-01 01:22:00 | `Unity.ILPP.Trigger.exe` | self |
| 2026-10-31 22:33:48 | `Unity.ILPP.Trigger.exe` | self |

`Unity.ILPP.Trigger.exe` is Unity's Burst/IL post-processor; repeated crashes there precede "Burst compile failed" style errors. `Unity.exe` + `KERNELBASE.dll` is typically a managed `Environment.FailFast`/unhandled exception or a native abort, often a licensing or graphics-device failure in batch mode.

---

## 8. Risk flags for this machine

### 8.1 Windows Installer transaction (1618) — the significant one

Direct evidence, in chronological order:

```
19:01:59  WinGet: "Starting: ...audacity-win-4.0.1-x86_64.msi /passive /norestart"
19:02:00  WinGet: "<E> ShellExecute installer failed: 1618"
19:02:00  WinGet: "<E> Terminating context: 0x8a150102 ... InstallFlow.cpp:239"
```

and in the Application log:

```
18:22:01  MsiInstaller id=1040  Beginning a Windows Installer transaction: ...\Audacity.Audacity.4.0.1.262721353\audacity-win-4.0.1-x86_64.msi  (client pid 11304)
19:24:40  MsiInstaller id=1042  Ending a Windows Installer transaction:  ...same MSI...                              (client pid 11304)
```

Supporting facts:
- No `1033` ("installed the product") or `11707` ("Installation completed successfully") event exists for Audacity.
- `winget list --name Audacity` → *"No installed package found matching input criteria."* No Audacity uninstall registry entry.
- A single `msiexec.exe` PID 10688, `CreationDate = 2026-11-01 18:22:01` — i.e. the same process that opened the 18:22 transaction — was still alive and holding the installer mutex at 19:25, 19:27 and 19:31, **70 minutes after it started and 7 minutes after the transaction "ended"**. `msiserver` showed `Running` at 19:25 and `TrustedInstaller` was running.
- The per-transaction MSI logs `Audacity...-18-22-01.log`, `-19-01-25.log`, `-19-01-59.log` are all **0 KB** — the engine never got far enough to write a log, so the failure was at the mutex/engine-start stage, not inside the product's install sequence.
- The `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\InProgress` key was **absent**, which is typical for a hung/orphaned engine rather than an active UI install.

**State changed during the audit** (reported honestly): at **19:34:18** the `msiexec` process had exited, `msiserver` was `Stopped` and `TrustedInstaller` was `Stopped`. The 1618 block was therefore self-clearing, but it lasted about 70 minutes and cost at least two failed Audacity attempts (18:22 and 19:01).

**Consequence:** during that window *every* MSI-based install on this machine would fail with 1618 — "Another installation is already in progress." This explains why `krita-x64-6.0.4-setup.exe` (168 MB, 2026-11-01), `lmms-1.2.2-win64.exe` (35 MB, 2026-11-01) and `audacity-win-4.0.1-x86_64.msi` (47 MB, 2026-11-01) are sitting in `Downloads` and `Temp\WinGet` with **no** corresponding uninstall registry entries — none of the three is installed.

### 8.2 BitLocker — could not measure
`Get-BitLockerVolume` produced no output and `manage-bde -status C:` returned *"ERROR: An attempt to access a required resource was denied. Check that you have administrative rights."* **Encryption state is unknown**, not "off". Volume `C:` is `Healthy/OK` but encryption status is not visible to a non-elevated session.

### 8.3 Disk pressure from Unity caches and editors

| Item | Size | Pressure |
|---|---|---|
| 3 Unity editors in `C:\Program Files\Unity\Hub\Editor` | **29.66 GB** | the repo pins `6000.0.64f1` (7.42 GB); the other two are 6.93 GB and **15.31 GB** |
| `%LOCALAPPDATA%\Unity` | 0.85 GB | cache + Caches |
| `%APPDATA%\UnityHub` | 0.39 GB | |
| `%LOCALAPPDATA%\UnityHub` | 185 MB | small |
| Repo `Library` | 1.81 GB | regenerable |
| Repo `TestResults` | 149 MB | captures/logs |

Unity's own footprint (editors + user caches + Library) is ≈ **32.9 GB**, about 8.5% of the whole disk. Per-editor, `6000.5.7f1` alone is larger than the entire user profile's `Downloads` folder.

### 8.4 Leftover installers and `.bundle` files

| File | Size | Date | Notes |
|---|---|---|---|
| `C:\Users\Asus\Desktop\LaundryLegends-backup-20261101-1837.bundle` | 582 MB | 2026-11-01 18:37 | header reads `# v2 git bundle` — a real git backup of the project |
| `C:\Users\Asus\Desktop\drive-download-20260930T144504Z-1-001.zip` | 238 MB | 2026-09-30 | Google Drive archive |
| `C:\Users\Asus\Desktop\drive-download-20260930T144518Z-1-001.zip` | 99 MB | 2026-09-30 | Google Drive archive |
| `C:\Users\Asus\Downloads\krita-x64-6.0.4-setup.exe` | 168 MB | 2026-11-01 | never installed |
| `C:\Users\Asus\Downloads\audacity-win-4.0.1-x86_64.msi` | 47 MB | 2026-11-01 | never installed |
| `C:\Users\Asus\Downloads\lmms-1.2.2-win64.exe` | 35 MB | 2026-11-01 | never installed |
| `%LOCALAPPDATA%\Temp\WinGet\Audacity.Audacity.4.0.1.262721353\audacity-win-4.0.1-x86_64.msi` | ≈47 MB | 2026-11-01 | WinGet cache copy of the same MSI |

The `.bundle` is **not junk**: it is a dated git bundle, the kind used as an offline project checkpoint. There are no other `.bundle` files above 20 MB outside game install folders.

Also present: a **second folder** `C:\Users\Asus\Desktop\LaundryLegends` (3,043 MB). It was **not opened** — `AGENTS.md` forbids accessing another Laundry Legends project. Only its aggregate size was measured from the parent directory listing.

### 8.5 Miscellaneous
- `%LOCALAPPDATA%\Temp\jofxdt2q` at 2,753 MB is the single largest temp item and has not been touched since 2026-10-30. The name matches an unpacker staging directory, not a Windows component.
- The stray `C:\Users\Asus\Desktop\AI` file that `PROJECT_STATE.md` lists as "cleanup pending" **no longer exists** (`Get-Item` returned nothing). That approval item can be closed.
- `%LOCALAPPDATA%\Google\Chrome\...\OptGuideOnDeviceModel\...\weights.bin` (2,730 MB) + `cache.bin` (1,344 MB) are Chrome's on-device Gemini model files ≈ **4 GB**.

---

## 9. Safe to delete

Everything below is either regenerated automatically, already duplicated elsewhere, or is an abandoned staging artifact. **Nothing in this audit deleted any of it** — these are recommendations for the owner.

| Path | Size | What it is / what regenerates |
|---|---|---|
| `%LOCALAPPDATA%\Temp\jofxdt2q` | **2,753 MB** | stale unpack staging dir, untouched since 2026-10-30. Nothing regenerates it; if whatever created it is re-run it recreates its own. |
| `%LOCALAPPDATA%\Temp` (remainder: `scoped_dir24512_540937250` 500 MB, `Diagnostics` 185 MB, `Cline-0.0.48-updater-zthgxV` 116 MB, `cori-meta.onnx` 109 MB, `chocolatey` 101 MB, misc) | **≈1,960 MB** | temp files; apps recreate on demand. 1,812 MB of `Temp` is already older than 7 days. |
| `%LOCALAPPDATA%\Temp\acg_texture_zips` | 507 MB | downloaded ambientCG texture archives already imported into `Assets/LaundryLegends/Art/Textures` — re-downloadable from ambientCG, no longer needed on disk |
| `%LOCALAPPDATA%\Temp\opencode` | 380 MB | session temp; regenerates per session. Close running OpenCode sessions first. |
| Recycle Bin (`C:\$Recycle.Bin\S-1-5-21-...-1001`) | **366 MB** / 38,290 files | deleted files; irreversible, so empty only after confirming nothing needed |
| `%LOCALAPPDATA%\Microsoft\WinGet\Packages` | 735 MB | WinGet's installer download cache; re-downloaded on next install |
| `%LOCALAPPDATA%\Temp\WinGet\Audacity.Audacity.4.0.1.262721353` | ≈47 MB | cached MSI for a failed install; re-downloaded by `winget` |
| `%LOCALAPPDATA%\canva-updater\installer.exe` (204 MB) **or** the `pending\Canva-Setup-1.126.2.exe` copy — keep one | 204 MB | duplicate installer pair |
| `%LOCALAPPDATA%\unityhub-updater\installer.exe` (184 MB) **or** `Local\UnityHub\pending\UnityHubSetup-3.22.5-x64.exe` — keep one | 184 MB | duplicate installer pair, Unity Hub 3.22.5 already installed |
| `%LOCALAPPDATA%\@opencodedesktop-updater\installer.exe` (252 MB) **or** `pending\opencode-desktop-win-x64.exe` — keep one | 252 MB | duplicate installer pair |
| `.cache\opencode\opencode-service-12140-8de2737a.exe` | 242 MB | abandoned per-session service binary; duplicates the installed CLI |
| `.local\share\claude\versions\2.1.299` **or** `.local\bin\claude.exe` | 257 MB | byte-identical old-version copy left by the Claude updater |
| `%LOCALAPPDATA%\Temp\scoped_dir24512_540937250` Edge `ProvenanceData`/`component_crx_cache` files | 324 MB | byte-identical copies of live Edge files (already counted in Temp) |
| `C:\Windows\Temp` | 0 MB | already empty |

**Realistic recoverable total: ≈ 7.8 GB** (2,753 + 1,960 + 507 + 366 + 735 + 204 + 184 + 252 + 242 + 257), i.e. C: would go from 60.0 GB free (13.4%) to roughly **67.8 GB free (15.2%)** — just over the threshold.

Bigger, but requiring an explicit owner decision (not "safe to delete" unilaterally):

| Candidate | Size | Caveat |
|---|---|---|
| `C:\Users\Asus\.ollama\models` | 40.1 GB | Only safe if the local LLM is no longer used. **`state.json` records that the global OpenCode config defaults to `ollama/qwen3.5:4b`** — removing models would break that default. |
| Unity editor `6000.5.7f1` | 15.3 GB | Not the repo's editor (`6000.0.64f1`). Removing an editor is an uninstall, which is outside this audit's authority. |
| Unity editor `6000.0.0f1` | 6.9 GB | Same caveat. |
| `%LOCALAPPDATA%\Google\Chrome\...\OptGuideOnDeviceModel` | 4,075 MB | Disabling Chrome's on-device model in Chrome settings reclaims it; deleting by hand re-downloads it. |
| `C:\ProgramData\NVIDIA Corporation` | 7,543 MB | Driver installer/cache leftovers; use Windows Disk Cleanup, not manual deletion. |
| `Downloads\Terminator.Collection...` | 13,132 MB | Owner's media. |

---

## 10. Do NOT delete

| Path | Size | Why |
|---|---|---|
| `C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends\**` | 4.24 GB | The active Unity project: `Assets`, `ProjectSettings`, `Packages`, docs, `.orchestrator`, `ArtSource` |
| `...\LaundryLegends\Library` | 1.81 GB | Regenerable by Unity, but deleting it forces a full reimport and a long rebuild; not worth it at 60 GB free |
| `...\LaundryLegends\TestResults` | 149 MB | Playtest captures and test evidence referenced by project reports |
| `...\LaundryLegends\.git` | (inside 4.24 GB) | Repository history; no remote exists, so this is the only copy |
| `C:\Users\Asus\Desktop\LaundryLegends-backup-20261101-1837.bundle` | 582 MB | Verified git bundle v2 — a dated offline backup of the project |
| `C:\Users\Asus\Desktop\LaundryLegends\` (second folder) | 3,043 MB | Another project of the same name; explicitly off-limits, contents not inspected |
| `C:\Users\Asus\.ollama\models` | 40.1 GB | Backing store for the global OpenCode default model `ollama/qwen3.5:4b` |
| `C:\Users\Asus\Downloads\audacity-win-4.0.1-x86_64.msi`, `krita-x64-6.0.4-setup.exe`, `lmms-1.2.2-win64.exe` | 250 MB | Downloaded, never installed. Keep until the installs succeed, then discard. |
| `C:\Users\Asus\Downloads\Terminator.Collection...` and Desktop movie files | 24 GB | Owner's media, not system junk |
| Desktop game folders (`Farm Factory Simulator`, `Big Ambitions`, `Supermarket Simulator`, `Mon Bazou`, `SELF.MADE`, `Startup.Company`) | ≈17 GB | Installed games |
| `C:\Windows\WinSxS`, `C:\Windows\Installer`, `C:\Windows\assembly` | 16.6 / 1.7 / 7.2 GB | Component store and MSI patch cache. Manual deletion breaks Windows and uninstall/repair. Only `DISM /StartComponentCleanup` is legitimate, and it needs elevation. |
| `PDF/`pending` updater copies listed in §9 | — | Keep exactly one of each pair; the `pending\` copy is the queued next version |

---

## 11. Machine issues to look at (ranked)

**1. `C:` is at 13.4% free — below the 15% floor.**
Evidence: `Get-Volume` → 446.8 GB size, 60.0 GB remaining. Unified Unity editor + project + profile footprint is ≈ 313 GB of the 386.8 GB in use.
Smallest safe next step: empty `%LOCALAPPDATA%\Temp` and the Recycle Bin (≈3.1 GB), which requires no approval from anyone but the owner and no reboot.

**2. Windows Installer 1618 blockage (was live for ~70 minutes today, now cleared).**
Evidence: `WinGet` log `19:02:00 <E> ShellExecute installer failed: 1618`; `msiexec` PID 10688 alive from 18:22:01 to ~19:34; `MsiInstaller` 1040 at 18:22:01 and 1042 at 19:24:40 with no 1033/11707 success event; all three Audacity MSI logs 0 KB; Audacity absent from `winget list` and the uninstall registry. At 19:34 `msiexec` count = 0 and `msiserver` = `Stopped`.
Smallest safe next step: retry one install now that the mutex is free — `winget install --id Audacity.Audacity -e` — then confirm with `winget list --name Audacity`. If 1618 returns, the minimal fix is a reboot (not killing `msiexec` mid-transaction).

**3. Three MSI/EXE installers downloaded but never installed.**
Evidence: `krita-x64-6.0.4-setup.exe` (168 MB, 2026-11-01), `lmms-1.2.2-win64.exe` (35 MB, 2026-11-01), `audacity-win-4.0.1-x86_64.msi` (47 MB, 2026-11-01) in `Downloads`; the uninstall registry has **no** entry matching `Krita|LMMS|Audacity`.
Smallest safe next step: once issue 2 is clear, run one install at a time and verify each in the uninstall registry.

**4. Unity crashed 3× and its IL post-processor 4× in the last 36 hours.**
Evidence: `Application Error` events — `Unity.exe 6000.0.64.24759` faulting in `KERNELBASE.dll` at 2026-11-01 14:36:20 and 15:36:10 and 03:18:45; `Unity.ILPP.Trigger.exe` at 2026-11-01 01:22, 01:24, 02:17 and 2026-10-31 22:33.
Smallest safe next step: read `%LOCALAPPDATA%\Unity\Editor\Editor.log` and `...\Library\BurstCache` logs around those timestamps before running the next batch compile, so a real project defect is not mistaken for a licence/tooling failure.

**5. Defender has never run a full scan.**
Evidence: `FullScanAge = 4294967295` (sentinel for "never"); quick scan last ran 2026-10-31.
Smallest safe next step: schedule a full scan overnight; do not run it during a Unity import, since `MsMpEng` is already the #2 process at 432 MB.

**6. 22.2 GB of Unity editors beyond the one the project uses.**
Evidence: `C:\Program Files\Unity\Hub\Editor` contains `6000.0.0f1` 6,932 MB, `6000.0.64f1` 7,420 MB (the repo's pinned editor), `6000.5.7f1` 15,314 MB.
Smallest safe next step: owner decides whether `6000.5.7f1` and `6000.0.0f1` are still needed; removing an editor is an uninstall and must be approved separately. Do not remove `6000.0.64f1`.

**7. Recycle Bin holds 38,290 files for only 366 MB.**
Evidence: `Get-ChildItem 'C:\$Recycle.Bin' -Recurse` → 383,811,373 bytes, 38,290 files.
Smallest safe next step: review, then empty. Low risk, low reward, but it also reduces directory overhead.

**8. Startup carries three unnecessary background apps.**
Evidence: `SunJavaUpdateSched` (legacy Java updater, no Java dev work in this project), `CanvaAutoLaunchAvailabilityCheckAgent`, Edge `--win-session-start`, plus `Ollama` and `Docker Desktop` auto-starting.
Smallest safe next step: disable Java's updater entry first — it is the only one with no plausible use here. Do not touch `SecurityHealth` or `GoogleDriveFS` without asking.

**9. Chrome's on-device model consumes ~4 GB.**
Evidence: `weights.bin` 2,730 MB + `cache.bin` 1,344 MB under `...\Chrome\User Data\OptGuideOnDeviceModel\2025.8.21.1028\`.
Smallest safe next step: if unused, disable the feature in Chrome settings; manual deletion causes a re-download.

---

## 12. Could not measure (and why)

| Item | Reason |
|---|---|
| **BitLocker state** | `Get-BitLockerVolume` returned nothing and `manage-bde -status C:` returned *"An attempt to access a required resource was denied"*. Requires an elevated session. Unknown ≠ off. |
| **SMART / drive reliability** | `Get-StorageReliabilityCounter` → *"Access to a CIM resource was not available to the client"*. No temperature, power-on hours, wear, or error counters. Healthy/OK status comes only from `Get-PhysicalDisk` / `Get-CimInstance Win32_DiskDrive`. |
| **Defender exclusions** | `Get-MpPreference` → *"N/A: Must be an administrator to view exclusions"*. Cannot confirm whether the Unity project path is excluded. |
| **Volume Shadow Copy usage** | `vssadmin list shadowstorage` → *"You don't have the correct permissions"*. |
| **Ollama model names** | `ollama list` exceeded a 120 s timeout (it starts the model server first) and was abandoned rather than killed mid-install. Blob sizes were measured directly on disk instead; the 4 large blobs are 17.7, 8.8, 8.6 and 5.0 GB. |
| **Contents of `C:\Users\Asus\Desktop\LaundryLegends`** | Deliberately not inspected: `AGENTS.md` forbids accessing another Laundry Legends project. Only its 3,043 MB aggregate size appears here. |
| **Unity library/test validity** | Out of scope: this audit did not run Unity, compile, or tests. |
| **Full-profile file total above 500 MB** | The enumeration succeeded (33 files), but the summed total of all 33 was not retained; only the top 10 ranked entries are listed. |
| **Process start times / command lines for protected processes** | `msiexec` `CreationDate` was available via CIM but `StartTime`/`CommandLine` were not (SYSTEM process, non-elevated reader). |

No scan was skipped for time reasons. The heaviest scans were the two full-profile enumerations (71.7 s and 54 s), both inside the 2-minute budget.

---

## Appendix — commands used

```
Get-Volume                                              # disk sizes/free
Get-CimInstance Win32_OperatingSystem                   # RAM, uptime, build
Get-PhysicalDisk / Get-CimInstance Win32_DiskDrive       # disk health
Get-StorageReliabilityCounter                            # SMART (denied)
robocopy <dir> NULL /L /E /NJH /NFL /NDL /NC /BYTES /R:0 /W:0   # read-only size probe
Get-ChildItem C:\Users\Asus -Recurse -File -Force | Where Length -gt 500MB
Get-ChildItem C:\Users\Asus -Recurse -File -Force | Where Length -gt 100MB | Group Length
Get-FileHash -Algorithm SHA256                           # duplicate confirmation
Get-ItemProperty HKLM/HKCU ...\Uninstall\*               # installed software
Get-CimInstance Win32_StartupCommand + Run keys + Startup folders
Get-HotFix | Sort InstalledOn -Descending
Microsoft.Update.Session.QueryHistory(0,5)
Get-MpComputerStatus / Get-MpPreference
Get-WinEvent Application (MsiInstaller, Application Error, 1618 search)
Get-Content <WinGet log> -Tail 30                        # 1618 evidence
Get-Service wuauserv,UsoSvc,BITS,msiserver,TrustedInstaller
Get-BitLockerVolume / manage-bde -status C:              # denied
vssadmin list shadowstorage                              # denied
```

---

**Audit performed read-only.** The only file created is this report. Nothing was deleted, installed, uninstalled, stopped, or reconfigured, and the Unity project at `C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends` was measured but not modified.
