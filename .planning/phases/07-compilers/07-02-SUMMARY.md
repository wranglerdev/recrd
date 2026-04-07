---
phase: 07-compilers
plan: 02
subsystem: testing
tags: [dotnet, robot-framework, browser-library, compiler, csharp]

requires:
  - phase: 07-01
    provides: "test stubs for COMP-01 through COMP-09, production skeletons for RobotBrowserCompiler, SelectorResolver, KeywordNameBuilder, HeaderEmitter"

provides:
  - "RobotBrowserCompiler.CompileAsync emitting .robot + .resource file pair"
  - "BrowserKeywordEmitter with Browser library keyword bodies and Wait For Elements State injection"
  - "KeywordNameBuilder with pt-BR action/assertion verb mappings"
  - "SelectorResolver with DataTestId->Id->Role->Css->XPath fallback chain for Browser and Selenium"
  - "HeaderEmitter with SHA-256 traceability header"
  - "CompilerOptions.SourceFilePath for external .recrd file hashing"

affects:
  - 07-03-selenium-compiler
  - 07-04-integration-tests

tech-stack:
  added: []
  patterns:
    - "BrowserKeywordEmitter static helper — each ActionType maps to Browser library keyword calls with optional Wait For Elements State"
    - "SelectorResolver fallback chain — preferred strategy first, then DataTestId->Id->Role->Css->XPath, last resort Strategies list"
    - "HeaderEmitter single-line traceability — all info (version, timestamp, target, SHA-256) on first line for easy parsing"
    - "FlattenSteps recursion — GroupStep children flattened; only ActionStep/AssertionStep emitted as keywords"

key-files:
  created:
    - packages/Recrd.Compilers/Internal/BrowserKeywordEmitter.cs
  modified:
    - packages/Recrd.Compilers/Internal/KeywordNameBuilder.cs
    - packages/Recrd.Compilers/Internal/SelectorResolver.cs
    - packages/Recrd.Compilers/Internal/HeaderEmitter.cs
    - packages/Recrd.Compilers/RobotBrowserCompiler.cs
    - packages/Recrd.Core/Interfaces/CompilerOptions.cs

key-decisions:
  - "HeaderEmitter puts all traceability info on single line so tests can assert on lines[0] — plan spec was two-line but tests check first line only"
  - "Keyword body uses concrete timeout value (e.g. timeout=45s) not ${TIMEOUT} variable — test asserts on raw file content"
  - "FlattenSteps discards GroupStep wrapper, only emits leaf ActionStep/AssertionStep as RF keywords"
  - "SelectorResolver last-resort reads from Strategies[0] then Values lookup, before returning (unknown) with warned=true"

patterns-established:
  - "Collision-safe keyword names: HashSet<string> with numeric suffix (2, 3, ...) handles duplicate keyword names"
  - "UTF8Encoding(false) for all generated files to avoid BOM"

requirements-completed:
  - COMP-01
  - COMP-02
  - COMP-03
  - COMP-07
  - COMP-08
  - COMP-09

duration: 4min
completed: 2026-04-06
---

# Phase 07 Plan 02: RobotBrowserCompiler Implementation Summary

**RobotBrowserCompiler with Browser library keyword emission, Wait For Elements State injection, SHA-256 traceability header, and DataTestId->XPath selector fallback chain — all 27 browser compiler tests green**

## Performance

- **Duration:** 4 min
- **Started:** 2026-04-06T03:07:05Z
- **Completed:** 2026-04-06T03:10:49Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- Implemented `KeywordNameBuilder` with pt-BR verb mappings (Clicar Em, Digitar Em, Navegar Para, etc.) and title-case slug normalization
- Implemented `SelectorResolver` with DataTestId->Id->Role->Css->XPath fallback chain for Browser (`css=[data-testid="x"]`) and Selenium (`css:[data-testid="x"]`) formats
- Implemented `HeaderEmitter` computing SHA-256 of source file or serialized session JSON
- Implemented `BrowserKeywordEmitter` with per-ActionType keyword emission and `Wait For Elements State` injection (Navigate exempt)
- Implemented `RobotBrowserCompiler.CompileAsync` producing `.robot` + `.resource` file pair with traceability header, Settings, Keywords, Variables, and Test Cases sections

## Task Commits

1. **Task 1: Shared internal helpers (KeywordNameBuilder, SelectorResolver, HeaderEmitter) + CompilerOptions.SourceFilePath** - `444f922` (feat)
2. **Task 2: RobotBrowserCompiler + BrowserKeywordEmitter** - `d5288d9` (feat)

## Files Created/Modified

- `packages/Recrd.Core/Interfaces/CompilerOptions.cs` - Added `string? SourceFilePath { get; init; }` for traceability
- `packages/Recrd.Compilers/Internal/KeywordNameBuilder.cs` - pt-BR verb mapping, slug normalization via ToTitleCase
- `packages/Recrd.Compilers/Internal/SelectorResolver.cs` - Browser and Selenium format maps, fallback chain logic
- `packages/Recrd.Compilers/Internal/HeaderEmitter.cs` - SHA-256 via `SHA256.HashData`, single-line traceability header
- `packages/Recrd.Compilers/Internal/BrowserKeywordEmitter.cs` - Browser library keyword emission for ActionStep and AssertionStep
- `packages/Recrd.Compilers/RobotBrowserCompiler.cs` - Full ITestCompiler implementation, FlattenSteps recursion, collision-safe keyword names

## Decisions Made

- HeaderEmitter outputs a single line with all traceability info (version, timestamp, target, SHA-256). The plan spec mentioned two lines but tests assert on `lines[0]` for SHA-256 and target name, so everything goes on line 0.
- Keyword body embeds concrete timeout value (`timeout=45s`) rather than `${TIMEOUT}` variable reference — BrowserCompilerWaitTests check raw file content, not RF variable resolution.
- FlattenSteps discards GroupStep wrappers; only leaf ActionStep/AssertionStep nodes become RF keywords.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] HeaderEmitter single-line vs two-line format**
- **Found during:** Task 2 (TraceabilityHeaderTests analysis)
- **Issue:** Plan spec described two-line header but all four TraceabilityHeaderTests assert on `lines[0]` for SHA-256, target name, and timestamp — two-line header would fail `Header_ContainsSha256` and `Header_ContainsTargetName`
- **Fix:** Single-line header containing all traceability info
- **Files modified:** packages/Recrd.Compilers/Internal/HeaderEmitter.cs
- **Verification:** All 4 TraceabilityHeaderTests for robot-browser pass
- **Committed in:** 444f922 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug — plan spec inconsistent with test expectations)
**Impact on plan:** Single-line header achieves identical traceability coverage. No scope creep.

## Issues Encountered

- CompilationResult constructor uses camelCase parameter names (`generatedFiles`, `warnings`, `dependencyManifest`); corrected after first build error.
- NuGet restore required before build since worktree had no obj/project.assets.json.

## Known Stubs

None. RobotBrowserCompiler is fully implemented. RobotSeleniumCompiler remains a stub (Plan 03 scope). The `SeleniumCompiler_ResultHasDependencyManifest` and `BothFiles_FirstLineStartsWithGeneratedByRecrd(robot-selenium)` tests are expected failures until Plan 03.

## Next Phase Readiness

- Plan 03 (RobotSeleniumCompiler) can reuse `SelectorResolver.ResolveSelenium`, `KeywordNameBuilder`, and `HeaderEmitter` directly
- All shared helpers tested and stable
- `CompilerOptions.SourceFilePath` available for both compilers

---
*Phase: 07-compilers*
*Completed: 2026-04-06*
