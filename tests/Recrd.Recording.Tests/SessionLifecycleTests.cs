using Xunit;

namespace Recrd.Recording.Tests;

public class SessionLifecycleTests
{
    [Fact]
    public async Task Pause_FreezesEventCapture()
        => Assert.Fail("Red: REC-06 — pause freezes event capture not implemented");

    [Fact]
    public async Task Pause_EnablesAssertionMode()
        => Assert.Fail("Red: REC-06 — pause enables assertion mode not implemented");

    [Fact]
    public async Task Resume_RestartsEventCapture()
        => Assert.Fail("Red: REC-07 — resume restarts event capture not implemented");

    [Fact]
    public async Task Stop_FlushesSessionToRecrdFile()
        => Assert.Fail("Red: REC-08 — stop flushes session to .recrd file not implemented");

    [Fact]
    public async Task Stop_SessionDeserializesBackWithAllSteps()
        => Assert.Fail("Red: REC-08 — stop session deserializes back with all steps not implemented");

    [Fact]
    public async Task Stop_OutputFileIsUtf8Json()
        => Assert.Fail("Red: REC-08 — stop output file is UTF-8 JSON not implemented");
}
