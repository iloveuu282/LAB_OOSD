using System;
namespace QuanLyKhachSan.Forms {
 public class BookingForm:BaseForm {
  public BookingForm():base("Phiếu đặt và nhận phòng","SELECT d.MaDat,d.MaKhach,c.SoPhong,c.SoNguoi,d.NgayNhan,d.NgayTra,d.TienCoc,d.TrangThai FROM DatPhong d JOIN DatPhongCT c ON c.MaDat=d.MaDat ORDER BY d.NgayNhan DESC") {
   Field("id","Mã đặt phòng","DP01"); Field("customer","Mã khách","K01"); Field("staff","Lễ tân","NV01"); Field("channel","Kênh đặt","Trực tiếp"); Field("in","Ngày nhận",DateTime.Today.ToString("yyyy-MM-dd")); Field("out","Ngày trả",DateTime.Today.AddDays(1).ToString("yyyy-MM-dd")); Field("deposit","Tiền cọc","0"); Field("room","Số phòng","A101"); Field("count","Số người","2");
   Button("Đặt phòng",()=>Exec("sp_DatPhong",P("@MaDat",V("id")),P("@MaKhach",V("customer")),P("@MaNV",V("staff")),P("@Kenh",V("channel")),P("@NgayNhan",Day("in")),P("@NgayTra",Day("out")),P("@TienCoc",D("deposit")),P("@SoPhong",V("room")),P("@SoNguoi",I("count"))));
   Field("person","Họ tên người ở","Người ở mẫu"); Field("cccd","CCCD người ở","TEST-CCCD-002"); Field("nation","Quốc tịch","Việt Nam");
   Button("Thêm người ở",()=>Exec("sp_ThemNguoi",P("@MaDat",V("id")),P("@SoPhong",V("room")),P("@HoTen",V("person")),P("@CCCD",V("cccd")),P("@QuocTich",V("nation"))));
   Button("Nhận phòng",()=>Exec("sp_NhanPhong",P("@MaDat",V("id"))));
   Button("Xem người lưu trú",()=>Grid.DataSource=Data.Db.Query("SELECT * FROM NguoiLuuTru"),false);
   Button("In phiếu đặt",()=>PrintForm.Show(this,"PHIẾU ĐẶT PHÒNG",Data.Db.Query("SELECT d.MaDat,k.HoTen AS Khach,c.SoPhong,c.SoNguoi,d.NgayNhan,d.NgayTra,d.TienCoc,d.TrangThai FROM DatPhong d JOIN DatPhongCT c ON c.MaDat=d.MaDat JOIN KhachHang k ON k.MaKhach=d.MaKhach WHERE d.MaDat=@id",P("@id",V("id")))),false);
  }
 }
}
