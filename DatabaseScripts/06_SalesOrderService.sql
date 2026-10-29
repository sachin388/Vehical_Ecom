-- ==================================================================================
-- Service: SalesOrder Service
-- Target Database: SalesOrderDb
-- Description: DDL & realistic seed data generation (1,000 records per table)
-- Engine: Microsoft SQL Server (Transact-SQL)
-- ==================================================================================

SET NOCOUNT ON;
GO

-- 1. Create Database if not exists
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'SalesOrderDb')
BEGIN
    CREATE DATABASE [SalesOrderDb];
END
GO

USE [SalesOrderDb];
GO

-- 2. Drop existing foreign keys & tables if recreating
IF OBJECT_ID(N'[dbo].[FK_SalesOrderLineAccessories_SalesOrderLines_SalesOrderLineId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[SalesOrderLineAccessories] DROP CONSTRAINT [FK_SalesOrderLineAccessories_SalesOrderLines_SalesOrderLineId];
IF OBJECT_ID(N'[dbo].[FK_SalesOrderLines_SalesOrders_SalesOrderId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[SalesOrderLines] DROP CONSTRAINT [FK_SalesOrderLines_SalesOrders_SalesOrderId];
GO

IF OBJECT_ID(N'[dbo].[SalesOrderLineAccessories]', 'U') IS NOT NULL DROP TABLE [dbo].[SalesOrderLineAccessories];
IF OBJECT_ID(N'[dbo].[SalesOrderLines]', 'U') IS NOT NULL DROP TABLE [dbo].[SalesOrderLines];
IF OBJECT_ID(N'[dbo].[SalesOrderHistory]', 'U') IS NOT NULL DROP TABLE [dbo].[SalesOrderHistory];
IF OBJECT_ID(N'[dbo].[OrderCancellationSagas]', 'U') IS NOT NULL DROP TABLE [dbo].[OrderCancellationSagas];
IF OBJECT_ID(N'[dbo].[ProcessedIntegrationEvents]', 'U') IS NOT NULL DROP TABLE [dbo].[ProcessedIntegrationEvents];
IF OBJECT_ID(N'[dbo].[OutboxMessages]', 'U') IS NOT NULL DROP TABLE [dbo].[OutboxMessages];
IF OBJECT_ID(N'[dbo].[SalesOrders]', 'U') IS NOT NULL DROP TABLE [dbo].[SalesOrders];
GO

-- 3. Create Tables
CREATE TABLE [dbo].[SalesOrders] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [PublicId] UNIQUEIDENTIFIER NOT NULL,
    [OrderNumber] NVARCHAR(50) NOT NULL,
    [CustomerId] NVARCHAR(100) NOT NULL,
    [DealerId] BIGINT NULL,
    [Status] INT NOT NULL, -- 1=Draft, 2=PendingPayment, 3=Confirmed, 4=Allocated, 5=Processing, 6=Completed, 7=Cancelled
    [SubtotalSnapshot] DECIMAL(18,2) NOT NULL,
    [DiscountSnapshot] DECIMAL(18,2) NOT NULL,
    [TaxSnapshot] DECIMAL(18,2) NOT NULL,
    [GrandTotalSnapshot] DECIMAL(18,2) NOT NULL,
    [Currency] NVARCHAR(10) NOT NULL CONSTRAINT [DF_SalesOrders_Currency] DEFAULT ('INR'),
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_SalesOrders_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_SalesOrders_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_SalesOrders] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_SalesOrders_PublicId] ON [dbo].[SalesOrders] ([PublicId] ASC);
CREATE UNIQUE NONCLUSTERED INDEX [IX_SalesOrders_OrderNumber] ON [dbo].[SalesOrders] ([OrderNumber] ASC);
GO

CREATE TABLE [dbo].[SalesOrderLines] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [SalesOrderId] BIGINT NOT NULL,
    [ItemId] INT NOT NULL,
    [ItemCode] NVARCHAR(100) NOT NULL,
    [ItemNameSnapshot] NVARCHAR(250) NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitPriceSnapshot] DECIMAL(18,2) NOT NULL,
    [DiscountSnapshot] DECIMAL(18,2) NOT NULL,
    [TaxSnapshot] DECIMAL(18,2) NOT NULL,
    [LineTotalSnapshot] DECIMAL(18,2) NOT NULL,
    [Currency] NVARCHAR(10) NOT NULL CONSTRAINT [DF_SalesOrderLines_Currency] DEFAULT ('INR'),
    CONSTRAINT [PK_SalesOrderLines] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SalesOrderLines_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) 
        REFERENCES [dbo].[SalesOrders] ([Id]) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX [IX_SalesOrderLines_SalesOrderId] ON [dbo].[SalesOrderLines] ([SalesOrderId] ASC);
