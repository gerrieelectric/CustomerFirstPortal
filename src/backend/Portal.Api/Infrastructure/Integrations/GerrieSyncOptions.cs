namespace Portal.Application.Infrastructure.Integrations;

public class GerrieSyncOptions
{
    public bool Enabled { get; set; } = true;
    public string EndpointPath { get; set; } = "/recognitions";
    public int MaxAttempts { get; set; } = 5;
    public int BaseDelaySeconds { get; set; } = 60;
    public int WorkerIntervalSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 25;
}
