# Phase 4: Gherkin Generator - Research

**Researched:** 2026-03-27
**Domain:** .NET 10 / C# Gherkin text emission, AST traversal, pt-BR BDD keyword mapping
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Element references in generated steps use the best human-readable selector value — prefer `data-testid` value, then `id`, then `role`. E.g. `Clica no elemento "submit-btn"`, `Digita "<login>" no campo "username"`. No raw CSS/XPath expressions in step text.
- **D-02:** Variable placeholders use the `<variable_name>` syntax in the scenario body. E.g. `Digita "<login>" no campo "username"`.
- **D-03:** The `Feature:` name is derived from `Session.Metadata.BaseUrl`. Every session has a base URL, so this is always available.
- **D-04:** Scenario/Esquema do Cenário tags are configurable via `GherkinGeneratorOptions` — no tags emitted by default.
- **D-05:** Minimal file structure: `# language: pt` header → blank line → `Feature:` → blank line → scenario keyword (no `Background:` section, no scenario description prose).
- **D-06:** `GherkinException` is a `sealed class` (not record) extending `Exception`. Carries exactly two structured properties: `VariableName` (string) and `DataFilePath` (string).
- **D-07:** GHER-04 extra-column warning is emitted to `stderr` as plain text (not a thrown exception). The generator's `TextWriter output` param is the `.feature` target; stderr warnings use `Console.Error` or an injected `TextWriter` at Claude's discretion.
- **D-08:** Expose `IGherkinGenerator` interface + `GherkinGenerator` sealed class. Follows the `ITestCompiler`/`IDataProvider` pattern.
- **D-09:** Signature: `Task GenerateAsync(Session session, IDataProvider? dataProvider, TextWriter output, GherkinGeneratorOptions? options = null, CancellationToken ct = default)`.
- **D-10:** `GherkinGeneratorOptions` carries at minimum: `IReadOnlyList<string>? Tags` (null = no tags).
- **D-11:** TDD: all tests committed red on `tdd/phase-04` branch before any implementation. Green phase commits implementation only after all tests pass.
- **D-12:** Test file organization:
  - `FixedScenarioTests.cs` — zero-variable session → `Cenário` (GHER-01, GHER-07, GHER-08)
  - `DataDrivenTests.cs` — variable sessions → `Esquema do Cenário` + `Exemplos` table, column ordering (GHER-02, GHER-09)
  - `VariableMismatchTests.cs` — missing variable → `GherkinException`, extra column → stderr warning (GHER-03, GHER-04)
  - `GroupingTests.cs` — `GroupStep` keyword mapping + default heuristic (GHER-05, GHER-06)
  - `DeterminismTests.cs` — byte-identical output across multiple runs (GHER-07)

### Claude's Discretion

- Exact pt-BR verb templates for each `ActionType` (`Click`, `Type`, `Select`, `Navigate`, `Upload`, `DragDrop`) and `AssertionType` (`TextEquals`, `TextContains`, `Visible`, `Enabled`, `UrlMatches`)
- Whether `GherkinGeneratorOptions` is a record or class
- Whether stderr warning injection uses a `TextWriter` constructor parameter or `Console.Error` directly
- Indentation style in `.feature` output (2 spaces vs 4 spaces — pick one consistently)
- Whether `IDataProvider` null means "no data" (emit `Cenário`) or "use session variable count to decide"

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| GHER-01 | Session with zero variables emits `Cenário` (single scenario) | IDataProvider null/omitted → Cenário branch; confirmed by AST `Session.Variables.Count == 0` check |
| GHER-02 | Session with ≥1 variable emits `Esquema do Cenário` + `Exemplos` pipe-delimited table | `Esquema do Cenário` is the correct pt-BR keyword; `Exemplos` table uses pipe delimiter per Gherkin spec |
| GHER-03 | Variable missing from data columns → hard error (`GherkinException`) naming variable and file | `GherkinException` sealed class with `VariableName` + `DataFilePath`; throw after materializing column headers from IDataProvider |
| GHER-04 | Extra data column not in AST → warning to stderr only | D-07: write to injected `TextWriter` or `Console.Error`; do not throw |
| GHER-05 | `GroupStep(given)` → `Dado`/`E`; `GroupStep(when)` → `Quando`/`E`; `GroupStep(then)` → `Então`/`E` | Verified pt-BR keywords from official Cucumber language registry |
| GHER-06 | Default heuristic (no GroupStep): first navigation → `Dado`, interactions → `Quando`, assertions → `Então` | Requires two-pass or look-ahead; first navigation = first `ActionStep` with `ActionType.Navigate` |
| GHER-07 | Output is deterministic: same AST + same data = byte-identical `.feature` across runs | No `DateTime.Now`, no hash-based or non-deterministic ordering anywhere in the emission path |
| GHER-08 | Output file always UTF-8, no BOM, `# language: pt` header | `new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))` or pass `TextWriter` already configured to UTF-8 no-BOM |
| GHER-09 | Columns in `Exemplos` table ordered by first variable appearance in scenario body | Walk steps in order, collect variable names as first seen; use that list as column order |
</phase_requirements>

