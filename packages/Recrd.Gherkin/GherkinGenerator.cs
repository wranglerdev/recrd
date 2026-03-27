using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Recrd.Gherkin.Internal;

namespace Recrd.Gherkin;

public sealed class GherkinGenerator : IGherkinGenerator
{
    public async Task GenerateAsync(
        Session session,
        IDataProvider? dataProvider,
        TextWriter output,
        GherkinGeneratorOptions? options = null,
        CancellationToken ct = default)
    {
        // Header (GHER-08, D-05)
        await output.WriteLineAsync("# language: pt");
        await output.WriteLineAsync();

        // Feature line (D-03)
        var featureName = session.Metadata.BaseUrl ?? "(sem URL base)";
        await output.WriteLineAsync($"Funcionalidade: {featureName}");
        await output.WriteLineAsync();

        // Tags (D-04)
        if (options?.Tags is { Count: > 0 } tags)
        {
            foreach (var tag in tags)
            {
                await output.WriteLineAsync($"  @{tag}");
            }
        }

        // Scenario keyword (GHER-01 vs GHER-02)
        if (session.Variables.Count == 0)
        {
            await output.WriteLineAsync($"  Cen\u00e1rio: {featureName}");
        }
        else
        {
            await output.WriteLineAsync($"  Esquema do Cen\u00e1rio: {featureName}");
        }

        // Step emission — collect rendered texts for first-appearance column ordering
        var renderedStepTexts = new List<string>();
        var hasGroupSteps = session.Steps.Any(s => s is GroupStep);

        if (hasGroupSteps)
        {
            // GroupStep path (GHER-05)
            await EmitGroupStepsAsync(session.Steps, output, renderedStepTexts);
        }
        else
        {
            // Default heuristic path (GHER-06)
            await EmitHeuristicStepsAsync(session.Steps, output, renderedStepTexts);
        }

        // Exemplos section for data-driven scenarios (GHER-02, GHER-03, GHER-04, GHER-09)
        if (session.Variables.Count > 0)
        {
            await EmitExemplosAsync(session, dataProvider, renderedStepTexts, output, options, ct);
        }

        // Trailing newline
        await output.WriteLineAsync();

        // Flush
        await output.FlushAsync(ct);
    }

    private static async Task EmitExemplosAsync(
        Session session,
        IDataProvider? dataProvider,
        IReadOnlyList<string> renderedStepTexts,
        TextWriter output,
        GherkinGeneratorOptions? options,
        CancellationToken ct)
    {
        // If no data provider is supplied, skip Exemplos emission silently
        if (dataProvider is null)
        {
            return;
        }

        // Materialize all rows
        var (dataColumnHeaders, rows) = await ExemplosTableBuilder.MaterializeDataAsync(dataProvider, ct);

        // Validate variable coverage (GHER-03): every declared variable must appear in data columns
        var dataColumnSet = new HashSet<string>(dataColumnHeaders, StringComparer.Ordinal);
        foreach (var variable in session.Variables)
        {
            if (!dataColumnSet.Contains(variable.Name))
            {
                throw new GherkinException(
                    variable.Name,
                    options?.DataFilePath ?? "(unknown)",
                    $"Variable '{variable.Name}' is declared in the session but missing from data columns. " +
                    $"Available columns: {string.Join(", ", dataColumnHeaders)}");
            }
        }

        // Warn on extra columns (GHER-04): data columns not referenced by any variable
        var sessionVariableNames = new HashSet<string>(
            session.Variables.Select(v => v.Name),
            StringComparer.Ordinal);

        var warningWriter = options?.WarningWriter;
        foreach (var column in dataColumnHeaders)
        {
            if (!sessionVariableNames.Contains(column))
            {
                var message = $"Warning: data column '{column}' is not referenced by any variable in the session and will be ignored.";
                if (warningWriter is not null)
                {
                    await warningWriter.WriteLineAsync(message);
                }
            }
        }

        // Derive column order by first appearance of variable placeholders in step texts (GHER-09)
        var columnOrder = ExemplosTableBuilder.DeriveColumnOrder(renderedStepTexts);

        // If no placeholders found in step texts (edge case), fall back to session variable declaration order
        if (columnOrder.Count == 0)
        {
            columnOrder = session.Variables.Select(v => v.Name).ToList();
        }

        // Only emit columns that are actually in the session variables
        columnOrder = columnOrder.Where(c => sessionVariableNames.Contains(c)).ToList();

        // Add any session variables not found via placeholders (maintain determinism)
        foreach (var variable in session.Variables)
        {
            if (!columnOrder.Contains(variable.Name))
            {
                columnOrder.Add(variable.Name);
            }
        }

        // Emit Exemplos block
        await output.WriteLineAsync();
        await output.WriteLineAsync("    Exemplos:");
        await output.WriteLineAsync(ExemplosTableBuilder.RenderHeader(columnOrder));
        foreach (var row in rows)
        {
            await output.WriteLineAsync(ExemplosTableBuilder.RenderRow(columnOrder, row));
        }
    }

    private static async Task EmitGroupStepsAsync(
        IReadOnlyList<IStep> topLevelSteps,
        TextWriter output,
        List<string> renderedStepTexts)
    {
        foreach (var step in topLevelSteps)
        {
            if (step is GroupStep groupStep)
            {
                var keyword = groupStep.GroupType switch
                {
                    GroupType.Given => "Dado",
                    GroupType.When => "Quando",
                    GroupType.Then => "Ent\u00e3o",
                    _ => "E",
                };

                var first = true;
                foreach (var child in groupStep.Steps)
                {
                    var rendered = StepTextRenderer.Render(child);
                    renderedStepTexts.Add(rendered);
                    var kw = first ? keyword : "E";
                    await output.WriteLineAsync($"    {kw} {rendered}");
                    first = false;
                }
            }
            else
            {
                // Non-GroupStep at top level (edge case): render with E continuation
                var rendered = StepTextRenderer.Render(step);
                renderedStepTexts.Add(rendered);
                await output.WriteLineAsync($"    E {rendered}");
            }
        }
    }

    private static async Task EmitHeuristicStepsAsync(
        IReadOnlyList<IStep> steps,
        TextWriter output,
        List<string> renderedStepTexts)
    {
        var classified = GroupingClassifier.Classify(steps);
        GroupType? currentGroup = null;

        foreach (var (group, step) in classified)
        {
            var rendered = StepTextRenderer.Render(step);
            renderedStepTexts.Add(rendered);

            string keyword;
            if (group != currentGroup)
            {
                // First step in a new region — use primary keyword
                keyword = group switch
                {
                    GroupType.Given => "Dado",
                    GroupType.When => "Quando",
                    GroupType.Then => "Ent\u00e3o",
                    _ => "E",
                };
                currentGroup = group;
            }
            else
            {
                keyword = "E";
            }

            await output.WriteLineAsync($"    {keyword} {rendered}");
        }
    }
}
