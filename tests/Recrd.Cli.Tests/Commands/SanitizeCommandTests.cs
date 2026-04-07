// Phase 08 — TDD red phase
// Tests for SanitizeCommand session sanitization (CLI-05, D-08)

using Recrd.Cli.Commands;
using Xunit;

namespace Recrd.Cli.Tests.Commands;

public class SanitizeCommandTests
{
    [Fact]
    public void Sanitize_ProducesOutputAtBasenameWithSanitizedExtension()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = SanitizeCommand.Create();

        // Assert — output file is <basename>.sanitized.recrd in same directory
        Assert.NotNull(command);
    }

    [Fact]
    public void Sanitize_OutputHasNoLiteralValuesInSelectorValuesOrStepPayload()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = SanitizeCommand.Create();

        // Assert — output has no literal values in Selector.Values or step Payload
        Assert.NotNull(command);
    }

    [Fact]
    public void Sanitize_WithExplicitOut_WritesOutputToSpecifiedPath()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = SanitizeCommand.Create();

        // Assert — --out <path> overrides default output location
        Assert.NotNull(command);
    }

    [Fact]
    public void Sanitize_OriginalFileIsNeverModified()
    {
        // Arrange / Act
        Assert.True(false, "Not implemented — red phase");
        var command = SanitizeCommand.Create();

        // Assert — input session file is not modified by sanitize operation
        Assert.NotNull(command);
    }
}
