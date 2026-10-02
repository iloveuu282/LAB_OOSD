-- Chạy SAU 01–04. Positive integration tests, rollback mọi dữ liệu test.
-- Script này chưa được chạy trong môi trường tạo bài (không có SQL Server).
USE EShopping_Lab4;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRAN;
 DECLARE @username nvarchar(50)=N'SQLTEST_'+CONVERT(nvarchar(36),NEWID()),@customer int,@attempt uniqueidentifier=NEWID(),@order int;
 INSERT dbo.Customer(FullName,BirthDate,IdentityNo,Address,Phone,Username,PasswordHash,PasswordSalt,Email)
 VALUES(N'SQL Test','2000-01-01',N'TEST-ID',N'TEST ADDRESS','0901234567',@username,CONVERT(varbinary(32),REPLICATE('H',32)),CONVERT(varbinary(16),REPLICATE('S',16)),N'test@example.com');
 SET @customer=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.CheckoutAttempt(Id,CustomerId,RecipientName,RecipientAddress,RecipientPhone,RegionId,ShippingId,CardTypeId,Goods,Shipping,CardFee)
 VALUES(@attempt,@customer,N'Người nhận khác',N'TEST ADDRESS','0909876543','INNER','EXPRESS','VISA',1000000,0,0);
 INSERT dbo.CheckoutAttemptLine VALUES(@attempt,'SP001',N'Bàn phím tại lúc đặt',2,500000);
 EXEC dbo.RecordPayment @attempt,'AUTHORIZED',1000000,'TEST-REF','TEST-TOKEN','1111';
 EXEC dbo.CompleteCheckout @attempt;
 SELECT @order=Id FROM dbo.[Order] WHERE AttemptId=@attempt;
 IF @order IS NULL THROW 51901,N'IT01 Không tạo đơn.',1;
 IF (SELECT Total FROM dbo.[Order] WHERE Id=@order)<>1000000 THROW 51902,N'IT02 Sai tổng tiền.',1;
 IF (SELECT COUNT(*) FROM dbo.OrderLine WHERE OrderId=@order)<>1 THROW 51903,N'IT03 Sai số dòng.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.EmailOutbox WHERE OrderId=@order AND State='PENDING' AND MessageId=@attempt) THROW 51904,N'IT04 Thiếu outbox.',1;
 EXEC dbo.CompleteCheckout @attempt;
 IF (SELECT COUNT(*) FROM dbo.[Order] WHERE AttemptId=@attempt)<>1 THROW 51905,N'IT05 Đơn bị trùng.',1;
 UPDATE dbo.Customer SET FullName=N'Đổi tên' WHERE Id=@customer;
 IF (SELECT BuyerName FROM dbo.[Order] WHERE Id=@order)<>N'SQL Test' THROW 51906,N'IT06 Không giữ snapshot người mua.',1;
 -- Terminal payment cannot be overwritten by late UNKNOWN callback.
 EXEC dbo.RecordPayment @attempt,'UNKNOWN',1000000;
 IF (SELECT State FROM dbo.CheckoutAttempt WHERE Id=@attempt)<>'COMPLETED' THROW 51907,N'IT07 Trạng thái terminal bị ghi đè.',1;
 ROLLBACK;
 PRINT N'PASS IT01–IT07. Dữ liệu test đã rollback; identity có thể có khoảng trống.';
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO
