-- ==================================================================================
-- Service: Dealer Service
-- Target Database: DealerDb
-- Description: DDL & realistic seed data generation (1,000 records per table)
-- Engine: Microsoft SQL Server (Transact-SQL)
-- ==================================================================================

SET NOCOUNT ON;
GO

-- 1. Create Database if not exists
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'DealerDb')
BEGIN
    CREATE DATABASE [DealerDb];
END
GO

USE [DealerDb];
GO

-- 2. Drop existing foreign keys & tables if recreating
IF OBJECT_ID(N'[dbo].[FK_DealerBranches_Dealers_DealerId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[DealerBranches] DROP CONSTRAINT [FK_DealerBranches_Dealers_DealerId];
IF OBJECT_ID(N'[dbo].[FK_DealerInventories_Dealers_DealerId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[DealerInventories] DROP CONSTRAINT [FK_DealerInventories_Dealers_DealerId];
GO

IF OBJECT_ID(N'[dbo].[DealerOrderAssignments]', 'U') IS NOT NULL DROP TABLE [dbo].[DealerOrderAssignments];
IF OBJECT_ID(N'[dbo].[DealerInventories]', 'U') IS NOT NULL DROP TABLE [dbo].[DealerInventories];
IF OBJECT_ID(N'[dbo].[DealerBranches]', 'U') IS NOT NULL DROP TABLE [dbo].[DealerBranches];
IF OBJECT_ID(N'[dbo].[Dealers]', 'U') IS NOT NULL DROP TABLE [dbo].[Dealers];
GO

-- 3. Create Tables
CREATE TABLE [dbo].[Dealers] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [DealerCode] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL,
    [Phone] NVARCHAR(50) NOT NULL,
    [Status] INT NOT NULL CONSTRAINT [DF_Dealers_Status] DEFAULT (1), -- 1=Active, 2=Inactive, 3=Suspended
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Dealers_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_Dealers] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Dealers_DealerCode] ON [dbo].[Dealers] ([DealerCode] ASC);
GO

CREATE TABLE [dbo].[DealerBranches] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [DealerId] BIGINT NOT NULL,
    [BranchCode] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [AddressLine] NVARCHAR(300) NOT NULL,
    [City] NVARCHAR(100) NOT NULL,
    [StateCode] NVARCHAR(10) NOT NULL,
    [PostalCode] NVARCHAR(20) NOT NULL,
    [Latitude] DECIMAL(10,7) NULL,
    [Longitude] DECIMAL(10,7) NULL,
    [ContactName] NVARCHAR(150) NOT NULL,
    [ContactPhone] NVARCHAR(50) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_DealerBranches_IsActive] DEFAULT (1),
    CONSTRAINT [PK_DealerBranches] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_DealerBranches_Dealers_DealerId] FOREIGN KEY ([DealerId]) 
        REFERENCES [dbo].[Dealers] ([Id]) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX [IX_DealerBranches_DealerId] ON [dbo].[DealerBranches] ([DealerId] ASC);
GO

CREATE TABLE [dbo].[DealerInventories] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [DealerId] BIGINT NOT NULL,
    [ItemId] INT NOT NULL,
    [SKU] NVARCHAR(100) NOT NULL,
    [Quantity] INT NOT NULL,
    [ReservedQuantity] INT NOT NULL CONSTRAINT [DF_DealerInventories_ReservedQuantity] DEFAULT (0),
    [UpdatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_DealerInventories_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_DealerInventories] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_DealerInventories_Dealers_DealerId] FOREIGN KEY ([DealerId]) 
        REFERENCES [dbo].[Dealers] ([Id]) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX [IX_DealerInventories_DealerId_ItemId] ON [dbo].[DealerInventories] ([DealerId] ASC, [ItemId] ASC);
GO

