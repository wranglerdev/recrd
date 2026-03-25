using Recrd.Core.Exceptions;
using Recrd.Core.Interfaces;
using Recrd.Data;

namespace Recrd.Data.Tests;

/// <summary>
/// User Story: As a test author, I want to supply a CSV file with test data
/// so that the same recording executes against multiple data sets.
///
/// Acceptance criteria:
/// - Parses valid RFC 4180 CSV files correctly
/// - Handles UTF-8 BOM without corruption
/// - Streams large files without OOM
/// - Throws DataParseException with line number on malformed input
/// </summary>
public sealed class CsvDataProviderTests : IAsyncLifetime
{
    private string _tempDir = default!;
    private readonly CsvDataProvider _provider = new();

    public Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Directory.Delete(_tempDir, recursive: true);
        return Task.CompletedTask;
    }

    private string WriteTempFile(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task ReadAsync_ValidCsv_ReturnsAllRows()
    {
        var csv = "login,senha\nadmin,123\nuser,abc\n";
        var path = WriteTempFile("data.csv", csv);

        var rows = await _provider.ReadAsync(path, new DataProviderOptions()).ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal("admin", rows[0]["login"]);
        Assert.Equal("abc", rows[1]["senha"]);
    }

    [Fact]
    public async Task ReadAsync_MalformedFile_ThrowsDataParseExceptionWithLineNumber()
    {
        // Mismatched column count on line 3
        var csv = "login,senha\nadmin,123\nonly_one_column\n";
        var path = WriteTempFile("bad.csv", csv);

        var ex = await Assert.ThrowsAsync<DataParseException>(async () =>
            await _provider.ReadAsync(path, new DataProviderOptions()).ToListAsync());

        Assert.NotNull(ex.LineNumber);
        Assert.Equal(3, ex.LineNumber);
    }

    [Fact]
    public async Task ReadAsync_BomEncodedFile_ParsesFirstColumnWithoutGarbage()
    {
        // UTF-8 BOM prefix
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var content = System.Text.Encoding.UTF8.GetBytes("login,senha\nadmin,123\n");
        var path = Path.Combine(_tempDir, "bom.csv");
        await File.WriteAllBytesAsync(path, [.. bom, .. content]);

        var rows = await _provider.ReadAsync(path, new DataProviderOptions()).ToListAsync();

        Assert.Single(rows);
        Assert.True(rows[0].ContainsKey("login"), $"Expected key 'login' but got: {string.Join(", ", rows[0].Keys)}");
    }

    [Fact]
    public async Task ReadAsync_CustomDelimiter_ParsesCorrectly()
    {
        var csv = "login;senha\nadmin;123\n";
        var path = WriteTempFile("semicolon.csv", csv);

        var rows = await _provider.ReadAsync(path, new DataProviderOptions(CsvDelimiter: ';')).ToListAsync();

        Assert.Single(rows);
        Assert.Equal("admin", rows[0]["login"]);
    }

    [Fact]
    public async Task ReadAsync_LargeFile_StreamsWithoutOOM()
    {
        // Generate ~50 MB CSV
        var path = Path.Combine(_tempDir, "large.csv");
        await using var writer = new StreamWriter(path);
        await writer.WriteLineAsync("col1,col2,col3,col4,col5");
        for (var i = 0; i < 500_000; i++)
            await writer.WriteLineAsync($"value{i},value{i},value{i},value{i},value{i}");

        var before = GC.GetTotalMemory(true);
        var count = 0;
        await foreach (var _ in _provider.ReadAsync(path, new DataProviderOptions()))
            count++;

        var after = GC.GetTotalMemory(false);
        var deltaBytes = after - before;

        Assert.Equal(500_000, count);
        Assert.True(deltaBytes < 100 * 1024 * 1024, $"Heap delta was {deltaBytes / 1024 / 1024} MB, expected < 100 MB");
    }

    [Fact]
    public async Task ReadAsync_QuotedFields_ParsesCorrectly()
    {
        var csv = "login,mensagem\nadmin,\"Olá, Admin\"\n";
        var path = WriteTempFile("quoted.csv", csv);

        var rows = await _provider.ReadAsync(path, new DataProviderOptions()).ToListAsync();

        Assert.Single(rows);
        Assert.Equal("Olá, Admin", rows[0]["mensagem"]);
    }
}