GO

CREATE TABLE [dbo].[SalesOrderLineAccessories] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [SalesOrderLineId] BIGINT NOT NULL,
    [AccessoryId] INT NOT NULL,
    [AccessoryNameSnapshot] NVARCHAR(250) NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitPriceSnapshot] DECIMAL(18,2) NOT NULL,
    [LineTotalSnapshot] DECIMAL(18,2) NOT NULL,
    CONSTRAINT [PK_SalesOrderLineAccessories] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SalesOrderLineAccessories_SalesOrderLines_SalesOrderLineId] FOREIGN KEY ([SalesOrderLineId]) 
        REFERENCES [dbo].[SalesOrderLines] ([Id]) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX [IX_SalesOrderLineAccessories_SalesOrderLineId] ON [dbo].[SalesOrderLineAccessories] ([SalesOrderLineId] ASC);
GO

CREATE TABLE [dbo].[SalesOrderHistory] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [SalesOrderPublicId] UNIQUEIDENTIFIER NOT NULL,
    [FromStatus] NVARCHAR(50) NULL,
    [ToStatus] NVARCHAR(50) NOT NULL,
    [ActorId] NVARCHAR(100) NOT NULL,
    [ActorType] NVARCHAR(50) NOT NULL CONSTRAINT [DF_SalesOrderHistory_ActorType] DEFAULT ('User'),
    [Reason] NVARCHAR(500) NOT NULL,
    [ChangedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_SalesOrderHistory_ChangedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CorrelationId] NVARCHAR(100) NOT NULL,
    CONSTRAINT [PK_SalesOrderHistory] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE NONCLUSTERED INDEX [IX_SalesOrderHistory_SalesOrderPublicId_ChangedAtUtc] 
    ON [dbo].[SalesOrderHistory] ([SalesOrderPublicId] ASC, [ChangedAtUtc] ASC);
GO

CREATE TABLE [dbo].[OrderCancellationSagas] (
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [SalesOrderPublicId] UNIQUEIDENTIFIER NOT NULL,
    [Status] INT NOT NULL, -- 1=Initiated, 2=WaitingForPaymentCompensation, 3=WaitingForDealerRelease, 4=Completed, 5=Failed
    [PaymentCompensationRequired] BIT NOT NULL,
    [PaymentCompensationCompleted] BIT NOT NULL,
    [DealerReleaseRequired] BIT NOT NULL,
    [DealerReleaseCompleted] BIT NOT NULL,
    [RequestedBy] NVARCHAR(100) NOT NULL,
    [Reason] NVARCHAR(500) NOT NULL,
    [LastError] NVARCHAR(MAX) NULL,
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_OrderCancellationSagas_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_OrderCancellationSagas] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE NONCLUSTERED INDEX [IX_OrderCancellationSagas_SalesOrderPublicId] ON [dbo].[OrderCancellationSagas] ([SalesOrderPublicId] ASC);
GO

CREATE TABLE [dbo].[ProcessedIntegrationEvents] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [ConsumerName] NVARCHAR(200) NOT NULL,
    [EventId] UNIQUEIDENTIFIER NOT NULL,
    [ProcessedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_ProcessedIntegrationEvents_ProcessedAtUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ProcessedIntegrationEvents] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE NONCLUSTERED INDEX [IX_ProcessedIntegrationEvents_ConsumerName_EventId] 
    ON [dbo].[ProcessedIntegrationEvents] ([ConsumerName] ASC, [EventId] ASC);
GO

CREATE TABLE [dbo].[OutboxMessages] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [EventId] UNIQUEIDENTIFIER NOT NULL,
    [AggregateId] NVARCHAR(100) NOT NULL,
    [EventType] NVARCHAR(200) NOT NULL,
    [Topic] NVARCHAR(200) NOT NULL,
    [PayloadJson] NVARCHAR(MAX) NOT NULL,
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_OutboxMessages_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [PublishedAtUtc] DATETIME2(7) NULL,
    [RetryCount] INT NOT NULL CONSTRAINT [DF_OutboxMessages_RetryCount] DEFAULT (0),
    [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_OutboxMessages_Status] DEFAULT ('Pending'),
    [Error] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_OutboxMessages] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- 4. Seed Data (1,000 Records per table)
