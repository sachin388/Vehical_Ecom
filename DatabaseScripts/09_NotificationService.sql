-- ==================================================================================
-- Service: NotificationSystem Service
-- Target Database: NotificationDb
-- Description: DDL & realistic seed data generation (1,000 records per table)
-- Engine: Microsoft SQL Server (Transact-SQL)
-- ==================================================================================

SET NOCOUNT ON;
GO

-- 1. Create Database if not exists
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'NotificationDb')
BEGIN
    CREATE DATABASE [NotificationDb];
END
GO

USE [NotificationDb];
GO

-- 2. Drop existing foreign keys & tables if recreating
IF OBJECT_ID(N'[dbo].[FK_Notifications_NotificationTemplates_TemplateId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Notifications] DROP CONSTRAINT [FK_Notifications_NotificationTemplates_TemplateId];
GO

IF OBJECT_ID(N'[dbo].[Notifications]', 'U') IS NOT NULL DROP TABLE [dbo].[Notifications];
IF OBJECT_ID(N'[dbo].[NotificationPreferences]', 'U') IS NOT NULL DROP TABLE [dbo].[NotificationPreferences];
IF OBJECT_ID(N'[dbo].[NotificationTemplates]', 'U') IS NOT NULL DROP TABLE [dbo].[NotificationTemplates];
GO

-- 3. Create Tables
CREATE TABLE [dbo].[NotificationTemplates] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(100) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Channel] NVARCHAR(50) NOT NULL, -- Email, SMS, Push, WhatsApp
    [Subject] NVARCHAR(300) NOT NULL,
    [BodyTemplate] NVARCHAR(MAX) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_NotificationTemplates_IsActive] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_NotificationTemplates_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_NotificationTemplates] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_NotificationTemplates_Code] ON [dbo].[NotificationTemplates] ([Code] ASC);
GO

CREATE TABLE [dbo].[Notifications] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [NotificationReference] NVARCHAR(100) NOT NULL,
    [TemplateId] BIGINT NOT NULL,
    [Recipient] NVARCHAR(255) NOT NULL,
    [Channel] NVARCHAR(50) NOT NULL,
    [Subject] NVARCHAR(300) NOT NULL,
    [Body] NVARCHAR(MAX) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Notifications_Status] DEFAULT ('Delivered'), -- Queued, Sent, Delivered, Failed
    [SentAtUtc] DATETIME2(7) NULL,
    [CreatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Notifications_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Notifications_NotificationTemplates_TemplateId] FOREIGN KEY ([TemplateId]) 
        REFERENCES [dbo].[NotificationTemplates] ([Id])
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Notifications_NotificationReference] ON [dbo].[Notifications] ([NotificationReference] ASC);
CREATE NONCLUSTERED INDEX [IX_Notifications_Recipient] ON [dbo].[Notifications] ([Recipient] ASC);
GO

CREATE TABLE [dbo].[NotificationPreferences] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [EmailEnabled] BIT NOT NULL CONSTRAINT [DF_NotificationPreferences_EmailEnabled] DEFAULT (1),
    [SmsEnabled] BIT NOT NULL CONSTRAINT [DF_NotificationPreferences_SmsEnabled] DEFAULT (1),
    [PushEnabled] BIT NOT NULL CONSTRAINT [DF_NotificationPreferences_PushEnabled] DEFAULT (1),
    [WhatsAppEnabled] BIT NOT NULL CONSTRAINT [DF_NotificationPreferences_WhatsAppEnabled] DEFAULT (1),
    [MarketingConsent] BIT NOT NULL CONSTRAINT [DF_NotificationPreferences_MarketingConsent] DEFAULT (0),
    [UpdatedAtUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_NotificationPreferences_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_NotificationPreferences] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_NotificationPreferences_UserId] ON [dbo].[NotificationPreferences] ([UserId] ASC);
GO

-- 4. Seed Data (1,000 Records per table)
-- 4.1 Insert 1,000 NotificationTemplates
SET IDENTITY_INSERT [dbo].[NotificationTemplates] ON;

