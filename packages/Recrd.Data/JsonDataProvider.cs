using System.Runtime.CompilerServices;
using Recrd.Core.Interfaces;

namespace Recrd.Data;

public sealed class JsonDataProvider : IDataProvider
{
    private readonly string _filePath;

    public JsonDataProvider(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public async IAsyncEnumerable<IReadOnlyDictionary<string, string>> StreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO: Phase 3 Plan 03 — implement System.Text.Json streaming
        await Task.CompletedTask;
        throw new NotImplementedException();
#pragma warning disable CS0162 // Unreachable code — yield required for async iterator signature
        yield break;
#pragma warning restore CS0162
    }
}
