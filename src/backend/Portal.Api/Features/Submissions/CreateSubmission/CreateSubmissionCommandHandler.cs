using MediatR;
using Portal.Application.DTOs.Submissions;
using Portal.Domain.Entities;
using Portal.Domain.Enums;
using Portal.Application.Infrastructure.Notifications;
using Portal.Application.Infrastructure.Persistence;
using Portal.Application.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Portal.Application.Features.Submissions.CreateSubmission;

public class CreateSubmissionCommandHandler : IRequestHandler<CreateSubmissionCommand, SubmissionResponse>
{
    private readonly PortalDbContext _dbContext;
    private readonly IAdminSubmissionNotificationService _adminSubmissionNotificationService;
    private readonly ISubmissionSuggestionQueue? _suggestionQueue;
    private readonly ILogger<CreateSubmissionCommandHandler>? _logger;

    public CreateSubmissionCommandHandler(
        PortalDbContext dbContext,
        IAdminSubmissionNotificationService adminSubmissionNotificationService,
        ISubmissionSuggestionQueue? suggestionQueue = null,
        ILogger<CreateSubmissionCommandHandler>? logger = null)
    {
        _dbContext = dbContext;
        _adminSubmissionNotificationService = adminSubmissionNotificationService;
        _suggestionQueue = suggestionQueue;
        _logger = logger;
    }

    public async Task<SubmissionResponse> Handle(CreateSubmissionCommand request, CancellationToken cancellationToken)
    {
        // Parse submission type
        if (!Enum.TryParse<SubmissionType>(request.SubmissionType, out var submissionType))
        {
            throw new ArgumentException($"Invalid submission type: {request.SubmissionType}");
        }

        // Create domain entity
        var submission = new Submission
        {
            SubmissionId = Guid.NewGuid(),
            SubmissionType = submissionType,
            Status = SubmissionStatus.Submitted,
            SubmittedByName = request.SubmittedByName,
            SubmittedByEmail = request.SubmittedByEmail,
            SubmittedByDepartment = request.SubmittedByDepartment,
            SubmittedByEntraObjectId = request.SubmittedByEntraObjectId,
            NomineeName = request.NomineeName,
            NomineeEmail = request.NomineeEmail,
            NomineeDepartment = request.NomineeDepartment,
            CompanyName = request.CompanyName,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            RecognitionReason = request.RecognitionReason,
            ServiceRating = request.ServiceRating.HasValue ? (byte)request.ServiceRating.Value : null,
            SubmittedUtc = DateTime.UtcNow
        };

        // Save to database
        _dbContext.Submissions.Add(submission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Create audit event
        var auditEvent = new AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            SubmissionId = submission.SubmissionId,
            ActorEmail = request.SubmittedByEmail,
            ActorName = request.SubmittedByName,
            EventType = AuditEventType.Submitted,
            EventDetail = $"Submission created for nominee: {request.NomineeName}",
            CreatedUtc = DateTime.UtcNow
        };

        _dbContext.AuditEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _adminSubmissionNotificationService.NotifySubmissionCreatedAsync(submission, cancellationToken);

        // Queue suggestion generation for hosted background processing.
        if (_suggestionQueue != null)
        {
            if (!_suggestionQueue.TryQueue(submission.SubmissionId))
            {
                _logger?.LogWarning(
                    "Suggestion queue is full; skipping suggestion generation enqueue for submission {SubmissionId}",
                    submission.SubmissionId);
            }
        }

        // Return DTO
        return MapToResponse(submission);
    }

    private SubmissionResponse MapToResponse(Submission submission)
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
