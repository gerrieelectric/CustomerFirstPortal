using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Analytics;
using Portal.Domain.Enums;
using Portal.Application.Infrastructure.Persistence;

namespace Portal.Application.Features.Analytics.GetAdminReviewActivity;

public class GetAdminReviewActivityQueryHandler : IRequestHandler<GetAdminReviewActivityQuery, AdminReviewActivityDto>
{
    private readonly PortalDbContext _dbContext;

    public GetAdminReviewActivityQueryHandler(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminReviewActivityDto> Handle(GetAdminReviewActivityQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _dbContext.SubmissionReviews
            .AsNoTracking()
            .Include(r => r.AdminUser)
            .Include(r => r.Submission)
            .ToListAsync(cancellationToken);

        var adminStats = reviews
            .Where(r => r.AdminUser != null)
            .GroupBy(r => r.AdminUser)
            .Select(g => new AdminReviewActivityDto.AdminActivityItem
            {
                AdminUserId = g.Key!.AdminUserId,
                AdminName = g.Key.DisplayName,
                AdminEmail = g.Key.Email,
                TotalReviewsCompleted = g.Count(),
                ApprovalsCount = g.Count(r => r.Decision == ReviewDecision.Approved),
                RejectionsCount = g.Count(r => r.Decision == ReviewDecision.Rejected),
                AverageReviewTimeHours = g.Average(r => (r.ReviewedUtc - r.Submission.SubmittedUtc).TotalHours),
                LastReviewUtc = g.Max(r => r.ReviewedUtc)
            })
            .OrderByDescending(a => a.TotalReviewsCompleted)
            .ToList();

        return new AdminReviewActivityDto
        {
            AdminStats = adminStats,
            GeneratedUtc = DateTime.UtcNow
        };
    }
}
