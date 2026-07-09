namespace Portal.Application.DTOs.Submissions;

public class AdminReviewRequest
{
    public string ReviewDecision { get; set; } = null!;

    public string? Notes { get; set; }

    public string? EditedRecognitionReason { get; set; }

    public string RowVersion { get; set; } = null!;
}
