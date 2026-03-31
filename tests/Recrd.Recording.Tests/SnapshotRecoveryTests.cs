using System.Text;
using System.Text.Json;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Recrd.Core.Serialization;
using Recrd.Recording.Engine;
using Recrd.Recording.Snapshots;
using Xunit;

namespace Recrd.Recording.Tests;

public class SnapshotRecoveryTests
{
    [Fact]
    public async Task PartialSnapshot_WrittenEvery30Seconds()
    {
        // Use a fast interval so we can test without waiting 30s
        var partialPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.recrd.partial");
        var session = BuildMinimalSession();

        try
        {
            await using var writer = new PartialSnapshotWriter(
                () => session,
                partialPath,
                TimeSpan.FromMilliseconds(50));

            writer.Start();

            // Wait a bit longer than the interval for the first tick
            await Task.Delay(200);

            Assert.True(File.Exists(partialPath), "Partial file should exist after first tick");
        }
        finally
        {
            if (File.Exists(partialPath)) File.Delete(partialPath);
        }
    }

    [Fact]
    public async Task PartialSnapshot_ContainsCurrentSessionState()
    {
        var partialPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.recrd.partial");
        var session = BuildMinimalSession();

        try
        {
            var writer = new PartialSnapshotWriter(
                () => session,
                partialPath,
                TimeSpan.FromSeconds(30)); // won't tick; we call directly

            // Call WriteSnapshotAsync directly (internal, accessible via InternalsVisibleTo)
            await writer.WriteSnapshotAsync();

            var json = await File.ReadAllTextAsync(partialPath, Encoding.UTF8);
            var deserialized = JsonSerializer.Deserialize(json, RecrdJsonContext.Default.Session);

            Assert.NotNull(deserialized);
            Assert.Equal(session.Metadata.Id, deserialized!.Metadata.Id);
            Assert.Equal(1, deserialized.SchemaVersion);

            await writer.DisposeAsync();
        }
        finally
        {
            if (File.Exists(partialPath)) File.Delete(partialPath);
        }
    }

    [Fact]
    public async Task PartialSnapshot_DeletedOnSuccessfulStop()
    {
        var partialPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.recrd.partial");
        var session = BuildMinimalSession();

        // Write the partial file first
        await using var writer = new PartialSnapshotWriter(
            () => session,
            partialPath,
            TimeSpan.FromSeconds(30));
        await writer.WriteSnapshotAsync();

        Assert.True(File.Exists(partialPath), "Partial file should exist before stop");

        // Act: simulate stop cleanup
        writer.DeletePartialFile();

        Assert.False(File.Exists(partialPath), "Partial file should be deleted after stop");
    }

    [Fact]
    public async Task Recover_ReconstructsSessionFromPartial()
    {
        var partialPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.recrd.partial");
        var session = BuildMinimalSession();

        try
        {
            // Write partial file
            var json = JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session);
            await File.WriteAllTextAsync(partialPath, json, Encoding.UTF8);

            // Act: recover via engine
            var channel = new Recrd.Core.Pipeline.RecordingChannel();
            await using var engine = new PlaywrightRecorderEngine(channel);
            var recovered = await engine.RecoverAsync(partialPath);

            // Assert
            Assert.NotNull(recovered);
            Assert.Equal(session.Metadata.Id, recovered.Metadata.Id);
            Assert.Equal(1, recovered.SchemaVersion);
        }
        finally
        {
            if (File.Exists(partialPath)) File.Delete(partialPath);
        }
    }

    [Fact]
    public async Task Recover_ThrowsWhenNoPartialExists()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.recrd.partial");

        var channel = new Recrd.Core.Pipeline.RecordingChannel();
        await using var engine = new PlaywrightRecorderEngine(channel);

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => engine.RecoverAsync(missingPath));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static Session BuildMinimalSession() =>
        new Session(
            SchemaVersion: 1,
            Metadata: new SessionMetadata(
                Id: Guid.NewGuid().ToString("D"),
                CreatedAt: DateTimeOffset.UtcNow,
                BrowserEngine: "chromium",
                ViewportSize: new ViewportSize(1280, 720),
                BaseUrl: null),
            Variables: Array.Empty<Variable>(),
            Steps: Array.Empty<IStep>());
}
