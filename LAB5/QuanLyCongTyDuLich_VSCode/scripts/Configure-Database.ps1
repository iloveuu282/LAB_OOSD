param([string]$Server, [string]$Database = 'QuanLyCongTyDuLich')
. "$PSScriptRoot\Common.ps1"
if ($Database -notmatch '^[A-Za-z][A-Za-z0-9_]{0,80}$') { throw 'Tên database chỉ gồm chữ, số và dấu gạch dưới.' }
if (!$Server) {
    $candidates = New-Object 'System.Collections.Generic.List[string]'
    foreach ($key in @('HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL','HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL')) {
        if (Test-Path $key) {
            (Get-ItemProperty $key).PSObject.Properties | Where-Object { $_.Name -notlike 'PS*' } | ForEach-Object {
                $name = if ($_.Name -eq 'MSSQLSERVER') { '.' } else { '.\' + $_.Name }
                if (!$candidates.Contains($name)) { $candidates.Add($name) }
            }
        }
    }
    foreach ($s in @('.\SQLEXPRESS', '.', '(localdb)\MSSQLLocalDB')) { if (!$candidates.Contains($s)) { $candidates.Add($s) } }
    $available = @(); $existing = @()
    foreach ($s in $candidates) {
        $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
        $b['Data Source'] = $s; $b['Initial Catalog'] = 'master'; $b['Integrated Security'] = $true; $b['Connect Timeout'] = 3
        try {
            $cn = New-Connection $b.ConnectionString
            try {
                $available += $s
                $cmd = $cn.CreateCommand(); $cmd.CommandText = 'SELECT DB_ID(@n)'; [void]$cmd.Parameters.AddWithValue('@n',$Database)
                $id = $cmd.ExecuteScalar(); if ($id -ne [DBNull]::Value) { $existing += $s }; $cmd.Dispose()
            } finally { $cn.Dispose() }
        } catch { Write-Host ('Không kết nối được ' + $s) }
    }
    if ($existing.Count -eq 1) { $Server = $existing[0] }
    elseif ($existing.Count -gt 1) { throw ('Database tồn tại ở nhiều instance: ' + ($existing -join ', ') + '. Chạy lại với -Server tên-instance đúng.') }
    elseif ($available.Count -eq 1) { $Server = $available[0] }
    elseif ($available.Count -gt 1) { throw ('Có nhiều instance chạy được: ' + ($available -join ', ') + '. Chọn instance đang dùng trong SSMS bằng -Server.') }
    else { throw 'Không kết nối được SQL Server. Khởi động SQL Server service, hoặc cài LocalDB, rồi chỉ rõ -Server như tên trong SSMS.' }
}
$b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$b['Data Source'] = $Server; $b['Initial Catalog'] = 'master'; $b['Integrated Security'] = $true; $b['Connect Timeout'] = 5
$cn = New-Connection $b.ConnectionString; $cn.Dispose()
foreach ($relative in @('src\QuanLyCongTyDuLich\App.config','tests\QuanLyCongTyDuLich.Tests\App.config')) {
    $file = Join-Path $ProjectRoot $relative
    [xml]$doc = Get-Content $file -Raw
    $item = @($doc.configuration.connectionStrings.add) | Where-Object { $_.name -eq 'QuanLyCongTyDuLichDB' }
    $b['Initial Catalog'] = if ($relative -like 'tests*') { $Database + '_Test' } else { $Database }
    $b['Connect Timeout'] = 15
    $item.SetAttribute('connectionString', $b.ConnectionString)
    $doc.Save($file)
}
Write-Host ('Đã cấu hình Server=' + $Server + '; Database=' + $Database + '; Test=' + $Database + '_Test')
Write-Host 'Tiếp theo: Initialize-Database.ps1 rồi Build.ps1. Không cần sửa App.config bằng tay.'
