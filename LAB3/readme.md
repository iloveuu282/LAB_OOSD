 Họ tên: Mai Việt Hoàng
 MSSV: 1250080058
Bài Lab: Bài 3 - Hệ thống quản lý khách sạn
Môi trường: Windows 10/11; VS Code; .NET 10 SDK; SQL Server Express; Microsoft MSSQL extension; ADO.NET (`Microsoft.Data.SqlClient`).
-rạng thái: Đã chuẩn bị mã nguồn, script, sơ đồ và mẫu báo cáo;

`Database/01_schema.sql`: 17 bảng và view hóa đơn; chạy trên database mới một lần.
`Database/02_procedures.sql`: nghiệp vụ có transaction và kiểm tra trùng lịch, sức chứa, dịch vụ, thanh toán.
`Database/03_seed.sql`: dữ liệu minh họa; chạy một lần.
`QuanLyKhachSan/`: project WinForms, các Form và lớp truy cập SQL.
`UML/diagrams.md`: Use Case, lớp, trạng thái, tuần tự, hoạt động; Mermaid có thể nhập vào draw.io.
`Docs/`: workbook Use Case, hướng dẫn, bảng kiểm thử, biểu mẫu; `Bao_cao_LAB3.docx` là bản Word cần hoàn thiện bằng ảnh thật.
`Evidence/`: chỉ thêm ảnh do chính bạn chụp trên máy mình, xóa thông tin riêng trước khi commit.
1. Cài VS Code, .NET 10 SDK (không chỉ Runtime), SQL Server Express và các extension VS Code C# Dev Kit, C#, SQL Server (mssql). Kiểm tra terminal PowerShell: `dotnet --version`. WinForms chỉ chạy trên Windows.
2. Mở VS Code → File → Open Folder → chọn thư mục LAB3. Không chọn mỗi file `.cs`. Trong extension MSSQL tạo connection: Server `localhost\SQLEXPRESS`, Authentication `Windows`, Database `master`, bật Trust server certificate nếu máy dùng chứng chỉ tự ký. Nếu SQL Server cài dạng default instance, Server là `localhost`.
3. Mở lần lượt `Database/01_schema.sql`, `02_procedures.sql`, `03_seed.sql`. Với từng file, chọn kết nối rồi bấm Run Query; đợi file chạy xong trước khi mở file tiếp theo. `01_schema.sql` và `03_seed.sql` chỉ chạy một lần trên database mới. Xác nhận `SELECT COUNT(*) AS SoPhong FROM QuanLyKhachSanLab3.dbo.Phong;` trả về 3.
4. Mở terminal trong LAB3, chạy:

   ```powershell
   dotnet restore .\QuanLyKhachSan\QuanLyKhachSan.csproj
   dotnet build .\QuanLyKhachSan\QuanLyKhachSan.csproj
   dotnet run --project .\QuanLyKhachSan\QuanLyKhachSan.csproj
   ```
5. `Data/Db.cs` mặc định kết nối `localhost\SQLEXPRESS` bằng tài khoản Windows. Nếu dùng instance khác, đổi `Db.Cs` hoặc đặt biến môi trường PowerShell cho terminal hiện tại: `$env:HOTEL_DB_CONNECTION = 'Server=localhost;Database=QuanLyKhachSanLab3;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'`. Với LocalDB dùng `Server=(localdb)\MSSQLLocalDB` thay `localhost`.
6. Trong ứng dụng thao tác: lắp thiết bị → đặt phòng → thêm ít nhất một người ở mỗi phòng → nhận phòng → ghi dịch vụ → đền bù nếu có → hóa đơn → thanh toán đủ → trả phòng → thống kê. Các mã `K01`, `NV01`–`NV03`, `A101`, `TV01`, `DV01` đã có từ seed. Dùng mã phiếu mới cho lần chạy tiếp theo.
7. Chụp chính cửa sổ Form, Print Preview và kết quả MSSQL extension trên máy bạn; điền báo cáo Word và bảng kiểm thử. Các Form dựng trực tiếp bằng mã C#, chỉnh bố cục trong VS Code; VS Code không cung cấp WinForms Designer kéo thả.

| Form | Mở từ màn hình chính | Nhập và thao tác | Bảng/procedure |
| --- | --- | --- | --- |
| CatalogForm | Danh mục và phòng | Thêm phòng/khách/dịch vụ; mã là duy nhất | `Phong`, `KhachHang`, `DichVu` |
| EquipmentForm | Tiện nghi và lắp đặt | Mã phiếu, thiết bị, phòng, ngày, NV → Lập phiếu | `sp_LapDat` |
| BookingForm | Đặt và nhận phòng | Khách, NV, ngày, phòng, sức chứa, cọc → Đặt; thêm từng người → Nhận; In phiếu | `sp_DatPhong`, `sp_ThemNguoi`, `sp_NhanPhong` |
| ServiceForm | Dịch vụ | Mã đặt, phòng, ngày, dịch vụ, số lượng → Ghi | `sp_GhiDichVu` |
| CheckoutForm | Đền bù, hóa đơn, thanh toán | Nếu hư: phiếu, thiết bị, mức độ → Lập và In đền bù; nhập ngày tính tiền → Lập HĐ; nhiều lần ghi thanh toán → In HĐ → Trả | `sp_DenBu`, `sp_LapHoaDon`, `sp_ThanhToan`, `sp_TraPhong` |
| StatsForm | Thống kê | Từ ngày, đến ngày → Thống kê | `vHoaDon` |

Chi tiết từng file và thao tác viết code trong VS Code: `Docs/huong_dan_code_tung_form.md`. Mọi truy vấn nhận dữ liệu từ giao diện đều dùng tham số SQL.
- Khoảng đặt phòng `[Ngày nhận, Ngày trả)` nên trả ngày 3 và nhận ngày 3 không giao nhau.
- Khóa phòng trong transaction rồi kiểm tra trùng lịch giúp hai thao tác đặt cùng phòng không qua kiểm tra đồng thời.
- `UNIQUE(MaTN, Ngay)` chặn 2 phiếu lắp đặt một thiết bị trong một ngày. Vị trí hiện tại là phiếu mới nhất đến ngày kiểm tra.
- Dịch vụ cùng phiếu, phòng, ngày, mã được cộng `SoLuong`, giữ đơn giá tại lần ghi đầu tiên.
- Tiền phòng lấy đơn giá khi đặt nhân số ngày do nhân viên xác nhận; tổng phải trả = max(0, tiền phòng + dịch vụ + đền bù - cọc). Nếu cọc vượt chi phí, `TienCocHoan` cho biết khoản cần hoàn thủ công. Chưa có nghiệp vụ ghi nhận hoàn cọc.
- Một phiếu đặt có thể chứa nhiều phòng ở tầng dữ liệu, nhưng Form minh họa đặt **mỗi lần một phòng**; muốn một phiếu nhiều phòng phải mở rộng UI và nghiệp vụ cùng transaction.
- Nhập khách và nhân viên danh mục cần phân quyền trong hệ thống thật; bài lab chưa xây đăng nhập/phân quyền.
- Hủy/no-show được mô tả UML theo tài liệu mẫu nhưng bản mã này chưa có nút xử lý; không báo đã hoàn thành hai luồng đó.
Theo `Docs/kiem_thu.md`. Sau khi chạy, ghi Pass/Fail, phiên bản SQL và ảnh ở `Evidence/`; nếu lỗi, ghi cách khắc phục. Hiện chưa có kết quả chạy thực tế vì mã được soạn trong môi trường không có Windows/SQL Server.