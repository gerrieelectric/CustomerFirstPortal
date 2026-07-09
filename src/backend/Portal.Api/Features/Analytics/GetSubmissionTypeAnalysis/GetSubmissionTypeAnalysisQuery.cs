using MediatR;
using Portal.Application.DTOs.Analytics;

namespace Portal.Application.Features.Analytics.GetSubmissionTypeAnalysis;

public class GetSubmissionTypeAnalysisQuery : IRequest<SubmissionTypeAnalysisDto>
{
}
