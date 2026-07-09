namespace Portal.Application.DTOs.Analytics;

public class ApprovalMetricsDto
{
    public int TotalReviewed { get; set; }
    public int TotalApproved { get; set; }
    public int TotalRejected { get; set; }

    public decimal ApprovalRate { get; set; } // 0-100%
    public decimal RejectionRate { get; set; } // 0-100%

    public double AverageReviewTimeHours { get; set; }
    public double MinReviewTimeHours { get; set; }
    public double MaxReviewTimeHours { get; set; }

    public int ReviewedTodayCount { get; set; }
    public int ApprovedTodayCount { get; set; }

    public DateTime GeneratedUtc { get; set; }
}
