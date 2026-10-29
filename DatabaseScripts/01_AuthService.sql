-- ==================================================================================
-- Service: AuthService
-- Target Database: VehicleEcommerceAuthDb (or AuthDb)
-- Description: DDL & realistic seed data generation (1,000 records per table)
-- Engine: Microsoft SQL Server (Transact-SQL)
-- ==================================================================================

SET NOCOUNT ON;
GO

-- 1. Create Database if not exists
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'VehicleEcommerceAuthDb')
BEGIN
    CREATE DATABASE [VehicleEcommerceAuthDb];
END
GO

USE [VehicleEcommerceAuthDb];
GO

-- 2. Create Schema / Tables
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Roles')
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(50) NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Roles_Name] ON [dbo].[Roles] ([Name] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [FirstName] NVARCHAR(100) NOT NULL,
        [LastName] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(255) NOT NULL,
        [PasswordHash] NVARCHAR(MAX) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Users_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAtUtc] DATETIME2(7) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users] ([Email] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'UserRoles')
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [RoleId] UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_UserRoles_RoleId] ON [dbo].[UserRoles] ([RoleId] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'RefreshTokens')
BEGIN
    CREATE TABLE [dbo].[RefreshTokens] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [TokenHash] NVARCHAR(128) NOT NULL,
        [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_RefreshTokens_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [ExpiresAtUtc] DATETIME2(7) NOT NULL,
        [RevokedAtUtc] DATETIME2(7) NULL,
        [ReplacedByTokenId] UNIQUEIDENTIFIER NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_RefreshTokens_TokenHash] ON [dbo].[RefreshTokens] ([TokenHash] ASC);
    CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens] ([UserId] ASC);
END
GO

-- 3. Seed 1,000 Records into Each Table
BEGIN TRANSACTION;

-- Clean existing data if required to reset to exact 1,000 records
DELETE FROM [dbo].[RefreshTokens];
DELETE FROM [dbo].[UserRoles];
DELETE FROM [dbo].[Users];
DELETE FROM [dbo].[Roles];

