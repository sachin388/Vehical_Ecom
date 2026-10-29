-- ==================================================================================
-- Service: Items (Vehicle Catalog Service)
-- Target Database: VehicleEcommerceDev (or ItemsCatalogDb)
-- Description: DDL & realistic seed data generation (1,000 records per table)
-- Engine: Microsoft SQL Server (Transact-SQL)
-- ==================================================================================

SET NOCOUNT ON;
GO

-- 1. Create Database if not exists
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'VehicleEcommerceDev')
BEGIN
    CREATE DATABASE [VehicleEcommerceDev];
END
GO

USE [VehicleEcommerceDev];
GO

-- 2. Drop legacy or dependent foreign keys if table exists
IF OBJECT_ID(N'[dbo].[FK_Items_VehicleCategories_VehicleCategoryId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Items] DROP CONSTRAINT [FK_Items_VehicleCategories_VehicleCategoryId];
IF OBJECT_ID(N'[dbo].[FK_Items_VehicleVariants_VehicleVariantId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Items] DROP CONSTRAINT [FK_Items_VehicleVariants_VehicleVariantId];
IF OBJECT_ID(N'[dbo].[FK_Items_VehicleModels_VehicleModelId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Items] DROP CONSTRAINT [FK_Items_VehicleModels_VehicleModelId];
IF OBJECT_ID(N'[dbo].[FK_Items_VehicleMakes_VehicleMakeId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Items] DROP CONSTRAINT [FK_Items_VehicleMakes_VehicleMakeId];
IF OBJECT_ID(N'[dbo].[FK_VehicleSpecifications_VehicleVariants_VehicleVariantId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[VehicleSpecifications] DROP CONSTRAINT [FK_VehicleSpecifications_VehicleVariants_VehicleVariantId];
IF OBJECT_ID(N'[dbo].[FK_VehicleVariants_VehicleModels_VehicleModelId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[VehicleVariants] DROP CONSTRAINT [FK_VehicleVariants_VehicleModels_VehicleModelId];
IF OBJECT_ID(N'[dbo].[FK_VehicleModels_VehicleMakes_VehicleMakeId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[VehicleModels] DROP CONSTRAINT [FK_VehicleModels_VehicleMakes_VehicleMakeId];
GO

-- Drop tables if need schema realignment
IF OBJECT_ID(N'[dbo].[VehicleSpecifications]', 'U') IS NOT NULL DROP TABLE [dbo].[VehicleSpecifications];
IF OBJECT_ID(N'[dbo].[Items]', 'U') IS NOT NULL DROP TABLE [dbo].[Items];
IF OBJECT_ID(N'[dbo].[VehicleVariants]', 'U') IS NOT NULL DROP TABLE [dbo].[VehicleVariants];
IF OBJECT_ID(N'[dbo].[VehicleModels]', 'U') IS NOT NULL DROP TABLE [dbo].[VehicleModels];
IF OBJECT_ID(N'[dbo].[VehicleMakes]', 'U') IS NOT NULL DROP TABLE [dbo].[VehicleMakes];
IF OBJECT_ID(N'[dbo].[VehicleCategories]', 'U') IS NOT NULL DROP TABLE [dbo].[VehicleCategories];
GO

-- 3. Create Tables
CREATE TABLE [dbo].[VehicleCategories] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_VehicleCategories_IsActive] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_VehicleCategories_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_VehicleCategories] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_VehicleCategories_Name] ON [dbo].[VehicleCategories] ([Name] ASC);
GO

CREATE TABLE [dbo].[VehicleMakes] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_VehicleMakes_IsActive] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_VehicleMakes_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_VehicleMakes] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_VehicleMakes_Name] ON [dbo].[VehicleMakes] ([Name] ASC);
GO

CREATE TABLE [dbo].[VehicleModels] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [VehicleMakeId] INT NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_VehicleModels_IsActive] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_VehicleModels_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_VehicleModels] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_VehicleModels_VehicleMakes_VehicleMakeId] FOREIGN KEY ([VehicleMakeId]) 
        REFERENCES [dbo].[VehicleMakes] ([Id])
);
CREATE NONCLUSTERED INDEX [IX_VehicleModels_VehicleMakeId] ON [dbo].[VehicleModels] ([VehicleMakeId] ASC);
GO

