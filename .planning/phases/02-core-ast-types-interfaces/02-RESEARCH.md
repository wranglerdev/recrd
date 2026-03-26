# Phase 2: Core AST Types & Interfaces - Research

**Researched:** 2026-03-26
**Domain:** .NET 10 C# Records, System.Text.Json polymorphic source generation, System.Threading.Channels
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** AST step types (`ActionStep`, `AssertionStep`, `GroupStep`) are sealed C# **records** with init-only properties — immutable, structural equality, with-expressions for transformation, clean pattern matching. Not classes or abstract hierarchies.
- **D-02:** `ActionStep` uses a single record with an `ActionType` enum (`Click`, `Type`, `Select`, `Navigate`, `Upload`, `DragDrop`) plus a `Payload` dictionary for subtype-specific data. No separate ClickStep/TypeStep etc. types.
- **D-03:** `AssertionStep` uses a single record with an `AssertionType` enum (`TextEquals`, `TextContains`, `Visible`, `Enabled`, `UrlMatches`) plus a `Payload` dictionary. Same flat pattern as ActionStep.
- **D-04:** `GroupStep` is a sealed record with a `GroupType` enum (`Given`, `When`, `Then`) and a `Steps` collection of child steps.
- **D-05:** `Session`, `Selector`, `Variable`, `RecordedEvent` are also sealed records where applicable.
- **D-06:** Use **System.Text.Json** only — no Newtonsoft.Json. Keeps `Recrd.Core` dependency-free beyond the .NET 10 BCL.
- **D-07:** Polymorphic step serialization uses **`[JsonPolymorphic]` + `[JsonDerivedType]` attributes** on the step base type (or `IStep` interface). Emits a `$type` discriminator automatically. No hand-written JsonConverter for polymorphism.
- **D-08:** Use **source-generated `JsonSerializerContext`** (`[JsonSourceGenerationOptions]`) for AOT and self-contained single-file publish compatibility.
- **D-09:** Expose the pipeline as a **thin wrapper class** over `System.Threading.Channels.Channel<RecordedEvent>`. Wrapper exposes explicit `WriteAsync`, `ReadAllAsync`, `Complete`, and `Cancel` surface. Extract an interface to make it mockable in tests.
- **D-10:** Underlying channel is **bounded** (`BoundedChannel<RecordedEvent>`) with a configurable capacity (default 1000). Capacity is constructor-injectable so tests can use small values. Provides natural backpressure — `WriteAsync` awaits if full.
- **D-11:** Tests in `Recrd.Core.Tests` organized into behavior-suite files: `SessionSerializationTests.cs`, `StepModelTests.cs`, `SelectorVariableTests.cs`, `ChannelPipelineTests.cs`, `InterfaceContractTests.cs`.
- **D-12:** Subtype coverage uses **`[Theory]` + `[MemberData]`** (or `TheoryData<T>`) providing one test case per `ActionType`/`AssertionType` enum value.
- **D-13:** **All test files committed red in one atomic commit** on a `tdd/phase-02` branch prefix before implementation. CI-06 tolerates test failures on that branch prefix.

### Claude's Discretion

- Exact wrapper class name (`RecordingChannel`, `EventPipeline`, or similar)
- Exact interface name for the channel wrapper (`IRecordingChannel`, etc.)
- JSON property naming convention (camelCase vs PascalCase for the `.recrd` file)
- How `Selector` priority array is represented in the record (ranked `IReadOnlyList<SelectorStrategy>` or `SelectorType` + fallback chain)
- Whether `Variable` name validation throws at record constructor or via a static factory method
- `Payload` dictionary key/value types (`Dictionary<string, string>` vs `Dictionary<string, object?>`)

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CORE-01 | `Session` AST root with `metadata`, `variables`, and `steps` fields serializable to/from JSON with `schemaVersion: 1` | System.Text.Json source generation; `[JsonSerializable(typeof(Session))]` with metadata mode |
| CORE-02 | `ActionStep` covering click, type, select, navigate, upload, drag-and-drop | Sealed record + `ActionType` enum + `Payload` dict; `[JsonDerivedType]` discriminator |
| CORE-03 | `AssertionStep` covering text-equals, text-contains, visible, enabled, URL-matches | Same flat pattern; `[JsonDerivedType]` discriminator |
| CORE-04 | `GroupStep` with given/when/then type, containing child steps | Sealed record with recursive `IReadOnlyList<IStep>` |
| CORE-05 | `Selector` with type and ranked priority array | Sealed record; `SelectorStrategy` enum + `IReadOnlyList<SelectorStrategy>` |
| CORE-06 | `Variable` with validated name (`^[a-z][a-z0-9_]{0,63}$`), linked step reference | Constructor validation via `ArgumentException` (with `Nullable enable` / `TreatWarningsAsErrors`) |
| CORE-07 | `ITestCompiler` interface: `TargetName`, `CompileAsync(Session, CompilerOptions) → CompilationResult` | Pure C# interface; companion result records in `Recrd.Core` |
| CORE-08 | `IDataProvider` interface: `IAsyncEnumerable<IReadOnlyDictionary<string,string>> StreamAsync()` | Built-in `IAsyncEnumerable<T>` — no external dependency |
| CORE-09 | `IEventInterceptor` plugin extension point interface | Minimal interface definition |
| CORE-10 | `IAssertionProvider` plugin extension point interface | Minimal interface definition |
| CORE-11 | `Channel<RecordedEvent>` pipeline with backpressure and cancellation | `System.Threading.Channels.Channel.CreateBounded<T>()` — BCL, no NuGet needed |
| CORE-12 | `RecordedEvent` envelope with Id, TimestampMs, EventType, Selectors, Payload, DataVariable | Sealed record; `Payload` as `IReadOnlyDictionary<string, string>` |
| CORE-13 | `Recrd.Core` has zero `Recrd.*` package dependencies | `Directory.Build.props` enforces `Nullable enable`/`TreatWarningsAsErrors`; only BCL + `System.Text.Json` |
</phase_requirements>

