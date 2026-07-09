using MediatR;
using Portal.Application.DTOs.Submissions;

namespace Portal.Application.Features.Submissions.GetSubmission;

public class GetSubmissionQuery : IRequest<SubmissionResponse?>
{
    public Guid SubmissionId { get; set; }

    public GetSubmissionQuery(Guid submissionId)
    {
        SubmissionId = submissionId;
    }
}
