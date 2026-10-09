using System.Data;
using System.Windows.Forms;
using QuanLyCongTyDuLich.Services;
 
namespace QuanLyCongTyDuLich.Forms
{
    /// <summary>Tiện ích dùng chung cho các Form.</summary>
    internal static class FormHelper
    {
        public static void Nap(ComboBox cbo, DataTable dt, string display, string value)
        {
            cbo.DisplayMember = display;
            cbo.ValueMember = value;
            cbo.DataSource = dt;
        }
 
        public static string Gia(ComboBox cbo)
        {
            return cbo.SelectedValue == null ? "" : cbo.SelectedValue.ToString();
        }
 
        public static string O(DataGridView dgv, string cot)
        {
            return dgv.CurrentRow == null ? "" : System.Convert.ToString(dgv.CurrentRow.Cells[cot].Value);
        }
 
        /// <summary>Hiển thị kết quả; trả về true nếu thành công.</summary>
        public static bool Bao(KetQuaXuLy k)
        {
            MessageBox.Show(k.ThongBao, k.ThanhCong ? "Thông báo" : "Không thực hiện được",
                MessageBoxButtons.OK, k.ThanhCong ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            return k.ThanhCong;
        }
    }
}