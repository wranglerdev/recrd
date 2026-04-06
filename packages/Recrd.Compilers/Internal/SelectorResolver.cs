using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Compilers.Internal;

internal static class SelectorResolver
{
    private static readonly SelectorStrategy[] FallbackChain =
    [
        SelectorStrategy.DataTestId,
        SelectorStrategy.Id,
        SelectorStrategy.Role,
        SelectorStrategy.Css,
        SelectorStrategy.XPath
    ];

    internal static (string formatted, bool warned) ResolveBrowser(Selector selector, SelectorStrategy preferred)
    {
        var (strategy, value, found) = Resolve(selector, preferred);
        if (!found)
            return ("(unknown)", warned: true);
        return (FormatBrowser(strategy, value), warned: false);
    }

    internal static (string formatted, bool warned) ResolveSelenium(Selector selector, SelectorStrategy preferred)
    {
        var (strategy, value, found) = Resolve(selector, preferred);
        if (!found)
            return ("(unknown)", warned: true);
        return (FormatSelenium(strategy, value), warned: false);
    }

    private static (SelectorStrategy strategy, string value, bool found) Resolve(Selector selector, SelectorStrategy preferred)
    {
        // Try preferred strategy first
        if (selector.Values.TryGetValue(preferred, out var preferredValue))
            return (preferred, preferredValue, true);

        // Walk fallback chain
        foreach (var strategy in FallbackChain)
        {
            if (strategy == preferred)
                continue;
            if (selector.Values.TryGetValue(strategy, out var chainValue))
                return (strategy, chainValue, true);
        }

        // Last resort: first available from Strategies list
        var firstStrategy = selector.Strategies.FirstOrDefault();
        if (selector.Values.TryGetValue(firstStrategy, out var lastValue))
            return (firstStrategy, lastValue, true);

        return (default, string.Empty, false);
    }

    private static string FormatBrowser(SelectorStrategy strategy, string value) => strategy switch
    {
        SelectorStrategy.DataTestId => $@"css=[data-testid=""{value}""]",
        SelectorStrategy.Id => $"id={value}",
        SelectorStrategy.Role => $"role={value}",
        SelectorStrategy.Css => $"css={value}",
        SelectorStrategy.XPath => $"xpath={value}",
        _ => $"css={value}"
    };

    private static string FormatSelenium(SelectorStrategy strategy, string value) => strategy switch
    {
        SelectorStrategy.DataTestId => $"css:[data-testid=\"{value}\"]",
        SelectorStrategy.Id => $"id:{value}",
        SelectorStrategy.Role => $"css:[role=\"{value}\"]",
        SelectorStrategy.Css => $"css:{value}",
        SelectorStrategy.XPath => $"xpath:{value}",
        _ => $"css:{value}"
    };
}
