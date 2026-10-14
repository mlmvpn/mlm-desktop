$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$payload=Join-Path $root 'dist\payload.zip'
if(Test-Path $payload){Remove-Item -LiteralPath $payload}
$archive=[System.IO.Compression.ZipFile]::Open($payload,[System.IO.Compression.ZipArchiveMode]::Create)
try{foreach($file in Get-Item "$root\dist\app\MLMDesktop.exe","$root\dist\app\MLMDesktop.exe.config","$root\dist\app\assets\rug.png","$root\dist\app\assets\MLM Desktop.png","$root\dist\app\assets\app.ico"){$name=$file.FullName.Substring((Join-Path $root 'dist\app').Length+1).Replace('\','/');[System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,$name,[System.IO.Compression.CompressionLevel]::Optimal)|Out-Null}}finally{$archive.Dispose()}
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /optimize+ /platform:x64 /target:winexe "/out:$root\dist\MLMDesktop-Setup-0.2.0.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "/resource:$payload,payload.zip" "/win32manifest:$root\src\app.manifest" "/win32icon:$root\assets\app.ico" "/resource:$PSScriptRoot\MLM Desktop.theme,MLMDesktop.theme" "$PSScriptRoot\Setup.cs" "$root\src\ThemeSelection.cs" "$root\src\DesktopMode.cs" "$root\src\Autostart.cs"
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed'}
Copy-Item "$root\src\MLMDesktop.exe.config" "$root\dist\MLMDesktop-Setup-0.2.0.exe.config" -Force
Copy-Item $payload "$root\dist\MLMDesktop-Portable-0.2.0.zip" -Force
Write-Host 'Setup and portable ZIP built.'
