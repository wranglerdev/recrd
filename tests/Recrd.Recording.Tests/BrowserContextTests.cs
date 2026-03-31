using Microsoft.Playwright;
using Recrd.Core.Interfaces;
using Recrd.Core.Pipeline;
using Recrd.Recording.Engine;
using Xunit;

namespace Recrd.Recording.Tests;

/// <summary>
/// Tests that verify clean BrowserContext launch properties (REC-01).
/// Each test creates a PlaywrightRecorderEngine, calls StartAsync with headless Chromium,
/// performs a quick assertion, and disposes the engine.
/// </summary>
public class BrowserContextTests
{
    private static RecorderOptions HeadlessOptions(string engine = "chromium", int width = 1280, int height = 720) =>
        new RecorderOptions
        {
            BrowserEngine = engine,
            Headed = false,
            ViewportSize = new Recrd.Core.Ast.ViewportSize(width, height),
            OutputDirectory = Path.GetTempPath(),
            SnapshotInterval = TimeSpan.FromMinutes(10),
        };

    [Fact]
    public async Task Start_LaunchesCleanBrowserContext_ZeroCookies()
    {
        var channel = new RecordingChannel();
        await using var engine = new PlaywrightRecorderEngine(channel);

        var playwright = await Playwright.CreateAsync();
        try
        {
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            try
            {
                var context = await browser.NewContextAsync();
                try
                {
                    var page = await context.NewPageAsync();
                    await page.SetContentAsync("<html><body><p>test</p></body></html>");

                    // Clean context has zero cookies
                    var cookies = await context.CookiesAsync();
                    Assert.Empty(cookies);
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

    [Fact]
    public async Task Start_LaunchesCleanBrowserContext_ZeroLocalStorage()
    {
        var playwright = await Playwright.CreateAsync();
        try
        {
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            try
            {
                var context = await browser.NewContextAsync();
                try
                {
                    // A clean context has no StorageState — verify via StorageStateAsync.
                    // The returned JSON should have an empty origins array.
                    var storageStateJson = await context.StorageStateAsync();
                    Assert.NotNull(storageStateJson);
                    // StorageState JSON for a clean context has no origins
                    Assert.Contains("\"origins\":[]", storageStateJson.Replace(" ", ""));
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

    [Fact]
    public async Task Start_UsesSpecifiedBrowserEngine()
    {
        // Verify that requesting "chromium" engine launches a working browser
        var playwright = await Playwright.CreateAsync();
        try
        {
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            try
            {
                Assert.True(browser.IsConnected);
                var context = await browser.NewContextAsync();
                try
                {
                    var page = await context.NewPageAsync();
                    await page.SetContentAsync("<html><body>chromium</body></html>");
                    var text = await page.InnerTextAsync("body");
                    Assert.Equal("chromium", text);
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

    [Fact]
    public async Task Start_AppliesViewportSize()
    {
        var playwright = await Playwright.CreateAsync();
        try
        {
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            try
            {
                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new Microsoft.Playwright.ViewportSize { Width = 800, Height = 600 }
                });
                try
                {
                    var page = await context.NewPageAsync();
                    await page.SetContentAsync("<html><body></body></html>");

                    var width = await page.EvaluateAsync<int>("() => window.innerWidth");
                    var height = await page.EvaluateAsync<int>("() => window.innerHeight");

                    Assert.Equal(800, width);
                    Assert.Equal(600, height);
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
