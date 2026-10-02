using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EShopping
{
 public static class Rules {
  public static void Required(string value,int max,string label) { if(string.IsNullOrWhiteSpace(value)||value.Length>max)throw new InvalidOperationException(label+" bắt buộc và tối đa "+max+" ký tự."); }
  public static void Phone(string p) { if(p==null||!Regex.IsMatch(p,@"^\+?[0-9]{8,15}$"))throw new InvalidOperationException("Điện thoại phải có 8–15 chữ số, có thể bắt đầu bằng +."); }
  public static void Recipient(Recipient r) { if(r==null)throw new InvalidOperationException("Thiếu người nhận.");Required(r.Name,120,"Tên người nhận");Required(r.Address,300,"Địa chỉ nhận");Phone(r.Phone);Required(r.RegionId,10,"Khu vực"); }
  public static void Card(CardInput c,DateTime now) {
   if(c==null)throw new InvalidOperationException("Thiếu thông tin thẻ.");
   if(!new[]{"VISA","MASTER","DISCOVER","AMEX"}.Contains(c.Type))throw new InvalidOperationException("Loại thẻ không hỗ trợ.");
   int digits=c.Type=="AMEX"?15:16,csv=c.Type=="AMEX"?4:3;
   if(c.Number==null||!Regex.IsMatch(c.Number,"^[0-9]{"+digits+"}$"))throw new InvalidOperationException("Số thẻ phải có "+digits+" chữ số theo đề bài.");
   if(c.Csv==null||!Regex.IsMatch(c.Csv,"^[0-9]{"+csv+"}$"))throw new InvalidOperationException("CSV phải có "+csv+" chữ số.");
   Required(c.Holder,120,"Chủ thẻ");
   if(new DateTime(c.Expiry.Year,c.Expiry.Month,1)<new DateTime(now.Year,now.Month,1))throw new InvalidOperationException("Thẻ đã hết hạn.");
  }
  public static decimal Shipping(decimal goods,string method,decimal configuredFee) {
   if(method=="SAMEDAY"&&goods>=5000000m)return 0;
   if(method=="EXPRESS"&&goods>=1000000m)return 0;
   return configuredFee;
  }
  public static decimal CardFee(decimal goods,decimal shipping,CardType type) { return decimal.Round((goods+shipping)*type.FeeRate+type.FixedFee,2,MidpointRounding.AwayFromZero); }
 }
 public class AccountService {
  readonly IShopRepository repo;
  public AccountService(IShopRepository repository) {repo=repository;}
  public int Register(Registration r) {
   if(r==null||r.Customer==null)throw new InvalidOperationException("Thiếu thông tin đăng ký.");var u=r.Customer;
   u.FullName=(u.FullName??"").Trim();u.Username=(u.Username??"").Trim();u.Email=string.IsNullOrWhiteSpace(u.Email)?null:u.Email.Trim();
   Rules.Required(u.FullName,120,"Họ tên");Rules.Required(u.Username,50,"Tên đăng nhập");Rules.Required(u.IdentityNo,40,"CMND/Passport");Rules.Required(u.Address,300,"Địa chỉ");Rules.Phone(u.Phone);
   if(u.BirthDate.Date>DateTime.Today)throw new InvalidOperationException("Ngày sinh không được ở tương lai.");
   if(r.Password==null||r.Password.Length<8||r.Password.Length>128)throw new InvalidOperationException("Mật khẩu phải dài 8–128 ký tự.");
   if(u.Email!=null) {
    if(u.Email.Length>254)throw new InvalidOperationException("Email quá dài.");
    try {var m=new MailAddress(u.Email);if(m.Address!=u.Email)throw new FormatException();}catch(FormatException){throw new InvalidOperationException("Email không hợp lệ.");}
   }
   byte[] salt=new byte[16];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(salt);
   return repo.Register(u,Hash(r.Password,salt),salt);
  }
  public Customer Login(string username,string password) {
   var found=repo.FindLogin((username??"").Trim());
   if(found==null||password==null)throw new InvalidOperationException("Tên đăng nhập hoặc mật khẩu không đúng.");
   var computed=Hash(password,found.Item3);int diff=0;for(int i=0;i<computed.Length;i++)diff|=computed[i]^found.Item2[i];
   if(diff!=0)throw new InvalidOperationException("Tên đăng nhập hoặc mật khẩu không đúng.");return found.Item1;
  }
  public static byte[] Hash(string password,byte[] salt) {using(var p=new Rfc2898DeriveBytes(password,salt,100000,HashAlgorithmName.SHA256))return p.GetBytes(32);}
 }
 public class CatalogService {
  readonly IProductAdapter products;
  public CatalogService(IProductAdapter p){products=p;}
  public List<Category> Categories(){return products.Categories();}
  public List<Product> List(string id){return products.List(id);}
  public Product Detail(string id){return products.Get(id);}
 }
 public class CartService {
  readonly List<CartLine> lines=new List<CartLine>();
  public List<CartLine> Lines {get{return lines.Select(x=>new CartLine{ProductId=x.ProductId,Name=x.Name,Quantity=x.Quantity,DisplayPrice=x.DisplayPrice}).ToList();}}
  public void Add(Product p,int quantity) {
   if(p==null||!p.InStock)throw new InvalidOperationException("Sản phẩm đã hết hàng.");
   if(quantity<1||quantity>999)throw new InvalidOperationException("Số lượng phải từ 1 đến 999.");
   var l=lines.FirstOrDefault(x=>x.ProductId==p.Id);if(l!=null){Set(p.Id,checked(l.Quantity+quantity));l.DisplayPrice=p.Price;}
   else lines.Add(new CartLine{ProductId=p.Id,Name=p.Name,Quantity=quantity,DisplayPrice=p.Price});
  }
  public void Set(string id,int quantity){if(quantity<1||quantity>999)throw new InvalidOperationException("Số lượng phải từ 1 đến 999.");var l=lines.FirstOrDefault(x=>x.ProductId==id);if(l==null)throw new InvalidOperationException("Không có dòng giỏ hàng.");l.Quantity=quantity;}
  public void Remove(string id){lines.RemoveAll(x=>x.ProductId==id);}
  public void Clear(){lines.Clear();}
 }
 public class CheckoutService {
  readonly IShopRepository repo;readonly IProductAdapter products;readonly IPaymentAdapter payment;readonly IEmailAdapter email;
  public CheckoutService(IShopRepository r,IProductAdapter p,IPaymentAdapter pay,IEmailAdapter mail){repo=r;products=p;payment=pay;email=mail;}
  public List<Region> Regions(){return repo.Regions();}
  public List<ShippingOption> ShippingOptions(){return repo.ShippingOptions();}
  public List<CardType> CardTypes(){return repo.CardTypes();}
  public bool RequestExists(Guid id){return repo.GetRequest(id)!=null;}
  public OrderReceipt GetReceipt(int id,int customerId){var o=repo.Receipt(id);if(o.Customer.Id!=customerId)throw new InvalidOperationException("Đơn không thuộc tài khoản này.");return o;}
  public Quote Quote(IList<CartLine> cart,string regionId,string method,string cardType) {
   if(cart==null||cart.Count==0)throw new InvalidOperationException("Giỏ hàng trống.");
   if(!repo.ShippingOptions().Any(x=>x.Id==method)||!repo.Regions().Any(x=>x.Id==regionId))throw new InvalidOperationException("Hình thức hoặc khu vực giao không hợp lệ.");
   var type=repo.CardTypes().FirstOrDefault(x=>x.Id==cardType);if(type==null)throw new InvalidOperationException("Loại thẻ không hợp lệ.");
   var q=new Quote{ShippingId=method,RegionId=regionId,CardType=cardType,CreatedAt=DateTime.Now};
   foreach(var l in cart) {
    if(l.Quantity<1||l.Quantity>999)throw new InvalidOperationException("Số lượng không hợp lệ.");var p=products.Get(l.ProductId);
    if(!p.InStock)throw new InvalidOperationException(p.Name+" đã hết hàng.");
    q.Lines.Add(new OrderLine{ProductId=p.Id,Name=p.Name,Quantity=l.Quantity,UnitPrice=p.Price});if(p.Price!=l.DisplayPrice)q.PriceChanged=true;
   }
   if(q.Goods<=0)throw new InvalidOperationException("Tiền hàng phải lớn hơn 0.");
   q.Shipping=Rules.Shipping(q.Goods,method,repo.ShippingFee(regionId,method));q.CardFee=Rules.CardFee(q.Goods,q.Shipping,type);return q;
  }
  public CheckoutOutcome Submit(Guid id,int customerId,Recipient recipient,Quote accepted,CardInput card) {
   if(customerId<=0)throw new InvalidOperationException("Cần đăng nhập.");repo.GetCustomer(customerId);
   var existing=repo.GetRequest(id);if(existing!=null)return Resume(id,customerId);
   Rules.Recipient(recipient);Rules.Card(card,DateTime.Now);
   if(accepted==null||accepted.CardType!=card.Type||accepted.RegionId!=recipient.RegionId)throw new InvalidOperationException("Hãy tính lại và xác nhận tổng tiền.");
   var current=Quote(accepted.Lines.Select(x=>new CartLine{ProductId=x.ProductId,Quantity=x.Quantity,DisplayPrice=x.UnitPrice}).ToList(),recipient.RegionId,accepted.ShippingId,card.Type);
   if(current.PriceChanged||current.Shipping!=accepted.Shipping||current.CardFee!=accepted.CardFee||current.Total!=accepted.Total)
    throw new InvalidOperationException("Giá hoặc phí đã thay đổi. Tính lại trước khi xác nhận.");
   var request=new CheckoutRequest{Id=id,CustomerId=customerId,Recipient=recipient,Quote=current,State="CREATED"};
   repo.SaveRequest(request); // durable intent before external side effect
   PaymentResult result;
   try { result=payment.Authorize(id,current.Total,card); }
   catch(Exception ex) when(ex is TimeoutException||ex is System.IO.IOException) {result=new PaymentResult{State="UNKNOWN",Amount=current.Total};}
   finally {card.ClearSensitive();}
   repo.SetPayment(id,result);return Finish(repo.GetRequest(id));
  }
  public CheckoutOutcome Resume(Guid id,int customerId) {
   var a=repo.GetRequest(id);if(a==null)throw new InvalidOperationException("Mã yêu cầu không tồn tại.");
   if(a.CustomerId!=customerId)throw new InvalidOperationException("Yêu cầu không thuộc tài khoản đang đăng nhập.");
   if(a.State=="CREATED"||a.State=="UNKNOWN") {
    try {var r=payment.Query(id,a.Quote.Total);repo.SetPayment(id,r);a=repo.GetRequest(id);}
    catch(Exception ex) when(ex is TimeoutException||ex is System.IO.IOException) {return new CheckoutOutcome{State="UNKNOWN",Message="Dịch vụ chưa sẵn sàng. Giữ mã yêu cầu và đối soát sau."};}
   }
   return Finish(a);
  }
  CheckoutOutcome Finish(CheckoutRequest a) {
   if(a.State=="UNKNOWN"||a.State=="CREATED")return new CheckoutOutcome{State="UNKNOWN",Message="Chưa rõ kết quả thanh toán. Chọn Đối soát; không tạo yêu cầu mới."};
   if(a.State=="DECLINED")return new CheckoutOutcome{State="DECLINED",Message="Kiểm tra thanh toán bị từ chối. Chưa tạo đơn. Có thể sửa thẻ và tạo lần thử mới."};
   int orderId=a.OrderId??repo.Complete(a.Id);var order=repo.Receipt(orderId);
   // Email is after COMMIT. A mail error never invalidates an already confirmed order.
   TryEmail(order);order=repo.Receipt(orderId);
   return new CheckoutOutcome{State="COMPLETED",Order=order,Message="Đặt hàng thành công. Email: "+order.EmailState};
  }
  public void RetryEmail(int orderId,int customerId) {var order=repo.Receipt(orderId);if(order.Customer.Id!=customerId)throw new InvalidOperationException("Đơn không thuộc tài khoản này.");TryEmail(order);}
  void TryEmail(OrderReceipt o) {
   if(o.EmailState=="SKIPPED"||o.EmailState=="SENT")return;
   try {email.Send(o.RequestId,o.Customer.Email,"Xác nhận đơn #"+o.Id,EmailBody(o));repo.EmailStatus(o.Id,"SENT",null);}
   catch(Exception ex) {try {repo.EmailStatus(o.Id,"FAILED",ex.Message);}catch{/* Durable PENDING outbox remains retryable. */}}
  }
  public static string EmailBody(OrderReceipt o) {
   var b=new StringBuilder();b.AppendLine("Đơn hàng #"+o.Id);b.AppendLine("Thời điểm: "+o.OrderedAt.ToString("yyyy-MM-dd HH:mm:ss"));
   b.AppendLine("Người mua: "+o.Customer.FullName);b.AppendLine("Người nhận: "+o.Recipient.Name);b.AppendLine("Địa chỉ: "+o.Recipient.Address);
   b.AppendLine("Điện thoại: "+o.Recipient.Phone);b.AppendLine("Khu vực: "+o.Recipient.RegionId);b.AppendLine("Giao hàng: "+o.Quote.ShippingId);
   foreach(var l in o.Quote.Lines)b.AppendLine(l.ProductId+" | "+l.Name+" | SL "+l.Quantity+" | Đơn giá "+l.UnitPrice.ToString("N2")+" | Thành tiền "+l.Total.ToString("N2"));
   b.AppendLine("Tiền hàng: "+o.Quote.Goods.ToString("N2"));b.AppendLine("Phí giao: "+o.Quote.Shipping.ToString("N2"));b.AppendLine("Lệ phí giao dịch: "+o.Quote.CardFee.ToString("N2"));b.AppendLine("Tổng: "+o.Quote.Total.ToString("N2")+" VND");return b.ToString();
  }
 }
}
