# Phase 7: Compilers - Research

**Researched:** 2026-04-05
**Domain:** Robot Framework 7 code generation, Browser library, SeleniumLibrary, E2E subprocess testing
**Confidence:** HIGH (core RF format, interface contracts, existing code); MEDIUM (Browser library keyword signatures — docs timed out, confirmed via README + GitHub source); LOW (RF "minimum version declaration" in Settings — not found in any source)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** `.resource` file contains page-object keywords wrapping low-level Browser/Selenium keywords. `.robot` test case calls those keywords. Idiomatic RF7.
- **D-02:** Keyword names derived from action type + element label in pt-BR, slug-normalized from selector value. E.g., Click on `data-testid="submit-btn"` → `Clicar Em Submit Btn`. Compiler converts selector values to title-case pt-BR slugs (hyphen/underscore → space → title case).
- **D-03:** `.robot` suite `*** Settings ***` imports `.resource` and declares minimum RF version. `.resource` `*** Settings ***` declares Library (`Browser` or `SeleniumLibrary`). Both files carry traceability header comment (COMP-07).
- **D-04:** Fixture HTML covers all ActionTypes: button (Click), text input (Type), `<select>` (Select), file input (Upload), draggable/droppable pair (DragDrop), navigation link (Navigate), assertable text/URL (AssertionStep coverage).
- **D-05:** Recording/capture phase of integration tests uses `Page.SetContentAsync` — consistent with Phase 6 in-process pattern.
- **D-06:** Execute step uses Kestrel TestServer serving fixture HTML at real localhost URL + `Process.Start("robot", ...)` subprocess. Test asserts RF exit code = 0.
- **D-07:** CI must install `pip install robotframework robotframework-browser robotframework-seleniumlibrary` before integration tests. New CI step in Phase 7.
- **D-08:** `RobotSeleniumCompiler` uses implicit wait only: `Set Selenium Implicit Wait    ${TIMEOUT}s` in Suite Setup keyword in `.resource`.
- **D-09:** `CompilerOptions.TimeoutSeconds` (default 30) drives wait for both compilers. Browser compiler uses it for `Wait For Elements State` timeout; Selenium uses it for implicit wait duration. COMP-06's "default 10s" is superseded.
- **D-10:** Unresolvable selector: walk fallback chain (DataTestId → Id → Role → Css → XPath). If fully exhausted, emit warning in `CompilationResult.Warnings` and use last-available strategy value. Never throw.
- **D-11:** TDD mandate — all compiler tests committed failing on `tdd/phase-07` branch before implementation. Green phase only after all tests pass. Coverage ≥90% on `Recrd.Compilers`.

### Claude's Discretion

- Exact keyword slug normalization algorithm (punctuation handling, accented chars, length limits)
- `CompilationResult.DependencyManifest` content
- Whether Suite Teardown lives in `.resource` or `.robot`
- Internal class structure (helpers vs monolithic)
- Whether E2E round-trip lives in `Recrd.Integration.Tests` or a new sub-suite of `Recrd.Compilers.Tests`

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| COMP-01 | `RobotBrowserCompiler` emits RF7-compatible `.robot` suite and `.resource` file | RF7 file format, Browser library Settings block, keyword structure documented below |
| COMP-02 | `RobotBrowserCompiler` uses `css=[data-testid="..."]` preferred selector; falls back per `--selector-strategy` | Browser library CSS selector syntax; `CompilerOptions.PreferredSelectorStrategy` contract documented |
| COMP-03 | `RobotBrowserCompiler` inserts `Wait For Elements State` before every interaction keyword | `Wait For Elements State` signature and state enum documented; `${TIMEOUT}s` format confirmed |
| COMP-04 | `RobotSeleniumCompiler` emits RF7-compatible `.robot` suite and `.resource` file | SeleniumLibrary Settings block, keyword mapping documented below |
| COMP-05 | `RobotSeleniumCompiler` prefers `id:...` selector; falls back to `css:...` then `xpath:...` | SeleniumLibrary selector strategy syntax confirmed |
| COMP-06 | `RobotSeleniumCompiler` emits configurable implicit/explicit waits (superseded by D-09: default 30s via CompilerOptions) | `Set Selenium Implicit Wait` keyword usage documented |
| COMP-07 | Both compilers emit traceability header: version, timestamp, SHA-256, target name | RF comment syntax (`#`) confirmed; header placement as file-top comments |
| COMP-08 | Both compilers emit `*** Settings ***` block declaring minimum RF version | Investigated — no native "Require Minimum" setting exists in RF7; use `Metadata` key-value or comment in header |
| COMP-09 | `CompilationResult` includes generated file list, warnings list, dependency manifest | `CompilationResult` C# record fully read; all three fields confirmed |
| COMP-10 | Round-trip E2E: `record → compile → execute` passes on fixture web app with zero manual edits | Kestrel real-port fixture + `Process.Start("robot", ...)` pattern documented |
</phase_requirements>

---

## Summary

Phase 7 implements two `ITestCompiler` implementations — `RobotBrowserCompiler` and `RobotSeleniumCompiler` — that translate a `Session` AST into RF7-compatible `.robot` + `.resource` file pairs. The domain is Robot Framework 7 code generation: both the file format (well-understood, confirmed via official user guide) and the library-specific keyword APIs (Browser library 19.13.0, SeleniumLibrary 6.8.0).

