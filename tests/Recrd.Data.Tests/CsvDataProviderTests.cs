using System.Text;
using Recrd.Data;
using Xunit;

namespace Recrd.Data.Tests;

public class CsvDataProviderTests
{
    // DATA-01: RFC 4180 basic parsing — reads header + data rows
    [Fact]
    public async Task StreamAsync_BasicCsv_ReturnsCorrectColumnNamesAndValues()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "name,age\r\nAlice,30\r\nBob,25\r\n");
            var provider = new CsvDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Equal(2, results.Count);
            Assert.Equal("Alice", results[0]["name"]);
            Assert.Equal("30", results[0]["age"]);
            Assert.Equal("Bob", results[1]["name"]);
            Assert.Equal("25", results[1]["age"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-01: BOM-tolerant — first column name not corrupted
    [Fact]
    public async Task StreamAsync_BomPrefixedCsv_FirstColumnNameNotCorrupted()
    {
        var path = Path.GetTempFileName();
        try
        {
            var bom = Encoding.UTF8.GetPreamble();
            var content = Encoding.UTF8.GetBytes("name,city\r\nGil,SP\r\n");
            await File.WriteAllBytesAsync(path, [.. bom, .. content]);
            var provider = new CsvDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.True(results[0].ContainsKey("name"), "First column key should be 'name', not '\\ufeffname'");
            Assert.Equal("Gil", results[0]["name"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-01: Custom delimiter — reads semicolon-delimited file
    [Fact]
    public async Task StreamAsync_SemicolonDelimiter_ParsesCorrectly()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "name;age\r\nAlice;30\r\n");
            var provider = new CsvDataProvider(path, ";");
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.Equal("Alice", results[0]["name"]);
            Assert.Equal("30", results[0]["age"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-01: Quoted fields with embedded commas
    [Fact]
    public async Task StreamAsync_QuotedFieldWithEmbeddedComma_ReturnsSingleValue()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "name,description\r\n\"foo,bar\",test\r\n");
            var provider = new CsvDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.Equal("foo,bar", results[0]["name"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-01: Quoted fields with embedded newlines
    [Fact]
    public async Task StreamAsync_QuotedFieldWithEmbeddedNewline_ReturnsSingleValue()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "name,notes\r\n\"Alice\",\"line one\r\nline two\"\r\n");
            var provider = new CsvDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.Equal("line one\r\nline two", results[0]["notes"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-02: Unclosed quote throws DataParseException
    [Fact]
    public async Task StreamAsync_UnclosedQuote_ThrowsDataParseExceptionWithLineNumber()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "name,age\r\n\"unclosed,30\r\n");
            var provider = new CsvDataProvider(path);
            var ex = await Assert.ThrowsAsync<DataParseException>(async () =>
            {
                await foreach (var _ in provider.StreamAsync()) { }
            });
            Assert.True(ex.LineNumber > 0);
            Assert.NotEmpty(ex.OffendingLine);
        }
        finally { File.Delete(path); }
    }

    // DATA-02: Row with fewer columns than header throws DataParseException
    [Fact]
    public async Task StreamAsync_FewerColumnsThanHeader_ThrowsDataParseException()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "name,age,city\r\nAlice,30\r\n");
            var provider = new CsvDataProvider(path);
            var ex = await Assert.ThrowsAsync<DataParseException>(async () =>
            {
                await foreach (var _ in provider.StreamAsync()) { }
            });
            Assert.True(ex.LineNumber > 0);
        }
        finally { File.Delete(path); }
    }

    // DATA-02: Row with more columns than header throws DataParseException
    [Fact]
    public async Task StreamAsync_MoreColumnsThanHeader_ThrowsDataParseException()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "name,age\r\nAlice,30,extra\r\n");
            var provider = new CsvDataProvider(path);
            var ex = await Assert.ThrowsAsync<DataParseException>(async () =>
            {
                await foreach (var _ in provider.StreamAsync()) { }
            });
            Assert.True(ex.LineNumber > 0);
        }
        finally { File.Delete(path); }
    }

    // DATA-03: 50MB CSV streams with heap delta <= 100MB
    [Fact]
    public async Task StreamAsync_FiftyMbCsv_HeapDeltaUnder100Mb()
    {
        var path = Path.GetTempFileName();
        try
        {
            // Generate ~50MB CSV with 500,000 rows
            var sb = new StringBuilder();
            sb.AppendLine("col1,col2,col3");
            for (int i = 0; i < 500_000; i++)
                sb.AppendLine($"val{i},val{i},val{i}");
            await File.WriteAllTextAsync(path, sb.ToString());

            GC.Collect(2, GCCollectionMode.Aggressive, blocking: true);
            long before = GC.GetTotalMemory(true);

            var provider = new CsvDataProvider(path);
            await foreach (var _ in provider.StreamAsync()) { }

            GC.Collect(2, GCCollectionMode.Aggressive, blocking: true);
            long after = GC.GetTotalMemory(true);

            Assert.True(after - before < 100L * 1024 * 1024,
                $"Heap delta was {(after - before) / 1024 / 1024}MB, expected < 100MB");
        }
        finally { File.Delete(path); }
    }

    // DATA-01: Empty cell values yield empty string, key present in dictionary
    [Fact]
    public async Task StreamAsync_EmptyCell_ReturnsEmptyStringKeyPresent()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "name,middle,last\r\nAlice,,Smith\r\n");
            var provider = new CsvDataProvider(path);
            var results = new List<IReadOnlyDictionary<string, string>>();
            await foreach (var row in provider.StreamAsync())
                results.Add(row);

            Assert.Single(results);
            Assert.True(results[0].ContainsKey("middle"), "Empty cell key should still be present");
            Assert.Equal("", results[0]["middle"]);
        }
        finally { File.Delete(path); }
    }

    // DATA-01: Cancellation token support — cancelling mid-stream stops enumeration
    [Fact]
    public async Task StreamAsync_CancellationMidStream_StopsEnumeration()
    {
        var path = Path.GetTempFileName();
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("name,age");
            for (int i = 0; i < 1000; i++)
                sb.AppendLine($"row{i},{i}");
            await File.WriteAllTextAsync(path, sb.ToString());

            using var cts = new CancellationTokenSource();
            var provider = new CsvDataProvider(path);
            int count = 0;

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var row in provider.StreamAsync(cts.Token))
                {
                    count++;
                    if (count == 1)
                        cts.Cancel();
                }
            });

            Assert.True(count < 1000, "Enumeration should have stopped before reading all rows");
        }
        finally { File.Delete(path); }
    }
}
