// Phase 08 — TDD green phase
// Tests for SessionControlCommand (pause/resume/stop) IPC socket behavior (CLI-02, D-02, D-04)

using System.CommandLine;
using System.Net.Sockets;
using System.Text.Json;
using Recrd.Cli.Commands;
using Recrd.Cli.Ipc;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class SessionControlCommandTests
{
    [Fact]
    public async Task Pause_SendsPauseCommandJsonOverSocket()
    {
        // Arrange
        var socketPath = Path.Combine(Path.GetTempPath(), $"pause-{Guid.NewGuid()}.sock");
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
            var command = SessionControlCommand.CreatePause(socketPath: socketPath);

            // Act
            int exitCode = await command.Parse([]).InvokeAsync();

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
    public async Task Resume_SendsResumeCommandJsonOverSocket()
    {
        // Arrange
        var socketPath = Path.Combine(Path.GetTempPath(), $"resume-{Guid.NewGuid()}.sock");
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
            var command = SessionControlCommand.CreateResume(socketPath: socketPath);

            // Act
            int exitCode = await command.Parse([]).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
            var received = await receiveTask;
            var msg = JsonSerializer.Deserialize<IpcMessage>(received!);
            Assert.Equal("resume", msg!.Command);
        }
        finally
        {
            if (File.Exists(socketPath)) File.Delete(socketPath);
        }
    }

    [Fact]
    public async Task Stop_SendsStopCommandJsonOverSocket()
    {
        // Arrange
        var socketPath = Path.Combine(Path.GetTempPath(), $"stop-{Guid.NewGuid()}.sock");
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
            var command = SessionControlCommand.CreateStop(socketPath: socketPath);

            // Act
            int exitCode = await command.Parse([]).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
            var received = await receiveTask;
            var msg = JsonSerializer.Deserialize<IpcMessage>(received!);
            Assert.Equal("stop", msg!.Command);
        }
        finally
        {
            if (File.Exists(socketPath)) File.Delete(socketPath);
        }
    }

    [Fact]
    public void Pause_HasCorrectCommandName()
    {
        // Arrange
        var command = SessionControlCommand.CreatePause();

        // Assert
        Assert.Equal("pause", command.Name);
    }
}