---

## Summary

Phase 4 is a pure text-generation problem with no external NuGet dependencies. The `GherkinGenerator` walks the `Session` AST (already fully implemented in `Recrd.Core`), maps each step to a pt-BR Gherkin keyword and human-readable sentence, then writes to a `TextWriter`. For data-driven cases, it peeks at `IDataProvider.StreamAsync()` to validate column coverage and emit the `Exemplos` table.

The core challenges are: (1) correct pt-BR keyword selection, including the `E` (And) continuation rule; (2) the default grouping heuristic, which requires an initial scan of steps to assign `Dado`/`Quando`/`Então` regions before emitting text; (3) strict determinism, which means no timestamps, no unordered collections, and no LINQ operations that sort by hash; and (4) column ordering in the `Exemplos` table, which must follow first-appearance order in the scenario body, not declaration order in `Session.Variables`.

**Primary recommendation:** Implement `GherkinGenerator` as a single-responsibility emitter class. Use `StringBuilder` internally for each section, `TextWriter.WriteAsync` for final output. Walk AST in declaration order; compute grouping regions in a pre-pass before emitting any text.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET 10 BCL — `System.Text` | (BCL) | `StringBuilder`, `TextWriter`, `StreamWriter`, `UTF8Encoding` | No external dep needed; everything required is in the BCL |
| xUnit | 2.9.3 (pinned in test csproj) | Test framework (already configured) | Project standard; existing csproj already references it |
| Moq | 4.20.72 (pinned in test csproj) | Mock `IDataProvider` in tests | Project standard; already in test csproj |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `System.IO.StringWriter` | (BCL) | In-test `TextWriter` to capture generated output as string | Every test that asserts on `.feature` file content |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| BCL `TextWriter` | `Scriban` / `Handlebars.NET` template engine | Template engines add a NuGet dep and indirection; this output is simple enough that direct string construction is cleaner and faster |
| Manual `StringBuilder` | `System.IO.Pipelines` | Pipelines are overkill — output is small, sequential, and synchronous within the generator |

**Installation:**

No new NuGet packages required. `Recrd.Gherkin.csproj` already references `Recrd.Core`. No additions needed.

**Version verification:**

Confirmed: `dotnet --version` on this machine returns `10.0.103`. All BCL APIs used are available in .NET 10.

---

## Architecture Patterns

### Recommended Project Structure

```
packages/Recrd.Gherkin/
├── IGherkinGenerator.cs         # Public interface (D-08)
├── GherkinGenerator.cs          # sealed class implementation
├── GherkinGeneratorOptions.cs   # Options record/class (D-10)
├── GherkinException.cs          # sealed Exception subclass (D-06)
└── Internal/
    ├── StepTextRenderer.cs      # Maps ActionStep/AssertionStep → pt-BR sentence
    ├── GroupingClassifier.cs    # Assigns Given/When/Then region to each step (GHER-05, GHER-06)
    └── ExemplosTableBuilder.cs  # Builds Exemplos table with correct column order (GHER-09)

tests/Recrd.Gherkin.Tests/
├── FixedScenarioTests.cs        # GHER-01, GHER-07, GHER-08
├── DataDrivenTests.cs           # GHER-02, GHER-09
├── VariableMismatchTests.cs     # GHER-03, GHER-04
├── GroupingTests.cs             # GHER-05, GHER-06
└── DeterminismTests.cs          # GHER-07
```

### Pattern 1: TextWriter-based Output (GHER-07, GHER-08)

