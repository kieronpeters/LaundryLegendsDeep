# Draws two original retro arcade posters (wordplay, no copied boxart).
Add-Type -AssemblyName System.Drawing

$outDir = Join-Path (Get-Location) 'Assets\LaundryLegends\Art\Textures\Posters'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$W = 512
$H = 768
$Scale = 2
$BitmapW = $W * $Scale
$BitmapH = $H * $Scale

function New-Canvas {
    $bmp = New-Object System.Drawing.Bitmap $BitmapW, $BitmapH
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.ScaleTransform($Scale, $Scale)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
    return @($bmp, $g)
}

function Draw-OutlinedText($g, $text, $font, $y, $color) {
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = [System.Drawing.StringAlignment]::Center
    $black = [System.Drawing.Brushes]::Black
    for ($dx = -3; $dx -le 3; $dx += 3) {
        for ($dy = -3; $dy -le 3; $dy += 3) {
            $g.DrawString($text, $font, $black, (New-Object System.Drawing.PointF (($W/2)+$dx), ($y+$dy)), $fmt)
        }
    }
    $brush = New-Object System.Drawing.SolidBrush $color
    $g.DrawString($text, $font, $brush, (New-Object System.Drawing.PointF ($W/2), $y), $fmt)
    $brush.Dispose(); $fmt.Dispose()
}

function Draw-Fighter($g, $x, $y, $scale, $color) {
    $brush = New-Object System.Drawing.SolidBrush $color
    # Head
    $g.FillEllipse($brush, ($x - 22*$scale), ($y - 190*$scale), (44*$scale), (44*$scale))
    # Torso
    $torso = @(
        (New-Object System.Drawing.PointF (($x-42*$scale), ($y-150*$scale))),
        (New-Object System.Drawing.PointF (($x+42*$scale), ($y-150*$scale))),
        (New-Object System.Drawing.PointF (($x+30*$scale), ($y-30*$scale))),
        (New-Object System.Drawing.PointF (($x-30*$scale), ($y-30*$scale)))
    )
    $g.FillPolygon($brush, $torso)
    # Punching arm
    $arm = New-Object System.Drawing.Pen $color, (26*$scale)
    $arm.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $arm.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($arm, ($x+30*$scale), ($y-130*$scale), ($x+95*$scale), ($y-110*$scale))
    # Guard arm
    $g.DrawLine($arm, ($x-30*$scale), ($y-130*$scale), ($x-70*$scale), ($y-80*$scale))
    # Legs
    $g.DrawLine($arm, ($x-16*$scale), ($y-35*$scale), ($x-55*$scale), ($y+95*$scale))
    $g.DrawLine($arm, ($x+16*$scale), ($y-35*$scale), ($x+60*$scale), ($y+95*$scale))
    $arm.Dispose(); $brush.Dispose()
}

function Draw-Sunburst($g, $cx, $cy, $color) {
    $brush = New-Object System.Drawing.SolidBrush $color
    for ($i = 0; $i -lt 20; $i++) {
        $a0 = ($i * 18) * [Math]::PI / 180
        $a1 = ($i * 18 + 8) * [Math]::PI / 180
        $pts = @(
            (New-Object System.Drawing.PointF $cx, $cy),
            (New-Object System.Drawing.PointF ($cx + 900*[Math]::Cos($a0)), ($cy + 900*[Math]::Sin($a0))),
            (New-Object System.Drawing.PointF ($cx + 900*[Math]::Cos($a1)), ($cy + 900*[Math]::Sin($a1)))
        )
        $g.FillPolygon($brush, $pts)
    }
    $brush.Dispose()
}

function Draw-Border($g) {
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 226, 178, 64)), 14
    $g.DrawRectangle($pen, 14, 14, ($W-28), ($H-28))
    $pen.Dispose()
}