CREATE TABLE [dbo].[DealerOrderAssignments] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [SalesOrderId] UNIQUEIDENTIFIER NOT NULL,
    [DealerId] BIGINT NOT NULL,
    [DealerBranchId] BIGINT NULL,
    [ItemId] INT NOT NULL,
    [SKU] NVARCHAR(100) NOT NULL,
    [Quantity] INT NOT NULL,
    [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_DealerOrderAssignments_Status] DEFAULT ('Assigned'),
    [AssignedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_DealerOrderAssignments_AssignedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_DealerOrderAssignments] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE NONCLUSTERED INDEX [IX_DealerOrderAssignments_SalesOrderId] ON [dbo].[DealerOrderAssignments] ([SalesOrderId] ASC);
GO

-- 4. Seed Data (1,000 Records per table)
-- 4.1 Insert 1,000 Dealers
SET IDENTITY_INSERT [dbo].[Dealers] ON;

DECLARE @DealerNames TABLE (Id INT PRIMARY KEY, Name VARCHAR(60));
INSERT INTO @DealerNames VALUES
(1, 'Apex Auto City'), (2, 'Metro Star Motors'), (3, 'Prestige Automotive Group'),
(4, 'Pinnacle Wheels Center'), (5, 'Horizon Motor Gallery'), (6, 'Silverstone Speedworks'),
(7, 'Heritage Wheels Hub'), (8, 'Elite Auto Gallerias'), (9, 'Frontier Mobility Networks'),
(10, 'Grand Highway Auto Group'), (11, 'Vanguard Automotive Prime'), (12, 'Paramount Motors Gateway');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[Dealers] (
    [Id], [DealerCode], [Name], [Email], [Phone], [Status], [CreatedAtUtc], [UpdatedAtUtc]
)
SELECT 
    n.N,
    CONCAT('DLR-', RIGHT('0000' + CAST(n.N AS VARCHAR(4)), 4)),
    CONCAT(dn.Name, ' (Hub ', RIGHT('000' + CAST(n.N AS VARCHAR(4)), 4), ')'),
    CONCAT('dealer.', RIGHT('0000' + CAST(n.N AS VARCHAR(4)), 4), '@dealerships-ecom.com'),
    CONCAT('+91-98', RIGHT('00000000' + CAST((n.N * 38291) % 99999999 AS VARCHAR(8)), 8)),
    1, -- Active
    DATEADD(DAY, - (n.N % 60), '2026-10-28 12:00:00'),
    NULL
FROM Numbers n
JOIN @DealerNames dn ON dn.Id = ((n.N - 1) % 12) + 1;

SET IDENTITY_INSERT [dbo].[Dealers] OFF;
GO

-- 4.2 Insert 1,000 DealerBranches (Mapped 1-to-1 to Dealers)
SET IDENTITY_INSERT [dbo].[DealerBranches] ON;

