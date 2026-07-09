using MediatR;
using Portal.Application.DTOs.Submissions;

namespace Portal.Application.Features.Submissions.CreateSubmission;

public class CreateSubmissionCommand : IRequest<SubmissionResponse>
{
    public string SubmissionType { get; set; } = null!;

    public string SubmittedByName { get; set; } = null!;
    public string SubmittedByEmail { get; set; } = null!;
    public string? SubmittedByDepartment { get; set; }
    public Guid? SubmittedByEntraObjectId { get; set; }

    public string NomineeName { get; set; } = null!;
    public string NomineeEmail { get; set; } = null!;
    public string? NomineeDepartment { get; set; }

    public string? CompanyName { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }

    public string RecognitionReason { get; set; } = null!;

    public int? ServiceRating { get; set; }

    public static CreateSubmissionCommand FromRequest(CreateSubmissionRequest request)
    {
        return new CreateSubmissionCommand
        {
            SubmissionType = request.SubmissionType,
            SubmittedByName = request.SubmittedByName ?? string.Empty,
            SubmittedByEmail = request.SubmittedByEmail ?? string.Empty,
            SubmittedByDepartment = request.SubmittedByDepartment,
            SubmittedByEntraObjectId = request.SubmittedByEntraObjectId,
            NomineeName = request.NomineeName,
            NomineeEmail = request.NomineeEmail,
            NomineeDepartment = request.NomineeDepartment,
            CompanyName = request.CompanyName,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            RecognitionReason = request.RecognitionReason,
            ServiceRating = request.ServiceRating
        };
    }
}
