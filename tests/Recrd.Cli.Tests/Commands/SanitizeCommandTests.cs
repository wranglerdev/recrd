// Phase 08 — TDD green phase
// Tests for SanitizeCommand session sanitization (CLI-05, D-08)

using System.CommandLine;
using System.Text.Json;
using Recrd.Cli.Commands;
using Recrd.Core.Ast;
using Recrd.Core.Serialization;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class SanitizeCommandTests
{
    private readonly Session _testSession = new(
        1,
        new SessionMetadata("id", DateTimeOffset.UtcNow, "chromium", new ViewportSize(1280, 720)),
        [],
        [
            new ActionStep(
                ActionType.Click,
                new Selector([SelectorStrategy.Css], new Dictionary<SelectorStrategy, string> { [SelectorStrategy.Css] = ".btn" }),
                new Dictionary<string, string> { ["text"] = "Submit" }
            )
        ]
    );

    [Fact]
    public async Task Sanitize_ProducesOutputAtBasenameWithSanitizedExtension()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var sessionPath = Path.Combine(tempDir, "test.recrd");
        var expectedOut = Path.Combine(tempDir, "test.sanitized.recrd");
        
        await File.WriteAllTextAsync(sessionPath, JsonSerializer.Serialize(_testSession, RecrdJsonContext.Default.Session));

        try
        {
            var command = SanitizeCommand.Create();

            // Act
            int exitCode = await command.Parse([sessionPath]).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedOut));
            
            var sanitizedJson = await File.ReadAllTextAsync(expectedOut);
            var sanitized = JsonSerializer.Deserialize(sanitizedJson, RecrdJsonContext.Default.Session);
            Assert.NotNull(sanitized);
            var action = (ActionStep)sanitized.Steps[0];
            Assert.Equal("***", action.Selector.Values[SelectorStrategy.Css]);
            Assert.Empty(action.Payload);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Sanitize_WithExplicitOut_WritesOutputToSpecifiedPath()
    {
        // Arrange
        var sessionPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.recrd");
        var outPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.recrd");
        
        await File.WriteAllTextAsync(sessionPath, JsonSerializer.Serialize(_testSession, RecrdJsonContext.Default.Session));

        try
        {
            var command = SanitizeCommand.Create();

            // Act
            int exitCode = await command.Parse([sessionPath, "--out", outPath]).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outPath));
        }
        finally
        {
            if (File.Exists(sessionPath)) File.Delete(sessionPath);
            if (File.Exists(outPath)) File.Delete(outPath);
        }
    }

    [Fact]
    public async Task Sanitize_InvalidJson_ExitsWithCode1()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"invalid-{Guid.NewGuid()}.recrd");
        await File.WriteAllTextAsync(tempFile, "{ broken }");
        var command = SanitizeCommand.Create();

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
