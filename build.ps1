param([switch]$SkipInstaller)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$wpf = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
$out = Join-Path $root 'dist\app'
New-Item -ItemType Directory -Force $out | Out-Null
$refs = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xaml.dll',"$wpf\WindowsBase.dll","$wpf\PresentationCore.dll","$wpf\PresentationFramework.dll")
$compilerArgs = @('/nologo','/optimize+','/platform:x64','/target:winexe',"/out:$out\MLMDesktop.exe","/win32manifest:$root\src\app.manifest")
if(Test-Path "$root\assets\app.ico"){$compilerArgs += "/win32icon:$root\assets\app.ico"}
foreach($r in $refs){$compilerArgs += "/reference:$r"}
$compilerArgs += (Get-ChildItem "$root\src\*.cs").FullName
& $csc @compilerArgs
if($LASTEXITCODE -ne 0){throw 'App compilation failed'}
Copy-Item "$root\src\MLMDesktop.exe.config" $out -Force
Copy-Item "$root\assets" $out -Recurse -Force
foreach($suite in @('Cloth','DesktopMode')){
 $sources = @("$root\src\$suite.cs","$root\tests\$($suite)Tests.cs")
 if($suite -eq 'Cloth'){$sources += "$root\src\ClothContacts.cs"}
 & $csc /nologo "/out:$root\tests\$($suite)Tests.exe" @sources
 if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
 & "$root\tests\$($suite)Tests.exe"
 if($LASTEXITCODE -ne 0){throw "$suite tests failed"}
}
if(!$SkipInstaller){& "$root\installer\build-installer.ps1"}
Write-Host "Built: $out\MLMDesktop.exe"
