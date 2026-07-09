using Microsoft.Extensions.Options;
using Portal.Application.Infrastructure.Notifications;

namespace Portal.Application.Infrastructure.HostedServices;

public sealed class NotificationDispatchWorkerService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationDispatchOptions> options,
    ILogger<NotificationDispatchWorkerService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly NotificationDispatchOptions _options = options.Value;
    private readonly ILogger<NotificationDispatchWorkerService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Notification dispatch worker started. Enabled={Enabled}, BatchSize={BatchSize}",
            _options.Enabled,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_options.Enabled)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<INotificationDispatchProcessor>();
                    await processor.ProcessPendingAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification dispatch worker iteration failed.");
            }

            var delaySeconds = Math.Max(5, _options.WorkerIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
    }
}
