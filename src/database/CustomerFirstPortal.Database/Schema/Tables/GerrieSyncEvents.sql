CREATE TABLE [dbo].[GerrieSyncEvents]
(
    [GerrieSyncEventId]    UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_GerrieSyncEvents_GerrieSyncEventId] DEFAULT (NEWSEQUENTIALID()),
    [SubmissionId]         UNIQUEIDENTIFIER NOT NULL,
    [AttemptNumber]        INT NOT NULL,
    [SyncStatus]           NVARCHAR(20) NOT NULL,
    [ResponseCode]         NVARCHAR(50) NULL,
    [ResponseMessage]      NVARCHAR(4000) NULL,
    [ExternalRecordId]     NVARCHAR(100) NULL,
    [CreatedUtc]           DATETIME2 (0) NOT NULL CONSTRAINT [DF_GerrieSyncEvents_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_GerrieSyncEvents] PRIMARY KEY CLUSTERED ([GerrieSyncEventId] ASC),
    CONSTRAINT [FK_GerrieSyncEvents_Submissions_SubmissionId]
        FOREIGN KEY ([SubmissionId]) REFERENCES [dbo].[Submissions] ([SubmissionId]) ON DELETE CASCADE,
    CONSTRAINT [CK_GerrieSyncEvents_SyncStatus]
        CHECK ([SyncStatus] IN (N'Pending', N'Succeeded', N'Failed')),
    CONSTRAINT [CK_GerrieSyncEvents_AttemptNumber]
        CHECK ([AttemptNumber] > 0)
);

GO

CREATE INDEX [IX_GerrieSyncEvents_SubmissionId_CreatedUtc]
    ON [dbo].[GerrieSyncEvents] ([SubmissionId] ASC, [CreatedUtc] DESC);