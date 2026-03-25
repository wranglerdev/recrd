using Recrd.Core.Ast;

namespace Recrd.Core.Tests.Ast;

/// <summary>
/// User Story: As a developer, I want the Session AST to correctly represent
/// a recorded browser session so that downstream components can reliably
/// read its structure.
/// </summary>
public sealed class SessionTests
{
    [Fact]
    public void Session_WithSchemaVersion1_HasExpectedSchemaVersion()
    {
        var metadata = new SessionMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "chromium", "1280x720", null);
        var session = new Session(1, metadata, [], []);

        Assert.Equal(1, session.SchemaVersion);
    }

    [Fact]
    public void Session_WithNoVariables_HasEmptyVariablesList()
    {
        var metadata = new SessionMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "chromium", "1280x720", null);
        var session = new Session(1, metadata, [], []);

        Assert.Empty(session.Variables);
    }

    [Fact]
    public void Session_WithVariables_ExposesAllDeclaredVariables()
    {
        var metadata = new SessionMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "chromium", "1280x720", null);
        var variables = new List<Variable> { new("login"), new("senha") };
        var session = new Session(1, metadata, variables, []);

        Assert.Equal(2, session.Variables.Count);
        Assert.Contains(session.Variables, v => v.Name == "login");
        Assert.Contains(session.Variables, v => v.Name == "senha");
    }

    [Fact]
    public void Session_Metadata_ContainsBrowserEngine()
    {
        var metadata = new SessionMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "firefox", "1920x1080", "https://example.com");
        var session = new Session(1, metadata, [], []);

        Assert.Equal("firefox", session.Metadata.BrowserEngine);
        Assert.Equal("1920x1080", session.Metadata.ViewportSize);
        Assert.Equal("https://example.com", session.Metadata.BaseUrl);
    }
}
