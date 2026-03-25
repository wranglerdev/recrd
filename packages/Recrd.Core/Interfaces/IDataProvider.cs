namespace Recrd.Core.Interfaces;

public interface IDataProvider
{
    IAsyncEnumerable<IReadOnlyDictionary<string, string>> ReadAsync(string filePath, DataProviderOptions options, CancellationToken cancellationToken = default);
}

public sealed record DataProviderOptions(char CsvDelimiter = ',');
