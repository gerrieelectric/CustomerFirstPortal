CREATE TABLE [dbo].[AuditEvents]
(
    [AuditEventId]         UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_AuditEvents_AuditEventId] DEFAULT (NEWSEQUENTIALID()),
    [SubmissionId]         UNIQUEIDENTIFIER NULL,
    [ActorName]            NVARCHAR(200) NOT NULL,
    [ActorEmail]           NVARCHAR(256) NULL,
    [EventType]            NVARCHAR(50) NOT NULL,
    [EventDetail]          NVARCHAR(MAX) NULL,
    [CreatedUtc]           DATETIME2 (0) NOT NULL CONSTRAINT [DF_AuditEvents_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AuditEvents] PRIMARY KEY CLUSTERED ([AuditEventId] ASC),
    CONSTRAINT [FK_AuditEvents_Submissions_SubmissionId]
        FOREIGN KEY ([SubmissionId]) REFERENCES [dbo].[Submissions] ([SubmissionId]),
    CONSTRAINT [CK_AuditEvents_EventType]
        CHECK ([EventType] IN (N'Submitted', N'AttachmentAdded', N'ReviewOpened', N'Approved', N'Rejected', N'GerrieSyncQueued', N'GerrieSyncSucceeded', N'GerrieSyncFailed', N'AdminUpdated'))
);

GO

CREATE INDEX [IX_AuditEvents_SubmissionId_CreatedUtc]
    ON [dbo].[AuditEvents] ([SubmissionId] ASC, [CreatedUtc] DESC);