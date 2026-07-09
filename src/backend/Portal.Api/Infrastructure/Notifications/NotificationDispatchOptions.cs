namespace Portal.Application.Infrastructure.Notifications;

public class NotificationDispatchOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxAttempts { get; set; } = 5;
    public int BaseDelaySeconds { get; set; } = 30;
    public int WorkerIntervalSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 50;
}
