# Requirements: recrd

**Defined:** 2026-03-26
**Core Value:** Record once, compile to a round-trip-verified, data-driven Robot Framework 7 suite with zero manual keyword writing.

---

## v1 Requirements

### Foundation — Core Types & Interfaces

- [x] **CORE-01**: `Session` AST root with `metadata`, `variables`, and `steps` fields serializable to/from JSON with `schemaVersion: 1`
- [x] **CORE-02**: `ActionStep` covering click, type, select, navigate, upload, drag-and-drop
- [x] **CORE-03**: `AssertionStep` covering text-equals, text-contains, visible, enabled, URL-matches
- [x] **CORE-04**: `GroupStep` with `given`/`when`/`then` type, containing child steps
- [x] **CORE-05**: `Selector` with type (data-testid, id, role, css, xpath) and ranked priority array
- [x] **CORE-06**: `Variable` with name (`^[a-z][a-z0-9_]{0,63}$` validated), linked step reference
- [x] **CORE-07**: `ITestCompiler` interface: `TargetName`, `CompileAsync(Session, CompilerOptions) → CompilationResult`
- [x] **CORE-08**: `IDataProvider` interface: `IAsyncEnumerable<IReadOnlyDictionary<string,string>> StreamAsync()`
- [x] **CORE-09**: `IEventInterceptor` plugin extension point interface
- [x] **CORE-10**: `IAssertionProvider` plugin extension point interface
- [x] **CORE-11**: `Channel<RecordedEvent>` pipeline infrastructure with backpressure and cancellation support
- [x] **CORE-12**: `RecordedEvent` envelope with Id, TimestampMs (monotonic), EventType, Selectors, Payload, DataVariable
- [x] **CORE-13**: `Recrd.Core` has zero `Recrd.*` package dependencies (CI-enforced)

### Foundation — Data Providers

- [ ] **DATA-01**: `CsvDataProvider` — RFC 4180 compliant, BOM-tolerant, configurable delimiter
- [ ] **DATA-02**: `CsvDataProvider` throws `DataParseException` with line number on malformed input (missing closing quote, mismatched column count)
- [ ] **DATA-03**: `CsvDataProvider` streams `IAsyncEnumerable<T>` with ≤1000 rows in-memory; 50MB file peak heap delta ≤100MB
- [ ] **DATA-04**: `JsonDataProvider` — root-level JSON array of flat objects; dot-notation flattening for nested objects
- [ ] **DATA-05**: `JsonDataProvider` throws `DataParseException` on non-array root

### Foundation — Gherkin Generator

- [ ] **GHER-01**: Session with zero variables emits `Cenário` (single scenario)
- [ ] **GHER-02**: Session with ≥1 variable emits `Esquema do Cenário` + `Exemplos` pipe-delimited table
- [ ] **GHER-03**: Variable missing from data columns → hard error (`GherkinException`) with variable name and file reference
- [ ] **GHER-04**: Extra data column not in AST → warning to stderr (not an error)
- [ ] **GHER-05**: `GroupStep(given)` → `Dado`/`E`; `GroupStep(when)` → `Quando`/`E`; `GroupStep(then)` → `Então`/`E`
- [ ] **GHER-06**: Default heuristic (no GroupStep): first navigation → `Dado`, interactions → `Quando`, assertions → `Então`
- [ ] **GHER-07**: Output is deterministic: same AST + same data = byte-identical `.feature` across runs
- [ ] **GHER-08**: Output file always UTF-8, no BOM, `# language: pt` header
- [ ] **GHER-09**: Columns in `Exemplos` table ordered by first variable appearance in scenario body

### Foundation — CI Pipeline

- [ ] **CI-01**: GitHub Actions pipeline: restore → build → test → coverage gate → format check
- [ ] **CI-02**: Coverage gate fails build if `Recrd.Core`, `Recrd.Data`, `Recrd.Gherkin`, or `Recrd.Compilers` drop below 90% line coverage
- [ ] **CI-03**: `dotnet format --verify-no-changes` enforced on every push/PR
- [ ] **CI-04**: Weekly scheduled Stryker.NET mutation run on `Recrd.Core`
- [ ] **CI-05**: `main`-branch-only: `dotnet pack` → NuGet push (pre-release tag)
- [ ] **CI-06**: TDD red phase: CI runs tests but does NOT fail the build on test failures during a designated red-phase branch prefix (e.g., `tdd/phase-*`)

