// Phase 08 — TDD green phase
// Tests for SessionSocket Unix domain socket server (D-02, D-04)

using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Recrd.Cli.Ipc;
using Xunit;

namespace Recrd.Cli.Tests.Ipc;

public class SessionSocketTests
{
    [Fact]
    public async Task Server_AcceptsConnectionAndReadsJsonCommand()
    {
        // Arrange
        var socketPath = Path.Combine(Path.GetTempPath(), $"socket-{Guid.NewGuid()}.sock");
        await using var server = new SessionSocket(socketPath, NullLogger.Instance);
        var cts = new CancellationTokenSource();
        var receivedCommands = new List<string>();

        var listenTask = server.StartListeningAsync(cmd =>
        {
            receivedCommands.Add(cmd);
            return Task.CompletedTask;
        }, cts.Token);

        try
        {
            await Task.Delay(100);
            
            using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            await client.ConnectAsync(new UnixDomainSocketEndPoint(socketPath));
            await using var stream = new NetworkStream(client);
            await using var writer = new StreamWriter(stream) { AutoFlush = true };
            await writer.WriteLineAsync(JsonSerializer.Serialize(new IpcMessage("pause")));

            // Act
            await Task.Delay(100);

            // Assert
            Assert.Contains("pause", receivedCommands);
        }
        finally
        {
            cts.Cancel();
            await listenTask;
        }
    }

    [Fact]
    public async Task Server_CleansUpSocketFileOnStop()
    {
        // Arrange
        var socketPath = Path.Combine(Path.GetTempPath(), $"cleanup-{Guid.NewGuid()}.sock");
        await using var server = new SessionSocket(socketPath, NullLogger.Instance);
        var cts = new CancellationTokenSource();

        var listenTask = server.StartListeningAsync(_ => Task.CompletedTask, cts.Token);

        try
        {
            await Task.Delay(100);
            Assert.True(File.Exists(socketPath));
            
            using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            await client.ConnectAsync(new UnixDomainSocketEndPoint(socketPath));
            await using var stream = new NetworkStream(client);
            await using var writer = new StreamWriter(stream) { AutoFlush = true };
            await writer.WriteLineAsync(JsonSerializer.Serialize(new IpcMessage("stop")));

            // Act
            await listenTask;

            // Assert
            Assert.False(File.Exists(socketPath));
        }
        finally
        {
            cts.Cancel();
        }
    }

    [Fact]
    public async Task IsSessionActiveAsync_DeletesStaleSocketFile()
    {
        // Arrange
        var socketPath = Path.Combine(Path.GetTempPath(), $"stale-{Guid.NewGuid()}.sock");
        await File.WriteAllTextAsync(socketPath, "stale");

        // Act
        bool isActive = await SessionSocket.IsSessionActiveAsync(socketPath);

        // Assert
        Assert.False(isActive);
        Assert.False(File.Exists(socketPath));
    }
}
