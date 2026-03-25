# Product Requirements Document — `recrd` v1.1

| Field | Value |
|---|---|
| **Author** | Architecture Team |
| **Status** | Draft |
| **Last Revised** | 2026-03-25 |
| **Target Runtime** | .NET 10 (LTS) |
| **Supersedes** | PRD v1.0 |

---

## 1. Product Overview

**Name:** `recrd`

**One-liner:** A .NET 10 CLI that records browser interactions, emits pt-BR Gherkin, and compiles executable Robot Framework test suites with native data-driven testing.

**Philosophy:** Test-Driven Development from day zero; plugin-oriented architecture exposed through a stable public API surface; deterministic, reproducible output — the same recording plus the same data must always produce byte-identical test artifacts.

---

## 2. Objectives & Success Metrics

| Objective | Measurable Target |
|---|---|
| Eliminate E2E boilerplate | ≥ 80% reduction in lines of hand-written Robot keywords for a standard CRUD flow |
| Data-Driven Testing at scale | Record once, replay against ≥ 10,000 rows from a 50 MB CSV without OOM or blocking the main thread |
| BDD standardization | 100% of generated artifacts include a valid `.feature` file in pt-BR Gherkin |
| Selector resilience | ≥ 3 locator strategies per captured element, ranked by stability (`data-testid` > `id` > CSS > XPath) |
| Compilation fidelity | Round-trip: `record → compile → execute` passes on a green-field app with zero manual edits |

---

## 3. Functional Scope

### 3.1 Interactive Recording Engine

#### 3.1.1 Browser Communication

The recording engine communicates with Chromium via the **Chrome DevTools Protocol (CDP)** over a WebSocket connection managed by `Playwright .NET` (Microsoft.Playwright). Playwright was chosen over raw CDP because it provides transport-level reconnection, multi-browser support (Chromium, Firefox, WebKit), and a stable API surface that absorbs upstream protocol changes.

The engine operates two CDP channels simultaneously:

- **Page channel** — injects a recording overlay (a thin JavaScript agent) into every frame via `Page.EvaluateAsync`. This agent listens to DOM events (`click`, `input`, `change`, `blur`, `submit`, navigation) and forwards structured `RecordedEvent` payloads to the host through `Page.ExposeBindingAsync`.
- **Inspector channel** — drives a secondary side-panel (Playwright BrowserContext with `--app` flag) that renders the live event stream, variable tagging controls, and the assertion builder. Communication between the inspector panel and the main recording context uses an in-process `Channel<RecordedEvent>` (System.Threading.Channels) — no HTTP, no sockets, no serialization overhead.

#### 3.1.2 Session Lifecycle

```
recrd start [--browser chromium|firefox|webkit] [--headed] [--viewport 1280x720]
  │
  ├─ 1. Launch clean BrowserContext (incognito, no cookies, no cache)
  ├─ 2. Inject recording agent into target page
  ├─ 3. Open inspector side-panel
  │
  │   ◄── user interacts with the page ──►
  │
  ├─ 4. recrd pause   → freezes event capture; enables assertion mode
  ├─ 5. recrd resume  → returns to recording mode
  └─ 6. recrd stop    → flushes AST to disk as .recrd session file (JSON)
```

#### 3.1.3 Event Capture

Events recorded (non-exhaustive; extensible via `IEventInterceptor` plugin):

| DOM Event | Captured Payload |
|---|---|
| `click` | target selectors, coordinates, modifier keys |
| `input` / `change` | target selectors, previous value, new value, input type |
| `select` | target selectors, selected option value and text |
| `hover` | target selectors (only when explicitly tagged by user) |
| `navigation` | URL, trigger type (link / form / pushState / replaceState) |
| `file upload` | target selectors, file name, MIME type, size (file bytes not stored) |
| `drag-and-drop` | source selectors, target selectors, offset |

Each event is wrapped in a `RecordedEvent` envelope:

```csharp
public sealed record RecordedEvent(
    Guid       Id,
    long       TimestampMs,     // monotonic, relative to session start
    string     EventType,       // "click", "input", "navigation", ...
    Selector[] Selectors,       // ordered by priority
    JsonNode?  Payload,         // event-type-specific data
    string?    DataVariable     // null unless user tagged this field
);
```

#### 3.1.4 Live Variable Templating

During recording, the user can right-click any input field and select **"Tag as Variable"** from the inspector panel. This replaces the literal captured value with a named placeholder (e.g., `<username>`). Variables become the columns of the `Exemplos` table when data is injected later.

Naming rules: `^[a-z][a-z0-9_]{0,63}$`. Duplicates within a session are rejected at tag-time with a visible warning in the inspector panel.

#### 3.1.5 Visual Assertions

When recording is paused (`recrd pause`), the user can right-click any element to insert an assertion step. Supported assertion types for v1.0:

| Assertion | Robot Browser Keyword | Robot Selenium Keyword |
|---|---|---|
| Text equals | `Get Text  selector  ==  expected` | `Element Text Should Be  locator  expected` |
| Text contains | `Get Text  selector  *=  expected` | `Element Should Contain  locator  expected` |
| Element visible | `Get Element State  selector  visible  ==  true` | `Element Should Be Visible  locator` |
| Element enabled | `Get Element State  selector  enabled  ==  true` | `Element Should Be Enabled  locator` |
| URL matches | `Get Url  ==  expected` | `Location Should Be  expected` |

Additional assertion types will be added through the `IAssertionProvider` plugin interface.

---

### 3.2 Abstract Syntax Tree (AST)

The AST is the canonical intermediate representation. Every upstream component (recording engine, future import adapters) writes to it; every downstream component (Gherkin generator, compilers) reads from it. No downstream component ever touches raw DOM events.

#### 3.2.1 AST Schema (simplified)

```
Session
├── metadata: { id, createdAt, browserEngine, viewportSize, baseUrl }
├── variables: Variable[]          // declared data-variable placeholders
└── steps: Step[]
        ├── ActionStep             // click, type, select, navigate, upload, drag
        ├── AssertionStep          // text, visibility, state, URL
        └── GroupStep              // logical grouping for Gherkin "Dado/Quando/Então" blocks
```

A `GroupStep` lets the user (or a future AI classifier) organize raw actions into BDD sections. When no grouping is provided, the default heuristic is: first navigation → `Dado`; interactions → `Quando`; assertions → `Então`.

#### 3.2.2 Persistence Format

Sessions are serialized as `.recrd` files (JSON, UTF-8, no BOM). The schema is versioned with a `"schemaVersion": 1` field at the root. Future breaking changes increment this version; the CLI refuses to process files with an unknown schema version and prints an upgrade command.

---

### 3.3 pt-BR Gherkin Generator

The Gherkin engine (`Recrd.Gherkin`) walks the AST and emits a `.feature` file. The mapping between AST nodes and Gherkin keywords follows these rules:

| AST Structure | Gherkin Output |
|---|---|
| Session with zero variables | `Cenário` (single scenario) |
| Session with ≥ 1 variable | `Esquema do Cenário` + `Exemplos` table |
| GroupStep typed `given` | `Dado` / `E` (subsequent) |
| GroupStep typed `when` | `Quando` / `E` (subsequent) |
| GroupStep typed `then` | `Então` / `E` (subsequent) |
| ActionStep (no group) | `Quando` (first) / `E` (subsequent) |
| AssertionStep (no group) | `Então` (first) / `E` (subsequent) |

#### 3.3.1 Data Injection

When `--data <file>` is provided:

1. The data provider (`IDataProvider`) parses the file into `IReadOnlyList<IReadOnlyDictionary<string, string>>`.
2. The Gherkin engine validates that every declared variable in the AST has a matching column in the data.
3. Mismatches produce a hard error (missing column) or a warning (extra column) written to stderr.
4. The `Exemplos` table is appended to the feature file using pipe-delimited Gherkin table syntax, with columns ordered to match their first appearance in the scenario body.

