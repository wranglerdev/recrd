// Phase 08 — TDD green phase
// Tests for VersionCommand assembly version output (CLI-07)

using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Recrd.Cli.Commands;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class VersionCommandTests
{
    [Fact]
    public async Task Version_ExitsWithCode0()
    {
        // Arrange
        var command = VersionCommand.Create();

        // Act
        int exitCode = await command.Parse("").InvokeAsync();

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Version_HasCorrectCommandName()
    {
        // Arrange
        var command = VersionCommand.Create();

        // Assert
        Assert.Equal("version", command.Name);
    }
}
