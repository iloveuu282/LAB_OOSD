USE EShopping_Lab4;
GO
-- Tất cả mức phí và giờ xử lý dưới đây là dữ liệu giả định phục vụ bài tập.
IF NOT EXISTS(SELECT 1 FROM dbo.ShippingMethod WHERE Id='NORMAL')
 INSERT dbo.ShippingMethod VALUES('NORMAL',N'Giao thường',48),('EXPRESS',N'Chuyển phát nhanh',24),('SAMEDAY',N'Nhanh trong ngày',8);
IF NOT EXISTS(SELECT 1 FROM dbo.Region WHERE Id='INNER')
 INSERT dbo.Region VALUES('INNER',N'Nội thành'),('OUTER',N'Ngoại thành'),('OTHER',N'Tỉnh khác');
IF NOT EXISTS(SELECT 1 FROM dbo.ShippingRate)
 INSERT dbo.ShippingRate VALUES('INNER','NORMAL',20000),('INNER','EXPRESS',40000),('INNER','SAMEDAY',80000),
 ('OUTER','NORMAL',30000),('OUTER','EXPRESS',60000),('OUTER','SAMEDAY',120000),
 ('OTHER','NORMAL',50000),('OTHER','EXPRESS',100000),('OTHER','SAMEDAY',200000);
IF NOT EXISTS(SELECT 1 FROM dbo.CardType)
 INSERT dbo.CardType VALUES('VISA',0,0),('MASTER',0,0),('DISCOVER',0.01,0),('AMEX',0.015,0);
-- Không tạo mật khẩu dạng rõ. Đăng ký tài khoản bằng ứng dụng.
GO
