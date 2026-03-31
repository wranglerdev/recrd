using Recrd.Core.Ast;

namespace Recrd.Core.Interfaces;

public interface IRecorderEngine : IAsyncDisposable
{
    Task<Session> StartAsync(RecorderOptions options, CancellationToken cancellationToken = default);
    Task PauseAsync(CancellationToken cancellationToken = default);
    Task ResumeAsync(CancellationToken cancellationToken = default);
    Task<Session> StopAsync(string outputPath, CancellationToken cancellationToken = default);
    Task<Session> RecoverAsync(string partialPath, CancellationToken cancellationToken = default);
}