Supported data formats (v1.0):

| Format | Provider | Notes |
|---|---|---|
| CSV | `CsvDataProvider` | RFC 4180 compliant; BOM-tolerant; configurable delimiter via `--csv-delimiter` |
| JSON | `JsonDataProvider` | Expects a root-level JSON array of flat objects |

Future formats (Excel, YAML, database query) will implement `IDataProvider` without changes to the generator.

---

### 3.4 Compilers

Compilers implement `ITestCompiler` and translate the AST into an executable test suite for a specific technology stack.

```csharp
public interface ITestCompiler
{
    string TargetName { get; }          // "robot-browser", "robot-selenium"
    Task<CompilationResult> CompileAsync(Session session, CompilerOptions options);
}
```

`CompilationResult` contains the list of generated files (`.robot`, `.resource`, `__init__.robot`), any warnings, and the resolved dependency manifest (e.g., required Robot Framework libraries).

#### 3.4.1 robot-browser

| Aspect | Detail |
|---|---|
| Technology | Robot Framework + Browser library (Playwright under the hood) |
| Selector strategy | Preferred: `css=...` with `data-testid`. Fallback chain configurable via `--selector-strategy` |
| Async handling | Automatic `Wait For Elements State` before interaction keywords |
| Network interception | Supported via `New Promise` / `Wait For Response` keywords (opt-in with `--intercept` flag) |
| Output | `.robot` suite + `.resource` file with reusable keywords |

#### 3.4.2 robot-selenium

| Aspect | Detail |
|---|---|
| Technology | Robot Framework + SeleniumLibrary (Python Selenium) |
| Selector strategy | Preferred: `id:...`. Fallback: `css:...`, then `xpath:...` |
| Async handling | Configurable implicit/explicit waits via `--timeout` (default: 10s) |
| Output | `.robot` suite + `.resource` file |

Both compilers emit a header comment block in every generated file containing the `recrd` version, compilation timestamp, source `.recrd` session hash (SHA-256), and the compiler target name. This guarantees traceability from any `.robot` file back to its source recording.

---

### 3.5 CLI Command Reference

```
recrd start [options]          Launch recording session
  --browser <engine>           chromium (default) | firefox | webkit
  --headed                     Show browser window (default: true)
  --viewport <WxH>             Viewport size (default: 1280x720)
  --base-url <url>             Starting URL (optional)

recrd pause                    Pause recording, enable assertion mode
recrd resume                   Resume recording
recrd stop                     End session, write .recrd file

recrd compile <session.recrd> [options]
  --target <compiler>          robot-browser (default) | robot-selenium
  --data <file>                CSV or JSON file for data injection
  --csv-delimiter <char>       Delimiter for CSV (default: ,)
  --out <directory>            Output directory (default: ./output)
  --selector-strategy <chain>  Comma-separated priority: data-testid,id,css,xpath
  --timeout <seconds>          Implicit wait timeout (Selenium only, default: 10)
  --intercept                  Enable network interception keywords (Browser only)

recrd validate <session.recrd> Validate AST schema and variable consistency
recrd version                  Print version and runtime info
recrd plugins list             List installed compiler/data-provider plugins
recrd plugins install <pkg>    Install a plugin from NuGet
```

---

### 3.6 VS Code Extension

The VS Code extension (`apps/vscode-extension/`) is a thin UI wrapper over the CLI — it does not contain business logic.

| Feature | Implementation |
|---|---|
| Start/Stop recording | Invokes `recrd start` / `recrd stop` via `child_process.spawn` |
| Compiler target picker | QuickPick dropdown → passes `--target` to `recrd compile` |
| Data file attachment | File picker → passes `--data` to `recrd compile` |
| Live preview | Watches the `.recrd` session file via `fs.watch`, runs incremental compile on change, and renders the `.feature` + `.robot` output in a WebView panel |
| Status bar | Shows recording state (recording / paused / idle) and elapsed time |

