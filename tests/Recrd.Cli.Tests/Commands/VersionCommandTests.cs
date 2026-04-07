// Phase 08 — TDD red phase
// Tests for VersionCommand assembly version output (CLI-07)

using Recrd.Cli.Commands;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class VersionCommandTests
{
    [Fact]
    public void Version_PrintsAssemblyVersionString_AndExitsWithCode0()
    {
        // Arrange / Act
        Assert.Fail("Not implemented — red phase");
        var command = VersionCommand.Create();

        // Assert — prints assembly version string and exits 0
        Assert.NotNull(command);
    }

    [Fact]
    public void Version_HasCorrectCommandName()
    {
        // Arrange / Act
        Assert.Fail("Not implemented — red phase");
        var command = VersionCommand.Create();

        // Assert — command.Name == "version"
        Assert.NotNull(command);
    }
}
