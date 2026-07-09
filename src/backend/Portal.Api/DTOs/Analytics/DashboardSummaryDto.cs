namespace Portal.Application.DTOs.Analytics;

public class DashboardSummaryDto
{
    public int TotalSubmissions { get; set; }
    public int PendingReview { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int GerrieSynced { get; set; }
    public int GerrieSyncFailed { get; set; }

    public decimal ApprovalRate { get; set; } // 0-100%
    public double AverageReviewTimeHours { get; set; }

    public DateTime GeneratedUtc { get; set; }
}
