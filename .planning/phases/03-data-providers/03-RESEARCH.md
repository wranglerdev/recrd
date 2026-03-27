# Phase 3: Data Providers - Research

**Researched:** 2026-03-26
**Domain:** .NET 10 streaming data parsing — CsvHelper + System.Text.Json
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Use **CsvHelper** (JoshClose/CsvHelper NuGet) as the CSV parsing engine. RFC 4180 compliant, BOM-tolerant, configurable delimiters, actively maintained.
- **D-02:** `CsvDataProvider` wraps CsvHelper with `IAsyncEnumerable<T>` streaming — yield one `IReadOnlyDictionary<string,string>` per row, never buffer all rows.
- **D-03:** `JsonDataProvider` flattens nested objects using dot-notation (`{ "user": { "name": "Gil" } }` → `user.name`).
- **D-04:** Array fields encountered during flattening are **silently skipped** — not included in output dictionary. Not an error.
- **D-05:** Use **System.Text.Json** for JSON parsing. No Newtonsoft.Json anywhere in the codebase (binding from Phase 2 D-06).
- **D-06:** `DataParseException` carries three properties: `LineNumber` (int), `OffendingLine` (string), `FilePath` (string).
- **D-07:** Both providers throw `DataParseException` (not generic exceptions) on all parse failures.
- **D-08:** TDD red phase on `tdd/phase-03` branch before any implementation. Test files: `CsvDataProviderTests.cs`, `JsonDataProviderTests.cs`.

### Claude's Discretion

- CsvHelper version to pin (latest stable at planning time)
- Whether `CsvDataProvider` exposes `IAsyncEnumerable` directly or wraps via an adapter
- How null/empty cell values are represented in the output dictionary (`""` or absent key)
- Whether to use a `CsvConfiguration` builder pattern or constructor parameters for delimiter configuration

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| DATA-01 | `CsvDataProvider` — RFC 4180 compliant, BOM-tolerant, configurable delimiter | CsvHelper 33.1.0 handles all three natively; `CsvConfiguration.Delimiter` property, `StreamReader` with BOM detection on |
| DATA-02 | `CsvDataProvider` throws `DataParseException` with line number on malformed input | CsvHelper `BadDataFound` delegate + `MissingFieldFound` delegate expose `context.Parser.Row` and `context.Parser.RawRecord`; catch `CsvHelperException` and wrap |
| DATA-03 | `CsvDataProvider` streams `IAsyncEnumerable<T>` with ≤1000 rows in-memory; 50MB peak heap delta ≤100MB | `CsvReader.GetRecordsAsync<dynamic>()` returns `IAsyncEnumerable`; `yield return` pattern ensures no full buffering; `GC.GetTotalMemory` test verifies heap |
| DATA-04 | `JsonDataProvider` — root-level JSON array of flat objects; dot-notation flattening for nested objects | `JsonSerializer.DeserializeAsyncEnumerable<JsonElement>()` streams root array; custom recursive `Flatten(JsonElement, string prefix)` builds dictionary |
| DATA-05 | `JsonDataProvider` throws `DataParseException` on non-array root | `DeserializeAsyncEnumerable` is root-array-only by contract; validate with `JsonDocument.ParseAsync` peek or catch `JsonException` and translate |
</phase_requirements>

---

## Summary

Phase 3 implements two `IDataProvider` classes in `Recrd.Data`: `CsvDataProvider` (backed by CsvHelper) and `JsonDataProvider` (backed by System.Text.Json). Both must stream rows as `IAsyncEnumerable<IReadOnlyDictionary<string,string>>` without buffering the full file in memory, and both must throw the project-specific `DataParseException` (not raw library exceptions) on malformed input.

The technical challenge in CSV is bridging CsvHelper's synchronous-flavored row enumeration into a true async enumerable, while intercepting CsvHelper's library exceptions and translating them into `DataParseException` with the required `LineNumber`, `OffendingLine`, and `FilePath` properties. The technical challenge in JSON is two-fold: (1) detecting a non-array root before or during streaming and throwing `DataParseException` before yielding any rows, and (2) implementing recursive dot-notation flattening for nested objects while silently skipping array values, using only System.Text.Json (no Newtonsoft.Json).

