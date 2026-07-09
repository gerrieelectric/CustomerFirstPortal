using MediatR;
using Portal.Application.DTOs.Analytics;

namespace Portal.Application.Features.Analytics.GetNominationReport;

public class GetNominationReportQuery : IRequest<NominationReportDto>
{
    public ReportDateRangeType RangeType { get; set; } = ReportDateRangeType.CalendarMonth;
    public DateTime? ReferenceDate { get; set; }
    public DateTime? CustomStartDate { get; set; }
    public DateTime? CustomEndDate { get; set; }
    public NominationTypeFilter TypeFilter { get; set; } = NominationTypeFilter.All;
    public int TopCount { get; set; } = 10;
}
