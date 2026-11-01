# Builds the MakeHuman NPC variants from makehuman_characters.json.
# Reuses the verified pilot harness: same embedded Python, same plugin API.
$ErrorActionPreference = 'Stop'
$root   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$userMH = Join-Path $root 'ArtSource\Downloads\MakeHuman\userhome'
$python = Join-Path $root 'Tools\MakeHuman\Python\python.exe'
$mhRoot = Join-Path $root 'Tools\MakeHuman\makehuman'
$plugs  = Join-Path $userMH 'makehuman\v1py3\plugins'
$specs  = Join-Path $PSScriptRoot 'makehuman_characters.json'
$outDir = Join-Path $root 'ArtSource\Exports\Characters'

if (-not (Test-Path $python)) { throw "MakeHuman python missing: $python" }
if (-not (Test-Path $plugs))  { throw "Plugin folder missing: $plugs" }

# 1) install the automation plugin and activate it
Copy-Item (Join-Path $PSScriptRoot 'makehuman_npc_plugin.py') (Join-Path $plugs 'npc_export.py') -Force
$settings = Join-Path $userMH 'makehuman\v1py3\settings.ini'
if (-not (Test-Path $settings)) { throw "MakeHuman settings.ini missing: $settings" }
$txt = Get-Content $settings -Raw
$txt = [regex]::Replace($txt, '"activeUserPlugins"\s*:\s*\[[^\]]*\]', '"activeUserPlugins": ["npc_export"]', 'Singleline')
Set-Content -Path $settings -Value $txt -NoNewline
Write-Output "activeUserPlugins -> $((Select-String -Path $settings -Pattern 'activeUserPlugins' -Context 0,2).Context.PostContext -join '' )"

$characters = Get-Content $specs -Raw | ConvertFrom-Json
foreach ($character in $characters) {
    $name = $character.name
    $oneSpec = Join-Path $env:TEMP "ll_$name.json"
    $json = $character | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($oneSpec, $json, (New-Object System.Text.UTF8Encoding($false)))

    $fbx = Join-Path $outDir "$name.fbx"
    $log = Join-Path $root "TestResults\makehuman-$name.log"

    $env:MH_HOME_LOCATION = $userMH
    $env:LL_REPO_ROOT = $root
    $env:LL_MH_ROOT = $mhRoot
    $env:LL_NPC_SPEC = $oneSpec
    $env:LL_NPC_FBX = $fbx
    $env:LL_NPC_LOG = $log

    Write-Output "=== building $name ==="
    $process = Start-Process -FilePath $python -ArgumentList 'makehuman.py' `
        -WorkingDirectory $mhRoot -WindowStyle Minimized -PassThru
    if (-not $process.WaitForExit(240000)) {
        $process.Kill()
        Write-Output "$name : TIMEOUT after 4 minutes"
        continue
    }

    if (Test-Path $fbx) {
        $size = (Get-Item $fbx).Length
        Write-Output "$name : OK $([math]::Round($size/1MB,2)) MB"
    } else {
        Write-Output "$name : FAILED (no FBX)"
    }
}
