# Roadmap: recrd

**Core Value:** Record once, compile to a round-trip-verified, data-driven Robot Framework 7 suite with zero manual keyword writing.

**Milestone:** v1

---

## Phases

- [x] **Phase 1: Monorepo Scaffold & Solution Structure** - sln, Directory.Build.props, project files, CI skeleton, dependency enforcement (completed 2026-03-26)
- [x] **Phase 2: Core AST Types & Interfaces** - All AST types, interfaces, and Channel pipeline in Recrd.Core (zero Recrd.* deps) (completed 2026-03-26)
- [x] **Phase 3: Data Providers** - CsvDataProvider and JsonDataProvider with streaming and error handling (completed 2026-03-27)
- [x] **Phase 4: Gherkin Generator** - pt-BR .feature output, Cenario vs Esquema, determinism, variable merging (completed 2026-03-27)
- [x] **Phase 5: CI Pipeline** - GitHub Actions, coverage gates, format check, Stryker, NuGet push, red-phase support (completed 2026-03-29)
- [x] **Phase 6: Recording Engine** - Playwright integration, event capture, inspector panel, variable tagging, constrained multi-tab (completed 2026-03-31)
- [x] **Phase 7: Compilers** - RobotBrowserCompiler, RobotSeleniumCompiler, RF7, traceability header, E2E round-trip (completed 2026-04-06)
- [x] **Phase 8: CLI Polish** - Full command surface, logging, help text, error formatting, cold-start target (completed 2026-04-09)
- [x] **Phase 8.1: CI Fixes & Cleanup** - Fix redundant framework references and CI task ordering (completed 2026-04-09)
- [x] **Phase 9: Distribution** - Self-contained publish, GitHub Releases, Homebrew tap, winget manifest (completed 2026-04-09)
- [x] **Phase 10: VS Code Extension** - Thin wrapper, live preview WebView, Marketplace publish (completed 2026-04-09)
- [x] **Phase 11: Plugin System** - AssemblyLoadContext isolation, discovery, version gating, exception safety (completed 2026-04-10)
- [ ] **Phase 12: Hardening** - Mutation testing, performance benchmarks, example plugins, docs

---

## Phase Details

### Phase 1: Monorepo Scaffold & Solution Structure
**Goal**: The repository is a buildable, testable .NET 10 monorepo with all project files, shared MSBuild properties, and the dependency isolation rule CI-enforced from day one.
**Depends on**: Nothing
**Requirements**: (structural — no REQUIREMENTS.md IDs map to scaffold; this phase unblocks all others)
**Success Criteria** (what must be TRUE):
  1. `dotnet build recrd.sln` exits zero from a clean checkout with no SDK other than .NET 10 installed
  2. All project and test project stubs exist under `apps/`, `packages/`, `tests/`, `plugins/` matching the documented structure
  3. `Directory.Build.props` applies shared properties (TFM, nullable, warnings-as-errors) to every project without per-project duplication
  4. A placeholder CI workflow exists that runs restore and build and exits zero on a push to `main`
  5. Running `dotnet dependency-graph` (or equivalent assertion) confirms `Recrd.Core` has zero `Recrd.*` package references
**Plans**: 7 plans

Plans:
- [x] 01-01-solution-scaffold-PLAN.md — recrd.sln, Directory.Build.props, global.json
- [x] 01-02-package-projects-PLAN.md — 5 package library stubs (Core, Data, Gherkin, Recording, Compilers)
- [x] 01-03-app-project-PLAN.md — recrd-cli console app stub
- [x] 01-04-placeholder-dirs-PLAN.md — plugins/ and apps/vscode-extension/ .gitkeep
- [x] 01-05-test-projects-PLAN.md — 6 xUnit test project stubs with PlaceholderTests
- [x] 01-06-ci-workflow-PLAN.md — GitHub Actions CI workflow with Core isolation check
- [x] 01-07-code-quality-tooling-PLAN.md — .editorconfig and dotnet-tools.json
**UI hint**: no

