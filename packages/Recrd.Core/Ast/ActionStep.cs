namespace Recrd.Core.Ast;

public sealed record ActionStep : IStep
{
    public ActionType ActionType { get; }
    public Selector Selector { get; }
    public IReadOnlyDictionary<string, string> Payload { get; }

    public ActionStep(ActionType ActionType, Selector Selector, IReadOnlyDictionary<string, string> Payload)
    {
        this.ActionType = ActionType;
        this.Selector = Selector ?? throw new ArgumentNullException(nameof(Selector));
        this.Payload = Payload ?? throw new ArgumentNullException(nameof(Payload));
    }
}
