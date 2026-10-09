param([ValidateSet('Debug','Release')][string]$Configuration='Debug')
. "$PSScriptRoot\Common.ps1"
$msbuild = Find-MSBuild
$pack = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
if (!(Test-Path $pack)) { throw 'Thiếu .NET Framework 4.7.2 targeting pack. Cài Developer Pack 4.7.2; runtime 4.8/4.8.1 không thay thế targeting pack.' }
& $msbuild (Join-Path $ProjectRoot 'QuanLyCongTyDuLich.sln') /t:Rebuild /m /nologo "/p:Configuration=$Configuration" /p:Platform=x64 /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw ('Build thất bại, exit code ' + $LASTEXITCODE) }
