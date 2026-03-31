using System.Text;
using System.Text.Json;
using Recrd.Core.Ast;
using Recrd.Core.Serialization;

namespace Recrd.Recording.Snapshots;

internal sealed class PartialSnapshotWriter : IAsyncDisposable
{
    private readonly Func<Session> _sessionProvider;
    private readonly string _partialPath;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public PartialSnapshotWriter(Func<Session> sessionProvider, string partialPath, TimeSpan interval)
    {
        _sessionProvider = sessionProvider ?? throw new ArgumentNullException(nameof(sessionProvider));
        _partialPath = partialPath ?? throw new ArgumentNullException(nameof(partialPath));
        _interval = interval;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _loopTask = RunSnapshotLoopAsync(_cts.Token);
    }

    private async Task RunSnapshotLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await WriteSnapshotAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation on stop — swallow
        }
    }

    internal async Task WriteSnapshotAsync(CancellationToken ct = default)
    {
        var session = _sessionProvider();
        var json = JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session);
        // UTF-8 without BOM per JSON spec
        await File.WriteAllTextAsync(_partialPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), ct);
    }

    public void DeletePartialFile()
    {
        if (File.Exists(_partialPath))
            File.Delete(_partialPath);
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            if (_loopTask is not null)
                await _loopTask;
            _cts.Dispose();
        }
    }
}
