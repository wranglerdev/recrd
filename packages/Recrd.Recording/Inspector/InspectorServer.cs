using System.Reflection;
using System.Text;
using Microsoft.Playwright;
using Recrd.Core.Pipeline;

namespace Recrd.Recording.Inspector;

/// <summary>
/// Manages a secondary BrowserContext that hosts the inspector side-panel HTML page.
/// Serves the embedded <c>inspector.html</c> via <see cref="IPage.RouteAsync"/> and exposes
/// <c>window.__recrdInspectorCallback</c> for bidirectional communication with the panel.
/// </summary>
internal sealed class InspectorServer : IAsyncDisposable
{
    private IBrowser? _inspectorBrowser;
    private IBrowserContext? _inspectorContext;
    private IPage? _inspectorPage;
    private bool _isOpen;
    private readonly Func<string, Task> _onInspectorCallback;

    public InspectorServer(Func<string, Task> onInspectorCallback)
    {
        _onInspectorCallback = onInspectorCallback ?? throw new ArgumentNullException(nameof(onInspectorCallback));
    }

    public bool IsOpen => _isOpen;

    /// <summary>
    /// Launches a secondary Chromium browser in app mode, serves inspector.html via RouteAsync,
    /// and registers <c>window.__recrdInspectorCallback</c>.
    /// </summary>
    public async Task OpenAsync(IPlaywright playwright)
    {
        ArgumentNullException.ThrowIfNull(playwright);

        // Launch secondary browser in app mode (per D-06, REC-11)
        _inspectorBrowser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            Args = new[] { "--app=http://recrd-inspector.local/" }
        });

        _inspectorContext = await _inspectorBrowser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 420, Height = 700 }
        });

        _inspectorPage = await _inspectorContext.NewPageAsync();

        // CRITICAL ORDER (RESEARCH.md Pitfall 1 analogy):
        // Register inspector callback BEFORE routing so it is available at page load.
        await _inspectorContext.ExposeFunctionAsync("__recrdInspectorCallback", async (string payload) =>
        {
            await _onInspectorCallback(payload);
        });

        // Serve inspector HTML via RouteAsync (per RESEARCH.md Pattern 3)
        var html = LoadEmbeddedHtml();
        await _inspectorPage.RouteAsync("**/*", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/html; charset=utf-8",
                Body = html
            });
        });

        await _inspectorPage.GotoAsync("http://recrd-inspector.local/");
        _isOpen = true;

        // Handle inspector being closed by the user (per RESEARCH.md Pitfall 5)
        _inspectorPage.Close += (_, _) => _isOpen = false;
    }

    /// <summary>
    /// Pushes a captured event to the inspector's live event stream via <c>window.__recrdPush</c>.
    /// </summary>
    public async Task PushEventAsync(RecordedEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        if (!_isOpen || _inspectorPage is null) return;

        try
        {
            // Get first (highest-priority) selector value for display
            var selectorValue = evt.Selectors.Count > 0
                ? evt.Selectors[0].Values.Values.FirstOrDefault() ?? string.Empty
                : string.Empty;

            // Escape the JSON strings to avoid injection into the EvaluateAsync call
            var typeStr = System.Text.Json.JsonEncodedText.Encode(evt.EventType.ToString()).ToString();
            var selectorStr = System.Text.Json.JsonEncodedText.Encode(selectorValue).ToString();
            var timestampStr = System.Text.Json.JsonEncodedText.Encode($"+{evt.TimestampMs}ms").ToString();

            await _inspectorPage.EvaluateAsync(
                $"window.__recrdPush({{type:{typeStr},selector:{selectorStr},timestamp:{timestampStr}}})");
        }
        catch (PlaywrightException)
        {
            // Inspector page closed — swallow and mark closed (per RESEARCH.md Pitfall 5)
            _isOpen = false;
        }
    }

    /// <summary>
    /// Updates the state badge on the inspector panel ('record', 'pause', or 'idle').
    /// </summary>
    public async Task SetStateAsync(string state)
    {
        if (!_isOpen || _inspectorPage is null) return;
        try
        {
            var safeState = state.Replace("'", "\\'");
            await _inspectorPage.EvaluateAsync($"window.__recrdSetState('{safeState}')");
        }
        catch (PlaywrightException) { _isOpen = false; }
    }

    /// <summary>
    /// Opens the "Tag as Variable" dialog on the inspector with the given selector JSON.
    /// </summary>
    public async Task ShowTagDialogAsync(string selectorJson)
    {
        if (!_isOpen || _inspectorPage is null) return;
        try
        {
            await _inspectorPage.EvaluateAsync($"window.__recrdShowTagDialog({selectorJson})");
        }
        catch (PlaywrightException) { _isOpen = false; }
    }

    /// <summary>
    /// Shows an error inside the tag dialog ('duplicate' or 'invalid').
    /// </summary>
    public async Task ShowTagErrorAsync(string errorType)
    {
        if (!_isOpen || _inspectorPage is null) return;
        try
        {
            var safeType = errorType.Replace("'", "\\'");
            await _inspectorPage.EvaluateAsync($"window.__recrdTagError('{safeType}')");
        }
        catch (PlaywrightException) { _isOpen = false; }
    }

    /// <summary>
    /// Closes the tag dialog and adds a variable chip for the given variable name.
    /// </summary>
    public async Task ShowTagSuccessAsync(string name)
    {
        if (!_isOpen || _inspectorPage is null) return;
        try
        {
            var safeName = name.Replace("'", "\\'");
            await _inspectorPage.EvaluateAsync($"window.__recrdTagSuccess('{safeName}')");
        }
        catch (PlaywrightException) { _isOpen = false; }
    }

    /// <summary>
    /// Opens the "Add Assertion" dialog on the inspector with the given assertion data JSON.
    /// </summary>
    public async Task ShowAssertDialogAsync(string dataJson)
    {
        if (!_isOpen || _inspectorPage is null) return;
        try
        {
            await _inspectorPage.EvaluateAsync($"window.__recrdShowAssertDialog({dataJson})");
        }
        catch (PlaywrightException) { _isOpen = false; }
    }

    private static string LoadEmbeddedHtml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("Recrd.Recording.Panel.inspector.html")
            ?? throw new InvalidOperationException(
                "inspector.html not found as embedded resource. " +
                "Ensure Panel/inspector.html has Build Action = EmbeddedResource.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public async ValueTask DisposeAsync()
    {
        _isOpen = false;
        if (_inspectorPage is not null)
        {
            try { await _inspectorPage.CloseAsync(); } catch (PlaywrightException) { }
            _inspectorPage = null;
        }

        if (_inspectorContext is not null)
        {
            try { await _inspectorContext.CloseAsync(); } catch (PlaywrightException) { }
            _inspectorContext = null;
        }

        if (_inspectorBrowser is not null)
        {
            try { await _inspectorBrowser.CloseAsync(); } catch (PlaywrightException) { }
            _inspectorBrowser = null;
        }
    }
}