**What:** Accept a `TextWriter` parameter instead of a file path. This decouples the generator from file I/O, making it testable with `StringWriter` and composable with any stream.

**When to use:** Always — this is the mandated API (D-09).

**Example:**

```csharp
// Source: .NET BCL — System.IO
// In tests: capture output without touching the filesystem
var sw = new StringWriter();
await generator.GenerateAsync(session, dataProvider: null, output: sw, ct: ct);
string featureText = sw.ToString();
Assert.StartsWith("# language: pt", featureText);

// For UTF-8 no-BOM file output (consumer's responsibility — CLI phase):
var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
await using var fileWriter = new StreamWriter(outputPath, append: false, utf8NoBom);
await generator.GenerateAsync(session, dataProvider, fileWriter, options, ct);
```

### Pattern 2: Two-Pass Step Rendering for Default Heuristic (GHER-06)

**What:** Before writing any text, do a pre-pass over `Session.Steps` to assign a `GroupType` (Given/When/Then) to each step. Then the emit pass uses the pre-computed assignments. This avoids look-ahead complexity in the emitter.

**When to use:** When `Session.Steps` contains no `GroupStep` wrappers (GHER-06 path).

**Algorithm:**

```csharp
// Pass 1 — classify
// Region starts as Given (from first Navigate ActionStep)
// Switches to When after first Navigate
// Switches to Then at first AssertionStep
//
// Rule:
//   All steps before (and including) first Navigate → Given
//   Steps after first Navigate that are ActionSteps → When
//   AssertionSteps → Then (once seen, all remaining assertions stay Then)
//
// Degenerate case: no Navigate found → everything is When (or Given if all assertions)
```

### Pattern 3: First-Appearance Column Ordering (GHER-09)

**What:** Walk the emitted step texts in order. The first time a `<variable_name>` placeholder appears, record that variable as the next column. Repeat for all steps to build an ordered list.

```csharp
// Collect column order by scanning step sentences in emission order
var columnOrder = new List<string>();
var seen = new HashSet<string>();
foreach (var stepText in renderedStepTexts)
{
    foreach (Match m in Regex.Matches(stepText, @"<([^>]+)>"))
    {
        var varName = m.Groups[1].Value;
        if (seen.Add(varName))
            columnOrder.Add(varName);
    }
}
```

### Pattern 4: Keyword Continuation with `E` (GHER-05)

**What:** The first step in a group uses the primary keyword (`Dado`, `Quando`, `Então`). Subsequent steps in the same group use `E` (And).

```csharp
// Emit logic within a group
bool isFirst = true;
foreach (var step in groupSteps)
{
    string keyword = isFirst ? primaryKeyword : "E";
    isFirst = false;
    await writer.WriteLineAsync($"    {keyword} {RenderStep(step)}");
}
```

### Anti-Patterns to Avoid

- **Emitting timestamps or GUIDs into the `.feature` body:** Breaks GHER-07 (determinism). The generator receives a `Session` that already has timestamps — never call `DateTime.Now` or `Guid.NewGuid()` inside the emitter.
- **Using `Session.Variables` order for `Exemplos` columns:** GHER-09 requires first-appearance-in-scenario-body order, which may differ from declaration order. The declaration order in `Session.Variables` is not authoritative for column ordering.
- **Buffering all `IDataProvider` rows into a `List<T>` before validation:** For large files this wastes memory. Validate columns from the first row only, then stream rows.
- **Writing the `Feature:` label with the full URL including protocol:** Feature names should be human-readable. Consider stripping `https://` prefix or using just the host+path. (This is discretion territory — pick a consistent rule.)
- **CSS or XPath in step text:** D-01 explicitly forbids this. The `Selector.Strategies` list must be walked to find the first of `DataTestId`, `Id`, `Role` — if none present, fall back to the first available value with a warning.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| UTF-8 no-BOM encoding | Custom byte-stripping logic | `new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)` | BCL handles it correctly for all stream types |
| BOM detection in IDataProvider output | Own parser | Already handled in `CsvDataProvider` / `JsonDataProvider` from Phase 3 | Data providers already deliver clean string dictionaries |
| Regex for variable placeholder extraction | String.IndexOf loops | `Regex.Matches(text, @"<([^>]+)>")` | Compiled regex is cleaner; correctness edge cases (nested angle brackets, etc.) |
| Gherkin keyword lookup table | `switch` expressions per language | Direct constants — pt-BR is the only target language | Single-language tool; no i18n abstraction needed |

