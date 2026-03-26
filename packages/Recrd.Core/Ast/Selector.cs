namespace Recrd.Core.Ast;

/// <summary>
/// Carries a ranked priority list of selector strategies and the corresponding selector values.
/// The first entry in <see cref="Strategies"/> is the highest-priority match.
/// </summary>
public sealed record Selector
{
    public IReadOnlyList<SelectorStrategy> Strategies { get; }
    public IReadOnlyDictionary<SelectorStrategy, string> Values { get; }

    public Selector(IReadOnlyList<SelectorStrategy> strategies, IReadOnlyDictionary<SelectorStrategy, string> values)
    {
        Strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }
}
