namespace Recrd.Core.Ast;

public sealed record Session(
    int SchemaVersion,
    SessionMetadata Metadata,
    IReadOnlyList<Variable> Variables,
    IReadOnlyList<Step> Steps
);
