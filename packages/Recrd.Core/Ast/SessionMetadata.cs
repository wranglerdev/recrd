namespace Recrd.Core.Ast;

public sealed record SessionMetadata
{
    public string Id { get; }
    public DateTimeOffset CreatedAt { get; }
    public string BrowserEngine { get; }
    public ViewportSize ViewportSize { get; }
    public string? BaseUrl { get; init; }

    public SessionMetadata(string Id, DateTimeOffset CreatedAt, string BrowserEngine, ViewportSize ViewportSize, string? BaseUrl = null)
    {
        this.Id = Id ?? throw new ArgumentNullException(nameof(Id));
        this.CreatedAt = CreatedAt;
        this.BrowserEngine = BrowserEngine ?? throw new ArgumentNullException(nameof(BrowserEngine));
        this.ViewportSize = ViewportSize ?? throw new ArgumentNullException(nameof(ViewportSize));
        this.BaseUrl = BaseUrl;
    }
}
