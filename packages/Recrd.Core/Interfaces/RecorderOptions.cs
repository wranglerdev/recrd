using Recrd.Core.Ast;

namespace Recrd.Core.Interfaces;

public sealed record RecorderOptions
{
    public string BrowserEngine { get; init; } = "chromium";
    public bool Headed { get; init; } = true;
    public ViewportSize ViewportSize { get; init; } = new(1280, 720);
    public string? BaseUrl { get; init; }
    public string OutputDirectory { get; init; } = ".";
    public TimeSpan SnapshotInterval { get; init; } = TimeSpan.FromSeconds(30);
}
