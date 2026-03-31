using System.Text.Json;
using Recrd.Core.Ast;

namespace Recrd.Recording.Selectors;

/// <summary>
/// Parses the selector JSON object produced by the JS recording agent's __extractSelectors function
/// and converts it to a <see cref="Selector"/> instance.
/// </summary>
internal static class SelectorExtractor
{
    private static readonly IReadOnlyDictionary<string, SelectorStrategy> _strategyMap =
        new Dictionary<string, SelectorStrategy>(StringComparer.Ordinal)
        {
            ["DataTestId"] = SelectorStrategy.DataTestId,
            ["Id"] = SelectorStrategy.Id,
            ["Role"] = SelectorStrategy.Role,
            ["Css"] = SelectorStrategy.Css,
            ["XPath"] = SelectorStrategy.XPath,
        };

    /// <summary>
    /// Extracts a <see cref="Selector"/> from the JSON element produced by the JS agent.
    /// Expected shape: <c>{ strategies: string[], values: { [key]: string } }</c>
    /// </summary>
    public static Selector Extract(JsonElement selectorsJson)
    {
        var strategies = new List<SelectorStrategy>();
        var values = new Dictionary<SelectorStrategy, string>();

        if (selectorsJson.ValueKind == JsonValueKind.Object)
        {
            // Parse strategies array
            if (selectorsJson.TryGetProperty("strategies", out var strategiesEl)
                && strategiesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in strategiesEl.EnumerateArray())
                {
                    var name = item.GetString();
                    if (name is not null && _strategyMap.TryGetValue(name, out var strategy))
                    {
                        if (!strategies.Contains(strategy))
                            strategies.Add(strategy);
                    }
                }
            }

            // Parse values object
            if (selectorsJson.TryGetProperty("values", out var valuesEl)
                && valuesEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in valuesEl.EnumerateObject())
                {
                    if (_strategyMap.TryGetValue(prop.Name, out var strategy))
                    {
                        var val = prop.Value.GetString();
                        if (val is not null)
                            values[strategy] = val;
                    }
                }
            }
        }

        // Ensure Css is always present as fallback
        if (!strategies.Contains(SelectorStrategy.Css))
        {
            strategies.Add(SelectorStrategy.Css);
            values[SelectorStrategy.Css] = "*";
        }

        return new Selector(strategies.AsReadOnly(), values);
    }
}