---

## Summary

Phase 2 creates the foundational library `Recrd.Core` — the single source of truth for all AST types, interfaces, and the recording pipeline. Every other package in the monorepo will depend on it, so correctness and zero external dependencies are the primary constraints.

The technology surface is entirely within the .NET 10 BCL: sealed C# records with init-only properties, System.Text.Json with `[JsonPolymorphic]`/`[JsonDerivedType]` attribute-based polymorphism and source-generated `JsonSerializerContext`, and `System.Threading.Channels.Channel.CreateBounded<T>()` for backpressure-aware event streaming. No NuGet packages are required in `Recrd.Core.csproj` beyond what .NET 10 ships.

The dominant design challenge is the polymorphic step hierarchy: `Session.steps` is a heterogeneous list of `IStep` (or a base record), and round-trip JSON fidelity requires a `$type` discriminator that is known at compile time. Source generation supports metadata mode for polymorphism — critical for self-contained single-file AOT publish (DIST-01). The key constraint is that polymorphism is supported in **metadata-based** source generation but **not** in fast-path source generation, which constrains how `JsonSerializerContext` must be configured.

**Primary recommendation:** Use `[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]` + `[JsonDerivedType]` on the `IStep` interface or base record; use `[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]` on the `RecrdJsonContext` class. Use `TheoryData<ActionType>` (xUnit 2.9 generic strongly-typed overload) for enum-driven theory tests.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `System.Text.Json` | BCL (.NET 10) | JSON serialization/deserialization | Built into BCL, AOT-compatible, no NuGet, mandated by D-06 |
| `System.Threading.Channels` | BCL (.NET 10) | Producer/consumer pipeline with backpressure | Built into BCL since .NET Core 3.0, no NuGet required |
| `xunit` | 2.9.3 | Unit test framework | Already in `Recrd.Core.Tests.csproj` |
| `Moq` | 4.20.72 | Mock interface implementations in tests | Already in `Recrd.Core.Tests.csproj` |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `System.Text.RegularExpressions` | BCL (.NET 10) | `Variable` name validation regex | `[GeneratedRegex]` source-gen for zero-allocation match |
| `coverlet.collector` | 8.0.1 | Code coverage collection | Already configured in test project |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `System.Text.Json` | `Newtonsoft.Json` | Newtonsoft is faster to configure but adds a NuGet dep — prohibited by D-06 |
| Attribute-based polymorphism | Custom `JsonConverter<IStep>` | Custom converter gives more control but violates D-07 (no hand-written converter for polymorphism) |
| `TheoryData<T>` | `IEnumerable<object[]>` via `[MemberData]` | `IEnumerable<object[]>` loses compile-time type safety; `TheoryData<T>` is the xUnit v2/v3 recommendation |

**Installation:** No new packages required for `Recrd.Core`. The test project already has all dependencies. For `Recrd.Core.csproj`, only BCL types are used.

**Version verification:** Confirmed against installed packages via `dotnet list package`:
- `xunit` 2.9.3 (resolved)
- `Moq` 4.20.72 (resolved)
- `Microsoft.NET.Test.Sdk` 18.3.0 (resolved)
- `coverlet.collector` 8.0.1 (resolved)
- .NET SDK: 10.0.103 (confirmed via `dotnet --version`)

---

## Architecture Patterns

### Recommended Project Structure

