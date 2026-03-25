using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Recording.Tests;

/// <summary>
/// User Story: As a developer, I want the recording engine to capture real
/// browser interactions via Playwright/CDP so that recordings reflect
/// genuine user flows.
///
/// NOTE: These tests require a real browser (Playwright). They are marked
/// with the [Trait("Category", "Integration")] attribute and are skipped
/// in unit test runs via the CI filter.
///
/// Acceptance criteria:
/// - Click events produce a RecordedEvent of type "click" with valid selectors
/// - New sessions start with zero cookies and empty localStorage
/// - Captured elements provide selectors ranked by stability (data-testid first)
/// </summary>
public sealed class RecordingEngineTests
{
    [Fact(Skip = "Requires Playwright browser install — run with integration profile")]
    [Trait("Category", "Integration")]
    public async Task StartAsync_ClickOnFixturePage_EmitsClickRecordedEvent()
    {
        // Arrange: start recording engine against a fixture HTML page
        // Act: simulate a click via Playwright evaluation
        // Assert: Channel receives a RecordedEvent with EventType == "click"
        Assert.Fail("Not implemented — requires fixture web app");
    }

    [Fact(Skip = "Requires Playwright browser install — run with integration profile")]
    [Trait("Category", "Integration")]
    public async Task StartAsync_NewSession_HasNoCookiesOrLocalStorage()
    {
        // Arrange + Act: launch a new recording session
        // Assert: cookies count == 0, localStorage is empty
        Assert.Fail("Not implemented — requires fixture web app");
    }

    [Fact(Skip = "Requires Playwright browser install — run with integration profile")]
    [Trait("Category", "Integration")]
    public async Task StartAsync_ElementWithDataTestId_SelectorsRankedByStability()
    {
        // Arrange: page with element having data-testid, id, and class
        // Act: click the element
        // Assert: RecordedEvent.Selectors[0].Strategy == "data-testid"
        Assert.Fail("Not implemented — requires fixture web app");
    }
}