### Phase 2: Core AST Types & Interfaces
**Goal**: `Recrd.Core` contains all AST types, interfaces, and the Channel pipeline — fully unit-tested, with zero dependencies on other `Recrd.*` packages.
**Depends on**: Phase 1
**Requirements**: CORE-01, CORE-02, CORE-03, CORE-04, CORE-05, CORE-06, CORE-07, CORE-08, CORE-09, CORE-10, CORE-11, CORE-12, CORE-13
**Success Criteria** (what must be TRUE):
  1. A `Session` can be serialized to JSON and deserialized back with full fidelity, including `schemaVersion: 1`, all metadata fields, typed steps, and variables
  2. `ActionStep`, `AssertionStep`, and `GroupStep` instances are constructible for all documented subtypes (click, type, select, navigate, upload, drag-drop; text-equals, text-contains, visible, enabled, URL-matches; given/when/then)
  3. `Selector` instances rank by the defined priority array; `Variable` names are validated against `^[a-z][a-z0-9_]{0,63}$` at construction
  4. `Channel<RecordedEvent>` accepts events with backpressure, supports cancellation, and drains without deadlock under test
  5. All four interfaces (`ITestCompiler`, `IDataProvider`, `IEventInterceptor`, `IAssertionProvider`) are defined and `Recrd.Core` builds with zero `Recrd.*` references (CI gate green)
**Plans**: 4 plans

Plans:
- [x] 02-01-PLAN.md — TDD red phase: all 5 test suites committed failing on tdd/phase-02 branch
- [x] 02-02-PLAN.md — AST types: IStep, step records, enums, Selector, Variable, Session
- [x] 02-03-PLAN.md — Interfaces (ITestCompiler, IDataProvider, IEventInterceptor, IAssertionProvider) + RecordingChannel pipeline
- [x] 02-04-PLAN.md — RecrdJsonContext serialization + green phase (all tests pass)

### Phase 3: Data Providers
**Goal**: `Recrd.Data` delivers production-quality CSV and JSON data providers that stream rows without OOM risk and throw typed exceptions on malformed input.
**Depends on**: Phase 2
**Requirements**: DATA-01, DATA-02, DATA-03, DATA-04, DATA-05
**Success Criteria** (what must be TRUE):
  1. `CsvDataProvider` parses a UTF-8 BOM-prefixed RFC 4180 file with a non-default delimiter and returns correct column names and values
  2. A malformed CSV (unclosed quote or mismatched column count) throws `DataParseException` carrying the offending line number — not a generic exception
  3. A 50 MB CSV file streams to completion with a peak heap delta no greater than 100 MB, verified by a test using `GC.GetTotalMemory`
  4. `JsonDataProvider` flattens `{ "user": { "name": "Gil" } }` to a column named `user.name`
  5. A JSON file whose root is an object (not array) throws `DataParseException` with a message that explains the root-must-be-array constraint
**Plans**: 4 plans

Plans:
- [x] 03-01-PLAN.md — TDD red phase: DataParseException, stub providers, CsvHelper dep, all test suites (red)
- [x] 03-02-PLAN.md — CsvDataProvider implementation (RFC 4180, BOM, delimiter, streaming, error handling)
- [x] 03-03-PLAN.md — JsonDataProvider implementation (dot-notation flattening, array skip, non-array root error)
- [x] 03-04-PLAN.md — Green phase: full suite verification and tdd/phase-03 merge to main