```
packages/Recrd.Core/
├── Ast/
│   ├── Session.cs              # Root AST record
│   ├── SessionMetadata.cs      # Metadata sub-record
│   ├── IStep.cs                # Step marker interface (base for polymorphism)
│   ├── ActionStep.cs           # Sealed record + ActionType enum
│   ├── AssertionStep.cs        # Sealed record + AssertionType enum
│   ├── GroupStep.cs            # Sealed record + GroupType enum
│   ├── Selector.cs             # Sealed record + SelectorStrategy enum
│   └── Variable.cs             # Sealed record with name validation
├── Pipeline/
│   ├── RecordedEvent.cs        # Sealed record envelope
│   ├── EventType.cs            # Enum for event categories
│   ├── IRecordingChannel.cs    # Interface for testability
│   └── RecordingChannel.cs     # Thin BoundedChannel wrapper
├── Interfaces/
│   ├── ITestCompiler.cs        # + CompilerOptions + CompilationResult records
│   ├── IDataProvider.cs
│   ├── IEventInterceptor.cs
│   └── IAssertionProvider.cs
└── Serialization/
    └── RecrdJsonContext.cs     # JsonSerializerContext, metadata mode
```

```
tests/Recrd.Core.Tests/
├── SessionSerializationTests.cs   # CORE-01
├── StepModelTests.cs              # CORE-02, CORE-03, CORE-04
├── SelectorVariableTests.cs       # CORE-05, CORE-06
├── ChannelPipelineTests.cs        # CORE-11, CORE-12
└── InterfaceContractTests.cs      # CORE-07–CORE-10, CORE-13
```

### Pattern 1: Polymorphic Step Hierarchy with Source Generation

**What:** `IStep` interface decorated with `[JsonPolymorphic]` + `[JsonDerivedType]` for each concrete step record. `RecrdJsonContext` uses `GenerationMode.Metadata`.

**When to use:** Any time a polymorphic list (`Session.Steps`) must round-trip through JSON.

**Critical constraint:** Polymorphism is supported in **metadata-based** source generation only, not fast-path. Using `GenerationMode = JsonSourceGenerationMode.Metadata` (or the default both-modes) enables this.

**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism
// Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ActionStep), typeDiscriminator: "action")]
[JsonDerivedType(typeof(AssertionStep), typeDiscriminator: "assertion")]
[JsonDerivedType(typeof(GroupStep), typeDiscriminator: "group")]
public interface IStep { }

public sealed record ActionStep(
    ActionType ActionType,
    Selector Selector,
    IReadOnlyDictionary<string, string> Payload) : IStep;

// Source-gen context — metadata mode required for polymorphism
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(IStep))]       // Register interface explicitly
[JsonSerializable(typeof(ActionStep))]
[JsonSerializable(typeof(AssertionStep))]
[JsonSerializable(typeof(GroupStep))]
internal sealed partial class RecrdJsonContext : JsonSerializerContext { }
```

**Important:** `$type` discriminator must appear at the start of each step JSON object by default. If reading externally-written `.recrd` files with mid-object `$type`, set `AllowOutOfOrderMetadataProperties = true`.

### Pattern 2: Bounded Channel Wrapper

**What:** `RecordingChannel` wraps `Channel.CreateBounded<RecordedEvent>` and exposes a minimal interface for testability.

**When to use:** Recording engine (Phase 6) and inspector (Phase 6) communicate through this wrapper.

**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/core/extensions/channels

public interface IRecordingChannel
{
    ValueTask WriteAsync(RecordedEvent evt, CancellationToken ct = default);
    IAsyncEnumerable<RecordedEvent> ReadAllAsync(CancellationToken ct = default);
    void Complete();
    void Cancel(Exception? error = null);
}

public sealed class RecordingChannel : IRecordingChannel
{
    private readonly Channel<RecordedEvent> _channel;
    private readonly CancellationTokenSource _cts = new();

    public RecordingChannel(int capacity = 1000)
    {
        _channel = Channel.CreateBounded<RecordedEvent>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,  // backpressure
                SingleWriter = false,
                SingleReader = false
            });
    }

    public ValueTask WriteAsync(RecordedEvent evt, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(evt,
               CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token).Token);

    public IAsyncEnumerable<RecordedEvent> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);

    public void Complete() => _channel.Writer.Complete();
    public void Cancel(Exception? error = null) => _channel.Writer.Complete(error);
}
```

**Backpressure behavior:** `FullMode = BoundedChannelFullMode.Wait` means `WriteAsync` suspends until a slot opens — the only full-mode option that provides true backpressure.

### Pattern 3: Variable Name Validation

**What:** `Variable` record validates its `Name` property against `^[a-z][a-z0-9_]{0,63}$` at construction time. With `TreatWarningsAsErrors` + `Nullable enable`, the constructor must be explicit (not positional primary constructor) to throw `ArgumentException` cleanly.

**When to use:** Anywhere `Variable` is instantiated.