### Recording Engine

- [ ] **REC-01**: Launch clean `BrowserContext` (incognito, zero cookies, zero localStorage) via Playwright .NET
- [ ] **REC-02**: Inject JavaScript recording agent into every frame via `Page.EvaluateAsync` on navigation
- [ ] **REC-03**: DOM events captured: click, input/change, select, hover (explicit only), navigation, file upload, drag-and-drop
- [ ] **REC-04**: Each captured event wrapped as `RecordedEvent` and pushed to `Channel<RecordedEvent>`
- [ ] **REC-05**: Selector extraction per element: `data-testid` > `id` > `role`-based > CSS class chain > XPath; minimum 3 strategies per element
- [ ] **REC-06**: `recrd pause` freezes event capture, enables assertion mode
- [ ] **REC-07**: `recrd resume` returns to recording mode
- [ ] **REC-08**: `recrd stop` flushes AST to `.recrd` session file (JSON, UTF-8)
- [ ] **REC-09**: Incremental `.recrd.partial` snapshots written every 30 seconds during session
- [ ] **REC-10**: `recrd recover` reconstructs session from latest `.recrd.partial` snapshot
- [ ] **REC-11**: Inspector side-panel opens as secondary `BrowserContext` with `--app` flag
- [ ] **REC-12**: Inspector panel displays live event stream from `Channel<RecordedEvent>`
- [ ] **REC-13**: Right-click → "Tag as Variable" in inspector replaces literal value with named placeholder; duplicate names rejected with visible warning
- [ ] **REC-14**: Right-click → assertion builder (pause mode) inserts `AssertionStep` into AST
- [ ] **REC-15**: Multi-tab constrained: single-level popup handling (OAuth redirect opens new page, events captured, page closed automatically on navigation back)

### Compilers

- [ ] **COMP-01**: `RobotBrowserCompiler` emits RF7-compatible `.robot` suite and `.resource` file
- [ ] **COMP-02**: `RobotBrowserCompiler` uses `css=[data-testid="..."]` as preferred selector; falls back per `--selector-strategy`
- [ ] **COMP-03**: `RobotBrowserCompiler` inserts `Wait For Elements State` before every interaction keyword
- [ ] **COMP-04**: `RobotSeleniumCompiler` emits RF7-compatible `.robot` suite and `.resource` file
- [ ] **COMP-05**: `RobotSeleniumCompiler` prefers `id:...` selector; falls back to `css:...` then `xpath:...`
- [ ] **COMP-06**: `RobotSeleniumCompiler` emits configurable implicit/explicit waits (default 10s)
- [ ] **COMP-07**: Both compilers emit traceability header: `recrd` version, compilation timestamp, source `.recrd` SHA-256, compiler target name
- [ ] **COMP-08**: Both compilers emit `*** Settings ***` block declaring minimum RF version
- [ ] **COMP-09**: `CompilationResult` includes: generated file list, warnings list, dependency manifest
- [ ] **COMP-10**: Round-trip E2E: `record → compile → execute` passes on fixture web app (static HTML + simple SPA) with zero manual edits

### CLI Surface

- [ ] **CLI-01**: `recrd start [--browser chromium|firefox|webkit] [--headed] [--viewport WxH] [--base-url url]`
- [ ] **CLI-02**: `recrd pause`, `recrd resume`, `recrd stop`
- [ ] **CLI-03**: `recrd compile <session.recrd> [--target robot-browser|robot-selenium] [--data file] [--csv-delimiter char] [--out dir] [--selector-strategy chain] [--timeout secs] [--intercept]`
- [ ] **CLI-04**: `recrd validate <session.recrd>` — validates AST schema and variable consistency, exits non-zero on error
- [ ] **CLI-05**: `recrd sanitize <session.recrd>` — strips all literal values, keeps variable placeholders and structure
- [ ] **CLI-06**: `recrd recover` — reconstructs session from latest `.recrd.partial`
- [ ] **CLI-07**: `recrd version` — prints version and runtime info
- [ ] **CLI-08**: `recrd plugins list` / `recrd plugins install <pkg>` — plugin management
- [ ] **CLI-09**: `--verbosity quiet|normal|detailed|diagnostic` across all commands
- [ ] **CLI-10**: Structured logging via `Microsoft.Extensions.Logging`; `--log-output json` for machine-parseable output
- [ ] **CLI-11**: `recrd stop` prints summary: total events, variables declared, session duration, file sizes
- [ ] **CLI-12**: CLI cold start < 500ms (measured via `time recrd version`)