**Key insight:** This is a text-generation problem, not a parsing problem. All complexity lives in the traversal and classification logic, not in any library API.

---

## Pt-BR Gherkin Keywords Reference

Source: Cucumber official language registry (verified 2026-03-27).

| English | pt-BR | Continuation |
|---------|-------|-------------|
| `Feature:` | `Funcionalidade:` | — |
| `Scenario:` | `Cenário:` | — |
| `Scenario Outline:` | `Esquema do Cenário:` | — |
| `Examples:` | `Exemplos:` | — |
| `Given` | `Dado` / `Dada` / `Dados` / `Dadas` | `E` |
| `When` | `Quando` | `E` |
| `Then` | `Então` | `E` |
| `And` | `E` | — |
| `But` | `Mas` | — |

**Project choice (locked by D-05):** Use `Dado` (masculine singular) as the canonical Given keyword. Use `E` for all continuations regardless of GroupType.

---

## Step Text Template Decisions (Claude's Discretion — Recommended)

These are the recommended pt-BR verb templates. The planner should treat these as the implementation target.

### ActionType Templates

| ActionType | Template | Notes |
|------------|---------|-------|
| `Navigate` | `Navega para "{url}"` | URL comes from `Payload["url"]` or `Payload["href"]`; if absent, use selector value |
| `Click` | `Clica no elemento "{selector}"` | Selector = best human-readable value (data-testid → id → role) |
| `Type` | `Digita "{value}" no campo "{selector}"` | If value is a variable placeholder: `Digita "<var>" no campo "{selector}"`; `Payload["value"]` |
| `Select` | `Seleciona "{value}" no campo "{selector}"` | `Payload["value"]` |
| `Upload` | `Envia o arquivo "{filename}" no campo "{selector}"` | `Payload["filename"]` or `Payload["path"]` |
| `DragDrop` | `Arrasta "{source}" para "{target}"` | Two selectors involved — source is the step's Selector; target from `Payload["target"]` |

### AssertionType Templates

| AssertionType | Template | Notes |
|---------------|---------|-------|
| `TextEquals` | `O texto do elemento "{selector}" é "{expected}"` | `Payload["expected"]` |
| `TextContains` | `O texto do elemento "{selector}" contém "{expected}"` | `Payload["expected"]` |
| `Visible` | `O elemento "{selector}" está visível` | No payload value needed |
| `Enabled` | `O elemento "{selector}" está habilitado` | No payload value needed |
| `UrlMatches` | `A URL corresponde a "{pattern}"` | `Payload["pattern"]`; no selector needed |

**Selector resolution rule (D-01):**

```csharp
private static string BestSelectorValue(Selector selector)
{
    var preferenceOrder = new[] { SelectorStrategy.DataTestId, SelectorStrategy.Id, SelectorStrategy.Role };
    foreach (var strategy in preferenceOrder)
    {
        if (selector.Values.TryGetValue(strategy, out var value))
            return value;
    }
    // Fallback: first available value in Strategies list order
    foreach (var strategy in selector.Strategies)
    {
        if (selector.Values.TryGetValue(strategy, out var value))
            return value;
    }
    return "(unknown)";
}
```

---

## Common Pitfalls

### Pitfall 1: Variable Placeholder Injection Order

**What goes wrong:** Developer substitutes variable placeholders using `Session.Variables` list order, producing an `Exemplos` column header that does not match step body appearance order.

**Why it happens:** `Session.Variables` is a declaration list; step bodies use variables in whatever order the user happened to write them.

**How to avoid:** Always derive column order from a first-appearance scan of the rendered step texts (GHER-09). The `Session.Variables` list is only used to validate that all declared variables appear in data columns.

**Warning signs:** Test with a session where `Session.Variables = [b, a]` but the first step uses `<a>` before `<b>` — the `Exemplos` header must be `| a | b |`.

### Pitfall 2: Byte-Identical Output Broken by Culture-Sensitive Operations

**What goes wrong:** Output differs across machines or locales because a `ToString()` call uses `CultureInfo.CurrentCulture`.

**Why it happens:** Default `ToString()` for dates, numbers, and some string operations is culture-sensitive.

