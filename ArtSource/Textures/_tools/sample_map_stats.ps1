# Sample the mean luminance of each roughness map.
#
# Unity's Built-In "Standard" shader has no roughness slot: it uses a packed
# Metallic (R) + Smoothness (A) map. Until a channel-packing step exists, the
# mean roughness below is the value to type into the material's Smoothness
# slider (Smoothness = 1 - Roughness) so a flat approximation looks right.
#
# Run from the repository root:
#   powershell -ExecutionPolicy Bypass -File ArtSource/Textures/_tools/sample_map_stats.ps1

Add-Type -AssemblyName System.Drawing

$root = Join-Path $PSScriptRoot '..'
Get-ChildItem -Path $root -Directory | Where-Object { $_.Name -notlike '_*' } | ForEach-Object {
    $set = $_.Name
    Get-ChildItem -Path $_.FullName -Filter '*_Roughness.jpg' | ForEach-Object {
        $src = [System.Drawing.Image]::FromFile($_.FullName)
        # Downscale to 64x64 so the average is fast and still representative.
        $small = New-Object System.Drawing.Bitmap 64, 64
        $g = [System.Drawing.Graphics]::FromImage($small)
        $g.DrawImage($src, 0, 0, 64, 64)
        $g.Dispose()
        $src.Dispose()

        $sum = 0.0
        for ($y = 0; $y -lt 64; $y++) {
            for ($x = 0; $x -lt 64; $x++) {
                $c = $small.GetPixel($x, $y)
                $sum += (0.2126 * $c.R + 0.7152 * $c.G + 0.0722 * $c.B) / 255.0
            }
        }
        $small.Dispose()

        $mean = $sum / 4096.0
        [PSCustomObject]@{
            Set             = $set
            Map             = $_.Name
            MeanRoughness   = [math]::Round($mean, 3)
            SuggestedSmooth = [math]::Round(1.0 - $mean, 3)
        }
    }
} | Format-Table -AutoSize
