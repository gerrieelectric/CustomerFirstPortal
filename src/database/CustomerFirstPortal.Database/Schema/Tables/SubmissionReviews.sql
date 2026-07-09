CREATE TABLE [dbo].[SubmissionReviews]
(
    [SubmissionReviewId]   UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_SubmissionReviews_SubmissionReviewId] DEFAULT (NEWSEQUENTIALID()),
    [SubmissionId]         UNIQUEIDENTIFIER NOT NULL,
    [AdminUserId]          INT NOT NULL,
    [Decision]             NVARCHAR(20) NOT NULL,
    [ReviewNotes]          NVARCHAR(2000) NULL,
    [EditedRecognitionReason] NVARCHAR(MAX) NULL,
    [ReviewedUtc]          DATETIME2 (0) NOT NULL CONSTRAINT [DF_SubmissionReviews_ReviewedUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SubmissionReviews] PRIMARY KEY CLUSTERED ([SubmissionReviewId] ASC),
    CONSTRAINT [FK_SubmissionReviews_Submissions_SubmissionId]
        FOREIGN KEY ([SubmissionId]) REFERENCES [dbo].[Submissions] ([SubmissionId]) ON DELETE CASCADE,
    CONSTRAINT [FK_SubmissionReviews_AdminUsers_AdminUserId]
        FOREIGN KEY ([AdminUserId]) REFERENCES [dbo].[AdminUsers] ([AdminUserId]),
    CONSTRAINT [CK_SubmissionReviews_Decision]
        CHECK ([Decision] IN (N'Approved', N'Rejected', N'EditedAndApproved'))
);

GO

CREATE INDEX [IX_SubmissionReviews_SubmissionId_ReviewedUtc]
    ON [dbo].[SubmissionReviews] ([SubmissionId] ASC, [ReviewedUtc] DESC);