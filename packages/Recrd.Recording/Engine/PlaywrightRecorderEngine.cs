using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Recrd.Core.Pipeline;
using Recrd.Core.Serialization;
using Recrd.Recording.Snapshots;

namespace Recrd.Recording.Engine;

/// <summary>
/// Implements <see cref="IRecorderEngine"/> using Playwright .NET.
/// Launches a clean BrowserContext, injects the JS recording agent, captures 7 DOM event types,
/// and pushes <see cref="RecordedEvent"/> instances to the <see cref="IRecordingChannel"/> pipeline.
/// </summary>
public sealed class PlaywrightRecorderEngine : IRecorderEngine
{
    private readonly IRecordingChannel _channel;
    private IPlaywright? _playwright;
    private IBrowser? _recordingBrowser;
    private IBrowserContext? _recordingContext;
    private IPage? _recordingPage;
    private SessionBuilder? _sessionBuilder;
    private string _agentScript = string.Empty;
#pragma warning disable CS0414 // Field assigned but never read — used in Plan 04 (Inspector)
    private bool _isPaused;
#pragma warning restore CS0414
    private CancellationTokenSource? _snapshotCts;
    private PartialSnapshotWriter? _snapshotWriter;
    private string _partialPath = string.Empty;

    // Inspector fields — wired in Plan 04
    // Popup tracking — wired in Plan 05

    public PlaywrightRecorderEngine(IRecordingChannel channel)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    /// <inheritdoc/>
    public async Task<Session> StartAsync(RecorderOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Load embedded recording agent
        _agentScript = LoadEmbeddedScript();

        // Create Playwright instance
        _playwright = await Playwright.CreateAsync();

        // Select browser type
        var browserType = options.BrowserEngine.ToLowerInvariant() switch
        {
            "firefox" => _playwright.Firefox,
            "webkit" => _playwright.Webkit,
            _ => _playwright.Chromium,
        };

        // Launch browser (headed = visible window)
        _recordingBrowser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !options.Headed,
        });

        // Create a clean context — no StorageState means zero cookies/localStorage (REC-01)
        _recordingContext = await _recordingBrowser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new Microsoft.Playwright.ViewportSize
            {
                Width = options.ViewportSize.Width,
                Height = options.ViewportSize.Height,
            },
        });

        // CRITICAL ORDER (RESEARCH.md Pitfall 1):
        // ExposeFunctionAsync MUST be registered before AddInitScriptAsync.
        // The init script runs when the page loads; __recrdCapture must already be defined.
        await _recordingContext.ExposeFunctionAsync("__recrdCapture", (string payload) =>
        {
            // Fire-and-forget within Playwright callback context; sync scheduling
            _ = HandleCapturedEventAsync(payload);
        });

        await _recordingContext.AddInitScriptAsync(script: _agentScript);

        // Open the recording page
        _recordingPage = await _recordingContext.NewPageAsync();

        // Build session metadata
        var metadata = new SessionMetadata(
            Id: Guid.NewGuid().ToString("D"),
            CreatedAt: DateTimeOffset.UtcNow,
            BrowserEngine: options.BrowserEngine,
            ViewportSize: options.ViewportSize,
            BaseUrl: options.BaseUrl);

        _sessionBuilder = new SessionBuilder(metadata);

        // Start partial snapshot writer (30s interval by default per REC-09)
        _partialPath = Path.Combine(options.OutputDirectory, $"{metadata.Id}.recrd.partial");
        _snapshotWriter = new PartialSnapshotWriter(
            () => _sessionBuilder!.Build(),
            _partialPath,
            options.SnapshotInterval);
        _snapshotWriter.Start();

        // Navigate to base URL if provided
        if (!string.IsNullOrEmpty(options.BaseUrl))
        {
            await _recordingPage.GotoAsync(options.BaseUrl);
        }

        return _sessionBuilder.Build();
    }

    /// <inheritdoc/>
    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        _isPaused = true;
        if (_recordingPage is not null)
        {
            await _recordingPage.EvaluateAsync("window.__recrdSetMode('pause')");
        }
    }

    /// <inheritdoc/>
    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        _isPaused = false;
        if (_recordingPage is not null)
        {
            await _recordingPage.EvaluateAsync("window.__recrdSetMode('record')");
        }
    }

    /// <inheritdoc/>
    public async Task<Session> StopAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputPath);

        // 1. Cancel snapshot timer
        if (_snapshotWriter is not null)
            await _snapshotWriter.DisposeAsync();

        // 2. Build final session
        var session = _sessionBuilder!.Build();

        // 3. Serialize and write .recrd file (UTF-8 without BOM per JSON spec)
        var json = JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session);
        await File.WriteAllTextAsync(outputPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);

        // 4. Delete .recrd.partial per D-11
        _snapshotWriter?.DeletePartialFile();

        // 5. Complete channel — no more events will be written
        _channel.Complete();

        // 6. Close browser resources
        if (_recordingPage is not null) await _recordingPage.CloseAsync();
        if (_recordingContext is not null) await _recordingContext.CloseAsync();
        if (_recordingBrowser is not null) await _recordingBrowser.CloseAsync();

        return session;
    }

    /// <inheritdoc/>
    public async Task<Session> RecoverAsync(string partialPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(partialPath))
            throw new FileNotFoundException($"No partial snapshot found at '{partialPath}'.", partialPath);

        var json = await File.ReadAllTextAsync(partialPath, Encoding.UTF8, cancellationToken);
        var session = JsonSerializer.Deserialize(json, RecrdJsonContext.Default.Session)
            ?? throw new InvalidOperationException($"Failed to deserialize session from '{partialPath}'.");
        return session;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Cancel snapshot timer if running
        if (_snapshotWriter is not null)
        {
            await _snapshotWriter.DisposeAsync();
            _snapshotWriter = null;
        }

        if (_snapshotCts is not null)
        {
            await _snapshotCts.CancelAsync();
            _snapshotCts.Dispose();
            _snapshotCts = null;
        }

        if (_recordingPage is not null)
        {
            await _recordingPage.CloseAsync();
            _recordingPage = null;
        }

        if (_recordingContext is not null)
        {
            await _recordingContext.CloseAsync();
            _recordingContext = null;
        }

        if (_recordingBrowser is not null)
        {
            await _recordingBrowser.CloseAsync();
            _recordingBrowser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static string LoadEmbeddedScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Recrd.Recording.Scripts.recording-agent.js";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. " +
                "Ensure Scripts/recording-agent.js has Build Action = EmbeddedResource.");

        using var reader = new System.IO.StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private async Task HandleCapturedEventAsync(string payload)
    {
        if (_sessionBuilder is null)
            return;

        // Try to build a regular RecordedEvent
        var evt = RecordedEventBuilder.Build(payload);
        if (evt is not null)
        {
            await _channel.WriteAsync(evt);
            _sessionBuilder.AddStep(_sessionBuilder.ConvertToStep(evt));
            return;
        }

        // Try to parse as a special event (TagStart, TagConfirm, AssertStart, AssertConfirm)
        var special = RecordedEventBuilder.ParseSpecialEvent(payload);
        if (special.HasValue)
        {
            // Variable tagging and assertion flows — stubbed for Plan 04 (Inspector)
            HandleSpecialEvent(special.Value.Type, special.Value.Data);
        }
    }

    private static void HandleSpecialEvent(string type, System.Text.Json.JsonElement data)
    {
        // Plan 04 will implement the full tag/assert confirmation flows.
        // For now, log to debug output so events are not silently dropped.
        System.Diagnostics.Debug.WriteLine($"[Recrd] Special event received: {type}");
    }
}
