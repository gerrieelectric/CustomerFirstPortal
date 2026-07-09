using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Analytics;
using Portal.Domain.Enums;
using Portal.Application.Infrastructure.Persistence;

namespace Portal.Application.Features.Analytics.GetRecognitionByNominee;

public class GetRecognitionByNomineeQueryHandler : IRequestHandler<GetRecognitionByNomineeQuery, RecognitionByNomineeDto>
{
    private readonly PortalDbContext _dbContext;

    public GetRecognitionByNomineeQueryHandler(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RecognitionByNomineeDto> Handle(GetRecognitionByNomineeQuery request, CancellationToken cancellationToken)
    {
        var submissions = await _dbContext.Submissions
            .AsNoTracking()
            .Include(s => s.Reviews)
            .ToListAsync(cancellationToken);

        var nomineeGroups = submissions
            .GroupBy(s => new { s.NomineeName, s.NomineeEmail, s.NomineeDepartment })
            .Select(g => new
            {
                NomineeName = g.Key.NomineeName,
                NomineeEmail = g.Key.NomineeEmail,
                Department = g.Key.NomineeDepartment,
                Submissions = g.ToList()
            })
            .Select(ng => new RecognitionByNomineeDto.NomineeRecognitionItem
            {
                NomineeName = ng.NomineeName,
                NomineeEmail = ng.NomineeEmail,
                Department = ng.Department,
                TotalRecognitions = ng.Submissions.Count,
                ApprovedRecognitions = ng.Submissions.Count(s => s.Status == SubmissionStatus.Approved),
                AverageServiceRating = ng.Submissions.Where(s => s.ServiceRating.HasValue)
                    .Select(s => (double)(s.ServiceRating ?? 0))
                    .DefaultIfEmpty(0)
                    .Average(),
                MostRecentRecognition = ng.Submissions.Max(s => s.SubmittedUtc)
            })
            .OrderByDescending(n => n.ApprovedRecognitions)
            .Take(request.TopCount)
            .ToList();

        return new RecognitionByNomineeDto
        {
            TopNominees = nomineeGroups,
            GeneratedUtc = DateTime.UtcNow
        };
    }
}
