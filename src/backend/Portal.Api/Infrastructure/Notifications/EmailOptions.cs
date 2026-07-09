namespace Portal.Application.Infrastructure.Notifications;

public class EmailOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string From { get; set; } = "noreply@gerrie.com";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DevelopmentOverrideTo { get; set; } = string.Empty;
}