-- Generate Helper Numbers CTE (1 to 1000)
;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1
    CROSS JOIN Digits d2
    CROSS JOIN Digits d3
),
RoleNames AS (
    SELECT N,
        CASE 
            WHEN N = 1 THEN 'SuperAdmin'
            WHEN N = 2 THEN 'SystemAdministrator'
            WHEN N = 3 THEN 'DealerPrincipal'
            WHEN N = 4 THEN 'DealershipSalesManager'
            WHEN N = 5 THEN 'InventoryManager'
            WHEN N = 6 THEN 'Customer'
            WHEN N = 7 THEN 'FinanceExecutive'
            WHEN N = 8 THEN 'SupportRepresentative'
            WHEN N = 9 THEN 'AuditComplianceOfficer'
            WHEN N = 10 THEN 'LogisticsCoordinator'
            WHEN N = 11 THEN 'ProcurementSpecialist'
            WHEN N = 12 THEN 'WarrantyInspector'
            WHEN N = 13 THEN 'MarketingManager'
            WHEN N = 14 THEN 'RegionalSalesDirector'
            WHEN N = 15 THEN 'FleetOperationsLead'
            WHEN N = 16 THEN 'QualityAssuranceAuditor'
            WHEN N = 17 THEN 'DataProtectionOfficer'
            WHEN N = 18 THEN 'PaymentOperationsAnalyst'
            WHEN N = 19 THEN 'VehicleDeliveryCoordinator'
            WHEN N = 20 THEN 'CustomerSuccessAgent'
            ELSE CONCAT('Role_', 
                CASE (N % 8)
                    WHEN 0 THEN 'CatalogSpecialist_'
                    WHEN 1 THEN 'PriceAnalyst_'
                    WHEN 2 THEN 'InventoryAuditor_'
                    WHEN 3 THEN 'DealerAuditor_'
                    WHEN 4 THEN 'OrderSupervisor_'
                    WHEN 5 THEN 'PaymentApprover_'
                    WHEN 6 THEN 'SupportAgent_'
                    ELSE 'ComplianceLead_'
                END, RIGHT('000' + CAST(N AS VARCHAR(4)), 4))
        END AS RoleName,
        -- Deterministic GUID generation for roles
        CAST(CAST(HASHBYTES('MD5', 'ROLE_' + CAST(N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER) AS RoleGuid
    FROM Numbers
)
INSERT INTO [dbo].[Roles] ([Id], [Name])
SELECT RoleGuid, RoleName
FROM RoleNames;

-- Insert 1,000 Users
;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1
    CROSS JOIN Digits d2
    CROSS JOIN Digits d3
),
UserData AS (
    SELECT N,
        CHOOSE(((N - 1) % 40) + 1,
            'Aarav', 'Vivaan', 'Aditya', 'Vihaan', 'Arjun', 'Sai', 'Reyansh', 'Ayaan', 'Krishna', 'Ishaan',
            'Alex', 'Michael', 'David', 'James', 'Robert', 'John', 'William', 'Daniel', 'Matthew', 'Lucas',
            'Priya', 'Ananya', 'Diya', 'Isha', 'Aadhya', 'Saanvi', 'Kiara', 'Myra', 'Sara', 'Riya',
            'Sophia', 'Emma', 'Olivia', 'Ava', 'Isabella', 'Mia', 'Charlotte', 'Amelia', 'Harper', 'Evelyn'
        ) AS FirstName,
        CHOOSE(((N - 1) % 40) + 1,
            'Sharma', 'Verma', 'Patel', 'Mehta', 'Gupta', 'Singh', 'Kumar', 'Reddy', 'Chopra', 'Iyer',
            'Smith', 'Johnson', 'Williams', 'Brown', 'Jones', 'Garcia', 'Miller', 'Davis', 'Rodriguez', 'Martinez',
            'Deshmukh', 'Kulkarni', 'Joshi', 'Bose', 'Chatterjee', 'Banerjee', 'Nair', 'Pillai', 'Rao', 'Menon',
            'Anderson', 'Taylor', 'Thomas', 'Moore', 'Jackson', 'Martin', 'Lee', 'Perez', 'Thompson', 'White'
        ) AS LastName,
        -- Deterministic GUID for User
        CAST(CAST(HASHBYTES('MD5', 'USER_' + CAST(N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER) AS UserGuid
    FROM Numbers
)
INSERT INTO [dbo].[Users] ([Id], [FirstName], [LastName], [Email], [PasswordHash], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
SELECT 
    UserGuid,
    FirstName,
    LastName,
    LOWER(CONCAT(FirstName, '.', LastName, RIGHT('000' + CAST(N AS VARCHAR(4)), 4), '@vehiclex-ecom.com')),
    -- Realistic BCrypt test hash ($2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy -> Password123!)
    '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy',
    CASE WHEN N % 25 = 0 THEN 0 ELSE 1 END,
    DATEADD(MINUTE, -N * 7, '2026-10-28 12:00:00'),
    CASE WHEN N % 3 = 0 THEN DATEADD(MINUTE, -N * 2, '2026-10-28 12:00:00') ELSE NULL END
FROM UserData;

-- Insert 1,000 UserRoles (1-to-1 mapping user to corresponding role)
;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1
    CROSS JOIN Digits d2
    CROSS JOIN Digits d3
)
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT 
    CAST(CAST(HASHBYTES('MD5', 'USER_' + CAST(N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    CAST(CAST(HASHBYTES('MD5', 'ROLE_' + CAST(N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER)
FROM Numbers;

-- Insert 1,000 RefreshTokens (one per user)
;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1
    CROSS JOIN Digits d2
    CROSS JOIN Digits d3
)
INSERT INTO [dbo].[RefreshTokens] (
    [Id], [UserId], [TokenHash], [CreatedAtUtc], [ExpiresAtUtc], [RevokedAtUtc], [ReplacedByTokenId]
)
SELECT 
    CAST(CAST(HASHBYTES('MD5', 'REFRESH_TOKEN_' + CAST(N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    CAST(CAST(HASHBYTES('MD5', 'USER_' + CAST(N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'REFRESH_TOKEN_SEED_STRING_' + CAST(N AS VARCHAR(10))), 2),
    DATEADD(DAY, - (N % 14), '2026-10-28 12:00:00'),
    DATEADD(DAY, 7 - (N % 14), '2026-10-28 12:00:00'),
    CASE WHEN N % 7 = 0 THEN DATEADD(DAY, -1, '2026-10-28 12:00:00') ELSE NULL END,
    NULL
FROM Numbers;

COMMIT TRANSACTION;
GO

-- 4. Verification Query
SELECT 'Roles' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Roles]
UNION ALL
SELECT 'Users', COUNT(*) FROM [dbo].[Users]
UNION ALL
SELECT 'UserRoles', COUNT(*) FROM [dbo].[UserRoles]
UNION ALL
SELECT 'RefreshTokens', COUNT(*) FROM [dbo].[RefreshTokens];
GO
