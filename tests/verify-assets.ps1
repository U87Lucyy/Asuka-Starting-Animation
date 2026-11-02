$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path $PSScriptRoot -Parent)
Add-Type -AssemblyName System.Drawing
function Assert($ok, $message) { if (-not $ok) { throw $message } }
$portrait = [Drawing.Bitmap]::FromFile((Join-Path $PWD 'assets\asuka.png'))
try {
    Assert ($portrait.GetPixel(0, 0).A -eq 0) 'Portrait background must be transparent.'
    Assert ($portrait.GetPixel(512, 500).A -gt 240) 'Portrait face must be opaque.'
} finally { $portrait.Dispose() }
foreach ($name in 'asuka-closed.png','asuka-half.png') {
    $eyeFrame = [Drawing.Bitmap]::FromFile((Join-Path $PWD "assets\$name"))
    try {
        Assert ($eyeFrame.Width -eq 1024 -and $eyeFrame.Height -eq 1536) 'Eye animation layers must stay registered to the portrait.'
        if ($name -eq 'asuka-closed.png') { Assert ($eyeFrame.GetPixel(0,0).A -eq 0) 'The outline source must have a transparent background.' }
        Assert ($eyeFrame.GetPixel(466,477).A -gt 240) 'The eye patch must be opaque.'
    } finally { $eyeFrame.Dispose() }
}
$wav = [IO.File]::ReadAllBytes((Join-Path $PWD 'assets\launch.wav'))
Assert ([Text.Encoding]::ASCII.GetString($wav,0,4) -eq 'RIFF') 'WAV RIFF header missing.'
Assert ([BitConverter]::ToInt32($wav,24) -eq 44100) 'Sound must be 44.1 kHz.'
Assert ([BitConverter]::ToInt16($wav,22) -eq 2) 'Sound must be stereo.'
Assert ($wav.Length -eq 882044) 'Sound must be exactly 5 seconds PCM16 stereo.'
$peak = 0
for ($i=44; $i -lt $wav.Length; $i+=2) { $peak = [Math]::Max($peak,[Math]::Abs([int][BitConverter]::ToInt16($wav,$i))) }
Assert ($peak -gt 1000 -and $peak -lt 30000) 'Sound must be non-silent and below clipping.'
$final = [Drawing.Bitmap]::FromFile((Join-Path $PWD 'previews\frame-4.30.png'))
try {
    Assert ((Get-Item 'previews\frame-4.30.png').LastWriteTimeUtc -ge (Get-Item 'AsukaCodex.exe').LastWriteTimeUtc) 'Keyframes must come from the current executable.'
    Assert ($final.Width -eq 1920 -and $final.Height -eq 1080) 'Keyframes must be 1920x1080.'
    $face = $final.GetPixel(1400,500)
    Assert ($face.G -gt 90 -and $face.B -gt 50) 'Final frame must reveal the character face.'
} finally { $final.Dispose() }
function BlueEyePixels($file) {
    $image = [Drawing.Bitmap]::FromFile((Join-Path $PWD "previews\$file"))
    $count = 0
    try {
        for ($y=405; $y -lt 520; $y+=2) { for ($x=1250; $x -lt 1380; $x+=2) {
            $pixel=$image.GetPixel($x,$y)
            if ($pixel.B -gt 80 -and $pixel.B -gt $pixel.G*1.15 -and $pixel.B -gt $pixel.R*1.2) { $count++ }
        } }
    } finally { $image.Dispose() }
    return $count
}
$closedBlue=BlueEyePixels 'frame-2.45.png'
$openBlue=BlueEyePixels 'frame-3.30.png'
Assert ($openBlue -gt $closedBlue+20) 'The eyes must visibly open after the closed-eye phase.'
$lines = [Drawing.Bitmap]::FromFile((Join-Path $PWD 'previews\frame-1.45.png'))
try {
    $lit = 0
    for ($y=200;$y -lt 600;$y+=3) { for ($x=1100;$x -lt 1700;$x+=3) {
        $p=$lines.GetPixel($x,$y)
        if ($p.R -gt 180 -and $p.G -gt 90) { $lit++ }
    } }
    Assert ($lit -gt 150 -and $lit -lt 10000) 'The entrance must contain luminous lines rather than an opaque portrait.'
} finally { $lines.Dispose() }
$wideLines = [Drawing.Bitmap]::FromFile((Join-Path $PWD 'previews\lines-2592.png'))
try {
    Assert ($wideLines.GetPixel(2460,780).G -lt 70) 'Wide layouts must not reveal the colored suit during the line-only entrance.'
} finally { $wideLines.Dispose() }
Write-Host "PASS: registered transparent portraits, five-second WAV (peak=$peak), visible line entrance, eyes open ($closedBlue -> $openBlue blue pixels), final face."
