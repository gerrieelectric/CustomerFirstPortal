namespace Portal.Application.DTOs.Analytics;

public class SubmissionTypeAnalysisDto
{
    public List<TypeBreakdown> Types { get; set; } = [];
    public DateTime GeneratedUtc { get; set; }

    public class TypeBreakdown
    {
        public string SubmissionType { get; set; } = string.Empty;
        public int Count { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public decimal ApprovalRate { get; set; }
        public double AverageServiceRating { get; set; }
    }
}