The `.resource` file follows the page-object pattern: each recorded step becomes a named keyword in `.resource`, and the `.robot` test case simply calls those keywords sequentially. This keeps the generated test files readable and aligns with idiomatic RF7 community conventions. Keyword names are generated from the ActionType verb (pt-BR) plus a slug of the selector value.

The E2E round-trip (COMP-10) combines two test techniques: in-process Playwright `SetContentAsync` for the recording side (consistent with Phase 6), and a real Kestrel TestServer with a `Process.Start("robot", ...)` subprocess for the execute side. The subprocess pattern is the only way to truly validate that the generated `.robot` files are syntactically and semantically valid RF7 — an in-process RF execution would require a Python FFI which is impractical in .NET.

**Primary recommendation:** Model both compilers as `TextWriter`-based generators following the same `async Task WriteToAsync(TextWriter, ...)` internal helper pattern used by `GherkinGenerator`. Split each compiler into `KeywordNameBuilder`, `SelectorResolver`, and `HeaderEmitter` helpers. The E2E round-trip test lives in `Recrd.Integration.Tests` using `IAsyncLifetime` + a minimal Kestrel host.

---

## Standard Stack

### Core (verified via pip index — 2026-04-05)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| robotframework | 7.4.2 | RF7 execution engine for E2E subprocess | Latest stable; aligns with RF7-only decision |
| robotframework-browser | 19.13.0 | Browser library (Playwright-powered) for robot-browser target | Playwright-native; requires `rfbrowser init` post-install |
| robotframework-seleniumlibrary | 6.8.0 | Selenium wrapper for robot-selenium target | Canonical RF Selenium library |
| Microsoft.AspNetCore.TestHost | 10.0.x | Kestrel TestServer for E2E fixture serving | Already in .NET 10 SDK; serves static HTML at real port |

### Supporting (.NET side — already in solution)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit | 2.9.3 | Unit + integration tests | All test files |
| Moq | 4.20.72 | Mock `ITestCompiler`, `CompilerOptions` | Unit tests for compiler helpers |
| coverlet.msbuild | 6.0.4 | Coverage collection | Already in Recrd.Compilers.Tests.csproj |
| System.Security.Cryptography | BCL | SHA-256 of `.recrd` file for traceability header | No NuGet needed |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| rfbrowser-browser 19.x | rfbrowser-browser 18.x | 18.x supported Python 3.8+; 19.x requires 3.10+. Ubuntu-latest ships Python 3.12, so 19.x is fine. |
| Process.Start("robot",...) | Python NET interop | Process.Start keeps .NET test isolated from Python runtime; simpler CI; subprocess exit code is the canonical E2E signal |
| IHost + Kestrel real port | TestServer in-memory | In-memory server has no real URL — `robot` subprocess cannot reach it; real Kestrel port required |

**Installation (CI step to add):**
```bash
pip install robotframework robotframework-browser robotframework-seleniumlibrary
rfbrowser init
```

**Version verification:** Confirmed via `pip3 index versions` against PyPI on 2026-04-05.

---

## Architecture Patterns

### Recommended Project Structure

```
packages/Recrd.Compilers/
├── RobotBrowserCompiler.cs         # ITestCompiler implementation
├── RobotSeleniumCompiler.cs        # ITestCompiler implementation
└── Internal/
    ├── KeywordNameBuilder.cs       # ActionType + selector → pt-BR keyword name
    ├── SelectorResolver.cs         # Fallback chain: DataTestId → Id → Role → Css → XPath
    ├── HeaderEmitter.cs            # Traceability header (version, timestamp, SHA-256, target)
    ├── BrowserKeywordEmitter.cs    # Browser-library-specific keyword bodies
    └── SeleniumKeywordEmitter.cs   # SeleniumLibrary-specific keyword bodies

tests/Recrd.Compilers.Tests/
├── BrowserCompilerOutputTests.cs       # .robot/.resource structure, COMP-01, COMP-08
├── BrowserCompilerSelectorTests.cs     # Selector fallback chain, COMP-02
├── BrowserCompilerWaitTests.cs         # Wait For Elements State insertion, COMP-03
├── SeleniumCompilerOutputTests.cs      # .robot/.resource structure, COMP-04, COMP-08
├── SeleniumCompilerSelectorTests.cs    # id: / css: / xpath: priority, COMP-05
├── SeleniumCompilerWaitTests.cs        # Implicit wait emission, COMP-06, D-08
├── TraceabilityHeaderTests.cs          # Header content for both compilers, COMP-07
├── CompilationResultTests.cs           # GeneratedFiles, Warnings, DependencyManifest, COMP-09
└── KeywordNameBuilderTests.cs          # Slug normalization, pt-BR verbs, D-02

tests/Recrd.Integration.Tests/
└── RoundTripTests.cs                   # record → compile → execute, COMP-10
```

### Pattern 1: TextWriter-based generation (mirrors GherkinGenerator)

**What:** Both compilers write RF files through a `TextWriter`, enabling `StringWriter`-based unit testing without file I/O.
**When to use:** Everywhere — the public `CompileAsync` creates files, but internal helpers write to `TextWriter`.

```csharp
// Internal pattern (mirrors GherkinGenerator)
internal static async Task WriteResourceAsync(
    Session session,
    CompilerOptions options,
    TextWriter writer)
{
    await writer.WriteLineAsync("# Generated by recrd ...");
    await writer.WriteLineAsync("*** Settings ***");
    await writer.WriteLineAsync("Library    Browser");
    await writer.WriteLineAsync();
    await writer.WriteLineAsync("*** Keywords ***");
    foreach (var step in FlattenSteps(session.Steps))
        await WriteKeywordAsync(step, options, writer);
}
```

