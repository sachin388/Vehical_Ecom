-- ==================================================================================
-- Service: ItemsPriceBreakup Service
-- Target Database: PriceBreakupDb (or ItemsPriceBreakupDb)
-- Description: DDL & realistic seed data generation (1,000 records per table)
-- Engine: Microsoft SQL Server (Transact-SQL)
-- ==================================================================================

SET NOCOUNT ON;
GO

-- 1. Create Database if not exists
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'PriceBreakupDb')
BEGIN
    CREATE DATABASE [PriceBreakupDb];
END
GO

USE [PriceBreakupDb];
GO

-- 2. Drop existing foreign keys & tables if recreating
IF OBJECT_ID(N'[dbo].[FK_PriceBreakupLines_PriceBreakups_PriceBreakupId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[PriceBreakupLines] DROP CONSTRAINT [FK_PriceBreakupLines_PriceBreakups_PriceBreakupId];
GO

IF OBJECT_ID(N'[dbo].[PriceBreakupLines]', 'U') IS NOT NULL DROP TABLE [dbo].[PriceBreakupLines];
IF OBJECT_ID(N'[dbo].[PriceBreakups]', 'U') IS NOT NULL DROP TABLE [dbo].[PriceBreakups];
GO

-- 3. Create Tables
CREATE TABLE [dbo].[PriceBreakups] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [ItemId] INT NOT NULL,
    [SKU] NVARCHAR(100) NOT NULL,
    [StateCode] NVARCHAR(10) NOT NULL,
    [VehicleCategory] NVARCHAR(100) NOT NULL,
    [PriceVersion] INT NOT NULL CONSTRAINT [DF_PriceBreakups_PriceVersion] DEFAULT (1),
    [Currency] NVARCHAR(10) NOT NULL CONSTRAINT [DF_PriceBreakups_Currency] DEFAULT ('INR'),
    [SubTotal] DECIMAL(18,2) NOT NULL,
    [TotalTax] DECIMAL(18,2) NOT NULL,
    [TotalCharges] DECIMAL(18,2) NOT NULL,
    [TotalDiscount] DECIMAL(18,2) NOT NULL,
    [FinalOnRoadPrice] DECIMAL(18,2) NOT NULL,
    [Status] INT NOT NULL CONSTRAINT [DF_PriceBreakups_Status] DEFAULT (1), -- 1=Calculated, 2=Snapshot, 3=Cancelled
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_PriceBreakups_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_PriceBreakups] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE NONCLUSTERED INDEX [IX_PriceBreakups_ItemId_StateCode] ON [dbo].[PriceBreakups] ([ItemId] ASC, [StateCode] ASC);
GO

CREATE TABLE [dbo].[PriceBreakupLines] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [PriceBreakupId] BIGINT NOT NULL,
    [ChargeType] INT NOT NULL, -- 1=ExShowroom, 2=GST, 3=RTO, 4=Registration, 5=Insurance, 6=Handling, 7=Other, 8=Discount, 9=Accessory
    [Code] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [TaxAmount] DECIMAL(18,2) NOT NULL,
    [IsTaxIncluded] BIT NOT NULL CONSTRAINT [DF_PriceBreakupLines_IsTaxIncluded] DEFAULT (0),
    [CalculationBasis] NVARCHAR(250) NOT NULL,
    [RuleVersion] NVARCHAR(20) NOT NULL CONSTRAINT [DF_PriceBreakupLines_RuleVersion] DEFAULT ('v1'),
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_PriceBreakupLines_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_PriceBreakupLines] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PriceBreakupLines_PriceBreakups_PriceBreakupId] FOREIGN KEY ([PriceBreakupId]) 
        REFERENCES [dbo].[PriceBreakups] ([Id]) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX [IX_PriceBreakupLines_PriceBreakupId] ON [dbo].[PriceBreakupLines] ([PriceBreakupId] ASC);
GO

-- 4. Seed Data (1,000 Records per table)
-- 4.1 Insert 1,000 PriceBreakups
SET IDENTITY_INSERT [dbo].[PriceBreakups] ON;

DECLARE @StateLookup TABLE (Id INT PRIMARY KEY, Code VARCHAR(5));
INSERT INTO @StateLookup VALUES 
(1, 'MH'), (2, 'DL'), (3, 'KA'), (4, 'TN'), (5, 'TS'),
(6, 'GJ'), (7, 'HR'), (8, 'UP'), (9, 'WB'), (10, 'KL');

