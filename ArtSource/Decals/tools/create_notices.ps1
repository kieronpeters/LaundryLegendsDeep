# Build the two in-world paper notices as PNG decals.
#
# The paper stock comes from a CC0 ambientCG texture; everything else (layout,
# type, tape) is drawn here with System.Drawing so no additional licence applies.
#
#   powershell -ExecutionPolicy Bypass -File ArtSource/Decals/tools/create_notices.ps1 `
#       -Paper "C:\path\to\Paper002_1K-JPG_Color.jpg"
#
# Writes ArtSource/Decals/06_notices/notice_lost_umbrella_1024x1408.png and
# notice_price_list_1024x1408.png (RGBA, sheet taped to the wall on transparency).

param(
    [Parameter(Mandatory = $true)][string]$Paper,
    [string]$OutDir
)

Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = "Stop"
if (-not $OutDir) { $OutDir = Join-Path $PSScriptRoot "..\06_notices" }

$CanvasW = 1024
$CanvasH = 1408
$SheetTop = 56
$InkColor = [System.Drawing.Color]::FromArgb(255, 34, 30, 26)
$InkSoftColor = [System.Drawing.Color]::FromArgb(210, 60, 55, 48)

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

function New-PaperSheet {
    $bmp = New-Object System.Drawing.Bitmap $CanvasW, $CanvasH, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $sheet = New-Object System.Drawing.Rectangle 0, $SheetTop, $CanvasW, ($CanvasH - $SheetTop)

    # Warm paper base.
    $baseBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 239, 233, 219))
    $g.FillRectangle($baseBrush, $sheet)
    $baseBrush.Dispose()

    # CC0 paper fibre over the base, kept subtle so the type stays crisp.
    $paperImg = [System.Drawing.Image]::FromFile($Paper)
    $attrs = New-Object System.Drawing.Imaging.ImageAttributes
    $cm = New-Object System.Drawing.Imaging.ColorMatrix
    $cm.Matrix33 = 0.42
    $attrs.SetColorMatrix($cm)
    $g.DrawImage($paperImg, $sheet, 0, 0, $paperImg.Width, $paperImg.Height,
        [System.Drawing.GraphicsUnit]::Pixel, $attrs)
    $paperImg.Dispose()

    # Aged edges: warm vignette so the sheet does not read as a flat white quad.
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddRectangle($sheet)
    $edge = New-Object System.Drawing.Drawing2D.PathGradientBrush $path
    $edge.CenterColor = [System.Drawing.Color]::FromArgb(0, 120, 96, 60)
    $edge.SurroundColors = @([System.Drawing.Color]::FromArgb(46, 120, 96, 60))
    $g.FillRectangle($edge, $sheet)
    $edge.Dispose(); $path.Dispose()

    # A few faint damp blotches, bottom-biased like a sheet pinned near a wall.
    foreach ($b in @(@(150, 1180, 250, 170), @(780, 980, 190, 230), @(420, 430, 210, 120), @(880, 1330, 240, 150))) {
        $bp = New-Object System.Drawing.Drawing2D.GraphicsPath
        $bp.AddEllipse($b[0], $b[1], $b[2], $b[3])
        $bb = New-Object System.Drawing.Drawing2D.PathGradientBrush $bp
        $bb.CenterColor = [System.Drawing.Color]::FromArgb(20, 120, 100, 70)
        $bb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 120, 100, 70))
        $g.FillEllipse($bb, $b[0], $b[1], $b[2], $b[3])
        $bb.Dispose(); $bp.Dispose()
    }

    # Tape strip across the top edge, sitting on the transparency.
    $tapeBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(170, 246, 244, 232))
    $g.FillRectangle($tapeBrush, 356, ($SheetTop - 16), 320, 44)
    $tapePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(60, 214, 210, 196)), 2
    $g.DrawRectangle($tapePen, 356, ($SheetTop - 16), 320, 44)
    $tapeBrush.Dispose(); $tapePen.Dispose()

    return @{ Bitmap = $bmp; Graphics = $g }
}

function Draw-Ink([System.Drawing.Graphics]$g, [string]$text, [System.Drawing.Font]$font,
                  [System.Drawing.Brush]$brush, [single]$x, [single]$y, [string]$align = "Left") {
    $sf = New-Object System.Drawing.StringFormat
    if ($align -eq "Center") { $sf.Alignment = [System.Drawing.StringAlignment]::Center }
    if ($align -eq "Right") { $sf.Alignment = [System.Drawing.StringAlignment]::Far }
    # A one-pixel ghost pass fakes ink bleed from a tired photocopier.
    $ghostBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(48, 34, 30, 26))
    $g.DrawString($text, $font, $ghostBrush, ($x + 1), ($y + 1), $sf)
    $g.DrawString($text, $font, $brush, $x, $y, $sf)
    $ghostBrush.Dispose(); $sf.Dispose()
}

function Save-Notice($bmp, [string]$name) {
    $path = Join-Path $OutDir $name
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output ("wrote {0} ({1:N0} KB)" -f $path, ((Get-Item $path).Length / 1KB))
}

# ---------------------------------------------------------------- notice one
$n1 = New-PaperSheet
$g = $n1.Graphics
$inkBrush = New-Object System.Drawing.SolidBrush $InkColor
$inkSoftBrush = New-Object System.Drawing.SolidBrush $InkSoftColor
$fHead = New-Object System.Drawing.Font "Segoe Print", 66, ([System.Drawing.FontStyle]::Bold)
$fSub = New-Object System.Drawing.Font "Segoe Print", 27, ([System.Drawing.FontStyle]::Regular)
$fBody = New-Object System.Drawing.Font "Segoe Print", 26, ([System.Drawing.FontStyle]::Regular)
$fSmall = New-Object System.Drawing.Font "Segoe Print", 21, ([System.Drawing.FontStyle]::Regular)
$fFaint = New-Object System.Drawing.Font "Segoe Print", 21, ([System.Drawing.FontStyle]::Regular)
$faintBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(170, 70, 64, 56))

