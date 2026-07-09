using MediatR;
using Portal.Application.DTOs.Analytics;

namespace Portal.Application.Features.Analytics.GetAdminReviewActivity;

public class GetAdminReviewActivityQuery : IRequest<AdminReviewActivityDto>
{
}