### Pattern 2: RF7 `.resource` file structure (Browser library)

```robot
# Generated by recrd 1.0.0 | 2026-04-05T10:00:00Z | sha256:abc123 | robot-browser
*** Settings ***
Library    Browser

*** Keywords ***
Clicar Em Submit Btn
    Wait For Elements State    css=[data-testid="submit-btn"]    visible    timeout=${TIMEOUT}s
    Click    css=[data-testid="submit-btn"]

Digitar Em Email Input    [Arguments]    ${value}
    Wait For Elements State    css=[data-testid="email-input"]    visible    timeout=${TIMEOUT}s
    Type Text    css=[data-testid="email-input"]    ${value}

Abrir Suite
    New Browser    chromium    headless=true
    New Page    ${BASE_URL}

Fechar Suite
    Close Browser
```

### Pattern 3: RF7 `.robot` file structure (Browser library)

```robot
# Generated by recrd 1.0.0 | 2026-04-05T10:00:00Z | sha256:abc123 | robot-browser
*** Settings ***
Resource    session.resource
Suite Setup      Abrir Suite
Suite Teardown   Fechar Suite

*** Variables ***
${BASE_URL}     http://localhost:5000
${TIMEOUT}      30

*** Test Cases ***
Sessao Gravada
    Clicar Em Submit Btn
    Digitar Em Email Input    valor@exemplo.com
```

### Pattern 4: RF7 `.resource` file structure (SeleniumLibrary)

```robot
# Generated by recrd 1.0.0 | 2026-04-05T10:00:00Z | sha256:abc123 | robot-selenium
*** Settings ***
Library    SeleniumLibrary

*** Keywords ***
Abrir Suite
    Open Browser    ${BASE_URL}    chrome
    Set Selenium Implicit Wait    ${TIMEOUT}s

Fechar Suite
    Close All Browsers

Clicar Em Submit Btn
    Click Element    id:submit-btn

Digitar Em Email Input    [Arguments]    ${value}
    Input Text    id:email-input    ${value}
```

### Pattern 5: RF7 `.robot` file structure (SeleniumLibrary)

```robot
# Generated by recrd 1.0.0 | 2026-04-05T10:00:00Z | sha256:abc123 | robot-selenium
*** Settings ***
Resource    session.resource
Suite Setup      Abrir Suite
Suite Teardown   Fechar Suite

*** Variables ***
${BASE_URL}     http://localhost:5000
${TIMEOUT}      30

*** Test Cases ***
Sessao Gravada
    Clicar Em Submit Btn
    Digitar Em Email Input    valor@exemplo.com
```

### Anti-Patterns to Avoid

- **Generating `*** Test Cases ***` with inline steps:** Violates D-01. Steps belong in `.resource` as keywords.
- **Using `Set Selenium Timeout` in Browser compiler:** Wrong library. Browser uses `timeout=` parameter on `Wait For Elements State`.
- **Throwing on unresolvable selector:** Violates D-10. Always produce valid output; add to `Warnings`.
- **Emitting `\r\n` line endings:** RF files must use `\n`; use `new UTF8Encoding(false)` to avoid BOM (established pattern from Phase 6).
- **Calling `rfbrowser init` in test — not CI setup:** `rfbrowser init` downloads Node deps; must happen before test execution, in a CI step.

---

## ActionType → RF Keyword Mapping

### Browser Library (COMP-01 / COMP-03)

| ActionType | Pre-condition keyword | Interaction keyword | Payload key used |
|------------|----------------------|---------------------|------------------|
| Click | `Wait For Elements State    {sel}    visible    timeout=${TIMEOUT}s` | `Click    {sel}` | — |
| Type | `Wait For Elements State    {sel}    visible    timeout=${TIMEOUT}s` | `Type Text    {sel}    {value}` | `value` |
| Select | `Wait For Elements State    {sel}    visible    timeout=${TIMEOUT}s` | `Select Options By    {sel}    value    {value}` | `value` |
| Navigate | — (no wait needed) | `Go To    {url}` | `url` or `href` |
| Upload | `Wait For Elements State    {sel}    visible    timeout=${TIMEOUT}s` | `Upload File By Selector    {sel}    {path}` | `path` or `filename` |
| DragDrop | `Wait For Elements State    {src}    visible    timeout=${TIMEOUT}s` | `Drag And Drop    {src}    {target}` | `target` (selector value for drop target) |

### Browser Library AssertionType → RF Keyword

| AssertionType | RF Keyword |
|---------------|------------|
| TextEquals | `Get Text    {sel}    ==    {expected}` |
| TextContains | `Get Text    {sel}    contains    {expected}` |
| Visible | `Wait For Elements State    {sel}    visible` |
| Enabled | `Wait For Elements State    {sel}    enabled` |
| UrlMatches | `Get Url    ==    {pattern}` |

### SeleniumLibrary (COMP-04)

| ActionType | Interaction keyword | Payload key used |
|------------|---------------------|------------------|
| Click | `Click Element    {sel}` | — |
| Type | `Input Text    {sel}    {value}` | `value` |
| Select | `Select From List By Value    {sel}    {value}` | `value` |
| Navigate | `Go To    {url}` | `url` or `href` |
| Upload | `Choose File    {sel}    {path}` | `path` or `filename` |
| DragDrop | `Drag And Drop    {src}    {target}` | `target` |

