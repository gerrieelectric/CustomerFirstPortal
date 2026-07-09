namespace Portal.Application.DTOs.Analytics;

public class SubmissionsByStatusDto
{
    public List<StatusBreakdown> Statuses { get; set; } = [];
    public DateTime GeneratedUtc { get; set; }

    public class StatusBreakdown
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }
}
