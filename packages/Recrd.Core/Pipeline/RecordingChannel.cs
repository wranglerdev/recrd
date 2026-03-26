using System.Threading.Channels;

namespace Recrd.Core.Pipeline;

public sealed class RecordingChannel : IRecordingChannel, IDisposable
{
    private readonly Channel<RecordedEvent> _channel;
    private readonly CancellationTokenSource _cts = new();

    public RecordingChannel(int capacity = 1000)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        _channel = Channel.CreateBounded<RecordedEvent>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = false
            });
    }

    public ValueTask WriteAsync(RecordedEvent evt, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        return _channel.Writer.WriteAsync(evt, linkedCts.Token);
    }

    public IAsyncEnumerable<RecordedEvent> ReadAllAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAllAsync(cancellationToken);

    public void Complete()
        => _channel.Writer.Complete();

    public void Cancel(Exception? error = null)
    {
        _cts.Cancel();
        _channel.Writer.Complete(error ?? new OperationCanceledException());
    }

    public void Dispose()
        => _cts.Dispose();
}