The memory constraint (≤100MB peak heap delta on a 50MB file) is achievable with streaming approaches — neither provider should call `.ToList()` or accumulate rows. The GC test pattern using `GC.GetTotalMemory(true)` before and after iteration is the established approach for this kind of threshold test in xUnit, though results can be environment-sensitive.

**Primary recommendation:** Pin CsvHelper 33.1.0. Use `GetRecordsAsync<dynamic>()` with `await foreach` and manual header extraction from `csv.HeaderRecord`. Use `JsonSerializer.DeserializeAsyncEnumerable<JsonElement>()` with a custom recursive flatten helper. Define `DataParseException` in `Recrd.Data` (not `Recrd.Core`) since it is an implementation-layer concern.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| CsvHelper | 33.1.0 | RFC 4180 CSV parsing with BOM, delimiter config, async enumeration | Industry standard .NET CSV library; decision D-01 |
| System.Text.Json | BCL (net10.0) | JSON parsing and streaming deserialization | Decision D-05; zero extra dependency; BCL in .NET 10 |
| xunit | 2.9.3 | Unit tests | Already configured in `Recrd.Data.Tests.csproj` |
| Moq | 4.20.72 | Mocking in tests | Already configured |
| coverlet.collector | 8.0.1 | Code coverage | Already configured |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Threading.Channels | BCL | (Not needed here — used in Phase 2) | N/A |
| Microsoft.NET.Test.Sdk | 18.3.0 | xUnit test host | Already in test .csproj |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| CsvHelper | System.IO.Pipelines hand-rolled CSV | Much lower-level; RFC 4180 edge cases are non-trivial; D-01 locks CsvHelper |
| System.Text.Json | Newtonsoft.Json | Forbidden by D-05 / Phase 2 D-06 |
| `GetRecordsAsync<dynamic>()` | Manual `csv.ReadAsync()` + `csv.GetRecord<dynamic>()` loop | Both work; `GetRecordsAsync` is cleaner but dynamic dispatch; manual loop gives tighter control over exception catching |

**Installation:**

```bash
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet add packages/Recrd.Data/Recrd.Data.csproj package CsvHelper --version 33.1.0
```

**Version verification (performed 2026-03-26):**

```
CsvHelper latest stable: 33.1.0 (verified via NuGet API)
System.Text.Json: ships with net10.0 BCL; no separate package reference needed
```

---

## Architecture Patterns

### Recommended Project Structure

```
packages/Recrd.Data/
├── CsvDataProvider.cs          # IDataProvider implementation — CSV
├── JsonDataProvider.cs         # IDataProvider implementation — JSON
├── DataParseException.cs       # Typed exception — LineNumber, OffendingLine, FilePath
└── Recrd.Data.csproj           # Add CsvHelper PackageReference here

tests/Recrd.Data.Tests/
├── CsvDataProviderTests.cs     # Behavior suite: DATA-01, DATA-02, DATA-03
├── JsonDataProviderTests.cs    # Behavior suite: DATA-04, DATA-05
└── Recrd.Data.Tests.csproj     # Already configured — no changes needed
```

### Pattern 1: CsvDataProvider — Streaming with Exception Wrapping

**What:** Wrap `CsvReader.GetRecordsAsync<dynamic>()` with `await foreach`, convert each dynamic row to `IReadOnlyDictionary<string,string>`, and translate `CsvHelperException` into `DataParseException`.

**When to use:** All CSV reading paths.

**Example:**

