using System.Runtime.CompilerServices;
using Recrd.Core.Interfaces;

namespace Recrd.Data;

public sealed class CsvDataProvider : IDataProvider
{
    private readonly string _filePath;
    private readonly string _delimiter;

    public CsvDataProvider(string filePath, string delimiter = ",")
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _delimiter = delimiter ?? throw new ArgumentNullException(nameof(delimiter));
    }

    public async IAsyncEnumerable<IReadOnlyDictionary<string, string>> StreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO: Phase 3 Plan 02 — implement CsvHelper streaming
        await Task.CompletedTask;
        throw new NotImplementedException();
#pragma warning disable CS0162 // Unreachable code — yield required for async iterator signature
        yield break;
#pragma warning restore CS0162
    }
}
