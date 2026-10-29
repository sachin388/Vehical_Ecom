-- ==================================================================================
-- Service: Accessory Service
-- Target Database: AccessoryDb
-- Description: DDL & realistic seed data generation (1,000 records per table)
-- Engine: Microsoft SQL Server (Transact-SQL)
-- ==================================================================================

SET NOCOUNT ON;
GO

-- 1. Create Database if not exists
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'AccessoryDb')
BEGIN
    CREATE DATABASE [AccessoryDb];
END
GO

USE [AccessoryDb];
GO

-- 2. Drop existing foreign keys & tables if recreating
IF OBJECT_ID(N'[dbo].[FK_AccessoryCompatibilities_Accessories_AccessoryId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[AccessoryCompatibilities] DROP CONSTRAINT [FK_AccessoryCompatibilities_Accessories_AccessoryId];
GO

IF OBJECT_ID(N'[dbo].[AccessoryCompatibilities]', 'U') IS NOT NULL DROP TABLE [dbo].[AccessoryCompatibilities];
IF OBJECT_ID(N'[dbo].[Accessories]', 'U') IS NOT NULL DROP TABLE [dbo].[Accessories];
GO

-- 3. Create Tables
CREATE TABLE [dbo].[Accessories] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(100) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(MAX) NOT NULL,
    [Price] DECIMAL(18,2) NOT NULL,
    [Currency] NVARCHAR(10) NOT NULL CONSTRAINT [DF_Accessories_Currency] DEFAULT ('INR'),
    [AvailableQuantity] INT NOT NULL CONSTRAINT [DF_Accessories_AvailableQuantity] DEFAULT (0),
    [Status] INT NOT NULL CONSTRAINT [DF_Accessories_Status] DEFAULT (1), -- 1=Active, 2=Inactive, 3=Discontinued
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Accessories_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_Accessories] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Accessories_Code] ON [dbo].[Accessories] ([Code] ASC);
GO

CREATE TABLE [dbo].[AccessoryCompatibilities] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [AccessoryId] BIGINT NOT NULL,
    [ItemId] INT NOT NULL,
    [SKU] NVARCHAR(100) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_AccessoryCompatibilities_IsActive] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_AccessoryCompatibilities_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_AccessoryCompatibilities] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AccessoryCompatibilities_Accessories_AccessoryId] FOREIGN KEY ([AccessoryId]) 
        REFERENCES [dbo].[Accessories] ([Id]) ON DELETE CASCADE
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_AccessoryCompatibilities_AccessoryId_ItemId] 
    ON [dbo].[AccessoryCompatibilities] ([AccessoryId] ASC, [ItemId] ASC);
GO

-- 4. Seed Data (1,000 Records per table)
-- 4.1 Insert 1,000 Accessories
SET IDENTITY_INSERT [dbo].[Accessories] ON;

DECLARE @AccLookup TABLE (
    Id INT PRIMARY KEY,
    Name VARCHAR(80),
    Category VARCHAR(40),
    BasePrice DECIMAL(18,2)
);
INSERT INTO @AccLookup VALUES
(1, 'All-Weather Premium 3D Floor Liners', 'Interior Protection', 5499.00),
(2, 'Dashcam 4K UHD Front & Rear with GPS', 'Electronics & Safety', 18999.00),
(3, 'Carbon Fiber Aerodynamic Rear Spoiler', 'Exterior Styling', 24500.00),
(4, 'Ceramic Pro 9H Paint Protection Shield', 'Body & Paint Care', 32000.00),
(5, 'Wireless MagSafe Fast Vehicle Charger', 'Interior Convenience', 4299.00),
(6, 'Aero Crossbars Heavy-Duty Roof Rack', 'Utility & Travel', 14500.00),
(7, 'Illuminated Door Sill Guards with Emblem', 'Interior Styling', 6800.00),
(8, 'High-Performance Brembo Brake Caliper Kit', 'Performance Upgrades', 68000.00),
(9, 'Nappa Leather Custom Fitted Seat Covers', 'Interior Luxury', 28500.00),
(10, 'Front Bumper Guard with LED DRL', 'Exterior Protection', 12900.00),
(11, 'Motorized Retractable Side Step Runners', 'Convenience & Access', 45000.00),
(12, 'Dynamic Ambient LED Interior Lighting Kit', 'Interior Ambiance', 8500.00),
(13, 'Underbody Anti-Rust Corrosion Protection', 'Vehicle Maintenance', 7500.00),
(14, 'Sunshade Magnetic Snap-On Privacy Blinds', 'Interior Comfort', 3499.00),
(15, 'Heavy Duty Mud Guards Front & Rear', 'Exterior Protection', 2199.00),
(16, 'Sport Alloy Wheels 19-Inch Diamond Cut', 'Wheels & Tires', 82000.00),
(17, 'Tire Pressure Monitoring System (TPMS) Pro', 'Safety & Electronics', 9200.00),
(18, 'Acoustic Soundproofing Insulation Package', 'Cabin Comfort', 16500.00),
(19, 'Integrated Heads-Up Display (HUD) Module', 'Electronics & Tech', 21500.00),
(20, 'Digital Smart Rearview Mirror with Camera', 'Electronics & Vision', 15999.00);

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[Accessories] (
    [Id], [Code], [Name], [Description], [Price], [Currency], [AvailableQuantity],
    [Status], [CreatedAtUtc], [UpdatedAtUtc]
)
SELECT 
    n.N,
    CONCAT('ACC-', RIGHT('00000' + CAST(n.N AS VARCHAR(6)), 6)),
    CONCAT(a.Name, ' [Edition #', RIGHT('000' + CAST(n.N AS VARCHAR(4)), 4), ']'),
    CONCAT('Genuine OEM certified automotive accessory classified under ', a.Category, '. Engineered with weather-resistant materials and plug-and-play installation.'),
    CAST(a.BasePrice + ((n.N * 387) % 5000) AS DECIMAL(18,2)),
    'INR',
    20 + (n.N % 180), -- In stock 20..200 units
    1, -- Active
    DATEADD(DAY, - (n.N % 40), '2026-10-28 12:00:00'),
    NULL
FROM Numbers n
JOIN @AccLookup a ON a.Id = ((n.N - 1) % 20) + 1;

SET IDENTITY_INSERT [dbo].[Accessories] OFF;
GO

-- 4.2 Insert 1,000 AccessoryCompatibilities (Mapped 1-to-1 to Accessories and ItemId)
SET IDENTITY_INSERT [dbo].[AccessoryCompatibilities] ON;

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[AccessoryCompatibilities] (
    [Id], [AccessoryId], [ItemId], [SKU], [IsActive], [CreatedAtUtc], [UpdatedAtUtc]
)
SELECT 
    n.N,
    n.N, -- AccessoryId 1..1000
    n.N, -- ItemId 1..1000
    CONCAT('VEH-SKU-', RIGHT('00000' + CAST(n.N AS VARCHAR(6)), 6)),
    1, -- Active
    DATEADD(DAY, - (n.N % 30), '2026-10-28 12:00:00'),
    NULL
FROM Numbers n;

SET IDENTITY_INSERT [dbo].[AccessoryCompatibilities] OFF;
GO

-- 5. Verification Query
SELECT 'Accessories' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Accessories]
UNION ALL
SELECT 'AccessoryCompatibilities', COUNT(*) FROM [dbo].[AccessoryCompatibilities];
GO