CREATE TABLE [dbo].[VehicleVariants] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [VehicleModelId] INT NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [VariantCode] NVARCHAR(100) NOT NULL,
    [Engine] NVARCHAR(MAX) NULL,
    [Transmission] NVARCHAR(MAX) NULL,
    [EngineCapacity] DECIMAL(18,2) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_VehicleVariants_IsActive] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_VehicleVariants_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_VehicleVariants] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_VehicleVariants_VehicleModels_VehicleModelId] FOREIGN KEY ([VehicleModelId]) 
        REFERENCES [dbo].[VehicleModels] ([Id])
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_VehicleVariants_VariantCode] ON [dbo].[VehicleVariants] ([VariantCode] ASC);
CREATE NONCLUSTERED INDEX [IX_VehicleVariants_VehicleModelId] ON [dbo].[VehicleVariants] ([VehicleModelId] ASC);
GO

CREATE TABLE [dbo].[VehicleSpecifications] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [VehicleVariantId] INT NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Value] NVARCHAR(500) NOT NULL,
    CONSTRAINT [PK_VehicleSpecifications] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_VehicleSpecifications_VehicleVariants_VehicleVariantId] FOREIGN KEY ([VehicleVariantId]) 
        REFERENCES [dbo].[VehicleVariants] ([Id]) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX [IX_VehicleSpecifications_VehicleVariantId] ON [dbo].[VehicleSpecifications] ([VehicleVariantId] ASC);
GO

CREATE TABLE [dbo].[Items] (
    [ItemId] INT IDENTITY(1,1) NOT NULL,
    [SKU] NVARCHAR(100) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [VehicleMakeId] INT NOT NULL,
    [VehicleModelId] INT NOT NULL,
    [VehicleVariantId] INT NOT NULL,
    [VehicleCategoryId] INT NOT NULL,
    [Status] INT NOT NULL, -- 1=Active, 2=Inactive, 3=Discontinued
    [Description] NVARCHAR(2000) NULL,
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Items_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_Items] PRIMARY KEY CLUSTERED ([ItemId] ASC),
    CONSTRAINT [FK_Items_VehicleMakes_VehicleMakeId] FOREIGN KEY ([VehicleMakeId]) 
        REFERENCES [dbo].[VehicleMakes] ([Id]),
    CONSTRAINT [FK_Items_VehicleModels_VehicleModelId] FOREIGN KEY ([VehicleModelId]) 
        REFERENCES [dbo].[VehicleModels] ([Id]),
    CONSTRAINT [FK_Items_VehicleVariants_VehicleVariantId] FOREIGN KEY ([VehicleVariantId]) 
        REFERENCES [dbo].[VehicleVariants] ([Id]),
    CONSTRAINT [FK_Items_VehicleCategories_VehicleCategoryId] FOREIGN KEY ([VehicleCategoryId]) 
        REFERENCES [dbo].[VehicleCategories] ([Id])
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Items_SKU] ON [dbo].[Items] ([SKU] ASC);
GO

-- 4. Seed Data (1,000 Records per table)
-- 4.1 Insert 1,000 VehicleCategories
SET IDENTITY_INSERT [dbo].[VehicleCategories] ON;

DECLARE @CatLookup TABLE (Id INT PRIMARY KEY, Name VARCHAR(50));
INSERT INTO @CatLookup VALUES 
(1, 'Compact Sedan'), (2, 'Executive Luxury Sedan'), (3, 'Compact Crossover SUV'), (4, 'Mid-Size Family SUV'),
(5, 'Full-Size Flagship SUV'), (6, 'Electric Performance Sedan'), (7, 'Electric Adventure SUV'), (8, 'Sports Coupe'),
(9, 'Luxury Grand Tourer'), (10, 'Track Supercar'), (11, 'Heavy-Duty Pickup Truck'), (12, 'Urban Electric Hatch'),
(13, 'Convertible Roadster'), (14, 'Commercial Cargo Van'), (15, 'Multi-Purpose Passenger MPV'), (16, 'Off-Road 4x4'),
(17, 'Station Wagon Estate'), (18, 'Shooting Brake Performance'), (19, 'Plug-In Hybrid SUV'), (20, 'Hypercar Limited');

