// Phase 08 — TDD green phase
// Tests for CompileCommand CLI argument and option parsing (CLI-03)

using System.CommandLine;
using System.Text.Json;
using Recrd.Cli.Commands;
using Recrd.Core.Ast;
using Recrd.Core.Serialization;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class CompileCommandTests
{
    [Fact]
    public void Compile_HasCorrectCommandName()
    {
        // Arrange
        var command = CompileCommand.Create();

        // Assert
        Assert.Equal("compile", command.Name);
    }

    [Fact]
    public void Compile_HasAllExpectedOptions()
    {
        // Arrange
        var command = CompileCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "--target");
        Assert.Contains(command.Options, o => o.Name == "--data");
        Assert.Contains(command.Options, o => o.Name == "--csv-delimiter");
        Assert.Contains(command.Options, o => o.Name == "--out");
        Assert.Contains(command.Options, o => o.Name == "--selector-strategy");
        Assert.Contains(command.Options, o => o.Name == "--timeout");
        Assert.Contains(command.Options, o => o.Name == "--intercept");
    }

    [Fact]
    public void Compile_DefaultTarget_IsRobotBrowser()
    {
        // Arrange
        var command = CompileCommand.Create();

        // Assert
        var targetOption = command.Options.First(o => o.Name == "--target");
        // Accessing default value in System.CommandLine 2.0.5
        Assert.Equal("robot-browser", targetOption.GetDefaultValue());
    }

    [Fact]
    public async Task Compile_WhenSessionNotFound_ExitsWithCode1()
    {
        // Arrange
        var sessionPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.recrd");
        var command = CompileCommand.Create();

        // Act
        int exitCode = await command.Parse([sessionPath]).InvokeAsync();

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Compile_WithInvalidExtension_ExitsWithCode1()
    {
        // Arrange
        var sessionPath = Path.Combine(Path.GetTempPath(), $"invalid-{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(sessionPath, "{}");
        var command = CompileCommand.Create();

        try
        {
            // Act
            int exitCode = await command.Parse([sessionPath]).InvokeAsync();

            // Assert
            Assert.Equal(1, exitCode);
        }
        finally
        {
            if (File.Exists(sessionPath)) File.Delete(sessionPath);
        }
    }

    [Fact]
    public async Task Compile_WithInvalidJson_ExitsWithCode1()
    {
        // Arrange
        var sessionPath = Path.Combine(Path.GetTempPath(), $"invalid-{Guid.NewGuid()}.recrd");
        await File.WriteAllTextAsync(sessionPath, "{ broken }");
        var command = CompileCommand.Create();

        try
        {
            // Act
            int exitCode = await command.Parse([sessionPath]).InvokeAsync();

            // Assert
            Assert.Equal(1, exitCode);
        }
        finally
        {
            if (File.Exists(sessionPath)) File.Delete(sessionPath);
        }
    }

    [Fact]
    public async Task Compile_Success_RobotBrowser()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var sessionPath = Path.Combine(tempDir, "test.recrd");
        var session = new Session(1, new SessionMetadata("id", DateTimeOffset.UtcNow, "chromium", new ViewportSize(1280, 720)), [], []);
        await File.WriteAllTextAsync(sessionPath, JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session));
        var outDir = Path.Combine(tempDir, "out");

        var command = CompileCommand.Create();

        try
        {
            // Act
            int exitCode = await command.Parse([sessionPath, "--target", "robot-browser", "--out", outDir]).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
            Assert.True(Directory.Exists(outDir));
            Assert.True(File.Exists(Path.Combine(outDir, "test.feature")));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Compile_Success_RobotSelenium()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var sessionPath = Path.Combine(tempDir, "test.recrd");
        var session = new Session(1, new SessionMetadata("id", DateTimeOffset.UtcNow, "chromium", new ViewportSize(1280, 720)), [], []);
        await File.WriteAllTextAsync(sessionPath, JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session));
        var outDir = Path.Combine(tempDir, "out-sel");

        var command = CompileCommand.Create();

        try
        {
            // Act
            int exitCode = await command.Parse([sessionPath, "--target", "robot-selenium", "--out", outDir]).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
            Assert.True(Directory.Exists(outDir));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Compile_WithJsonData_Success()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var sessionPath = Path.Combine(tempDir, "test.recrd");
        var session = new Session(1, new SessionMetadata("id", DateTimeOffset.UtcNow, "chromium", new ViewportSize(1280, 720)), [], []);
        await File.WriteAllTextAsync(sessionPath, JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session));

        var dataPath = Path.Combine(tempDir, "data.json");
        await File.WriteAllTextAsync(dataPath, "[{\"user\":\"test\"}]");

        var outDir = Path.Combine(tempDir, "out");
        var command = CompileCommand.Create();

        try
        {
            // Act
            int exitCode = await command.Parse([sessionPath, "--data", dataPath, "--out", outDir]).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Compile_WithCsvData_ExitsWithCode1IfFileNotFound()
    {
        // Arrange
        var sessionPath = Path.Combine(Path.GetTempPath(), $"valid-{Guid.NewGuid()}.recrd");
        var session = new Session(1, new SessionMetadata("id", DateTimeOffset.UtcNow, "chromium", new ViewportSize(1,1)), [], []);
        await File.WriteAllTextAsync(sessionPath, JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session));
        var dataPath = Path.Combine(Path.GetTempPath(), "missing.csv");
        var command = CompileCommand.Create();

        try
        {
            // Act
            int exitCode = await command.Parse([sessionPath, "--data", dataPath]).InvokeAsync();

            // Assert
            Assert.Equal(1, exitCode);
        }
        finally
        {
            if (File.Exists(sessionPath)) File.Delete(sessionPath);
        }
    }

    [Fact]
    public async Task Compile_WithUnsupportedDataFormat_ExitsWithCode1()
    {
        // Arrange
        var sessionPath = Path.Combine(Path.GetTempPath(), $"valid-{Guid.NewGuid()}.recrd");
        var session = new Session(1, new SessionMetadata("id", DateTimeOffset.UtcNow, "chromium", new ViewportSize(1,1)), [], []);
        await File.WriteAllTextAsync(sessionPath, JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session));
        var dataPath = Path.Combine(Path.GetTempPath(), "test.txt");
        await File.WriteAllTextAsync(dataPath, "data");
        var command = CompileCommand.Create();

        try
        {
            // Act
            int exitCode = await command.Parse([sessionPath, "--data", dataPath]).InvokeAsync();

            // Assert
            Assert.Equal(1, exitCode);
        }
        finally
        {
            if (File.Exists(sessionPath)) File.Delete(sessionPath);
            if (File.Exists(dataPath)) File.Delete(dataPath);
        }
    }

    [Fact]
    public async Task Compile_WithCustomCsvDelimiter_Success()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var sessionPath = Path.Combine(tempDir, "test.recrd");
        var session = new Session(1, new SessionMetadata("id", DateTimeOffset.UtcNow, "chromium", new ViewportSize(1280, 720)), [], []);
        await File.WriteAllTextAsync(sessionPath, JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session));
        
        var dataPath = Path.Combine(tempDir, "data.csv");
        await File.WriteAllTextAsync(dataPath, "user;pass\nbob;123");
        
        var outDir = Path.Combine(tempDir, "out");
        var command = CompileCommand.Create();

        try
        {
            // Act
            int exitCode = await command.Parse([sessionPath, "--data", dataPath, "--csv-delimiter", ";", "--out", outDir]).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
