param([switch]$NoBuild)
. "$PSScriptRoot\Common.ps1"
if (!$NoBuild) { & "$PSScriptRoot\Build.ps1" }
$exe = Join-Path $ProjectRoot 'src\QuanLyCongTyDuLich\bin\Debug\QuanLyCongTyDuLich.exe'
if (!(Test-Path $exe)) { throw 'Chưa có exe. Chạy Build.ps1 trước.' }
Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
