Add-Type -AssemblyName System.Drawing
$bitmap = New-Object System.Drawing.Bitmap 64,64
$g = [System.Drawing.Graphics]::FromImage($bitmap)
$g.Clear([System.Drawing.Color]::Transparent)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.FillRectangle([System.Drawing.Brushes]::BurlyWood,5,12,54,40)
$g.FillRectangle([System.Drawing.Brushes]::MidnightBlue,8,15,48,34)
$g.FillRectangle([System.Drawing.Brushes]::IndianRed,12,19,40,26)
$pts = [System.Drawing.Point[]]@([System.Drawing.Point]::new(32,21),[System.Drawing.Point]::new(44,32),[System.Drawing.Point]::new(32,43),[System.Drawing.Point]::new(20,32))
$g.FillPolygon([System.Drawing.Brushes]::Wheat,$pts)
$g.FillEllipse([System.Drawing.Brushes]::MidnightBlue,28,28,8,8)
$icon=[System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
$stream=[System.IO.File]::Create((Join-Path $PSScriptRoot '..\assets\app.ico'))
$icon.Save($stream);$stream.Dispose();$icon.Dispose();$g.Dispose();$bitmap.Dispose()
