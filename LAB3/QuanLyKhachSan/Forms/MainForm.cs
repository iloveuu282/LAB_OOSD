using System;
using System.Windows.Forms;
namespace QuanLyKhachSan.Forms {
 public class MainForm:Form {
  public MainForm() { Text="LAB3 - Quản lý khách sạn"; Width=430; Height=440; StartPosition=FormStartPosition.CenterScreen;
   var p=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,Padding=new Padding(20)}; Controls.Add(p);
   Add(p,"Danh mục và phòng",()=>new CatalogForm()); Add(p,"Tiện nghi và lắp đặt",()=>new EquipmentForm()); Add(p,"Đặt và nhận phòng",()=>new BookingForm()); Add(p,"Dịch vụ",()=>new ServiceForm()); Add(p,"Đền bù, hóa đơn, thanh toán",()=>new CheckoutForm()); Add(p,"Thống kê",()=>new StatsForm());
  }
  void Add(FlowLayoutPanel p,string title,Func<Form> make) { var b=new Button{Text=title,Width=350,Height=48}; b.Click+=(s,e)=>{using(var f=make())f.ShowDialog(this);}; p.Controls.Add(b); }
 }
}
