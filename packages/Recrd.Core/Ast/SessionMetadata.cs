namespace Recrd.Core.Ast;

public sealed record SessionMetadata(
    Guid Id,
    DateTimeOffset CreatedAt,
    string BrowserEngine,
    string ViewportSize,
    string? BaseUrl
);
