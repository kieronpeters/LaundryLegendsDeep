#requires -Version 5.1
<#
.SYNOPSIS
  Window capture and synthetic input for the Laundry Legends player build.

.DESCRIPTION
  One script, several actions, so an agent can see the running game and drive it
  with the same coordinate space it sees:

    windows   list processes that have a visible main window
    capture   capture the window client area to a PNG (PrintWindow, then a
              desktop copy as a fallback for DirectX surfaces)
    click     click at client-relative coordinates
    keys      press keys in sequence, e.g. "w:400,e:80,escape:60"
    move      relative mouse movement (camera look)
    type      type literal text
    focus     bring the window to the foreground

  Coordinates for click are client-area pixels of the captured image. The JSON
  returned by capture reports the client origin and size so callers can map
  between the image and the screen.

.EXAMPLE
  .\game-vision.ps1 -Action windows
  .\game-vision.ps1 -Action capture -ProcessName LaundryLegends -Out TestResults\game-vision\shot.png
  .\game-vision.ps1 -Action keys -ProcessName LaundryLegends -Keys "w:600"
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('windows', 'capture', 'click', 'keys', 'move', 'type', 'focus')]
    [string]$Action,

    [string]$ProcessName = 'LaundryLegends',
    [string]$TitleLike,
    [int]$ProcessId,

    [string]$Out,
    [int]$X = 0,
    [int]$Y = 0,
    [int]$W = 0,
    [int]$H = 0,

    [ValidateSet('left', 'right', 'middle')]
    [string]$Button = 'left',
    [int]$Count = 1,

    [string]$Keys,
    [string]$Text,
    [int]$DX = 0,
    [int]$DY = 0,
    [switch]$NoFocus
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class LLNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint flags);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int command);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint code, uint mapType);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
'@

# Per-monitor v2 awareness keeps capture pixels and click pixels identical.
try { [void][LLNative]::SetProcessDpiAwarenessContext([IntPtr](-4)) } catch { }

$MOUSEEVENTF_MOVE = 0x0001
$MOUSEEVENTF_LEFTDOWN = 0x0002
$MOUSEEVENTF_LEFTUP = 0x0004
$MOUSEEVENTF_RIGHTDOWN = 0x0008
$MOUSEEVENTF_RIGHTUP = 0x0010
$MOUSEEVENTF_MIDDLEDOWN = 0x0020
$MOUSEEVENTF_MIDDLEUP = 0x0040
$KEYEVENTF_KEYUP = 0x0002

function Get-TargetWindow {
    if ($ProcessId -gt 0) {
        $process = Get-Process -Id $ProcessId -ErrorAction Stop
        if ($process.MainWindowHandle -eq 0) { throw "Process $ProcessId has no main window." }
        return $process
    }

    $candidates = Get-Process | Where-Object {
        $_.MainWindowHandle -ne 0 -and (
            ($ProcessName -and $_.ProcessName -like "*$ProcessName*") -or
            ($TitleLike -and $_.MainWindowTitle -like "*$TitleLike*")
        )
    }

    $process = @($candidates) | Select-Object -First 1
    if (-not $process) { throw "No visible window matched ProcessName='$ProcessName' TitleLike='$TitleLike'." }
    return $process
}

function Focus-Window([IntPtr]$handle) {
    if ([LLNative]::GetForegroundWindow() -eq $handle) { return $true }
    [void][LLNative]::ShowWindow($handle, 9) # SW_RESTORE
    [void][LLNative]::SetForegroundWindow($handle)
    Start-Sleep -Milliseconds 80
    return ([LLNative]::GetForegroundWindow() -eq $handle)
}

function Get-ClientMetrics([IntPtr]$handle) {
    $windowRect = New-Object LLNative+RECT
    $clientRect = New-Object LLNative+RECT
    [void][LLNative]::GetWindowRect($handle, [ref]$windowRect)
    [void][LLNative]::GetClientRect($handle, [ref]$clientRect)

    $origin = New-Object LLNative+POINT
    $origin.X = 0
    $origin.Y = 0
    [void][LLNative]::ClientToScreen($handle, [ref]$origin)

    [pscustomobject]@{
        Window = [pscustomobject]@{
            x = $windowRect.Left; y = $windowRect.Top
            width = $windowRect.Right - $windowRect.Left
            height = $windowRect.Bottom - $windowRect.Top
        }
        ClientOrigin = [pscustomobject]@{ x = $origin.X; y = $origin.Y }
        ClientWidth = $clientRect.Right - $clientRect.Left
        ClientHeight = $clientRect.Bottom - $clientRect.Top
        Dpi = [LLNative]::GetDpiForWindow($handle)
    }
}

