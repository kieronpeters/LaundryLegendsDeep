Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
    public const uint KEYUP = 0x0002;
    public const uint MOUSEEVENTF_MOVE = 0x0001;
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
}
"@

$exe = 'C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends\Build\LaundryLegends.exe'
$out = 'C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends\TestResults\playtest3'
New-Item -ItemType Directory -Force -Path $out | Out-Null

function Save-Screen([string]$name) {
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap($b.Width, $b.Height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($b.X, $b.Y, 0, 0, $bmp.Size)
    $p = Join-Path $out $name
    $bmp.Save($p, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "saved $name"
}

function Hold-Key([int]$vk, [int]$ms) {
    [Win]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $ms
    [Win]::keybd_event($vk, 0, [Win]::KEYUP, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 250
}

function Turn([int]$dx) {
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    [Win]::SetCursorPos([int]($b.Width/2), [int]($b.Height/2))
    Start-Sleep -Milliseconds 120
    for ($i = 0; $i -lt 10; $i++) {
        [Win]::mouse_event([Win]::MOUSEEVENTF_MOVE, [int]($dx/10), 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 40
    }
    Start-Sleep -Milliseconds 200
}

$proc = Start-Process -FilePath $exe -PassThru
Start-Sleep -Seconds 10
$shell = New-Object -ComObject WScript.Shell
$null = $shell.AppActivate($proc.Id)
Start-Sleep -Seconds 1

# Fullscreen
[System.Windows.Forms.SendKeys]::SendWait('%{ENTER}')
Start-Sleep -Seconds 3
Save-Screen '01-menu-fullscreen.png'

# Start with mouse click on the START GAME button (centre, ~62% down).
$b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
[Win]::SetCursorPos([int]($b.Width * 0.5), [int]($b.Height * 0.62))
Start-Sleep -Milliseconds 300
[Win]::mouse_event([Win]::MOUSEEVENTF_LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
[Win]::mouse_event([Win]::MOUSEEVENTF_LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Seconds 9
Save-Screen '02-game-start.png'

# Walk forward, then look around, then walk again.
Hold-Key 0x57 1400
Save-Screen '03-walk-forward.png'

Turn 320
Save-Screen '04-turn-right.png'

Hold-Key 0x57 1800
Save-Screen '05-walk-toward-room.png'

# Try to interact with whatever is in front.
Hold-Key 0x45 200
Save-Screen '06-interact.png'

# Boundary test: push into a wall for 2s.
Hold-Key 0x57 2000
Save-Screen '07-boundary.png'

# Look down at hands.
[Win]::SetCursorPos([int]($b.Width/2), [int]($b.Height/2))
for ($i = 0; $i -lt 12; $i++) { [Win]::mouse_event([Win]::MOUSEEVENTF_MOVE, 0, 45, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 40 }
Start-Sleep -Milliseconds 300
Save-Screen '08-hands-down.png'

$alive = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
Write-Output ("running=" + [bool]$alive)
