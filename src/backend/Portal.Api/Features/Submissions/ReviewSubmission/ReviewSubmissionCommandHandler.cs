using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Submissions;
using Portal.Domain.Entities;
using Portal.Domain.Enums;
using Portal.Application.Infrastructure.Integrations;
using Portal.Application.Infrastructure.Persistence;

namespace Portal.Application.Features.Submissions.ReviewSubmission;

public class ReviewSubmissionCommandHandler : IRequestHandler<ReviewSubmissionCommand, SubmissionResponse>
{
    private readonly PortalDbContext _dbContext;
    private readonly IGerrieSyncService _gerrieSyncService;

    public ReviewSubmissionCommandHandler(
        PortalDbContext dbContext,
        IGerrieSyncService gerrieSyncService)
    {
        _dbContext = dbContext;
        _gerrieSyncService = gerrieSyncService;
    }

    public async Task<SubmissionResponse> Handle(ReviewSubmissionCommand request, CancellationToken cancellationToken)
    {
        // Parse review decision
        if (!Enum.TryParse<ReviewDecision>(request.ReviewDecision, out var reviewDecision))
        {
            throw new ArgumentException($"Invalid review decision: {request.ReviewDecision}");
        }

        // Get submission
        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.SubmissionId == request.SubmissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Submission {request.SubmissionId} not found");

        _dbContext.Entry(submission).Property(s => s.RowVersion).OriginalValue = request.RowVersion;

        // Verify submission is in correct status for review
        if (submission.Status != SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException(
                $"Submission cannot be reviewed. Current status: {submission.Status}. " +
                $"Only 'Submitted' status submissions can be reviewed.");
        }

        // Update submission status based on decision
        switch (reviewDecision)
        {
            case ReviewDecision.Approved:
                submission.Status = SubmissionStatus.Approved;
                submission.ApprovedUtc = DateTime.UtcNow;
                submission.RecognitionReason = request.EditedRecognitionReason!;
                break;

            case ReviewDecision.Rejected:
                submission.Status = SubmissionStatus.Rejected;
                submission.RejectedUtc = DateTime.UtcNow;
                break;
        }

        submission.ReviewedByAdminUserId = request.AdminUserId;

        // Create review record
        var review = new SubmissionReview
        {
            SubmissionReviewId = Guid.NewGuid(),
            SubmissionId = submission.SubmissionId,
            AdminUserId = request.AdminUserId,
            Decision = reviewDecision,
            ReviewedUtc = DateTime.UtcNow,
            ReviewNotes = request.Notes,
            EditedRecognitionReason = request.EditedRecognitionReason
        };

        _dbContext.SubmissionReviews.Add(review);

        // Create audit event
        var auditEvent = new AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            SubmissionId = submission.SubmissionId,
            ActorEmail = request.AdminEmail,
            ActorName = request.AdminName,
            EventType = reviewDecision switch
            {
                ReviewDecision.Approved => AuditEventType.Approved,
                ReviewDecision.Rejected => AuditEventType.Rejected,
                _ => AuditEventType.ReviewOpened
            },
            EventDetail = $"Review decision: {reviewDecision}. Notes: {request.Notes ?? "(none)"}",
            CreatedUtc = DateTime.UtcNow
        };

        _dbContext.AuditEvents.Add(auditEvent);

        // Save changes
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (submission.Status == SubmissionStatus.Approved)
        {
            await _gerrieSyncService.SyncApprovedSubmissionAsync(
                submission,
                request.AdminName,
                request.AdminEmail,
                cancellationToken);
        }

        // Return updated submission
        return MapToResponse(submission);
    }

    private SubmissionResponse MapToResponse(Portal.Domain.Entities.Submission submission)
    {
        return new SubmissionResponse
        {
            SubmissionId = submission.SubmissionId,
            SubmissionType = submission.SubmissionType.ToString(),
            Status = submission.Status.ToString(),
            SubmittedByName = submission.SubmittedByName,
            SubmittedByEmail = submission.SubmittedByEmail,
            SubmittedByDepartment = submission.SubmittedByDepartment,
            NomineeName = submission.NomineeName,
            NomineeEmail = submission.NomineeEmail,
            NomineeDepartment = submission.NomineeDepartment,
            CompanyName = submission.CompanyName,
            CustomerName = submission.CustomerName,
            CustomerEmail = submission.CustomerEmail,
            RecognitionReason = submission.RecognitionReason,
            ServiceRating = submission.ServiceRating,
            SubmittedUtc = submission.SubmittedUtc,
            ApprovedUtc = submission.ApprovedUtc,
            RejectedUtc = submission.RejectedUtc,
            RowVersion = Convert.ToBase64String(submission.RowVersion),
            ReviewedByAdminUserId = submission.ReviewedByAdminUserId,
            GerrieRecordId = submission.GerrieRecordId,
            LastGerrieSyncUtc = submission.LastGerrieSyncUtc
        };
    }
}
