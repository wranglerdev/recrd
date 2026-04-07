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
        Assert.True(false, "Not implemented — red phase");
        var factory = LoggingSetup.Create("quiet", jsonOutput: false);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Create_NormalVerbosity_MapsToLogLevelInformation()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var factory = LoggingSetup.Create("normal", jsonOutput: false);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Create_DetailedVerbosity_MapsToLogLevelDebug()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var factory = LoggingSetup.Create("detailed", jsonOutput: false);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Create_DiagnosticVerbosity_MapsToLogLevelTrace()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var factory = LoggingSetup.Create("diagnostic", jsonOutput: false);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Create_JsonOutputTrue_CreatesFactoryWithJsonFormatter()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var factory = LoggingSetup.Create("normal", jsonOutput: true);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Create_UnknownVerbosity_DefaultsToLogLevelInformation()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var factory = LoggingSetup.Create("unknown-value", jsonOutput: false);

        // Assert
        Assert.NotNull(factory);
    }
}
