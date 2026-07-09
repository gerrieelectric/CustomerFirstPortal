namespace Portal.Application.DTOs.Analytics;

public class NominationReportDto
{
    public DateTime RangeStartUtc { get; set; }
    public DateTime RangeEndUtc { get; set; }
    public string RangeType { get; set; } = string.Empty;
    public string TypeFilter { get; set; } = string.Empty;
    public List<NomineeReportItem> TopNominated { get; set; } = [];
    public List<NomineeReportItem> TopApproved { get; set; } = [];
    public DateTime GeneratedUtc { get; set; }

    public class NomineeReportItem
    {
        public string NomineeName { get; set; } = string.Empty;
        public string? NomineeEmail { get; set; }
        public string? Department { get; set; }
        public int TotalNominations { get; set; }
        public int ApprovedNominations { get; set; }
        public DateTime MostRecentNominationUtc { get; set; }
    }
}
