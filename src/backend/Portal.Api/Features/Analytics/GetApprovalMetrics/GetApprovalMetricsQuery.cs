using MediatR;
using Portal.Application.DTOs.Analytics;

namespace Portal.Application.Features.Analytics.GetApprovalMetrics;

public class GetApprovalMetricsQuery : IRequest<ApprovalMetricsDto>
{
}
