USE master;
GO
IF DB_ID(N'EShopping_ProductMock') IS NULL CREATE DATABASE EShopping_ProductMock;
GO
USE EShopping_ProductMock;
GO
IF OBJECT_ID('dbo.Category','U') IS NULL CREATE TABLE dbo.Category(Id varchar(20) PRIMARY KEY,Name nvarchar(100) NOT NULL);
IF OBJECT_ID('dbo.Product','U') IS NULL CREATE TABLE dbo.Product(Id varchar(20) PRIMARY KEY,CategoryId varchar(20) NOT NULL REFERENCES dbo.Category(Id),
 Name nvarchar(150) NOT NULL,Manufacturer nvarchar(100) NOT NULL,Description nvarchar(1000) NOT NULL,
 Specifications nvarchar(1000) NOT NULL,Price decimal(18,2) NOT NULL CHECK(Price>=0),InStock bit NOT NULL);
IF OBJECT_ID('dbo.ProductImage','U') IS NULL CREATE TABLE dbo.ProductImage(ProductId varchar(20) NOT NULL REFERENCES dbo.Product(Id),
 Ordinal int NOT NULL CHECK(Ordinal>0),Path nvarchar(300) NOT NULL,PRIMARY KEY(ProductId,Ordinal));
GO
IF NOT EXISTS(SELECT 1 FROM dbo.Category)
 INSERT dbo.Category VALUES('COMPUTER',N'Thiết bị máy tính'),('CAMERA',N'Máy ảnh'),('TOY',N'Đồ chơi'),('HOME',N'Điện gia dụng');
IF NOT EXISTS(SELECT 1 FROM dbo.Product)
 INSERT dbo.Product VALUES
 ('SP001','COMPUTER',N'Bàn phím ABC',N'ABC',N'Bàn phím cho học tập và làm việc.',N'USB; 104 phím; màu đen',500000,1),
 ('SP002','CAMERA',N'Máy ảnh ABC',N'ABC',N'Máy ảnh mẫu phục vụ demo.',N'24 MP; màn hình 3 inch',5000000,1),
 ('SP003','TOY',N'Bộ lắp ghép',N'ABC Toys',N'Đồ chơi lắp ghép mẫu.',N'200 mảnh; độ tuổi 6+',250000,1),
 ('SP004','HOME',N'Máy xay ABC',N'ABC Home',N'Sản phẩm đang hết hàng để kiểm thử.',N'500 W; 1.5 lít',1000000,0),
 ('SP005','COMPUTER',N'Chuột ABC',N'ABC',N'Chuột có dây.',N'USB; 1200 DPI',100000,1);
IF NOT EXISTS(SELECT 1 FROM dbo.ProductImage)
 INSERT dbo.ProductImage SELECT Id,1,N'Images/'+Id+N'_1.png' FROM dbo.Product;
IF NOT EXISTS(SELECT 1 FROM dbo.ProductImage WHERE Ordinal=2)
 INSERT dbo.ProductImage SELECT Id,2,N'Images/'+Id+N'_2.png' FROM dbo.Product;
GO
