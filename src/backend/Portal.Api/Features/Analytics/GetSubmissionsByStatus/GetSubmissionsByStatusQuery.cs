using MediatR;
using Portal.Application.DTOs.Analytics;

namespace Portal.Application.Features.Analytics.GetSubmissionsByStatus;

public class GetSubmissionsByStatusQuery : IRequest<SubmissionsByStatusDto>
{
}
