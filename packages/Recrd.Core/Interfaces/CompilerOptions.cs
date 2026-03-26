using Recrd.Core.Ast;

namespace Recrd.Core.Interfaces;

public sealed record CompilerOptions
{
    public string OutputDirectory { get; init; } = ".";
    public SelectorStrategy PreferredSelectorStrategy { get; init; } = SelectorStrategy.DataTestId;
    public int TimeoutSeconds { get; init; } = 30;
}
