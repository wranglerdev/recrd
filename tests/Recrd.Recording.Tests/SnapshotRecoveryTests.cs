using Xunit;

namespace Recrd.Recording.Tests;

public class SnapshotRecoveryTests
{
    [Fact]
    public async Task PartialSnapshot_WrittenEvery30Seconds()
        => Assert.Fail("Red: REC-09 — partial snapshot written every 30s not implemented");

    [Fact]
    public async Task PartialSnapshot_ContainsCurrentSessionState()
        => Assert.Fail("Red: REC-09 — partial snapshot contains current session state not implemented");

    [Fact]
    public async Task PartialSnapshot_DeletedOnSuccessfulStop()
        => Assert.Fail("Red: REC-09 — partial snapshot deleted on successful stop not implemented");

    [Fact]
    public async Task Recover_ReconstructsSessionFromPartial()
        => Assert.Fail("Red: REC-10 — recover reconstructs session from partial not implemented");

    [Fact]
    public async Task Recover_ThrowsWhenNoPartialExists()
        => Assert.Fail("Red: REC-10 — recover throws when no partial exists not implemented");
}
