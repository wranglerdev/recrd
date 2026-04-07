// Phase 08 — TDD red phase
// Tests for SessionClient Unix domain socket client (D-02, D-04)

using Recrd.Cli.Ipc;
using Xunit;

namespace Recrd.Cli.Tests.Ipc;

public class SessionClientTests
{
    [Fact]
    public void Client_ConnectsAndSendsCommandSuccessfully()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var client = new SessionClient();

        // Assert — client connects to session.sock and sends JSON command
        Assert.NotNull(client);
    }

    [Fact]
    public void Client_ExitsWithCode1WhenSocketNotFound()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var client = new SessionClient();

        // Assert — exits 1 if session.sock not found (no active session)
        Assert.NotNull(client);
    }

    [Fact]
    public void Client_SendsCorrectJsonCommandFormat()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var client = new SessionClient();

        // Assert — sends {"command":"<name>"} format matching D-04 spec
        Assert.NotNull(client);
    }
}
