param([ValidateSet('Debug','Release')][string]$Configuration='Debug')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild=$null
if(Test-Path $vswhere){$msbuild=& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1}
if(-not $msbuild){$cmd=Get-Command MSBuild.exe -ErrorAction SilentlyContinue;if($cmd){$msbuild=$cmd.Source}}
if(-not $msbuild){throw 'Cài Visual Studio với .NET desktop development và Developer Pack 4.7.2; hoặc chạy từ Developer PowerShell.'}
& $msbuild (Join-Path $root 'EShopping.sln') '/t:Build' "/p:Configuration=$Configuration" '/p:Platform=Any CPU' '/nologo'
if($LASTEXITCODE -ne 0){throw 'Build thất bại. Kiểm tra Targeting Pack .NET Framework 4.7.2.'}
Write-Host "Build thành công: $root\WinForms\bin\$Configuration\EShopping.exe"