**Example:**
```csharp
// Regex in BCL; use [GeneratedRegex] for zero-allocation (compile-time source gen)
public sealed record Variable
{
    [GeneratedRegex(@"^[a-z][a-z0-9_]{0,63}$", RegexOptions.Compiled)]
    private static partial Regex NamePattern();

    public string Name { get; }
    public string? StepRef { get; init; }

    public Variable(string name, string? stepRef = null)
    {
        if (!NamePattern().IsMatch(name))
            throw new ArgumentException(
                $"Variable name '{name}' does not match ^[a-z][a-z0-9_]{{0,63}}$.",
                nameof(name));
        Name = name;
        StepRef = stepRef;
    }
}
```

**Alternative (static factory):** A `Variable.Create(string name)` static factory returning a `Variable` or throwing is equivalent; use whichever the planner chooses. Constructor approach is simpler.

### Pattern 4: xUnit Theory with TheoryData for Enum Coverage

**What:** `TheoryData<ActionType>` provides compile-time type-safe parameterized tests for all enum values.

**When to use:** `StepModelTests.cs` — one `[Theory]` per step type, exhausting all enum values.

**Example:**
```csharp
// Source: https://andrewlock.net/creating-strongly-typed-xunit-theory-test-data-with-theorydata/

public static TheoryData<ActionType> AllActionTypes =>
    new(Enum.GetValues<ActionType>());

[Theory]
[MemberData(nameof(AllActionTypes))]
public void ActionStep_IsConstructibleForAllSubtypes(ActionType actionType)
{
    var step = new ActionStep(actionType, SomeSelector, new Dictionary<string, string>());
    Assert.Equal(actionType, step.ActionType);
}
```

**Note on xUnit 2.9 (v2):** `TheoryData<T>` is available in xUnit v2. Use `Enum.GetValues<T>()` (generic, .NET 5+) for clean enum value enumeration.

### Pattern 5: Session JSON Round-Trip Structure

**What:** `Session` root record includes `schemaVersion: 1` as a required field. The `Steps` list is typed as `IReadOnlyList<IStep>` for polymorphism.

**Example (target JSON shape):**
```json
{
  "schemaVersion": 1,
  "metadata": {
    "id": "...",
    "createdAt": "2026-03-26T00:00:00Z",
    "browserEngine": "chromium",
    "viewportSize": { "width": 1280, "height": 720 },
    "baseUrl": "https://example.com"
  },
  "variables": [],
  "steps": [
    {
      "$type": "action",
      "actionType": "navigate",
      "selector": { ... },
      "payload": { "url": "https://example.com" }
    }
  ]
}
```

**Note:** `PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase` in `RecrdJsonContext` produces the camelCase keys shown above. This is the recommended convention for `.recrd` files (more conventional for JSON, aligns with standard ecosystem).

### Anti-Patterns to Avoid

- **Abstract class hierarchy for steps:** Violates D-01. Use sealed records + discriminated enum, not `abstract ActionStepBase : StepBase`.
- **Hand-written `JsonConverter<IStep>`:** Violates D-07. `[JsonPolymorphic]` + `[JsonDerivedType]` is the correct approach.
- **`JsonSourceGenerationMode.Serialization` (fast-path) only:** Fast-path does NOT support polymorphism. The context must use metadata mode (or default both-modes) for the polymorphic `IStep` hierarchy to serialize/deserialize correctly.
- **Unbounded channel:** Violates D-10. Always use `Channel.CreateBounded<T>()`.
- **`BoundedChannelFullMode.DropWrite` or `DropOldest`:** These modes silently discard events — not appropriate for the recording pipeline where every event matters. Use `FullMode = BoundedChannelFullMode.Wait`.
- **`Payload` as `Dictionary<string, object?>`:** Complicates source generation (requires registering many runtime types). Prefer `Dictionary<string, string>` (D-02, D-03 delegate richness to the payload keys).

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| JSON polymorphic dispatch | Custom switch-on-string converter | `[JsonPolymorphic]` + `[JsonDerivedType]` | Handles discriminator emit, parsing, unknown type error modes — many edge cases |
| Async backpressure queue | `ConcurrentQueue<T>` + `SemaphoreSlim` | `System.Threading.Channels.Channel.CreateBounded<T>()` | Handles FIFO ordering, single/multi reader/writer, completion signaling, cancellation — production-tested in BCL |
| Regex validation | `Regex.IsMatch(pattern, name)` (string-based) | `[GeneratedRegex]` partial method | Compile-time source generation, zero-alloc, no startup cost |
| Enum exhaustion in tests | Manual `new object[] { ActionType.Click }, new object[] { ... }` list | `Enum.GetValues<ActionType>()` wrapped in `TheoryData<ActionType>` | Auto-includes new enum values; compile-time safety |
| AOT JSON serialization | Reflection-based `JsonSerializer.Serialize<T>(obj)` without context | `JsonSerializer.Serialize(obj, RecrdJsonContext.Default.Session)` | Required for `PublishSingleFile` / native AOT (DIST-01) |