DECLARE @TierLookup TABLE (Id INT PRIMARY KEY, Name VARCHAR(30));
INSERT INTO @TierLookup VALUES 
(1, 'Standard Tier'), (2, 'Premium Edition'), (3, 'Elite Class'), (4, 'Signature Series'), (5, 'Sport Dynamics'),
(6, 'All-Terrain Spec'), (7, 'Urban Mobility'), (8, 'Long Range Fleet'), (9, 'First Edition'), (10, 'Executive Spec');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[VehicleCategories] ([Id], [Name], [Description], [IsActive], [CreatedAtUtc])
SELECT 
    n.N,
    CONCAT(c.Name, ' [', t.Name, ' #', RIGHT('000' + CAST(n.N AS VARCHAR(4)), 4), ']'),
    CONCAT('Vehicle category classification for ', c.Name, ' vehicles engineered for consumer and commercial distribution.'),
    1,
    DATEADD(DAY, -n.N, '2026-10-28 12:00:00')
FROM Numbers n
JOIN @CatLookup c ON c.Id = ((n.N - 1) % 20) + 1
JOIN @TierLookup t ON t.Id = (((n.N - 1) / 20) % 10) + 1;

SET IDENTITY_INSERT [dbo].[VehicleCategories] OFF;
GO

-- 4.2 Insert 1,000 VehicleMakes
SET IDENTITY_INSERT [dbo].[VehicleMakes] ON;

DECLARE @MakeLookup TABLE (Id INT PRIMARY KEY, Name VARCHAR(50));
INSERT INTO @MakeLookup VALUES
(1, 'Toyota'), (2, 'Honda'), (3, 'Ford'), (4, 'BMW'), (5, 'Mercedes-Benz'),
(6, 'Audi'), (7, 'Porsche'), (8, 'Tesla'), (9, 'Hyundai'), (10, 'Kia'),
(11, 'Volkswagen'), (12, 'Volvo'), (13, 'Lexus'), (14, 'Chevrolet'), (15, 'Nissan'),
(16, 'Mazda'), (17, 'Subaru'), (18, 'Land Rover'), (19, 'Jaguar'), (20, 'Jeep'),
(21, 'Ferrari'), (22, 'Lamborghini'), (23, 'Maserati'), (24, 'Alfa Romeo'), (25, 'Genesis'),
(26, 'Dodge'), (27, 'Ram'), (28, 'Suzuki'), (29, 'Tata Motors'), (30, 'Mahindra'),
(31, 'Aston Martin'), (32, 'Bentley'), (33, 'Rolls-Royce'), (34, 'McLaren'), (35, 'Bugatti'),
(36, 'Lucid'), (37, 'Rivian'), (38, 'Polestar'), (39, 'BYD'), (40, 'Skoda'),
(41, 'Renault'), (42, 'Peugeot'), (43, 'Mini'), (44, 'Acura'), (45, 'Infiniti'),
(46, 'Lincoln'), (47, 'Cadillac'), (48, 'GMC'), (49, 'Chrysler'), (50, 'Citroen');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[VehicleMakes] ([Id], [Name], [Description], [IsActive], [CreatedAtUtc])
SELECT 
    n.N,
    CASE 
        WHEN n.N <= 50 THEN m.Name
        ELSE CONCAT(m.Name, ' Auto Group (Div ', RIGHT('000' + CAST(n.N AS VARCHAR(4)), 4), ')')
    END,
    CONCAT('Official vehicle manufacturer and global distribution subsidiary for ', m.Name, '.'),
    1,
    DATEADD(DAY, -n.N, '2026-10-28 12:00:00')
FROM Numbers n
JOIN @MakeLookup m ON m.Id = ((n.N - 1) % 50) + 1;

SET IDENTITY_INSERT [dbo].[VehicleMakes] OFF;
GO

-- 4.3 Insert 1,000 VehicleModels
SET IDENTITY_INSERT [dbo].[VehicleModels] ON;

