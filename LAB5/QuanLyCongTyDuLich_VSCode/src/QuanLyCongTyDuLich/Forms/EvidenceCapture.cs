using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace QuanLyCongTyDuLich.Forms
{
    internal static class EvidenceCapture
    {
        internal static void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.Control && e.Shift && e.KeyCode == Keys.S)) return;
            e.SuppressKeyPress = true;
            var form = sender as Form;
            if (form == null) return;
            try
            {
                string dir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "Evidence"));
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, form.Name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                using (var bmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }
                MessageBox.Show("Đã lưu ảnh Form: " + path);
            }
            catch (Exception ex) { MessageBox.Show("Không lưu được ảnh: " + ex.Message); }
        }
    }
}