### Phase 4: Gherkin Generator
**Goal**: `Recrd.Gherkin` walks the AST and emits a valid, deterministic pt-BR `.feature` file for both fixed-scenario and data-driven cases.
**Depends on**: Phase 3
**Requirements**: GHER-01, GHER-02, GHER-03, GHER-04, GHER-05, GHER-06, GHER-07, GHER-08, GHER-09
**Success Criteria** (what must be TRUE):
  1. A session with no variables produces a file containing `Cenario` (not `Esquema do Cenario`)
  2. A session with variables produces `Esquema do Cenario` and a pipe-delimited `Exemplos` table whose column order matches the first appearance of each variable in the scenario body
  3. When a variable declared in the AST is missing from the data file, `GherkinException` is thrown naming the missing variable and the data file path; an extra column in the data produces a warning to stderr only
  4. `GroupStep(given)` steps emit `Dado`/`E`; the default heuristic (no GroupStep) assigns the first navigation to `Dado`, interactions to `Quando`, and assertions to `Entao`
  5. Running the generator twice on the identical AST and data inputs produces byte-identical `.feature` output; the file is always UTF-8 with no BOM and opens with `# language: pt`
**Plans**: 4 plans

Plans:
- [x] 04-01-PLAN.md — TDD red phase: public types + 5 test suites (red) on tdd/phase-04
- [x] 04-02-PLAN.md — Fixed scenario: StepTextRenderer, GroupingClassifier, GherkinGenerator Cenario path
- [x] 04-03-PLAN.md — Data-driven: ExemplosTableBuilder, Esquema do Cenario + variable validation
- [x] 04-04-PLAN.md — Green phase: full suite verification + tdd/phase-04 merge to main

### Phase 5: CI Pipeline
**Goal**: Every push to the repository triggers a fully automated quality gate: build, test, coverage, format, with scheduled mutation testing and gated NuGet publish on `main`.
**Depends on**: Phase 4
**Requirements**: CI-01, CI-02, CI-03, CI-04, CI-05, CI-06
**Success Criteria** (what must be TRUE):
  1. A pull request against `main` triggers a GitHub Actions workflow that runs restore -> build -> test -> coverage gate -> format check in sequence, failing the PR if any step exits non-zero
  2. Dropping line coverage below 90% on any of `Recrd.Core`, `Recrd.Data`, `Recrd.Gherkin`, or `Recrd.Compilers` fails the build with a descriptive message identifying which project fell below threshold
  3. Introducing a formatting violation causes `dotnet format --verify-no-changes` to fail the CI run with a diff
  4. A weekly scheduled workflow runs Stryker.NET on `Recrd.Core` and posts a mutation score report
  5. Pushing a pre-release tag on `main` triggers `dotnet pack` followed by a NuGet push; pushing on a non-`main` branch does not trigger packaging
  6. Pushing to a branch prefixed `tdd/phase-*` runs tests but does not fail the build on test failures, enabling the TDD red phase
**Plans**: 3 plans

Plans:
- [x] 05-01-PLAN.md — Enhanced ci.yml: per-project coverage gates, TDD red-phase support, IPv4 env
- [x] 05-02-PLAN.md — Weekly Stryker.NET mutation workflow + dotnet-stryker tool manifest
- [x] 05-03-PLAN.md — NuGet publish workflow for pre-release tags on GitHub Packages

### Phase 6: Recording Engine
**Goal**: `Recrd.Recording` captures live browser interactions via Playwright into the AST Channel pipeline, with an inspector side-panel for variable tagging, assertion insertion, and constrained popup handling.
**Depends on**: Phase 2
**Requirements**: REC-01, REC-02, REC-03, REC-04, REC-05, REC-06, REC-07, REC-08, REC-09, REC-10, REC-11, REC-12, REC-13, REC-14, REC-15
**Success Criteria** (what must be TRUE):
  1. `recrd start` launches a clean BrowserContext with zero cookies and zero localStorage; performing a click on a fixture page results in a `RecordedEvent` of type `click` appearing on the `Channel<RecordedEvent>` with at least 3 ranked selectors
  2. All seven event types (click, input/change, select, hover, navigation, file upload, drag-and-drop) each produce a correctly typed and populated `RecordedEvent`
  3. `recrd pause` stops event capture; `recrd resume` restarts it; `recrd stop` flushes the AST to a `.recrd` JSON file that can be deserialized back to a `Session` with all steps intact
  4. A `.recrd.partial` snapshot file is written every 30 seconds during a session; `recrd recover` reconstructs a `Session` from the latest partial without manual intervention
  5. The inspector side-panel opens alongside the recording context; right-clicking a field and selecting "Tag as Variable" replaces the literal value with a named placeholder; attempting to reuse a name shows a visible warning; right-click in pause mode inserts an `AssertionStep` into the AST
  6. A single-level OAuth-style popup (new page opened by `window.open`) has its events captured and is automatically treated as a constrained popup context; events from the popup appear on the same channel with a popup scope marker
