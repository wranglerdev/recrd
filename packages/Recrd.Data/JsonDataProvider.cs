using System.Runtime.CompilerServices;
using System.Text.Json;
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
        await using var stream = File.OpenRead(_filePath);
        var enumerator = JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(
            stream, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);

        await using var _ = enumerator;

        while (true)
        {
            bool hasNext;
            try
            {
                hasNext = await enumerator.MoveNextAsync();
            }
            catch (JsonException ex)
            {
                throw new DataParseException(
                    lineNumber: (int)(ex.LineNumber ?? 0) + 1,
                    offendingLine: string.Empty,
                    filePath: _filePath,
                    message: $"JSON root must be an array. {ex.Message}",
                    innerException: ex);
            }

            if (!hasNext)
                break;

            yield return FlattenElement(enumerator.Current, string.Empty);
        }
    }

    private static Dictionary<string, string> FlattenElement(JsonElement element, string prefix)
    {
        var result = new Dictionary<string, string>();

        if (element.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var prop in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var entry in FlattenElement(prop.Value, key))
                        result[entry.Key] = entry.Value;
                    break;

                case JsonValueKind.Array:
                    // Silently skip array fields per D-04
                    break;

                case JsonValueKind.Null:
                    result[key] = "";
                    break;

                case JsonValueKind.True:
                    result[key] = "True";
                    break;

                case JsonValueKind.False:
                    result[key] = "False";
                    break;

                default:
                    result[key] = prop.Value.ToString();
                    break;
            }
        }

        return result;
    }
}