DECLARE @TemplateCategories TABLE (
    Id INT PRIMARY KEY,
    CodePrefix VARCHAR(30),
    NamePrefix VARCHAR(60),
    Channel VARCHAR(20),
    SubjectTemplate VARCHAR(150),
    BodyContent VARCHAR(300)
);
INSERT INTO @TemplateCategories VALUES
(1, 'ORDER_CONFIRMED', 'Vehicle Order Confirmed', 'Email', 'Your Vehicle Order #{OrderNumber} Has Been Confirmed!', 'Thank you for choosing our platform. Your vehicle configuration has been verified and sent for dealer inventory allocation.'),
(2, 'PAYMENT_SUCCEEDED', 'Payment Receipt & Confirmation', 'Email', 'Receipt for Your Payment {PaymentReference}', 'We have successfully received your payment of {Amount} {Currency}. Tax invoice is now available for download.'),
(3, 'DEALER_ALLOCATED', 'Dealership Vehicle Stock Reserved', 'SMS', 'Vehicle Stock Allocation Update', 'Great news! Dealership {DealerName} has successfully allocated VIN #{VinNumber} for your order #{OrderNumber}.'),
(4, 'PRE_DELIVERY_INSPECTION', 'Pre-Delivery Inspection Complete', 'WhatsApp', 'Your Vehicle PDI Inspection Is Ready', 'Your vehicle has passed multi-point quality certification inspection. Please schedule your delivery appointment.'),
(5, 'OUT_FOR_DELIVERY', 'Vehicle Ready For Handover', 'Push', 'Vehicle Ready For Delivery!', 'Your vehicle is prepped and polished. Dealership representative is waiting for key handover ceremony.'),
(6, 'DELIVERY_COMPLETED', 'Delivery Ceremony Confirmation', 'Email', 'Welcome to the Family! Delivery Complete', 'Congratulations on taking delivery of your brand new 2026 vehicle! Review warranty coverage and roadside support contacts.'),
(7, 'CANCELLATION_REFUND', 'Order Cancellation & Refund Processed', 'Email', 'Order Cancellation Update #{OrderNumber}', 'As requested, your order cancellation has been processed. Full refund has been initiated back to your original payment method.'),
(8, 'SERVICE_REMINDER', 'Scheduled Service Care Alert', 'SMS', 'Upcoming Maintenance Service Reminder', 'Keep your vehicle operating at peak performance. Schedule your complimentary 1,000 km checkup at your preferred dealer.');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[NotificationTemplates] (
    [Id], [Code], [Name], [Channel], [Subject], [BodyTemplate], [IsActive], [CreatedAtUtc], [UpdatedAtUtc]
)
SELECT 
    n.N,
    CONCAT('TMPL_', tc.CodePrefix, '_', RIGHT('000' + CAST(n.N AS VARCHAR(4)), 4)),
    CONCAT(tc.NamePrefix, ' [Template Edition #', RIGHT('000' + CAST(n.N AS VARCHAR(4)), 4), ']'),
    tc.Channel,
    tc.SubjectTemplate,
    CONCAT(tc.BodyContent, ' [System Template Identifier: ', RIGHT('000' + CAST(n.N AS VARCHAR(4)), 4), ']'),
    1,
    DATEADD(DAY, - (n.N % 90), '2026-10-28 12:00:00'),
    NULL
FROM Numbers n
JOIN @TemplateCategories tc ON tc.Id = ((n.N - 1) % 8) + 1;

SET IDENTITY_INSERT [dbo].[NotificationTemplates] OFF;
GO

-- 4.2 Insert 1,000 Notifications (Mapped 1-to-1 to NotificationTemplates)
SET IDENTITY_INSERT [dbo].[Notifications] ON;

DECLARE @ChannelList TABLE (Id INT PRIMARY KEY, Channel VARCHAR(20));
INSERT INTO @ChannelList VALUES (1, 'Email'), (2, 'SMS'), (3, 'WhatsApp'), (4, 'Push');

