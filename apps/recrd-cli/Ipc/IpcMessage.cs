// IpcMessage — newline-delimited JSON IPC command message (D-04)

namespace Recrd.Cli.Ipc;

/// <summary>
/// Represents a JSON-serializable IPC command sent from a control command (pause/resume/stop)
/// to the running start process over the Unix domain socket.
/// Protocol: newline-delimited JSON — one IpcMessage per line.
/// </summary>
internal sealed record IpcMessage(string Command);
