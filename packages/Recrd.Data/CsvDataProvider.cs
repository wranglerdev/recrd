using Recrd.Core.Interfaces;

namespace Recrd.Data;

public sealed class CsvDataProvider : IDataProvider
{
    public async IAsyncEnumerable<IReadOnlyDictionary<string, string>> ReadAsync(
        string filePath,
        DataProviderOptions options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
        yield break;
    }
}
