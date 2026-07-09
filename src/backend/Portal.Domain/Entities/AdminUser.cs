using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class AdminUser
{
    public int AdminUserId { get; set; }
    public Guid EntraObjectId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public bool CanReview { get; set; } = true;
    public bool ReceiveNotifications { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Submission> ReviewedSubmissions { get; set; } = [];
    public ICollection<SubmissionReview> Reviews { get; set; } = [];
}
