using MediatR;
using Portal.Application.DTOs.Analytics;

namespace Portal.Application.Features.Analytics.GetDashboardSummary;

public class GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>
{
}
