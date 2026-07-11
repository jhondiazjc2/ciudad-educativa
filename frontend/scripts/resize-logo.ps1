$dest = Join-Path $PSScriptRoot '..\public\CiudadEducativalogo.png'
Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile($dest)
$w = 320
$h = [int][Math]::Round($img.Height * ($w / $img.Width))
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($img, 0, 0, $w, $h)
$img.Dispose()
$g.Dispose()
$bmp.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output "Resized to ${w}x${h}, bytes: $((Get-Item $dest).Length)"