### Distribution

- [ ] **DIST-01**: Self-contained single-file publish for win-x64, osx-arm64, osx-x64, linux-x64
- [ ] **DIST-02**: GitHub Releases automation (binary assets attached on tag push)
- [ ] **DIST-03**: Homebrew tap formula
- [ ] **DIST-04**: winget manifest

### VS Code Extension

- [ ] **VSCE-01**: Start/stop recording via `recrd start` / `recrd stop` (`child_process.spawn`)
- [ ] **VSCE-02**: Compiler target picker (QuickPick) → `--target` flag
- [ ] **VSCE-03**: Data file picker → `--data` flag
- [ ] **VSCE-04**: Live preview WebView — watches `.recrd` via `fs.watch`, incremental compile on change, renders `.feature` + `.robot`
- [ ] **VSCE-05**: Status bar shows recording state (recording / paused / idle) and elapsed time
- [ ] **VSCE-06**: CLI-only communication (stdout/stderr + exit codes, no proprietary IPC)
- [ ] **VSCE-07**: VS Code Marketplace publish pipeline; minimum VS Code 1.85

### Plugin System

- [ ] **PLUG-01**: Plugin discovery: scan `~/.recrd/plugins/` for assemblies exporting `ITestCompiler`, `IDataProvider`, `IEventInterceptor`, or `IAssertionProvider`
- [ ] **PLUG-02**: Plugin loading via `AssemblyLoadContext` isolation; `Recrd.Core` shared from host to avoid type identity issues
- [ ] **PLUG-03**: Host rejects plugins built against incompatible major version of `Recrd.Core` with clear error message
- [ ] **PLUG-04**: Unhandled plugin exceptions caught, logged, surfaced as compilation warnings — host process never crashes

---

## v2 Requirements

### Performance Benchmarks

- **PERF-01**: BenchmarkDotNet integration tests for: recording latency (<50ms), compile time (<3s/1000 steps), CSV/JSON 50MB parse (<10s, ≤100MB heap delta)
- **PERF-02**: Performance regression detection in CI (BenchmarkDotNet baseline comparison)

### Observability

- **OBS-01**: Session summary report on `recrd stop` includes timing breakdown per event type
- **OBS-02**: OpenTelemetry traces for compile pipeline (opt-in)

### Extended Browser Support

- **BROW-01**: Full multi-tab recording (parallel page contexts, inter-tab navigation)
- **BROW-02**: Firefox and WebKit first-class E2E test coverage (v1 focuses on Chromium)

---

## Out of Scope

| Feature | Reason |
|---------|--------|
| `.side` file import (Selenium IDE) | Unstable format, no format coverage guarantee, complex parser with poor ROI |
| Robot Framework 6 support | Doubles compiler test surface; RF7-only is cleaner |
| AI-assisted step grouping (ONNX) | Heavy dependency; default heuristic sufficient for v1; better as plugin |
| Full multi-tab recording | AST complexity too high for v1; constrained popup support is the exception |
| Telemetry / opt-in network calls | Privacy-first; never by default |
| Real browser profile access | Security boundary — sandboxed BrowserContext only |
| Mobile browser recording | Out of Playwright .NET scope for v1 |
| Excel / YAML / database `IDataProvider` | Future plugin; CSV + JSON sufficient for v1 |

