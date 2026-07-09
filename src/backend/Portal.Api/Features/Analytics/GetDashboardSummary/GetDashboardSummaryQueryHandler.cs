using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Analytics;
using Portal.Domain.Enums;
using Portal.Application.Infrastructure.Persistence;

namespace Portal.Application.Features.Analytics.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly PortalDbContext _dbContext;

    public GetDashboardSummaryQueryHandler(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var submissions = await _dbContext.Submissions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalCount = submissions.Count;
        var pendingCount = submissions.Count(s => s.Status == SubmissionStatus.Submitted);
        var approvedCount = submissions.Count(s => s.Status == SubmissionStatus.Approved || s.Status == SubmissionStatus.GerrieSynced);
        var rejectedCount = submissions.Count(s => s.Status == SubmissionStatus.Rejected);
        var gerrieSyncFailedCount = submissions.Count(s => s.Status == SubmissionStatus.GerrieSyncFailed);

        var reviewedCount = approvedCount + rejectedCount;
        var approvalRate = reviewedCount > 0 ? (decimal)approvedCount / reviewedCount * 100 : 0;

        var reviewsData = await _dbContext.SubmissionReviews
            .AsNoTracking()
            .Include(r => r.Submission)
            .ToListAsync(cancellationToken);

        var averageReviewTimeHours = reviewsData.Count > 0
            ? reviewsData.Average(r => (r.ReviewedUtc - r.Submission.SubmittedUtc).TotalHours)
            : 0;

        return new DashboardSummaryDto
        {
            TotalSubmissions = totalCount,
            PendingReview = pendingCount,
            Approved = approvedCount,
            Rejected = rejectedCount,
            GerrieSynced = 0,
            GerrieSyncFailed = gerrieSyncFailedCount,
            ApprovalRate = approvalRate,
            AverageReviewTimeHours = averageReviewTimeHours,
            GeneratedUtc = DateTime.UtcNow
        };
    }
}