**Plans**: 5 plans

Plans:
- [x] 06-01-PLAN.md — TDD red phase: IRecorderEngine interface, test deps, 37 failing test stubs on tdd/phase-06
- [x] 06-02-PLAN.md — Core engine: PlaywrightRecorderEngine, JS recording agent, 7 event types, selector extraction
- [x] 06-03-PLAN.md — Session lifecycle: pause/resume/stop, partial snapshots, recovery
- [x] 06-04-PLAN.md — Inspector panel: side-panel UI, live event stream, variable tagging, assertion builder
- [x] 06-05-PLAN.md — Popup handling + green phase: constrained popup capture, all 37 tests pass
**UI hint**: yes

### Phase 7: Compilers
**Goal**: `Recrd.Compilers` translates a `Session` into an executable RF7 `.robot` + `.resource` pair for both `robot-browser` and `robot-selenium` targets, with traceability headers and a passing E2E round-trip.
**Depends on**: Phase 4, Phase 6
**Requirements**: COMP-01, COMP-02, COMP-03, COMP-04, COMP-05, COMP-06, COMP-07, COMP-08, COMP-09, COMP-10
**Success Criteria** (what must be TRUE):
  1. `RobotBrowserCompiler` emits a `.robot` suite and `.resource` file where every click step maps to `Click  css=[data-testid="..."]` and every interaction is preceded by `Wait For Elements State`
  2. `RobotSeleniumCompiler` emits a `.robot` suite and `.resource` file where clicks use `id:...` selectors, falling back to `css:...` then `xpath:...`, with configurable implicit/explicit waits defaulting to 10 seconds
  3. Every generated file from both compilers contains a header comment block with the `recrd` version, compilation timestamp, source `.recrd` SHA-256 hash, and compiler target name
  4. Both compilers emit a `*** Settings ***` block that declares the minimum RF version
  5. `CompilationResult` carries the full list of generated files, any warnings, and a dependency manifest; the full `record -> compile -> execute` pipeline against the fixture web app (static HTML + simple SPA) completes with zero manual edits
**Plans**: 4 plans

Plans:
- [x] 07-01-PLAN.md — TDD red phase: 9 test suites + production stubs committed failing on tdd/phase-07
- [x] 07-02-PLAN.md — RobotBrowserCompiler implementation + shared helpers (KeywordNameBuilder, SelectorResolver, HeaderEmitter)
- [x] 07-03-PLAN.md — RobotSeleniumCompiler implementation + SeleniumKeywordEmitter
- [x] 07-04-PLAN.md — E2E round-trip tests (Kestrel + robot subprocess) + CI RF installation step

### Phase 8: CLI Polish
**Goal**: The `recrd` CLI exposes its complete command surface with structured logging, human-readable help text, machine-parseable output mode, and a cold-start time under 500 ms.
**Depends on**: Phase 6, Phase 7
**Requirements**: CLI-01, CLI-02, CLI-03, CLI-04, CLI-05, CLI-06, CLI-07, CLI-08, CLI-09, CLI-10, CLI-11, CLI-12
**Success Criteria** (what must be TRUE):
  1. All commands are reachable and documented: `start`, `pause`, `resume`, `stop`, `compile`, `validate`, `sanitize`, `recover`, `version`, `plugins list`, `plugins install`; `--help` on any command prints all flags and descriptions
  2. `recrd validate <session.recrd>` exits non-zero and prints an actionable error message when the AST schema is invalid or variable consistency fails
  3. `recrd sanitize <session.recrd>` produces a new session file with all literal values stripped and only variable placeholders and structure intact
  4. `recrd stop` prints a human-readable summary: total events captured, variables declared, session duration, and output file sizes
  5. `--verbosity quiet|normal|detailed|diagnostic` controls console output volume; `--log-output json` switches to machine-parseable structured JSON logs
  6. `time recrd version` completes in under 500 ms on the target platforms (Windows, macOS, Linux)
