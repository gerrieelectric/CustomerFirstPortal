CREATE TABLE [dbo].[SubmissionAttachments]
(
    [SubmissionAttachmentId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_SubmissionAttachments_SubmissionAttachmentId] DEFAULT (NEWSEQUENTIALID()),
    [SubmissionId]           UNIQUEIDENTIFIER NOT NULL,
    [OriginalFileName]       NVARCHAR(260) NOT NULL,
    [BlobName]               NVARCHAR(260) NOT NULL,
    [ContentType]            NVARCHAR(150) NOT NULL,
    [FileSizeBytes]          BIGINT NOT NULL,
    [UploadedByName]         NVARCHAR(200) NOT NULL,
    [UploadedUtc]            DATETIME2 (0) NOT NULL CONSTRAINT [DF_SubmissionAttachments_UploadedUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SubmissionAttachments] PRIMARY KEY CLUSTERED ([SubmissionAttachmentId] ASC),
    CONSTRAINT [FK_SubmissionAttachments_Submissions_SubmissionId]
        FOREIGN KEY ([SubmissionId]) REFERENCES [dbo].[Submissions] ([SubmissionId]) ON DELETE CASCADE,
    CONSTRAINT [CK_SubmissionAttachments_FileSizeBytes]
        CHECK ([FileSizeBytes] > 0)
);

GO

CREATE INDEX [IX_SubmissionAttachments_SubmissionId]
    ON [dbo].[SubmissionAttachments] ([SubmissionId] ASC);