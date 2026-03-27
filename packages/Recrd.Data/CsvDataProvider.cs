using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
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
        using var reader = new StreamReader(_filePath, detectEncodingFromByteOrderMarks: true);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = _delimiter,
            BadDataFound = args =>
            {
                var row = args.Context?.Parser?.Row ?? 0;
                var raw = args.Context?.Parser?.RawRecord ?? string.Empty;
                throw new DataParseException(row, raw, _filePath, $"Bad CSV data at line {row}: {args.Field}");
            },
            MissingFieldFound = null,
        };

        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        if (csv.HeaderRecord is null)
            throw new DataParseException(1, string.Empty, _filePath, "CSV file has no header row");

        var headers = csv.HeaderRecord;
        var headerCount = headers.Length;

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowData = BuildRow(csv, headers, headerCount);
            yield return rowData;
        }
    }

    private IReadOnlyDictionary<string, string> BuildRow(CsvReader csv, string[] headers, int headerCount)
    {
        try
        {
            if (csv.Parser.Count != headerCount)
                throw new DataParseException(
                    csv.Parser.Row,
                    csv.Parser.RawRecord ?? string.Empty,
                    _filePath,
                    $"Row {csv.Parser.Row} has {csv.Parser.Count} fields but header has {headerCount} columns");

            var dict = new Dictionary<string, string>(headerCount);
            for (int i = 0; i < headerCount; i++)
                dict[headers[i]] = csv.GetField(i) ?? string.Empty;

            return dict;
        }
        catch (DataParseException)
        {
            throw;
        }
        catch (CsvHelperException ex)
        {
            throw new DataParseException(
                csv.Parser.Row,
                csv.Parser.RawRecord ?? string.Empty,
                _filePath,
                ex.Message,
                ex);
        }
    }
}
