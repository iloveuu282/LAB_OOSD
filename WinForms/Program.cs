using System;
using System.IO;
using System.Windows.Forms;

namespace EShopping
{
 static class Program {
  [STAThread] static int Main(string[] args) {
   if (Array.Exists(args, x => x == "--self-test")) {
    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "self-test-results.txt");
    return SelfTests.Run(path);
   }
   Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
   try {
    var repo = new SqlShopRepository(); var products = new SqlProductMockAdapter();
    var payment = new MockPaymentAdapter(); var email = new FileEmailAdapter();
    Application.Run(new MainForm(repo, products, payment, email)); return 0;
   } catch (Exception ex) { MessageBox.Show("Không khởi động được. Chạy script SQL và kiểm tra App.config.\n" + ex.Message, "e-SHOPPING"); return 1; }
  }
 }
}
