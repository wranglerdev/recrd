// Phase 08 — TDD green phase
// Tests for PluginsCommand plugin management (CLI-08)

using System.CommandLine;
using Recrd.Cli.Commands;
using Recrd.Cli.Plugins;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class PluginsCommandTests
{
    private readonly PluginManager _pluginManager;
    private readonly string _tempDir;

    public PluginsCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _pluginManager = new PluginManager(_tempDir);
    }

    [Fact]
    public void PluginsCommand_HasListSubcommand()
    {
        // Arrange
        var command = PluginsCommand.Create(_pluginManager);

        // Assert
        Assert.Contains(command.Subcommands, s => s.Name == "list");
    }

    [Fact]
    public void PluginsCommand_HasInstallSubcommandWithPackageArgument()
    {
        // Arrange
        var command = PluginsCommand.Create(_pluginManager);
        var install = command.Subcommands.First(s => s.Name == "install");

        // Assert
        Assert.Contains(install.Arguments, a => a.Name == "package");
    }

    [Fact]
    public async Task PluginsList_WithAssemblies_PrintsPluginNames()
    {
        // Arrange
        var pluginSubdir = Path.Combine(_tempDir, "TestPlugin");
        Directory.CreateDirectory(pluginSubdir);
        // We need a real-ish DLL for MetadataReader not to fail, but PluginManager handles exceptions
        await File.WriteAllTextAsync(Path.Combine(pluginSubdir, "TestPlugin.dll"), "not-a-dll");
        
        var command = PluginsCommand.Create(_pluginManager);
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
            Assert.Contains("load error", output.ToString()); // Since it's not a real DLL
        }
        finally
        {
            Console.SetOut(originalOut);
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public async Task PluginsList_Empty_PrintsNoPlugins()
    {
        // Arrange
        var command = PluginsCommand.Create(_pluginManager);
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
            Assert.Contains("No plugins installed", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task PluginsInstall_ExitsWithCode0()
    {
        // Arrange
        var command = PluginsCommand.Create(_pluginManager);
        var install = command.Subcommands.First(s => s.Name == "install");

        // Act
        int exitCode = await install.Parse(["MyPkg"]).InvokeAsync();

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task PluginsInstall_PrintsInstallationGuide()
    {
        // Arrange
        var command = PluginsCommand.Create(_pluginManager);
        var install = command.Subcommands.First(s => s.Name == "install");
        var output = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(output);

        try
        {
            // Act
            await install.Parse(["MyPkg"]).InvokeAsync();

            // Assert
            var result = output.ToString();
            Assert.Contains("To install MyPkg:", result);
            Assert.Contains("dotnet publish", result);
            Assert.Contains("Copy the publish output", result);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
