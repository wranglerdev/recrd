using Recrd.Core.Pipeline;

namespace Recrd.Core.Interfaces;

public interface IEventInterceptor
{
    ValueTask<RecordedEvent?> InterceptAsync(RecordedEvent evt, CancellationToken cancellationToken = default);
}
