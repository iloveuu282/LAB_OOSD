param([string]$Server='(localdb)\MSSQLLocalDB')
$ErrorActionPreference='Stop'
if(-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)){throw 'Không có sqlcmd. Chạy lần lượt 01–04 trong SSMS hoặc extension MSSQL của VS Code.'}
$root=Split-Path $PSScriptRoot -Parent
foreach($file in @('01_shop_schema.sql','02_shop_procedures.sql','03_shop_seed.sql','04_product_mock.sql')){
 & sqlcmd -S $Server -E -C -b -f 65001 -i (Join-Path $root "Database\$file")
 if($LASTEXITCODE -ne 0){throw "Lỗi chạy $file"}
}
Write-Host 'Đã tạo hai database. Nếu server khác mặc định, sửa cả hai connection string trong WinForms/App.config.'
