using System;
using Microsoft.Data.SqlClient;
using QuanLyKhachSan.Data;
namespace QuanLyKhachSan.Forms {
 public class CatalogForm:BaseForm {
  public CatalogForm():base("Danh mục và phòng","SELECT p.SoPhong,k.Ten AS Khu,p.SucChua,p.DonGiaNgay,p.BaoTri FROM Phong p JOIN KhuVuc k ON k.MaKhu=p.MaKhu") {
   Field("room","Số phòng"); Field("zone","Mã khu", "A"); Field("cap","Sức chứa","2"); Field("price","Giá/ngày","600000");
   Button("Thêm phòng",()=>Insert("INSERT Phong(SoPhong,MaKhu,SucChua,DonGiaNgay) VALUES(@r,@z,@c,@p)",P("@r",V("room")),P("@z",V("zone")),P("@c",I("cap")),P("@p",D("price"))));
   Field("customer","Mã khách","K02");Field("name","Họ tên khách");Field("cccd","CCCD");Field("nation","Quốc tịch","Việt Nam");
   Button("Thêm khách",()=>Insert("INSERT KhachHang(MaKhach,HoTen,CCCD,QuocTich) VALUES(@a,@b,@c,@d)",P("@a",V("customer")),P("@b",V("name")),P("@c",V("cccd")),P("@d",V("nation"))));
   Field("service","Mã dịch vụ","DV03");Field("svname","Tên dịch vụ");Field("unit","Đơn vị","Lượt");Field("svprice","Đơn giá","100000");
   Button("Thêm dịch vụ",()=>Insert("INSERT DichVu VALUES(@a,@b,@c,@d)",P("@a",V("service")),P("@b",V("svname")),P("@c",V("unit")),P("@d",D("svprice"))));
   Button("Xem khách",()=>Grid.DataSource=Db.Query("SELECT * FROM KhachHang"),false);
   Button("Xem dịch vụ",()=>Grid.DataSource=Db.Query("SELECT * FROM DichVu"),false);
  }
  void Insert(string sql,params SqlParameter[] ps) { using(var c=new SqlConnection(Db.Cs)) using(var cmd=new SqlCommand(sql,c)) {cmd.Parameters.AddRange(ps);c.Open();cmd.ExecuteNonQuery();} }
 }
}
