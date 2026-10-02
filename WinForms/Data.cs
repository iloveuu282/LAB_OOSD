using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace EShopping
{
 public static class Db {
  public static string Connection(string name) { return ConfigurationManager.ConnectionStrings[name].ConnectionString; }
  public static SqlCommand Command(SqlConnection c, string sql, params object[] pairs) {
   var cmd = new SqlCommand(sql,c); cmd.CommandTimeout=15;
   for(int i=0;i<pairs.Length;i+=2) {
    var p=cmd.Parameters.AddWithValue((string)pairs[i],pairs[i+1] ?? DBNull.Value);
    if(pairs[i+1] is decimal) { p.SqlDbType=SqlDbType.Decimal;p.Precision=18;p.Scale=2; }
   }
   return cmd;
  }
  public static DataTable Query(string connection, string sql, params object[] pairs) {
   using(var c=new SqlConnection(Connection(connection))) using(var cmd=Command(c,sql,pairs)) using(var da=new SqlDataAdapter(cmd)) {
    var t=new DataTable(); da.Fill(t); return t;
   }
  }
  public static object Scalar(string connection,string sql,params object[] pairs) {
   using(var c=new SqlConnection(Connection(connection))) using(var cmd=Command(c,sql,pairs)) { c.Open();return cmd.ExecuteScalar(); }
  }
  public static string Text(DataRow r,string col) { return r.IsNull(col) ? null : Convert.ToString(r[col]); }
 }
 public class SqlShopRepository : IShopRepository {
  public int Register(Customer u,byte[] hash,byte[] salt) {
   try { return Convert.ToInt32(Db.Scalar("Shop",@"INSERT dbo.Customer(FullName,BirthDate,IdentityNo,Address,Phone,Username,PasswordHash,PasswordSalt,Email)
    VALUES(@name,@dob,@ident,@address,@phone,@user,@hash,@salt,@email); SELECT CONVERT(int,SCOPE_IDENTITY());",
    "@name",u.FullName,"@dob",u.BirthDate.Date,"@ident",u.IdentityNo,"@address",u.Address,"@phone",u.Phone,
    "@user",u.Username,"@hash",hash,"@salt",salt,"@email",u.Email)); }
   catch(SqlException ex) { if(ex.Number==2601 || ex.Number==2627) throw new InvalidOperationException("Tên đăng nhập đã tồn tại."); throw; }
  }
  static Customer CustomerFrom(DataRow r) { return new Customer { Id=Convert.ToInt32(r["Id"]),FullName=Db.Text(r,"FullName"),BirthDate=Convert.ToDateTime(r["BirthDate"]),
   IdentityNo=Db.Text(r,"IdentityNo"),Address=Db.Text(r,"Address"),Phone=Db.Text(r,"Phone"),Username=Db.Text(r,"Username"),Email=Db.Text(r,"Email") }; }
  public Tuple<Customer,byte[],byte[]> FindLogin(string username) {
   var t=Db.Query("Shop","SELECT * FROM dbo.Customer WHERE Username=@u","@u",username);
   if(t.Rows.Count==0)return null; var r=t.Rows[0];return Tuple.Create(CustomerFrom(r),(byte[])r["PasswordHash"],(byte[])r["PasswordSalt"]);
  }
  public Customer GetCustomer(int id) { var t=Db.Query("Shop","SELECT * FROM dbo.Customer WHERE Id=@id","@id",id);if(t.Rows.Count==0)throw new InvalidOperationException("Không có khách hàng.");return CustomerFrom(t.Rows[0]); }
  public List<ShippingOption> ShippingOptions() { return Db.Query("Shop","SELECT * FROM dbo.ShippingMethod ORDER BY ProcessingHours DESC").AsEnumerable().Select(r=>new ShippingOption {Id=Db.Text(r,"Id"),Name=Db.Text(r,"Name"),ProcessingHours=Convert.ToInt32(r["ProcessingHours"])}).ToList(); }
  public List<Region> Regions() { return Db.Query("Shop","SELECT * FROM dbo.Region ORDER BY Id").AsEnumerable().Select(r=>new Region {Id=Db.Text(r,"Id"),Name=Db.Text(r,"Name")}).ToList(); }
  public List<CardType> CardTypes() { return Db.Query("Shop","SELECT * FROM dbo.CardType ORDER BY Id").AsEnumerable().Select(r=>new CardType {Id=Db.Text(r,"Id"),FeeRate=Convert.ToDecimal(r["FeeRate"]),FixedFee=Convert.ToDecimal(r["FixedFee"])}).ToList(); }
  public decimal ShippingFee(string regionId,string shippingId) {
   var value=Db.Scalar("Shop","SELECT Fee FROM dbo.ShippingRate WHERE RegionId=@r AND ShippingId=@s","@r",regionId,"@s",shippingId);
   if(value==null)throw new InvalidOperationException("Chưa cấu hình phí giao hàng.");return Convert.ToDecimal(value);
  }
  public void SaveRequest(CheckoutRequest a) {
   using(var c=new SqlConnection(Db.Connection("Shop"))) {
    c.Open();using(var tr=c.BeginTransaction()) {
     try {
      using(var cmd=Db.Command(c,@"INSERT dbo.CheckoutAttempt(Id,CustomerId,RecipientName,RecipientAddress,RecipientPhone,RegionId,ShippingId,CardTypeId,Goods,Shipping,CardFee)
        VALUES(@id,@c,@n,@addr,@p,@r,@s,@type,@goods,@ship,@fee)","@id",a.Id,"@c",a.CustomerId,"@n",a.Recipient.Name,"@addr",a.Recipient.Address,"@p",a.Recipient.Phone,
        "@r",a.Recipient.RegionId,"@s",a.Quote.ShippingId,"@type",a.Quote.CardType,"@goods",a.Quote.Goods,"@ship",a.Quote.Shipping,"@fee",a.Quote.CardFee)) {
       cmd.Transaction=tr;cmd.ExecuteNonQuery();
      }
      foreach(var line in a.Quote.Lines)using(var cmd=Db.Command(c,@"INSERT dbo.CheckoutAttemptLine(AttemptId,ProductId,ProductName,Quantity,UnitPrice) VALUES(@id,@p,@n,@q,@price)",
       "@id",a.Id,"@p",line.ProductId,"@n",line.Name,"@q",line.Quantity,"@price",line.UnitPrice)) {cmd.Transaction=tr;cmd.ExecuteNonQuery();}
      tr.Commit();
     } catch { tr.Rollback();throw; }
    }
   }
  }
  public CheckoutRequest GetRequest(Guid id) {
   var t=Db.Query("Shop",@"SELECT a.*,p.ProviderReference,p.PaymentToken,p.Last4,o.Id AS OrderId FROM dbo.CheckoutAttempt a
    LEFT JOIN dbo.PaymentRecord p ON p.AttemptId=a.Id LEFT JOIN dbo.[Order] o ON o.AttemptId=a.Id WHERE a.Id=@id","@id",id);
   if(t.Rows.Count==0)return null; var r=t.Rows[0];
   var q=new Quote { ShippingId=Db.Text(r,"ShippingId"),RegionId=Db.Text(r,"RegionId"),CardType=Db.Text(r,"CardTypeId"),
    Shipping=Convert.ToDecimal(r["Shipping"]),CardFee=Convert.ToDecimal(r["CardFee"]),CreatedAt=Convert.ToDateTime(r["CreatedAt"]) };
   q.Lines=Db.Query("Shop","SELECT * FROM dbo.CheckoutAttemptLine WHERE AttemptId=@id ORDER BY ProductId","@id",id).AsEnumerable()
    .Select(x=>new OrderLine {ProductId=Db.Text(x,"ProductId"),Name=Db.Text(x,"ProductName"),Quantity=Convert.ToInt32(x["Quantity"]),UnitPrice=Convert.ToDecimal(x["UnitPrice"])}).ToList();
   return new CheckoutRequest {Id=id,CustomerId=Convert.ToInt32(r["CustomerId"]),Quote=q,State=Db.Text(r,"State"),Reference=Db.Text(r,"ProviderReference"),Token=Db.Text(r,"PaymentToken"),Last4=Db.Text(r,"Last4"),
    Recipient=new Recipient {Name=Db.Text(r,"RecipientName"),Address=Db.Text(r,"RecipientAddress"),Phone=Db.Text(r,"RecipientPhone"),RegionId=q.RegionId},OrderId=r.IsNull("OrderId")?(int?)null:Convert.ToInt32(r["OrderId"])};
  }
  public void SetPayment(Guid id,PaymentResult r) {
   Db.Scalar("Shop","EXEC dbo.RecordPayment @id,@state,@amount,@reference,@token,@last4","@id",id,"@state",r.State,"@amount",r.Amount,"@reference",r.Reference,"@token",r.Token,"@last4",r.Last4);
  }
  public int Complete(Guid id) { return Convert.ToInt32(Db.Scalar("Shop","EXEC dbo.CompleteCheckout @id","@id",id)); }
  public OrderReceipt Receipt(int orderId) {
   var t=Db.Query("Shop",@"SELECT o.*,e.State AS EmailState FROM dbo.[Order] o LEFT JOIN dbo.EmailOutbox e ON e.OrderId=o.Id WHERE o.Id=@id","@id",orderId);
   if(t.Rows.Count==0)throw new InvalidOperationException("Không có đơn hàng.");var r=t.Rows[0];var a=GetRequest((Guid)r["AttemptId"]);
   // Read immutable order lines, not current external product prices.
   a.Quote.Lines=Db.Query("Shop","SELECT * FROM dbo.OrderLine WHERE OrderId=@id ORDER BY ProductId","@id",orderId).AsEnumerable()
    .Select(x=>new OrderLine{ProductId=Db.Text(x,"ProductId"),Name=Db.Text(x,"ProductName"),Quantity=Convert.ToInt32(x["Quantity"]),UnitPrice=Convert.ToDecimal(x["UnitPrice"])}).ToList();
   return new OrderReceipt {Id=orderId,RequestId=a.Id,Customer=new Customer{Id=a.CustomerId,FullName=Db.Text(r,"BuyerName"),Email=Db.Text(r,"BuyerEmail")},
    Recipient=a.Recipient,Quote=a.Quote,OrderedAt=Convert.ToDateTime(r["OrderedAt"]),EmailState=Db.Text(r,"EmailState") ?? "SKIPPED"};
  }
  public void EmailStatus(int orderId,string status,string error) {
   Db.Scalar("Shop",@"UPDATE dbo.EmailOutbox SET State=@s,Attempts=Attempts+1,LastError=@e,UpdatedAt=SYSDATETIME() WHERE OrderId=@id AND State<>'SENT'",
    "@s",status,"@e",error==null?null:error.Substring(0,Math.Min(error.Length,300)),"@id",orderId);
  }
 }
}