DECLARE @RecipientList TABLE (Id INT PRIMARY KEY, Email VARCHAR(100), Phone VARCHAR(20));
INSERT INTO @RecipientList VALUES
(1, 'alex.sharma@vehiclex-ecom.com', '+91-9820011223'),
(2, 'priya.verma@vehiclex-ecom.com', '+91-9820044556'),
(3, 'michael.patel@vehiclex-ecom.com', '+91-9820077889'),
(4, 'ananya.mehta@vehiclex-ecom.com', '+91-9820099001'),
(5, 'david.gupta@vehiclex-ecom.com', '+91-9820122334'),
(6, 'diya.singh@vehiclex-ecom.com', '+91-9820155667'),
(7, 'james.kumar@vehiclex-ecom.com', '+91-9820188990'),
(8, 'isha.reddy@vehiclex-ecom.com', '+91-9820211223'),
(9, 'robert.chopra@vehiclex-ecom.com', '+91-9820244556'),
(10, 'aadhya.iyer@vehiclex-ecom.com', '+91-9820277889');

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[Notifications] (
    [Id], [NotificationReference], [TemplateId], [Recipient], [Channel],
    [Subject], [Body], [Status], [SentAtUtc], [CreatedAtUtc]
)
SELECT 
    n.N,
    CONCAT('NTF-2026-', RIGHT('000000' + CAST(n.N AS VARCHAR(6)), 6)),
    n.N, -- TemplateId 1..1000
    CASE 
        WHEN ch.Channel IN ('SMS', 'WhatsApp') THEN rec.Phone
        ELSE CONCAT('customer.', RIGHT('0000' + CAST(n.N AS VARCHAR(4)), 4), '@vehiclex-ecom.com')
    END,
    ch.Channel,
    CONCAT('Official Vehicle Notification Alert #', RIGHT('000000' + CAST(n.N AS VARCHAR(6)), 6)),
    CONCAT('Dear Customer, your vehicle status update has been recorded successfully for order reference SO-2026-', RIGHT('000000' + CAST(n.N AS VARCHAR(6)), 6), '.'),
    'Delivered',
    DATEADD(MINUTE, - (n.N * 7), '2026-10-28 12:00:00'),
    DATEADD(MINUTE, - (n.N * 8), '2026-10-28 12:00:00')
FROM Numbers n
JOIN @ChannelList ch ON ch.Id = ((n.N - 1) % 4) + 1
JOIN @RecipientList rec ON rec.Id = ((n.N - 1) % 10) + 1;

SET IDENTITY_INSERT [dbo].[Notifications] OFF;
GO

-- 4.3 Insert 1,000 NotificationPreferences
SET IDENTITY_INSERT [dbo].[NotificationPreferences] ON;

;WITH Digits AS (
    SELECT 0 AS d UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
),
Numbers AS (
    SELECT (d3.d * 100 + d2.d * 10 + d1.d + 1) AS N
    FROM Digits d1 CROSS JOIN Digits d2 CROSS JOIN Digits d3
)
INSERT INTO [dbo].[NotificationPreferences] (
    [Id], [UserId], [EmailEnabled], [SmsEnabled], [PushEnabled],
    [WhatsAppEnabled], [MarketingConsent], [UpdatedAtUtc]
)
SELECT 
    n.N,
    CAST(CAST(HASHBYTES('MD5', 'USER_' + CAST(n.N AS VARCHAR(10))) AS BINARY(16)) AS UNIQUEIDENTIFIER),
    1, -- Email Enabled
    1, -- SMS Enabled
    CASE WHEN n.N % 3 = 0 THEN 0 ELSE 1 END,
    1, -- WhatsApp Enabled
    CASE WHEN n.N % 5 = 0 THEN 1 ELSE 0 END, -- Marketing Consent
    DATEADD(DAY, - (n.N % 60), '2026-10-28 12:00:00')
FROM Numbers n;

SET IDENTITY_INSERT [dbo].[NotificationPreferences] OFF;
GO

-- 5. Verification Query
SELECT 'NotificationTemplates' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[NotificationTemplates]
UNION ALL
SELECT 'Notifications', COUNT(*) FROM [dbo].[Notifications]
UNION ALL
SELECT 'NotificationPreferences', COUNT(*) FROM [dbo].[NotificationPreferences];
GO
