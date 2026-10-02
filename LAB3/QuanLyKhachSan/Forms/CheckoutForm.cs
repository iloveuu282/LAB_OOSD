using System;
using System.Drawing.Printing;
using System.Windows.Forms;
using QuanLyKhachSan.Data;
namespace QuanLyKhachSan.Forms {
 public class CheckoutForm:BaseForm {
  public CheckoutForm():base("Đền bù, hóa đơn và thanh toán","SELECT MaHD,MaDat,SoNgay,TienPhong,TienDV,TienDenBu,TienCoc,TongPhaiTra,TienCocHoan,DaTra,ConLai,TrangThai FROM vHoaDon ORDER BY Ngay DESC") {
   Field("id","Mã đặt phòng","DP01");Field("room","Số phòng","A101");Field("staff","Nhân viên","NV03");Field("damage","Mã đền bù","DB01");Field("device","Thiết bị","TV01");Field("level","Mức độ","Hư nhẹ");
   Button("Lập phiếu đền bù",()=>Exec("sp_DenBu",P("@MaDenBu",V("damage")),P("@MaDat",V("id")),P("@SoPhong",V("room")),P("@MaNV",V("staff")),P("@MaTN",V("device")),P("@MucDo",V("level"))));
   Button("In phiếu đền bù",()=>PrintForm.Show(this,"PHIẾU ĐỀN BÙ",Db.Query("SELECT b.MaDenBu,b.MaDat,b.SoPhong,b.Ngay,ct.MaTN,ct.MucDo,ct.SoTien FROM DenBu b JOIN DenBuCT ct ON ct.MaDenBu=b.MaDenBu WHERE b.MaDenBu=@id",P("@id",V("damage")))),false);
   Field("invoice","Số hóa đơn","HD01");Field("days","Số ngày tính tiền","1");
   Button("Lập hóa đơn",()=>Exec("sp_LapHoaDon",P("@MaHD",V("invoice")),P("@MaDat",V("id")),P("@MaNV",V("staff")),P("@SoNgay",I("days"))));
   Field("payment","Mã thanh toán","TT01");Field("method","Hình thức","Tiền mặt");Field("amount","Số tiền","0");
   Button("Ghi thanh toán",()=>Exec("sp_ThanhToan",P("@MaTT",V("payment")),P("@MaHD",V("invoice")),P("@HinhThuc",V("method")),P("@SoTien",D("amount"))));
   Button("Trả phòng",()=>Exec("sp_TraPhong",P("@MaHD",V("invoice"))));
   Button("In hóa đơn",PrintCurrent,false);
  }
  void PrintCurrent() {
   var id=V("invoice"); var h=Db.Query("SELECT * FROM vHoaDon WHERE MaHD=@id",P("@id",id));
   if(h.Rows.Count==0) throw new Exception("Chưa có hóa đơn");
   var row=h.Rows[0];
   var pd=new PrintDocument();
   pd.PrintPage+=(sender,e)=> { float y=60; var font=new System.Drawing.Font("Arial",11); var bold=new System.Drawing.Font("Arial",16,System.Drawing.FontStyle.Bold); var g=e.Graphics;
    g.DrawString("HÓA ĐƠN KHÁCH SẠN",bold,System.Drawing.Brushes.Black,70,y);y+=50;
    foreach(var key in new[]{"MaHD","MaDat","SoNgay","TienPhong","TienDV","TienDenBu","TienCoc","TongPhaiTra","TienCocHoan","DaTra","ConLai","TrangThai"}) {g.DrawString(key+": "+row[key],font,System.Drawing.Brushes.Black,70,y);y+=30;}
    g.DrawString("Người lập: ____________________",font,System.Drawing.Brushes.Black,70,y+30);
   };
   using(var preview=new PrintPreviewDialog{Document=pd,Width=800,Height=700}) preview.ShowDialog(this);
  }
 }
}
