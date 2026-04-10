using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Compilers.Internal;

internal static class BrowserKeywordEmitter
{
    private const string Sep = "    ";

    internal static async Task WriteKeywordAsync(
        TextWriter writer,
        ActionStep step,
        CompilerOptions options,
        List<string> warnings)
    {
        var (selector, warned) = SelectorResolver.ResolveBrowser(step.Selector, options.PreferredSelectorStrategy);
        if (warned)
            warnings.Add($"Could not resolve selector for {step.ActionType} step; emitted '(unknown)'.");

        var timeout = options.TimeoutSeconds;

        switch (step.ActionType)
        {
            case ActionType.Click:
                await writer.WriteLineAsync($"{Sep}Wait For Elements State{Sep}{selector}{Sep}visible{Sep}timeout={timeout}s");
                await writer.WriteLineAsync($"{Sep}Click{Sep}{selector}");
                break;

            case ActionType.Type:
                await writer.WriteLineAsync($"{Sep}Wait For Elements State{Sep}{selector}{Sep}visible{Sep}timeout={timeout}s");
                await writer.WriteLineAsync($"{Sep}Type Text{Sep}{selector}{Sep}${{value}}");
                break;

            case ActionType.Select:
                await writer.WriteLineAsync($"{Sep}Wait For Elements State{Sep}{selector}{Sep}visible{Sep}timeout={timeout}s");
                await writer.WriteLineAsync($"{Sep}Select Options By{Sep}{selector}{Sep}value{Sep}${{value}}");
                break;

            case ActionType.Navigate:
                var url = step.Payload.GetValueOrDefault("url") ?? step.Payload.GetValueOrDefault("href") ?? string.Empty;
                await writer.WriteLineAsync($"{Sep}Go To{Sep}{url}");
                break;

            case ActionType.Upload:
                var path = step.Payload.GetValueOrDefault("path") ?? step.Payload.GetValueOrDefault("filename") ?? string.Empty;
                await writer.WriteLineAsync($"{Sep}Wait For Elements State{Sep}{selector}{Sep}visible{Sep}timeout={timeout}s");
                await writer.WriteLineAsync($"{Sep}Upload File By Selector{Sep}{selector}{Sep}{path}");
                break;

            case ActionType.DragDrop:
                var targetRaw = step.Payload.GetValueOrDefault("target") ?? string.Empty;
                var targetSelector = string.IsNullOrEmpty(targetRaw) ? targetRaw : $"css={targetRaw}";
                await writer.WriteLineAsync($"{Sep}Wait For Elements State{Sep}{selector}{Sep}visible{Sep}timeout={timeout}s");
                await writer.WriteLineAsync($"{Sep}Drag And Drop{Sep}{selector}{Sep}{targetSelector}");
                break;
        }
    }

    internal static async Task WriteAssertionKeywordAsync(
        TextWriter writer,
        AssertionStep step,
        CompilerOptions options,
        List<string> warnings)
    {
        var (selector, warned) = SelectorResolver.ResolveBrowser(step.Selector, options.PreferredSelectorStrategy);
        if (warned)
            warnings.Add($"Could not resolve selector for {step.AssertionType} assertion; emitted '(unknown)'.");

        switch (step.AssertionType)
        {
            case AssertionType.TextEquals:
                await writer.WriteLineAsync($"{Sep}Get Text{Sep}{selector}{Sep}=={Sep}${{expected}}");
                break;

            case AssertionType.TextContains:
                await writer.WriteLineAsync($"{Sep}Get Text{Sep}{selector}{Sep}contains{Sep}${{expected}}");
                break;

            case AssertionType.Visible:
                await writer.WriteLineAsync($"{Sep}Wait For Elements State{Sep}{selector}{Sep}visible");
                break;

            case AssertionType.Enabled:
                await writer.WriteLineAsync($"{Sep}Wait For Elements State{Sep}{selector}{Sep}enabled");
                break;

            case AssertionType.UrlMatches:
                await writer.WriteLineAsync($"{Sep}Get Url{Sep}=={Sep}${{expected}}");
                break;
        }
    }
}
