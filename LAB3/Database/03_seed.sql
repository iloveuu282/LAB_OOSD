USE QuanLyKhachSanLab3;
GO
INSERT KhuVuc VALUES('A',N'Khu A'),('B',N'Khu B');
INSERT NhanVien VALUES('NV01',N'Nguyễn Thu Hà',N'Lễ tân'),('NV02',N'Trần Minh An',N'Phục vụ'),('NV03',N'Lê Hoàng Nam',N'Thanh toán');
INSERT Phong(SoPhong,MaKhu,SucChua,DonGiaNgay) VALUES('A101','A',2,600000),('A102','A',3,800000),('B201','B',4,1200000);
INSERT LoaiTienNghi VALUES('TV',N'Ti vi'),('TL',N'Tủ lạnh');
INSERT TienNghi VALUES('TV01','TV',1,N'Tốt'),('TL01','TL',1,N'Tốt');
INSERT DichVu VALUES('DV01',N'Ăn sáng',N'Suất',120000),('DV02',N'Giặt đồ',N'Bộ',50000);
INSERT QuyDinhDenBu VALUES('TV',N'Hư nhẹ',500000),('TV',N'Mất',5000000),('TL',N'Hư nhẹ',400000),('TL',N'Mất',4000000);
INSERT KhachHang VALUES('K01',N'Khách mẫu', 'TEST-CCCD-001',N'Việt Nam',NULL);
GO
