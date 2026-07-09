using MediatR;
using Portal.Application.DTOs.Submissions;

namespace Portal.Application.Features.Submissions.ReviewSubmission;

public class ReviewSubmissionCommand : IRequest<SubmissionResponse>
{
    public Guid SubmissionId { get; set; }

    public string ReviewDecision { get; set; } = null!;

    public string? Notes { get; set; }

    public string? EditedRecognitionReason { get; set; }

    public int AdminUserId { get; set; }
    public string AdminName { get; set; } = null!;
    public string AdminEmail { get; set; } = null!;
    public byte[] RowVersion { get; set; } = [];

    public static ReviewSubmissionCommand FromRequest(
        Guid submissionId,
        AdminReviewRequest request,
        int adminUserId,
        string adminName,
        string adminEmail)
    {
        if (string.IsNullOrWhiteSpace(request.RowVersion))
        {
            throw new ArgumentException("RowVersion is required.");
        }

        byte[] rowVersion;
        try
        {
            rowVersion = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("RowVersion is invalid.", ex);
        }

        return new ReviewSubmissionCommand
        {
            SubmissionId = submissionId,
            ReviewDecision = request.ReviewDecision,
            Notes = request.Notes,
            EditedRecognitionReason = request.EditedRecognitionReason,
            AdminUserId = adminUserId,
            AdminName = adminName,
            AdminEmail = adminEmail,
            RowVersion = rowVersion
        };
    }
}
