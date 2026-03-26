namespace Recrd.Core.Ast;

public sealed record SessionMetadata
{
    public string Id { get; }
    public DateTimeOffset CreatedAt { get; }
    public string BrowserEngine { get; }
    public ViewportSize ViewportSize { get; }
    public string? BaseUrl { get; init; }

    public SessionMetadata(string id, DateTimeOffset createdAt, string browserEngine, ViewportSize viewportSize, string? baseUrl = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        CreatedAt = createdAt;
        BrowserEngine = browserEngine ?? throw new ArgumentNullException(nameof(browserEngine));
        ViewportSize = viewportSize ?? throw new ArgumentNullException(nameof(viewportSize));
        BaseUrl = baseUrl;
    }
}