The extension communicates with the CLI exclusively through its stdout/stderr streams and exit codes. No proprietary IPC protocol is introduced.

---

## 4. Architecture

### 4.1 Monorepo Structure

```
recrd/
├── apps/
│   ├── recrd-cli/                   .NET 10 console app (entry point)
│   └── vscode-extension/            TypeScript VS Code extension
├── packages/
│   ├── Recrd.Core/                  AST types, interfaces, Channel infra
│   │   ├── Ast/                     Session, Step, Selector, Variable models
│   │   ├── Interfaces/              IRecorderEngine, ITestCompiler, IDataProvider,
│   │   │                            IEventInterceptor, IAssertionProvider
│   │   └── Pipeline/                Recording pipeline orchestration
│   ├── Recrd.Recording/             CDP/Playwright recording engine
│   ├── Recrd.Data/                  CSV and JSON data providers
│   ├── Recrd.Gherkin/               AST → pt-BR .feature generator
│   └── Recrd.Compilers/             robot-browser and robot-selenium compilers
├── plugins/                         Example third-party compiler/data plugins
├── tests/
│   ├── Recrd.Core.Tests/
│   ├── Recrd.Recording.Tests/
│   ├── Recrd.Data.Tests/
│   ├── Recrd.Gherkin.Tests/
│   ├── Recrd.Compilers.Tests/
│   └── Recrd.Integration.Tests/     Full record → compile → execute round-trips
├── docs/                            Documentation site (Docusaurus or similar)
├── .github/                         CI workflows
├── Directory.Build.props            Shared MSBuild properties
├── recrd.sln                        Solution file
└── README.md
```

Key changes from v1.0 structure:

- `Recrd.Recording` is extracted from `Recrd.Core` because the Playwright dependency is heavy (~200 MB of browser binaries); consumers who only need compilation (CI pipelines) should not pull it in.
- `tests/` lives at the root as a peer to `packages/` to keep test projects from inflating production package sizes.
- `plugins/` directory provides example implementations for third-party authors.

### 4.2 Dependency Graph

```
recrd-cli
  ├── Recrd.Core          (always)
  ├── Recrd.Recording     (only for `start/pause/resume/stop` commands)
  ├── Recrd.Data          (only when --data is provided)
  ├── Recrd.Gherkin       (always for compile)
  └── Recrd.Compilers     (always for compile)

Recrd.Recording  →  Recrd.Core
Recrd.Data       →  Recrd.Core
Recrd.Gherkin    →  Recrd.Core
Recrd.Compilers  →  Recrd.Core, Recrd.Gherkin
```

No circular dependencies are permitted. `Recrd.Core` depends on zero other `Recrd.*` packages. This is enforced at CI via `dotnet dependency-graph` analysis.

### 4.3 Plugin Architecture

Plugins are .NET assemblies distributed as NuGet packages following the naming convention `Recrd.Plugin.*`. The CLI discovers plugins at startup by scanning `--plugin-dir` (default: `~/.recrd/plugins/`) for assemblies that export types implementing `ITestCompiler`, `IDataProvider`, `IEventInterceptor`, or `IAssertionProvider`.

Plugin loading uses `AssemblyLoadContext` isolation to prevent version conflicts between the host and plugins. Plugins declare their required `Recrd.Core` version range via NuGet dependency metadata; the host refuses to load plugins built against an incompatible major version.

### 4.4 Data Flow

```
┌──────────────┐     RecordedEvent      ┌──────────┐
│  Browser      │ ──── Channel<T> ────► │  AST      │
│  (Playwright) │                        │  Builder  │
└──────────────┘                        └────┬─────┘
                                              │
                                         .recrd file (JSON)
                                              │
                    ┌─────────────────────────┼─────────────────────────┐
                    │                         │                         │
              ┌─────▼──────┐          ┌───────▼───────┐         ┌──────▼──────┐
              │ Gherkin     │          │ Data Provider │         │  Compiler   │
              │ Generator   │          │ (CSV/JSON)    │         │  (Robot)    │
              └─────┬──────┘          └───────┬───────┘         └──────┬──────┘
                    │                         │                        │
               .feature file            row data merged          .robot suite
                                       into Esquema              .resource file
```

