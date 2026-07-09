using MediatR;
using Portal.Application.DTOs.Submissions;

namespace Portal.Application.Features.Submissions.UpdateSubmission;

public class UpdateSubmissionCommand : IRequest<SubmissionResponse>
{
    public Guid SubmissionId { get; set; }

    public string? CompanyName { get; set; }
    public string? NomineeName { get; set; }
    public string? NomineeEmail { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }

    public string? RecognitionReason { get; set; }

    public int? ServiceRating { get; set; }

    public static UpdateSubmissionCommand FromRequest(Guid submissionId, UpdateSubmissionRequest request)
    {
        return new UpdateSubmissionCommand
        {
            SubmissionId = submissionId,
            CompanyName = request.CompanyName,
            NomineeName = request.NomineeName,
            NomineeEmail = request.NomineeEmail,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            RecognitionReason = request.RecognitionReason,
            ServiceRating = request.ServiceRating
        };
    }
}