DECLARE @CatTypeLookup TABLE (Id INT PRIMARY KEY, Name VARCHAR(50));
INSERT INTO @CatTypeLookup VALUES 
(1, 'Executive Luxury Sedan'), (2, 'Mid-Size Family SUV'), (3, 'Electric Performance Sedan'),
(4, 'Compact Crossover SUV'), (5, 'Heavy-Duty Pickup Truck'), (6, 'Sports Coupe'),
(7, 'Ultra-Luxury Grand Tourer'), (8, 'Rugged Off-Road 4x4');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
),
CalculatedPrices AS (
    SELECT 
        n.N,
        s.Code AS StCode,
        c.Name AS CatName,
        CAST(1200000.00 + ((n.N * 18451) % 8500000) AS DECIMAL(18,2)) AS SubTot,
        CAST((1200000.00 + ((n.N * 18451) % 8500000)) * 0.28 AS DECIMAL(18,2)) AS TaxTot,
        CAST((1200000.00 + ((n.N * 18451) % 8500000)) * 0.12 + 45000.00 AS DECIMAL(18,2)) AS ChargesTot,
        CAST(35000.00 + ((n.N * 2500) % 50000) AS DECIMAL(18,2)) AS DiscTot
    FROM Numbers n
    JOIN @StateLookup s ON s.Id = ((n.N - 1) % 10) + 1
    JOIN @CatTypeLookup c ON c.Id = ((n.N - 1) % 8) + 1
)
INSERT INTO [dbo].[PriceBreakups] (
    [Id], [ItemId], [SKU], [StateCode], [VehicleCategory], [PriceVersion], [Currency],
    [SubTotal], [TotalTax], [TotalCharges], [TotalDiscount], [FinalOnRoadPrice], [Status], [CreatedAtUtc]
)
SELECT 
    N,
    N, -- ItemId 1..1000
    CONCAT('VEH-SKU-', RIGHT('00000' + CAST(N AS VARCHAR(6)), 6)),
    StCode,
    CatName,
    1,
    'INR',
    SubTot,
    TaxTot,
    ChargesTot,
    DiscTot,
    CAST(SubTot + TaxTot + ChargesTot - DiscTot AS DECIMAL(18,2)),
    1, -- Calculated
    DATEADD(DAY, - (N % 30), '2026-10-28 12:00:00')
FROM CalculatedPrices;

SET IDENTITY_INSERT [dbo].[PriceBreakups] OFF;
GO

-- 4.2 Insert 1,000 PriceBreakupLines (Mapped 1-to-1 to PriceBreakups)
SET IDENTITY_INSERT [dbo].[PriceBreakupLines] ON;

DECLARE @LineLookup TABLE (
    Id INT PRIMARY KEY,
    ChargeType INT,
    Code VARCHAR(30),
    Name VARCHAR(100),
    Basis VARCHAR(200)
);
INSERT INTO @LineLookup VALUES
(1, 1, 'EX_SHOWROOM', 'Vehicle Base Ex-Showroom Price', 'Base Manufacturer Suggested Retail Price (MSRP)'),
(2, 2, 'GST_28', 'Goods & Services Tax (GST 28%)', 'Statutory 28% GST on vehicle base price'),
(3, 3, 'RTO_ROAD_TAX', 'State Road Transport Tax (RTO)', 'State Government Road Tax evaluation based on cubic capacity and fuel type'),
(4, 5, 'INS_COMPREHENSIVE', 'Comprehensive 3-Year Motor Insurance', '1-Year Own Damage + 3-Year Third Party statutory liability coverage'),
(5, 4, 'REG_FASTAG', 'Registration Fees & FASTag Kit', 'Statutory smart-card registration, high security registration plates (HSRP) and FASTag RFID'),
(6, 6, 'HANDLING_LOGISTICS', 'Dealership Handling & Transport', 'Factory-to-dealer logistics, inspection, pre-delivery preparation (PDI)'),
(7, 8, 'DISC_CORPORATE', 'Corporate Customer Rebate', 'Corporate partner fleet benefit discount deduction'),
(8, 9, 'ACC_ESSENTIAL_PACK', 'Essential Accessory Protection Pack', 'Mud flaps, rubber boot mat, cabin carpet liners, and ceramic glass coat'),
(9, 7, 'TCS_1_PERCENT', 'Tax Collected at Source (TCS 1%)', 'Section 206C(1F) 1% tax collection for motor vehicles exceeding 10 Lakhs'),
(10, 2, 'CESS_AUTOMOTIVE', 'Automotive Compensation Cess (3%)', 'Central compensation cess for luxury and crossover vehicle segments');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[PriceBreakupLines] (
    [Id], [PriceBreakupId], [ChargeType], [Code], [Name], [Amount], [TaxAmount],
    [IsTaxIncluded], [CalculationBasis], [RuleVersion], [CreatedAtUtc]
)
SELECT 
    n.N,
    n.N, -- Mapped to PriceBreakupId 1..1000
    l.ChargeType,
    l.Code,
    l.Name,
    CAST(25000.00 + ((n.N * 3147) % 250000) AS DECIMAL(18,2)),
    CASE WHEN l.ChargeType = 2 THEN CAST(15000.00 + ((n.N * 1234) % 65000) AS DECIMAL(18,2)) ELSE 0.00 END,
    0,
    l.Basis,
    'v1.2',
    DATEADD(DAY, - (n.N % 30), '2026-10-28 12:00:00')
FROM Numbers n
JOIN @LineLookup l ON l.Id = ((n.N - 1) % 10) + 1;

SET IDENTITY_INSERT [dbo].[PriceBreakupLines] OFF;
GO

-- 5. Verification Query
SELECT 'PriceBreakups' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[PriceBreakups]
UNION ALL
SELECT 'PriceBreakupLines', COUNT(*) FROM [dbo].[PriceBreakupLines];
GO
