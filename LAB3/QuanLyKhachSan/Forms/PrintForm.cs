using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
namespace QuanLyKhachSan.Forms {
 public static class PrintForm {
  public static void Show(Form owner,string title,DataTable rows) {
   if(rows.Rows.Count==0) throw new Exception("Không tìm thấy dữ liệu để in");
   var document=new PrintDocument();
   document.PrintPage+=(s,e)=>{
    float y=65; var g=e.Graphics;
    using(var bold=new Font("Arial",16,FontStyle.Bold)) using(var font=new Font("Arial",10)) {
     g.DrawString(title,bold,Brushes.Black,55,y); y+=45;
     foreach(DataRow row in rows.Rows) { foreach(DataColumn col in rows.Columns) { g.DrawString(col.ColumnName+": "+Convert.ToString(row[col]),font,Brushes.Black,65,y); y+=23; } y+=15; }
     g.DrawString("Người lập: ____________________       Khách hàng: ____________________",font,Brushes.Black,65,y+20);
    }
   };
   using(var preview=new PrintPreviewDialog{Document=document,Width=850,Height=720}) preview.ShowDialog(owner);
  }
 }
}