DECLARE @ModelLookup TABLE (Id INT PRIMARY KEY, Name VARCHAR(50));
INSERT INTO @ModelLookup VALUES
(1, 'Camry'), (2, 'Civic'), (3, 'Mustang'), (4, '3 Series'), (5, 'C-Class'),
(6, 'A4'), (7, '911 Carrera'), (8, 'Model Y'), (9, 'Tucson'), (10, 'Sportage'),
(11, 'Golf'), (12, 'XC60'), (13, 'RX 350'), (14, 'Silverado'), (15, 'Altima'),
(16, 'CX-5'), (17, 'Outback'), (18, 'Defender'), (19, 'F-Pace'), (20, 'Wrangler'),
(21, '296 GTB'), (22, 'Huracan'), (23, 'Grecale'), (24, 'Giulia'), (25, 'GV70'),
(26, 'Challenger'), (27, '1500 TRX'), (28, 'Swift'), (29, 'Nexon EV'), (30, 'XUV700'),
(31, 'DB12'), (32, 'Continental GT'), (33, 'Ghost'), (34, '750S'), (35, 'Chiron'),
(36, 'Air Sapphire'), (37, 'R1S'), (38, 'Polestar 3'), (39, 'Seal'), (40, 'Octavia RS'),
(41, 'Megane E-Tech'), (42, '3008'), (43, 'Cooper SE'), (44, 'Integra Type S'), (45, 'QX60'),
(46, 'Aviator'), (47, 'Escalade'), (48, 'Sierra AT4'), (49, 'Pacifica'), (50, 'C5 Aircross');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[VehicleModels] ([Id], [VehicleMakeId], [Name], [Description], [IsActive], [CreatedAtUtc])
SELECT 
    n.N,
    n.N, -- 1-to-1 with VehicleMakeId
    CASE 
        WHEN n.N <= 50 THEN m.Name
        ELSE CONCAT(m.Name, ' Gen-', RIGHT('000' + CAST(n.N AS VARCHAR(4)), 4))
    END,
    CONCAT('Production automotive model line for ', m.Name, ' with advanced chassis and cockpit technology.'),
    1,
    DATEADD(DAY, -n.N, '2026-10-28 12:00:00')
FROM Numbers n
JOIN @ModelLookup m ON m.Id = ((n.N - 1) % 50) + 1;

SET IDENTITY_INSERT [dbo].[VehicleModels] OFF;
GO

-- 4.4 Insert 1,000 VehicleVariants
SET IDENTITY_INSERT [dbo].[VehicleVariants] ON;

DECLARE @VarLookup TABLE (Id INT PRIMARY KEY, Name VARCHAR(50), Engine VARCHAR(80), Trans VARCHAR(80), Cc DECIMAL(18,2));
INSERT INTO @VarLookup VALUES
(1, 'LE Front-Wheel Drive', '2.5L Dynamic Force Inline-4', 'Electronic Continuously Variable Transmission (e-CVT)', 2.5),
(2, 'XLE AWD Hybrid', '2.5L Hybrid Atkinson-Cycle 4-Cyl', 'e-CVT Intelligent AWD', 2.5),
(3, 'M Sport xDrive TwinPower', '3.0L BMW TwinPower Turbo Inline-6', '8-Speed Steptronic Sport Automatic', 3.0),
(4, 'AMG Performance Biturbo', '4.0L V8 Biturbo Handcrafted', 'AMG SPEEDSHIFT MCT 9-Speed', 4.0),
(5, 'Quattro Prestige S-Tronic', '2.0L TFSI Turbocharged Inline-4', '7-Speed S tronic Dual-Clutch', 2.0),
(6, 'Long Range Dual Motor AWD', 'Dual Permanent Magnet Electric Motors', 'Single-Speed Direct Drive Electric', 0.0),
(7, 'Performance Plaid Tri-Motor', 'Tri-Motor Ultra Performance All-Wheel Drive', 'Single-Speed Fixed Ratio', 0.0),
(8, 'Turbo S E-Hybrid PDK', '4.0L Twin-Turbo V8 + Electric Motor', '8-Speed Porsche Doppelkupplung (PDK)', 4.0),
(9, 'GT-Line Turbo AWD', '1.6L Smartstream Turbo GDI', '8-Speed Automatic Transmission', 1.6),
(10, 'Fearless+ Long Range EV', 'Permanent Magnet Synchronous Motor (40.5 kWh)', 'Automatic Single Speed EV', 0.0),
(11, 'AX7 Luxury Pack Diesel 4WD', '2.2L mHawk Turbocharged CRDe Diesel', '6-Speed Torque Converter Automatic', 2.2),
(12, 'Rubicon 4x4 Off-Road', '3.6L Pentastar V6 with eTorque', '8-Speed Automatic 4x4 with Rock-Trac', 3.6),
(13, 'Black Edition Twin-Turbo', '3.5L Twin-Turbo EcoBoost V6', '10-Speed SelectShift Automatic', 3.5),
(14, 'Sport Touring 6-Speed', '1.5L Turbocharged 16-Valve DOHC', '6-Speed Close-Ratio Manual Transmission', 1.5),
(15, 'Limited Track Carbon Pack', '5.2L Supercharged Cross-Plane V8', '7-Speed Dual-Clutch Tremec Transmission', 5.2);

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[VehicleVariants] ([Id], [VehicleModelId], [Name], [VariantCode], [Engine], [Transmission], [EngineCapacity], [IsActive], [CreatedAtUtc])
SELECT 
    n.N,
    n.N, -- Mapped 1-to-1 to VehicleModelId
    CONCAT(v.Name, ' (Trim #', RIGHT('000' + CAST(n.N AS VARCHAR(4)), 4), ')'),
    CONCAT('VAR-', RIGHT('00000' + CAST(n.N AS VARCHAR(6)), 6)),
    v.Engine,
    v.Trans,
    v.Cc,
    1,
    DATEADD(DAY, -n.N, '2026-10-28 12:00:00')
