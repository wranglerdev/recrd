// SessionSocket — Unix domain socket server for session lifecycle control (D-02, D-04)
// Full implementation in Plan 02 (start command + recording engine wiring)

namespace Recrd.Cli.Ipc;

/// <summary>
/// Unix domain socket server that accepts control commands (pause/resume/stop)
/// while a recording session is active. Socket file lives at ~/.recrd/session.sock.
/// </summary>
internal sealed class SessionSocket : IAsyncDisposable
{
    // Stub — full implementation in Plan 02
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
