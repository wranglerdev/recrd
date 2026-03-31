using Xunit;

namespace Recrd.Recording.Tests;

public class InspectorPanelTests
{
    [Fact]
    public async Task Inspector_OpensAsSecondaryBrowserContext()
        => Assert.Fail("Red: REC-11 — inspector opens as secondary BrowserContext not implemented");

    [Fact]
    public async Task Inspector_DisplaysLiveEventStream()
        => Assert.Fail("Red: REC-12 — inspector displays live event stream not implemented");

    [Fact]
    public async Task Inspector_EventStreamScrollsToNewest()
        => Assert.Fail("Red: REC-12 — inspector event stream scrolls to newest not implemented");

    [Fact]
    public async Task Inspector_TagAsVariable_ReplacesLiteralWithPlaceholder()
        => Assert.Fail("Red: REC-13 — tag as variable replaces literal with placeholder not implemented");

    [Fact]
    public async Task Inspector_TagAsVariable_DuplicateNameShowsWarning()
        => Assert.Fail("Red: REC-13 — tag as variable duplicate name shows warning not implemented");

    [Fact]
    public async Task Inspector_TagAsVariable_InvalidNameShowsWarning()
        => Assert.Fail("Red: REC-13 — tag as variable invalid name shows warning not implemented");

    [Fact]
    public async Task Inspector_AssertionBuilder_InsertsAssertionStepInPauseMode()
        => Assert.Fail("Red: REC-14 — assertion builder inserts assertion step in pause mode not implemented");

    [Fact]
    public async Task Inspector_AssertionBuilder_HiddenInRecordingMode()
        => Assert.Fail("Red: REC-14 — assertion builder hidden in recording mode not implemented");
}
