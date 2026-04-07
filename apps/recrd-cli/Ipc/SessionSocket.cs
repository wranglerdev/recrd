// SessionSocket — Unix domain socket server for session lifecycle control (D-02, D-04)

using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Recrd.Cli.Ipc;

/// <summary>
/// Unix domain socket server that accepts control commands (pause/resume/stop)
/// while a recording session is active. Socket file lives at ~/.recrd/session.sock.
/// </summary>
internal sealed class SessionSocket : IAsyncDisposable
{
    private readonly string _socketPath;
    private readonly ILogger _logger;
    private Socket? _serverSocket;

    /// <summary>
    /// Default socket path: ~/.recrd/session.sock
    /// </summary>
    public static string DefaultSocketPath { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".recrd",
            "session.sock");

    public SessionSocket(string socketPath, ILogger logger)
    {
        _socketPath = socketPath;
        _logger = logger;
    }

    /// <summary>
    /// Checks if there is an active session by probing the socket.
    /// If the socket file exists but no listener responds, the stale file is deleted (Pitfall 3).
    /// </summary>
    public static async Task<bool> IsSessionActiveAsync(string socketPath)
    {
        if (!File.Exists(socketPath))
            return false;

        using var probeSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            await probeSocket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath));
            return true;
        }
        catch (SocketException)
        {
            // Stale socket — no listener. Delete the dead file.
            try { File.Delete(socketPath); } catch { /* best effort */ }
            return false;
        }
    }

    /// <summary>
    /// Starts listening on the Unix domain socket and dispatches incoming commands.
    /// Deletes the socket file when the loop exits (either via stop command or cancellation).
    /// </summary>
    /// <param name="commandHandler">Async handler called for each incoming command string.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task StartListeningAsync(Func<string, Task> commandHandler, CancellationToken ct)
    {
        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(_socketPath)!);

        _serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _serverSocket.Bind(new UnixDomainSocketEndPoint(_socketPath));
        _serverSocket.Listen(1);

        _logger.LogDebug("Session socket listening at {SocketPath}", _socketPath);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                Socket clientSocket;
                try
                {
                    clientSocket = await _serverSocket.AcceptAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                await using var networkStream = new NetworkStream(clientSocket, ownsSocket: true);
                using var reader = new StreamReader(networkStream);

                var line = await reader.ReadLineAsync(ct);
                if (line is null)
                    continue;

                IpcMessage? msg;
                try
                {
                    msg = JsonSerializer.Deserialize<IpcMessage>(line);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Received invalid IPC message: {Line}", line);
                    continue;
                }

                if (msg is null)
                    continue;

                // Validate command against known values (T-8-04 mitigation)
                var validCommands = new[] { "pause", "resume", "stop" };
                if (!Array.Exists(validCommands, c => c == msg.Command))
                {
                    _logger.LogWarning("Received unknown IPC command: {Command} — ignoring", msg.Command);
                    continue;
                }

                await commandHandler(msg.Command);

                if (msg.Command == "stop")
                    break;
            }
        }
        finally
        {
            File.Delete(_socketPath);
            _logger.LogDebug("Session socket removed at {SocketPath}", _socketPath);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_serverSocket is not null)
        {
            _serverSocket.Dispose();
            _serverSocket = null;
        }

        if (File.Exists(_socketPath))
        {
            try { File.Delete(_socketPath); } catch { /* best effort */ }
        }

        await ValueTask.CompletedTask;
    }
}
