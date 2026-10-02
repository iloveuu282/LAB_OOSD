using System;
using QuanLyKhachSan.Data;
namespace QuanLyKhachSan.Forms {
 public class StatsForm:BaseForm {
  public StatsForm():base("Thống kê hóa đơn","SELECT MaHD,MaDat,Ngay,TongPhaiTra,DaTra,ConLai FROM vHoaDon ORDER BY Ngay DESC") {
   Field("from","Từ ngày",DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"));Field("to","Đến ngày",DateTime.Today.ToString("yyyy-MM-dd"));
   Button("Thống kê",()=> {if(Day("to")<Day("from")) throw new Exception("Ngày kết thúc trước ngày bắt đầu"); Grid.DataSource=Db.Query("SELECT CAST(Ngay AS date) AS Ngay,COUNT(*) AS SoHoaDon,SUM(TongPhaiTra) AS DoanhThu,SUM(DaTra) AS DaThu FROM vHoaDon WHERE Ngay>=@a AND Ngay<DATEADD(day,1,@b) GROUP BY CAST(Ngay AS date) ORDER BY Ngay",P("@a",Day("from")),P("@b",Day("to")));},false);
  }
 }
}
