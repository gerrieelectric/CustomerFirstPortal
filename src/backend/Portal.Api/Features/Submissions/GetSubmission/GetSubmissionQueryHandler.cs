using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Api.Features.Submissions;
using Portal.Application.DTOs.Submissions;
using Portal.Application.Infrastructure.Persistence;

namespace Portal.Application.Features.Submissions.GetSubmission;

public class GetSubmissionQueryHandler : IRequestHandler<GetSubmissionQuery, SubmissionResponse?>
{
    private readonly PortalDbContext _dbContext;

    public GetSubmissionQueryHandler(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SubmissionResponse?> Handle(GetSubmissionQuery request, CancellationToken cancellationToken)
    {
        var submission = await _dbContext.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SubmissionId == request.SubmissionId, cancellationToken);

        if (submission == null)
            return null;

        var attachments = await _dbContext.SubmissionAttachments
            .AsNoTracking()
            .Where(a => a.SubmissionId == submission.SubmissionId)
            .OrderByDescending(a => a.UploadedUtc)
            .Select(a => new AttachmentListItemDto
            {
                SubmissionAttachmentId = a.SubmissionAttachmentId,
                OriginalFileName = a.OriginalFileName,
                ContentType = a.ContentType,
                FileSizeBytes = a.FileSizeBytes,
                UploadedByName = a.UploadedByName,
                UploadedUtc = a.UploadedUtc
            })
            .ToListAsync(cancellationToken);

        return MapToResponse(submission, attachments);
    }

    private SubmissionResponse MapToResponse(Portal.Domain.Entities.Submission submission, List<AttachmentListItemDto> attachments)
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
            LastGerrieSyncUtc = submission.LastGerrieSyncUtc,
            Attachments = attachments
        };
    }
}