---

## 5. Test-Driven Development Strategy

### 5.1 Test Pyramid

| Layer | Scope | Tool | Target Coverage |
|---|---|---|---|
| Unit | Single class/method in isolation | xUnit + Moq | ≥ 90% line coverage on `Recrd.Core`, `Recrd.Data`, `Recrd.Gherkin`, `Recrd.Compilers` |
| Integration | Package interactions (e.g., AST → Gherkin → Compiler pipeline) | xUnit + TestContainers | ≥ 80% of documented use cases |
| E2E | Full `recrd start → stop → compile → execute` against a fixture web app | xUnit + Playwright (for the fixture app) + Robot Framework (execution verification) | Critical path per compiler target |

### 5.2 Red-Green-Refactor by Package

**Recrd.Data.Tests**

- `CsvDataProvider_MalformedFile_ThrowsDataParseException`: Malformed CSV (missing closing quote, mismatched column count) must throw `DataParseException` with line number and description.
- `CsvDataProvider_BomEncoded_ParsesCorrectly`: UTF-8 BOM prefix must not appear as garbage in the first column name.
- `CsvDataProvider_LargeFile_StreamsWithoutOOM`: 50 MB file parsed with peak heap delta ≤ 100 MB (verified via `GC.GetTotalMemory`).
- `JsonDataProvider_NonArrayRoot_ThrowsDataParseException`: Root-level object (not array) must throw with a clear message.
- `JsonDataProvider_NestedObjects_FlattenedWithDotNotation`: `{ "user": { "name": "Gil" } }` yields column `user.name`.

**Recrd.Gherkin.Tests**

- `Generator_NoVariables_EmitsCenario`: AST without tagged variables produces `Cenário`, not `Esquema do Cenário`.
- `Generator_WithVariables_EmitsEsquemaDoCenario`: AST with tagged variables produces `Esquema do Cenário` and an `Exemplos` table.
- `Generator_VariableMismatch_ThrowsGherkinException`: Variable `<email>` in AST but no `email` column in data → hard error.
- `Generator_StepGrouping_DefaultHeuristic`: Navigation → `Dado`, actions → `Quando`, assertions → `Então`.
- `Generator_Output_IsIdempotent`: Same AST + same data = byte-identical `.feature` output across runs.

**Recrd.Compilers.Tests**

- `RobotBrowserCompiler_ClickStep_EmitsClickKeyword`: AST click → `Click  css=[data-testid="submit"]`.
- `RobotBrowserCompiler_TextAssertion_EmitsGetText`: Assertion → `Get Text  css=...  ==  expected`.
- `RobotSeleniumCompiler_ClickStep_EmitsClickElement`: AST click → `Click Element  id:submit`.
- `RobotSeleniumCompiler_TextAssertion_EmitsElementTextShouldBe`: Assertion → `Element Text Should Be  id:...  expected`.
- `BothCompilers_Output_IncludesTraceabilityHeader`: Header comment with version, timestamp, session SHA-256.

**Recrd.Recording.Tests**

- `RecordingEngine_ClickCapture_EmitsRecordedEvent`: Playwright injects a click on a fixture page; the Channel receives a `RecordedEvent` of type `click` with valid selectors.
- `RecordingEngine_CleanContext_NoCookies`: New session starts with zero cookies and empty localStorage.
- `RecordingEngine_MultipleSelectors_RankedByStability`: Captured element with `data-testid`, `id`, and class produces selectors in that order.

### 5.3 CI Pipeline

```
push / PR
  ├── dotnet restore
  ├── dotnet build --no-restore
  ├── dotnet test --no-build --collect:"XPlat Code Coverage"
  ├── coverage gate (fail if < thresholds)
  ├── dotnet format --verify-no-changes (code style)
  ├── mutation testing (Stryker.NET) on Recrd.Core — weekly scheduled run
  └── (main branch only) dotnet pack → NuGet push (pre-release tag)
```