**Plans**: 4 plans

Plans:
- [x] 08-01-PLAN.md — TDD red phase: all CLI commands stubs + failing tests
- [x] 08-02-PLAN.md — Core commands: start, stop, pause, resume, version
- [x] 08-03-PLAN.md — Processing commands: compile, validate, sanitize, recover
- [x] 08-04-PLAN.md — Polish: plugins, verbosity, log-output, stop summary
**UI hint**: no

### Phase 8.1: CI Fixes & Cleanup
**Goal**: Fix build warnings and CI task ordering to ensure stable pipeline execution and clean local development experience.
**Depends on**: Phase 5, Phase 7
**Requirements**: CI-01, COMP-10
**Success Criteria** (what must be TRUE):
  1. `dotnet build` on the solution produces zero NETSDK1086 warnings
  2. CI workflow `ci.yml` installs Playwright browsers before any test execution step
**Plans**: 2 plans

Plans:
- [x] 08.1-01-PLAN.md — Fix redundant framework reference and reorder CI tasks
- [x] 08.1-02-PLAN.md — Update Node.js to 22 and fix Playwright installation path

### Phase 9: Distribution
**Goal**: A tagged release produces self-contained single-file binaries for all four platforms, attached to a GitHub Release, with a Homebrew formula and a winget manifest ready for submission.
**Depends on**: Phase 8
**Requirements**: DIST-01, DIST-02, DIST-03, DIST-04
**Success Criteria** (what must be TRUE):
  1. Self-contained single-file binaries for `win-x64`, `osx-arm64`, `osx-x64`, and `linux-x64` are produced by the build pipeline without requiring the .NET SDK on the target machine
  2. Pushing a version tag triggers a GitHub Actions workflow that attaches all four binaries as assets to a GitHub Release and publishes release notes
  3. The Homebrew formula installs the macOS binary and `brew install recrd && recrd version` exits zero
  4. The winget manifest is well-formed and passes `winget validate` locally
**Plans**: 4 plans

Plans:
- [x] 09-01-PLAN.md — TDD red phase: Distribution test scaffold
- [x] 09-02-PLAN.md — Self-contained publish pipeline
- [x] 09-03-PLAN.md — GitHub Releases + Release Notes automation
- [x] 09-04-PLAN.md — Homebrew formula and winget manifest

### Phase 10: VS Code Extension
**Goal**: The VS Code extension provides start/stop recording, target and data file pickers, a live preview WebView, and a status bar — all via CLI subprocess calls — and is publishable to the Marketplace.
**Depends on**: Phase 8
**Requirements**: VSCE-01, VSCE-02, VSCE-03, VSCE-04, VSCE-05, VSCE-06, VSCE-07
**Success Criteria** (what must be TRUE):
  1. The extension starts and stops a recording via `recrd start` / `recrd stop` invoked through `child_process.spawn`; stdout and stderr from the CLI are the only communication channel (no custom IPC)
  2. The compiler target QuickPick passes `--target robot-browser` or `--target robot-selenium` to `recrd compile`; the data file picker passes `--data <file>`
  3. The live preview WebView renders the current `.feature` and `.robot` output, updating within seconds of the `.recrd` file changing on disk (via `fs.watch`)
  4. The status bar item shows `recording`, `paused`, or `idle` with elapsed time during an active session
  5. The extension package is built and published to the VS Code Marketplace against a minimum VS Code version of 1.85
**Plans**: 4 plans

Plans:
- [ ] 10-01-PLAN.md — Scaffold extension and implement Status Bar
- [ ] 10-02-PLAN.md — CLI process management (start/stop)
- [ ] 10-03-PLAN.md — Target/Data pickers and compiler integration
- [ ] 10-04-PLAN.md — Live Preview WebView and Packaging
**UI hint**: yes

