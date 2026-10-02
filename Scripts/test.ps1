param([ValidateSet('Debug','Release')][string]$Configuration='Debug')
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -Configuration $Configuration
$root=Split-Path $PSScriptRoot -Parent
$exe=Join-Path $root "WinForms\bin\$Configuration\EShopping.exe"
$run=Start-Process -FilePath $exe -ArgumentList '--self-test' -Wait -PassThru
$result=Join-Path (Split-Path $exe) 'self-test-results.txt'
Get-Content $result
if($run.ExitCode -ne 0){throw 'Có test thất bại; đọc self-test-results.txt.'}
