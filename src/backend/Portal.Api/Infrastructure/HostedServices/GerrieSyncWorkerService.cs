using Microsoft.Extensions.Options;
using Portal.Application.Infrastructure.Integrations;

namespace Portal.Application.Infrastructure.HostedServices;

public sealed class GerrieSyncWorkerService(
    IServiceScopeFactory scopeFactory,
    IOptions<GerrieSyncOptions> options,
    ILogger<GerrieSyncWorkerService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly GerrieSyncOptions _options = options.Value;
    private readonly ILogger<GerrieSyncWorkerService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Gerrie sync worker started. Enabled={Enabled}, BatchSize={BatchSize}",
            _options.Enabled,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_options.Enabled)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IGerrieSyncService>();
                    await syncService.ProcessPendingAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gerrie sync worker iteration failed.");
            }

            var delaySeconds = Math.Max(5, _options.WorkerIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
    }
}