### Phase 11: Plugin System
**Goal**: The CLI loads third-party `Recrd.Plugin.*` assemblies from `~/.recrd/plugins/` in isolated `AssemblyLoadContext` contexts, enforces major-version compatibility, and never lets a plugin crash the host process.
**Depends on**: Phase 8
**Requirements**: PLUG-01, PLUG-02, PLUG-03, PLUG-04
**Success Criteria** (what must be TRUE):
  1. Placing a NuGet-package-derived assembly in `~/.recrd/plugins/` causes `recrd plugins list` to display it as an available compiler or data provider
  2. A plugin loaded via `AssemblyLoadContext` isolation can implement `ITestCompiler` and be invoked by `recrd compile --target <plugin-target>` without type identity conflicts with the host's `Recrd.Core`
  3. A plugin built against an incompatible major version of `Recrd.Core` causes the CLI to print a clear rejection message and skip that plugin, without failing the overall command
  4. An unhandled exception thrown inside a plugin during compilation is caught, logged as a warning, and reported in `CompilationResult.Warnings` — the host process exits zero and completes the compile with remaining plugins
**Plans**: 4 plans

Plans:
- [x] 11-01-PLAN.md — TDD red phase: Plugin discovery tests
- [x] 11-02-PLAN.md — AssemblyLoadContext implementation and isolation
- [x] 11-03-PLAN.md — Version gating and host integration
- [x] 11-04-PLAN.md — Exception safety and reporting

### Phase 12: Hardening
**Goal**: The codebase achieves measurable resilience through mutation testing, performance benchmarks, example plugin implementations, and contributor documentation.
**Depends on**: Phase 11
**Requirements**: (no standalone REQUIREMENTS.md IDs — covers PERF and observability targets from PROJECT.md and PRD section 6)
**Success Criteria** (what must be TRUE):
  1. Stryker.NET mutation testing on `Recrd.Core` achieves a mutation score reported in CI; any score below an agreed threshold blocks merge
  2. BenchmarkDotNet benchmarks assert: recording latency < 50 ms, compile time < 3 s for a 1000-step session, CSV/JSON 50 MB parse < 10 s with peak heap delta <= 100 MB; all three pass in CI
  3. At least one fully functional example plugin lives in `plugins/` implementing a non-trivial `ITestCompiler` or `IDataProvider` and is documented with a README
  4. A contributor can follow written instructions to scaffold, implement, test, and install a new `Recrd.Plugin.*` package end-to-end
**Plans**: 4 plans

Plans:
- [ ] 12-01-PLAN.md — Performance benchmarks (BenchmarkDotNet)
- [ ] 12-02-PLAN.md — Mutation testing hardening (Stryker.NET)
- [ ] 12-03-PLAN.md — Example plugin implementation
- [ ] 12-04-PLAN.md — Contributor docs and project cleanup

---

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Monorepo Scaffold & Structure | 7/7 | Complete | 2026-03-26 |
| 2. Core AST Types & Interfaces | 4/4 | Complete | 2026-03-26 |
| 3. Data Providers | 4/4 | Complete | 2026-03-27 |
| 4. Gherkin Generator | 4/4 | Complete | 2026-03-27 |
| 5. CI Pipeline | 3/3 | Complete | 2026-03-29 |
| 6. Recording Engine | 5/5 | Complete | 2026-03-31 |
| 7. Compilers | 4/4 | Complete | 2026-04-06 |
| 8. CLI Polish | 4/4 | Complete | 2026-04-09 |
| 8.1 CI Fixes & Cleanup | 2/2 | Complete | 2026-04-09 |
| 9. Distribution | 4/4 | Complete | 2026-04-09 |
| 10. VS Code Extension | 4/4 | Complete | 2026-04-09 |
| 11. Plugin System | 4/3 | Complete   | 2026-04-10 |
| 12. Hardening | 0/4 | Not Started | - |

