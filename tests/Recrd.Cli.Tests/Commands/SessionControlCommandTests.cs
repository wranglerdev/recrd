// Phase 08 — TDD red phase
// Tests for SessionControlCommand (pause/resume/stop) IPC socket behavior (CLI-02, D-02, D-04)

using Recrd.Cli.Commands;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class SessionControlCommandTests
{
    [Fact]
    public void Pause_SendsPauseCommandJsonOverSocket()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = SessionControlCommand.CreatePause();

        // Assert — sends {"command":"pause"} over session.sock
        Assert.NotNull(command);
    }

    [Fact]
    public void Resume_SendsResumeCommandJsonOverSocket()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = SessionControlCommand.CreateResume();

        // Assert — sends {"command":"resume"} over session.sock
        Assert.NotNull(command);
    }

    [Fact]
    public void Stop_SendsStopCommandJsonOverSocket()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = SessionControlCommand.CreateStop();

        // Assert — sends {"command":"stop"} over session.sock
        Assert.NotNull(command);
    }

    [Fact]
    public void Pause_HasCorrectCommandName()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = SessionControlCommand.CreatePause();

        // Assert — command.Name == "pause"
        Assert.NotNull(command);
    }
}