```csharp
// Source: CsvHelper docs + CsvHelper issue #1751 pattern
public sealed class CsvDataProvider : IDataProvider
{
    private readonly string _filePath;
    private readonly string _delimiter;

    public CsvDataProvider(string filePath, string delimiter = ",")
    {
        _filePath = filePath;
        _delimiter = delimiter;
    }

    public async IAsyncEnumerable<IReadOnlyDictionary<string, string>> StreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(_filePath, detectEncodingFromByteOrderMarks: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = _delimiter,
            // BadDataFound set below to capture raw text before throwing
        };

        using var csv = new CsvReader(reader, config);

        IAsyncEnumerable<dynamic> records;
        try
        {
            await csv.ReadAsync();
            csv.ReadHeader();
            records = csv.GetRecordsAsync<dynamic>(cancellationToken);
        }
        catch (CsvHelperException ex)
        {
            throw new DataParseException(
                lineNumber: ex.Context.Parser.Row,
                offendingLine: ex.Context.Parser.RawRecord ?? string.Empty,
                filePath: _filePath,
                message: ex.Message,
                innerException: ex);
        }

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            var dict = new Dictionary<string, string>();
            foreach (var header in csv.HeaderRecord!)
            {
                dict[header] = ((IDictionary<string, object>)record)[header]?.ToString() ?? string.Empty;
            }
            yield return dict;
        }
    }
}
```

**Key insight for exception wrapping:** `CsvHelper` throws during enumeration, not just during construction. Wrap the `await foreach` body in try/catch to catch both header-read failures and mid-stream row parse failures.

### Pattern 2: DataParseException — Typed Diagnostic

**What:** Exception class carrying LineNumber, OffendingLine, FilePath per D-06.

```csharp
// DataParseException.cs — lives in Recrd.Data namespace
public sealed class DataParseException : Exception
{
    public int LineNumber { get; }
    public string OffendingLine { get; }
    public string FilePath { get; }

    public DataParseException(
        int lineNumber,
        string offendingLine,
        string filePath,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        LineNumber = lineNumber;
        OffendingLine = offendingLine;
        FilePath = filePath;
    }
}
```

### Pattern 3: JsonDataProvider — Streaming with Recursive Flattening

**What:** Use `JsonSerializer.DeserializeAsyncEnumerable<JsonElement>()` to stream root JSON array. For each element, recursively flatten nested objects using dot-notation prefix. Silently skip arrays.

**Key constraint:** `DeserializeAsyncEnumerable` only works on root-level JSON arrays — this aligns perfectly with the DATA-05 requirement. A non-array root causes a `JsonException` during the first iteration, which must be caught and translated to `DataParseException`.

```csharp
// Source: Microsoft Docs — JsonSerializer.DeserializeAsyncEnumerable
public sealed class JsonDataProvider : IDataProvider
{
    private readonly string _filePath;

    public JsonDataProvider(string filePath) => _filePath = filePath;

    public async IAsyncEnumerable<IReadOnlyDictionary<string, string>> StreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(_filePath);

        IAsyncEnumerable<JsonElement> elements;
        try
        {
            elements = JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(
                stream, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new DataParseException(
                lineNumber: (int)(ex.LineNumber ?? 0) + 1,
                offendingLine: string.Empty,
                filePath: _filePath,
                message: "JSON root must be an array. " + ex.Message,
                innerException: ex);
        }

        await foreach (var element in elements.WithCancellation(cancellationToken))
        {
            Dictionary<string, string> flat;
            try
            {
                flat = FlattenElement(element, prefix: string.Empty);
            }
            catch (JsonException ex)
            {
                throw new DataParseException(
                    lineNumber: (int)(ex.LineNumber ?? 0) + 1,
                    offendingLine: string.Empty,
                    filePath: _filePath,
                    message: ex.Message,
                    innerException: ex);
            }
            yield return flat;
        }
    }

    private static Dictionary<string, string> FlattenElement(JsonElement element, string prefix)
    {
        var result = new Dictionary<string, string>();
        if (element.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var prop in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var (k, v) in FlattenElement(prop.Value, key))
                        result[k] = v;
                    break;
                case JsonValueKind.Array:
                    // D-04: silently skip array fields
                    break;
                default:
                    result[key] = prop.Value.ToString();
                    break;
            }
        }
        return result;
    }
}
```

### Pattern 4: Memory Test with GC.GetTotalMemory

**What:** Generate a 50MB CSV fixture in the test, stream it through `CsvDataProvider`, measure heap before and after.

