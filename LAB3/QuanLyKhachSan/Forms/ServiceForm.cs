using System;
namespace QuanLyKhachSan.Forms {
 public class ServiceForm:BaseForm {
  public ServiceForm():base("Phiếu sử dụng dịch vụ","SELECT s.MaDat,s.SoPhong,s.Ngay,s.MaDV,d.Ten,s.SoLuong,s.DonGia,s.SoLuong*s.DonGia AS ThanhTien FROM SuDungDV s JOIN DichVu d ON d.MaDV=s.MaDV ORDER BY s.Ngay DESC") {
   Field("id","Mã đặt phòng","DP01");Field("room","Số phòng","A101");Field("day","Ngày dùng",DateTime.Today.ToString("yyyy-MM-dd"));Field("service","Mã dịch vụ","DV01");Field("quantity","Số lượng","1");
   Button("Ghi dịch vụ",()=>Exec("sp_GhiDichVu",P("@MaDat",V("id")),P("@SoPhong",V("room")),P("@Ngay",Day("day")),P("@MaDV",V("service")),P("@SoLuong",I("quantity"))));
  }
 }
}
