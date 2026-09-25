# UML LAB3 Quản lý khách sạn

Các khối Mermaid hiển thị ngay trên GitHub; để sửa bằng draw.io chọn Arrange → Insert → Advanced → Mermaid rồi dán nội dung từng khối. Quan hệ dùng khóa trong `Database/01_schema.sql`.

## Use Case tổng quát và phân rã

```mermaid
flowchart LR
  LT[Lễ tân] --> DP((Đặt phòng))
  LT --> NP((Nhận phòng))
  PV[Phục vụ] --> LD((Lắp đặt tiện nghi))
  PV --> DV((Ghi dịch vụ))
  PV --> DB((Lập đền bù))
  TT[Thanh toán] --> HD((Lập hóa đơn))
  TT --> TToan((Thanh toán và trả phòng))
  QL[Quản lý] --> TK((Thống kê))
```

```mermaid
flowchart TB
  DP[Đặt phòng] --> KH[Tra cứu khách]
  DP --> SC[Kiểm tra sức chứa]
  DP --> L[Kiểm tra giao lịch]
  DP --> P[Lưu phiếu và phòng]
  NP[Nhận phòng] --> NLT[Thêm người lưu trú]
  NP --> ST[Đổi trạng thái]
```

```mermaid
flowchart TB
  TN[Phòng và tiện nghi] --> P[Danh mục phòng]
  TN --> TB[Thiết bị]
  TN --> LD[Lắp đặt]
  TP[Trả phòng] --> DB[Đền bù]
  TP --> HD[Hóa đơn]
  TP --> TT[Thanh toán]
  TK[Thống kê] --> DT[Doanh thu]
  TK --> SD[Sử dụng dịch vụ]
```

## Biểu đồ lớp phân tích

```mermaid
classDiagram
  KhuVuc : +string MaKhu

  Phong : +string SoPhong 
  Phong : +int SucChua 
  Phong : +decimal DonGiaNgay

  DatPhong : +string MaDat 
  DatPhong : +date NgayNhan 
  DatPhong : +date NgayTra 
  DatPhong : +decimal TienCoc

  DatPhongCT : +string MaDat 
  DatPhongCT : +string SoPhong 
  DatPhongCT : +int SoNguoi

  KhachHang : +string MaKhach 
  KhachHang : +string CCCD

  NguoiLuuTru : +int MaNguoi 
  NguoiLuuTru : +string CCCD

  LoaiTienNghi : +string MaLoai

  TienNghi : +string MaTN 
  TienNghi : +int SoThuTu

  LapDat : +string MaLapDat 
  LapDat : +date Ngay

  DichVu : +string MaDV 
  DichVu : +decimal DonGia

  SuDungDV : +string MaDat 
  SuDungDV : +string SoPhong 
  SuDungDV : +date Ngay 
  SuDungDV : +string MaDV 
  SuDungDV : +int SoLuong

  DenBu : +string MaDenBu

  DenBuCT : +string MaDenBu 
  DenBuCT : +string MaTN 
  DenBuCT : +decimal SoTien

  HoaDon : +string MaHD 
  HoaDon : +decimal TongPhaiTra

  ThanhToan : +string MaTT 
  ThanhToan : +decimal SoTien

  KhuVuc "1" --> "0..*" Phong
  Phong "1" --> "0..*" DatPhongCT
  DatPhong "1" *-- "1..*" DatPhongCT
  KhachHang "1" --> "0..*" DatPhong
  DatPhongCT "1" *-- "0..*" NguoiLuuTru
  LoaiTienNghi "1" --> "0..*" TienNghi
  TienNghi "1" --> "0..*" LapDat
  Phong "1" --> "0..*" LapDat
  DatPhongCT "1" --> "0..*" SuDungDV
  DichVu "1" --> "0..*" SuDungDV
  DatPhongCT "1" --> "0..*" DenBu
  DenBu "1" *-- "1..*" DenBuCT
  DatPhong "1" --> "0..1" HoaDon
  HoaDon "1" *-- "0..*" ThanhToan
```

## Trạng thái phiếu đặt và hóa đơn

```mermaid
stateDiagram-v2
  [*] --> DaDat
  DaDat --> DangO: Nhận phòng
  DaDat --> Huy: Hủy
  DaDat --> NoShow: Xác nhận vắng mặt
  DangO --> DaTra: Thanh toán đủ và trả phòng
```