FROM Numbers n
JOIN @VarLookup v ON v.Id = ((n.N - 1) % 15) + 1;

SET IDENTITY_INSERT [dbo].[VehicleVariants] OFF;
GO

-- 4.5 Insert 1,000 VehicleSpecifications
SET IDENTITY_INSERT [dbo].[VehicleSpecifications] ON;

DECLARE @SpecLookup TABLE (Id INT PRIMARY KEY, Name VARCHAR(50), Value VARCHAR(100));
INSERT INTO @SpecLookup VALUES
(1, 'Peak Horsepower', '375 HP @ 5800 RPM'),
(2, 'Peak Torque', '450 lb-ft (610 Nm) @ 1600 RPM'),
(3, 'Acceleration 0-100 km/h', '4.2 seconds'),
(4, 'Top Speed', '155 mph (250 km/h) electronically limited'),
(5, 'EPA / WLTP Combined Range', '320 miles (515 km) combined WLTP'),
(6, 'Battery / Fuel Capacity', '82.0 kWh usable Lithium-Ion Pack'),
(7, 'Drivetrain Configuration', 'Intelligent All-Wheel Drive (AWD)'),
(8, 'Curb Weight & Payload', '2,150 kg (4,740 lbs) Curb Weight'),
(9, 'Seating Layout', '5-Passenger Ergonomic Sport Seating'),
(10, 'Fast Charge Capability', '10% to 80% in 18 minutes via 350kW DC');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[VehicleSpecifications] ([Id], [VehicleVariantId], [Name], [Value])
SELECT 
    n.N,
    n.N, -- Mapped to VehicleVariantId 1..1000
    s.Name,
    s.Value
FROM Numbers n
JOIN @SpecLookup s ON s.Id = ((n.N - 1) % 10) + 1;

SET IDENTITY_INSERT [dbo].[VehicleSpecifications] OFF;
GO

-- 4.6 Insert 1,000 Items (Vehicle Catalog Entries)
SET IDENTITY_INSERT [dbo].[Items] ON;

DECLARE @MakeNames TABLE (Id INT PRIMARY KEY, Name VARCHAR(50));
INSERT INTO @MakeNames VALUES
(1, 'Toyota'), (2, 'Honda'), (3, 'Ford'), (4, 'BMW'), (5, 'Mercedes-Benz'),
(6, 'Audi'), (7, 'Porsche'), (8, 'Tesla'), (9, 'Hyundai'), (10, 'Kia'),
(11, 'Volkswagen'), (12, 'Volvo'), (13, 'Lexus'), (14, 'Chevrolet'), (15, 'Nissan'),
(16, 'Mazda'), (17, 'Subaru'), (18, 'Land Rover'), (19, 'Jaguar'), (20, 'Jeep'),
(21, 'Ferrari'), (22, 'Lamborghini'), (23, 'Maserati'), (24, 'Alfa Romeo'), (25, 'Genesis'),
(26, 'Dodge'), (27, 'Ram'), (28, 'Suzuki'), (29, 'Tata Motors'), (30, 'Mahindra'),
(31, 'Aston Martin'), (32, 'Bentley'), (33, 'Rolls-Royce'), (34, 'McLaren'), (35, 'Bugatti'),
(36, 'Lucid'), (37, 'Rivian'), (38, 'Polestar'), (39, 'BYD'), (40, 'Skoda'),
(41, 'Renault'), (42, 'Peugeot'), (43, 'Mini'), (44, 'Acura'), (45, 'Infiniti'),
(46, 'Lincoln'), (47, 'Cadillac'), (48, 'GMC'), (49, 'Chrysler'), (50, 'Citroen');