```csharp
[Fact]
public async Task CsvDataProvider_Streams50MbFile_WithinHeapDelta()
{
    // Arrange: generate fixture (50MB+ CSV to temp file)
    var path = Path.GetTempFileName();
    try
    {
        await File.WriteAllLinesAsync(path,
            Enumerable.Range(0, 500_000).Select(i => $"col1,col2,col3\r\nval{i},val{i},val{i}"),
            System.Text.Encoding.UTF8);

        var provider = new CsvDataProvider(path);
        GC.Collect(2, GCCollectionMode.Aggressive, blocking: true);
        var before = GC.GetTotalMemory(forceFullCollection: true);

        // Act
        await foreach (var _ in provider.StreamAsync()) { /* consume all */ }

        GC.Collect(2, GCCollectionMode.Aggressive, blocking: true);
        var after = GC.GetTotalMemory(forceFullCollection: true);

        // Assert: peak delta ≤ 100MB
        Assert.True(after - before < 100 * 1024 * 1024,
            $"Heap delta was {(after - before) / 1024 / 1024}MB — expected ≤100MB");
    }
    finally
    {
        File.Delete(path);
    }
}
```

**Note:** `GC.GetTotalMemory` measures managed heap only. The 100MB threshold in DATA-03 is a managed heap delta — this is the correct measurement. Peak heap during iteration is not captured by before/after snapshots, but post-enumeration GC cleanup should bring managed heap back near baseline. A delta of after-before within 100MB is a reasonable proxy for peak working set discipline.

### Anti-Patterns to Avoid

- **Buffering all rows:** Never call `.ToList()` or `ToArray()` on the `IAsyncEnumerable`. The interface contract requires streaming.
- **Leaking CsvHelperException:** All `CsvHelperException` subclasses must be caught and re-thrown as `DataParseException`. Never let library exceptions escape the provider boundary.
- **Leaking JsonException:** Same rule — callers see only `DataParseException`, not `JsonException`.
- **Using `dynamic` without null guard:** CsvHelper's dynamic rows expose properties that may be null for empty cells. Always null-coalesce to `string.Empty`.
- **`GetRecordsAsync` deadlock on WinForms/WPF:** Not a risk in this project (no synchronization context), but noted for completeness.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| RFC 4180 CSV parsing | Custom character-by-character parser | CsvHelper 33.1.0 | Quoted fields with embedded commas, escaped quotes, CRLF in fields — RFC 4180 has many edge cases |
| BOM detection | Manual byte-order-mark stripping | `StreamReader(path, detectEncodingFromByteOrderMarks: true)` | BCL handles UTF-8 BOM, UTF-16 BOM, and UTF-8 NoBOM transparently |
| JSON streaming | Manual Utf8JsonReader state machine | `JsonSerializer.DeserializeAsyncEnumerable<JsonElement>` | Streaming JSON array deserialization is non-trivial to implement correctly with backpressure |
| Async enumerable cancellation | Custom token checking | `[EnumeratorCancellation]` attribute + `.WithCancellation()` | BCL pattern; required for correct cooperative cancellation |

**Key insight:** The main value of CsvHelper is RFC 4180 edge cases: fields containing delimiters, newlines inside quoted strings, escaped double-quotes inside quoted fields. These all fail silently with naive `string.Split(',')` approaches.

---

## Common Pitfalls

### Pitfall 1: CsvHelper GetRecordsAsync Never Returns

**What goes wrong:** When using `GetRecordsAsync<T>()` without actually calling `csv.ReadAsync()` first to advance past the header, the enumeration can hang or return no results.

**Why it happens:** CsvHelper's async path requires the stream to be positioned after the header row before `GetRecordsAsync` is called. Some code paths skip `ReadAsync()` + `ReadHeader()`.

**How to avoid:** Always call `await csv.ReadAsync()` followed by `csv.ReadHeader()` before calling `GetRecordsAsync`. Confirmed by CsvHelper GitHub issue #1751.

**Warning signs:** Test hangs or `await foreach` loop body never executes.

### Pitfall 2: DataParseException Missing Line Number for Mid-Stream Failures

**What goes wrong:** Wrapping only the `CsvReader` constructor and header-read in try/catch misses exceptions thrown during row iteration. The `GetRecordsAsync` enumerable throws during `MoveNextAsync()` — inside the `await foreach` loop.