-- 4.1 Insert 1,000 SalesOrders
SET IDENTITY_INSERT [dbo].[SalesOrders] ON;

DECLARE @StatusLookup TABLE (Id INT PRIMARY KEY, Code INT);
INSERT INTO @StatusLookup VALUES 
(1, 1), -- Draft
(2, 2), -- PendingPayment
(3, 3), -- Confirmed
(4, 4), -- Allocated
(5, 5), -- Processing
(6, 6); -- Completed

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
),
OrderCalc AS (
    SELECT 
        n.N,
        CAST(CAST(HASHBYTES('MD5', 'SO_ORDER_' + CAST(n.N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER) AS OrderGuid,
        s.Code AS OrderStatus,
        CAST(1450000.00 + ((n.N * 18713) % 7200000) AS DECIMAL(18,2)) AS SubTot,
        CAST(35000.00 + ((n.N * 2500) % 50000) AS DECIMAL(18,2)) AS DiscTot,
        CAST((1450000.00 + ((n.N * 18713) % 7200000)) * 0.28 AS DECIMAL(18,2)) AS TaxTot
    FROM Numbers n
    JOIN @StatusLookup s ON s.Id = ((n.N - 1) % 6) + 1
)
INSERT INTO [dbo].[SalesOrders] (
    [Id], [PublicId], [OrderNumber], [CustomerId], [DealerId], [Status],
    [SubtotalSnapshot], [DiscountSnapshot], [TaxSnapshot], [GrandTotalSnapshot],
    [Currency], [CreatedAtUtc], [UpdatedAtUtc]
)
SELECT 
    N,
    OrderGuid,
    CONCAT('SO-2026-', RIGHT('000000' + CAST(N AS VARCHAR(6)), 6)),
    CONCAT('CUST-', RIGHT('0000' + CAST(((N - 1) % 500) + 1 AS VARCHAR(5)), 5)),
    ((N - 1) % 100) + 1, -- DealerId 1..100
    OrderStatus,
    SubTot,
    DiscTot,
    TaxTot,
    CAST(SubTot - DiscTot + TaxTot AS DECIMAL(18,2)),
    'INR',
    DATEADD(MINUTE, -N * 15, '2026-10-28 12:00:00'),
    DATEADD(MINUTE, -N * 5, '2026-10-28 12:00:00')
FROM OrderCalc;

SET IDENTITY_INSERT [dbo].[SalesOrders] OFF;
GO

-- 4.2 Insert 1,000 SalesOrderLines (Mapped 1-to-1 to SalesOrders)
SET IDENTITY_INSERT [dbo].[SalesOrderLines] ON;

DECLARE @VehicleModelNames TABLE (Id INT PRIMARY KEY, Name VARCHAR(50));
INSERT INTO @VehicleModelNames VALUES
(1, '2026 Toyota Camry XLE Hybrid'), (2, '2026 BMW 330i M Sport'), (3, '2026 Tesla Model Y Long Range'),
(4, '2026 Porsche 911 Carrera 4S'), (5, '2026 Mercedes-Benz C300 AMG'), (6, '2026 Audi A4 Quattro Prestige'),
(7, '2026 Hyundai Ioniq 5 AWD'), (8, '2026 Tata Nexon Fearless+ EV'), (9, '2026 Mahindra XUV700 AX7L'),
(10, '2026 Ford Mustang GT Fastback');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
),
LineCalc AS (
    SELECT 
        n.N,
        CAST(1450000.00 + ((n.N * 18713) % 7200000) AS DECIMAL(18,2)) AS SubTot,
        CAST(35000.00 + ((n.N * 2500) % 50000) AS DECIMAL(18,2)) AS DiscTot,
        CAST((1450000.00 + ((n.N * 18713) % 7200000)) * 0.28 AS DECIMAL(18,2)) AS TaxTot
    FROM Numbers n
)
INSERT INTO [dbo].[SalesOrderLines] (
    [Id], [SalesOrderId], [ItemId], [ItemCode], [ItemNameSnapshot], [Quantity],
    [UnitPriceSnapshot], [DiscountSnapshot], [TaxSnapshot], [LineTotalSnapshot], [Currency]
)
SELECT 
    lc.N,
    lc.N, -- SalesOrderId 1..1000
    lc.N, -- ItemId 1..1000
    CONCAT('VEH-SKU-', RIGHT('00000' + CAST(lc.N AS VARCHAR(6)), 6)),
    vm.Name,
    1, -- Quantity
    lc.SubTot,
    lc.DiscTot,
    lc.TaxTot,
    CAST(lc.SubTot - lc.DiscTot + lc.TaxTot AS DECIMAL(18,2)),
    'INR'
FROM LineCalc lc
JOIN @VehicleModelNames vm ON vm.Id = ((lc.N - 1) % 10) + 1;

SET IDENTITY_INSERT [dbo].[SalesOrderLines] OFF;
GO

-- 4.3 Insert 1,000 SalesOrderLineAccessories (Mapped 1-to-1 to SalesOrderLines)
SET IDENTITY_INSERT [dbo].[SalesOrderLineAccessories] ON;

DECLARE @AccNameLookup TABLE (Id INT PRIMARY KEY, Name VARCHAR(80), Price DECIMAL(18,2));
INSERT INTO @AccNameLookup VALUES
(1, 'All-Weather Premium 3D Floor Liners', 5499.00),
(2, 'Dashcam 4K UHD Front & Rear with GPS', 18999.00),
(3, 'Ceramic Pro 9H Paint Protection Shield', 32000.00),
(4, 'Wireless MagSafe Fast Vehicle Charger', 4299.00),
(5, 'Aero Crossbars Heavy-Duty Roof Rack', 14500.00),
(6, 'Illuminated Door Sill Guards with Emblem', 6800.00),
(7, 'Nappa Leather Custom Fitted Seat Covers', 28500.00),
(8, 'Dynamic Ambient LED Interior Lighting Kit', 8500.00);

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[SalesOrderLineAccessories] (
    [Id], [SalesOrderLineId], [AccessoryId], [AccessoryNameSnapshot],
    [Quantity], [UnitPriceSnapshot], [LineTotalSnapshot]
)
SELECT 
    n.N,
    n.N, -- SalesOrderLineId 1..1000
    ((n.N - 1) % 1000) + 1,
    acc.Name,
    1,
    acc.Price,
    acc.Price
FROM Numbers n
JOIN @AccNameLookup acc ON acc.Id = ((n.N - 1) % 8) + 1;

SET IDENTITY_INSERT [dbo].[SalesOrderLineAccessories] OFF;
GO

-- 4.4 Insert 1,000 SalesOrderHistory
SET IDENTITY_INSERT [dbo].[SalesOrderHistory] ON;

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[SalesOrderHistory] (
    [Id], [SalesOrderPublicId], [FromStatus], [ToStatus], [ActorId], [ActorType],
    [Reason], [ChangedAtUtc], [CorrelationId]
)
SELECT 
    n.N,
    CAST(CAST(HASHBYTES('MD5', 'SO_ORDER_' + CAST(n.N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    'PendingPayment',
    'Confirmed',
    CONCAT('USR-ACTOR-', RIGHT('000' + CAST(((n.N - 1) % 50) + 1 AS VARCHAR(3)), 3)),
    'System',
    'Customer payment successfully authorized and validated via payment gateway webhook.',
    DATEADD(MINUTE, - (n.N * 10), '2026-10-28 12:00:00'),
    CONCAT('CORR-ORDER-', RIGHT('000000' + CAST(n.N AS VARCHAR(6)), 6))
FROM Numbers n;

SET IDENTITY_INSERT [dbo].[SalesOrderHistory] OFF;
GO

-- 4.5 Insert 1,000 OrderCancellationSagas
;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[OrderCancellationSagas] (
    [Id], [SalesOrderPublicId], [Status], [PaymentCompensationRequired],
    [PaymentCompensationCompleted], [DealerReleaseRequired], [DealerReleaseCompleted],
    [RequestedBy], [Reason], [LastError], [CreatedAtUtc], [UpdatedAtUtc]
)
SELECT 
    CAST(CAST(HASHBYTES('MD5', 'CANC_SAGA_' + CAST(n.N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    CAST(CAST(HASHBYTES('MD5', 'SO_ORDER_' + CAST(n.N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    CASE WHEN n.N % 4 = 0 THEN 4 ELSE 1 END, -- 4=Completed, 1=Initiated
    1,
    CASE WHEN n.N % 4 = 0 THEN 1 ELSE 0 END,
    1,
    CASE WHEN n.N % 4 = 0 THEN 1 ELSE 0 END,
    CONCAT('customer.service@dealership-', RIGHT('000' + CAST(((n.N - 1) % 50) + 1 AS VARCHAR(3)), 3), '.com'),
    'Customer requested delivery rescheduling or order variation before factory dispatch.',
    NULL,
    DATEADD(HOUR, - (n.N % 48), '2026-10-28 12:00:00'),
    DATEADD(HOUR, - (n.N % 24), '2026-10-28 12:00:00')
FROM Numbers n;
GO

-- 4.6 Insert 1,000 ProcessedIntegrationEvents
SET IDENTITY_INSERT [dbo].[ProcessedIntegrationEvents] ON;

DECLARE @ConsumerLookup TABLE (Id INT PRIMARY KEY, Name VARCHAR(80));
INSERT INTO @ConsumerLookup VALUES
(1, 'SalesOrder.PaymentSucceededHandler'),
(2, 'SalesOrder.DealerAllocatedHandler'),
(3, 'SalesOrder.PaymentCompensationCompletedHandler'),
(4, 'SalesOrder.DealerReleaseCompletedHandler');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[ProcessedIntegrationEvents] (
    [Id], [ConsumerName], [EventId], [ProcessedAtUtc]
)
SELECT 
    n.N,
    c.Name,
    CAST(CAST(HASHBYTES('MD5', 'PROCESSED_EVT_' + CAST(n.N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    DATEADD(MINUTE, - (n.N * 8), '2026-10-28 12:00:00')
FROM Numbers n
JOIN @ConsumerLookup c ON c.Id = ((n.N - 1) % 4) + 1;

SET IDENTITY_INSERT [dbo].[ProcessedIntegrationEvents] OFF;
GO

-- 4.7 Insert 1,000 OutboxMessages
SET IDENTITY_INSERT [dbo].[OutboxMessages] ON;

DECLARE @TopicLookup TABLE (Id INT PRIMARY KEY, EvtType VARCHAR(60), Topic VARCHAR(60));
INSERT INTO @TopicLookup VALUES
(1, 'SalesOrderCreatedEvent', 'salesorder.events'),
(2, 'SalesOrderConfirmedEvent', 'salesorder.events'),
(3, 'SalesOrderAllocatedEvent', 'salesorder.events'),
(4, 'SalesOrderCancelledEvent', 'salesorder.events');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[OutboxMessages] (
    [Id], [EventId], [AggregateId], [EventType], [Topic], [PayloadJson],
    [CreatedAtUtc], [PublishedAtUtc], [RetryCount], [Status], [Error]
)
SELECT 
    n.N,
    CAST(CAST(HASHBYTES('MD5', 'OUTBOX_EVT_' + CAST(n.N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    CONCAT('SO-2026-', RIGHT('000000' + CAST(n.N AS VARCHAR(6)), 6)),
    t.EvtType,
    t.Topic,
    CONCAT('{"orderNumber":"SO-2026-', RIGHT('000000' + CAST(n.N AS VARCHAR(6)), 6), '","status":"Published","currency":"INR","timestamp":"2026-10-28T12:00:00Z"}'),
    DATEADD(MINUTE, - (n.N * 12), '2026-10-28 12:00:00'),
    DATEADD(MINUTE, - (n.N * 11), '2026-10-28 12:00:00'),
    0,
    'Published',
    NULL
FROM Numbers n
JOIN @TopicLookup t ON t.Id = ((n.N - 1) % 4) + 1;

SET IDENTITY_INSERT [dbo].[OutboxMessages] OFF;
GO

-- 5. Verification Query
SELECT 'SalesOrders' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[SalesOrders]
UNION ALL
SELECT 'SalesOrderLines', COUNT(*) FROM [dbo].[SalesOrderLines]
UNION ALL
SELECT 'SalesOrderLineAccessories', COUNT(*) FROM [dbo].[SalesOrderLineAccessories]
UNION ALL
SELECT 'SalesOrderHistory', COUNT(*) FROM [dbo].[SalesOrderHistory]
UNION ALL
SELECT 'OrderCancellationSagas', COUNT(*) FROM [dbo].[OrderCancellationSagas]
UNION ALL
SELECT 'ProcessedIntegrationEvents', COUNT(*) FROM [dbo].[ProcessedIntegrationEvents]
UNION ALL
SELECT 'OutboxMessages', COUNT(*) FROM [dbo].[OutboxMessages];
GO
