using System;
namespace QuanLyKhachSan.Forms {
 public class EquipmentForm:BaseForm {
  public EquipmentForm():base("Tiện nghi và phiếu lắp đặt","SELECT l.MaLapDat,l.MaTN,l.SoPhong,l.Ngay,l.MaNV FROM LapDat l ORDER BY l.Ngay DESC") {
   Field("id","Số phiếu", "LD01"); Field("device","Mã thiết bị", "TV01"); Field("room","Số phòng", "A101"); Field("day","Ngày lắp đặt",DateTime.Today.ToString("yyyy-MM-dd")); Field("staff","Nhân viên", "NV02");
   Button("Lập phiếu lắp đặt",()=>Exec("sp_LapDat",P("@MaLapDat",V("id")),P("@MaTN",V("device")),P("@SoPhong",V("room")),P("@Ngay",Day("day")),P("@MaNV",V("staff"))));
  }
 }
}
