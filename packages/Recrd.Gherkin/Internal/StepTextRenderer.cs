using Recrd.Core.Ast;

namespace Recrd.Gherkin.Internal;

internal static class StepTextRenderer
{
    internal static string Render(IStep step) =>
        step switch
        {
            ActionStep a => RenderAction(a),
            AssertionStep a => RenderAssertion(a),
            _ => throw new InvalidOperationException($"Unknown step type: {step.GetType().Name}"),
        };

    private static string BestSelectorValue(Selector selector)
    {
        var preferenceOrder = new[]
        {
            SelectorStrategy.DataTestId,
            SelectorStrategy.Id,
            SelectorStrategy.Role,
        };

        foreach (var strategy in preferenceOrder)
        {
            if (selector.Values.TryGetValue(strategy, out var value))
                return value;
        }

        foreach (var strategy in selector.Strategies)
        {
            if (selector.Values.TryGetValue(strategy, out var value))
                return value;
        }

        return "(unknown)";
    }

    private static string RenderAction(ActionStep step) =>
        step.ActionType switch
        {
            ActionType.Navigate =>
                $"Navega para \"{step.Payload.GetValueOrDefault("url") ?? step.Payload.GetValueOrDefault("href") ?? BestSelectorValue(step.Selector)}\"",
            ActionType.Click =>
                $"Clica no elemento \"{BestSelectorValue(step.Selector)}\"",
            ActionType.Type =>
                $"Digita \"{step.Payload.GetValueOrDefault("value")}\" no campo \"{BestSelectorValue(step.Selector)}\"",
            ActionType.Select =>
                $"Seleciona \"{step.Payload.GetValueOrDefault("value")}\" no campo \"{BestSelectorValue(step.Selector)}\"",
            ActionType.Upload =>
                $"Envia o arquivo \"{step.Payload.GetValueOrDefault("filename") ?? step.Payload.GetValueOrDefault("path") ?? "(unknown)"}\" no campo \"{BestSelectorValue(step.Selector)}\"",
            ActionType.DragDrop =>
                $"Arrasta \"{BestSelectorValue(step.Selector)}\" para \"{step.Payload.GetValueOrDefault("target") ?? "(unknown)"}\"",
            _ => throw new InvalidOperationException($"Unknown action type: {step.ActionType}"),
        };

    private static string RenderAssertion(AssertionStep step) =>
        step.AssertionType switch
        {
            AssertionType.TextEquals =>
                $"O texto do elemento \"{BestSelectorValue(step.Selector)}\" \u00e9 \"{step.Payload.GetValueOrDefault("expected")}\"",
            AssertionType.TextContains =>
                $"O texto do elemento \"{BestSelectorValue(step.Selector)}\" cont\u00e9m \"{step.Payload.GetValueOrDefault("expected")}\"",
            AssertionType.Visible =>
                $"O elemento \"{BestSelectorValue(step.Selector)}\" est\u00e1 vis\u00edvel",
            AssertionType.Enabled =>
                $"O elemento \"{BestSelectorValue(step.Selector)}\" est\u00e1 habilitado",
            AssertionType.UrlMatches =>
                $"A URL corresponde a \"{step.Payload.GetValueOrDefault("pattern")}\"",
            _ => throw new InvalidOperationException($"Unknown assertion type: {step.AssertionType}"),
        };
}
