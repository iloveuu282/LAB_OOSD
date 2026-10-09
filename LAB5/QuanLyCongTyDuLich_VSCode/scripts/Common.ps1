Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path $PSScriptRoot -Parent
function Find-MSBuild {
    $cmd = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $paths = @(& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe')
        if ($paths.Count -gt 0) { return $paths[0] }
    }
    throw 'Chưa có MSBuild. Mở Visual Studio Installer, cài .NET desktop development hoặc Build Tools (Desktop development with .NET).'
}
function Read-AppConnection {
    [xml]$doc = Get-Content (Join-Path $ProjectRoot 'src\QuanLyCongTyDuLich\App.config') -Raw
    $item = @($doc.configuration.connectionStrings.add) | Where-Object { $_.name -eq 'QuanLyCongTyDuLichDB' }
    if (!$item) { throw 'Thiếu connection string QuanLyCongTyDuLichDB.' }
    return [string]$item.connectionString
}
function New-Connection([string]$ConnectionString) {
    $cn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try { $cn.Open(); return $cn } catch { $cn.Dispose(); throw }
}
function Set-Database([string]$ConnectionString, [string]$Database) {
    $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $ConnectionString
    $b['Initial Catalog'] = $Database
    return $b.ConnectionString
}
