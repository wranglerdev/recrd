namespace Recrd.Core.Ast;

public sealed record AssertionStep : IStep
{
    public AssertionType AssertionType { get; }
    public Selector Selector { get; }
    public IReadOnlyDictionary<string, string> Payload { get; }

    public AssertionStep(AssertionType AssertionType, Selector Selector, IReadOnlyDictionary<string, string> Payload)
    {
        this.AssertionType = AssertionType;
        this.Selector = Selector ?? throw new ArgumentNullException(nameof(Selector));
        this.Payload = Payload ?? throw new ArgumentNullException(nameof(Payload));
    }
}
