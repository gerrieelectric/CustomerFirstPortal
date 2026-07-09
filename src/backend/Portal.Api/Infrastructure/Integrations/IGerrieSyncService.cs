using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Integrations;

public interface IGerrieSyncService
{
    Task SyncApprovedSubmissionAsync(
        Submission submission,
        string actorName,
        string? actorEmail,
        CancellationToken cancellationToken = default);

    Task QueueSubmissionSyncAsync(
        Submission submission,
        string actorName,
        string? actorEmail,
        CancellationToken cancellationToken = default);

    Task ProcessPendingAsync(CancellationToken cancellationToken);
}