DECLARE @CityLookup TABLE (
    Id INT PRIMARY KEY,
    City VARCHAR(40),
    StateCode VARCHAR(5),
    PostalBase INT,
    Lat DECIMAL(10,7),
    Lon DECIMAL(10,7)
);
INSERT INTO @CityLookup VALUES
(1, 'Mumbai', 'MH', 400001, 18.9220000, 72.8347000),
(2, 'Bengaluru', 'KA', 560001, 12.9716000, 77.5946000),
(3, 'New Delhi', 'DL', 110001, 28.6139000, 77.2090000),
(4, 'Hyderabad', 'TS', 500001, 17.3850000, 78.4867000),
(5, 'Chennai', 'TN', 600001, 13.0827000, 80.2707000),
(6, 'Pune', 'MH', 411001, 18.5204000, 73.8567000),
(7, 'Ahmedabad', 'GJ', 380001, 23.0225000, 72.5714000),
(8, 'Kolkata', 'WB', 700001, 22.5726000, 88.3639000),
(9, 'Gurgaon', 'HR', 122001, 28.4595000, 77.0266000),
(10, 'Noida', 'UP', 201301, 28.5355000, 77.3910000);

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[DealerBranches] (
    [Id], [DealerId], [BranchCode], [Name], [AddressLine], [City], [StateCode],
    [PostalCode], [Latitude], [Longitude], [ContactName], [ContactPhone], [IsActive]
)
SELECT 
    n.N,
    n.N, -- DealerId 1..1000
    CONCAT('BR-', RIGHT('0000' + CAST(n.N AS VARCHAR(4)), 4)),
    CONCAT(c.City, ' Main Auto Plaza (Branch #', RIGHT('000' + CAST(n.N AS VARCHAR(4)), 4), ')'),
    CONCAT('Plot ', (n.N % 300) + 1, ', Automotive Tech Corridor, Sector ', (n.N % 25) + 1),
    c.City,
    c.StateCode,
    CAST(c.PostalBase + (n.N % 90) AS VARCHAR(20)),
    c.Lat + CAST((n.N % 50) * 0.001 AS DECIMAL(10,7)),
    c.Lon + CAST((n.N % 50) * 0.001 AS DECIMAL(10,7)),
    CONCAT('Manager ', CHOOSE(((n.N - 1) % 10) + 1, 'Rajesh', 'Amit', 'Priya', 'Sunil', 'Kiran', 'Deepak', 'Anand', 'Neha', 'Vikram', 'Rohit')),
    CONCAT('+91-97', RIGHT('00000000' + CAST((n.N * 47391) % 99999999 AS VARCHAR(8)), 8)),
    1 -- Active
FROM Numbers n
JOIN @CityLookup c ON c.Id = ((n.N - 1) % 10) + 1;

SET IDENTITY_INSERT [dbo].[DealerBranches] OFF;
GO

-- 4.3 Insert 1,000 DealerInventories (Mapped 1-to-1 to Dealers and ItemId)
SET IDENTITY_INSERT [dbo].[DealerInventories] ON;

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[DealerInventories] (
    [Id], [DealerId], [ItemId], [SKU], [Quantity], [ReservedQuantity], [UpdatedAtUtc]
)
SELECT 
    n.N,
    n.N, -- DealerId 1..1000
    n.N, -- ItemId 1..1000
    CONCAT('VEH-SKU-', RIGHT('00000' + CAST(n.N AS VARCHAR(6)), 6)),
    10 + (n.N % 30), -- In stock 10..40
    (n.N % 5), -- Reserved 0..4
    DATEADD(MINUTE, - (n.N * 15), '2026-10-28 12:00:00')
FROM Numbers n;

SET IDENTITY_INSERT [dbo].[DealerInventories] OFF;
GO

-- 4.4 Insert 1,000 DealerOrderAssignments
SET IDENTITY_INSERT [dbo].[DealerOrderAssignments] ON;

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[DealerOrderAssignments] (
    [Id], [SalesOrderId], [DealerId], [DealerBranchId], [ItemId],
    [SKU], [Quantity], [Status], [AssignedAtUtc], [UpdatedAtUtc]
)
SELECT 
    n.N,
    CAST(CAST(HASHBYTES('MD5', 'SO_ORDER_' + CAST(n.N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    n.N, -- DealerId 1..1000
    n.N, -- DealerBranchId 1..1000
    n.N, -- ItemId 1..1000
    CONCAT('VEH-SKU-', RIGHT('00000' + CAST(n.N AS VARCHAR(6)), 6)),
    1,
    'Assigned',
    DATEADD(MINUTE, - (n.N * 10), '2026-10-28 12:00:00'),
    NULL
FROM Numbers n;

SET IDENTITY_INSERT [dbo].[DealerOrderAssignments] OFF;
GO

-- 5. Verification Query
SELECT 'Dealers' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Dealers]
UNION ALL
SELECT 'DealerBranches', COUNT(*) FROM [dbo].[DealerBranches]
UNION ALL
SELECT 'DealerInventories', COUNT(*) FROM [dbo].[DealerInventories]
UNION ALL
SELECT 'DealerOrderAssignments', COUNT(*) FROM [dbo].[DealerOrderAssignments];
GO
