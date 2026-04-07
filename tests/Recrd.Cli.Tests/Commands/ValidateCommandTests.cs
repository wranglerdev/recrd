// Phase 08 — TDD red phase
// Tests for ValidateCommand session file validation (CLI-04)

using Recrd.Cli.Commands;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class ValidateCommandTests
{
    [Fact]
    public void Validate_ValidSession_ExitsWithCode0()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = ValidateCommand.Create();

        // Assert — valid session.recrd exits 0
        Assert.NotNull(command);
    }

    [Fact]
    public void Validate_InvalidJson_ExitsWithCode1AndErrorOnStderr()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = ValidateCommand.Create();

        // Assert — invalid JSON exits 1 with error on stderr
        Assert.NotNull(command);
    }

    [Fact]
    public void Validate_HasSessionArgument()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = ValidateCommand.Create();

        // Assert — command accepts <session> FileInfo argument
        Assert.NotNull(command);
    }
}
