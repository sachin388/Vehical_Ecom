-- ==================================================================================
-- Service: Payment Service
-- Target Database: PaymentDb
-- Description: DDL & realistic seed data generation (1,000 records per table)
-- Engine: Microsoft SQL Server (Transact-SQL)
-- ==================================================================================

SET NOCOUNT ON;
GO

-- 1. Create Database if not exists
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'PaymentDb')
BEGIN
    CREATE DATABASE [PaymentDb];
END
GO

USE [PaymentDb];
GO

-- 2. Drop existing foreign keys & tables if recreating
IF OBJECT_ID(N'[dbo].[FK_PaymentTransactions_Payments_PaymentId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[PaymentTransactions] DROP CONSTRAINT [FK_PaymentTransactions_Payments_PaymentId];
IF OBJECT_ID(N'[dbo].[FK_PaymentRefunds_Payments_PaymentId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[PaymentRefunds] DROP CONSTRAINT [FK_PaymentRefunds_Payments_PaymentId];
GO

IF OBJECT_ID(N'[dbo].[PaymentRefunds]', 'U') IS NOT NULL DROP TABLE [dbo].[PaymentRefunds];
IF OBJECT_ID(N'[dbo].[PaymentTransactions]', 'U') IS NOT NULL DROP TABLE [dbo].[PaymentTransactions];
IF OBJECT_ID(N'[dbo].[Payments]', 'U') IS NOT NULL DROP TABLE [dbo].[Payments];
GO

-- 3. Create Tables
CREATE TABLE [dbo].[Payments] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [PaymentReference] NVARCHAR(100) NOT NULL,
    [SalesOrderId] UNIQUEIDENTIFIER NOT NULL,
    [CustomerId] NVARCHAR(100) NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [Currency] NVARCHAR(10) NOT NULL CONSTRAINT [DF_Payments_Currency] DEFAULT ('INR'),
    [Status] NVARCHAR(50) NOT NULL, -- Captured, Authorized, Initiated, Refunded
    [PaymentMethod] NVARCHAR(50) NOT NULL, -- CreditCard, NetBanking, UPI, BankTransfer, WireTransfer
    [IdempotencyKey] NVARCHAR(150) NOT NULL,
    [GatewayTransactionId] NVARCHAR(150) NULL,
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Payments_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Payments_PaymentReference] ON [dbo].[Payments] ([PaymentReference] ASC);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Payments_IdempotencyKey] ON [dbo].[Payments] ([IdempotencyKey] ASC);
CREATE NONCLUSTERED INDEX [IX_Payments_SalesOrderId] ON [dbo].[Payments] ([SalesOrderId] ASC);
GO

CREATE TABLE [dbo].[PaymentTransactions] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [PaymentId] BIGINT NOT NULL,
    [TransactionType] NVARCHAR(50) NOT NULL, -- Authorization, Capture, Refund, Reversal
    [Amount] DECIMAL(18,2) NOT NULL,
    [Currency] NVARCHAR(10) NOT NULL CONSTRAINT [DF_PaymentTransactions_Currency] DEFAULT ('INR'),
    [Status] NVARCHAR(50) NOT NULL, -- Success, Failed, Pending
    [GatewayResponseCode] NVARCHAR(50) NULL,
    [GatewayMessage] NVARCHAR(500) NULL,
    [ProcessedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_PaymentTransactions_ProcessedAtUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PaymentTransactions_Payments_PaymentId] FOREIGN KEY ([PaymentId]) 
        REFERENCES [dbo].[Payments] ([Id]) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX [IX_PaymentTransactions_PaymentId] ON [dbo].[PaymentTransactions] ([PaymentId] ASC);
GO

CREATE TABLE [dbo].[PaymentRefunds] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [PaymentId] BIGINT NOT NULL,
    [SalesOrderPublicId] UNIQUEIDENTIFIER NOT NULL,
    [RefundReference] NVARCHAR(100) NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [Reason] NVARCHAR(500) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL, -- Requested, Completed, Rejected
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_PaymentRefunds_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CompletedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_PaymentRefunds] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PaymentRefunds_Payments_PaymentId] FOREIGN KEY ([PaymentId]) 
        REFERENCES [dbo].[Payments] ([Id])
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_PaymentRefunds_RefundReference] ON [dbo].[PaymentRefunds] ([RefundReference] ASC);
CREATE NONCLUSTERED INDEX [IX_PaymentRefunds_SalesOrderPublicId] ON [dbo].[PaymentRefunds] ([SalesOrderPublicId] ASC);
GO

-- 4. Seed Data (1,000 Records per table)
-- 4.1 Insert 1,000 Payments
SET IDENTITY_INSERT [dbo].[Payments] ON;

DECLARE @MethodLookup TABLE (Id INT PRIMARY KEY, Method VARCHAR(40));
INSERT INTO @MethodLookup VALUES
(1, 'UPI FastPay'), (2, 'CreditCard VISA/MasterCard'), (3, 'NetBanking Corporate'),
(4, 'Automotive Wire Transfer'), (5, 'Bank Loan Disbursement');

