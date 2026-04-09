using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Recrd.Cli;
using Xunit;

namespace Recrd.Cli.Tests;

public class ProgramTests
{
    [Fact]
    public async Task Program_Main_WithHelp_ExitsWithCode0()
    {
        // Act
        int exitCode = await Program.Main(["--help"]);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Program_Main_WithVersion_ExitsWithCode0()
    {
        // Act
        int exitCode = await Program.Main(["version"]);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void CreateRootCommand_ContainsAllSubcommands()
    {
        // Act
        var root = Program.CreateRootCommand();

        // Assert
        Assert.Contains(root.Subcommands, s => s.Name == "start");
        Assert.Contains(root.Subcommands, s => s.Name == "pause");
        Assert.Contains(root.Subcommands, s => s.Name == "resume");
        Assert.Contains(root.Subcommands, s => s.Name == "stop");
        Assert.Contains(root.Subcommands, s => s.Name == "compile");
        Assert.Contains(root.Subcommands, s => s.Name == "validate");
        Assert.Contains(root.Subcommands, s => s.Name == "sanitize");
        Assert.Contains(root.Subcommands, s => s.Name == "recover");
        Assert.Contains(root.Subcommands, s => s.Name == "version");
        Assert.Contains(root.Subcommands, s => s.Name == "plugins");
    }

    [Fact]
    public void CreateRootCommand_HasGlobalOptions()
    {
        // Act
        var root = Program.CreateRootCommand();

        // Assert
        Assert.Contains(root.Options, o => o.Name == "--verbosity");
        Assert.Contains(root.Options, o => o.Name == "--log-output");
    }
}