# ---------------- Poster 1: STREET FOLDERS II ----------------
$bmp, $g = New-Canvas
$rect = New-Object System.Drawing.Rectangle 0, 0, $W, $H
$grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 12, 16, 40)), ([System.Drawing.Color]::FromArgb(255, 58, 18, 70)), 90
$g.FillRectangle($grad, $rect)
$grad.Dispose()
Draw-Sunburst $g 256 330 ([System.Drawing.Color]::FromArgb(90, 240, 130, 40))
Draw-Fighter $g 165 560 1.15 ([System.Drawing.Color]::FromArgb(255, 40, 90, 190))
Draw-Fighter $g 355 560 1.15 ([System.Drawing.Color]::FromArgb(255, 190, 45, 45))
$band = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(210, 10, 10, 14))
$g.FillRectangle($band, 0, 40, $W, 250)
$band.Dispose()
Draw-OutlinedText $g 'STREET' (New-Object System.Drawing.Font 'Impact', 62) 48 ([System.Drawing.Color]::FromArgb(255, 255, 205, 60))
Draw-OutlinedText $g 'FOLDERS' (New-Object System.Drawing.Font 'Impact', 62) 118 ([System.Drawing.Color]::FromArgb(255, 255, 205, 60))
Draw-OutlinedText $g 'II' (New-Object System.Drawing.Font 'Impact', 54) 190 ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
Draw-OutlinedText $g 'THE FINAL RINSE' ([System.Drawing.Font]::new('Arial', 22, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)) 640 ([System.Drawing.Color]::FromArgb(255, 255, 160, 60))
Draw-OutlinedText $g '2 PLAYERS  -  1 BASKET' (New-Object System.Drawing.Font 'Arial', 16) 700 ([System.Drawing.Color]::FromArgb(255, 220, 220, 220))
Draw-Border $g
$bmp.Save((Join-Path $outDir 'street_folders_ii.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

# ---------------- Poster 2: SPIN CYCLE SHOWDOWN ----------------
$bmp, $g = New-Canvas
$rect = New-Object System.Drawing.Rectangle 0, 0, $W, $H
$grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 6, 30, 40)), ([System.Drawing.Color]::FromArgb(255, 10, 80, 80)), 90
$g.FillRectangle($grad, $rect)
$bubble = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(70, 200, 240, 255))
foreach ($b in @(@(80,300,60),@(420,240,40),@(380,520,70),@(120,560,35),@(460,620,50))) {
    $g.FillEllipse($bubble, ($b[0]-$b[2]/2), ($b[1]-$b[2]/2), $b[2], $b[2])
}
$bubble.Dispose()
Draw-Fighter $g 256 600 1.35 ([System.Drawing.Color]::FromArgb(255, 30, 80, 90))
# Laundry basket in the fighter's hands
$basket = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 210, 190, 120))
$g.FillRectangle($basket, 150, 470, 210, 90)
$g.FillRectangle($basket, 165, 440, 180, 40)
$basket.Dispose()
$band = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(210, 4, 18, 24))
$g.FillRectangle($band, 0, 40, $W, 235)
$band.Dispose()
Draw-OutlinedText $g 'SPIN CYCLE' (New-Object System.Drawing.Font 'Impact', 58) 46 ([System.Drawing.Color]::FromArgb(255, 120, 240, 230))
Draw-OutlinedText $g 'SHOWDOWN' (New-Object System.Drawing.Font 'Impact', 58) 112 ([System.Drawing.Color]::FromArgb(255, 120, 240, 230))
Draw-OutlinedText $g 'SUDS OF FURY' ([System.Drawing.Font]::new('Arial', 22, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)) 640 ([System.Drawing.Color]::FromArgb(255, 255, 205, 60))
Draw-OutlinedText $g 'INSERT COIN' (New-Object System.Drawing.Font 'Arial', 18) 700 ([System.Drawing.Color]::FromArgb(255, 220, 220, 220))
Draw-Border $g
$bmp.Save((Join-Path $outDir 'spin_cycle_showdown.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

# ---------------- Poster 3: RACER RIOT ----------------
$bmp, $g = New-Canvas
$rect = New-Object System.Drawing.Rectangle 0, 0, $W, $H
$grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 18, 8, 42)), ([System.Drawing.Color]::FromArgb(255, 18, 70, 110)), 90
$g.FillRectangle($grad, $rect)
$grad.Dispose()
Draw-Sunburst $g 256 430 ([System.Drawing.Color]::FromArgb(85, 255, 120, 40))

# Speed lines
$linePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(180, 120, 220, 255)), 8
$linePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$linePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawLine($linePen, 30, 420, 150, 420)
$g.DrawLine($linePen, 20, 470, 130, 470)
$g.DrawLine($linePen, 40, 520, 140, 520)
$g.DrawLine($linePen, 370, 420, 490, 420)
$g.DrawLine($linePen, 390, 470, 500, 470)
$linePen.Dispose()

# Car body
$body = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 220, 64, 42))
$g.FillRectangle($body, 105, 520, 302, 92)
$roof = @(
    (New-Object System.Drawing.PointF 160, 520),
    (New-Object System.Drawing.PointF 210, 450),
    (New-Object System.Drawing.PointF 330, 450),
    (New-Object System.Drawing.PointF 385, 520)
)
$g.FillPolygon($body, $roof)
$body.Dispose()
$window = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 30, 40, 70))
$g.FillRectangle($window, 205, 472, 118, 38)
$window.Dispose()
$wheel = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 12, 12, 16))
$g.FillEllipse($wheel, 130, 585, 78, 78)
$g.FillEllipse($wheel, 315, 585, 78, 78)
$wheel.Dispose()
$hub = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 210, 210, 220))
$g.FillEllipse($hub, 156, 611, 26, 26)
$g.FillEllipse($hub, 341, 611, 26, 26)
$hub.Dispose()