---

## 6. Non-Functional Requirements

### 6.1 Performance

| Metric | Target | How Measured |
|---|---|---|
| Recording latency | < 50ms from DOM event to AST node written | Stopwatch in `RecordedEvent` pipeline |
| Compile time (1000-step session) | < 3 seconds | CI benchmark test |
| CSV/JSON parsing (50 MB) | < 10 seconds, peak heap delta ≤ 100 MB | BenchmarkDotNet |
| CLI cold start | < 500 ms to first prompt | `time recrd version` |

### 6.2 Reliability

- **State isolation**: Every recording session launches a fresh BrowserContext. No shared profile, no cookie carryover.
- **Crash recovery**: The recording agent writes incremental `.recrd.partial` snapshots every 30 seconds. If the process is killed, `recrd recover` reconstructs the session from the latest snapshot.
- **Selector resilience**: Minimum 3 locator strategies per element. Ranking: `data-testid` > `id` > `role`-based > CSS class chain > XPath. Configurable via `--selector-strategy`.

### 6.3 Security

- The recording engine executes in a sandboxed BrowserContext with no access to the user's real browser profile, cookies, or credentials.
- `.recrd` session files may contain PII (usernames, emails typed during recording). A `recrd sanitize <session.recrd>` command strips all literal values, keeping only variable placeholders and structural data.
- Plugin assemblies are loaded in an isolated `AssemblyLoadContext`. Plugins cannot access the host's filesystem beyond their own directory without explicit `IPluginPermission` grant.
- No telemetry or network calls are made by the CLI unless explicitly opted-in via `--telemetry`.

### 6.4 Observability

- Structured logging via `Microsoft.Extensions.Logging` with `ILogger<T>` injection across all packages.
- Default sink: console (human-readable). Configurable sinks via `--log-output json` for machine-parseable output.
- Verbosity levels: `--verbosity quiet|normal|detailed|diagnostic` (maps to LogLevel).
- The recording session emits a summary report on `recrd stop`: total events captured, variables declared, session duration, and file sizes.

### 6.5 Compatibility & Distribution

| Dimension | Requirement |
|---|---|
| OS | Windows 10+, macOS 12+, Ubuntu 20.04+ (same targets as Playwright .NET) |
| Runtime | Self-contained single-file publish (no .NET SDK required on target machine) |
| Distribution | GitHub Releases (binaries), NuGet (library packages), Homebrew tap, winget manifest, AUR (community) |
| VS Code Extension | VS Code Marketplace, minimum VS Code 1.85 |
| Backwards compat | `.recrd` schema v1 files will be loadable by all future CLI versions. Breaking schema changes require a new major version of the schema with a documented migration path. |

---

## 7. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Playwright .NET lags behind Node.js Playwright | Medium | Medium | Pin to stable release; abstract via `IRecorderEngine` so a raw CDP adapter can replace Playwright if needed |
| CDP protocol changes break event capture | Low | High | Recording agent is injected JS (DOM-level), not CDP-level; CDP is only used for transport, not event interception |
| Robot Framework keyword syntax changes between versions | Low | Medium | Compiler emits a `*** Settings ***` header declaring minimum RF version; integration tests run against RF 6.x and 7.x |
| pt-BR Gherkin edge cases (accents, encoding) | Medium | Low | All text processing uses `System.Text.Runes` / `StringInfo` for grapheme-correct handling; `.feature` files are always UTF-8 |
| Large data files cause memory pressure | Medium | Medium | `IDataProvider` contract mandates `IAsyncEnumerable<T>` streaming; batch size capped at 1000 rows in-memory |
| Plugin stability | Medium | Medium | Isolated `AssemblyLoadContext`; unhandled plugin exceptions are caught, logged, and surfaced as compilation warnings — never crash the host |

---