DECLARE @PayStatusLookup TABLE (Id INT PRIMARY KEY, Status VARCHAR(30));
INSERT INTO @PayStatusLookup VALUES
(1, 'Captured'), (2, 'Authorized'), (3, 'Captured'), (4, 'Refunded');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[Payments] (
    [Id], [PaymentReference], [SalesOrderId], [CustomerId], [Amount], [Currency],
    [Status], [PaymentMethod], [IdempotencyKey], [GatewayTransactionId], [CreatedAtUtc], [UpdatedAtUtc]
)
SELECT 
    n.N,
    CONCAT('PAY-2026-', RIGHT('000000' + CAST(n.N AS VARCHAR(6)), 6)),
    CAST(CAST(HASHBYTES('MD5', 'SO_ORDER_' + CAST(n.N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    CONCAT('CUST-', RIGHT('0000' + CAST(((n.N - 1) % 500) + 1 AS VARCHAR(5)), 5)),
    CAST(1500000.00 + ((n.N * 18713) % 7500000) AS DECIMAL(18,2)),
    'INR',
    pst.Status,
    pm.Method,
    CONCAT('IDEMP-KEY-', CONVERT(NVARCHAR(36), CAST(CAST(HASHBYTES('MD5', 'IDEMP_' + CAST(n.N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER))),
    CONCAT('GTW-TXN-', RIGHT('00000000' + CAST((n.N * 59281) % 99999999 AS VARCHAR(8)), 8)),
    DATEADD(MINUTE, - (n.N * 12), '2026-10-28 12:00:00'),
    DATEADD(MINUTE, - (n.N * 10), '2026-10-28 12:00:00')
FROM Numbers n
JOIN @MethodLookup pm ON pm.Id = ((n.N - 1) % 5) + 1
JOIN @PayStatusLookup pst ON pst.Id = ((n.N - 1) % 4) + 1;

SET IDENTITY_INSERT [dbo].[Payments] OFF;
GO

-- 4.2 Insert 1,000 PaymentTransactions (Mapped 1-to-1 to Payments)
SET IDENTITY_INSERT [dbo].[PaymentTransactions] ON;

DECLARE @TxnTypeLookup TABLE (Id INT PRIMARY KEY, TxnType VARCHAR(30), RespCode VARCHAR(20), Msg VARCHAR(100));
INSERT INTO @TxnTypeLookup VALUES
(1, 'Capture', '200_OK', 'Payment captured successfully via instant clearing house.'),
(2, 'Authorization', 'AUTH_OK', 'Funds authorized and placed on hold pending dealer allocation.'),
(3, 'Capture', '200_OK', 'Full order value successfully debited from customer escrow account.'),
(4, 'Refund', 'REFUND_SETTLED', 'Refund credit processed back to originating payment instrument.');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[PaymentTransactions] (
    [Id], [PaymentId], [TransactionType], [Amount], [Currency], [Status],
    [GatewayResponseCode], [GatewayMessage], [ProcessedAtUtc]
)
SELECT 
    n.N,
    n.N, -- PaymentId 1..1000
    tx.TxnType,
    CAST(1500000.00 + ((n.N * 18713) % 7500000) AS DECIMAL(18,2)),
    'INR',
    'Success',
    tx.RespCode,
    tx.Msg,
    DATEADD(MINUTE, - (n.N * 11), '2026-10-28 12:00:00')
FROM Numbers n
JOIN @TxnTypeLookup tx ON tx.Id = ((n.N - 1) % 4) + 1;

SET IDENTITY_INSERT [dbo].[PaymentTransactions] OFF;
GO

-- 4.3 Insert 1,000 PaymentRefunds (Mapped 1-to-1 to Payments)
SET IDENTITY_INSERT [dbo].[PaymentRefunds] ON;

DECLARE @RefundReasonLookup TABLE (Id INT PRIMARY KEY, Reason VARCHAR(200));
INSERT INTO @RefundReasonLookup VALUES
(1, 'Order cancellation requested by customer prior to vehicle VIN assignment.'),
(2, 'Customer upgraded vehicle booking to higher performance variant.'),
(3, 'Dealership stock reallocation timeline exceeded SLA threshold.'),
(4, 'Financing loan terms restructured with partner bank direct settlement.');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[PaymentRefunds] (
    [Id], [PaymentId], [SalesOrderPublicId], [RefundReference], [Amount],
    [Reason], [Status], [CreatedAtUtc], [CompletedAtUtc]
)
SELECT 
    n.N,
    n.N, -- PaymentId 1..1000
    CAST(CAST(HASHBYTES('MD5', 'SO_ORDER_' + CAST(n.N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    CONCAT('RFND-2026-', RIGHT('000000' + CAST(n.N AS VARCHAR(6)), 6)),
    CAST(1500000.00 + ((n.N * 18713) % 7500000) AS DECIMAL(18,2)),
    r.Reason,
    CASE WHEN n.N % 3 = 0 THEN 'Requested' ELSE 'Completed' END,
    DATEADD(MINUTE, - (n.N * 9), '2026-10-28 12:00:00'),
    DATEADD(MINUTE, - (n.N * 4), '2026-10-28 12:00:00')
FROM Numbers n
JOIN @RefundReasonLookup r ON r.Id = ((n.N - 1) % 4) + 1;

SET IDENTITY_INSERT [dbo].[PaymentRefunds] OFF;
GO

-- 5. Verification Query
SELECT 'Payments' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Payments]
UNION ALL
SELECT 'PaymentTransactions', COUNT(*) FROM [dbo].[PaymentTransactions]
UNION ALL
SELECT 'PaymentRefunds', COUNT(*) FROM [dbo].[PaymentRefunds];
GO