**Key insight:** `System.Threading.Channels` and `System.Text.Json` cover the two most complex problems in this phase (async pipeline, polymorphic serialization) with BCL implementations that have already handled edge cases (deadlock avoidance, cancellation propagation, AOT compatibility).

---

## Common Pitfalls

### Pitfall 1: Fast-Path Source Generation Breaks Polymorphism

**What goes wrong:** Registering the context with `GenerationMode = JsonSourceGenerationMode.Serialization` (fast-path) causes polymorphic deserialization to silently fall back or throw at runtime. The `$type` discriminator is ignored.

**Why it happens:** Fast-path source generation bypasses the metadata-resolver code path that handles `[JsonPolymorphic]`. Only metadata mode reads the discriminator.

**How to avoid:** Use `GenerationMode = JsonSourceGenerationMode.Metadata` on the context, OR use the default (both-modes), and explicitly register `IStep` and all derived types with `[JsonSerializable]`.

**Warning signs:** `JsonSerializer.Deserialize<Session>(json, RecrdJsonContext.Default.Session)` returns `ActionStep` as a plain `IStep` with no properties populated.

### Pitfall 2: `$type` Discriminator Must Be First Property

**What goes wrong:** If you write a `.recrd` file with `$type` appearing mid-object (e.g., after `actionType`), deserialization throws by default.

**Why it happens:** Default behavior requires `$type` to appear first in the JSON object to avoid buffering the entire object before knowing the concrete type.

**How to avoid:** Either (a) rely on `System.Text.Json` emitting `$type` first (it always does during serialization), or (b) set `AllowOutOfOrderMetadataProperties = true` for externally-authored `.recrd` files. For files that `recrd` itself writes, the default ordering is safe.

**Warning signs:** `JsonException: '$type' metadata property must precede all other properties in a polymorphic JSON object.`

### Pitfall 3: `Nullable enable` + `TreatWarningsAsErrors` Blocks Positional Records

**What goes wrong:** Positional primary constructors (`public sealed record ActionStep(ActionType ActionType, ...)`) on types with non-nullable reference type properties produce CS8618 when `Nullable enable` + `TreatWarningsAsErrors` are both active (they are, via `Directory.Build.props`). The build fails.

**Why it happens:** The compiler cannot prove the init-only properties are set during construction when using the positional syntax with external object initializers.

**How to avoid:** For records with `string` or reference-type properties, use explicit non-positional constructors (the primary constructor assigns all non-nullable fields). Or use `required` modifier (C# 11+) to force initialization at call site.

**Warning signs:** `error CS8618: Non-nullable property 'X' must contain a non-null value when exiting constructor.`

### Pitfall 4: `IStep` Source Generation Registration Gap

**What goes wrong:** If `IStep` (the interface) is not explicitly decorated with `[JsonPolymorphic]` / `[JsonDerivedType]`, serialization of `Session.Steps` (typed as `IReadOnlyList<IStep>`) emits only the base interface properties (none), producing `{}` for every step.

**Why it happens:** Source generation resolves types at compile time. The interface needs its own entry-point in the context.

**How to avoid:** Both annotate `IStep` with `[JsonPolymorphic]` + `[JsonDerivedType(typeof(ActionStep), ...)]` AND register `[JsonSerializable(typeof(IStep))]` on the context.

**Warning signs:** Round-trip test fails: deserialized `Session.Steps` contains instances of unknown/wrong type.

### Pitfall 5: Channel Deadlock Under Test

**What goes wrong:** Test reads `ReadAllAsync` synchronously while writer never calls `Complete()`, causing the reader to wait forever.

**Why it happens:** `ReadAllAsync` returns an `IAsyncEnumerable<T>` that terminates only when the writer signals completion via `Writer.Complete()`. If the test forgets to call `Complete()` (or the wrapper's `Complete()`), the `await foreach` loop hangs.

**How to avoid:** Always call `Complete()` in the test's finally block or after all writes. Pass a `CancellationToken` with a short timeout in test helpers. Test the `Cancel()` path explicitly.

**Warning signs:** Test hangs with no assertion failure.

### Pitfall 6: `Dictionary<string, string>` Is Not AOT-Safe by Default

**What goes wrong:** `Dictionary<string, string>` as the `Payload` type requires explicit `[JsonSerializable(typeof(Dictionary<string, string>))]` registration in the context for source generation. Without it, serialization may work via reflection in debug but fail in native AOT / `PublishSingleFile`.

**How to avoid:** Register `Dictionary<string, string>` explicitly in `RecrdJsonContext`. Also register `IReadOnlyDictionary<string, string>` if used in property declarations.

---

## Code Examples

Verified patterns from official sources:

### Session Record Shape
```csharp
// Requires explicit registration in RecrdJsonContext
public sealed record Session(
    int SchemaVersion,
    SessionMetadata Metadata,
    IReadOnlyList<Variable> Variables,
    IReadOnlyList<IStep> Steps);

public sealed record SessionMetadata(
    string Id,
    DateTimeOffset CreatedAt,
    string BrowserEngine,
    ViewportSize ViewportSize,
    string? BaseUrl);

public sealed record ViewportSize(int Width, int Height);
```

### Polymorphic IStep Registration
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ActionStep),    typeDiscriminator: "action")]
[JsonDerivedType(typeof(AssertionStep), typeDiscriminator: "assertion")]
[JsonDerivedType(typeof(GroupStep),     typeDiscriminator: "group")]
public interface IStep { }
```

### Source Generation Context
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata,  // required for polymorphism
    WriteIndented = false)]
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(IStep))]
[JsonSerializable(typeof(ActionStep))]
[JsonSerializable(typeof(AssertionStep))]
[JsonSerializable(typeof(GroupStep))]
[JsonSerializable(typeof(IReadOnlyList<IStep>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, string>))]
internal sealed partial class RecrdJsonContext : JsonSerializerContext { }
```