**How to avoid:** The generator should never format numeric or date values — it only emits string values from `Payload` dictionaries (already strings) and constant keywords. No locale risk in the current design.

**Warning signs:** Any call to `DateTime.Now.ToString()` or `decimal.ToString()` inside the emitter is a red flag.

### Pitfall 3: GherkinException Thrown Before First Row Is Read

**What goes wrong:** Generator throws `GherkinException` (missing variable) before it can check the actual column headers, because the developer checked `Session.Variables` against an empty enumerable.

**Why it happens:** `IDataProvider.StreamAsync()` is an async stream — column headers come from the first row's keys.

**How to avoid:** Materialize the first row first, extract its keys as the column set, then validate. If the stream is empty and the session has variables, the behavior should be defined — throw `GherkinException` (no data to satisfy variables).

**Warning signs:** Validation logic that runs before `await foreach` starts is likely wrong.

### Pitfall 4: `E` Continuation Emitted for Single-Step Groups

**What goes wrong:** A `GroupStep` with one child step emits `E` instead of `Dado`/`Quando`/`Então`.

**Why it happens:** Developer initializes `isFirst = false` or off-by-one in the continuation logic.

**How to avoid:** Use a flag that is reset for each group. The first step in any group always gets the primary keyword.

### Pitfall 5: `SessionMetadata.BaseUrl` Is Nullable

**What goes wrong:** `Feature: null` or `NullReferenceException` when `BaseUrl` is not set.

**Why it happens:** `SessionMetadata.BaseUrl` is declared `string? BaseUrl { get; init; }` — it is nullable.

**How to avoid:** Guard in the generator: if `session.Metadata.BaseUrl` is null or empty, emit `Funcionalidade: (sem URL base)` or throw a descriptive `GherkinException`. The CONTEXT.md says "Every session has a base URL" — but the type system does not enforce this. A defensive null-check is safer than trusting the caller.

---

## Code Examples

Verified patterns from existing codebase and BCL:

### StringWriter Test Pattern (per project's established style)

```csharp
// Source: mirrors CsvDataProviderTests.cs pattern in tests/Recrd.Data.Tests/
[Fact]
public async Task GenerateAsync_ZeroVariableSession_EmitsCenario()
{
    var session = new Session(
        SchemaVersion: 1,
        Metadata: new SessionMetadata("id1", DateTimeOffset.UtcNow, "chromium",
            new ViewportSize(1280, 720), BaseUrl: "https://example.com"),
        Variables: [],
        Steps: [
            new ActionStep(ActionType.Navigate, MakeSelector("home"), new Dictionary<string, string> { ["url"] = "https://example.com" }.AsReadOnly())
        ]);

    var sw = new StringWriter();
    var generator = new GherkinGenerator();
    await generator.GenerateAsync(session, dataProvider: null, output: sw);

    var text = sw.ToString();
    Assert.StartsWith("# language: pt", text);
    Assert.Contains("Funcionalidade:", text);
    Assert.Contains("Cenário:", text);
    Assert.DoesNotContain("Esquema do Cenário", text);
    Assert.DoesNotContain("Exemplos", text);
}
```

### Mocking IDataProvider (Moq)

```csharp
// Source: Moq 4.x pattern; IDataProvider is in Recrd.Core.Interfaces
var mockProvider = new Mock<IDataProvider>();
mockProvider
    .Setup(p => p.StreamAsync(It.IsAny<CancellationToken>()))
    .Returns(AsyncEnumerable(
        new Dictionary<string, string> { ["login"] = "alice", ["password"] = "secret" },
        new Dictionary<string, string> { ["login"] = "bob",   ["password"] = "pass2"  }
    ));

static async IAsyncEnumerable<IReadOnlyDictionary<string, string>> AsyncEnumerable(
    params IReadOnlyDictionary<string, string>[] rows)
{
    foreach (var row in rows) yield return row;
    await Task.CompletedTask;
}
```

### UTF-8 No-BOM TextWriter (GHER-08)

```csharp
// Source: .NET BCL System.IO
var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
await using var writer = new StreamWriter(outputPath, append: false, utf8NoBom);
// Then pass writer to GenerateAsync — the generator does not set encoding
```

### GherkinException Pattern (mirrors DataParseException from Recrd.Data)

