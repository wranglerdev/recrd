// Phase 08 — TDD red phase
// Tests for LoggingSetup.Create() verbosity-to-LogLevel mapping (D-06, D-07, CLI-09, CLI-10)

using Microsoft.Extensions.Logging;
using Recrd.Cli.Logging;
using Xunit;

namespace Recrd.Cli.Tests.Logging;

public class LoggingSetupTests
{
    [Fact]
    public void Create_QuietVerbosity_MapsToLogLevelError()
    {
        // Arrange / Act
        using var factory = LoggingSetup.Create("quiet", jsonOutput: false);
        var logger = factory.CreateLogger("Test");

        // Assert
        Assert.NotNull(factory);
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.False(logger.IsEnabled(LogLevel.Warning));
    }

    [Fact]
    public void Create_NormalVerbosity_MapsToLogLevelInformation()
    {
        // Arrange / Act
        using var factory = LoggingSetup.Create("normal", jsonOutput: false);
        var logger = factory.CreateLogger("Test");

        // Assert
        Assert.NotNull(factory);
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
    }

    [Fact]
    public void Create_DetailedVerbosity_MapsToLogLevelDebug()
    {
        // Arrange / Act
        using var factory = LoggingSetup.Create("detailed", jsonOutput: false);
        var logger = factory.CreateLogger("Test");

        // Assert
        Assert.NotNull(factory);
        Assert.True(logger.IsEnabled(LogLevel.Debug));
        Assert.False(logger.IsEnabled(LogLevel.Trace));
    }

    [Fact]
    public void Create_DiagnosticVerbosity_MapsToLogLevelTrace()
    {
        // Arrange / Act
        using var factory = LoggingSetup.Create("diagnostic", jsonOutput: false);
        var logger = factory.CreateLogger("Test");

        // Assert
        Assert.NotNull(factory);
        Assert.True(logger.IsEnabled(LogLevel.Trace));
    }

    [Fact]
    public void Create_JsonOutputTrue_CreatesFactory()
    {
        // Arrange / Act
        using var factory = LoggingSetup.Create("normal", jsonOutput: true);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Create_UnknownVerbosity_DefaultsToLogLevelInformation()
    {
        // Arrange / Act
        using var factory = LoggingSetup.Create("unknown-value", jsonOutput: false);
        var logger = factory.CreateLogger("Test");

        // Assert
        Assert.NotNull(factory);
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
    }
}
