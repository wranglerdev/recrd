using Recrd.Core.Exceptions;
using Recrd.Core.Interfaces;
using Recrd.Data;

namespace Recrd.Data.Tests;

/// <summary>
/// User Story: As a test author, I want to supply a JSON file with test data
/// so that structured data can drive parameterized test scenarios.
///
/// Acceptance criteria:
/// - Parses root-level JSON arrays of flat objects
/// - Flattens nested objects using dot-notation keys
/// - Throws DataParseException for non-array root
/// </summary>
public sealed class JsonDataProviderTests : IAsyncLifetime
{
    private string _tempDir = default!;
    private readonly JsonDataProvider _provider = new();

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
    public async Task ReadAsync_ValidJsonArray_ReturnsAllRows()
    {
        var json = """[{"login":"admin","senha":"123"},{"login":"user","senha":"abc"}]""";
        var path = WriteTempFile("data.json", json);

        var rows = await _provider.ReadAsync(path, new DataProviderOptions()).ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal("admin", rows[0]["login"]);
        Assert.Equal("abc", rows[1]["senha"]);
    }

    [Fact]
    public async Task ReadAsync_NonArrayRoot_ThrowsDataParseException()
    {
        var json = """{"login":"admin"}""";
        var path = WriteTempFile("object.json", json);

        var ex = await Assert.ThrowsAsync<DataParseException>(async () =>
            await _provider.ReadAsync(path, new DataProviderOptions()).ToListAsync());

        Assert.Contains("array", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadAsync_NestedObjects_FlattenedWithDotNotation()
    {
        var json = """[{"user":{"name":"Gil","role":"admin"}}]""";
        var path = WriteTempFile("nested.json", json);

        var rows = await _provider.ReadAsync(path, new DataProviderOptions()).ToListAsync();

        Assert.Single(rows);
        Assert.True(rows[0].ContainsKey("user.name"), $"Expected 'user.name' but got: {string.Join(", ", rows[0].Keys)}");
        Assert.Equal("Gil", rows[0]["user.name"]);
        Assert.Equal("admin", rows[0]["user.role"]);
    }

    [Fact]
    public async Task ReadAsync_EmptyArray_ReturnsNoRows()
    {
        var path = WriteTempFile("empty.json", "[]");

        var rows = await _provider.ReadAsync(path, new DataProviderOptions()).ToListAsync();

        Assert.Empty(rows);
    }
}