### SeleniumLibrary AssertionType → RF Keyword

| AssertionType | RF Keyword |
|---------------|------------|
| TextEquals | `Element Text Should Be    {sel}    {expected}` |
| TextContains | `Element Should Contain    {sel}    {expected}` |
| Visible | `Element Should Be Visible    {sel}` |
| Enabled | `Element Should Be Enabled    {sel}` |
| UrlMatches | `Location Should Contain    {pattern}` |

---

## Selector Resolution

### SelectorStrategy → Browser Library selector string

| SelectorStrategy | Browser selector format |
|-----------------|------------------------|
| DataTestId | `css=[data-testid="{value}"]` |
| Id | `id={value}` |
| Role | `role={value}` |
| Css | `css={value}` |
| XPath | `xpath={value}` |

### SelectorStrategy → SeleniumLibrary locator string

| SelectorStrategy | SeleniumLibrary locator format |
|-----------------|-------------------------------|
| DataTestId | `css:[data-testid="{value}"]` |
| Id | `id:{value}` |
| Role | `css:[role="{value}"]` |
| Css | `css:{value}` |
| XPath | `xpath:{value}` |

**Fallback chain implementation (D-10):**

```csharp
// Source: D-10 (CONTEXT.md) + SelectorStrategy enum
private static readonly SelectorStrategy[] FallbackChain =
[
    SelectorStrategy.DataTestId,
    SelectorStrategy.Id,
    SelectorStrategy.Role,
    SelectorStrategy.Css,
    SelectorStrategy.XPath,
];

internal static (string selector, bool warned) Resolve(
    Selector selector,
    SelectorStrategy preferred,
    Func<SelectorStrategy, string> format)
{
    // Try preferred first, then fallback chain
    var chain = FallbackChain.Prepend(preferred).Distinct();
    string? lastValue = null;
    foreach (var strategy in chain)
    {
        if (selector.Values.TryGetValue(strategy, out var value))
            return (format(strategy, value), false);
        // track last strategy with any value for last-resort
    }
    // Fully exhausted — use any available value
    var anyStrategy = selector.Strategies.FirstOrDefault(s => selector.Values.ContainsKey(s));
    if (anyStrategy != default && selector.Values.TryGetValue(anyStrategy, out var fallback))
        return (format(anyStrategy, fallback), true); // warned=true
    return ("(unknown)", true);
}
```

---

## pt-BR Slug Normalization (Claude's Discretion)

**Algorithm for keyword name generation (D-02):**

```
Input:  ActionType.Click, selector value "submit-btn" (from data-testid)
Output: "Clicar Em Submit Btn"
```

**Recommended algorithm:**

1. Map ActionType to pt-BR verb:
   - Click → "Clicar Em"
   - Type → "Digitar Em"
   - Select → "Selecionar Em"
   - Navigate → "Navegar Para"
   - Upload → "Enviar Arquivo Em"
   - DragDrop → "Arrastar"

2. Take selector value (preferred strategy value):
   - Strip `data-testid="..."` wrapper if present — use just the attribute value
   - Replace `-`, `_`, `.` with space
   - Remove non-alphanumeric, non-space characters (excluding accented Latin chars)
   - Title-case each word using invariant culture (ASCII title-case; accented chars stay unchanged)
   - Trim to 64 characters total (RF keyword limit is not strict but long names are unwieldy)

3. Concatenate: `"{verb} {slug}"`

**Examples:**
- `click`, `data-testid="submit-btn"` → `Clicar Em Submit Btn`
- `type`, `id="email-input"` → `Digitar Em Email Input`
- `select`, `css=".dropdown"` → `Selecionar Em Dropdown`
- `navigate`, `url="https://example.com"` → `Navegar Para` (no element slug for Navigate)

**Conflict resolution:** If two steps produce identical keyword names (same verb + same slug), append a numeric suffix: `Clicar Em Submit Btn`, `Clicar Em Submit Btn 2`, etc.

**Accented chars:** Do not normalize/remove accented characters from selector values — Brazilian selectors rarely contain them, but if present, preserve them.

---

## Traceability Header (COMP-07)

RF files use `#` for comments. There is no structured "traceability" block in RF7 — the convention is comment lines at the file top.

**Recommended format (two-line header):**

```robot
# Generated by recrd {version} at {timestamp} for target {targetName}
# Source: {sessionFilePath} SHA-256: {sha256}
```

**Implementation notes:**
- `{version}` = `Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"`
- `{timestamp}` = ISO 8601 UTC: `DateTimeOffset.UtcNow.ToString("O")`
- `{sha256}` = computed if `options.OutputDirectory` has a known source file, otherwise passed in as a parameter. The public `CompileAsync(Session, CompilerOptions)` signature does not include a session file path, so SHA-256 must be passed via `CompilerOptions` extended with an optional `SourceFilePath` property, OR computed from the serialized `Session` bytes (deterministic serialization via `RecrdJsonContext`).

**Recommendation:** Extend `CompilerOptions` with `string? SourceFilePath { get; init; }`. If null, hash the `Session` JSON bytes. This avoids breaking `ITestCompiler.CompileAsync` contract.

---

## "Minimum RF Version" Declaration in Settings (COMP-08)

