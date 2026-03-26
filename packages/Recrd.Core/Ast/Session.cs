namespace Recrd.Core.Ast;

public sealed record Session
{
    public int SchemaVersion { get; }
    public SessionMetadata Metadata { get; }
    public IReadOnlyList<Variable> Variables { get; }
    public IReadOnlyList<IStep> Steps { get; }

    public Session(int SchemaVersion, SessionMetadata Metadata, IReadOnlyList<Variable> Variables, IReadOnlyList<IStep> Steps)
    {
        this.SchemaVersion = SchemaVersion;
        this.Metadata = Metadata ?? throw new ArgumentNullException(nameof(Metadata));
        this.Variables = Variables ?? throw new ArgumentNullException(nameof(Variables));
        this.Steps = Steps ?? throw new ArgumentNullException(nameof(Steps));
    }
}