### Bounded Channel Creation
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/core/extensions/channels
var channel = Channel.CreateBounded<RecordedEvent>(
    new BoundedChannelOptions(capacity: 1000)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleWriter = false,
        SingleReader = false,
        AllowSynchronousContinuations = false
    });
```

### Channel Drain-Without-Deadlock (Test Pattern)
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/core/extensions/channels
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var sut = new RecordingChannel(capacity: 2);

await sut.WriteAsync(new RecordedEvent(...));
sut.Complete();  // signal end-of-stream

var results = new List<RecordedEvent>();
await foreach (var evt in sut.ReadAllAsync(cts.Token))
    results.Add(evt);

Assert.Single(results);
```

### Variable Validation Pattern
```csharp
// Avoids CS8618 under TreatWarningsAsErrors + Nullable enable
public sealed record Variable
{
    [GeneratedRegex(@"^[a-z][a-z0-9_]{0,63}$")]
    private static partial Regex NamePattern();

    public string Name { get; }
    public string? StepRef { get; init; }

    public Variable(string name, string? stepRef = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (!NamePattern().IsMatch(name))
            throw new ArgumentException(
                $"Variable name must match ^[a-z][a-z0-9_]{{0,63}}$.", nameof(name));
        Name = name;
        StepRef = stepRef;
    }
}
```