DECLARE @ModelNames TABLE (Id INT PRIMARY KEY, Name VARCHAR(50));
INSERT INTO @ModelNames VALUES
(1, 'Camry'), (2, 'Civic'), (3, 'Mustang'), (4, '3 Series'), (5, 'C-Class'),
(6, 'A4'), (7, '911 Carrera'), (8, 'Model Y'), (9, 'Tucson'), (10, 'Sportage'),
(11, 'Golf'), (12, 'XC60'), (13, 'RX 350'), (14, 'Silverado'), (15, 'Altima'),
(16, 'CX-5'), (17, 'Outback'), (18, 'Defender'), (19, 'F-Pace'), (20, 'Wrangler'),
(21, '296 GTB'), (22, 'Huracan'), (23, 'Grecale'), (24, 'Giulia'), (25, 'GV70'),
(26, 'Challenger'), (27, '1500 TRX'), (28, 'Swift'), (29, 'Nexon EV'), (30, 'XUV700'),
(31, 'DB12'), (32, 'Continental GT'), (33, 'Ghost'), (34, '750S'), (35, 'Chiron'),
(36, 'Air Sapphire'), (37, 'R1S'), (38, 'Polestar 3'), (39, 'Seal'), (40, 'Octavia RS'),
(41, 'Megane E-Tech'), (42, '3008'), (43, 'Cooper SE'), (44, 'Integra Type S'), (45, 'QX60'),
(46, 'Aviator'), (47, 'Escalade'), (48, 'Sierra AT4'), (49, 'Pacifica'), (50, 'C5 Aircross');

DECLARE @VarNames TABLE (Id INT PRIMARY KEY, Name VARCHAR(50));
INSERT INTO @VarNames VALUES
(1, 'LE FWD'), (2, 'XLE AWD Hybrid'), (3, 'M Sport xDrive'), (4, 'AMG Biturbo'),
(5, 'Quattro Prestige'), (6, 'Long Range Dual Motor'), (7, 'Plaid Tri-Motor'),
(8, 'Turbo S E-Hybrid'), (9, 'GT-Line Turbo AWD'), (10, 'Fearless+ EV Long Range'),
(11, 'AX7 Luxury Pack 4WD'), (12, 'Rubicon 4x4 Off-Road'), (13, 'Black Edition Twin-Turbo'),
(14, 'Sport Touring Manual'), (15, 'Carbon Track Pack');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[Items] (
    [ItemId], [SKU], [Name], [VehicleMakeId], [VehicleModelId], [VehicleVariantId], [VehicleCategoryId],
    [Status], [Description], [CreatedAtUtc], [UpdatedAtUtc]
)
SELECT 
    n.N,
    CONCAT('VEH-SKU-', RIGHT('00000' + CAST(n.N AS VARCHAR(6)), 6)),
    CONCAT('2026 ', mk.Name, ' ', md.Name, ' ', vr.Name),
    n.N, -- VehicleMakeId
    n.N, -- VehicleModelId
    n.N, -- VehicleVariantId
    ((n.N - 1) % 1000) + 1, -- VehicleCategoryId
    1, -- Active
    CONCAT('Brand new 2026 model year ', mk.Name, ' ', md.Name, ' featuring factory warranty, connected telematics, and tailored cabin ergonomics.'),
    DATEADD(DAY, -n.N, '2026-10-28 12:00:00'),
    DATEADD(HOUR, -n.N, '2026-10-28 12:00:00')
FROM Numbers n
JOIN @MakeNames mk ON mk.Id = ((n.N - 1) % 50) + 1
JOIN @ModelNames md ON md.Id = ((n.N - 1) % 50) + 1
JOIN @VarNames vr ON vr.Id = ((n.N - 1) % 15) + 1;

SET IDENTITY_INSERT [dbo].[Items] OFF;
GO

-- 5. Verification Query
SELECT 'VehicleCategories' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[VehicleCategories]
UNION ALL
SELECT 'VehicleMakes', COUNT(*) FROM [dbo].[VehicleMakes]
UNION ALL
SELECT 'VehicleModels', COUNT(*) FROM [dbo].[VehicleModels]
UNION ALL
SELECT 'VehicleVariants', COUNT(*) FROM [dbo].[VehicleVariants]
UNION ALL
SELECT 'VehicleSpecifications', COUNT(*) FROM [dbo].[VehicleSpecifications]
UNION ALL
SELECT 'Items', COUNT(*) FROM [dbo].[Items];
GO
