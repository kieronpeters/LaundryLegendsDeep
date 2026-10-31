# Creates one MakeHuman pilot character and exports FBX, unattended.
#
# MakeHuman 1.2.0 exposes no --nogui/--script switch, so automation goes through
# the documented plugin API: the plugin is dropped into the *user* plugins folder
# and listed in settings.ini -> activeUserPlugins. MakeHuman then calls its
# load(app) at startup; the plugin builds the character, exports, and quits.
#
# Usage:  powershell -ExecutionPolicy Bypass -File ArtSource\Blender\makehuman_pilot_run.ps1

$ErrorActionPreference = 'Stop'

$root   = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$mhRoot = Join-Path $root 'Tools\MakeHuman\makehuman'
$mhHome = Join-Path $root 'ArtSource\Downloads\MakeHuman\userhome'
$userMH = Join-Path $mhHome 'makehuman\v1py3'
$plugs  = Join-Path $userMH 'plugins'
$settings = Join-Path $userMH 'settings.ini'
$python = Join-Path $root 'Tools\MakeHuman\Python\python.exe'

if (-not (Test-Path $python)) { throw "MakeHuman python not found: $python" }

New-Item -ItemType Directory -Force -Path $mhHome, $plugs | Out-Null

# 1) install the automation plugin as a user plugin
Copy-Item (Join-Path $PSScriptRoot 'makehuman_pilot_plugin.py') (Join-Path $plugs 'pilot_export.py') -Force

# 2) activate it. Written before launch so MakeHuman sees it during loadPlugins().
if (Test-Path $settings) {
    $txt = Get-Content $settings -Raw
    $txt = [regex]::Replace($txt, '"activeUserPlugins"\s*:\s*\[[^\]]*\]', '"activeUserPlugins": ["pilot_export"]')
    # MakeHuman reads settings.ini as plain UTF-8 and chokes on a BOM.
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($settings, $txt, $utf8NoBom)
} else {
    throw "settings.ini missing - run MakeHuman once first ($settings)"
}
Write-Output "activeUserPlugins -> $((Select-String -Path $settings -Pattern 'activeUserPlugins').Line.Trim())"

# 3) environment
# ---------------------------------------------------------------------------
# Tool patch (idempotent, applied to our own extracted copy - never upstream):
# MakeHuman's FBX exporter hard-codes "PremultiplyAlpha: True" and
# "Texture alpha: 1.0" in its texture property *template*
# (plugins/9_export_fbx/fbx_material.py). Blender's FBX importer reacts by
# wiring every diffuse texture's alpha channel into the shader and switching the
# material to alpha-hashed, which renders opaque garments (and especially the
# shoes, whose texture alpha is 0 outside the UV islands) transparent.
# Setting both to "no alpha" makes the exported materials plain opaque.
$fbxmat = Join-Path $mhRoot 'plugins\9_export_fbx\fbx_material.py'
$fbxbin = Join-Path $mhRoot 'plugins\9_export_fbx\fbx_binary.py'
foreach ($f in @($fbxmat, $fbxbin)) {
    if (-not (Test-Path $f)) { throw "FBX exporter not found: $f" }
}
$utf8NoBom = New-Object System.Text.UTF8Encoding $false

$patched = 0
foreach ($f in @($fbxmat, $fbxbin)) {
    $t = [System.IO.File]::ReadAllText($f)
    $before = $t
    $t = $t.Replace('(b"PremultiplyAlpha", (True, "p_bool", False))', '(b"PremultiplyAlpha", (False, "p_bool", False))')
    $t = $t.Replace('(b"Texture alpha", (1.0, "p_double", False))', '(b"Texture alpha", (0.0, "p_double", False))')
    $t = $t.Replace('(b"AlphaSource", (2, "p_enum", False)),  # Black (i.e. texture''s alpha), XXX name guessed!.', '(b"AlphaSource", (0, "p_enum", False)),  # No alpha (opaque textures).')
    # ReflectionFactor 1.0 in the template makes Blender's FBX importer set
    # Metallic = 1.0 on every material, which renders skin/clothes/shoes as
    # tinted metal. MakeHuman's own per-material "Reflectivity" is 0.0, so the
    # template default is simply wrong.
    $t = $t.Replace('(b"ReflectionFactor", (1.0, "p_number", True))', '(b"ReflectionFactor", (0.0, "p_number", True))')
    if ($t -ne $before) {
        [System.IO.File]::WriteAllText($f, $t, $utf8NoBom)
        $patched++
        Write-Output "patched $(Split-Path $f -Leaf): no texture alpha"
    }
}
if ($patched -eq 0) { Write-Output "FBX exporter already patched (no texture alpha flags)" }

$env:MH_HOME_LOCATION = $mhHome
$env:LL_REPO_ROOT     = $root
$env:LL_MH_ROOT       = $mhRoot
$env:LL_PILOT_FBX     = Join-Path $root 'ArtSource\Exports\Characters\Pilot_Human.fbx'
$env:LL_PILOT_LOG     = Join-Path $root 'ArtSource\Notes\makehuman_pilot_run.log'

Remove-Item $env:LL_PILOT_FBX -Force -ErrorAction SilentlyContinue
Remove-Item $env:LL_PILOT_LOG -Force -ErrorAction SilentlyContinue

Write-Output "launching: $python makehuman.py (plugin pilot_export)"
$p = Start-Process -FilePath $python -ArgumentList 'makehuman.py' `
        -WorkingDirectory $mhRoot -PassThru -WindowStyle Minimized

$timeout = 600
$sw = [Diagnostics.Stopwatch]::StartNew()
while (-not $p.HasExited -and $sw.Elapsed.TotalSeconds -lt $timeout) { Start-Sleep -Milliseconds 500 }
if (-not $p.HasExited) {
    Write-Output "TIMEOUT after $timeout s - killing pid $($p.Id)"
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    $code = 'timeout'
} else {
    $code = $p.ExitCode
}
Write-Output "makehuman exit: $code after $([int]$sw.Elapsed.TotalSeconds) s"

# 4) flatten the shoe texture alpha (see makehuman_flatten_alpha.py header)
$shoeTex = Join-Path $root 'ArtSource\Exports\Characters\textures\shoes01_diffuse.png'
if (Test-Path $shoeTex) {
    $blender = 'C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe'
    if (Test-Path $blender) {
        & $blender -b --factory-startup --python (Join-Path $PSScriptRoot 'makehuman_flatten_alpha.py') -- --png $shoeTex 2>&1 |
            Select-String -Pattern 'flattened alpha'
    } else {
        Write-Output "WARNING: blender not found, shoe texture alpha left as-is"
    }
}

Write-Output "`n--- plugin run log ---"
if (Test-Path $env:LL_PILOT_LOG) { Get-Content $env:LL_PILOT_LOG } else { Write-Output "NO RUN LOG - plugin did not execute" }

Write-Output "`n--- makehuman stderr (tail) ---"
$err = Join-Path $userMH 'python_err.txt'
if (Test-Path $err) { Get-Content $err | Select-Object -Last 25 }

Write-Output "`n--- output ---"
if (Test-Path $env:LL_PILOT_FBX) {
    Get-ChildItem (Split-Path $env:LL_PILOT_FBX) | Select-Object Name, Length | Format-Table -AutoSize
} else {
    Write-Output "FBX NOT PRODUCED"
}
