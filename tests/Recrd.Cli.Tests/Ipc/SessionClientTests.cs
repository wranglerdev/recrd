// Phase 08 — TDD green phase
// Tests for SessionClient Unix domain socket client (D-02, D-04)

using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Recrd.Cli.Ipc;
using Xunit;

namespace Recrd.Cli.Tests.Ipc;

public class SessionClientTests
{
    [Fact]
    public async Task Client_ConnectsAndSendsCommandSuccessfully()
    {
        // Arrange
        var socketPath = Path.Combine(Path.GetTempPath(), $"client-{Guid.NewGuid()}.sock");
        using var serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        serverSocket.Bind(new UnixDomainSocketEndPoint(socketPath));
        serverSocket.Listen(1);

        var receiveTask = Task.Run(async () =>
        {
            using var client = await serverSocket.AcceptAsync();
            await using var stream = new NetworkStream(client);
            using var reader = new StreamReader(stream);
            return await reader.ReadLineAsync();
        });

        try
        {
            // Act
            int exitCode = await SessionClient.SendCommandAsync("pause", NullLogger.Instance, CancellationToken.None, socketPath);

            // Assert
            Assert.Equal(0, exitCode);
            var received = await receiveTask;
            var msg = JsonSerializer.Deserialize<IpcMessage>(received!);
            Assert.Equal("pause", msg!.Command);
        }
        finally
        {
            if (File.Exists(socketPath)) File.Delete(socketPath);
        }
    }

    [Fact]
    public async Task Client_ExitsWithCode1WhenSocketNotFound()
    {
        // Arrange
        var socketPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.sock");

        // Act
        int exitCode = await SessionClient.SendCommandAsync("pause", NullLogger.Instance, CancellationToken.None, socketPath);

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Client_SendsCorrectJsonCommandFormat()
    {
        // Arrange
        var socketPath = Path.Combine(Path.GetTempPath(), $"format-{Guid.NewGuid()}.sock");
        using var serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        serverSocket.Bind(new UnixDomainSocketEndPoint(socketPath));
        serverSocket.Listen(1);

        var receiveTask = Task.Run(async () =>
        {
            using var client = await serverSocket.AcceptAsync();
            await using var stream = new NetworkStream(client);
            using var reader = new StreamReader(stream);
            return await reader.ReadLineAsync();
        });

        try
        {
            // Act
            await SessionClient.SendCommandAsync("stop", NullLogger.Instance, CancellationToken.None, socketPath);

            // Assert
            var received = await receiveTask;
            Assert.Equal("{\"Command\":\"stop\"}", received);
        }
        finally
        {
            if (File.Exists(socketPath)) File.Delete(socketPath);
        }
    }
}
