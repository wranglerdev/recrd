namespace Recrd.Core.Interfaces;

public interface IRecorderEngine
{
    Task StartAsync(RecorderOptions options, CancellationToken cancellationToken = default);
    Task PauseAsync(CancellationToken cancellationToken = default);
    Task ResumeAsync(CancellationToken cancellationToken = default);
    Task<Ast.Session> StopAsync(CancellationToken cancellationToken = default);
}

public sealed record RecorderOptions(
    string Browser = "chromium",
    bool Headed = true,
    string Viewport = "1280x720",
    string? BaseUrl = null
);
