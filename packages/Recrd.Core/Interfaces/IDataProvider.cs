namespace Recrd.Core.Interfaces;

public interface IDataProvider
{
    IAsyncEnumerable<IReadOnlyDictionary<string, string>> StreamAsync(CancellationToken cancellationToken = default);
}
