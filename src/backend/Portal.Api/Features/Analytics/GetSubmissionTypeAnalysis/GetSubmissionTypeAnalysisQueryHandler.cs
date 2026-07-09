using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Analytics;
using Portal.Domain.Enums;
using Portal.Application.Infrastructure.Persistence;

namespace Portal.Application.Features.Analytics.GetSubmissionTypeAnalysis;

public class GetSubmissionTypeAnalysisQueryHandler : IRequestHandler<GetSubmissionTypeAnalysisQuery, SubmissionTypeAnalysisDto>
{
    private readonly PortalDbContext _dbContext;

    public GetSubmissionTypeAnalysisQueryHandler(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SubmissionTypeAnalysisDto> Handle(GetSubmissionTypeAnalysisQuery request, CancellationToken cancellationToken)
    {
        var submissions = await _dbContext.Submissions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var typeGroups = submissions
            .GroupBy(s => s.SubmissionType.ToString())
            .Select(g => new SubmissionTypeAnalysisDto.TypeBreakdown
            {
                SubmissionType = g.Key,
                Count = g.Count(),
                ApprovedCount = g.Count(s => s.Status == SubmissionStatus.Approved),
                RejectedCount = g.Count(s => s.Status == SubmissionStatus.Rejected),
                ApprovalRate = g.Count(s => s.Status == SubmissionStatus.Approved || s.Status == SubmissionStatus.Rejected) > 0
                    ? (decimal)g.Count(s => s.Status == SubmissionStatus.Approved) / g.Count(s => s.Status == SubmissionStatus.Approved || s.Status == SubmissionStatus.Rejected) * 100
                    : 0,
                AverageServiceRating = g.Where(s => s.ServiceRating.HasValue)
                    .Select(s => (double)(s.ServiceRating ?? 0))
                    .DefaultIfEmpty(0)
                    .Average()
            })
            .OrderByDescending(t => t.Count)
            .ToList();

        return new SubmissionTypeAnalysisDto
        {
            Types = typeGroups,
            GeneratedUtc = DateTime.UtcNow
        };
    }
}
