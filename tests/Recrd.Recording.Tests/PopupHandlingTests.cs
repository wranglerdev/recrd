using Microsoft.Playwright;
using Recrd.Recording.Engine;
using Xunit;

namespace Recrd.Recording.Tests;

/// <summary>
/// Tests for constrained popup handling (REC-15).
/// Verifies that events from popup pages carry the __popupScope marker,
/// and that the JS agent is automatically injected into popup pages.
/// </summary>
public class PopupHandlingTests
{
    [Fact]
    public async Task Popup_EventsCapturedFromNewPage()
    {
        // Verify that the RecordedEventBuilder adds __popupScope when isPopup=true in the JSON
        var json = """
        {
            "id": "evt-popup-001",
            "timestamp": 100.0,
            "type": "Click",
            "selectors": {
                "strategies": ["Css"],
                "values": {"Css": "button"}
            },
            "payload": {},
            "isPopup": true
        }
        """;

        var evt = RecordedEventBuilder.Build(json);

        Assert.NotNull(evt);
        Assert.True(evt!.Payload.ContainsKey("__popupScope"),
            "Events from popup pages must carry __popupScope marker in payload");
        Assert.Equal("true", evt.Payload["__popupScope"]);
    }

    [Fact]
    public async Task Popup_EventsHavePopupScopeMarker()
    {
        // Verify that __popupScope is "true" only when isPopup=true in the JSON,
        // and absent when isPopup=false.
        var popupJson = """
        {
            "id": "evt-popup-002",
            "timestamp": 200.0,
            "type": "InputChange",
            "selectors": {"strategies": ["Css"], "values": {"Css": "input"}},
            "payload": {"value": "test"},
            "isPopup": true
        }
        """;

        var normalJson = """
        {
            "id": "evt-normal-001",
            "timestamp": 300.0,
            "type": "Click",
            "selectors": {"strategies": ["Css"], "values": {"Css": "button"}},
            "payload": {},
            "isPopup": false
        }
        """;

        var popupEvt = RecordedEventBuilder.Build(popupJson);
        var normalEvt = RecordedEventBuilder.Build(normalJson);

        Assert.NotNull(popupEvt);
        Assert.NotNull(normalEvt);

        // Popup events have the scope marker
        Assert.True(popupEvt!.Payload.ContainsKey("__popupScope"));
        Assert.Equal("true", popupEvt.Payload["__popupScope"]);

        // Normal events do NOT have the scope marker
        Assert.False(normalEvt!.Payload.ContainsKey("__popupScope"),
            "Normal (non-popup) events must not carry __popupScope marker");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Popup_InitScriptInjectedAutomatically()
    {
        // Verify that BrowserContext-level AddInitScriptAsync propagates to popup pages.
        // Context-level init scripts run on every page opened in that context, including popups.
        // We test this by creating a context, registering an init script, opening a new page
        // (simulating a popup), and verifying the script ran.
        var playwright = await Playwright.CreateAsync();
        try
        {
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            try
            {
                var context = await browser.NewContextAsync();
                try
                {
                    // Register a context-level init script (simulates __recrdCapture registration)
                    await context.AddInitScriptAsync(script: "window.__recrdTestFlag = 'injected';");

                    // Open a "popup" page (new page in same context — analogous to window.open)
                    var popupPage = await context.NewPageAsync();
                    await popupPage.SetContentAsync("<html><body><p>popup</p></body></html>");

                    // Verify init script ran on the popup page
                    var flag = await popupPage.EvaluateAsync<string>("() => window.__recrdTestFlag");
                    Assert.Equal("injected", flag);
                }
                finally
                {
                    await context.CloseAsync();
                }
            }
            finally
            {
                await browser.CloseAsync();
            }
        }
        finally
        {
            playwright.Dispose();
        }
    }
}