```csharp
// Source: packages/Recrd.Data/DataParseException.cs (existing implementation)
namespace Recrd.Gherkin;

public sealed class GherkinException : Exception
{
    public string VariableName { get; }
    public string DataFilePath { get; }

    public GherkinException(string variableName, string dataFilePath, string message)
        : base(message)
    {
        VariableName = variableName;
        DataFilePath = dataFilePath;
    }
}
```

---

## Runtime State Inventory

Step 2.5 SKIPPED — this is a greenfield implementation phase (new files only, no rename/refactor/migration).

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 10 SDK | Build and test | Yes | 10.0.103 | — |
| xUnit 2.9.3 | Test runner | Yes (pinned in csproj) | 2.9.3 | — |
| Moq 4.20.72 | Mock IDataProvider | Yes (pinned in csproj) | 4.20.72 | — |

No missing dependencies. All required tooling is present.

---

## Validation Architecture

`nyquist_validation` is `true` in `.planning/config.json` — this section is required.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 |
| Config file | None (SDK default discovery) |
| Quick run command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Gherkin.Tests --no-build` |
| Full suite command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| GHER-01 | Zero-variable session emits `Cenário` | unit | `dotnet test --filter "FullyQualifiedName~FixedScenarioTests"` | No — Wave 0 |
| GHER-02 | Variable session emits `Esquema do Cenário` + `Exemplos` table | unit | `dotnet test --filter "FullyQualifiedName~DataDrivenTests"` | No — Wave 0 |
| GHER-03 | Missing variable → `GherkinException` | unit | `dotnet test --filter "FullyQualifiedName~VariableMismatchTests"` | No — Wave 0 |
| GHER-04 | Extra column → stderr warning (not exception) | unit | `dotnet test --filter "FullyQualifiedName~VariableMismatchTests"` | No — Wave 0 |
| GHER-05 | `GroupStep` keyword mapping (`Dado`/`Quando`/`Então`/`E`) | unit | `dotnet test --filter "FullyQualifiedName~GroupingTests"` | No — Wave 0 |
| GHER-06 | Default heuristic grouping (navigate → Dado, action → Quando, assert → Então) | unit | `dotnet test --filter "FullyQualifiedName~GroupingTests"` | No — Wave 0 |
| GHER-07 | Byte-identical output on repeated runs | unit | `dotnet test --filter "FullyQualifiedName~DeterminismTests"` | No — Wave 0 |
| GHER-08 | UTF-8 no-BOM, `# language: pt` header | unit | `dotnet test --filter "FullyQualifiedName~FixedScenarioTests"` | No — Wave 0 |
| GHER-09 | `Exemplos` columns ordered by first appearance in scenario body | unit | `dotnet test --filter "FullyQualifiedName~DataDrivenTests"` | No — Wave 0 |

### Sampling Rate

- **Per task commit:** `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Gherkin.Tests --no-build`
- **Per wave merge:** `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"`
- **Phase gate:** Full suite green + coverage ≥ 90% on `Recrd.Gherkin` before `/gsd:verify-work`

### Wave 0 Gaps

All five test files are new — none exist yet. Wave 0 must create:

- [ ] `tests/Recrd.Gherkin.Tests/FixedScenarioTests.cs` — covers GHER-01, GHER-07, GHER-08
- [ ] `tests/Recrd.Gherkin.Tests/DataDrivenTests.cs` — covers GHER-02, GHER-09
- [ ] `tests/Recrd.Gherkin.Tests/VariableMismatchTests.cs` — covers GHER-03, GHER-04
- [ ] `tests/Recrd.Gherkin.Tests/GroupingTests.cs` — covers GHER-05, GHER-06
- [ ] `tests/Recrd.Gherkin.Tests/DeterminismTests.cs` — covers GHER-07
- [ ] Remove `tests/Recrd.Gherkin.Tests/PlaceholderTests.cs` (replaced by above)

Framework already installed — no install step needed.

---

## Project Constraints (from CLAUDE.md)

