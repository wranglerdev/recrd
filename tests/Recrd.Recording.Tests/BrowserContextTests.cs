using Xunit;

namespace Recrd.Recording.Tests;

public class BrowserContextTests
{
    [Fact]
    public async Task Start_LaunchesCleanBrowserContext_ZeroCookies()
        => Assert.Fail("Red: REC-01 — clean BrowserContext launch not implemented");

    [Fact]
    public async Task Start_LaunchesCleanBrowserContext_ZeroLocalStorage()
        => Assert.Fail("Red: REC-01 — clean BrowserContext localStorage check not implemented");

    [Fact]
    public async Task Start_UsesSpecifiedBrowserEngine()
        => Assert.Fail("Red: REC-01 — browser engine selection not implemented");

    [Fact]
    public async Task Start_AppliesViewportSize()
        => Assert.Fail("Red: REC-01 — viewport size application not implemented");
}
