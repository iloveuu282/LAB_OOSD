using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

namespace EShopping
{
 // Dedicated adapter to a SEPARATE product database. e-SHOPPING UI has no product CRUD.
 public class SqlProductMockAdapter : IProductAdapter {
  public List<Category> Categories() { return Db.Query("Products","SELECT * FROM dbo.Category ORDER BY Id").AsEnumerable()
   .Select(r=>new Category {Id=Db.Text(r,"Id"),Name=Db.Text(r,"Name")}).ToList(); }
  static Product From(DataRow r) { return new Product {Id=Db.Text(r,"Id"),CategoryId=Db.Text(r,"CategoryId"),Name=Db.Text(r,"Name"),
   Manufacturer=Db.Text(r,"Manufacturer"),Description=Db.Text(r,"Description"),Specifications=Db.Text(r,"Specifications"),Price=Convert.ToDecimal(r["Price"]),InStock=Convert.ToBoolean(r["InStock"])}; }
  public List<Product> List(string categoryId) { return Db.Query("Products","SELECT * FROM dbo.Product WHERE CategoryId=@id ORDER BY Id","@id",categoryId).AsEnumerable().Select(From).ToList(); }
  public Product Get(string id) {
   var t=Db.Query("Products","SELECT * FROM dbo.Product WHERE Id=@id","@id",id);if(t.Rows.Count==0)throw new InvalidOperationException("Sản phẩm không tồn tại trong dịch vụ ngoài.");
   var p=From(t.Rows[0]);p.Images=Db.Query("Products","SELECT Path FROM dbo.ProductImage WHERE ProductId=@id ORDER BY Ordinal","@id",id)
    .AsEnumerable().Select(r=>Db.Text(r,"Path")).ToList();return p;
  }
 }
 // Authorize validates ability to pay; it does NOT move real money.
 // TimeoutThenApproved simulates an authorization whose response was lost.
 public class MockPaymentAdapter : IPaymentAdapter {
  readonly string folder;
  public MockPaymentAdapter(string root=null){folder=root??Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"PaymentMock");}
  public PaymentMode Mode {get;set;} = PaymentMode.Approved;
  public int AuthorizationCalls {get;private set;}
  public PaymentResult Authorize(Guid id,decimal amount,CardInput card) {
   AuthorizationCalls++;
   var previous=Read(id);if(previous!=null)return previous;
   if(Mode==PaymentMode.Unavailable)throw new IOException("Dịch vụ thanh toán giả lập không truy cập được.");
   var result=Mode==PaymentMode.Declined?new PaymentResult{State="DECLINED",Amount=amount}:Approved(id,amount,card.Number.Substring(card.Number.Length-4));
   Write(id,result);
   if(Mode==PaymentMode.TimeoutThenApproved)throw new TimeoutException("Chưa rõ kết quả. Dùng nút Đối soát cùng mã yêu cầu.");
   return result;
  }
  public PaymentResult Query(Guid id,decimal amount) {
   if(Mode==PaymentMode.Unavailable)throw new IOException("Dịch vụ chưa sẵn sàng.");
   // A missing ledger entry is a definitive NOT_FOUND in this synchronous mock,
   // normalized to DECLINED. Real asynchronous providers must keep UNKNOWN until final.
   return Read(id)??new PaymentResult{State="DECLINED",Amount=amount};
  }
  PaymentResult Read(Guid id){string p=Path.Combine(folder,id.ToString("N")+".txt");if(!File.Exists(p))return null;var x=File.ReadAllLines(p);return new PaymentResult{State=x[0],Amount=decimal.Parse(x[1],CultureInfo.InvariantCulture),Reference=x[2].Length==0?null:x[2],Token=x[3].Length==0?null:x[3],Last4=x[4].Length==0?null:x[4]};}
  void Write(Guid id,PaymentResult r){Directory.CreateDirectory(folder);string p=Path.Combine(folder,id.ToString("N")+".txt");string temp=p+".tmp";File.WriteAllLines(temp,new[]{r.State,r.Amount.ToString(CultureInfo.InvariantCulture),r.Reference??"",r.Token??"",r.Last4??""});File.Move(temp,p);}
  static PaymentResult Approved(Guid id,decimal amount,string last4) { return new PaymentResult {State="AUTHORIZED",Amount=amount,
   Reference="MOCK-AUTH-"+id.ToString("N"),Token="MOCK-TOKEN-"+id.ToString("N"),Last4=last4}; }
 }
 // Represents an external mail service using an idempotent local .eml-like text sink.
 public class FileEmailAdapter : IEmailAdapter {
  public bool Fail {get;set;}
  public void Send(Guid id,string address,string subject,string body) {
   if(Fail)throw new IOException("Lỗi gửi email giả lập.");
   string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"EmailOutbox");Directory.CreateDirectory(folder);
   string target=Path.Combine(folder,id.ToString("N")+".txt");
   if(File.Exists(target))return;
   File.WriteAllText(target,"To: "+address+"\r\nSubject: "+subject+"\r\n\r\n"+body,Encoding.UTF8);
  }
 }
}