| Directive | Impact on Phase 4 |
|-----------|-------------------|
| All `dotnet` commands prefixed with `DOTNET_SYSTEM_NET_DISABLEIPV6=1` | All build/test commands in plans must include this prefix |
| Never run the dev server — tell user to run | N/A (no dev server in this phase) |
| System.Text.Json only; no Newtonsoft.Json | Generator uses no JSON — no risk; but if any test fixture uses JSON session deserialization, use `RecrdJsonContext` from Phase 2 |
| TDD: all tests committed red before any implementation | `tdd/phase-04` branch; CI-06 tolerates test failures on `tdd/phase-*` prefix |
| ≥ 90% line coverage on `Recrd.Gherkin` (CI-02) | All 9 GHER requirements must have test coverage |
| `dotnet format --verify-no-changes` enforced (CI-03) | Every commit must pass format check |
| `TreatWarningsAsErrors true` in `Directory.Build.props` | No warning-level issues permitted; all `#pragma warning disable` uses must be justified (as done for CS0162 in Phase 3) |

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `Console.Write` directly | `TextWriter` injection | Standard .NET testing pattern | Testable without stream redirection |
| Template-engine based generators | Direct `StringBuilder`/`TextWriter` for simple formats | Ongoing | Simpler, no dep, easier to guarantee determinism |

**Deprecated/outdated:**

- Nothing — this is a new implementation. No migration from older code.

---

## Open Questions

1. **`DragDrop` second selector representation**
   - What we know: `ActionStep` has a single `Selector` field. `DragDrop` needs a source and a target element.
   - What's unclear: Is the target element in `Payload["targetSelector"]` as a raw CSS/XPath string, or is there a structured approach?
   - Recommendation: Check Phase 6 (Recording Engine) decisions when available. For Phase 4, assume `Payload` carries `"target"` key with a plain string value. Emit `Arrasta "{sourceSelector}" para "{target}"` using `Payload["target"]` as a raw label. Flag in generated step comment if needed.

2. **`Navigate` step and selector relevance**
   - What we know: `ActionStep(Navigate)` has a `Selector` field, but navigation is typically URL-based, not element-based.
   - What's unclear: Will `Payload["url"]` always be present for Navigate steps, or might the URL come from the Selector?
   - Recommendation: Prefer `Payload["url"]`; fall back to `BestSelectorValue(step.Selector)` if `Payload` does not contain `"url"`. Document this fallback in a comment.

3. **Empty `Session.Steps` list**
   - What we know: Nothing in GHER-01–09 specifies behavior for a session with zero steps.
   - What's unclear: Should the generator emit a `Cenário` with no steps (technically invalid Gherkin), throw, or silently emit a comment?
   - Recommendation: Emit the skeleton (`Cenário:` with no steps). Cucumber and Robot Framework parsers tolerate empty scenarios. Flag as a warning if desired — this is a corner case that the recording engine will not produce in practice.

---

## Sources

### Primary (HIGH confidence)

- `.planning/phases/04-gherkin-generator/04-CONTEXT.md` — All locked decisions verified directly
- `packages/Recrd.Core/Ast/*.cs` — Actual AST types read from source
- `packages/Recrd.Data/DataParseException.cs` — Exception pattern verified from source
- `tests/Recrd.Gherkin.Tests/Recrd.Gherkin.Tests.csproj` — xUnit/Moq versions verified from source
- Cucumber official language registry (via WebFetch on cucumber.io) — pt-BR keyword table verified

### Secondary (MEDIUM confidence)

- WebSearch — confirmed `# language: pt` header requirement for pt-BR Gherkin files; cross-referenced with Cucumber docs
- xUnit official docs (via WebSearch) — `StringWriter` + `TextWriter` injection pattern for output testing

### Tertiary (LOW confidence)

- Recommended pt-BR verb templates (e.g., `Clica no elemento`, `Navega para`) — these are not standardized; chosen to be idiomatic Brazilian Portuguese BDD step phrasing. No single authoritative source. Treat as a discretion choice per CONTEXT.md.

---

## Metadata

**Confidence breakdown:**

- Standard stack: HIGH — no new packages; all tooling verified from csproj files and `dotnet --version`
- Architecture: HIGH — patterns derived from existing Phase 2/3 implementations in this repo
- Pt-BR keywords: HIGH — verified from Cucumber official language registry
- Verb templates: LOW — idiomatic choice, no authoritative source; tagged as Claude's discretion
- Pitfalls: HIGH — derived from direct code analysis of AST types and requirements

**Research date:** 2026-03-27
**Valid until:** 2026-06-27 (stable domain — pt-BR Gherkin keywords and .NET BCL APIs do not change frequently)
