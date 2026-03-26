namespace Recrd.Core.Ast;

public sealed record Session
{
    public int SchemaVersion { get; }
    public SessionMetadata Metadata { get; }
    public IReadOnlyList<Variable> Variables { get; }
    public IReadOnlyList<IStep> Steps { get; }

    public Session(int schemaVersion, SessionMetadata metadata, IReadOnlyList<Variable> variables, IReadOnlyList<IStep> steps)
    {
        SchemaVersion = schemaVersion;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Variables = variables ?? throw new ArgumentNullException(nameof(variables));
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }
}