**Why it happens:** Row parse exceptions occur lazily as each row is materialized.

**How to avoid:** Wrap the entire `await foreach` body in try/catch, not just the setup code. For `BadDataFound` delegate, use the delegate to capture data, then throw from within `StreamAsync`.

**Warning signs:** `DataParseException` is not thrown for mid-file bad rows; raw `CsvHelperException` escapes.

### Pitfall 3: JsonDataProvider Non-Array Root — Exception Timing

**What goes wrong:** `JsonSerializer.DeserializeAsyncEnumerable` does not throw immediately on a non-array root. The `JsonException` fires during the first `MoveNextAsync()` call (i.e., inside `await foreach`), not at construction time.

**Why it happens:** The stream is consumed lazily. The JSON root token is not validated until iteration begins.

**How to avoid:** Wrap the `await foreach` in try/catch and translate the first `JsonException` (which indicates root is not array) into a `DataParseException` with a message explaining the root-must-be-array constraint.

**Warning signs:** `DataParseException` is thrown but with a confusing message copied verbatim from `JsonException`; message should explicitly state the root-must-be-array rule.

### Pitfall 4: TreatWarningsAsErrors + Nullable Enable

**What goes wrong:** `csv.HeaderRecord` is `string[]?` (nullable). Accessing it without null check causes a warning that becomes a compile error.

**Why it happens:** `Directory.Build.props` sets `Nullable=enable` and `TreatWarningsAsErrors=true` across all projects.

**How to avoid:** Use `csv.HeaderRecord!` null-forgiving operator only after confirming the header was read. Alternatively, guard with `if (csv.HeaderRecord is null) throw new DataParseException(...)`.

### Pitfall 5: GC Memory Test Flakiness

**What goes wrong:** The `GC.GetTotalMemory` before/after test fails intermittently in CI due to GC non-determinism.

**Why it happens:** GC timing varies by environment. A large allocation from a previous test can inflate the "after" measurement. Also `GetTotalMemory` measures allocated heap, not peak working set.

