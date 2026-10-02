-- SQL Server 2016 SP1+ / Express / LocalDB. Không xóa database hoặc dữ liệu cũ.
USE master;
GO
IF DB_ID(N'EShopping_Lab4') IS NULL CREATE DATABASE EShopping_Lab4;
GO
USE EShopping_Lab4;
GO
IF OBJECT_ID('dbo.Customer','U') IS NULL
CREATE TABLE dbo.Customer(
 Id int IDENTITY PRIMARY KEY, FullName nvarchar(120) NOT NULL, BirthDate date NOT NULL,
 IdentityNo nvarchar(40) NOT NULL, Address nvarchar(300) NOT NULL, Phone varchar(20) NOT NULL,
 Username nvarchar(50) NOT NULL UNIQUE, PasswordHash varbinary(32) NOT NULL, PasswordSalt varbinary(16) NOT NULL,
 Email nvarchar(254) NULL, CreatedAt datetime2 NOT NULL DEFAULT SYSDATETIME());
GO
IF OBJECT_ID('dbo.ShippingMethod','U') IS NULL
CREATE TABLE dbo.ShippingMethod(Id varchar(10) PRIMARY KEY, Name nvarchar(80) NOT NULL,
 ProcessingHours int NOT NULL CHECK(ProcessingHours>0));
IF OBJECT_ID('dbo.Region','U') IS NULL
CREATE TABLE dbo.Region(Id varchar(10) PRIMARY KEY, Name nvarchar(80) NOT NULL);
IF OBJECT_ID('dbo.ShippingRate','U') IS NULL
CREATE TABLE dbo.ShippingRate(RegionId varchar(10) NOT NULL REFERENCES dbo.Region(Id),
 ShippingId varchar(10) NOT NULL REFERENCES dbo.ShippingMethod(Id), Fee decimal(18,2) NOT NULL CHECK(Fee>=0),
 PRIMARY KEY(RegionId,ShippingId));
IF OBJECT_ID('dbo.CardType','U') IS NULL
CREATE TABLE dbo.CardType(Id varchar(20) PRIMARY KEY, FeeRate decimal(9,6) NOT NULL CHECK(FeeRate BETWEEN 0 AND 1),
 FixedFee decimal(18,2) NOT NULL CHECK(FixedFee>=0));
GO
IF OBJECT_ID('dbo.CheckoutAttempt','U') IS NULL
CREATE TABLE dbo.CheckoutAttempt(
 Id uniqueidentifier PRIMARY KEY, CustomerId int NOT NULL REFERENCES dbo.Customer(Id),
 RecipientName nvarchar(120) NOT NULL, RecipientAddress nvarchar(300) NOT NULL, RecipientPhone varchar(20) NOT NULL,
 RegionId varchar(10) NOT NULL REFERENCES dbo.Region(Id), ShippingId varchar(10) NOT NULL REFERENCES dbo.ShippingMethod(Id),
 CardTypeId varchar(20) NOT NULL REFERENCES dbo.CardType(Id),
 Goods decimal(18,2) NOT NULL CHECK(Goods>0), Shipping decimal(18,2) NOT NULL CHECK(Shipping>=0),
 CardFee decimal(18,2) NOT NULL CHECK(CardFee>=0), Total AS CONVERT(decimal(18,2),Goods+Shipping+CardFee) PERSISTED,
 State varchar(20) NOT NULL DEFAULT 'CREATED' CHECK(State IN('CREATED','AUTHORIZED','DECLINED','UNKNOWN','COMPLETED')),
 CreatedAt datetime2 NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
 CONSTRAINT FK_Attempt_Rate FOREIGN KEY(RegionId,ShippingId) REFERENCES dbo.ShippingRate(RegionId,ShippingId));
IF OBJECT_ID('dbo.CheckoutAttemptLine','U') IS NULL
CREATE TABLE dbo.CheckoutAttemptLine(
 AttemptId uniqueidentifier NOT NULL REFERENCES dbo.CheckoutAttempt(Id), ProductId varchar(20) NOT NULL,
 ProductName nvarchar(150) NOT NULL, Quantity int NOT NULL CHECK(Quantity BETWEEN 1 AND 999),
 UnitPrice decimal(18,2) NOT NULL CHECK(UnitPrice>=0), PRIMARY KEY(AttemptId,ProductId));
