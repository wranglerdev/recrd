namespace Recrd.Core.Pipeline;

public interface IRecordingChannel
{
    ValueTask WriteAsync(RecordedEvent evt, CancellationToken cancellationToken = default);
    IAsyncEnumerable<RecordedEvent> ReadAllAsync(CancellationToken cancellationToken = default);
    void Complete();
    void Cancel(Exception? error = null);
}