function Convert-KeyNameToVirtualKey([string]$name) {
    $map = @{
        'space' = 0x20; 'enter' = 0x0D; 'return' = 0x0D; 'escape' = 0x1B; 'esc' = 0x1B
        'tab' = 0x09; 'shift' = 0x10; 'ctrl' = 0x11; 'control' = 0x11; 'alt' = 0x12
        'up' = 0x26; 'down' = 0x28; 'left' = 0x25; 'right' = 0x27
        'backspace' = 0x08; 'delete' = 0x2E; 'home' = 0x24; 'end' = 0x23
        'pageup' = 0x21; 'pagedown' = 0x22; 'insert' = 0x2D
        'f1' = 0x70; 'f2' = 0x71; 'f3' = 0x72; 'f4' = 0x73; 'f5' = 0x74; 'f6' = 0x75
        'f7' = 0x76; 'f8' = 0x77; 'f9' = 0x78; 'f10' = 0x79; 'f11' = 0x7A; 'f12' = 0x7B
    }

    $lower = $name.ToLowerInvariant()
    if ($map.ContainsKey($lower)) { return [byte]$map[$lower] }
    if ($lower.Length -eq 1) {
        $char = $lower[0]
        if ($char -match '[a-z]') { return [byte]([int][char]$char.ToString().ToUpperInvariant()) }
        if ($char -match '[0-9]') { return [byte][int][char]$char }
    }

    throw "Unknown key name '$name'. Use letters, digits, space, enter, escape, tab, shift, ctrl, alt, arrows, f1-f12."
}

