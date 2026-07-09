CREATE TABLE [dbo].[Submissions]
(
    [SubmissionId]              UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_Submissions_SubmissionId] DEFAULT (NEWSEQUENTIALID()),
    [SubmissionType]            NVARCHAR(40) NOT NULL,
    [Status]                    NVARCHAR(30) NOT NULL CONSTRAINT [DF_Submissions_Status] DEFAULT (N'Submitted'),
    [SubmittedByName]           NVARCHAR(200) NOT NULL,
    [SubmittedByEmail]          NVARCHAR(256) NOT NULL,
    [SubmittedByDepartment]     NVARCHAR(100) NULL,
    [SubmittedByEntraObjectId]  UNIQUEIDENTIFIER NULL,
    [CompanyName]               NVARCHAR(200) NULL,
    [NomineeName]               NVARCHAR(200) NOT NULL,
    [NomineeEmail]              NVARCHAR(256) NULL,
    [NomineeDepartment]         NVARCHAR(100) NULL,
    [CustomerName]              NVARCHAR(200) NULL,
    [CustomerEmail]             NVARCHAR(256) NULL,
    [RecognitionReason]         NVARCHAR(MAX) NOT NULL,
    [ServiceRating]             TINYINT NULL,
    [SubmittedUtc]              DATETIME2 (0) NOT NULL CONSTRAINT [DF_Submissions_SubmittedUtc] DEFAULT (SYSUTCDATETIME()),
    [ApprovedUtc]               DATETIME2 (0) NULL,
    [RejectedUtc]               DATETIME2 (0) NULL,
    [ReviewedByAdminUserId]     INT NULL,
    [GerrieRecordId]            NVARCHAR(100) NULL,
    [LastGerrieSyncUtc]         DATETIME2 (0) NULL,
    [RowVersion]                ROWVERSION NOT NULL,
    CONSTRAINT [PK_Submissions] PRIMARY KEY CLUSTERED ([SubmissionId] ASC),
    CONSTRAINT [FK_Submissions_AdminUsers_ReviewedByAdminUserId]
        FOREIGN KEY ([ReviewedByAdminUserId]) REFERENCES [dbo].[AdminUsers] ([AdminUserId]),
    CONSTRAINT [CK_Submissions_SubmissionType]
        CHECK ([SubmissionType] IN (N'EmployeeToEmployee', N'EmployeeOnBehalfOfCustomer', N'CustomerToEmployee')),
    CONSTRAINT [CK_Submissions_Status]
        CHECK ([Status] IN (N'Draft', N'Submitted', N'Approved', N'Rejected', N'GerrieSynced', N'GerrieSyncFailed')),
    CONSTRAINT [CK_Submissions_ServiceRating]
        CHECK ([ServiceRating] IS NULL OR [ServiceRating] BETWEEN 1 AND 5),
    CONSTRAINT [CK_Submissions_CustomerName_RequiredForOnBehalf]
        CHECK ([SubmissionType] <> N'EmployeeOnBehalfOfCustomer' OR NULLIF(LTRIM(RTRIM([CustomerName])), N'') IS NOT NULL),
    CONSTRAINT [CK_Submissions_ApprovedRejectedExclusive]
        CHECK (NOT ([ApprovedUtc] IS NOT NULL AND [RejectedUtc] IS NOT NULL))
);

GO

CREATE INDEX [IX_Submissions_Status_SubmittedUtc]
    ON [dbo].[Submissions] ([Status] ASC, [SubmittedUtc] DESC);

GO

CREATE INDEX [IX_Submissions_NomineeName]
    ON [dbo].[Submissions] ([NomineeName] ASC);

GO

CREATE INDEX [IX_Submissions_SubmittedByEmail]
    ON [dbo].[Submissions] ([SubmittedByEmail] ASC);

GO

CREATE INDEX [IX_Submissions_ReviewedByAdminUserId]
    ON [dbo].[Submissions] ([ReviewedByAdminUserId] ASC)
    WHERE [ReviewedByAdminUserId] IS NOT NULL;