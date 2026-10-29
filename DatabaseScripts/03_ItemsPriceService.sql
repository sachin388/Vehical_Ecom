-- ==================================================================================
-- Service: ItemsPrice Service
-- Target Database: VehicleEcommerce_ItemPriceDb (or ItemsPriceDb)
-- Description: DDL & realistic seed data generation (1,000 records per table)
-- Engine: Microsoft SQL Server (Transact-SQL)
-- ==================================================================================

SET NOCOUNT ON;
GO

-- 1. Create Database if not exists
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'VehicleEcommerce_ItemPriceDb')
BEGIN
    CREATE DATABASE [VehicleEcommerce_ItemPriceDb];
END
GO

USE [VehicleEcommerce_ItemPriceDb];
GO

-- 2. Drop existing foreign keys & tables if recreating
IF OBJECT_ID(N'[dbo].[FK_PriceDiscounts_ItemPrices_ItemPriceId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[PriceDiscounts] DROP CONSTRAINT [FK_PriceDiscounts_ItemPrices_ItemPriceId];
GO

IF OBJECT_ID(N'[dbo].[PriceDiscounts]', 'U') IS NOT NULL DROP TABLE [dbo].[PriceDiscounts];
IF OBJECT_ID(N'[dbo].[ItemPrices]', 'U') IS NOT NULL DROP TABLE [dbo].[ItemPrices];
GO

-- 3. Create Tables
CREATE TABLE [dbo].[ItemPrices] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [ItemId] INT NOT NULL,
    [SKU] NVARCHAR(100) NOT NULL,
    [BasePrice] DECIMAL(18,2) NOT NULL,
    [Currency] NVARCHAR(10) NOT NULL CONSTRAINT [DF_ItemPrices_Currency] DEFAULT ('INR'),
    [EffectiveFromUtc] DATETIME2(7) NOT NULL,
    [EffectiveToUtc] DATETIME2(7) NULL,
    [Version] INT NOT NULL CONSTRAINT [DF_ItemPrices_Version] DEFAULT (1),
    [Status] INT NOT NULL CONSTRAINT [DF_ItemPrices_Status] DEFAULT (1), -- 1=Active, 2=Inactive, 3=Expired
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_ItemPrices_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_ItemPrices] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_ItemPrices_ItemId_Version] ON [dbo].[ItemPrices] ([ItemId] ASC, [Version] ASC);
CREATE NONCLUSTERED INDEX [IX_ItemPrices_ItemId_EffectiveFromUtc] ON [dbo].[ItemPrices] ([ItemId] ASC, [EffectiveFromUtc] ASC);
GO

CREATE TABLE [dbo].[PriceDiscounts] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [ItemPriceId] BIGINT NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [DiscountType] INT NOT NULL, -- 1=FixedAmount, 2=Percentage
    [DiscountValue] DECIMAL(18,2) NOT NULL,
    [MaximumDiscountAmount] DECIMAL(18,2) NULL,
    [EffectiveFromUtc] DATETIME2(7) NOT NULL,
    [EffectiveToUtc] DATETIME2(7) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_PriceDiscounts_IsActive] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_PriceDiscounts_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_PriceDiscounts] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PriceDiscounts_ItemPrices_ItemPriceId] FOREIGN KEY ([ItemPriceId]) 
        REFERENCES [dbo].[ItemPrices] ([Id]) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX [IX_PriceDiscounts_ItemPriceId] ON [dbo].[PriceDiscounts] ([ItemPriceId] ASC);
GO

-- 4. Seed Data (1,000 Records per table)
-- 4.1 Insert 1,000 ItemPrices
SET IDENTITY_INSERT [dbo].[ItemPrices] ON;

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[ItemPrices] (
    [Id], [ItemId], [SKU], [BasePrice], [Currency], [EffectiveFromUtc], [EffectiveToUtc],
    [Version], [Status], [CreatedAtUtc], [UpdatedAtUtc]
)
SELECT 
    n.N,
    n.N, -- ItemId 1..1000
    CONCAT('VEH-SKU-', RIGHT('00000' + CAST(n.N AS VARCHAR(6)), 6)),
    CAST(950000.00 + ((n.N * 19731) % 12500000) AS DECIMAL(18,2)), -- Realistic ex-showroom base price ₹9.5 Lakh to ₹1.34 Cr
    'INR',
    DATEADD(DAY, - (n.N % 30), '2026-10-28 12:00:00'),
    DATEADD(DAY, 365, '2026-10-28 12:00:00'),
    1, -- Version 1
    1, -- Active
    DATEADD(DAY, -n.N, '2026-10-28 12:00:00'),
    NULL
FROM Numbers n;

SET IDENTITY_INSERT [dbo].[ItemPrices] OFF;
GO

-- 4.2 Insert 1,000 PriceDiscounts (Mapped 1-to-1 to ItemPrices)
SET IDENTITY_INSERT [dbo].[PriceDiscounts] ON;

DECLARE @DiscountLookup TABLE (
    Id INT PRIMARY KEY, 
    Name VARCHAR(80), 
    DiscType INT, 
    Val DECIMAL(18,2), 
    MaxVal DECIMAL(18,2)
);
INSERT INTO @DiscountLookup VALUES
(1, 'Festive Season Cashback Advantage', 1, 50000.00, 50000.00),
(2, 'EV Clean Energy Incentive Rebate', 1, 75000.00, 75000.00),
(3, 'Corporate Partner Privilege Discount', 2, 4.50, 60000.00),
(4, 'Loyalty Vehicle Exchange Bonus', 1, 40000.00, 40000.00),
(5, 'Early Bird Model Launch Incentive', 2, 5.00, 75000.00),
(6, 'Year-End Inventory Clearance Benefit', 1, 85000.00, 85000.00),
(7, 'Armed Forces & Healthcare Honorarium', 1, 30000.00, 30000.00),
(8, 'Green Mobility Scrappage Incentive', 1, 35000.00, 35000.00),
(9, 'Commercial Fleet Volume Advantage', 2, 6.00, 120000.00),
(10, 'Special Dealership Weekend Voucher', 1, 25000.00, 25000.00);

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[PriceDiscounts] (
    [Id], [ItemPriceId], [Name], [DiscountType], [DiscountValue], [MaximumDiscountAmount],
    [EffectiveFromUtc], [EffectiveToUtc], [IsActive], [CreatedAtUtc], [UpdatedAtUtc]
)
SELECT 
    n.N,
    n.N, -- ItemPriceId 1..1000
    CONCAT(d.Name, ' [Code PR-DISC-', RIGHT('000' + CAST(n.N AS VARCHAR(4)), 4), ']'),
    d.DiscType,
    d.Val,
    d.MaxVal,
    DATEADD(DAY, - (n.N % 15), '2026-10-28 12:00:00'),
    DATEADD(DAY, 60, '2026-10-28 12:00:00'),
    1, -- Active
    DATEADD(DAY, - (n.N % 20), '2026-10-28 12:00:00'),
    NULL
FROM Numbers n
JOIN @DiscountLookup d ON d.Id = ((n.N - 1) % 10) + 1;

SET IDENTITY_INSERT [dbo].[PriceDiscounts] OFF;
GO

-- 5. Verification Query
SELECT 'ItemPrices' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[ItemPrices]
UNION ALL
SELECT 'PriceDiscounts', COUNT(*) FROM [dbo].[PriceDiscounts];
GO
