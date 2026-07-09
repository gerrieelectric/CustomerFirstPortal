using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Submissions;
using Portal.Domain.Enums;
using Portal.Application.Infrastructure.Persistence;

namespace Portal.Application.Features.Submissions.UpdateSubmission;

public class UpdateSubmissionCommandHandler : IRequestHandler<UpdateSubmissionCommand, SubmissionResponse>
{
    private readonly PortalDbContext _dbContext;

    public UpdateSubmissionCommandHandler(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SubmissionResponse> Handle(UpdateSubmissionCommand request, CancellationToken cancellationToken)
    {
        // Get submission
        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.SubmissionId == request.SubmissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Submission {request.SubmissionId} not found");

        // Verify submission is in Draft status
        if (submission.Status != SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException(
                $"Submission cannot be edited. Current status: {submission.Status}. " +
                $"Only 'Submitted' status submissions can be edited.");
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.CompanyName))
            submission.CompanyName = request.CompanyName;

        if (!string.IsNullOrEmpty(request.NomineeName))
            submission.NomineeName = request.NomineeName;

        if (!string.IsNullOrEmpty(request.NomineeEmail))
            submission.NomineeEmail = request.NomineeEmail;

        if (!string.IsNullOrEmpty(request.CustomerName))
            submission.CustomerName = request.CustomerName;

        if (!string.IsNullOrEmpty(request.CustomerEmail))
            submission.CustomerEmail = request.CustomerEmail;

        if (!string.IsNullOrEmpty(request.RecognitionReason))
            submission.RecognitionReason = request.RecognitionReason;

        if (request.ServiceRating.HasValue)
            submission.ServiceRating = (byte)request.ServiceRating.Value;

        // Save changes
        await _dbContext.SaveChangesAsync(cancellationToken);

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
