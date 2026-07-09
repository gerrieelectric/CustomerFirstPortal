using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Analytics;
using Portal.Application.Infrastructure.Persistence;
using Portal.Domain.Enums;

namespace Portal.Application.Features.Analytics.GetSubmissionsByStatus;

public class GetSubmissionsByStatusQueryHandler : IRequestHandler<GetSubmissionsByStatusQuery, SubmissionsByStatusDto>
{
    private readonly PortalDbContext _dbContext;

    public GetSubmissionsByStatusQueryHandler(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SubmissionsByStatusDto> Handle(GetSubmissionsByStatusQuery request, CancellationToken cancellationToken)
    {
        var submissions = await _dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.Status != SubmissionStatus.GerrieSynced)
            .ToListAsync(cancellationToken);

        var totalCount = submissions.Count;
        var statusGroups = submissions
            .GroupBy(s => s.Status.ToString())
            .Select(g => new SubmissionsByStatusDto.StatusBreakdown
            {
                Status = g.Key,
                Count = g.Count(),
                PercentageOfTotal = totalCount > 0 ? (decimal)g.Count() / totalCount * 100 : 0
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        return new SubmissionsByStatusDto
        {
            Statuses = statusGroups,
            GeneratedUtc = DateTime.UtcNow
        };
    }
}
