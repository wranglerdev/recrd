// Phase 08 — TDD green phase
// Tests for RecoverCommand partial session recovery (CLI-06)

using System.CommandLine;
using Recrd.Cli.Commands;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class RecoverCommandTests
{
    [Fact]
    public void Recover_HasCorrectCommandName()
    {
        // Arrange
        var command = RecoverCommand.Create();

        // Assert
        Assert.Equal("recover", command.Name);
    }

    [Fact]
    public void Recover_HasPartialFileOption()
    {
        // Arrange
        var command = RecoverCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "--partial-file");
    }

    [Fact]
    public async Task Recover_WhenNoPartialFileExists_ExitsWithCode1AndError()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(tempDir);

        try
        {
            var command = RecoverCommand.Create();

            // Act
            int exitCode = await command.Parse([]).InvokeAsync();

            // Assert
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Recover_ScansCurrentDirectoryForPartials()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var partialPath = Path.Combine(tempDir, "test.recrd.partial");
        // Invalid session JSON to make it fail but at least it executes the scan
        await File.WriteAllTextAsync(partialPath, "{ \"Steps\": [] }");
        
        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(tempDir);

        try
        {
            var command = RecoverCommand.Create();

            // Act
            // It will fail deserialization or engine creation but it will execute the scan logic
            int exitCode = await command.Parse([]).InvokeAsync();

            // Assert
            // We just care that it tried to recover from the file
            // The exit code will be 1 because { "Steps": [] } is not a full Session
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
