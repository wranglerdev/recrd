using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Compilers.Internal;

internal static class SeleniumKeywordEmitter
{
    private const string Sep = "    ";

    internal static async Task WriteKeywordAsync(
        TextWriter writer,
        ActionStep step,
        CompilerOptions options,
        List<string> warnings)
    {
        var (selector, warned) = SelectorResolver.ResolveSelenium(step.Selector, options.PreferredSelectorStrategy);
        if (warned)
            warnings.Add($"Could not resolve selector for {step.ActionType} step; emitted '(unknown)'.");

        switch (step.ActionType)
        {
            case ActionType.Click:
                await writer.WriteLineAsync($"{Sep}Click Element{Sep}{selector}");
                break;

            case ActionType.Type:
                var typeValue = step.Payload.GetValueOrDefault("value") ?? string.Empty;
                await writer.WriteLineAsync($"{Sep}Input Text{Sep}{selector}{Sep}{typeValue}");
                break;

            case ActionType.Select:
                var selectValue = step.Payload.GetValueOrDefault("value") ?? string.Empty;
                await writer.WriteLineAsync($"{Sep}Select From List By Value{Sep}{selector}{Sep}{selectValue}");
                break;

            case ActionType.Navigate:
                var url = step.Payload.GetValueOrDefault("url") ?? step.Payload.GetValueOrDefault("href") ?? string.Empty;
                await writer.WriteLineAsync($"{Sep}Go To{Sep}{url}");
                break;

            case ActionType.Upload:
                var path = step.Payload.GetValueOrDefault("path") ?? step.Payload.GetValueOrDefault("filename") ?? string.Empty;
                await writer.WriteLineAsync($"{Sep}Choose File{Sep}{selector}{Sep}{path}");
                break;

            case ActionType.DragDrop:
                var targetRaw = step.Payload.GetValueOrDefault("target") ?? string.Empty;
                var targetSelector = string.IsNullOrEmpty(targetRaw) ? targetRaw : $"css:{targetRaw}";
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
        var (selector, warned) = SelectorResolver.ResolveSelenium(step.Selector, options.PreferredSelectorStrategy);
        if (warned)
            warnings.Add($"Could not resolve selector for {step.AssertionType} assertion; emitted '(unknown)'.");

        var expected = step.Payload.GetValueOrDefault("expected")
            ?? step.Payload.GetValueOrDefault("pattern")
            ?? string.Empty;

        switch (step.AssertionType)
        {
            case AssertionType.TextEquals:
                await writer.WriteLineAsync($"{Sep}Element Text Should Be{Sep}{selector}{Sep}{expected}");
                break;

            case AssertionType.TextContains:
                await writer.WriteLineAsync($"{Sep}Element Should Contain{Sep}{selector}{Sep}{expected}");
                break;

            case AssertionType.Visible:
                await writer.WriteLineAsync($"{Sep}Element Should Be Visible{Sep}{selector}");
                break;

            case AssertionType.Enabled:
                await writer.WriteLineAsync($"{Sep}Element Should Be Enabled{Sep}{selector}");
                break;

            case AssertionType.UrlMatches:
                await writer.WriteLineAsync($"{Sep}Location Should Contain{Sep}{expected}");
                break;
        }
    }
}
