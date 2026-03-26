namespace Recrd.Core.Ast;

public sealed record GroupStep : IStep
{
    public GroupType GroupType { get; }
    public IReadOnlyList<IStep> Steps { get; }

    public GroupStep(GroupType groupType, IReadOnlyList<IStep> steps)
    {
        GroupType = groupType;
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }
}