Draw-Ink $g "LOST" $fHead $inkBrush 512 132 "Center"
Draw-Ink $g "one umbrella" $fSub $inkSoftBrush 512 268 "Center"

$rulePen = New-Object System.Drawing.Pen $InkColor, 3
$g.DrawLine($rulePen, 150, 330, 874, 330)
$rulePen.Dispose()

$lines = @(
    "Blue folding umbrella with a",
    "wooden handle. Left behind in",
    "the waiting chairs on Tuesday",
    "afternoon, most likely under",
    "the round table."
)
$y = 386
foreach ($line in $lines) {
    Draw-Ink $g $line $fBody $inkBrush 128 $y "Left"
    $y += 68
}

Draw-Ink $g "If you find it, please leave it" $fSmall $inkSoftBrush 128 780 "Left"
Draw-Ink $g "at the counter. Do not put it" $fSmall $inkSoftBrush 128 830 "Left"
Draw-Ink $g "in with the wash." $fSmall $inkSoftBrush 128 880 "Left"

$fReward = New-Object System.Drawing.Font "Segoe Print", 30, ([System.Drawing.FontStyle]::Bold)
$rewardText = "REWARD: one free wash"
$rewardWidth = [int]$g.MeasureString($rewardText, $fReward).Width
$boxPen = New-Object System.Drawing.Pen $InkColor, 4
$g.DrawRectangle($boxPen, 128, 990, ($rewardWidth + 52), 96)
Draw-Ink $g $rewardText $fReward $inkBrush 154 1010 "Left"
$boxPen.Dispose()

Draw-Ink $g "posted 14 Oct" $fFaint $faintBrush 128 1180 "Left"
Draw-Ink $g "- R." $fSmall $inkSoftBrush 800 1180 "Left"

$inkBrush.Dispose(); $inkSoftBrush.Dispose(); $faintBrush.Dispose()
$fHead.Dispose(); $fSub.Dispose(); $fBody.Dispose(); $fSmall.Dispose()
$fFaint.Dispose(); $fReward.Dispose()
$g.Dispose()
Save-Notice $n1.Bitmap "notice_lost_umbrella_1024x1408.png"
$n1.Bitmap.Dispose()

# ---------------------------------------------------------------- notice two
$n2 = New-PaperSheet
$g = $n2.Graphics
$inkBrush = New-Object System.Drawing.SolidBrush $InkColor
$fHead = New-Object System.Drawing.Font "Franklin Gothic Medium", 58, ([System.Drawing.FontStyle]::Regular)
$fRow = New-Object System.Drawing.Font "Consolas", 30, ([System.Drawing.FontStyle]::Regular)
$fRowBold = New-Object System.Drawing.Font "Consolas", 30, ([System.Drawing.FontStyle]::Bold)
$fFoot = New-Object System.Drawing.Font "Consolas", 22, ([System.Drawing.FontStyle]::Regular)

Draw-Ink $g "PRICE LIST" $fHead $inkBrush 512 128 "Center"
$rulePen = New-Object System.Drawing.Pen $InkColor, 4
$g.DrawLine($rulePen, 96, 228, 928, 228)
$g.DrawLine($rulePen, 96, 236, 928, 236)
$rulePen.Dispose()

$rows = @(
    @("Wash and fold", "1.20 / lb"),
    @("Wash only", "3.50"),
    @("Dry only", "3.00"),
    @("Shirt, pressed", "2.50"),
    @("Trousers, pressed", "2.75"),
    @("Comforter, single", "12.00"),
    @("Comforter, double", "16.00"),
    @("Curtains, per pair", "9.00"),
    @("Stain treatment", "2.00"),
    @("Same-day service", "+4.00"),
    @("Fold only", "0.60 / lb")
)
$y = 292
$dotPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(150, 90, 84, 74)), 3
$dotPen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dot
foreach ($row in $rows) {
    Draw-Ink $g $row[0] $fRow $inkBrush 104 $y "Left"
    Draw-Ink $g $row[1] $fRowBold $inkBrush 920 $y "Right"
    $labelWidth = $g.MeasureString($row[0], $fRow).Width
    $priceWidth = $g.MeasureString($row[1], $fRowBold).Width
    $dotY = $y + 36
    $g.DrawLine($dotPen, (104 + $labelWidth + 14), $dotY, (920 - $priceWidth - 18), $dotY)
    $y += 74
}
$dotPen.Dispose()

$rulePen = New-Object System.Drawing.Pen $InkColor, 4
$g.DrawLine($rulePen, 96, 1128, 928, 1128)
$rulePen.Dispose()

Draw-Ink $g "Drop off before 10am" $fFoot $inkBrush 104 1160 "Left"
Draw-Ink $g "Ready same day, 6pm" $fFoot $inkBrush 104 1204 "Left"
Draw-Ink $g "Open 7am - 9pm, Mon to Sat" $fFoot $inkBrush 104 1268 "Left"
Draw-Ink $g "Closed Sundays and holidays" $fFoot $inkBrush 104 1312 "Left"

$inkBrush.Dispose()
$fHead.Dispose(); $fRow.Dispose(); $fRowBold.Dispose(); $fFoot.Dispose()
$g.Dispose()
Save-Notice $n2.Bitmap "notice_price_list_1024x1408.png"
$n2.Bitmap.Dispose()
