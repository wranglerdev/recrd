# recrd

## What This Is

`recrd` is a .NET 10 CLI tool that records browser interactions via Playwright/CDP, emits pt-BR Gherkin (`.feature` files), and compiles executable Robot Framework 7 test suites with native data-driven testing support. It targets QA engineers who want to eliminate E2E test boilerplate and standardize BDD output without writing tests by hand.

## Core Value

Record once, compile to a round-trip-verified, data-driven Robot Framework suite with zero manual keyword writing.

## Requirements

### Validated

- [x] `Recrd.Core` — AST types (`Session`, `ActionStep`, `AssertionStep`, `GroupStep`, `Selector`, `Variable`), all interfaces (`ITestCompiler`, `IDataProvider`, `IEventInterceptor`, `IAssertionProvider`), `Channel<RecordedEvent>` pipeline — *Validated in Phase 02: core-ast-types-interfaces*

### Active

**Foundation**
- [x] `Recrd.Data` — `CsvDataProvider` (RFC 4180, BOM-tolerant, streaming) and `JsonDataProvider` (flat root array, dot-notation flattening), `IAsyncEnumerable<T>` contract, ≤1000 rows in-memory batch — *Validated in Phase 03: data-providers*
- [x] `Recrd.Gherkin` — AST → pt-BR `.feature` generator; `Cenário` vs `Esquema do Cenário` + `Exemplos` table; deterministic/idempotent output; variable mismatch hard error — *Validated in Phase 04: gherkin-generator*
- [x] CI pipeline — build, unit test, coverage gates (≥90% Core/Data/Gherkin/Compilers), `dotnet format` check, weekly Stryker.NET mutation run — *Validated in Phase 05: ci-pipeline*

**Recording Engine**
- [ ] `Recrd.Recording` — Playwright .NET CDP integration, DOM event capture (click, input, change, select, hover, navigation, file upload, drag-and-drop), selector extraction ranked by stability (`data-testid` > `id` > `role` > CSS > XPath)
- [ ] Inspector side-panel (minimal UI) — live event stream, variable tagging (right-click → "Tag as Variable"), assertion builder
- [ ] `.recrd` session file — JSON, UTF-8, `schemaVersion: 1`, incremental `.recrd.partial` snapshots every 30s, `recrd recover` command
- [ ] Multi-tab support (constrained) — single-level popup handling (OAuth redirects); no full multi-tab AST complexity

**Compilers**
- [ ] `RobotBrowserCompiler` — Robot Framework 7 + Browser library; selector priority chain; `Wait For Elements State` injection; traceability header (version, timestamp, SHA-256)
- [ ] `RobotSeleniumCompiler` — Robot Framework 7 + SeleniumLibrary; configurable implicit/explicit waits; traceability header
- [ ] E2E round-trip tests — `record → compile → execute` against fixture web app (static HTML + simple SPA)

**CLI Polish & Distribution**
- [ ] Full CLI surface: `start`, `pause`, `resume`, `stop`, `compile`, `validate`, `sanitize`, `recover`, `version`, `plugins list/install`
- [ ] Self-contained single-file publish for Windows 10+, macOS 12+, Ubuntu 20.04+
- [ ] GitHub Releases automation, Homebrew tap, winget manifest

**VS Code Extension**
- [ ] Thin wrapper: start/stop recording, target picker, data file picker, live preview WebView, status bar
- [ ] CLI-only communication via stdout/stderr and exit codes
- [ ] VS Code Marketplace publish pipeline

### Out of Scope

- `.side` file import (Selenium IDE) — complex parser, unstable format, no coverage guarantee on format stability
- Multi-tab full support (complex tab contexts, parallel page navigation) — AST complexity too high for v1; single-level popups are the constrained exception
- AI-assisted step grouping (ONNX Runtime) — deferred to plugin; default heuristic (navigation→Dado, interactions→Quando, assertions→Então) is sufficient for v1
- Robot Framework 6 support — RF7-only keeps compiler test surface single-target
- OAuth / real browser profile access — sandboxed BrowserContext only
- Telemetry — opt-in only, off by default

## Context

- **TDD mandate**: Full red-green per phase. All tests for a phase are written and committed red before any implementation begins. CI must be green (tests passing) at every commit post-implementation.
- **Tech stack**: .NET 10 (LTS), xUnit + Moq (unit), TestContainers (integration), Playwright .NET (recording engine and E2E fixture), Robot Framework 7 (compiler target), BenchmarkDotNet (perf), Stryker.NET (mutation)
- **Monorepo**: `apps/` (CLI + VS Code extension), `packages/` (Core, Recording, Data, Gherkin, Compilers), `tests/` (mirroring packages), `plugins/` (examples), `Directory.Build.props`, `recrd.sln`
- **Dependency rule**: `Recrd.Core` has zero `Recrd.*` deps — enforced in CI. `Recrd.Recording` is isolated to avoid pulling Playwright's ~200MB browser binaries into compile-only consumers.
- **Plugin system**: NuGet packages `Recrd.Plugin.*`, loaded from `~/.recrd/plugins/` via `AssemblyLoadContext` isolation; host rejects plugins with incompatible major version of `Recrd.Core`
- **Determinism**: Same AST + same data = byte-identical `.feature` output across all runs
- **Language**: Gherkin output always in pt-BR; variable naming `^[a-z][a-z0-9_]{0,63}$`
- **Performance targets**: recording latency <50ms, compile <3s (1000-step session), CSV/JSON 50MB parse <10s with ≤100MB heap delta, CLI cold start <500ms

## Constraints

- **Tech stack**: .NET 10 — cannot change runtime target
- **RF version**: Robot Framework 7 only — explicit decision to avoid dual compiler surface
- **Core isolation**: `Recrd.Core` must have zero `Recrd.*` dependencies — CI-enforced
- **TDD flow**: Tests committed red before implementation; no implementation without a failing test
- **Determinism**: Output must be byte-identical given same inputs — affects generator design
- **Data streaming**: `IDataProvider` must use `IAsyncEnumerable<T>`; max 1000 rows in-memory — memory safety constraint

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| RF 7 only (drop RF 6) | Single compiler test surface; cleaner keyword syntax | — Pending |
| Multi-tab: constrained (single-level popup only) | Full multi-tab AST complexity not worth v1 risk; OAuth use case covered | — Pending |
| AI step grouping as plugin only | ONNX dependency too heavy for core; heuristic sufficient for v1 | — Pending |
| `.side` import excluded | Selenium IDE format unstable; no format coverage guarantee | — Pending |
| Playwright .NET over raw CDP | Transport reconnection, multi-browser, stable API surface | — Pending |
| `AssemblyLoadContext` for plugins | Prevents version conflicts; host can reject incompatible major versions | — Pending |
| `Channel<T>` for event pipeline | No HTTP/socket/serialization overhead between recording and inspector | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd:transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-03-27 after Phase 04 completion — Recrd.Gherkin pt-BR generator complete, all 22 Gherkin tests green (83/83 total)*
