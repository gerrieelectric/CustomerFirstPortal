namespace Portal.Application.DTOs.Analytics;

public class AdminReviewActivityDto
{
    public List<AdminActivityItem> AdminStats { get; set; } = [];
    public DateTime GeneratedUtc { get; set; }

    public class AdminActivityItem
    {
        public int AdminUserId { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public int TotalReviewsCompleted { get; set; }
        public int ApprovalsCount { get; set; }
        public int RejectionsCount { get; set; }
        public double AverageReviewTimeHours { get; set; }
        public DateTime LastReviewUtc { get; set; }
    }
}