**Finding (LOW confidence — searched official user guide, RF7 release notes):** Robot Framework 7 does NOT have a built-in `Require Minimum Version` setting in `*** Settings ***`. The official user guide lists: `Library`, `Resource`, `Variables`, `Metadata`, `Suite Setup`, `Suite Teardown`, `Test Setup`, `Test Teardown`, `Test Tags`, `Test Timeout`, `Documentation`. No version constraint setting exists.

**Recommended approach for COMP-08:** Use `Metadata` key-value in the `.robot` Settings block:

```robot
*** Settings ***
Metadata    Generated-By    recrd {version}
Metadata    RF-Version      7
Resource    session.resource
Suite Setup      ...
```

This is valid RF7, appears in the HTML report, and satisfies the requirement to "declare minimum RF version." Alternatively, include the RF version requirement solely in the traceability header comment.

**Planner decision point:** Whether "minimum RF version" means a `Metadata` entry or just a comment is Claude's call — both are valid RF7. The requirement says "declares minimum RF version" without mandating a mechanism.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| SHA-256 computation | Custom hash loop | `System.Security.Cryptography.SHA256.HashData(bytes)` | BCL, zero deps |
| Selector format strings | Switch expressions per-strategy | `SelectorResolver` helper with strategy→format map | Testable, reusable across both compilers |
| RF file indentation | Manual spaces | 4-space indent constant; write via `TextWriter` helpers | RF requires consistent 4-space separation |
| Variable placeholder substitution in `.robot` | Custom regex | `${VARIABLE_NAME}` RF syntax — emit directly from `Variable.Name` | Variables are already in the AST |
| Random port for Kestrel fixture | Port scanner | `TcpListener(IPAddress.Any, 0)` → `.LocalEndpoint.Port` | OS guarantees availability |
| Process exit code check | Custom process monitor | `Process.WaitForExit()` + `process.ExitCode == 0` | Standard .NET API |

**Key insight:** RF code generation is pure string emission — the only complexity is the ActionType→keyword mapping table and the selector format map. Everything else is `TextWriter.WriteLineAsync` calls.

---

## Common Pitfalls

### Pitfall 1: Browser library requires `rfbrowser init` after `pip install`

**What goes wrong:** `pip install robotframework-browser` installs the Python package, but the Node.js gRPC dependencies are NOT installed. Running `robot` with Browser library fails with "Browser library not initialized."
**Why it happens:** Browser library communicates with Playwright via a gRPC server; node deps are separate from the pip install.
**How to avoid:** CI step must run `rfbrowser init` after `pip install robotframework-browser`. On ubuntu-latest, Node.js is pre-installed.
**Warning signs:** `robot` subprocess exits with non-zero and stderr contains "rfbrowser init".

### Pitfall 2: 4-space vs 2-space indentation in RF files

**What goes wrong:** RF requires consistent cell separation. Less than 2 spaces between keyword name and arguments causes parse errors. Community standard is 4 spaces.
**Why it happens:** Code generators often emit single spaces.
**How to avoid:** Use a `Separator = "    "` (4-space) constant everywhere. `Click    css=[data-testid="x"]` not `Click css=[data-testid="x"]`.
**Warning signs:** `robot` subprocess stderr shows "Parsing failed".

### Pitfall 3: TestServer in-memory vs real Kestrel port

**What goes wrong:** `WebApplicationFactory.CreateClient()` returns an `HttpClient` that hits an in-memory server with no real TCP port. The `robot` subprocess cannot reach it.
**Why it happens:** TestServer is designed for `HttpClient`-based tests, not external processes.
**How to avoid:** Start a real Kestrel host with `UseUrls("http://localhost:{port}")` using D-06 pattern. Use `IAsyncLifetime` to start/stop it around the test.
**Warning signs:** `robot` process times out on page load; Kestrel logs show no incoming connection.

### Pitfall 4: `Process.Start("robot", ...)` PATH resolution on Linux

**What goes wrong:** `robot` may not be on the PATH when `dotnet test` runs in CI because pip installs to `~/.local/bin` which may not be in PATH for the dotnet process.
**Why it happens:** `dotnet test` inherits the process environment; `~/.local/bin` is not always in CI PATH.
**How to avoid:** Set `PATH` in the CI workflow to include `~/.local/bin`, OR use `python3 -m robot` as the executable instead of `robot` directly.
**Warning signs:** `Process.Start` throws `Win32Exception` / `No such file or directory`.

### Pitfall 5: Keyword name collisions in page-object resource

**What goes wrong:** Two `ActionStep` records with the same ActionType and same selector value produce identical keyword names. RF rejects duplicate keyword definitions.
**Why it happens:** A user may click the same button twice (e.g., "Submit" to go to the next page, then again on a confirmation).
**How to avoid:** Track emitted keyword names in a `HashSet<string>`. Append numeric suffix on collision: `Clicar Em Submit Btn 2`.
**Warning signs:** `robot` subprocess exits with error "Keyword with name ... already exists".

### Pitfall 6: UTF-8 BOM in generated `.robot` files

**What goes wrong:** RF fails to parse `.robot` files with a BOM, especially on older Python versions.
**Why it happens:** `Encoding.UTF8` in .NET emits a BOM by default. `StreamWriter` uses this by default.
**How to avoid:** Use `new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)` — same pattern as Phase 6 established for `.recrd` files.
**Warning signs:** `robot` reports "File has BOM" or unexpected first character error.

### Pitfall 7: `Wait For Elements State` before Navigate and UrlMatches