---

## Coverage Summary

All 78 v1 requirements map to exactly one phase. No orphans.

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
| DATA-01 | Phase 3 | Complete |
| DATA-02 | Phase 3 | Complete |
| DATA-03 | Phase 3 | Complete |
| DATA-04 | Phase 3 | Complete |
| DATA-05 | Phase 3 | Complete |
| GHER-01 | Phase 4 | Complete |
| GHER-02 | Phase 4 | Complete |
| GHER-03 | Phase 4 | Complete |
| GHER-04 | Phase 4 | Complete |
| GHER-05 | Phase 4 | Complete |
| GHER-06 | Phase 4 | Complete |
| GHER-07 | Phase 4 | Complete |
| GHER-08 | Phase 4 | Complete |
| GHER-09 | Phase 4 | Complete |
| CI-01 | Phase 5 | Complete |
| CI-02 | Phase 5 | Complete |
| CI-03 | Phase 5 | Complete |
| CI-04 | Phase 5 | Complete |
| CI-05 | Phase 5 | Complete |
| CI-06 | Phase 5 | Complete |
| REC-01 | Phase 6 | Complete |
| REC-02 | Phase 6 | Complete |
| REC-03 | Phase 6 | Complete |
| REC-04 | Phase 6 | Complete |
| REC-05 | Phase 6 | Complete |
| REC-06 | Phase 6 | Complete |
| REC-07 | Phase 6 | Complete |
| REC-08 | Phase 6 | Complete |
| REC-09 | Phase 6 | Complete |
| REC-10 | Phase 6 | Complete |
| REC-11 | Phase 6 | Complete |
| REC-12 | Phase 6 | Complete |
| REC-13 | Phase 6 | Complete |
| REC-14 | Phase 6 | Complete |
| REC-15 | Phase 6 | Complete |
| COMP-01 | Phase 7 | Complete |
| COMP-02 | Phase 7 | Complete |
| COMP-03 | Phase 7 | Complete |
| COMP-04 | Phase 7 | Complete |
| COMP-05 | Phase 7 | Complete |
| COMP-06 | Phase 7 | Complete |
| COMP-07 | Phase 7 | Complete |
| COMP-08 | Phase 7 | Complete |
| COMP-09 | Phase 7 | Complete |
| COMP-10 | Phase 7 | Complete |
| CLI-01 | Phase 8 | Complete |
| CLI-02 | Phase 8 | Complete |
| CLI-03 | Phase 8 | Complete |
| CLI-04 | Phase 8 | Complete |
| CLI-05 | Phase 8 | Complete |
| CLI-06 | Phase 8 | Complete |
| CLI-07 | Phase 8 | Complete |
| CLI-08 | Phase 8 | Complete |
| CLI-09 | Phase 8 | Complete |
| CLI-10 | Phase 8 | Complete |
| CLI-11 | Phase 8 | Complete |
| CLI-12 | Phase 8 | Complete |
| DIST-01 | Phase 9 | Complete |
| DIST-02 | Phase 9 | Complete |
| DIST-03 | Phase 9 | Complete |
| DIST-04 | Phase 9 | Complete |
| VSCE-01 | Phase 10 | Complete |
| VSCE-02 | Phase 10 | Complete |
| VSCE-03 | Phase 10 | Complete |
| VSCE-04 | Phase 10 | Complete |
| VSCE-05 | Phase 10 | Complete |
| VSCE-06 | Phase 10 | Complete |
| VSCE-07 | Phase 10 | Complete |
| PLUG-01 | Phase 11 | Pending |
| PLUG-02 | Phase 11 | Pending |
| PLUG-03 | Phase 11 | Pending |
| PLUG-04 | Phase 11 | Pending |

**v1 requirements total: 78**
**Mapped: 78**
**Unmapped: 0**

---
*Roadmap created: 2026-03-26*
*Last updated: 2026-04-09 — Fixed summary table inconsistencies and added Phase 8.1*