```mermaid
stateDiagram-v2
  [*] --> ChuaThanhToan: Lập hóa đơn
  ChuaThanhToan --> ChuaThanhToan: Trả một phần
  ChuaThanhToan --> DaThanhToan: Đã đủ tiền
```

## Tuần tự nghiệp vụ

```mermaid
sequenceDiagram
  actor LT as Lễ tân
  participant F as BookingForm
  participant S as sp_DatPhong
  participant DB as SQL Server
  LT->>F: Ghi khách phòng số người ngày
  F->>S: Lập phiếu
  S->>DB: Khóa phòng và kiểm tra sức chứa lịch
  alt Hợp lệ
    S->>DB: Lưu phiếu và chi tiết
    S-->>F: Thành công
  else Không hợp lệ
    S-->>F: Từ chối và rollback
  end
```

```mermaid
sequenceDiagram
  actor LT as Lễ tân
  participant F as BookingForm
  participant S as sp_NhanPhong
  participant DB as SQL Server
  LT->>F: Thêm người ở rồi nhận phòng
  F->>S: Mã phiếu
  S->>DB: Kiểm tra phiếu và người lưu trú
  S->>DB: Chuyển trạng thái Đang ở
  S-->>F: Kết quả
```

```mermaid
sequenceDiagram
  actor PV as Phục vụ
  participant F as EquipmentForm
  participant S as sp_LapDat
  participant DB as SQL Server
  PV->>F: Thiết bị phòng ngày
  F->>S: Lập phiếu
  S->>DB: Kiểm tra thiết bị trong ngày
  S->>DB: Lưu phiếu hoặc rollback
```

```mermaid
sequenceDiagram
  actor NV as Nhân viên
  participant F as ServiceForm
  participant S as sp_GhiDichVu
  participant DB as SQL Server
  NV->>F: Dịch vụ số lượng ngày
  F->>S: Ghi dịch vụ
  S->>DB: Kiểm tra phiếu đang ở
  S->>DB: Tìm theo phiếu phòng ngày dịch vụ
  S->>DB: Cộng số lượng hoặc tạo dòng
```

```mermaid
sequenceDiagram
  actor KT as Thanh toán
  participant F as CheckoutForm
  participant S as SQL procedures
  participant DB as SQL Server
  KT->>F: Lập đền bù và hóa đơn
  F->>S: sp_DenBu và sp_LapHoaDon
  S->>DB: Tính phòng dịch vụ đền bù trừ cọc
  KT->>F: Ghi một hoặc nhiều lần thanh toán
  F->>S: sp_ThanhToan
  KT->>F: Trả phòng
  F->>S: sp_TraPhong
  S->>DB: Kiểm tra dư nợ bằng 0 rồi đóng phiếu
```

```mermaid
sequenceDiagram
  actor QL as Quản lý
  participant F as StatsForm
  participant DB as SQL Server
  QL->>F: Chọn khoảng ngày
  F->>DB: Truy vấn tổng hợp vHoaDon
  DB-->>F: Doanh thu và đã thu từng ngày
```

## Lớp chi tiết module

```mermaid
classDiagram
  Db : +Query(sql, params) DataTable 
  Db : +Run(proc, params) void

  BookingForm : +DatPhong() 
  BookingForm : +ThemNguoi() 
  BookingForm : +NhanPhong()

  CheckoutForm : +DenBu() 
  CheckoutForm : +LapHoaDon() 
  CheckoutForm : +ThanhToan() 
  CheckoutForm : +TraPhong()

  ServiceForm : +GhiDichVu()

  StatsForm : +ThongKe()

  BookingForm --> Db
  CheckoutForm --> Db
  ServiceForm --> Db
  StatsForm --> Db
```

## Hoạt động đặt phòng

```mermaid
flowchart TD
  A[Bắt đầu] --> B[Nhập thông tin phiếu]
  B --> C{Ngày và số người hợp lệ?}
  C -- Không --> X[Thông báo lỗi]
  C -- Có --> D{Phòng đủ chỗ và không bảo trì?}
  D -- Không --> X
  D -- Có --> E{Giao lịch phiếu đang hiệu lực?}
  E -- Có --> X
  E -- Không --> F[Lưu phiếu và chi tiết trong transaction]
  F --> G[Hiển thị phiếu đặt]
```
