-- Chay mot lan tren SQL Server LocalDB/Express. Khong xoa du lieu khi chay lai.
IF DB_ID(N'QuanLyKhachSanLab3') IS NULL CREATE DATABASE QuanLyKhachSanLab3;
GO
USE QuanLyKhachSanLab3;
GO
CREATE TABLE KhuVuc(MaKhu varchar(20) PRIMARY KEY, Ten nvarchar(100) NOT NULL UNIQUE);
CREATE TABLE NhanVien(MaNV varchar(20) PRIMARY KEY, HoTen nvarchar(100) NOT NULL, VaiTro nvarchar(30) NOT NULL);
CREATE TABLE Phong(SoPhong varchar(20) PRIMARY KEY, MaKhu varchar(20) NOT NULL REFERENCES KhuVuc(MaKhu), SucChua int NOT NULL CHECK(SucChua>0), DonGiaNgay decimal(18,2) NOT NULL CHECK(DonGiaNgay>=0), BaoTri bit NOT NULL DEFAULT 0);
CREATE TABLE LoaiTienNghi(MaLoai varchar(20) PRIMARY KEY, Ten nvarchar(100) NOT NULL UNIQUE);
CREATE TABLE TienNghi(MaTN varchar(30) PRIMARY KEY, MaLoai varchar(20) NOT NULL REFERENCES LoaiTienNghi(MaLoai), SoThuTu int NOT NULL CHECK(SoThuTu>0), TinhTrang nvarchar(100) NULL, CONSTRAINT UQ_TN_Loai_STT UNIQUE(MaLoai,SoThuTu));
CREATE TABLE LapDat(MaLapDat varchar(30) PRIMARY KEY, MaTN varchar(30) NOT NULL REFERENCES TienNghi(MaTN), SoPhong varchar(20) NOT NULL REFERENCES Phong(SoPhong), Ngay date NOT NULL, MaNV varchar(20) NOT NULL REFERENCES NhanVien(MaNV), GhiChu nvarchar(200), CONSTRAINT UQ_LapDat_TN_Ngay UNIQUE(MaTN,Ngay));
CREATE TABLE KhachHang(MaKhach varchar(20) PRIMARY KEY, HoTen nvarchar(100) NOT NULL, CCCD varchar(30) NOT NULL UNIQUE, QuocTich nvarchar(80) NOT NULL, DienThoai varchar(20));
CREATE TABLE DatPhong(MaDat varchar(30) PRIMARY KEY, MaKhach varchar(20) NOT NULL REFERENCES KhachHang(MaKhach), MaNV varchar(20) NOT NULL REFERENCES NhanVien(MaNV), Kenh nvarchar(20) NOT NULL CHECK(Kenh IN (N'Điện thoại',N'Website',N'Trực tiếp')), NgayNhan date NOT NULL, NgayTra date NOT NULL, TienCoc decimal(18,2) NOT NULL DEFAULT 0 CHECK(TienCoc>=0), TrangThai nvarchar(20) NOT NULL DEFAULT N'Đã đặt' CHECK(TrangThai IN(N'Đã đặt',N'Đang ở',N'Đã trả',N'Hủy',N'No-show')), NgayNhanThucTe datetime2, NgayTraThucTe datetime2, CONSTRAINT CK_Dat_Ngay CHECK(NgayTra>NgayNhan));
CREATE TABLE DatPhongCT(MaDat varchar(30) NOT NULL REFERENCES DatPhong(MaDat), SoPhong varchar(20) NOT NULL REFERENCES Phong(SoPhong), SoNguoi int NOT NULL CHECK(SoNguoi>0), DonGiaNgay decimal(18,2) NOT NULL CHECK(DonGiaNgay>=0), CONSTRAINT PK_DatPhongCT PRIMARY KEY(MaDat,SoPhong));
CREATE TABLE NguoiLuuTru(MaNguoi int IDENTITY PRIMARY KEY, MaDat varchar(30) NOT NULL, SoPhong varchar(20) NOT NULL, HoTen nvarchar(100) NOT NULL, CCCD varchar(30) NOT NULL, QuocTich nvarchar(80) NOT NULL, CONSTRAINT FK_NLT_CT FOREIGN KEY(MaDat,SoPhong) REFERENCES DatPhongCT(MaDat,SoPhong), CONSTRAINT UQ_NLT UNIQUE(MaDat,CCCD));
CREATE TABLE DichVu(MaDV varchar(20) PRIMARY KEY, Ten nvarchar(100) NOT NULL, DonVi nvarchar(30) NOT NULL, DonGia decimal(18,2) NOT NULL CHECK(DonGia>=0));
CREATE TABLE SuDungDV(MaDat varchar(30) NOT NULL, SoPhong varchar(20) NOT NULL, Ngay date NOT NULL, MaDV varchar(20) NOT NULL REFERENCES DichVu(MaDV), SoLuong int NOT NULL CHECK(SoLuong>0), DonGia decimal(18,2) NOT NULL CHECK(DonGia>=0), CONSTRAINT PK_SuDung PRIMARY KEY(MaDat,SoPhong,Ngay,MaDV), CONSTRAINT FK_SuDung_CT FOREIGN KEY(MaDat,SoPhong) REFERENCES DatPhongCT(MaDat,SoPhong));
CREATE TABLE QuyDinhDenBu(MaLoai varchar(20) NOT NULL REFERENCES LoaiTienNghi(MaLoai), MucDo nvarchar(40) NOT NULL, MucTien decimal(18,2) NOT NULL CHECK(MucTien>=0), PRIMARY KEY(MaLoai,MucDo));
CREATE TABLE DenBu(MaDenBu varchar(30) PRIMARY KEY, MaDat varchar(30) NOT NULL, SoPhong varchar(20) NOT NULL, MaNV varchar(20) NOT NULL REFERENCES NhanVien(MaNV), Ngay datetime2 NOT NULL DEFAULT SYSDATETIME(), CONSTRAINT FK_DenBu_CT FOREIGN KEY(MaDat,SoPhong) REFERENCES DatPhongCT(MaDat,SoPhong));
CREATE TABLE DenBuCT(MaDenBu varchar(30) NOT NULL REFERENCES DenBu(MaDenBu), MaTN varchar(30) NOT NULL REFERENCES TienNghi(MaTN), MucDo nvarchar(40) NOT NULL, SoTien decimal(18,2) NOT NULL CHECK(SoTien>=0), PRIMARY KEY(MaDenBu,MaTN));
CREATE TABLE HoaDon(MaHD varchar(30) PRIMARY KEY, MaDat varchar(30) NOT NULL UNIQUE REFERENCES DatPhong(MaDat), MaNV varchar(20) NOT NULL REFERENCES NhanVien(MaNV), Ngay datetime2 NOT NULL DEFAULT SYSDATETIME(), SoNgay int NOT NULL CHECK(SoNgay>0), TienPhong decimal(18,2) NOT NULL CHECK(TienPhong>=0), TienDV decimal(18,2) NOT NULL CHECK(TienDV>=0), TienDenBu decimal(18,2) NOT NULL CHECK(TienDenBu>=0), TienCoc decimal(18,2) NOT NULL CHECK(TienCoc>=0), TongPhaiTra AS CONVERT(decimal(18,2),CASE WHEN TienPhong+TienDV+TienDenBu>TienCoc THEN TienPhong+TienDV+TienDenBu-TienCoc ELSE 0 END) PERSISTED, TienCocHoan AS CONVERT(decimal(18,2),CASE WHEN TienCoc>TienPhong+TienDV+TienDenBu THEN TienCoc-(TienPhong+TienDV+TienDenBu) ELSE 0 END) PERSISTED);
CREATE TABLE ThanhToan(MaTT varchar(30) PRIMARY KEY, MaHD varchar(30) NOT NULL REFERENCES HoaDon(MaHD), Ngay datetime2 NOT NULL DEFAULT SYSDATETIME(), HinhThuc nvarchar(20) NOT NULL CHECK(HinhThuc IN(N'Tiền mặt',N'Chuyển khoản',N'Thẻ',N'Ví điện tử')), SoTien decimal(18,2) NOT NULL CHECK(SoTien>0));
GO
CREATE INDEX IX_DatPhong_ThoiGian ON DatPhong(NgayNhan,NgayTra,TrangThai);
CREATE INDEX IX_CT_Phong ON DatPhongCT(SoPhong,MaDat);
CREATE INDEX IX_LapDat_TN_Ngay ON LapDat(MaTN,Ngay DESC);
GO
CREATE VIEW vHoaDon AS SELECT h.*, COALESCE(t.DaTra,0) AS DaTra, h.TongPhaiTra-COALESCE(t.DaTra,0) AS ConLai, CASE WHEN h.TongPhaiTra=COALESCE(t.DaTra,0) THEN N'Đã thanh toán' ELSE N'Chưa thanh toán' END AS TrangThai FROM HoaDon h OUTER APPLY(SELECT SUM(SoTien) DaTra FROM ThanhToan WHERE MaHD=h.MaHD)t;
GO