function Send-Key([byte]$virtualKey, [int]$holdMilliseconds) {
    [LLNative]::keybd_event($virtualKey, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds ([Math]::Max(20, $holdMilliseconds))
    [LLNative]::keybd_event($virtualKey, 0, $KEYEVENTF_KEYUP, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 25
}

switch ($Action) {
    'windows' {
        Get-Process | Where-Object { $_.MainWindowHandle -ne 0 } |
            Sort-Object ProcessName |
            Select-Object ProcessName, Id, MainWindowTitle,
                @{n = 'handle'; e = { $_.MainWindowHandle.ToInt64() } } |
            ConvertTo-Json -Depth 3
        break
    }

    'capture' {
        $process = Get-TargetWindow
        $handle = $process.MainWindowHandle
        $metrics = Get-ClientMetrics $handle

        $captureX = [Math]::Max(0, [Math]::Min($X, $metrics.ClientWidth - 1))
        $captureY = [Math]::Max(0, [Math]::Min($Y, $metrics.ClientHeight - 1))
        $captureW = if ($W -gt 0) { [Math]::Min($W, $metrics.ClientWidth - $captureX) } else { $metrics.ClientWidth - $captureX }
        $captureH = if ($H -gt 0) { [Math]::Min($H, $metrics.ClientHeight - $captureY) } else { $metrics.ClientHeight - $captureY }

        if (-not $Out) {
            $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
            $Out = Join-Path 'TestResults\game-vision' "capture-$stamp.png"
        }

        $outPath = [System.IO.Path]::GetFullPath($Out)
        $outDir = [System.IO.Path]::GetDirectoryName($outPath)
        if ($outDir -and -not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

        $mode = 'printwindow'
        $full = New-Object System.Drawing.Bitmap $metrics.Window.width, $metrics.Window.height
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($full)
            try {
                $hdc = $graphics.GetHdc()
                try { $ok = [LLNative]::PrintWindow($handle, $hdc, 2) } finally { $graphics.ReleaseHdc($hdc) }
            } finally { $graphics.Dispose() }

            # DirectX surfaces often print black; sample the middle and fall back.
            $sample = @(
                $full.GetPixel([int]($full.Width / 2), [int]($full.Height / 2)),
                $full.GetPixel([int]($full.Width / 4), [int]($full.Height / 3)),
                $full.GetPixel([int]($full.Width * 3 / 4), [int]($full.Height * 2 / 3))
            )
            $dark = @($sample | Where-Object { $_.R -lt 8 -and $_.G -lt 8 -and $_.B -lt 8 }).Count

            $cropX = $metrics.ClientOrigin.x - $metrics.Window.x + $captureX
            $cropY = $metrics.ClientOrigin.y - $metrics.Window.y + $captureY

            if (-not $ok -or $dark -ge 2) {
                $mode = 'desktop-copy'
                $full.Dispose()
                $full = New-Object System.Drawing.Bitmap $metrics.Window.width, $metrics.Window.height
                $graphics = [System.Drawing.Graphics]::FromImage($full)
                $graphics.CopyFromScreen(
                    $metrics.Window.x, $metrics.Window.y, 0, 0,
                    (New-Object System.Drawing.Size $metrics.Window.width, $metrics.Window.height))
                $graphics.Dispose()
            }

            $crop = New-Object System.Drawing.Bitmap $captureW, $captureH
            $cropGraphics = [System.Drawing.Graphics]::FromImage($crop)
            $cropGraphics.DrawImage(
                $full,
                (New-Object System.Drawing.Rectangle 0, 0, $captureW, $captureH),
                (New-Object System.Drawing.Rectangle $cropX, $cropY, $captureW, $captureH),
                [System.Drawing.GraphicsUnit]::Pixel)
            $cropGraphics.Dispose()
            $crop.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
            $crop.Dispose()
        }
        finally {
            if ($full) { $full.Dispose() }
        }

        [pscustomobject]@{
            pid = $process.Id
            title = $process.MainWindowTitle
            handle = $handle.ToInt64()
            captureMode = $mode
            image = $outPath
            imageWidth = $captureW
            imageHeight = $captureH
            cropX = $captureX
            cropY = $captureY
            clientOrigin = $metrics.ClientOrigin
            clientSize = @{ width = $metrics.ClientWidth; height = $metrics.ClientHeight }
            dpi = $metrics.Dpi
        } | ConvertTo-Json -Depth 5
        break
    }

    'focus' {
        $process = Get-TargetWindow
        $focused = Focus-Window $process.MainWindowHandle
        [pscustomobject]@{ pid = $process.Id; title = $process.MainWindowTitle; focused = $focused } | ConvertTo-Json
        break
    }

    'click' {
        $process = Get-TargetWindow
        $handle = $process.MainWindowHandle
        $metrics = Get-ClientMetrics $handle
        if (-not $NoFocus) { [void](Focus-Window $handle) }

        $screenX = $metrics.ClientOrigin.x + $X
        $screenY = $metrics.ClientOrigin.y + $Y
        [void][LLNative]::SetCursorPos($screenX, $screenY)
        Start-Sleep -Milliseconds 60

        $down, $up = switch ($Button) {
            'right' { $MOUSEEVENTF_RIGHTDOWN, $MOUSEEVENTF_RIGHTUP }
            'middle' { $MOUSEEVENTF_MIDDLEDOWN, $MOUSEEVENTF_MIDDLEUP }
            default { $MOUSEEVENTF_LEFTDOWN, $MOUSEEVENTF_LEFTUP }
        }

        for ($i = 0; $i -lt [Math]::Max(1, $Count); $i++) {
            [LLNative]::mouse_event($down, 0, 0, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 40
            [LLNative]::mouse_event($up, 0, 0, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 70
        }

        [pscustomobject]@{
            pid = $process.Id; screenX = $screenX; screenY = $screenY
            clientX = $X; clientY = $Y; button = $Button; count = $Count
        } | ConvertTo-Json
        break
    }

    'move' {
        $process = Get-TargetWindow
        if (-not $NoFocus) { [void](Focus-Window $process.MainWindowHandle) }
        [LLNative]::mouse_event($MOUSEEVENTF_MOVE, $DX, $DY, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 60
        [pscustomobject]@{ pid = $process.Id; dx = $DX; dy = $DY } | ConvertTo-Json
        break
    }

    'keys' {
        if (-not $Keys) { throw "keys action needs -Keys, e.g. 'w:400,e:80'." }
        $process = Get-TargetWindow
        if (-not $NoFocus) { [void](Focus-Window $process.MainWindowHandle) }

        $sent = @()
        foreach ($part in $Keys.Split(',')) {
            $bits = $part.Trim().Split(':')
            $name = $bits[0]
            $hold = if ($bits.Length -gt 1) { [int]$bits[1] } else { 80 }
            $virtualKey = Convert-KeyNameToVirtualKey $name
            Send-Key $virtualKey $hold
            $sent += $name
        }

        [pscustomobject]@{ pid = $process.Id; keys = $sent } | ConvertTo-Json
        break
    }

    'type' {
        if (-not $Text) { throw "type action needs -Text." }
        $process = Get-TargetWindow
        if (-not $NoFocus) { [void](Focus-Window $process.MainWindowHandle) }
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.SendKeys]::SendWait($Text)
        [pscustomobject]@{ pid = $process.Id; typed = $Text.Length } | ConvertTo-Json
        break
    }
}
