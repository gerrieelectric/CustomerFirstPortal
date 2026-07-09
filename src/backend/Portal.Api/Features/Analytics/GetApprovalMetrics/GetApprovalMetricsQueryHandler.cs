using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Analytics;
using Portal.Domain.Enums;
using Portal.Application.Infrastructure.Persistence;

namespace Portal.Application.Features.Analytics.GetApprovalMetrics;

public class GetApprovalMetricsQueryHandler : IRequestHandler<GetApprovalMetricsQuery, ApprovalMetricsDto>
{
    private readonly PortalDbContext _dbContext;

    public GetApprovalMetricsQueryHandler(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApprovalMetricsDto> Handle(GetApprovalMetricsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _dbContext.SubmissionReviews
            .AsNoTracking()
            .Include(r => r.Submission)
            .ToListAsync(cancellationToken);

        var approvedCount = reviews.Count(r => r.Decision == ReviewDecision.Approved);
        var rejectedCount = reviews.Count(r => r.Decision == ReviewDecision.Rejected);
        var totalReviewedCount = approvedCount + rejectedCount;

        var approvalRate = totalReviewedCount > 0 ? (decimal)approvedCount / totalReviewedCount * 100 : 0;
        var rejectionRate = totalReviewedCount > 0 ? (decimal)rejectedCount / totalReviewedCount * 100 : 0;

        var reviewTimesHours = reviews
            .Select(r => (r.ReviewedUtc - r.Submission.SubmittedUtc).TotalHours)
            .ToList();

        var averageReviewTimeHours = reviewTimesHours.Count > 0 ? reviewTimesHours.Average() : 0;
        var minReviewTimeHours = reviewTimesHours.Count > 0 ? reviewTimesHours.Min() : 0;
        var maxReviewTimeHours = reviewTimesHours.Count > 0 ? reviewTimesHours.Max() : 0;

        var todayStart = DateTime.UtcNow.Date;
        var reviewedTodayCount = reviews.Count(r => r.ReviewedUtc.Date == todayStart);
        var approvedTodayCount = reviews.Count(r => r.Decision == ReviewDecision.Approved && r.ReviewedUtc.Date == todayStart);

        return new ApprovalMetricsDto
        {
            TotalReviewed = totalReviewedCount,
            TotalApproved = approvedCount,
            TotalRejected = rejectedCount,
            ApprovalRate = approvalRate,
            RejectionRate = rejectionRate,
            AverageReviewTimeHours = averageReviewTimeHours,
            MinReviewTimeHours = minReviewTimeHours,
            MaxReviewTimeHours = maxReviewTimeHours,
            ReviewedTodayCount = reviewedTodayCount,
            ApprovedTodayCount = approvedTodayCount,
            GeneratedUtc = DateTime.UtcNow
        };
    }
}
