// Phase 08 — TDD red phase
// Tests for SessionSocket Unix domain socket server (D-02, D-04)

using Recrd.Cli.Ipc;
using Xunit;

namespace Recrd.Cli.Tests.Ipc;

public class SessionSocketTests
{
    [Fact]
    public void Server_AcceptsConnectionAndReadsJsonCommand()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var server = new SessionSocket();

        // Assert — server accepts connection and reads a JSON command message
        Assert.NotNull(server);
    }

    [Fact]
    public void Server_CleansUpSocketFileOnStop()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var server = new SessionSocket();

        // Assert — socket file is deleted when server stops
        Assert.NotNull(server);
    }

    [Fact]
    public void Server_RejectsNewConnectionWhenSessionAlreadyRunning()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var server = new SessionSocket();

        // Assert — if session.sock already exists on startup, server signals error
        Assert.NotNull(server);
    }
}
