using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Analytics;
using Portal.Domain.Enums;
using Portal.Application.Infrastructure.Persistence;

namespace Portal.Application.Features.Analytics.GetNominationReport;

public class GetNominationReportQueryHandler : IRequestHandler<GetNominationReportQuery, NominationReportDto>
{
    private readonly PortalDbContext _dbContext;

    public GetNominationReportQueryHandler(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NominationReportDto> Handle(GetNominationReportQuery request, CancellationToken cancellationToken)
    {
        // Calculate the date range
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            request.RangeType,
            request.ReferenceDate,
            request.CustomStartDate,
            request.CustomEndDate);

        // Determine which submission types to include
        var includedTypes = GetIncludedTypes(request.TypeFilter);

        // Query submissions in the date range with the appropriate type filter
        var submissions = await _dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.SubmittedUtc >= startUtc && s.SubmittedUtc < endExclusiveUtc)
            .Where(s => includedTypes.Contains(s.SubmissionType))
            .ToListAsync(cancellationToken);

        // Group by nominee and calculate statistics
        var nomineeGroups = submissions
            .GroupBy(s => new { s.NomineeName, s.NomineeEmail, s.NomineeDepartment })
            .Select(g => new
            {
                NomineeName = g.Key.NomineeName,
                NomineeEmail = g.Key.NomineeEmail,
                Department = g.Key.NomineeDepartment,
                Submissions = g.ToList()
            })
            .Select(ng => new NominationReportDto.NomineeReportItem
            {
                NomineeName = ng.NomineeName,
                NomineeEmail = ng.NomineeEmail,
                Department = ng.Department,
                TotalNominations = ng.Submissions.Count,
                ApprovedNominations = ng.Submissions.Count(s => IsApproved(s.Status)),
                MostRecentNominationUtc = ng.Submissions.Max(s => s.SubmittedUtc)
            })
            .ToList();

        // Create the two reports: top nominated and top approved
        var topNominated = nomineeGroups
            .OrderByDescending(n => n.TotalNominations)
            .Take(request.TopCount)
            .ToList();

        var topApproved = nomineeGroups
            .OrderByDescending(n => n.ApprovedNominations)
            .ThenByDescending(n => n.TotalNominations)
            .Take(request.TopCount)
            .ToList();

        return new NominationReportDto
        {
            RangeStartUtc = startUtc,
            RangeEndUtc = endExclusiveUtc.AddTicks(-1), // Convert end-exclusive to inclusive
            RangeType = request.RangeType.ToString(),
            TypeFilter = request.TypeFilter.ToString(),
            TopNominated = topNominated,
            TopApproved = topApproved,
            GeneratedUtc = DateTime.UtcNow
        };
    }

    private static List<SubmissionType> GetIncludedTypes(NominationTypeFilter filter)
    {
        return filter switch
        {
            NominationTypeFilter.EmployeeToEmployee => new List<SubmissionType> { SubmissionType.EmployeeToEmployee },
            NominationTypeFilter.CustomerToEmployee => new List<SubmissionType>
            {
                SubmissionType.CustomerToEmployee,
                SubmissionType.EmployeeOnBehalfOfCustomer
            },
            NominationTypeFilter.All => new List<SubmissionType>
            {
                SubmissionType.EmployeeToEmployee,
                SubmissionType.EmployeeOnBehalfOfCustomer,
                SubmissionType.CustomerToEmployee
            },
            _ => throw new ArgumentException($"Unknown type filter: {filter}")
        };
    }

    private static bool IsApproved(SubmissionStatus status)
    {
        return status is SubmissionStatus.Approved
            or SubmissionStatus.GerrieSynced
            or SubmissionStatus.GerrieSyncFailed;
    }
}