IF OBJECT_ID('dbo.PaymentRecord','U') IS NULL
CREATE TABLE dbo.PaymentRecord(
 AttemptId uniqueidentifier PRIMARY KEY REFERENCES dbo.CheckoutAttempt(Id), State varchar(20) NOT NULL
 CHECK(State IN('AUTHORIZED','DECLINED','UNKNOWN')), Amount decimal(18,2) NOT NULL CHECK(Amount>0),
 ProviderReference varchar(100) NULL, PaymentToken varchar(120) NULL, Last4 varchar(4) NULL,
 UpdatedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
 CONSTRAINT CK_Payment_Last4 CHECK(Last4 IS NULL OR (LEN(Last4)=4 AND Last4 NOT LIKE '%[^0-9]%')),
 CONSTRAINT CK_Payment_Authorized CHECK(State<>'AUTHORIZED' OR (ProviderReference IS NOT NULL AND PaymentToken IS NOT NULL AND Last4 IS NOT NULL)));
GO
IF OBJECT_ID('dbo.[Order]','U') IS NULL
CREATE TABLE dbo.[Order](
 Id int IDENTITY PRIMARY KEY, AttemptId uniqueidentifier NOT NULL UNIQUE REFERENCES dbo.CheckoutAttempt(Id),
 CustomerId int NOT NULL REFERENCES dbo.Customer(Id), BuyerName nvarchar(120) NOT NULL, BuyerEmail nvarchar(254) NULL,
 RecipientName nvarchar(120) NOT NULL, RecipientAddress nvarchar(300) NOT NULL, RecipientPhone varchar(20) NOT NULL,
 RegionId varchar(10) NOT NULL REFERENCES dbo.Region(Id), ShippingId varchar(10) NOT NULL REFERENCES dbo.ShippingMethod(Id),
 CardTypeId varchar(20) NOT NULL REFERENCES dbo.CardType(Id),
 Goods decimal(18,2) NOT NULL CHECK(Goods>0), Shipping decimal(18,2) NOT NULL CHECK(Shipping>=0), CardFee decimal(18,2) NOT NULL CHECK(CardFee>=0),
 Total AS CONVERT(decimal(18,2),Goods+Shipping+CardFee) PERSISTED, OrderedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
 State varchar(20) NOT NULL DEFAULT 'CONFIRMED' CHECK(State='CONFIRMED'));
IF OBJECT_ID('dbo.OrderLine','U') IS NULL
CREATE TABLE dbo.OrderLine(OrderId int NOT NULL REFERENCES dbo.[Order](Id), ProductId varchar(20) NOT NULL,
 ProductName nvarchar(150) NOT NULL, Quantity int NOT NULL CHECK(Quantity BETWEEN 1 AND 999),
 UnitPrice decimal(18,2) NOT NULL CHECK(UnitPrice>=0), PRIMARY KEY(OrderId,ProductId));
IF OBJECT_ID('dbo.EmailOutbox','U') IS NULL
CREATE TABLE dbo.EmailOutbox(OrderId int PRIMARY KEY REFERENCES dbo.[Order](Id), MessageId uniqueidentifier NOT NULL UNIQUE,
 Recipient nvarchar(254) NOT NULL, State varchar(10) NOT NULL DEFAULT 'PENDING' CHECK(State IN('PENDING','SENT','FAILED')),
 Attempts int NOT NULL DEFAULT 0 CHECK(Attempts>=0), LastError nvarchar(300) NULL, UpdatedAt datetime2 NOT NULL DEFAULT SYSDATETIME());
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Order_Customer_Date' AND object_id=OBJECT_ID('dbo.[Order]'))
 CREATE INDEX IX_Order_Customer_Date ON dbo.[Order](CustomerId,OrderedAt DESC);
GO
