CREATE TABLE [dbo].[AdminUsers]
(
    [AdminUserId]            INT IDENTITY (1, 1) NOT NULL,
    [EntraObjectId]          UNIQUEIDENTIFIER NOT NULL,
    [Email]                  NVARCHAR(256) NOT NULL,
    [DisplayName]            NVARCHAR(200) NOT NULL,
    [Department]             NVARCHAR(100) NULL,
    [CanReview]              BIT NOT NULL CONSTRAINT [DF_AdminUsers_CanReview] DEFAULT ((1)),
    [ReceiveNotifications]   BIT NOT NULL CONSTRAINT [DF_AdminUsers_ReceiveNotifications] DEFAULT ((1)),
    [IsActive]               BIT NOT NULL CONSTRAINT [DF_AdminUsers_IsActive] DEFAULT ((1)),
    [CreatedUtc]             DATETIME2 (0) NOT NULL CONSTRAINT [DF_AdminUsers_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc]             DATETIME2 (0) NOT NULL CONSTRAINT [DF_AdminUsers_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AdminUsers] PRIMARY KEY CLUSTERED ([AdminUserId] ASC),
    CONSTRAINT [UQ_AdminUsers_EntraObjectId] UNIQUE NONCLUSTERED ([EntraObjectId] ASC),
    CONSTRAINT [UQ_AdminUsers_Email] UNIQUE NONCLUSTERED ([Email] ASC)
);