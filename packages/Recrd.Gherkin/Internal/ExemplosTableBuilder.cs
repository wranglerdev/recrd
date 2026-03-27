using System.Text.RegularExpressions;
using Recrd.Core.Interfaces;

namespace Recrd.Gherkin.Internal;

internal static partial class ExemplosTableBuilder
{
    [GeneratedRegex(@"<([^>]+)>")]
    private static partial Regex VariablePlaceholderPattern();

    /// <summary>
    /// Derives the Exemplos column order from first appearance of variable placeholders
    /// in the rendered step texts.
    /// </summary>
    internal static List<string> DeriveColumnOrder(IReadOnlyList<string> renderedStepTexts)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var order = new List<string>();

        foreach (var text in renderedStepTexts)
        {
            foreach (Match match in VariablePlaceholderPattern().Matches(text))
            {
                var variableName = match.Groups[1].Value;
                if (seen.Add(variableName))
                {
                    order.Add(variableName);
                }
            }
        }

        return order;
    }

    /// <summary>
    /// Renders the Exemplos header row.
    /// </summary>
    internal static string RenderHeader(IReadOnlyList<string> columnOrder)
    {
        return "      | " + string.Join(" | ", columnOrder) + " |";
    }

    /// <summary>
    /// Renders a single Exemplos data row.
    /// </summary>
    internal static string RenderRow(
        IReadOnlyList<string> columnOrder,
        IReadOnlyDictionary<string, string> row)
    {
        var cells = columnOrder
            .Select(col => row.TryGetValue(col, out var v) ? v : string.Empty)
            .ToList();
        return "      | " + string.Join(" | ", cells) + " |";
    }

    /// <summary>
    /// Materializes all rows from the data provider, returning column order (from first row keys)
    /// and all rows.
    /// </summary>
    internal static async Task<(List<string> ColumnOrder, List<IReadOnlyDictionary<string, string>> Rows)>
        MaterializeDataAsync(IDataProvider dataProvider, CancellationToken ct)
    {
        var rows = new List<IReadOnlyDictionary<string, string>>();

        await foreach (var row in dataProvider.StreamAsync(ct).ConfigureAwait(false))
        {
            rows.Add(row);
        }

        var columnOrder = rows.Count > 0
            ? rows[0].Keys.ToList()
            : new List<string>();

        return (columnOrder, rows);
    }
}
