using Recrd.Data;
using Xunit;

namespace Recrd.Data.Tests;

public class JsonDataProviderTests
{
    // DATA-04: Flat JSON array — returns dictionary with correct keys
    [Fact]
    public async Task StreamAsync_FlatJsonArray_ReturnsDictionaryWithCorrectKeys()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "[{\"name\":\"Gil\",\"age\":\"30\"}]");
            var provider = new JsonDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.Equal("Gil", results[0]["name"]);
            Assert.Equal("30", results[0]["age"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-04: Nested object flattening — returns dot-notation key
    [Fact]
    public async Task StreamAsync_NestedObject_ReturnsDotNotationKey()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "[{\"user\":{\"name\":\"Gil\"}}]");
            var provider = new JsonDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.True(results[0].ContainsKey("user.name"), "Nested object should flatten to 'user.name'");
            Assert.Equal("Gil", results[0]["user.name"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-04: Deeply nested — returns deep dot-notation key
    [Fact]
    public async Task StreamAsync_DeeplyNestedObject_ReturnsDeepDotNotationKey()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "[{\"a\":{\"b\":{\"c\":\"v\"}}}]");
            var provider = new JsonDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.True(results[0].ContainsKey("a.b.c"), "Deep nested should flatten to 'a.b.c'");
            Assert.Equal("v", results[0]["a.b.c"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-04: Array field silently skipped per D-04
    [Fact]
    public async Task StreamAsync_ArrayField_SkipsArrayKeyReturnsOtherKeys()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "[{\"tags\":[\"a\",\"b\"],\"name\":\"x\"}]");
            var provider = new JsonDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.False(results[0].ContainsKey("tags"), "Array field should be silently skipped");
            Assert.Equal("x", results[0]["name"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-04: Mixed nested and flat — returns correct combined dictionary
    [Fact]
    public async Task StreamAsync_MixedNestedAndFlat_ReturnsCorrectCombinedDictionary()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "[{\"name\":\"Gil\",\"address\":{\"city\":\"SP\",\"zip\":\"01310\"}}]");
            var provider = new JsonDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.Equal("Gil", results[0]["name"]);
            Assert.Equal("SP", results[0]["address.city"]);
            Assert.Equal("01310", results[0]["address.zip"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-05: Non-array root (object {}) throws DataParseException
    [Fact]
    public async Task StreamAsync_RootIsObject_ThrowsDataParseExceptionMentioningRootArray()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "{\"name\":\"Gil\"}");
            var provider = new JsonDataProvider(path);
            var ex = await Assert.ThrowsAsync<DataParseException>(async () =>
            {
                await foreach (var _ in provider.StreamAsync()) { }
            });
            Assert.Contains("root", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("array", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally { File.Delete(path); }
    }

    // DATA-05: Non-array root (string "hello") throws DataParseException
    [Fact]
    public async Task StreamAsync_RootIsString_ThrowsDataParseException()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "\"hello\"");
            var provider = new JsonDataProvider(path);
            var ex = await Assert.ThrowsAsync<DataParseException>(async () =>
            {
                await foreach (var _ in provider.StreamAsync()) { }
            });
            Assert.NotNull(ex.Message);
        }
        finally { File.Delete(path); }
    }

    // DATA-04: Null value in JSON renders as empty string
    [Fact]
    public async Task StreamAsync_NullValue_RendersAsEmptyString()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "[{\"name\":null,\"age\":\"30\"}]");
            var provider = new JsonDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.Equal("", results[0]["name"]);
            Assert.Equal("30", results[0]["age"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-04: Boolean and number values render as ToString()
    [Theory]
    [InlineData("[{\"active\":true,\"count\":42}]", "active", "True", "count", "42")]
    [InlineData("[{\"active\":false,\"ratio\":3.14}]", "active", "False", "ratio", "3.14")]
    public async Task StreamAsync_BooleanAndNumberValues_RenderAsToString(
        string json, string key1, string expected1, string key2, string expected2)
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, json);
            var provider = new JsonDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.Equal(expected1, results[0][key1]);
            Assert.Equal(expected2, results[0][key2]);
        }
        finally { File.Delete(path); }
    }
}
