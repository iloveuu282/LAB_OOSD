using System;
using System.Collections.Generic;
using System.Linq;

namespace EShopping
{
 public class Customer {
  public int Id { get; set; } public string FullName { get; set; }
  public DateTime BirthDate { get; set; } public string IdentityNo { get; set; }
  public string Address { get; set; } public string Phone { get; set; }
  public string Username { get; set; } public string Email { get; set; }
 }
 public class Registration { public Customer Customer { get; set; } public string Password { get; set; } }
 public class Category { public string Id { get; set; } public string Name { get; set; } public override string ToString() { return Name; } }
 public class Product {
  public string Id { get; set; } public string CategoryId { get; set; }
  public string Name { get; set; } public string Manufacturer { get; set; }
  public string Description { get; set; } public string Specifications { get; set; }
  public decimal Price { get; set; } public bool InStock { get; set; }
  public List<string> Images { get; set; } = new List<string>();
 }
 public class CartLine { public string ProductId { get; set; } public string Name { get; set; }
  public int Quantity { get; set; } public decimal DisplayPrice { get; set; }
  public decimal Total { get { return Quantity * DisplayPrice; } }
 }
 public class OrderLine { public string ProductId { get; set; } public string Name { get; set; }
  public int Quantity { get; set; } public decimal UnitPrice { get; set; }
  public decimal Total { get { return Quantity * UnitPrice; } }
 }
 public class ShippingOption { public string Id { get; set; } public string Name { get; set; }
  public int ProcessingHours { get; set; } public override string ToString() { return Name + " (" + ProcessingHours + " giờ xử lý)"; } }
 public class Region { public string Id { get; set; } public string Name { get; set; } public override string ToString() { return Name; } }
 public class CardType { public string Id { get; set; } public decimal FeeRate { get; set; }
  public decimal FixedFee { get; set; } public override string ToString() { return Id; } }
 public class Recipient { public string Name { get; set; } public string Address { get; set; }
  public string Phone { get; set; } public string RegionId { get; set; } }
 // Sensitive card fields are transient: never stored in SQL, email, logs or request snapshots.
 public class CardInput { public string Type { get; set; } public string Number { get; set; }
  public string Holder { get; set; } public DateTime Expiry { get; set; } public string Csv { get; set; }
  public void ClearSensitive() { Number = null; Csv = null; Holder = null; }
 }
 public class Quote {
  public List<OrderLine> Lines { get; set; } = new List<OrderLine>();
  public string ShippingId { get; set; } public string RegionId { get; set; } public string CardType { get; set; }
  public decimal Goods { get { return Lines.Sum(x => x.Total); } }
  public decimal Shipping { get; set; } public decimal CardFee { get; set; }
  public decimal Total { get { return Goods + Shipping + CardFee; } }
  public bool PriceChanged { get; set; } public DateTime CreatedAt { get; set; }
 }
 public class CheckoutRequest {
  public Guid Id { get; set; } public int CustomerId { get; set; } public Recipient Recipient { get; set; }
  public Quote Quote { get; set; } public string State { get; set; }
  public string Reference { get; set; } public string Token { get; set; } public string Last4 { get; set; }
  public int? OrderId { get; set; }
 }
 public class PaymentResult { public string State { get; set; } public string Reference { get; set; }
  public string Token { get; set; } public string Last4 { get; set; } public decimal Amount { get; set; } }
 public class OrderReceipt { public int Id { get; set; } public Guid RequestId { get; set; }
  public Customer Customer { get; set; } public Recipient Recipient { get; set; } public Quote Quote { get; set; }
  public DateTime OrderedAt { get; set; } public string EmailState { get; set; }
 }
 public class CheckoutOutcome { public string State { get; set; } public string Message { get; set; } public OrderReceipt Order { get; set; } }
 public enum PaymentMode { Approved, Declined, TimeoutThenApproved, Unavailable }
 public interface IProductAdapter {
  List<Category> Categories(); List<Product> List(string categoryId); Product Get(string id);
 }
 public interface IPaymentAdapter {
  PaymentResult Authorize(Guid requestId, decimal amount, CardInput card);
  PaymentResult Query(Guid requestId, decimal amount);
 }
 public interface IEmailAdapter { void Send(Guid messageId, string address, string subject, string body); }
 public interface IShopRepository {
  int Register(Customer customer, byte[] hash, byte[] salt);
  Tuple<Customer,byte[],byte[]> FindLogin(string username);
  Customer GetCustomer(int id);
  List<ShippingOption> ShippingOptions(); List<Region> Regions(); List<CardType> CardTypes();
  decimal ShippingFee(string regionId, string shippingId);
  void SaveRequest(CheckoutRequest request); CheckoutRequest GetRequest(Guid id);
  void SetPayment(Guid id, PaymentResult result); int Complete(Guid id);
  OrderReceipt Receipt(int orderId); void EmailStatus(int orderId, string status, string error);
 }
}