$band = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(215, 8, 8, 18))
$g.FillRectangle($band, 0, 40, $W, 240)
$band.Dispose()
Draw-OutlinedText $g 'RACER' (New-Object System.Drawing.Font 'Impact', 66) 46 ([System.Drawing.Color]::FromArgb(255, 255, 170, 60))
Draw-OutlinedText $g 'RIOT' (New-Object System.Drawing.Font 'Impact', 66) 116 ([System.Drawing.Color]::FromArgb(255, 255, 170, 60))
Draw-OutlinedText $g 'TURBO RINSE' ([System.Drawing.Font]::new('Arial', 22, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)) 640 ([System.Drawing.Color]::FromArgb(255, 120, 230, 255))
Draw-OutlinedText $g 'INSERT COIN' (New-Object System.Drawing.Font 'Arial', 18) 700 ([System.Drawing.Color]::FromArgb(255, 220, 220, 220))
Draw-Border $g
$bmp.Save((Join-Path $outDir 'racer_riot.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

# ---------------- Poster 4: LAUNDRO FIGHTER ----------------
$bmp, $g = New-Canvas
$rect = New-Object System.Drawing.Rectangle 0, 0, $W, $H
$grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 8, 18, 46)), ([System.Drawing.Color]::FromArgb(255, 22, 60, 120)), 90
$g.FillRectangle($grad, $rect)
$grad.Dispose()
Draw-Sunburst $g 256 360 ([System.Drawing.Color]::FromArgb(80, 90, 190, 255))
Draw-Fighter $g 160 560 1.2 ([System.Drawing.Color]::FromArgb(255, 40, 90, 190))
Draw-Fighter $g 360 560 1.2 ([System.Drawing.Color]::FromArgb(255, 210, 170, 40))
$band = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(215, 8, 10, 24))
$g.FillRectangle($band, 0, 40, $W, 240)
$band.Dispose()
Draw-OutlinedText $g 'LAUNDRO' (New-Object System.Drawing.Font 'Impact', 66) 46 ([System.Drawing.Color]::FromArgb(255, 255, 210, 60))
Draw-OutlinedText $g 'FIGHTER' (New-Object System.Drawing.Font 'Impact', 66) 116 ([System.Drawing.Color]::FromArgb(255, 255, 210, 60))
Draw-OutlinedText $g 'FOLD OR BE FOLDED' ([System.Drawing.Font]::new('Arial', 22, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)) 640 ([System.Drawing.Color]::FromArgb(255, 120, 220, 255))
Draw-OutlinedText $g 'INSERT COIN' (New-Object System.Drawing.Font 'Arial', 18) 700 ([System.Drawing.Color]::FromArgb(255, 220, 220, 220))
Draw-Border $g
$bmp.Save((Join-Path $outDir 'laundro_fighter.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

# ---------------- Poster 5: SUDS OF FURY ----------------
$bmp, $g = New-Canvas
$rect = New-Object System.Drawing.Rectangle 0, 0, $W, $H
$grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 30, 8, 48)), ([System.Drawing.Color]::FromArgb(255, 10, 70, 90)), 90
$g.FillRectangle($grad, $rect)
$grad.Dispose()
$bubble = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 160, 255, 240))
foreach ($b in @(@(90,300,90),@(430,260,60),@(390,540,110),@(120,590,70),@(470,640,40),@(60,470,45))) {
    $g.FillEllipse($bubble, ($b[0]-$b[2]/2), ($b[1]-$b[2]/2), $b[2], $b[2])
}
$bubble.Dispose()
Draw-Fighter $g 256 600 1.3 ([System.Drawing.Color]::FromArgb(255, 200, 40, 120))
$basket = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 220, 210, 240))
$g.FillRectangle($basket, 160, 470, 190, 90)
$g.FillRectangle($basket, 175, 445, 160, 30)
$basket.Dispose()
$band = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(215, 16, 6, 30))
$g.FillRectangle($band, 0, 40, $W, 235)
$band.Dispose()
Draw-OutlinedText $g 'SUDS OF' (New-Object System.Drawing.Font 'Impact', 64) 46 ([System.Drawing.Color]::FromArgb(255, 120, 250, 235))
Draw-OutlinedText $g 'FURY' (New-Object System.Drawing.Font 'Impact', 72) 118 ([System.Drawing.Color]::FromArgb(255, 255, 120, 190))
Draw-OutlinedText $g 'THE STAIN STRIKES BACK' ([System.Drawing.Font]::new('Arial', 21, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)) 640 ([System.Drawing.Color]::FromArgb(255, 255, 205, 60))
Draw-OutlinedText $g 'INSERT COIN' (New-Object System.Drawing.Font 'Arial', 18) 700 ([System.Drawing.Color]::FromArgb(255, 220, 220, 220))
Draw-Border $g
$bmp.Save((Join-Path $outDir 'suds_of_fury.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

"posters written to $outDir (2x resolution)"