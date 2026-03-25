using Recrd.Core.Ast;

namespace Recrd.Core.Interfaces;

public interface IEventInterceptor
{
    ValueTask<RecordedEvent?> InterceptAsync(RecordedEvent @event, CancellationToken cancellationToken = default);
}
