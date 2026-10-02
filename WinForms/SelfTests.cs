using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EShopping
{
 // Run: EShopping.exe --self-test (no UI and no SQL Server required).
 // Exercises the ACTUAL services with controlled adapter/repository test doubles.
 public static class SelfTests {
  static readonly List<string> log=new List<string>();static int failed;
  static void Equal<T>(T expected,T actual){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception("Expected "+expected+", actual "+actual);}
  static void Throws(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected validation exception");}
  static void Test(string id,Action action){try{action();log.Add("PASS "+id);}catch(Exception ex){failed++;log.Add("FAIL "+id+": "+ex.Message);}}
  static CardInput Card(string type="VISA"){return new CardInput{Type=type,Number=type=="AMEX"?"378282246310005":"4111111111111111",Csv=type=="AMEX"?"1234":"123",Holder="Test Holder",Expiry=DateTime.Today.AddYears(1)};}
  static Recipient Recipient(){return new Recipient{Name="Recipient Different",Address="Test address",Phone="0901234567",RegionId="INNER"};}
  class Fixture {
   public FakeRepo Repo=new FakeRepo();public FakeProducts Products=new FakeProducts();public FakePayment Payment=new FakePayment();public FakeEmail Email=new FakeEmail();
   public CheckoutService Service;public List<CartLine> Cart;
   public Fixture(){Service=new CheckoutService(Repo,Products,Payment,Email);Cart=new List<CartLine>{new CartLine{ProductId="P1",Name="Product",Quantity=2,DisplayPrice=500000m}};}
   public Quote Quote(string shipping="EXPRESS",string card="VISA"){return Service.Quote(Cart,"INNER",shipping,card);}
   public CheckoutOutcome Submit(Guid id){return Service.Submit(id,1,Recipient(),Quote(),Card());}
  }
  public static int Run(string output){log.Clear();failed=0;
   Test("UT01 express below threshold",()=>Equal(40000m,Rules.Shipping(999999m,"EXPRESS",40000m)));
   Test("UT02 express at threshold",()=>Equal(0m,Rules.Shipping(1000000m,"EXPRESS",40000m)));
   Test("UT03 same-day below threshold",()=>Equal(80000m,Rules.Shipping(4999999m,"SAMEDAY",80000m)));
   Test("UT04 same-day at threshold",()=>Equal(0m,Rules.Shipping(5000000m,"SAMEDAY",80000m)));
   Test("UT05 same-day at 1 million still charged",()=>Equal(80000m,Rules.Shipping(1000000m,"SAMEDAY",80000m)));
   Test("UT06 normal remains configured",()=>Equal(20000m,Rules.Shipping(5000000m,"NORMAL",20000m)));
   Test("UT07 AMEX format accepted",()=>Rules.Card(Card("AMEX"),DateTime.Today));
   Test("UT08 short VISA rejected",()=>{var c=Card();c.Number="123";Throws(()=>Rules.Card(c,DateTime.Today));});
   Test("UT09 wrong AMEX CSV rejected",()=>{var c=Card("AMEX");c.Csv="123";Throws(()=>Rules.Card(c,DateTime.Today));});
   Test("UT10 expired card rejected",()=>{var c=Card();c.Expiry=DateTime.Today.AddMonths(-1);Throws(()=>Rules.Card(c,DateTime.Today));});
   Test("UT11 current expiry month valid",()=>{var c=Card();c.Expiry=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);Rules.Card(c,DateTime.Today);});
   Test("UT12 cart aggregates product",()=>{var c=new CartService();var p=new Product{Id="P",Name="P",Price=1,InStock=true};c.Add(p,2);c.Add(p,3);Equal(5,c.Lines[0].Quantity);});
   Test("UT13 cart rejects zero",()=>{var c=new CartService();Throws(()=>c.Add(new Product{InStock=true},0));});
   Test("UT14 cart rejects unavailable",()=>Throws(()=>new CartService().Add(new Product{InStock=false},1)));
   Test("UT15 cart remove",()=>{var c=new CartService();c.Add(new Product{Id="P",InStock=true},1);c.Remove("P");Equal(0,c.Lines.Count);});
   Test("UT16 empty checkout rejected",()=>{var f=new Fixture();f.Cart.Clear();Throws(()=>f.Quote());});
   Test("UT17 live product price quoted",()=>{var f=new Fixture();f.Products.Item.Price=600000;var q=f.Quote();Equal(1200000m,q.Goods);Equal(true,q.PriceChanged);});
   Test("UT18 live out-of-stock rejected",()=>{var f=new Fixture();f.Products.Item.InStock=false;Throws(()=>f.Quote());});
   Test("UT19 price changed after quote blocks authorize",()=>{var f=new Fixture();var q=f.Quote();f.Products.Item.Price=600000;Throws(()=>f.Service.Submit(Guid.NewGuid(),1,Recipient(),q,Card()));Equal(0,f.Payment.AuthorizeCalls);});
   Test("UT20 successful immutable order snapshot",()=>{var f=new Fixture();var o=f.Submit(Guid.NewGuid());Equal("COMPLETED",o.State);f.Products.Item.Price=9999999;Equal(1000000m,o.Order.Quote.Goods);Equal("Recipient Different",o.Order.Recipient.Name);});
   Test("UT21 declined creates no order",()=>{var f=new Fixture();f.Payment.State="DECLINED";Equal("DECLINED",f.Submit(Guid.NewGuid()).State);Equal(0,f.Repo.Orders.Count);});
   Test("UT22 timeout then reconcile exactly once",()=>{var f=new Fixture();f.Payment.Timeout=true;var id=Guid.NewGuid();Equal("UNKNOWN",f.Submit(id).State);Equal(0,f.Repo.Orders.Count);Equal("COMPLETED",f.Service.Resume(id,1).State);Equal(1,f.Payment.AuthorizeCalls);Equal(1,f.Repo.Orders.Count);});
   Test("UT23 repeated confirmation idempotent",()=>{var f=new Fixture();var id=Guid.NewGuid();int o=f.Submit(id).Order.Id;Equal(o,f.Submit(id).Order.Id);Equal(1,f.Payment.AuthorizeCalls);Equal(1,f.Repo.Orders.Count);});
   Test("UT24 mail failure keeps confirmed order",()=>{var f=new Fixture();f.Email.Fail=true;var o=f.Submit(Guid.NewGuid());Equal("COMPLETED",o.State);Equal("FAILED",o.Order.EmailState);Equal(1,f.Repo.Orders.Count);});
   Test("UT25 mail retry idempotent",()=>{var f=new Fixture();f.Email.Fail=true;var o=f.Submit(Guid.NewGuid()).Order;f.Email.Fail=false;f.Service.RetryEmail(o.Id,1);f.Service.RetryEmail(o.Id,1);Equal("SENT",f.Repo.Receipt(o.Id).EmailState);Equal(1,f.Email.Messages.Count);});
   Test("UT26 optional email skipped",()=>{var f=new Fixture();f.Repo.User.Email=null;Equal("SKIPPED",f.Submit(Guid.NewGuid()).Order.EmailState);Equal(0,f.Email.Messages.Count);});
   Test("UT27 email omits card data",()=>{var f=new Fixture();f.Submit(Guid.NewGuid());string body=f.Email.Messages.Values.First();if(body.Contains("4111111111111111")||body.Contains("CSV")||body.Contains("MOCK-TOKEN")||body.Contains("VISA"))throw new Exception("Card data leaked");});
   Test("UT28 pending ownership enforced",()=>{var f=new Fixture();f.Payment.Timeout=true;var id=Guid.NewGuid();f.Submit(id);Throws(()=>f.Service.Resume(id,2));});
   Test("UT29 persistence failure after authorization recoverable",()=>{var f=new Fixture();f.Repo.FailComplete=true;var id=Guid.NewGuid();Throws(()=>f.Submit(id));Equal(0,f.Repo.Orders.Count);f.Repo.FailComplete=false;Equal("COMPLETED",f.Service.Resume(id,1).State);Equal(1,f.Payment.AuthorizeCalls);});
   Test("UT30 fee applied on goods plus shipping",()=>Equal(10800m,Rules.CardFee(1000000m,80000m,new CardType{FeeRate=0.01m})));
   Test("UT31 login password hashed",()=>{var r=new FakeRepo();var s=new AccountService(r);var u=new Customer{FullName="Test",Username="newuser",BirthDate=new DateTime(2000,1,1),IdentityNo="TEST-ID",Address="Test",Phone="0901234567"};s.Register(new Registration{Customer=u,Password="Lab4@123"});Equal("newuser",s.Login("newuser","Lab4@123").Username);Throws(()=>s.Login("newuser","wrong"));});
   Test("UT32 invalid email rejected",()=>{var r=new FakeRepo();var s=new AccountService(r);Throws(()=>s.Register(new Registration{Customer=new Customer{FullName="Test",Username="user",BirthDate=DateTime.Today,IdentityNo="ID",Address="Address",Phone="0901234567",Email="bad email"},Password="Lab4@123"}));});
   Test("UT33 sensitive input cleared after authorize",()=>{var f=new Fixture();var c=Card();f.Service.Submit(Guid.NewGuid(),1,Recipient(),f.Quote(),c);Equal<string>(null,c.Number);Equal<string>(null,c.Csv);});
   Test("UT34 product unavailable after quote blocks authorize",()=>{var f=new Fixture();var q=f.Quote();f.Products.Item.InStock=false;Throws(()=>f.Service.Submit(Guid.NewGuid(),1,Recipient(),q,Card()));Equal(0,f.Payment.AuthorizeCalls);});
   Test("UT35 adapter ledger survives new instance",()=>{string folder=Path.Combine(Path.GetTempPath(),"Lab4Test-"+Guid.NewGuid().ToString("N"));try{var p=new MockPaymentAdapter(folder){Mode=PaymentMode.TimeoutThenApproved};var id=Guid.NewGuid();try{p.Authorize(id,100m,Card());}catch(TimeoutException){}var result=new MockPaymentAdapter(folder).Query(id,100m);Equal("AUTHORIZED",result.State);Equal("1111",result.Last4);}finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}});
   Test("UT36 duplicate username rejected",()=>{var r=new FakeRepo();var s=new AccountService(r);Func<Registration> input=()=>new Registration{Customer=new Customer{FullName="Test",Username="dup",BirthDate=DateTime.Today,IdentityNo="ID",Address="Address",Phone="0901234567"},Password="Lab4@123"};s.Register(input());Throws(()=>s.Register(input()));});
   log.Add("Total "+(log.Count)+"; Failed "+failed+"; Runtime "+Environment.Version);File.WriteAllLines(output,log,Encoding.UTF8);return failed==0?0:1;
  }
  class FakeProducts:IProductAdapter {
   public Product Item=new Product{Id="P1",Name="Product",CategoryId="C",Price=500000m,InStock=true};
   public List<Category> Categories(){return new List<Category>{new Category{Id="C",Name="Category"}};}public List<Product> List(string id){return new List<Product>{Item};}public Product Get(string id){return Item;}
  }
  class FakePayment:IPaymentAdapter {
   public string State="AUTHORIZED";public bool Timeout;public int AuthorizeCalls;
   PaymentResult Result(decimal amount){return new PaymentResult{State=State,Amount=amount,Reference="REF",Token="TOKEN",Last4="1111"};}
   public PaymentResult Authorize(Guid id,decimal amount,CardInput c){AuthorizeCalls++;if(Timeout)throw new TimeoutException();return Result(amount);}public PaymentResult Query(Guid id,decimal amount){return Result(amount);}
  }
  class FakeEmail:IEmailAdapter {public bool Fail;public Dictionary<Guid,string> Messages=new Dictionary<Guid,string>();public void Send(Guid id,string address,string subject,string body){if(Fail)throw new IOException("Mail unavailable");Messages[id]=body;}}
  class FakeRepo:IShopRepository {
   public Customer User=new Customer{Id=1,FullName="Buyer",Email="buyer@example.com"};public bool FailComplete;
   readonly Dictionary<Guid,CheckoutRequest> requests=new Dictionary<Guid,CheckoutRequest>();public Dictionary<int,OrderReceipt> Orders=new Dictionary<int,OrderReceipt>();
   readonly Dictionary<string,Tuple<Customer,byte[],byte[]>> accounts=new Dictionary<string,Tuple<Customer,byte[],byte[]>>(StringComparer.OrdinalIgnoreCase);
   public int Register(Customer u,byte[] h,byte[] s){if(accounts.ContainsKey(u.Username))throw new InvalidOperationException("Duplicate");u.Id=accounts.Count+1;accounts.Add(u.Username,Tuple.Create(u,h,s));return u.Id;}
   public Tuple<Customer,byte[],byte[]> FindLogin(string u){return accounts.ContainsKey(u)?accounts[u]:null;}public Customer GetCustomer(int id){return User;}
   public List<ShippingOption> ShippingOptions(){return new List<ShippingOption>{new ShippingOption{Id="NORMAL"},new ShippingOption{Id="EXPRESS"},new ShippingOption{Id="SAMEDAY"}};}
   public List<Region> Regions(){return new List<Region>{new Region{Id="INNER"}};}
   public List<CardType> CardTypes(){return new List<CardType>{new CardType{Id="VISA"},new CardType{Id="AMEX",FeeRate=0.015m},new CardType{Id="DISCOVER",FeeRate=0.01m}};}
   public decimal ShippingFee(string r,string s){return s=="EXPRESS"?40000:s=="SAMEDAY"?80000:20000;}
   public void SaveRequest(CheckoutRequest a){requests.Add(a.Id,a);}public CheckoutRequest GetRequest(Guid id){return requests.ContainsKey(id)?requests[id]:null;}
   public void SetPayment(Guid id,PaymentResult r){var a=requests[id];if(a.State=="COMPLETED"||a.State=="AUTHORIZED"||a.State=="DECLINED")return;if(r.Amount!=a.Quote.Total)throw new InvalidOperationException("Amount mismatch");a.State=r.State;}
   public int Complete(Guid id){var a=requests[id];if(a.OrderId.HasValue)return a.OrderId.Value;if(FailComplete)throw new InvalidOperationException("Simulated transaction error");if(a.State!="AUTHORIZED")throw new InvalidOperationException("Not authorized");int orderId=Orders.Count+1;Orders.Add(orderId,new OrderReceipt{Id=orderId,RequestId=id,Customer=new Customer{Id=User.Id,FullName=User.FullName,Email=User.Email},Recipient=a.Recipient,Quote=a.Quote,OrderedAt=DateTime.Now,EmailState=User.Email==null?"SKIPPED":"PENDING"});a.State="COMPLETED";a.OrderId=orderId;return orderId;}
   public OrderReceipt Receipt(int id){return Orders[id];}public void EmailStatus(int id,string s,string e){Orders[id].EmailState=s;}
  }
 }
}
