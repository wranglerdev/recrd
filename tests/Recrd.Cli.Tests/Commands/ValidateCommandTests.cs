// Phase 08 — TDD green phase
// Tests for ValidateCommand session file validation (CLI-04)

using System.CommandLine;
using System.Text.Json;
using Recrd.Cli.Commands;
using Recrd.Core.Ast;
using Recrd.Core.Serialization;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class ValidateCommandTests
{
    [Fact]
    public async Task Validate_ValidSession_ExitsWithCode0()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"valid-{Guid.NewGuid()}.recrd");
        var session = new Session(
            1,
            new SessionMetadata("test-id", DateTimeOffset.UtcNow, "chromium", new ViewportSize(1280, 720)),
            [],
            [new ActionStep(ActionType.Click, new Selector([SelectorStrategy.Css], new Dictionary<SelectorStrategy, string> { [SelectorStrategy.Css] = ".btn" }), new Dictionary<string, string>())]
        );
        var json = JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session);
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            var command = ValidateCommand.Create();

            // Act
            int exitCode = await command.Parse([tempFile]).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Validate_InvalidJson_ExitsWithCode1()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"invalid-{Guid.NewGuid()}.recrd");
        await File.WriteAllTextAsync(tempFile, "{ broken }");
        var command = ValidateCommand.Create();

        try
        {
            // Act
            int exitCode = await command.Parse([tempFile]).InvokeAsync();

            // Assert
            Assert.Equal(1, exitCode);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Validate_EmptySession_ExitsWithCode1()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"empty-{Guid.NewGuid()}.recrd");
        await File.WriteAllTextAsync(tempFile, "null");
        var command = ValidateCommand.Create();

        try
        {
            // Act
            int exitCode = await command.Parse([tempFile]).InvokeAsync();

            // Assert
            Assert.Equal(1, exitCode);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