**What goes wrong:** `Go To` (Navigate) does not have an element to wait for. `Get Url` (UrlMatches) also has no element. Emitting `Wait For Elements State` before these causes a selector-not-found error.
**Why it happens:** Blanket "insert Wait before every interaction" rule conflicts with selector-less steps.
**How to avoid:** Only emit `Wait For Elements State` for steps that have a real element selector: Click, Type, Select, Upload, DragDrop. NOT for Navigate or UrlMatches.
**Warning signs:** `robot` error "Wait For Elements State" with empty or invalid selector.

---

## Code Examples

### Compiling to file (public API)

```csharp
// Source: packages/Recrd.Core/Interfaces/ITestCompiler.cs
public sealed class RobotBrowserCompiler : ITestCompiler
{
    public string TargetName => "robot-browser";

    public async Task<CompilationResult> CompileAsync(Session session, CompilerOptions options)
    {
        var outDir = options.OutputDirectory;
        Directory.CreateDirectory(outDir);

        var resourcePath = Path.Combine(outDir, "session.resource");
        var robotPath = Path.Combine(outDir, "session.robot");
        var warnings = new List<string>();

        await using (var sw = new StreamWriter(resourcePath, false, new UTF8Encoding(false)))
            await ResourceWriter.WriteAsync(session, options, sw, warnings);

        await using (var sw = new StreamWriter(robotPath, false, new UTF8Encoding(false)))
            await RobotWriter.WriteAsync(session, options, sw);

        return new CompilationResult(
            generatedFiles: [resourcePath, robotPath],
            warnings: warnings,
            dependencyManifest: new Dictionary<string, string>
            {
                ["robotframework"] = "7.x",
                ["robotframework-browser"] = "19.x",
            });
    }
}
```

### E2E round-trip test fixture (Kestrel + Process.Start)

```csharp
// Source: D-06 (CONTEXT.md) + khalidabuhakmeh.com pattern
public class RoundTripTests : IAsyncLifetime
{
    private IHost? _host;
    private string _baseUrl = string.Empty;

    public async Task InitializeAsync()
    {
        var port = GetFreePort();
        _baseUrl = $"http://localhost:{port}";
        _host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(wb =>
            {
                wb.UseUrls(_baseUrl);
                wb.Configure(app =>
                {
                    app.Run(ctx =>
                    {
                        ctx.Response.ContentType = "text/html";
                        return ctx.Response.WriteAsync(FixtureHtml);
                    });
                });
            })
            .Build();
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null) await _host.StopAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BrowserCompiler_RoundTrip_RobotExitCodeZero()
    {
        // 1. Build a Session that covers all ActionTypes (D-04)
        var session = BuildFixtureSession(_baseUrl);
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // 2. Compile
        var compiler = new RobotBrowserCompiler();
        var result = await compiler.CompileAsync(session, new CompilerOptions
        {
            OutputDirectory = outDir,
            TimeoutSeconds = 30
        });

        // 3. Execute robot subprocess
        var psi = new ProcessStartInfo("python3", $"-m robot --outputdir {outDir} {outDir}/session.robot")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(psi)!;
        await process.WaitForExitAsync();

        Assert.Equal(0, process.ExitCode);
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
```

### Unit test pattern (StringWriter)

