using System;
using System.Windows.Forms;
namespace QuanLyCongTyDuLich
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => MessageBox.Show(e.Exception.Message,
                "Không thực hiện được", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Application.Run(new Forms.FrmMain());
        }
    }
}
