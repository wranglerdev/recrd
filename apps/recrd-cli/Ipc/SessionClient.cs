// SessionClient — Unix domain socket client for pause/resume/stop commands (D-02, D-04)

using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Recrd.Cli.Output;

namespace Recrd.Cli.Ipc;

/// <summary>
/// Connects to ~/.recrd/session.sock and sends a JSON control command
/// ({ "command": "pause" | "resume" | "stop" }).
/// This is a thin client — it connects, sends one line, and disconnects.
/// </summary>
internal static class SessionClient
{
    /// <summary>
    /// Sends a control command to the active session socket.
    /// </summary>
    /// <param name="command">One of: "pause", "resume", "stop"</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>0 on success, 1 if no active session is found.</returns>
    public static async Task<int> SendCommandAsync(string command, ILogger logger, CancellationToken ct)
    {
        var socketPath = SessionSocket.DefaultSocketPath;

        if (!File.Exists(socketPath))
        {
            CliOutput.WriteError("No active session found. Start one with 'recrd start'.");
            return 1;
        }

        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), ct);
        }
        catch (SocketException ex)
        {
            logger.LogDebug(ex, "Failed to connect to session socket at {SocketPath}", socketPath);
            CliOutput.WriteError("No active session found. Start one with 'recrd start'.");
            return 1;
        }

        await using var networkStream = new NetworkStream(socket, ownsSocket: false);
        await using var writer = new StreamWriter(networkStream) { AutoFlush = true };

        var message = JsonSerializer.Serialize(new IpcMessage(command));
        await writer.WriteLineAsync(message.AsMemory(), ct);

        logger.LogDebug("Sent IPC command: {Command}", command);
        return 0;
    }
}
