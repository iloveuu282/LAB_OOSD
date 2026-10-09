param([string]$Server)
. "$PSScriptRoot\Common.ps1"
Write-Host ('Windows: ' + [Environment]::OSVersion.VersionString)
try { Write-Host ('MSBuild: ' + (Find-MSBuild)) } catch { Write-Warning $_.Exception.Message }
$pack = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
if (Test-Path $pack) { Write-Host '.NET Framework 4.7.2 targeting pack: có' }
else { Write-Warning 'Thiếu targeting pack 4.7.2. Cài Developer Pack 4.7.2 hoặc Individual components trong Visual Studio Installer.' }
foreach ($key in @('HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL','HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL')) {
    if (Test-Path $key) {
        $reg = Get-ItemProperty $key
        $reg.PSObject.Properties | Where-Object { $_.Name -notlike 'PS*' } | ForEach-Object {
            Write-Host ('SQL instance: ' + $(if ($_.Name -eq 'MSSQLSERVER') { '.' } else { '.\' + $_.Name }))
        }
    }
}
$ldb = Get-Command sqllocaldb.exe -ErrorAction SilentlyContinue
if ($ldb) { & $ldb.Source info }
if ($Server) {
    $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $b['Data Source'] = $Server; $b['Initial Catalog'] = 'master'; $b['Integrated Security'] = $true; $b['Connect Timeout'] = 5
    $cn = New-Connection $b.ConnectionString
    try { Write-Host ('Kết nối được: ' + $cn.DataSource) } finally { $cn.Dispose() }
}
