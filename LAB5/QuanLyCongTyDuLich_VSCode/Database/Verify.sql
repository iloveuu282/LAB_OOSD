USE QuanLyCongTyDuLich;
GO
SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName;
SELECT COUNT(*) AS SoBang FROM sys.tables WHERE is_ms_shipped=0;
SELECT MaTour, TenTour, SoNgay, DonGiaKhach FROM dbo.Tour;
SELECT * FROM dbo.ChuyenLe WHERE MaChuyen='CL004';
SELECT SoDKDoan, TrangThai, TienCoc FROM dbo.DangKyDoan WHERE SoDKDoan='DD002';
SELECT COUNT(*) AS SoPhanCongConLai FROM dbo.PhanCongHDV WHERE SoDKDoan='DD002';
