namespace Portal.Application.Infrastructure.Services;

public interface ISubmissionSuggestionQueue
{
    bool TryQueue(Guid submissionId);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
