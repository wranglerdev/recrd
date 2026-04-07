// Phase 08 — TDD red phase
// Tests for CompileCommand CLI argument and option parsing (CLI-03)

using Recrd.Cli.Commands;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class CompileCommandTests
{
    [Fact]
    public void Compile_WithTargetRobotBrowser_SelectsRobotBrowserCompiler()
    {
        // Arrange / Act
        Assert.Fail("Not implemented — red phase");
        var command = CompileCommand.Create();

        // Assert — --target robot-browser selects RobotBrowserCompiler
        Assert.NotNull(command);
    }

    [Fact]
    public void Compile_WithDataCsv_CreatesCsvDataProvider()
    {
        // Arrange / Act
        Assert.Fail("Not implemented — red phase");
        var command = CompileCommand.Create();

        // Assert — --data test.csv creates CsvDataProvider
        Assert.NotNull(command);
    }

    [Fact]
    public void Compile_WithOutDirectory_SetsCompilerOptionsOutputDirectory()
    {
        // Arrange / Act
        Assert.Fail("Not implemented — red phase");
        var command = CompileCommand.Create();

        // Assert — --out /tmp sets CompilerOptions.OutputDirectory="/tmp"
        Assert.NotNull(command);
    }

    [Fact]
    public void Compile_HasAllExpectedOptions()
    {
        // Arrange / Act
        Assert.Fail("Not implemented — red phase");
        var command = CompileCommand.Create();

        // Assert — command exposes --target, --data, --csv-delimiter, --out, --selector-strategy, --timeout, --intercept
        Assert.NotNull(command);
    }

    [Fact]
    public void Compile_DefaultTarget_IsRobotBrowser()
    {
        // Arrange / Act
        Assert.Fail("Not implemented — red phase");
        var command = CompileCommand.Create();

        // Assert — default --target value is "robot-browser"
        Assert.NotNull(command);
    }
}