```csharp
// Source: tests/Recrd.Gherkin.Tests/FixedScenarioTests.cs pattern
[Fact]
public async Task BrowserCompiler_Click_EmitsWaitThenClick()
{
    var session = new Session(
        SchemaVersion: 1,
        Metadata: new SessionMetadata("id", DateTimeOffset.UtcNow, "chromium",
            new ViewportSize(1280, 720), "http://localhost"),
        Variables: [],
        Steps: [new ActionStep(
            ActionType: ActionType.Click,
            Selector: new Selector(
                [SelectorStrategy.DataTestId],
                new Dictionary<SelectorStrategy, string>
                    { [SelectorStrategy.DataTestId] = "submit-btn" }),
            Payload: new Dictionary<string, string>())]
    );
    var compiler = new RobotBrowserCompiler();
    var sw = new StringWriter();
    // internal helper — or use CompileAsync with temp dir
    await ResourceWriter.WriteAsync(session, new CompilerOptions(), sw, []);
    var output = sw.ToString();
    Assert.Contains("Wait For Elements State", output);
    Assert.Contains("css=[data-testid=\"submit-btn\"]", output);
    Assert.Contains("Click    css=[data-testid=\"submit-btn\"]", output);
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| SeleniumLibrary default implicit wait | Explicit waits via `Wait Until Element Is Visible` | SeleniumLibrary 5+ | D-08 decision overrides: use implicit wait only for Selenium compiler |
| Browser library `Click With Options` | `Click` (simplified) | Browser lib 14+ | Simpler keyword name; options passed via separate keywords if needed |
| RF `.robot` extension for resources | `.resource` extension | RF 3.1+ | `.resource` is now the recommended extension for resource files |
| `Force Tags` | `Test Tags` | RF 6.0 | `Force Tags` deprecated in RF 7 |
| `rfbrowser init` (node required) | `rfbrowser init` still required OR `[bb]` extra without Node | Browser 19.x | Use `pip install robotframework-browser[bb]` to avoid Node dependency in CI |

**Deprecated/outdated:**
- `Force Tags` in Settings: replaced by `Test Tags` in RF 7 — do not emit
- `Open Browser` in Browser library: SeleniumLibrary only; Browser library uses `New Browser` / `New Page`
- `Close Browser` in SeleniumLibrary: use `Close All Browsers`

---

## Open Questions

1. **SHA-256 source for traceability header**
   - What we know: `CompileAsync(Session, CompilerOptions)` has no `sessionFilePath` parameter.
   - What's unclear: Should we hash the serialized `Session` JSON bytes, or require `CompilerOptions.SourceFilePath`?
   - Recommendation: Add `string? SourceFilePath { get; init; }` to `CompilerOptions`. If null, hash `Session` bytes via `RecrdJsonContext`. This is Claude's discretion per CONTEXT.md.

2. **`rfbrowser init` in CI — Node.js availability on ubuntu-latest**
   - What we know: ubuntu-latest on GitHub Actions ships with Node 20. `rfbrowser init` requires Node.
   - What's unclear: Does `ubuntu-latest` in the repo's CI already have Node on PATH?
   - Recommendation: Add `- uses: actions/setup-node@v4` with `node-version: '20'` before `rfbrowser init` to guarantee it. LOW confidence on current ubuntu-latest Node version.

3. **Integration test placement: `Recrd.Integration.Tests` vs new sub-suite**
   - What we know: `Recrd.Integration.Tests.csproj` already references all 5 packages including `Recrd.Compilers`. The E2E test (D-06) logically lives there.
   - What's unclear: Whether adding `Microsoft.AspNetCore.TestHost` to `Recrd.Integration.Tests.csproj` pulls in too many transitive deps.
   - Recommendation: Add `Microsoft.AspNetCore.TestHost` to `Recrd.Integration.Tests.csproj`. No new project needed. This is Claude's discretion.

4. **Upload keyword in Browser library**
   - What we know: Browser library has `Upload File By Selector` keyword (confirmed via GitHub source search).
   - What's unclear: Exact parameter name for the file path (`path` vs `filePath` vs positional).
   - Recommendation: Verify at implementation time via `rfbrowser init` and `rfbrowser show-trace`. Mark as LOW confidence; write unit test that checks the emitted keyword name matches actual RF keyword.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Python 3 | pip / robotframework | Yes | 3.13.12 | — |
| pip3 | RF package install | Yes | 25.3 | — |
| robot (RF CLI) | E2E subprocess | No | — | Install via `pip install robotframework` |
| rfbrowser | Browser library init | No | — | Install via `pip install robotframework-browser && rfbrowser init` |
| Node.js | rfbrowser init | Unknown | — | `actions/setup-node@v4` in CI |
| .NET 10 SDK | Build/test | Assumed | 10.x | — |

**Missing dependencies with no fallback:**
- `robot` CLI — required for E2E round-trip. Must be added to CI before integration test step.
- `rfbrowser init` — required for Browser library. Must run after `pip install robotframework-browser`.

**Missing dependencies with fallback:**
- Node.js — `actions/setup-node@v4` guarantees availability in CI. Not needed on local dev if using `[bb]` install variant.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 |
| Config file | `tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj` (existing) |
| Quick run command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Compilers.Tests --no-build --filter "Category!=Integration"` |
| Full suite (with E2E) | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Integration.Tests --no-build --filter "Category=Integration"` |
| Coverage gate command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Include="[Recrd.Compilers]*" /p:ExcludeByFile="**/obj/**/*.cs" /p:Threshold=90 /p:ThresholdType=line /p:ThresholdStat=minimum` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Test File | Automated Command |
|--------|----------|-----------|-----------|-------------------|
| COMP-01 | `RobotBrowserCompiler.CompileAsync` emits `.robot` + `.resource` files in RF7 format | unit | `BrowserCompilerOutputTests.cs` | Quick run command |
| COMP-02 | Browser compiler uses `css=[data-testid="..."]` preferred; falls back per `PreferredSelectorStrategy` | unit | `BrowserCompilerSelectorTests.cs` | Quick run command |
| COMP-03 | Browser compiler emits `Wait For Elements State` before each element-bearing interaction keyword | unit | `BrowserCompilerWaitTests.cs` | Quick run command |
| COMP-04 | `RobotSeleniumCompiler.CompileAsync` emits `.robot` + `.resource` files in RF7 format | unit | `SeleniumCompilerOutputTests.cs` | Quick run command |
| COMP-05 | Selenium compiler uses `id:` preferred, falls to `css:` then `xpath:` | unit | `SeleniumCompilerSelectorTests.cs` | Quick run command |
| COMP-06 | Selenium compiler emits implicit wait with `TimeoutSeconds` from `CompilerOptions` | unit | `SeleniumCompilerWaitTests.cs` | Quick run command |
| COMP-07 | Both compilers emit traceability header with version, timestamp, SHA-256, target name | unit | `TraceabilityHeaderTests.cs` | Quick run command |
| COMP-08 | Both compilers emit `*** Settings ***` block with RF version declaration (Metadata or comment) | unit | `BrowserCompilerOutputTests.cs`, `SeleniumCompilerOutputTests.cs` | Quick run command |
| COMP-09 | `CompilationResult` has non-null `GeneratedFiles`, `Warnings`, `DependencyManifest` | unit | `CompilationResultTests.cs` | Quick run command |
| COMP-10 | `record → compile → execute` round-trip: `robot` subprocess exits 0 (both compilers) | integration | `RoundTripTests.cs` (Integration.Tests) | Full suite command |

### Sampling Rate

