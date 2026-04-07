// Phase 08 — TDD red phase
// Tests for StartCommand CLI argument parsing behavior (CLI-01, D-03)

using Recrd.Cli.Commands;
using Recrd.Core.Interfaces;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class StartCommandTests
{
    [Fact]
    public void Start_WithDefaultOptions_ProducesRecorderOptionsWithChromiumAndHeadedAndDefaultViewport()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = StartCommand.Create();

        // Assert — default options: BrowserEngine="chromium", Headed=true, ViewportSize=1280x720
        Assert.NotNull(command);
    }

    [Fact]
    public void Start_WithBrowserFirefox_ProducesRecorderOptionsWithFirefoxBrowserEngine()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = StartCommand.Create();

        // Assert — --browser firefox results in BrowserEngine="firefox"
        Assert.NotNull(command);
    }

    [Fact]
    public void Start_WhenSessionSockExists_ExitsWithCode1AndErrorMessageContainingAlreadyRunning()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = StartCommand.Create();

        // Assert — exits 1 with error "already running" when session.sock exists
        Assert.NotNull(command);
    }

    [Fact]
    public void Start_WithBaseUrl_SetsBaseUrlInRecorderOptions()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = StartCommand.Create();

        // Assert — --base-url https://example.com propagates to RecorderOptions.BaseUrl
        Assert.NotNull(command);
    }

    [Fact]
    public void Start_HasAllExpectedOptions()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = StartCommand.Create();

        // Assert — command exposes --browser, --headed, --viewport, --base-url
        Assert.NotNull(command);
    }
}
