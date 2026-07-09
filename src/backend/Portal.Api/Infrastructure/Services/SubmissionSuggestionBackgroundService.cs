using Microsoft.EntityFrameworkCore;
using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Services;

public sealed class SubmissionSuggestionBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ISubmissionSuggestionQueue queue,
    ILogger<SubmissionSuggestionBackgroundService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly ISubmissionSuggestionQueue _queue = queue;
    private readonly ILogger<SubmissionSuggestionBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid submissionId;
            try
            {
                submissionId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Portal.Application.Infrastructure.Persistence.PortalDbContext>();
                var suggestionService = scope.ServiceProvider.GetRequiredService<ISuggestionGenerationService>();

                var submission = await dbContext.Submissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, stoppingToken);

                if (submission == null)
                {
                    _logger.LogWarning("Submission {SubmissionId} not found for suggestion generation.", submissionId);
                    continue;
                }

                await suggestionService.GenerateSuggestionsAsync(submission, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Suggestion background processing failed for submission {SubmissionId}", submissionId);
            }
        }
    }
}