## 8. Open Questions

These items require a decision before development begins:

1. **Should `recrd` support importing `.side` files (Selenium IDE)?** This would accelerate adoption by allowing migration of existing recordings, but adds a complex parser with no test coverage guarantee on Selenium IDE's format stability.
2. **Multi-tab recording** — v1.0 captures a single page. Supporting multi-tab flows (e.g., OAuth popups) significantly increases AST complexity. Defer to v1.1 or include a constrained version?
3. **AI-assisted step grouping** — the default `Dado/Quando/Então` heuristic is brittle. A local ML classifier (ONNX Runtime) could infer groupings from DOM context. Worth the dependency for v1.0 or better as a plugin?
4. **Robot Framework 7 exclusive or 6+7 support?** RF7 has breaking keyword syntax changes. Supporting both doubles compiler test surface.

---

## 9. Phased Roadmap

### Phase 1 — Foundation (Weeks 1–4)

- `Recrd.Core`: AST types, all interfaces (`ITestCompiler`, `IDataProvider`, `IEventInterceptor`, `IAssertionProvider`), Channel pipeline.
- `Recrd.Data`: CSV and JSON providers with full TDD coverage.
- `Recrd.Gherkin`: AST → `.feature` generator with variable/data merging.
- CI pipeline: build, test, coverage gates.

### Phase 2 — Recording Engine (Weeks 5–8)

- `Recrd.Recording`: Playwright .NET integration, event capture, selector extraction, variable tagging.
- Inspector side-panel (minimal viable UI).
- `.recrd` session file read/write with schema validation.
- Integration tests against a fixture web app (static HTML + simple SPA).

### Phase 3 — Compilers (Weeks 9–11)

- `RobotBrowserCompiler`: Full keyword emission, async wait injection, `.resource` extraction.
- `RobotSeleniumCompiler`: Equivalent coverage.
- Round-trip E2E tests: record → compile → execute against fixture app.

### Phase 4 — CLI Polish & Distribution (Week 12–13)

- CLI argument parsing, help text, error formatting.
- `recrd validate`, `recrd sanitize`, `recrd recover` commands.
- Self-contained publish for Windows, macOS, Linux.
- Homebrew tap, GitHub Releases automation.

### Phase 5 — VS Code Extension (Weeks 14–16)

- Extension scaffolding (Yeoman generator).
- Start/stop, target picker, data file picker, live preview WebView.
- Marketplace publishing pipeline.

### Phase 6 — Hardening (Ongoing)

- Mutation testing (Stryker.NET) monthly runs.
- Performance benchmarks tracked in CI (BenchmarkDotNet).
- Plugin SDK documentation and example plugins.
- Community feedback triage.

---

## Appendix A: Glossary

| Term | Definition |
|---|---|
| AST | Abstract Syntax Tree — the canonical in-memory representation of a recorded session |
| BDD | Behavior-Driven Development — methodology using natural-language scenarios |
| CDP | Chrome DevTools Protocol — the low-level wire protocol for browser automation |
| DDT | Data-Driven Testing — executing the same test logic against multiple data sets |
| RF | Robot Framework — the keyword-driven test automation framework |
| `.recrd` | The JSON session file format produced by the recording engine |

## Appendix B: Gherkin Output Example

```gherkin
# language: pt
# Gerado por recrd v1.0.0 em 2026-03-25T14:30:00Z
# Sessão: a1b2c3d4 | SHA-256: 9f86d08...

Funcionalidade: Autenticação de Usuários

  Esquema do Cenário: Login com diferentes perfis
    Dado que eu acesso a página de login
    Quando eu preencho o campo "Usuário" com "<login>"
    E eu preencho o campo "Senha" com "<senha>"
    E eu clico em "Entrar"
    Então eu devo ver a mensagem de boas-vindas "<mensagem>"

  Exemplos:
    | login  | senha | mensagem     |
    | admin  | 123   | Olá, Admin   |
    | user   | abc   | Olá, Usuário |
```