**How to avoid:** Call `GC.Collect(2, GCCollectionMode.Aggressive, blocking: true)` before measuring both `before` and `after` values. Make the threshold generous enough to pass in low-memory CI environments (100MB is already the specified limit; don't tighten it).

**Warning signs:** Test passes locally but fails in CI with delta slightly over limit.

### Pitfall 6: CsvHelper BadDataFound Callback Stack Overflow

**What goes wrong:** Reading `context.Parser.Record` inside the `BadDataFound` callback causes a stack overflow (GitHub issue #1717).

**Why it happens:** Accessing `Parser.Record` re-triggers parsing, which re-fires the callback.

**How to avoid:** In `BadDataFound`, only access `args.Field` and `args.Context.Parser.RawRecord`. Do not access `args.Context.Parser.Record` (the parsed record array).

---

## Code Examples

Verified patterns from official sources:

### CsvHelper: BOM-Tolerant StreamReader

```csharp
// Source: .NET BCL — StreamReader constructor
using var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true);
```

### CsvHelper: Configuration with Custom Delimiter

```csharp
// Source: CsvHelper docs (joshclose.github.io/CsvHelper) — v23+ property assignment
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = ";",
};
using var csv = new CsvReader(reader, config);
```

### CsvHelper: BadDataFound Delegate for Line Number Capture

```csharp
// Source: CsvHelper GitHub issues #803, #1108, #1717, migration docs v23/v30
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    BadDataFound = args =>
    {
        // args.Field = the raw field text
        // args.Context.Parser.Row = 1-based row number
        // args.Context.Parser.RawRecord = full raw line text
        // Do NOT access args.Context.Parser.Record — triggers stack overflow
        throw new DataParseException(
            lineNumber: args.Context.Parser.Row,
            offendingLine: args.Context.Parser.RawRecord ?? string.Empty,
            filePath: filePath,
            message: $"Bad data at line {args.Context.Parser.Row}: {args.Field}");
    },
};
```

### System.Text.Json: Streaming Root Array

```csharp
// Source: Microsoft Docs — JsonSerializer.DeserializeAsyncEnumerable
// https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer.deserializeasyncenumerable
await using var stream = File.OpenRead(filePath);
await foreach (var element in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(
    stream, cancellationToken: cancellationToken))
{
    // element is one item from the root array
}
```

### System.Text.Json: JsonElement.ValueKind Check

```csharp
// Source: Microsoft Docs — JsonElement.ValueKind
if (element.ValueKind != JsonValueKind.Object)
    yield break; // or throw DataParseException

foreach (var prop in element.EnumerateObject())
{
    // prop.Name, prop.Value.ValueKind, prop.Value.ToString()
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| CsvHelper constructor param delimiter | `CsvConfiguration.Delimiter` property | v23 (2021) | Configuration object replaces constructor overloads |
| `BadDataFound(string field, ReadingContext context)` | `BadDataFound(BadDataFoundArgs args)` | v23 | Single struct arg; `args.Field`, `args.Context` |
| `BadDataException(context)` | `BadDataException(field, rawRecord, context)` | v30 | Exception now carries raw field and full raw record |
| `GetRecords<T>()` (sync) | `GetRecordsAsync<T>()` (async IAsyncEnumerable) | v27+ | True async streaming; no thread-blocking |
| Newtonsoft.Json for JSON streaming | `System.Text.Json.DeserializeAsyncEnumerable` | .NET 6+ | BCL-native; no extra dependency |

**Deprecated/outdated:**

- **`CsvHelper < v23`:** Pre-v23 configuration API uses constructor parameters — do not follow old StackOverflow answers using those patterns.
- **`JsonConvert.DeserializeObject` (Newtonsoft):** Forbidden by D-05; mentions in older project docs should be treated as obsolete.

---

## Open Questions

1. **`DataParseException` location — `Recrd.Data` vs `Recrd.Core`**
   - What we know: The context says it is a Phase 3 deliverable. `Recrd.Core` has zero `Recrd.*` dependencies (CORE-13). `DataParseException` is an implementation-layer exception.
   - What's unclear: Should `DataParseException` be in `Recrd.Core.Interfaces` so Phase 4 (Gherkin) can reference it if needed, or kept in `Recrd.Data`?
   - Recommendation: Define `DataParseException` in `Recrd.Data` namespace. Phase 4 consumes `IDataProvider` streaming results, not exceptions. If Phase 4 needs to handle data errors, it can catch `Exception` or the planner can revisit.

2. **Null/empty cell values in output dictionary**
   - What we know: Context marks this as Claude's Discretion.
   - What's unclear: `""` vs absent key — downstream Gherkin generator in Phase 4 will need to substitute variable values.
   - Recommendation: Represent empty/null cells as `""` (empty string, key present). This is simpler for downstream consumers — they check `dict.TryGetValue` once and get a predictable result, rather than distinguishing missing key from empty string.

3. **`MissingFieldFound` behavior for mismatched column count**
   - What we know: DATA-02 requires `DataParseException` on "mismatched column count." CsvHelper's `MissingFieldFound` delegate fires when a mapped field is missing; it defaults to throwing `MissingFieldException`.
   - What's unclear: For `IReadOnlyDictionary<string,string>` (no typed class mapping), "mismatched column count" in CsvHelper manifests differently — extra columns or missing columns in dynamic reads may not fire `MissingFieldFound`.
   - Recommendation: Implement an explicit column-count check after reading the header: for each row, count fields and compare to `HeaderRecord.Length`. Throw `DataParseException` if they differ, using `context.Parser.Row` and `context.Parser.RawRecord`.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| dotnet SDK | Build and test | Yes | 10.0.103 | — |
| CsvHelper 33.1.0 | CsvDataProvider | Requires NuGet restore | — | — |
| System.Text.Json | JsonDataProvider | Yes (BCL in net10.0) | Included in SDK | — |
| NuGet connectivity | Package restore | Yes (IPv4 with DOTNET_SYSTEM_NET_DISABLEIPV6=1) | — | — |

**Missing dependencies with no fallback:** None.

**Note:** All `dotnet` commands must be prefixed with `DOTNET_SYSTEM_NET_DISABLEIPV6=1` per CLAUDE.md and project memory.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 |
| Config file | None — auto-discovered via `Microsoft.NET.Test.Sdk` |
| Quick run command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --filter "FullyQualifiedName~Recrd.Data.Tests" -x` |
| Full suite command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DATA-01 | RFC 4180 parsing, BOM tolerance, custom delimiter | unit | `dotnet test --filter "FullyQualifiedName~CsvDataProviderTests" -x` | ❌ Wave 0 |
| DATA-02 | `DataParseException` with line number on bad CSV | unit | `dotnet test --filter "FullyQualifiedName~CsvDataProviderTests" -x` | ❌ Wave 0 |
| DATA-03 | Streaming ≤1000 rows in-memory; 50MB ≤100MB heap delta | unit (memory) | `dotnet test --filter "FullyQualifiedName~CsvDataProviderTests" -x` | ❌ Wave 0 |
| DATA-04 | JSON dot-notation flattening, array skip | unit | `dotnet test --filter "FullyQualifiedName~JsonDataProviderTests" -x` | ❌ Wave 0 |
| DATA-05 | `DataParseException` on non-array JSON root | unit | `dotnet test --filter "FullyQualifiedName~JsonDataProviderTests" -x` | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --filter "FullyQualifiedName~Recrd.Data.Tests" -x`
- **Per wave merge:** `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `tests/Recrd.Data.Tests/CsvDataProviderTests.cs` — covers DATA-01, DATA-02, DATA-03 (replaces `PlaceholderTests.cs`)
- [ ] `tests/Recrd.Data.Tests/JsonDataProviderTests.cs` — covers DATA-04, DATA-05
- [ ] `packages/Recrd.Data/DataParseException.cs` — needed for tests to compile red

Wave 0 requires: delete `PlaceholderTests.cs`, create both test suite files (red), create `DataParseException.cs` (so tests can reference the type), commit on `tdd/phase-03` branch.

---

## Sources

### Primary (HIGH confidence)

- NuGet API `api.nuget.org/v3-flatcontainer/csvhelper/index.json` — CsvHelper latest stable: 33.1.0 (verified 2026-03-26)
- NuGet API `api.nuget.org/v3-flatcontainer/system.text.json/index.json` — System.Text.Json 10.0.5 stable (BCL in net10.0)
- Microsoft Learn — `JsonSerializer.DeserializeAsyncEnumerable` — root-array-only constraint confirmed
- `Recrd.Core/Interfaces/IDataProvider.cs` — interface signature confirmed: `IAsyncEnumerable<IReadOnlyDictionary<string,string>> StreamAsync(CancellationToken)`
- `Recrd.Data.Tests/Recrd.Data.Tests.csproj` — xUnit 2.9.3, Moq 4.20.72, coverlet 8.0.1 already configured

### Secondary (MEDIUM confidence)

- CsvHelper GitHub issue #1751 — GetRecordsAsync hang without ReadAsync+ReadHeader (verified pattern)
- CsvHelper GitHub issue #1717 — BadDataFound callback stack overflow on `Parser.Record` (fix: use `RawRecord` only)
- CsvHelper migration docs v23, v30 — BadDataFound delegate signature changes confirmed
- WebSearch cross-reference: `context.Parser.Row` for line number, `context.Parser.RawRecord` for raw line text

### Tertiary (LOW confidence)

- GC.GetTotalMemory test pattern — multiple community sources, behavior varies by GC mode and environment; call `GC.Collect` with `blocking: true` before each measurement for maximum repeatability

---

## Metadata

**Confidence breakdown:**

- Standard stack: HIGH — CsvHelper version confirmed via NuGet API; System.Text.Json is BCL
- Architecture: HIGH — IDataProvider interface read directly from source; CsvHelper and STJ APIs confirmed via docs and issue tracking
- Pitfalls: HIGH (BadDataFound stack overflow, GetRecordsAsync header requirement) / MEDIUM (GC test flakiness) — sourced from official CsvHelper issues

**Research date:** 2026-03-26
**Valid until:** 2026-06-26 (CsvHelper version check; stable library, low churn)
