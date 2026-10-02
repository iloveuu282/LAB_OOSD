USE EShopping_Lab4;
GO
SELECT Id,Username,FullName,Email FROM dbo.Customer; -- Không in hash/salt.
SELECT Id,CustomerId,RecipientName,ShippingId,Goods,Shipping,CardFee,Total,OrderedAt,State FROM dbo.[Order] ORDER BY Id DESC;
SELECT * FROM dbo.OrderLine ORDER BY OrderId DESC;
SELECT Id,State,Total,CreatedAt FROM dbo.CheckoutAttempt ORDER BY CreatedAt DESC;
SELECT AttemptId,State,Amount,ProviderReference,Last4 FROM dbo.PaymentRecord;
SELECT OrderId,MessageId,Recipient,State,Attempts,LastError FROM dbo.EmailOutbox;
-- Product data is in the EXTERNAL mock database.
SELECT Id,Name,Price,InStock FROM EShopping_ProductMock.dbo.Product;
GO
