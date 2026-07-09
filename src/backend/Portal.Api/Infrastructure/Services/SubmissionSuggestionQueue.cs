using System.Threading.Channels;

namespace Portal.Application.Infrastructure.Services;

public sealed class SubmissionSuggestionQueue : ISubmissionSuggestionQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateBounded<Guid>(
        new BoundedChannelOptions(512)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    public bool TryQueue(Guid submissionId)
    {
        return _queue.Writer.TryWrite(submissionId);
    }

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}