### TheoryData Enum Exhaustion
```csharp
// Source: https://andrewlock.net/creating-strongly-typed-xunit-theory-test-data-with-theorydata/
public static TheoryData<ActionType> AllActionTypes =>
    new(Enum.GetValues<ActionType>());

[Theory]
[MemberData(nameof(AllActionTypes))]
public void ActionStep_RoundTrips_ForAllActionTypes(ActionType type)
{
    var step = new ActionStep(type, TestSelector, ImmutableDictionary<string, string>.Empty);
    var json = JsonSerializer.Serialize<IStep>(step, RecrdJsonContext.Default.IStep);
    var result = JsonSerializer.Deserialize<IStep>(json, RecrdJsonContext.Default.IStep);
    Assert.IsType<ActionStep>(result);
    Assert.Equal(type, ((ActionStep)result).ActionType);
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `Newtonsoft.Json` polymorphism via `TypeNameHandling` | `[JsonPolymorphic]` + `[JsonDerivedType]` | .NET 7 (2022) | No external dep; AOT-compatible |
| Reflection-based `JsonSerializer.Serialize<T>()` | Source-generated `JsonSerializerContext` | .NET 6+ stabilized, .NET 8 production-ready | Required for single-file/AOT publish |
| `ConcurrentQueue<T>` + `SemaphoreSlim` for async pipelines | `System.Threading.Channels` | .NET Core 3.0 (2019), stable since | Fewer bugs, proper backpressure, cancellation built-in |
| `object[]` arrays with `[MemberData]` | `TheoryData<T>` with `[MemberData]` | xUnit v2 (available for years; v3 adds `TheoryDataRow`) | Compile-time type safety for test inputs |
| Abstract class hierarchies for AST types | Sealed records + enum discriminators | C# 9 (2020) | Structural equality, pattern matching, no boilerplate |

**Deprecated/outdated:**
- `Newtonsoft.Json`: Works but adds NuGet dep — prohibited by D-06. Migration guide: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft
- `JsonStringEnumConverter` (non-generic): Not AOT-compatible. Use `JsonStringEnumConverter<TEnum>` (generic, .NET 7+) for enum-as-string serialization.
- `JsonSourceGenerationMode.Serialization` alone: Does not support polymorphic deserialization.

---

## Open Questions

1. **Payload type: `Dictionary<string, string>` vs `Dictionary<string, object?>`**
   - What we know: `Dictionary<string, string>` is simpler, AOT-safe with explicit registration, and sufficient for recording-era payloads (all captured values are strings). `Dictionary<string, object?>` allows richer types but requires more source-gen registrations and introduces nullable complexity under `TreatWarningsAsErrors`.
   - What's unclear: Whether any future compiler (Phase 7) will need typed payload values (e.g., `int` for coordinates).
   - Recommendation: Use `Dictionary<string, string>` for Phase 2. Compilers can parse values from strings at compile time. Defer `object?` to a future schema version.

2. **`IStep` interface vs abstract record base**
   - What we know: `[JsonPolymorphic]` works on both `interface` and `class` types (confirmed by official docs). An `abstract record StepBase` would provide `with`-expression inheritance; an `interface IStep` provides looser coupling.
   - What's unclear: Whether `GroupStep.Steps : IReadOnlyList<IStep>` (recursive) causes any source-gen cycle issues.
   - Recommendation: Use `interface IStep` (matches D-01 intent of no abstract hierarchy) and register it in source gen. Recursive `IReadOnlyList<IStep>` is handled by registering `IReadOnlyList<IStep>` explicitly.

3. **JSON property naming: camelCase vs PascalCase**
   - What we know: `.recrd` is a project-specific format with no external consumers yet. Both are valid.
   - Recommendation: Use `JsonKnownNamingPolicy.CamelCase` — more conventional for JSON, avoids confusion with XML conventions, easier to read in raw files.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|---------|
| .NET SDK 10 | All build/test | Yes | 10.0.103 | — |
| `dotnet test` | Running unit tests | Yes | (bundled with SDK) | — |
| `dotnet format` | Code style check (CI-03) | Yes | (SDK built-in) | — |
| `dotnet build` | Compilation | Yes | (SDK built-in) | — |

All dependencies are available. No missing tools block execution of this phase.

**IPv4 requirement:** All `dotnet` commands must be prefixed with `DOTNET_SYSTEM_NET_DISABLEIPV6=1` (per CLAUDE.md memory and project notes).

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 |
| Config file | `xunit.runner.json` (none yet — uses defaults) |
| Quick run command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Core.Tests/ --no-build` |
| Full suite command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CORE-01 | `Session` serializes/deserializes round-trip with `schemaVersion: 1`, all fields, typed steps | unit | `dotnet test --no-build --filter "FullyQualifiedName~SessionSerializationTests"` | ❌ Wave 0 |
| CORE-02 | `ActionStep` constructible for all 6 `ActionType` values | unit | `dotnet test --no-build --filter "FullyQualifiedName~StepModelTests"` | ❌ Wave 0 |
| CORE-03 | `AssertionStep` constructible for all 5 `AssertionType` values | unit | `dotnet test --no-build --filter "FullyQualifiedName~StepModelTests"` | ❌ Wave 0 |
| CORE-04 | `GroupStep` constructible for `Given`/`When`/`Then`, children accessible | unit | `dotnet test --no-build --filter "FullyQualifiedName~StepModelTests"` | ❌ Wave 0 |
| CORE-05 | `Selector` priority array ranks strategies correctly | unit | `dotnet test --no-build --filter "FullyQualifiedName~SelectorVariableTests"` | ❌ Wave 0 |
| CORE-06 | `Variable` validates name regex; invalid names throw `ArgumentException` | unit | `dotnet test --no-build --filter "FullyQualifiedName~SelectorVariableTests"` | ❌ Wave 0 |
| CORE-07 | `ITestCompiler` interface exists with correct signature | unit (compile + reflection) | `dotnet test --no-build --filter "FullyQualifiedName~InterfaceContractTests"` | ❌ Wave 0 |
| CORE-08 | `IDataProvider` interface exists with `IAsyncEnumerable<IReadOnlyDictionary<string,string>>` signature | unit (compile + reflection) | `dotnet test --no-build --filter "FullyQualifiedName~InterfaceContractTests"` | ❌ Wave 0 |
| CORE-09 | `IEventInterceptor` interface exists | unit (compile) | `dotnet test --no-build --filter "FullyQualifiedName~InterfaceContractTests"` | ❌ Wave 0 |
| CORE-10 | `IAssertionProvider` interface exists | unit (compile) | `dotnet test --no-build --filter "FullyQualifiedName~InterfaceContractTests"` | ❌ Wave 0 |
| CORE-11 | `RecordingChannel` supports backpressure and cancellation; drains without deadlock | unit | `dotnet test --no-build --filter "FullyQualifiedName~ChannelPipelineTests"` | ❌ Wave 0 |
| CORE-12 | `RecordedEvent` constructible with all fields; serializes cleanly | unit | `dotnet test --no-build --filter "FullyQualifiedName~ChannelPipelineTests"` | ❌ Wave 0 |
| CORE-13 | `Recrd.Core.csproj` has zero `Recrd.*` ProjectReference | unit (csproj reflection) | `dotnet test --no-build --filter "FullyQualifiedName~InterfaceContractTests"` | ❌ Wave 0 |

