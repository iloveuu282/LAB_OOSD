# Kiểm tra bộ mã nguồn trước khi giao

- Hai project khai báo đầy đủ file C#, tham chiếu project và target v4.7.2.
- Kiểm tra cân bằng dấu ngoặc C# sau khi bỏ chuỗi và comment trên toàn bộ mã nguồn; đây không phải biên dịch.
- Đủ 9 Form; control được khai báo và các handler sự kiện liên kết có phương thức tương ứng.
- 20 khối mã C#, App.config và SQL giữ nguyên so với Word hoàn thiện.
- App.config đúng khóa kết nối; cấu hình test dùng _Test; các JSON VS Code hợp lệ.
- Bộ runner có đúng 32 test duy nhất, kiểm tra tên CSDL test và nạp fixture riêng trước mỗi ca.
- Script có 16 bảng; script tạo CSDL mặc định giữ dữ liệu khi đã có bảng, chỉ reset khi thêm -Reset.
- PowerShell lưu UTF-8 BOM để đọc thông báo tiếng Việt trên Windows PowerShell 5.1.

Chưa biên dịch MSBuild, chưa kết nối SQL Server và chưa chạy TC01–TC32 trong môi trường Windows. Những kiểm tra trên chỉ là kiểm tra tĩnh, không phải kết quả test nghiệp vụ hay xác nhận giao diện chạy đúng. Chạy theo README để ghi kết quả thực tế.