---

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CORE-01 | Phase 2 | Complete |
| CORE-02 | Phase 2 | Complete |
| CORE-03 | Phase 2 | Complete |
| CORE-04 | Phase 2 | Complete |
| CORE-05 | Phase 2 | Complete |
| CORE-06 | Phase 2 | Complete |
| CORE-07 | Phase 2 | Complete |
| CORE-08 | Phase 2 | Complete |
| CORE-09 | Phase 2 | Complete |
| CORE-10 | Phase 2 | Complete |
| CORE-11 | Phase 2 | Complete |
| CORE-12 | Phase 2 | Complete |
| CORE-13 | Phase 2 | Complete |
| DATA-01 | Phase 3 | Pending |
| DATA-02 | Phase 3 | Pending |
| DATA-03 | Phase 3 | Pending |
| DATA-04 | Phase 3 | Pending |
| DATA-05 | Phase 3 | Pending |
| GHER-01 | Phase 4 | Pending |
| GHER-02 | Phase 4 | Pending |
| GHER-03 | Phase 4 | Pending |
| GHER-04 | Phase 4 | Pending |
| GHER-05 | Phase 4 | Pending |
| GHER-06 | Phase 4 | Pending |
| GHER-07 | Phase 4 | Pending |
| GHER-08 | Phase 4 | Pending |
| GHER-09 | Phase 4 | Pending |
| CI-01 | Phase 5 | Pending |
| CI-02 | Phase 5 | Pending |
| CI-03 | Phase 5 | Pending |
| CI-04 | Phase 5 | Pending |
| CI-05 | Phase 5 | Pending |
| CI-06 | Phase 5 | Pending |
| REC-01 | Phase 6 | Pending |
| REC-02 | Phase 6 | Pending |
| REC-03 | Phase 6 | Pending |
| REC-04 | Phase 6 | Pending |
| REC-05 | Phase 6 | Pending |
| REC-06 | Phase 6 | Pending |
| REC-07 | Phase 6 | Pending |
| REC-08 | Phase 6 | Pending |
| REC-09 | Phase 6 | Pending |
| REC-10 | Phase 6 | Pending |
| REC-11 | Phase 6 | Pending |
| REC-12 | Phase 6 | Pending |
| REC-13 | Phase 6 | Pending |
| REC-14 | Phase 6 | Pending |
| REC-15 | Phase 6 | Pending |
| COMP-01 | Phase 7 | Pending |
| COMP-02 | Phase 7 | Pending |
| COMP-03 | Phase 7 | Pending |
| COMP-04 | Phase 7 | Pending |
| COMP-05 | Phase 7 | Pending |
| COMP-06 | Phase 7 | Pending |
| COMP-07 | Phase 7 | Pending |
| COMP-08 | Phase 7 | Pending |
| COMP-09 | Phase 7 | Pending |
| COMP-10 | Phase 7 | Pending |
| CLI-01 | Phase 8 | Pending |
| CLI-02 | Phase 8 | Pending |
| CLI-03 | Phase 8 | Pending |
| CLI-04 | Phase 8 | Pending |
| CLI-05 | Phase 8 | Pending |
| CLI-06 | Phase 8 | Pending |
| CLI-07 | Phase 8 | Pending |
| CLI-08 | Phase 8 | Pending |
| CLI-09 | Phase 8 | Pending |
| CLI-10 | Phase 8 | Pending |
| CLI-11 | Phase 8 | Pending |
| CLI-12 | Phase 8 | Pending |
| DIST-01 | Phase 9 | Pending |
| DIST-02 | Phase 9 | Pending |
| DIST-03 | Phase 9 | Pending |
| DIST-04 | Phase 9 | Pending |
| VSCE-01 | Phase 10 | Pending |
| VSCE-02 | Phase 10 | Pending |
| VSCE-03 | Phase 10 | Pending |
| VSCE-04 | Phase 10 | Pending |
| VSCE-05 | Phase 10 | Pending |
| VSCE-06 | Phase 10 | Pending |
| VSCE-07 | Phase 10 | Pending |
| PLUG-01 | Phase 11 | Pending |
| PLUG-02 | Phase 11 | Pending |
| PLUG-03 | Phase 11 | Pending |
| PLUG-04 | Phase 11 | Pending |

**Coverage:**
- v1 requirements: 78 total
- Mapped to phases: 78
- Unmapped: 0

---
*Requirements defined: 2026-03-26*
*Last updated: 2026-03-26 — traceability updated after roadmap creation (Phase 1 = Scaffold, Phase 2 = Core, phases 2–11 shifted +1)*
