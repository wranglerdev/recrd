// Phase 08 — TDD red phase
// Tests for PluginsCommand plugin management (CLI-08)

using Recrd.Cli.Commands;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class PluginsCommandTests
{
    [Fact]
    public void PluginsList_ScansRecrdPluginsDirectory()
    {
        // Arrange / Act
        Assert.Fail("Not implemented — red phase");
        var command = PluginsCommand.Create();

        // Assert — plugins list scans ~/.recrd/plugins/ directory
        Assert.NotNull(command);
    }

    [Fact]
    public void PluginsList_WithEmptyDirectory_PrintsNoPluginsInstalledMessage()
    {
        // Arrange / Act
        Assert.Fail("Not implemented — red phase");
        var command = PluginsCommand.Create();

        // Assert — empty plugins dir prints "No plugins installed"
        Assert.NotNull(command);
    }

    [Fact]
    public void PluginsCommand_HasListSubcommand()
    {
        // Arrange / Act
        Assert.Fail("Not implemented — red phase");
        var command = PluginsCommand.Create();

        // Assert — command has a "list" subcommand
        Assert.NotNull(command);
    }

    [Fact]
    public void PluginsCommand_HasInstallSubcommandWithPackageArgument()
    {
        // Arrange / Act
        Assert.Fail("Not implemented — red phase");
        var command = PluginsCommand.Create();

        // Assert — command has an "install" subcommand with <package> string argument
        Assert.NotNull(command);
    }
}
