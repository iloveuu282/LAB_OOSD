using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace EShopping
{
 static class Ui {
  public static string Student {get{return ConfigurationManager.AppSettings["Student"]??"Họ tên và MSSV";}}
  public static Button Button(string text,Action action) {var b=new Button{Text=text,AutoSize=true,Height=32,Margin=new Padding(5)};b.Click+=(s,e)=>Guard(action);return b;}
  public static void Guard(Action action){try{action();}catch(Exception ex){MessageBox.Show(ex.Message,"e-SHOPPING",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
  public static Label Footer(){return new Label{Text=Student+"  |  LAB04 e-SHOPPING",Dock=DockStyle.Bottom,Height=32,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(236,240,245)};}
  public static DataGridView Grid(){return new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,MultiSelect=false,
   SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,RowHeadersVisible=false,BackgroundColor=Color.White,AutoGenerateColumns=true};}
  public static TableLayoutPanel Fields(){return new TableLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,ColumnCount=2,Padding=new Padding(12),ColumnStyles={new ColumnStyle(SizeType.Percent,38),new ColumnStyle(SizeType.Percent,62)}};}
  public static void Field(TableLayoutPanel t,string label,Control input){int row=t.RowCount++;t.RowStyles.Add(new RowStyle(SizeType.Absolute,42));t.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,row);input.Dock=DockStyle.Fill;input.Margin=new Padding(3,7,3,7);t.Controls.Add(input,1,row);}
  public static ComboBox Choice(){return new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};}
  public static FlowLayoutPanel Bar(){return new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,Padding=new Padding(8),WrapContents=true};}
 }
 public class RegisterForm : Form {
  public RegisterForm(AccountService account){Text="Đăng ký khách hàng";Size=new Size(640,580);MinimumSize=Size;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   var fields=Ui.Fields();var name=new TextBox{MaxLength=120};var dob=new DateTimePicker{Format=DateTimePickerFormat.Short,Value=new DateTime(2000,1,1),MaxDate=DateTime.Today};
   var identity=new TextBox{MaxLength=40};var address=new TextBox{MaxLength=300};var phone=new TextBox{MaxLength=20};var user=new TextBox{MaxLength=50};
   var pass=new TextBox{UseSystemPasswordChar=true,MaxLength=128};var email=new TextBox{MaxLength=254};
   Ui.Field(fields,"Họ tên",name);Ui.Field(fields,"Ngày sinh",dob);Ui.Field(fields,"CMND / Passport",identity);Ui.Field(fields,"Địa chỉ",address);Ui.Field(fields,"Điện thoại",phone);
   Ui.Field(fields,"Tên đăng nhập",user);Ui.Field(fields,"Mật khẩu (từ 8 ký tự)",pass);Ui.Field(fields,"Email (không bắt buộc)",email);
   var bar=Ui.Bar();bar.Controls.Add(Ui.Button("Đăng ký",()=>{account.Register(new Registration{Customer=new Customer{FullName=name.Text,BirthDate=dob.Value,IdentityNo=identity.Text,Address=address.Text,Phone=phone.Text,Username=user.Text,Email=email.Text},Password=pass.Text});MessageBox.Show("Đăng ký thành công. Hãy đăng nhập.");DialogResult=DialogResult.OK;Close();}));
   Controls.Add(fields);Controls.Add(bar);Controls.Add(Ui.Footer());
  }
 }
 public class LoginForm : Form {
  public Customer LoggedIn {get;private set;}
  public LoginForm(AccountService account){Text="Đăng nhập";Size=new Size(520,290);MinimumSize=Size;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   var fields=Ui.Fields();var user=new TextBox{MaxLength=50};var pass=new TextBox{UseSystemPasswordChar=true,MaxLength=128};Ui.Field(fields,"Tên đăng nhập",user);Ui.Field(fields,"Mật khẩu",pass);
   var bar=Ui.Bar();var b=Ui.Button("Đăng nhập",()=>{LoggedIn=account.Login(user.Text,pass.Text);DialogResult=DialogResult.OK;Close();});bar.Controls.Add(b);AcceptButton=b;
   Controls.Add(fields);Controls.Add(bar);Controls.Add(Ui.Footer());
  }
 }
 public class MainForm : Form {
  readonly IShopRepository repo;readonly CatalogService catalog;readonly AccountService account;readonly CartService cart=new CartService();
  readonly SqlProductMockAdapter products;readonly MockPaymentAdapter payment;readonly FileEmailAdapter email;
  readonly Label identity=new Label{AutoSize=true,Padding=new Padding(8)};readonly ComboBox category=Ui.Choice();
  readonly DataGridView productGrid=Ui.Grid(),cartGrid=Ui.Grid();readonly NumericUpDown quantity=new NumericUpDown{Minimum=1,Maximum=999,Value=1,Width=80};
  readonly NumericUpDown newQuantity=new NumericUpDown{Minimum=1,Maximum=999,Value=1,Width=80};Customer current;
  public MainForm(IShopRepository r,SqlProductMockAdapter p,MockPaymentAdapter pay,FileEmailAdapter mail){repo=r;products=p;payment=pay;email=mail;catalog=new CatalogService(p);account=new AccountService(r);
   Text="LAB04 e-SHOPPING";Size=new Size(1120,760);MinimumSize=new Size(900,600);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);
   var top=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(8)};top.Controls.Add(identity);
   top.Controls.Add(Ui.Button("Đăng ký",()=>new RegisterForm(account).ShowDialog(this)));
   top.Controls.Add(Ui.Button("Đăng nhập",()=>Login()));top.Controls.Add(Ui.Button("Đăng xuất",()=>{current=null;cart.Clear();RefreshCart();RefreshIdentity();}));
   var tabs=new TabControl{Dock=DockStyle.Fill};var catalogTab=new TabPage("Danh mục sản phẩm");var cartTab=new TabPage("Giỏ hàng");tabs.TabPages.Add(catalogTab);tabs.TabPages.Add(cartTab);
   var filter=new FlowLayoutPanel{Dock=DockStyle.Top,Height=52,Padding=new Padding(8)};filter.Controls.Add(new Label{Text="Nhóm sản phẩm",AutoSize=true,Padding=new Padding(0,8,0,0)});category.Width=300;filter.Controls.Add(category);filter.Controls.Add(Ui.Button("Tải lại",()=>LoadProducts()));
   var pbar=Ui.Bar();pbar.Controls.Add(new Label{Text="Số lượng",AutoSize=true,Padding=new Padding(0,8,0,0)});pbar.Controls.Add(quantity);
   pbar.Controls.Add(Ui.Button("Xem chi tiết",()=>{var p1=SelectedProduct();new ProductDetailForm(catalog.Detail(p1.Id),()=>{cart.Add(catalog.Detail(p1.Id),(int)quantity.Value);RefreshCart();}).ShowDialog(this);}));
   pbar.Controls.Add(Ui.Button("Thêm vào giỏ",()=>{cart.Add(catalog.Detail(SelectedProduct().Id),(int)quantity.Value);RefreshCart();MessageBox.Show("Đã thêm vào giỏ hàng.");}));
   catalogTab.Controls.Add(productGrid);catalogTab.Controls.Add(pbar);catalogTab.Controls.Add(filter);
   var cbar=Ui.Bar();cbar.Controls.Add(new Label{Text="Số lượng mới",AutoSize=true,Padding=new Padding(0,8,0,0)});cbar.Controls.Add(newQuantity);
   cbar.Controls.Add(Ui.Button("Cập nhật",()=>{cart.Set(SelectedCart().ProductId,(int)newQuantity.Value);RefreshCart();}));cbar.Controls.Add(Ui.Button("Xóa dòng",()=>{cart.Remove(SelectedCart().ProductId);RefreshCart();}));
   cbar.Controls.Add(Ui.Button("Tính tiền / Đối soát",()=>{if(current==null&&!Login())return;new CheckoutForm(repo,products,payment,email,current,cart,RefreshCart).ShowDialog(this);}));
   cartTab.Controls.Add(cartGrid);cartTab.Controls.Add(cbar);Controls.Add(tabs);Controls.Add(top);Controls.Add(Ui.Footer());
   category.SelectedIndexChanged+=(s,e)=>Ui.Guard(LoadProducts);category.DataSource=catalog.Categories();RefreshIdentity();RefreshCart();
  }
  bool Login(){using(var f=new LoginForm(account)){if(f.ShowDialog(this)!=DialogResult.OK)return false;current=f.LoggedIn;RefreshIdentity();return true;}}
  void RefreshIdentity(){identity.Text=current==null?"Khách chưa đăng nhập":"Xin chào "+current.FullName;}
  Product SelectedProduct(){var p=productGrid.CurrentRow==null?null:productGrid.CurrentRow.DataBoundItem as Product;if(p==null)throw new InvalidOperationException("Chọn sản phẩm trước.");return p;}
  CartLine SelectedCart(){var c=cartGrid.CurrentRow==null?null:cartGrid.CurrentRow.DataBoundItem as CartLine;if(c==null)throw new InvalidOperationException("Chọn dòng giỏ hàng trước.");return c;}
  void LoadProducts(){if(category.SelectedItem==null)return;productGrid.DataSource=catalog.List(((Category)category.SelectedItem).Id);
   foreach(string n in new[]{"CategoryId","Description","Specifications","Images"})if(productGrid.Columns.Contains(n))productGrid.Columns[n].Visible=false;
   productGrid.Columns["Id"].HeaderText="Mã";productGrid.Columns["Name"].HeaderText="Tên sản phẩm";productGrid.Columns["Manufacturer"].HeaderText="Nhà sản xuất";productGrid.Columns["Price"].HeaderText="Giá hiện hành";productGrid.Columns["Price"].DefaultCellStyle.Format="N0";productGrid.Columns["InStock"].HeaderText="Còn hàng";
  }
  void RefreshCart(){cartGrid.DataSource=cart.Lines;cartGrid.Columns["ProductId"].HeaderText="Mã";cartGrid.Columns["Name"].HeaderText="Sản phẩm";cartGrid.Columns["Quantity"].HeaderText="Số lượng";cartGrid.Columns["DisplayPrice"].HeaderText="Giá tham khảo";cartGrid.Columns["Total"].HeaderText="Thành tiền tham khảo";cartGrid.Columns["DisplayPrice"].DefaultCellStyle.Format="N0";cartGrid.Columns["Total"].DefaultCellStyle.Format="N0";}
 }
 public class ProductDetailForm : Form {
  public ProductDetailForm(Product p,Action add){Text="Chi tiết "+p.Name;Size=new Size(800,560);MinimumSize=Size;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   var image=new PictureBox{Dock=DockStyle.Left,Width=300,SizeMode=PictureBoxSizeMode.Zoom};int index=0;
   Action show=()=>{if(image.Image!=null){image.Image.Dispose();image.Image=null;}if(p.Images.Count==0)return;string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,p.Images[index]);if(File.Exists(path))using(var loaded=Image.FromFile(path))image.Image=new Bitmap(loaded);};
   var text=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Text=p.Id+" — "+p.Name+"\r\nNhà sản xuất: "+p.Manufacturer+"\r\nGiá: "+p.Price.ToString("N0")+" đ\r\n"+(p.InStock?"Còn hàng":"Hết hàng")+"\r\n\r\n"+p.Description+"\r\n\r\nThông số kỹ thuật\r\n"+p.Specifications+"\r\n\r\nHình minh họa mẫu, không phải ảnh thương mại."};
   var bar=Ui.Bar();bar.Controls.Add(Ui.Button("Ảnh tiếp theo",()=>{if(p.Images.Count>0){index=(index+1)%p.Images.Count;show();}}));var b=Ui.Button("Thêm vào giỏ",()=>{add();MessageBox.Show("Đã thêm vào giỏ.");});b.Enabled=p.InStock;bar.Controls.Add(b);
   Controls.Add(text);Controls.Add(image);Controls.Add(bar);Controls.Add(Ui.Footer());show();FormClosed+=(s,e)=>{if(image.Image!=null)image.Image.Dispose();};
  }
 }
 public class CheckoutForm : Form {
  readonly IShopRepository repo;readonly CheckoutService service;readonly Customer customer;readonly CartService cart;readonly Action refresh;
  readonly TextBox name=new TextBox{MaxLength=120},address=new TextBox{MaxLength=300},phone=new TextBox{MaxLength=20};
  readonly TextBox number=new TextBox{MaxLength=16},holder=new TextBox{MaxLength=120},csv=new TextBox{MaxLength=4,UseSystemPasswordChar=true};
  readonly DateTimePicker expiry=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="MM/yyyy",ShowUpDown=true,Value=DateTime.Today.AddYears(1)};
  readonly ComboBox region=Ui.Choice(),shipping=Ui.Choice(),cardType=Ui.Choice(),simulation=Ui.Choice();readonly CheckBox failMail=new CheckBox{Text="Giả lập email gửi lỗi",AutoSize=true};
  readonly TextBox requestId=new TextBox{Width=315};readonly Label total=new Label{Dock=DockStyle.Fill,AutoSize=true,Padding=new Padding(12)};
  readonly DataGridView quoteGrid=Ui.Grid();readonly Button confirm,reconcile,calculate,retryMail;readonly TableLayoutPanel recipientFields=Ui.Fields(),cardFields=Ui.Fields();
  Quote quote;Guid id=Guid.NewGuid();int? orderId;bool submittedHere;
  public CheckoutForm(IShopRepository r,IProductAdapter products,MockPaymentAdapter payment,FileEmailAdapter email,Customer u,CartService c,Action refreshCart){repo=r;customer=u;cart=c;refresh=refreshCart;service=new CheckoutService(r,products,payment,email);
   Text="Đặt hàng và tính tiền";Size=new Size(1060,850);MinimumSize=new Size(980,760);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   name.Text=u.FullName;address.Text=u.Address;phone.Text=u.Phone;holder.Text=u.FullName;
   region.DataSource=service.Regions();shipping.DataSource=service.ShippingOptions();cardType.DataSource=service.CardTypes();
   Ui.Field(recipientFields,"Người nhận",name);Ui.Field(recipientFields,"Địa chỉ nhận",address);Ui.Field(recipientFields,"Điện thoại",phone);Ui.Field(recipientFields,"Khu vực",region);Ui.Field(recipientFields,"Hình thức giao",shipping);
   Ui.Field(cardFields,"Loại thẻ",cardType);Ui.Field(cardFields,"Số thẻ thử nghiệm",number);Ui.Field(cardFields,"Chủ thẻ",holder);Ui.Field(cardFields,"Hết hạn MM/yyyy",expiry);Ui.Field(cardFields,"CSV",csv);
   var fields=new TableLayoutPanel{Dock=DockStyle.Top,Height=240,ColumnCount=2};fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
   fields.Controls.Add(recipientFields,0,0);fields.Controls.Add(cardFields,1,0);
   var mode=new FlowLayoutPanel{Dock=DockStyle.Top,Height=85,Padding=new Padding(8),WrapContents=true};simulation.DataSource=Enum.GetValues(typeof(PaymentMode));simulation.Width=200;mode.Controls.Add(new Label{Text="Mô phỏng thanh toán",AutoSize=true,Padding=new Padding(0,8,0,0)});mode.Controls.Add(simulation);mode.Controls.Add(failMail);
   mode.Controls.Add(new Label{Text="Không dùng thẻ thật. Email được ghi ra file; không gửi ra Internet.",AutoSize=true,Padding=new Padding(6,8,0,0)});
   simulation.SelectedIndexChanged+=(s,e)=>payment.Mode=(PaymentMode)simulation.SelectedItem;failMail.CheckedChanged+=(s,e)=>email.Fail=failMail.Checked;payment.Mode=(PaymentMode)simulation.SelectedItem;email.Fail=false;
   var idBar=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,Padding=new Padding(8)};idBar.Controls.Add(new Label{Text="Mã yêu cầu (giữ để khôi phục)",AutoSize=true,Padding=new Padding(0,8,0,0)});requestId.Text=id.ToString();idBar.Controls.Add(requestId);
   var bar=Ui.Bar();calculate=Ui.Button("1. Tính lại tổng tiền",Calculate);confirm=Ui.Button("2. Xác nhận đặt hàng",Submit);confirm.Enabled=false;
   reconcile=Ui.Button("Đối soát / Khôi phục",()=>{Guid parsed;if(!Guid.TryParse(requestId.Text,out parsed))throw new InvalidOperationException("Mã yêu cầu không hợp lệ.");var result=service.Resume(parsed,customer.Id);if(parsed!=id)submittedHere=false;id=parsed;Handle(result);});
   retryMail=Ui.Button("Gửi lại email",()=>{if(!orderId.HasValue)throw new InvalidOperationException("Chưa có đơn.");service.RetryEmail(orderId.Value,customer.Id);MessageBox.Show("Trạng thái email: "+service.GetReceipt(orderId.Value,customer.Id).EmailState);});retryMail.Enabled=false;
   bar.Controls.Add(calculate);bar.Controls.Add(confirm);bar.Controls.Add(reconcile);bar.Controls.Add(retryMail);
   var summary=new Panel{Dock=DockStyle.Bottom,Height=100};summary.Controls.Add(total);
   Controls.Add(quoteGrid);Controls.Add(summary);Controls.Add(fields);Controls.Add(mode);Controls.Add(idBar);Controls.Add(bar);Controls.Add(Ui.Footer());
   region.SelectedIndexChanged+=(s,e)=>InvalidateQuote();shipping.SelectedIndexChanged+=(s,e)=>InvalidateQuote();cardType.SelectedIndexChanged+=(s,e)=>InvalidateQuote();
   FormClosing+=(s,e)=>{number.Clear();csv.Clear();};
  }
  void InvalidateQuote(){quote=null;confirm.Enabled=false;total.Text="Hãy tính lại tổng tiền sau khi đổi lựa chọn.";}
  void Calculate(){quote=service.Quote(cart.Lines,((Region)region.SelectedItem).Id,((ShippingOption)shipping.SelectedItem).Id,((CardType)cardType.SelectedItem).Id);
   quoteGrid.DataSource=quote.Lines;quoteGrid.Columns["UnitPrice"].DefaultCellStyle.Format="N0";quoteGrid.Columns["Total"].DefaultCellStyle.Format="N0";
   total.Text="Tiền hàng: "+quote.Goods.ToString("N0")+" đ  |  Phí giao: "+quote.Shipping.ToString("N0")+" đ  |  Lệ phí thẻ: "+quote.CardFee.ToString("N2")+" đ\r\nTỔNG THANH TOÁN: "+quote.Total.ToString("N2")+" đ"+(quote.PriceChanged?"\r\nGiá sản phẩm đã đổi so với giỏ hàng. Xác nhận theo giá mới hiển thị.":"");confirm.Enabled=true;}
  void Submit(){confirm.Enabled=false;requestId.Text=id.ToString();requestId.ReadOnly=true;
   submittedHere=true;
   try{Handle(service.Submit(id,customer.Id,new Recipient{Name=name.Text,Address=address.Text,Phone=phone.Text,RegionId=((Region)region.SelectedItem).Id},quote,
    new CardInput{Type=((CardType)cardType.SelectedItem).Id,Number=number.Text,Holder=holder.Text,Expiry=expiry.Value,Csv=csv.Text}));}
   catch{try{if(service.RequestExists(id))SetLocked(true);else confirm.Enabled=quote!=null;}catch{SetLocked(true);}throw;}
   finally{number.Clear();csv.Clear();}
  }
  void SetLocked(bool locked){recipientFields.Enabled=!locked;cardFields.Enabled=!locked;calculate.Enabled=!locked;confirm.Enabled=!locked&&quote!=null;requestId.ReadOnly=locked;}
  void Handle(CheckoutOutcome outcome){MessageBox.Show(outcome.Message,"Kết quả");
   if(outcome.State=="COMPLETED"){orderId=outcome.Order.Id;if(submittedHere){cart.Clear();refresh();}SetLocked(true);retryMail.Enabled=outcome.Order.EmailState!="SKIPPED";
    new ReceiptForm(outcome.Order).ShowDialog(this);}
   else if(outcome.State=="DECLINED"){id=Guid.NewGuid();requestId.Text=id.ToString();SetLocked(false);}
   else SetLocked(true);
  }
 }
 public class ReceiptForm : Form {
  public ReceiptForm(OrderReceipt order){Text="Đơn hàng #"+order.Id;Size=new Size(820,650);MinimumSize=Size;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   var body=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,Text=CheckoutService.EmailBody(order)+"\r\nEmail: "+order.EmailState+"\r\nMã yêu cầu: "+order.RequestId};Controls.Add(body);Controls.Add(Ui.Footer());}
 }
}
