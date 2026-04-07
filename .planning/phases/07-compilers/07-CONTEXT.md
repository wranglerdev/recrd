# Phase 7: Compilers - Context

**Gathered:** 2026-04-05 (discuss mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

`Recrd.Compilers` translates a `Session` AST into an executable RF7 `.robot` suite + `.resource` keyword library for both `robot-browser` (Browser library) and `robot-selenium` (SeleniumLibrary) targets. Scope: both compiler implementations, traceability headers, `CompilationResult` population (files, warnings, dependency manifest), and a passing E2E round-trip (`record → compile → execute`) against a fixture web app.

CLI wiring, plugin system, and VS Code extension are out of scope for this phase.

</domain>

<decisions>
## Implementation Decisions

### RF Output Structure

- **D-01:** The `.resource` file contains **page-object keywords** that wrap the low-level Browser/Selenium keywords — not inline steps in `.robot`. Each recorded action becomes a named keyword in `.resource`. The `.robot` test case calls those keywords. This is idiomatic RF7 and keeps test cases readable.
- **D-02:** Keyword names are derived from **action type + element label in pt-BR**, slug-normalized from the selector value. E.g., Click on `data-testid="submit-btn"` → `Clicar Em Submit Btn`. The compiler converts selector values to title-case pt-BR slugs (hyphen/underscore → space → title case).
- **D-03:** The `.robot` suite's `*** Settings ***` block imports the `.resource` file and declares the minimum RF version. The `.resource` file's `*** Settings ***` block declares the Library (`Browser` or `SeleniumLibrary`). Both files carry the traceability header comment (COMP-07: version, compilation timestamp, source `.recrd` SHA-256, compiler target name).

### E2E Fixture App

- **D-04:** The fixture HTML covers **all ActionTypes**: a button (Click), a text input (Type), a `<select>` (Select), a file input (Upload), a draggable/droppable pair (DragDrop), a navigation link (Navigate), and assertable text/URL (for AssertionStep coverage). This verifies every compiler keyword mapping end-to-end (COMP-10).
- **D-05:** During the recording/capture phase of integration tests, the fixture HTML is served via **Playwright `Page.SetContentAsync`** — consistent with Phase 6 in-process patterns. No external server for the recording side.
- **D-06:** The **execute step** of the round-trip uses a **Kestrel TestServer** serving the fixture HTML at a real localhost URL, plus **`Process.Start("robot", ...)`** subprocess invocation. The test asserts RF exit code = 0. This constitutes a true end-to-end execution (COMP-10).
- **D-07:** CI must install Robot Framework and both libraries before running integration tests: `pip install robotframework robotframework-browser robotframework-seleniumlibrary`. This is a new CI dependency to add in Phase 7's CI step.

### Selenium Wait Strategy

- **D-08:** `RobotSeleniumCompiler` uses **implicit wait only**: `Set Selenium Implicit Wait    ${TIMEOUT}s` emitted inside the Suite Setup keyword in `.resource`. No per-step explicit wait boilerplate. Simpler output, consistent with SeleniumLibrary conventions.
- **D-09:** `CompilerOptions.TimeoutSeconds` (default 30) drives the wait value for **both compilers** — Browser compiler uses it for `Wait For Elements State` timeout, Selenium compiler uses it for implicit wait duration. COMP-06's "default 10s" is superseded by the already-defined `CompilerOptions` contract.

### Unresolvable Selector Handling

- **D-10:** When the preferred selector strategy is unavailable, the compiler walks the fallback chain (`DataTestId → Id → Role → Css → XPath`) until a value is found in `Selector.Values`. If the chain is fully exhausted (no strategy has a value), emit a **warning in `CompilationResult.Warnings`** and use the last available strategy's value as a last resort. Never throw — always produce valid (if fragile) RF output. This mirrors the Gherkin generator's warning-not-error approach for non-critical issues.

### TDD Mandate (carries forward)

- **D-11:** All compiler tests committed failing on `tdd/phase-07` branch before any implementation begins. Green phase commits only after all tests pass. Coverage gate ≥90% applies to `Recrd.Compilers` (same bar as all prior packages).

### Claude's Discretion

- Exact keyword slug normalization algorithm (punctuation handling, accented chars, length limits)
- `CompilationResult.DependencyManifest` content — suggested: `{ "robotframework": "7.x", "robotframework-browser": "x.y" }` or `{ "robotframework-seleniumlibrary": "x.y" }` respectively
- Whether the Suite Teardown (Close Browser / Close All Browsers) lives in `.resource` or is Claude-determined
- Internal compiler class structure (e.g., separate `KeywordNameBuilder`, `SelectorResolver`, `HeaderEmitter` helpers vs monolithic)
- Whether `Recrd.Integration.Tests` is where the E2E round-trip lives or a new `Recrd.Compilers.Tests` integration sub-suite

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements

- `.planning/REQUIREMENTS.md` §Compilers (COMP-01–COMP-10) — full compiler spec: selector priority, Wait For Elements State, implicit wait, traceability header, Settings block, CompilationResult contract, E2E round-trip
- `.planning/REQUIREMENTS.md` §CI Pipeline (CI-02) — coverage gate: `Recrd.Compilers` must maintain ≥90% line coverage

### Project Guidelines

- `CLAUDE.md` — TDD mandate (red-green per phase, `tdd/phase-*` branch prefix for CI-06), `DOTNET_SYSTEM_NET_DISABLEIPV6=1` prefix requirement

### Prior Phase Decisions (binding)

- `.planning/phases/02-core-ast-types-interfaces/02-CONTEXT.md` — `ITestCompiler`, `CompilationResult`, `CompilerOptions` interface contracts; `Selector` priority chain; `ActionType` / `AssertionType` enums; `RecrdJsonContext` serialization
- `.planning/phases/05-ci-pipeline/05-CONTEXT.md` — coverage gate pattern (per-project `dotnet test --threshold 90`)
- `.planning/phases/06-recording-engine/06-CONTEXT.md` §D-01 — in-process Playwright fixture pattern (`SetContentAsync`) for recording-side tests

### Core Interfaces (read directly)

- `packages/Recrd.Core/Interfaces/ITestCompiler.cs` — `TargetName`, `CompileAsync(Session, CompilerOptions)` contract
- `packages/Recrd.Core/Interfaces/CompilationResult.cs` — `GeneratedFiles`, `Warnings`, `DependencyManifest`
- `packages/Recrd.Core/Interfaces/CompilerOptions.cs` — `OutputDirectory`, `PreferredSelectorStrategy`, `TimeoutSeconds`
- `packages/Recrd.Core/Ast/ActionStep.cs`, `AssertionStep.cs`, `GroupStep.cs` — step types the compiler must handle
- `packages/Recrd.Core/Ast/Selector.cs`, `SelectorStrategy.cs` — priority chain and values dict

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- `packages/Recrd.Compilers/Placeholder.cs` — clean slate; just namespace declaration; replace entirely
- `tests/Recrd.Compilers.Tests/PlaceholderTests.cs` — replace with real test suites
- `tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj` — already has xUnit + Moq + coverlet.msbuild; no changes needed to project file
- `packages/Recrd.Compilers/Recrd.Compilers.csproj` — already references `Recrd.Core` and `Recrd.Gherkin`; no new project references needed for core compiler logic
- `tests/Recrd.Integration.Tests/PlaceholderTests.cs` — the E2E round-trip (D-06) likely lives here (references all 5 packages)

### Established Patterns

- Behavior-suite test organization: one file per concern — e.g., `BrowserCompilerSelectorTests.cs`, `SeleniumCompilerWaitTests.cs`, `TraceabilityHeaderTests.cs`, `RoundTripTests.cs`
- `[Theory]` + `[MemberData]` for multi-variant step type coverage (ActionType.Click, .Type, .Select, etc.)
- `IsPackable=false` already set on all test projects
- `PlaceholderTests.cs` pattern: delete and replace with real test suites

### Integration Points

- `Recrd.Integration.Tests` references `Recrd.Recording` — E2E round-trip tests depend on Phase 6's `IRecorderEngine` for the "record" step (or use a pre-built `Session` fixture)
- `RecrdJsonContext` in `Recrd.Core.Serialization` must be consulted if the compiler reads `.recrd` session files from disk (for CLI integration in Phase 8)
- Phase 8 (CLI) will wire `recrd compile` → `ITestCompiler.CompileAsync`; the compiler must be registerable as a named service

</code_context>

<specifics>
## Specific Ideas

- Generated keyword name example confirmed: Click on `data-testid="submit-btn"` → `Clicar Em Submit Btn` (action verb in pt-BR + slugged element name)
- Selenium implicit wait emitted as a keyword in `.resource` Suite Setup: `Set Selenium Implicit Wait    ${TIMEOUT}s` — not a per-step annotation
- Browser compiler mirrors Gherkin's warning-not-error philosophy for non-critical gaps (D-10)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 07-compilers*
*Context gathered: 2026-04-05*
