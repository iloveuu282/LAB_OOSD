param([string]$Case)
. "$PSScriptRoot\Common.ps1"
& "$PSScriptRoot\Build.ps1"
$exe = Join-Path $ProjectRoot 'tests\QuanLyCongTyDuLich.Tests\bin\Debug\QuanLyCongTyDuLich.Tests.exe'
$out = Join-Path $ProjectRoot 'TestResults'
if ($Case) { & $exe '--output' $out '--case' $Case } else { & $exe '--output' $out }
if ($LASTEXITCODE -ne 0) { throw 'Có test không đạt hoặc lỗi thiết lập. Xem CSV và log trong TestResults.' }