### Sampling Rate

- **Per task commit (red phase):** `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet build /home/gil/dev/recrd/recrd.sln --no-restore` — must build with 0 warnings
- **Per task commit (green phase):** `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Core.Tests/ --no-build` — specific test file passing
- **Per wave merge:** `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"`
- **Phase gate:** Full suite green + coverage ≥ 90% on `Recrd.Core` before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `tests/Recrd.Core.Tests/SessionSerializationTests.cs` — covers CORE-01
- [ ] `tests/Recrd.Core.Tests/StepModelTests.cs` — covers CORE-02, CORE-03, CORE-04
- [ ] `tests/Recrd.Core.Tests/SelectorVariableTests.cs` — covers CORE-05, CORE-06
- [ ] `tests/Recrd.Core.Tests/ChannelPipelineTests.cs` — covers CORE-11, CORE-12
- [ ] `tests/Recrd.Core.Tests/InterfaceContractTests.cs` — covers CORE-07, CORE-08, CORE-09, CORE-10, CORE-13
- [ ] Delete `tests/Recrd.Core.Tests/PlaceholderTests.cs` in the same Wave 0 commit
- [ ] Delete `packages/Recrd.Core/Placeholder.cs` in the same Wave 0 commit (implementation files)

---

## Project Constraints (from CLAUDE.md)

| Directive | Impact on This Phase |
|-----------|---------------------|
| Always prefix `dotnet` with `DOTNET_SYSTEM_NET_DISABLEIPV6=1` | All build/test commands in plan tasks must include this prefix |
| Use Context7 MCP for library documentation | Attempted — MCP unavailable; fell back to official Microsoft Docs (HIGH confidence) |
| Never run dev server | N/A — no server in this phase |
| `.NET 10`, `net10.0` target framework | `Directory.Build.props` already enforces `net10.0` |
| `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true`, `LangVersion latest` | Records with reference properties need explicit constructors to avoid CS8618 build failure |
| xUnit + Moq + coverlet already configured | No new test packages needed |
| `IsPackable=false` on test projects | Already set — do not modify |
| `PlaceholderTests.cs` pattern exists | Wave 0: delete and replace with real test suites |
| `Placeholder.cs` in `Recrd.Core` | Wave 0: delete and replace with real types |
| TDD mandate: all tests committed red before implementation | D-13: commit on `tdd/phase-02` branch prefix; CI-06 tolerates failures there |
| `Recrd.Core` must have zero `Recrd.*` dependencies | No `ProjectReference` to sibling packages; only BCL types |

---

## Sources

### Primary (HIGH confidence)

- [Microsoft Docs — System.Text.Json Polymorphism](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism) — `[JsonPolymorphic]`, `[JsonDerivedType]`, interface support, discriminator format, source-gen constraints
- [Microsoft Docs — System.Text.Json Source Generation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation) — `JsonSerializerContext`, metadata mode, `[JsonSourceGenerationOptions]`, combining contexts
- [Microsoft Docs — Channels (.NET)](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) — `Channel.CreateBounded`, `BoundedChannelOptions`, `FullMode`, producer/consumer patterns, `ReadAllAsync`
- [Microsoft Docs — Pattern Matching](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching) — switch expressions, enum dispatch, record positional patterns

### Secondary (MEDIUM confidence)

- [Andrew Lock — Creating Strongly Typed xUnit Theory Test Data with TheoryData](https://andrewlock.net/creating-strongly-typed-xunit-theory-test-data-with-theorydata/) — `TheoryData<T>` as type-safe `MemberData` alternative (verified against xUnit 2.9.3 which is already installed)
- [dotnet/runtime Discussion #115218](https://github.com/dotnet/runtime/discussions/115218) — Community confirmation that polymorphic source gen works with metadata mode

### Tertiary (LOW confidence)

- None identified.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries are BCL (.NET 10 SDK verified) or already in `Recrd.Core.Tests.csproj`
- Architecture: HIGH — all patterns verified against official Microsoft Docs (updated Dec 2024 / Oct 2025)
- Pitfalls: HIGH — fast-path/polymorphism constraint, `$type` ordering, CS8618 pattern all verified from official sources
- Test patterns: HIGH — xUnit 2.9.3 is installed and `TheoryData<T>` is available in xUnit v2

**Research date:** 2026-03-26
**Valid until:** 2026-06-26 (stable BCL — 90-day window; xUnit and .NET APIs are stable)
