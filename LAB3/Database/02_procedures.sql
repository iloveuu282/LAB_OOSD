USE QuanLyKhachSanLab3;
GO
CREATE OR ALTER PROCEDURE sp_DatPhong @MaDat varchar(30),@MaKhach varchar(20),@MaNV varchar(20),@Kenh nvarchar(20),@NgayNhan date,@NgayTra date,@TienCoc decimal(18,2),@SoPhong varchar(20),@SoNguoi int AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
 BEGIN TRY
  BEGIN TRAN;
  IF @NgayTra<=@NgayNhan OR @SoNguoi<=0 THROW 50001,N'Ngày hoặc số người không hợp lệ',1;
  IF NOT EXISTS(SELECT 1 FROM Phong WITH(UPDLOCK,HOLDLOCK) WHERE SoPhong=@SoPhong AND BaoTri=0 AND SucChua>=@SoNguoi) THROW 50002,N'Phòng bảo trì hoặc vượt sức chứa',1;
  IF EXISTS(SELECT 1 FROM DatPhongCT c JOIN DatPhong d ON d.MaDat=c.MaDat WHERE c.SoPhong=@SoPhong AND d.TrangThai IN(N'Đã đặt',N'Đang ở') AND @NgayNhan<d.NgayTra AND d.NgayNhan<@NgayTra) THROW 50003,N'Phòng trùng lịch',1;
  INSERT DatPhong(MaDat,MaKhach,MaNV,Kenh,NgayNhan,NgayTra,TienCoc) VALUES(@MaDat,@MaKhach,@MaNV,@Kenh,@NgayNhan,@NgayTra,@TienCoc);
  INSERT DatPhongCT(MaDat,SoPhong,SoNguoi,DonGiaNgay) SELECT @MaDat,SoPhong,@SoNguoi,DonGiaNgay FROM Phong WHERE SoPhong=@SoPhong;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE sp_ThemNguoi @MaDat varchar(30),@SoPhong varchar(20),@HoTen nvarchar(100),@CCCD varchar(30),@QuocTich nvarchar(80) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
 BEGIN TRY BEGIN TRAN;
  DECLARE @max int=(SELECT SoNguoi FROM DatPhongCT WITH(UPDLOCK,HOLDLOCK) WHERE MaDat=@MaDat AND SoPhong=@SoPhong);
  IF @max IS NULL OR NOT EXISTS(SELECT 1 FROM DatPhong WHERE MaDat=@MaDat AND TrangThai IN(N'Đã đặt',N'Đang ở')) THROW 50004,N'Phiếu không hợp lệ',1;
  IF (SELECT COUNT(*) FROM NguoiLuuTru WHERE MaDat=@MaDat AND SoPhong=@SoPhong)>=@max THROW 50005,N'Đã đủ số người đăng ký',1;
  INSERT NguoiLuuTru(MaDat,SoPhong,HoTen,CCCD,QuocTich) VALUES(@MaDat,@SoPhong,@HoTen,@CCCD,@QuocTich);
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE sp_NhanPhong @MaDat varchar(30) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRAN;
 BEGIN TRY
  IF EXISTS(SELECT 1 FROM DatPhong d WITH(UPDLOCK,HOLDLOCK) WHERE MaDat=@MaDat AND TrangThai=N'Đã đặt')
    AND NOT EXISTS(SELECT 1 FROM DatPhongCT c WHERE c.MaDat=@MaDat AND NOT EXISTS(SELECT 1 FROM NguoiLuuTru n WHERE n.MaDat=c.MaDat AND n.SoPhong=c.SoPhong))
    UPDATE DatPhong SET TrangThai=N'Đang ở',NgayNhanThucTe=SYSDATETIME() WHERE MaDat=@MaDat;
  ELSE THROW 50006,N'Phiếu chưa sẵn sàng hoặc thiếu người lưu trú',1;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE sp_LapDat @MaLapDat varchar(30),@MaTN varchar(30),@SoPhong varchar(20),@Ngay date,@MaNV varchar(20) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
 BEGIN TRY BEGIN TRAN;
  -- Lịch sử theo ngày; một thiết bị chỉ ở một phòng mỗi ngày, kể cả ngày không có phiếu mới.
  IF EXISTS(SELECT 1 FROM LapDat WITH(UPDLOCK,HOLDLOCK) WHERE MaTN=@MaTN AND Ngay=@Ngay) THROW 50007,N'Thiết bị đã có phiếu trong ngày',1;
  INSERT LapDat(MaLapDat,MaTN,SoPhong,Ngay,MaNV) VALUES(@MaLapDat,@MaTN,@SoPhong,@Ngay,@MaNV);
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE sp_GhiDichVu @MaDat varchar(30),@SoPhong varchar(20),@Ngay date,@MaDV varchar(20),@SoLuong int AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
 BEGIN TRY BEGIN TRAN;
  IF @SoLuong<=0 THROW 50008,N'Số lượng phải lớn hơn 0',1;
  IF NOT EXISTS(SELECT 1 FROM DatPhong d WITH(UPDLOCK,HOLDLOCK) JOIN DatPhongCT c ON c.MaDat=d.MaDat WHERE d.MaDat=@MaDat AND c.SoPhong=@SoPhong AND d.TrangThai=N'Đang ở') THROW 50009,N'Phòng chưa nhận hoặc đã trả',1;
  IF EXISTS(SELECT 1 FROM HoaDon WHERE MaDat=@MaDat) THROW 50010,N'Đã lập hóa đơn; không thể thêm dịch vụ',1;
  IF EXISTS(SELECT 1 FROM SuDungDV WITH(UPDLOCK,HOLDLOCK) WHERE MaDat=@MaDat AND SoPhong=@SoPhong AND Ngay=@Ngay AND MaDV=@MaDV)
   UPDATE SuDungDV SET SoLuong=SoLuong+@SoLuong WHERE MaDat=@MaDat AND SoPhong=@SoPhong AND Ngay=@Ngay AND MaDV=@MaDV;
  ELSE INSERT SuDungDV(MaDat,SoPhong,Ngay,MaDV,SoLuong,DonGia) SELECT @MaDat,@SoPhong,@Ngay,@MaDV,@SoLuong,DonGia FROM DichVu WHERE MaDV=@MaDV;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE sp_DenBu @MaDenBu varchar(30),@MaDat varchar(30),@SoPhong varchar(20),@MaNV varchar(20),@MaTN varchar(30),@MucDo nvarchar(40) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
 BEGIN TRY BEGIN TRAN;
  IF NOT EXISTS(SELECT 1 FROM DatPhong d WITH(UPDLOCK,HOLDLOCK) JOIN DatPhongCT c ON c.MaDat=d.MaDat WHERE d.MaDat=@MaDat AND c.SoPhong=@SoPhong AND d.TrangThai=N'Đang ở') OR EXISTS(SELECT 1 FROM HoaDon WHERE MaDat=@MaDat) THROW 50011,N'Phiếu không ở hoặc đã chốt hóa đơn',1;
  DECLARE @m decimal(18,2)=(SELECT q.MucTien FROM TienNghi t JOIN QuyDinhDenBu q ON q.MaLoai=t.MaLoai WHERE t.MaTN=@MaTN AND q.MucDo=@MucDo);
  IF @m IS NULL THROW 50012,N'Chưa có quy định đền bù',1;
  IF NOT EXISTS(SELECT 1 FROM LapDat l WHERE l.MaTN=@MaTN AND l.SoPhong=@SoPhong AND l.Ngay <= CAST(SYSDATETIME() AS date) AND NOT EXISTS(SELECT 1 FROM LapDat later WHERE later.MaTN=l.MaTN AND later.Ngay>l.Ngay AND later.Ngay<=CAST(SYSDATETIME() AS date))) THROW 50013,N'Thiết bị không thuộc phòng hiện tại',1;
  INSERT DenBu(MaDenBu,MaDat,SoPhong,MaNV) VALUES(@MaDenBu,@MaDat,@SoPhong,@MaNV);
  INSERT DenBuCT(MaDenBu,MaTN,MucDo,SoTien) VALUES(@MaDenBu,@MaTN,@MucDo,@m);
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE sp_LapHoaDon @MaHD varchar(30),@MaDat varchar(30),@MaNV varchar(20),@SoNgay int AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
 BEGIN TRY BEGIN TRAN;
  IF @SoNgay<=0 THROW 50014,N'Số ngày phải lớn hơn 0',1;
  IF NOT EXISTS(SELECT 1 FROM DatPhong WITH(UPDLOCK,HOLDLOCK) WHERE MaDat=@MaDat AND TrangThai=N'Đang ở') THROW 50015,N'Phiếu chưa nhận hoặc đã trả',1;
  INSERT HoaDon(MaHD,MaDat,MaNV,SoNgay,TienPhong,TienDV,TienDenBu,TienCoc)
  SELECT @MaHD,@MaDat,@MaNV,@SoNgay,@SoNgay*(SELECT COALESCE(SUM(DonGiaNgay),0) FROM DatPhongCT WHERE MaDat=@MaDat),
   (SELECT COALESCE(SUM(SoLuong*DonGia),0) FROM SuDungDV WHERE MaDat=@MaDat),
   (SELECT COALESCE(SUM(ct.SoTien),0) FROM DenBuCT ct JOIN DenBu b ON b.MaDenBu=ct.MaDenBu WHERE b.MaDat=@MaDat),TienCoc FROM DatPhong WHERE MaDat=@MaDat;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE sp_ThanhToan @MaTT varchar(30),@MaHD varchar(30),@HinhThuc nvarchar(20),@SoTien decimal(18,2) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
 BEGIN TRY BEGIN TRAN;
  DECLARE @tong decimal(18,2)=(SELECT TongPhaiTra FROM HoaDon WITH(UPDLOCK,HOLDLOCK) WHERE MaHD=@MaHD);
  IF @tong IS NULL OR @SoTien<=0 OR @SoTien>(@tong-COALESCE((SELECT SUM(SoTien) FROM ThanhToan WHERE MaHD=@MaHD),0)) THROW 50016,N'Số tiền vượt dư nợ hoặc hóa đơn không tồn tại',1;
  INSERT ThanhToan(MaTT,MaHD,HinhThuc,SoTien) VALUES(@MaTT,@MaHD,@HinhThuc,@SoTien);
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE sp_TraPhong @MaHD varchar(30) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
 BEGIN TRY BEGIN TRAN;
  DECLARE @ma varchar(30)=(SELECT MaDat FROM HoaDon WITH(UPDLOCK,HOLDLOCK) WHERE MaHD=@MaHD);
  IF @ma IS NULL OR NOT EXISTS(SELECT 1 FROM DatPhong WHERE MaDat=@ma AND TrangThai=N'Đang ở') OR EXISTS(SELECT 1 FROM vHoaDon WHERE MaHD=@MaHD AND ConLai<>0) THROW 50017,N'Hóa đơn chưa đủ tiền hoặc phiếu không đang ở',1;
  UPDATE DatPhong SET TrangThai=N'Đã trả',NgayTraThucTe=SYSDATETIME() WHERE MaDat=@ma;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
