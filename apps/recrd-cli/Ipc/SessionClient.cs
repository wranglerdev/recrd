// SessionClient — Unix domain socket client for pause/resume/stop commands (D-02, D-04)
// Full implementation in Plan 02 (session control commands)

namespace Recrd.Cli.Ipc;

/// <summary>
/// Connects to ~/.recrd/session.sock and sends a JSON control command
/// ({ "command": "pause" | "resume" | "stop" }).
/// </summary>
internal sealed class SessionClient
{
    // Stub — full implementation in Plan 02
}
