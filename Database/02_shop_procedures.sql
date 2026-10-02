USE EShopping_Lab4;
GO
CREATE OR ALTER PROCEDURE dbo.CompleteCheckout @AttemptId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRANSACTION;
  DECLARE @state varchar(20), @goods decimal(18,2), @total decimal(18,2), @id int;
  SELECT @state=State,@goods=Goods,@total=Total FROM dbo.CheckoutAttempt WITH(UPDLOCK,HOLDLOCK) WHERE Id=@AttemptId;
  IF @state IS NULL THROW 51001,N'Không tìm thấy yêu cầu đặt hàng.',1;
  SELECT @id=Id FROM dbo.[Order] WHERE AttemptId=@AttemptId;
  IF @id IS NOT NULL BEGIN COMMIT; SELECT @id AS OrderId; RETURN; END;
  IF @state<>'AUTHORIZED' THROW 51002,N'Chưa được dịch vụ thanh toán chấp thuận.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.PaymentRecord WHERE AttemptId=@AttemptId AND State='AUTHORIZED' AND Amount=@total)
   THROW 51003,N'Kết quả kiểm tra thanh toán không khớp tổng tiền.',1;
  IF @goods<>COALESCE((SELECT SUM(Quantity*UnitPrice) FROM dbo.CheckoutAttemptLine WHERE AttemptId=@AttemptId),0)
   THROW 51004,N'Tổng chi tiết không khớp tiền hàng.',1;
  INSERT dbo.[Order](AttemptId,CustomerId,BuyerName,BuyerEmail,RecipientName,RecipientAddress,RecipientPhone,
   RegionId,ShippingId,CardTypeId,Goods,Shipping,CardFee)
  SELECT a.Id,a.CustomerId,c.FullName,c.Email,a.RecipientName,a.RecipientAddress,a.RecipientPhone,
   a.RegionId,a.ShippingId,a.CardTypeId,a.Goods,a.Shipping,a.CardFee
  FROM dbo.CheckoutAttempt a JOIN dbo.Customer c ON c.Id=a.CustomerId WHERE a.Id=@AttemptId;
  SET @id=CONVERT(int,SCOPE_IDENTITY());
  INSERT dbo.OrderLine(OrderId,ProductId,ProductName,Quantity,UnitPrice)
   SELECT @id,ProductId,ProductName,Quantity,UnitPrice FROM dbo.CheckoutAttemptLine WHERE AttemptId=@AttemptId;
  INSERT dbo.EmailOutbox(OrderId,MessageId,Recipient)
   SELECT Id,AttemptId,BuyerEmail FROM dbo.[Order] WHERE Id=@id AND NULLIF(LTRIM(RTRIM(BuyerEmail)),N'') IS NOT NULL;
  UPDATE dbo.CheckoutAttempt SET State='COMPLETED',UpdatedAt=SYSDATETIME() WHERE Id=@AttemptId;
  COMMIT;
  SELECT @id AS OrderId;
 END TRY
 BEGIN CATCH
  IF XACT_STATE()<>0 ROLLBACK;
  THROW;
 END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.RecordPayment @AttemptId uniqueidentifier,@State varchar(20),@Amount decimal(18,2),
 @Reference varchar(100)=NULL,@Token varchar(120)=NULL,@Last4 varchar(4)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @old varchar(20),@total decimal(18,2);
  SELECT @old=State,@total=Total FROM dbo.CheckoutAttempt WITH(UPDLOCK,HOLDLOCK) WHERE Id=@AttemptId;
  IF @old IS NULL OR @Amount<>@total THROW 51005,N'Yêu cầu hoặc số tiền không hợp lệ.',1;
  IF @State NOT IN('AUTHORIZED','DECLINED','UNKNOWN') THROW 51006,N'Trạng thái không hợp lệ.',1;
  IF @old IN('COMPLETED','AUTHORIZED','DECLINED') BEGIN COMMIT; RETURN; END;
  IF EXISTS(SELECT 1 FROM dbo.PaymentRecord WHERE AttemptId=@AttemptId)
   UPDATE dbo.PaymentRecord SET State=@State,Amount=@Amount,ProviderReference=@Reference,PaymentToken=@Token,Last4=@Last4,UpdatedAt=SYSDATETIME() WHERE AttemptId=@AttemptId;
  ELSE INSERT dbo.PaymentRecord(AttemptId,State,Amount,ProviderReference,PaymentToken,Last4)
   VALUES(@AttemptId,@State,@Amount,@Reference,@Token,@Last4);
  UPDATE dbo.CheckoutAttempt SET State=@State,UpdatedAt=SYSDATETIME() WHERE Id=@AttemptId;
  COMMIT;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH
END;
GO
