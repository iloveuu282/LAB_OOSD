using System;
using System.Windows.Forms;
using QuanLyKhachSan.Forms;
namespace QuanLyKhachSan {
 static class Program { [STAThread] static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm()); } }
}
