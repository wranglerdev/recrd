using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Compilers.Internal;

internal static class SelectorResolver
{
    internal static (string formatted, bool warned) ResolveBrowser(Selector selector, SelectorStrategy preferred) =>
        throw new NotImplementedException();

    internal static (string formatted, bool warned) ResolveSelenium(Selector selector, SelectorStrategy preferred) =>
        throw new NotImplementedException();
}
