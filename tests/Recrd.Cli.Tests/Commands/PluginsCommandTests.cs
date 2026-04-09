// Phase 08 — TDD green phase
// Tests for PluginsCommand plugin management (CLI-08)

using System.CommandLine;
using Recrd.Cli.Commands;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class PluginsCommandTests
{
    [Fact]
    public void PluginsCommand_HasListSubcommand()
    {
        // Arrange
        var command = PluginsCommand.Create();

        // Assert
        Assert.Contains(command.Subcommands, s => s.Name == "list");
    }

    [Fact]
    public void PluginsCommand_HasInstallSubcommandWithPackageArgument()
    {
        // Arrange
        var command = PluginsCommand.Create();
        var install = command.Subcommands.First(s => s.Name == "install");

        // Assert
        Assert.Contains(install.Arguments, a => a.Name == "package");
    }

    [Fact]
    public async Task PluginsList_WithAssemblies_PrintsPluginNames()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "TestPlugin.dll"), "");
        
        var command = PluginsCommand.Create(tempDir);
        var list = command.Subcommands.First(s => s.Name == "list");
        var output = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(output);

        try
        {
            // Act
            int exitCode = await list.Parse([]).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Contains("TestPlugin", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task PluginsInstall_ExitsWithCode1()
    {
        // Arrange
        var command = PluginsCommand.Create();
        var install = command.Subcommands.First(s => s.Name == "install");

        // Act
        int exitCode = await install.Parse(["MyPkg"]).InvokeAsync();

        // Assert
        Assert.Equal(1, exitCode);
    }
}
