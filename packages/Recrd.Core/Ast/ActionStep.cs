namespace Recrd.Core.Ast;

public sealed record ActionStep : IStep
{
    public ActionType ActionType { get; }
    public Selector Selector { get; }
    public IReadOnlyDictionary<string, string> Payload { get; }

    public ActionStep(ActionType actionType, Selector selector, IReadOnlyDictionary<string, string> payload)
    {
        ActionType = actionType;
        Selector = selector ?? throw new ArgumentNullException(nameof(selector));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }
}