- **Per task commit:** `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Compilers.Tests --no-build --filter "Category!=Integration"`
- **Per wave merge:** Full suite including Integration.Tests (requires RF installed)
- **Phase gate:** Full suite green (including COMP-10 E2E) before `/gsd:verify-work`

### Wave 0 Gaps (files that must be created in Wave 0 / TDD red phase)

- [ ] `tests/Recrd.Compilers.Tests/BrowserCompilerOutputTests.cs` — covers COMP-01, COMP-08
- [ ] `tests/Recrd.Compilers.Tests/BrowserCompilerSelectorTests.cs` — covers COMP-02
- [ ] `tests/Recrd.Compilers.Tests/BrowserCompilerWaitTests.cs` — covers COMP-03
- [ ] `tests/Recrd.Compilers.Tests/SeleniumCompilerOutputTests.cs` — covers COMP-04, COMP-08
- [ ] `tests/Recrd.Compilers.Tests/SeleniumCompilerSelectorTests.cs` — covers COMP-05
- [ ] `tests/Recrd.Compilers.Tests/SeleniumCompilerWaitTests.cs` — covers COMP-06
- [ ] `tests/Recrd.Compilers.Tests/TraceabilityHeaderTests.cs` — covers COMP-07
- [ ] `tests/Recrd.Compilers.Tests/CompilationResultTests.cs` — covers COMP-09
- [ ] `tests/Recrd.Compilers.Tests/KeywordNameBuilderTests.cs` — covers D-02 slug normalization
- [ ] `tests/Recrd.Integration.Tests/RoundTripTests.cs` — covers COMP-10 (E2E)
- [ ] `Recrd.Integration.Tests.csproj` — needs `Microsoft.AspNetCore.TestHost` PackageReference added
- [ ] `ci.yml` — needs pip install + rfbrowser init step added before integration test step

---

## Project Constraints (from CLAUDE.md)

- All `dotnet` commands must be prefixed with `DOTNET_SYSTEM_NET_DISABLEIPV6=1`
- Never run the dev server — instruct user to run
- Use Context7 MCP for library documentation (used where available; timed out for Browser library docs, fell back to GitHub README + PyPI)
- TDD mandate (D-11): all tests committed red before implementation
- Coverage ≥ 90% on `Recrd.Compilers`
- CI coverage gate already defined in `ci.yml` for `Recrd.Compilers`
- Playwright browser install: use `playwright.sh install` (not `.ps1`) on Linux
- Generated files: UTF-8 no-BOM using `new UTF8Encoding(false)`

---

## Sources

### Primary (HIGH confidence)

- Direct file reads: `ITestCompiler.cs`, `CompilationResult.cs`, `CompilerOptions.cs`, `ActionType.cs`, `AssertionType.cs`, `SelectorStrategy.cs`, `Selector.cs`, `Session.cs`, `SessionMetadata.cs`, `GherkinGenerator.cs`, `StepTextRenderer.cs` — all confirmed from project source
- `ci.yml` — coverage gate pattern, Playwright install pattern confirmed
- [Robot Framework User Guide (latest)](https://robotframework.org/robotframework/latest/RobotFrameworkUserGuide.html) — Settings block contents, `.resource` file format, comment syntax
- `pip3 index versions` — confirmed package versions: robotframework 7.4.2, robotframework-browser 19.13.0, robotframework-seleniumlibrary 6.8.0

### Secondary (MEDIUM confidence)

- [GitHub: robotframework-browser README](https://github.com/MarketSquare/robotframework-browser/blob/main/README.md) — installation procedure, `rfbrowser init`, keyword examples
- [GitHub: robotframework-browser waiter.py](https://github.com/MarketSquare/robotframework-browser/blob/main/Browser/keywords/waiter.py) — `Wait For Elements State` signature and ElementState enum
- [robotframework.org SeleniumLibrary docs](https://robotframework.org/SeleniumLibrary/SeleniumLibrary.html) — SeleniumLibrary keyword list, selector syntax
- [khalidabuhakmeh.com E2E pattern](https://khalidabuhakmeh.com/end-to-end-test-with-aspnet-core-xunit-and-playwright) — Kestrel real-port + IAsyncLifetime pattern
- [Microsoft Learn: ASP.NET Core integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) — WebApplicationFactory, TestServer patterns
- [GitHub: RF7 release notes](https://github.com/robotframework/robotframework/blob/master/doc/releasenotes/rf-7.0.rst) — new features, breaking changes

### Tertiary (LOW confidence)

- WebSearch results for Browser library keyword mapping — cross-verified with GitHub source; HIGH confidence for `Click`, `Type Text`, `Wait For Elements State`; LOW for `Upload File By Selector` exact signature
- RF7 minimum version declaration — searched user guide, release notes; no native mechanism found; LOW confidence on `Metadata` as correct approach

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versions confirmed via PyPI index on 2026-04-05
- Architecture patterns: HIGH — mirrors existing GherkinGenerator pattern in the codebase
- ActionType→keyword mapping: HIGH (Click, Type, Navigate, Select), MEDIUM (Upload, DragDrop — Browser library specific keywords)
- Pitfalls: HIGH — all pitfalls verified from official docs or project history
- E2E test pattern: MEDIUM — confirmed via established .NET patterns; subprocess untested in this specific project

**Research date:** 2026-04-05
**Valid until:** 2026-05-05 (stable libraries; RF7 and SeleniumLibrary are not fast-moving)
