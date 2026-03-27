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

        // Step emission
        var hasGroupSteps = session.Steps.Any(s => s is GroupStep);

        if (hasGroupSteps)
        {
            // GroupStep path (GHER-05)
            await EmitGroupStepsAsync(session.Steps, output);
        }
        else
        {
            // Default heuristic path (GHER-06)
            await EmitHeuristicStepsAsync(session.Steps, output);
        }

        // Trailing newline
        await output.WriteLineAsync();

        // Flush
        await output.FlushAsync(ct);
    }

    private static async Task EmitGroupStepsAsync(IReadOnlyList<IStep> topLevelSteps, TextWriter output)
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
                    var kw = first ? keyword : "E";
                    await output.WriteLineAsync($"    {kw} {StepTextRenderer.Render(child)}");
                    first = false;
                }
            }
            else
            {
                // Non-GroupStep at top level (edge case): render with E continuation
                await output.WriteLineAsync($"    E {StepTextRenderer.Render(step)}");
            }
        }
    }

    private static async Task EmitHeuristicStepsAsync(IReadOnlyList<IStep> steps, TextWriter output)
    {
        var classified = GroupingClassifier.Classify(steps);
        GroupType? currentGroup = null;

        foreach (var (group, step) in classified)
        {
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

            await output.WriteLineAsync($"    {keyword} {StepTextRenderer.Render(step)}");
        }
    }
}
