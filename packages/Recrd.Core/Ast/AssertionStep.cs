namespace Recrd.Core.Ast;

public sealed record AssertionStep : IStep
{
    public AssertionType AssertionType { get; }
    public Selector Selector { get; }
    public IReadOnlyDictionary<string, string> Payload { get; }

    public AssertionStep(AssertionType assertionType, Selector selector, IReadOnlyDictionary<string, string> payload)
    {
        AssertionType = assertionType;
        Selector = selector ?? throw new ArgumentNullException(nameof(selector));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }
}
