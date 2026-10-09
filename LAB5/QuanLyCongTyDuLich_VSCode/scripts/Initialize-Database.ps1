param([switch]$Reset)
. "$PSScriptRoot\Common.ps1"
$cs = Read-AppConnection
$b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $cs
$database = $b['Initial Catalog']
if ($database -notmatch '^[A-Za-z][A-Za-z0-9_]{0,80}$') { throw 'Tên database không hợp lệ.' }
$master = New-Connection (Set-Database $cs 'master')
try {
    $cmd = $master.CreateCommand(); $cmd.CommandText = 'SELECT DB_ID(@n)'; [void]$cmd.Parameters.AddWithValue('@n',$database)
    $id = $cmd.ExecuteScalar(); $cmd.Dispose()
    if ($id -eq [DBNull]::Value) {
        $cmd = $master.CreateCommand(); $cmd.CommandText = 'CREATE DATABASE [' + $database + ']'; [void]$cmd.ExecuteNonQuery(); $cmd.Dispose()
    }
} finally { $master.Dispose() }
$cn = New-Connection $cs
try {
    $cmd = $cn.CreateCommand(); $cmd.CommandText = 'SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0'; $count = [int]$cmd.ExecuteScalar(); $cmd.Dispose()
    if ($count -gt 0 -and !$Reset) {
        Write-Host ('Database đã có ' + $count + ' bảng. Giữ dữ liệu hiện có. Chỉ dùng -Reset khi muốn xóa và nạp lại dữ liệu mẫu.'); return
    }
    if ($Reset) { Write-Warning ('Đang khôi phục dữ liệu mẫu; các bảng thực hành trong ' + $database + ' sẽ bị tạo lại.') }
    $sql = Get-Content (Join-Path $ProjectRoot 'Database\QuanLyCongTyDuLich.sql') -Raw -Encoding UTF8
    $sql = $sql.Replace('QuanLyCongTyDuLich',$database)
    foreach ($batch in [regex]::Split($sql,'(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$')) {
        if ([string]::IsNullOrWhiteSpace($batch)) { continue }
        $cmd = $cn.CreateCommand(); $cmd.CommandText = $batch; $cmd.CommandTimeout = 60
        try { [void]$cmd.ExecuteNonQuery() } finally { $cmd.Dispose() }
    }
    $cmd = $cn.CreateCommand(); $cmd.CommandText = 'SELECT DB_NAME() AS DatabaseName, (SELECT COUNT(*) FROM dbo.Tour) AS SoTour, (SELECT COUNT(*) FROM dbo.ChuyenLe) AS SoChuyen'
    $dt = New-Object System.Data.DataTable; $da = New-Object System.Data.SqlClient.SqlDataAdapter $cmd
    try { [void]$da.Fill($dt); $dt | Format-Table -AutoSize } finally { $da.Dispose(); $cmd.Dispose() }
} finally { $cn.Dispose() }
