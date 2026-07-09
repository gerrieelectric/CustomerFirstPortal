namespace Portal.Application.Infrastructure.Notifications;

public class ReviewLinkOptions
{
    public string BaseUrl { get; set; } = "https://localhost:4200";
    public int TokenExpiryHours { get; set; } = 72;
}
