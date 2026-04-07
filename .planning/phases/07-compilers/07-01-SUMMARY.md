---
phase: 07-compilers
plan: 01
subsystem: testing
tags: [xunit, dotnet, tdd, robot-framework, compiler]

# Dependency graph
requires:
  - phase: 02-core-ast-types-interfaces
    provides: ITestCompiler, CompilationResult, CompilerOptions, Selector, ActionStep, AssertionStep, Session
  - phase: 05-ci-pipeline
    provides: InternalsVisibleTo pattern, coverage gate conventions
provides:
  - Red test suites for COMP-01 through COMP-09 (45 failing tests)
  - Production stubs for RobotBrowserCompiler, RobotSeleniumCompiler, and 3 internal helpers
  - tdd/phase-07 branch with complete TDD red state
affects: [07-02, 07-03, 07-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - InternalsVisibleTo via AssemblyAttribute in csproj for internal helper test access
    - MakeSession/MakeSelector helper methods per test class for minimal session construction
    - Temp directory with Guid + cleanup in finally block for compiler output tests

key-files:
  created:
    - packages/Recrd.Compilers/RobotBrowserCompiler.cs
    - packages/Recrd.Compilers/RobotSeleniumCompiler.cs
    - packages/Recrd.Compilers/Internal/KeywordNameBuilder.cs
    - packages/Recrd.Compilers/Internal/SelectorResolver.cs
    - packages/Recrd.Compilers/Internal/HeaderEmitter.cs
    - tests/Recrd.Compilers.Tests/BrowserCompilerOutputTests.cs
    - tests/Recrd.Compilers.Tests/BrowserCompilerSelectorTests.cs
    - tests/Recrd.Compilers.Tests/BrowserCompilerWaitTests.cs
    - tests/Recrd.Compilers.Tests/SeleniumCompilerOutputTests.cs
    - tests/Recrd.Compilers.Tests/SeleniumCompilerSelectorTests.cs
    - tests/Recrd.Compilers.Tests/SeleniumCompilerWaitTests.cs
    - tests/Recrd.Compilers.Tests/TraceabilityHeaderTests.cs
    - tests/Recrd.Compilers.Tests/CompilationResultTests.cs
    - tests/Recrd.Compilers.Tests/KeywordNameBuilderTests.cs
  modified:
    - packages/Recrd.Compilers/Recrd.Compilers.csproj

key-decisions:
  - "InternalsVisibleTo added to Recrd.Compilers.csproj to expose KeywordNameBuilder to test project"
  - "tdd/phase-07 branch created in worktree per D-11 TDD mandate"

patterns-established:
  - "Pattern 1: MakeSession/MakeSelector private helpers in each test class — avoids shared state, keeps tests self-contained"
  - "Pattern 2: Temp directory with Guid for compiler output isolation, cleaned up in finally block"
  - "Pattern 3: InternalsVisibleTo via MSBuild AssemblyAttribute (same as Phase 6 pattern)"

requirements-completed:
  - COMP-01
  - COMP-02
  - COMP-03
  - COMP-04
  - COMP-05
  - COMP-06
  - COMP-07
  - COMP-08
  - COMP-09

# Metrics
duration: 5min
completed: 2026-04-06
---

# Phase 7 Plan 01: TDD Red Phase — Compilers Summary

**45 failing tests across 9 test suites (COMP-01 through COMP-09) + 5 production stubs committed on tdd/phase-07 branch, establishing the full compiler test contract**

## Performance

- **Duration:** 5 min
- **Started:** 2026-04-06T02:54:59Z
- **Completed:** 2026-04-06T03:00:19Z
- **Tasks:** 2
- **Files modified:** 15 (14 created + 1 modified)

## Accomplishments

- Created 5 production stub files (RobotBrowserCompiler, RobotSeleniumCompiler, and 3 internal helpers) all throwing NotImplementedException — compilation anchors for the red phase
- Created 9 test files covering all compiler requirements (COMP-01 through COMP-09): output structure, selector resolution, wait injection, implicit wait, traceability headers, compilation result contract, and pt-BR keyword name generation
- 45 tests all fail with NotImplementedException — correct TDD red state on tdd/phase-07 branch

## Task Commits

Each task was committed atomically:

1. **Task 1: Create production stubs and internal helper skeletons** - `493b3db` (feat)
2. **Task 2: Create all 9 red test suites covering COMP-01 through COMP-09** - `2540e72` (test)

**Plan metadata:** (docs commit to follow)

## Files Created/Modified

- `packages/Recrd.Compilers/RobotBrowserCompiler.cs` - ITestCompiler stub for Browser library target
- `packages/Recrd.Compilers/RobotSeleniumCompiler.cs` - ITestCompiler stub for SeleniumLibrary target
- `packages/Recrd.Compilers/Internal/KeywordNameBuilder.cs` - Stub for pt-BR keyword name generation
- `packages/Recrd.Compilers/Internal/SelectorResolver.cs` - Stub for selector strategy resolution
- `packages/Recrd.Compilers/Internal/HeaderEmitter.cs` - Stub for traceability header generation
- `packages/Recrd.Compilers/Recrd.Compilers.csproj` - Added InternalsVisibleTo Recrd.Compilers.Tests
- `tests/Recrd.Compilers.Tests/BrowserCompilerOutputTests.cs` - 7 tests: .robot/.resource files, Settings, keywords (COMP-01, COMP-08)
- `tests/Recrd.Compilers.Tests/BrowserCompilerSelectorTests.cs` - 4 tests: DataTestId, Id fallback, exhausted chain warning (COMP-02)
- `tests/Recrd.Compilers.Tests/BrowserCompilerWaitTests.cs` - 6 tests: Wait For Elements State, Navigate skip, timeout (COMP-03)
- `tests/Recrd.Compilers.Tests/SeleniumCompilerOutputTests.cs` - 5 tests: SeleniumLibrary, RF-Version, Suite Setup/Teardown (COMP-04, COMP-08)
- `tests/Recrd.Compilers.Tests/SeleniumCompilerSelectorTests.cs` - 4 tests: id:/css:/xpath:/css:[data-testid] locators (COMP-05)
- `tests/Recrd.Compilers.Tests/SeleniumCompilerWaitTests.cs` - 3 tests: Set Selenium Implicit Wait, timeout, no per-step Wait Until (COMP-06)
- `tests/Recrd.Compilers.Tests/TraceabilityHeaderTests.cs` - 4 tests: SHA-256, target name, ISO 8601 timestamp (COMP-07)
- `tests/Recrd.Compilers.Tests/CompilationResultTests.cs` - 4 tests: file count, DependencyManifest keys, Warnings not null (COMP-09)
- `tests/Recrd.Compilers.Tests/KeywordNameBuilderTests.cs` - 6 tests: pt-BR slug names for all ActionTypes (D-02)

## Decisions Made

- Added `InternalsVisibleTo` to `Recrd.Compilers.csproj` to expose `KeywordNameBuilder` (internal class) to the test project. Uses MSBuild AssemblyAttribute pattern established in Phase 6, not a separate AssemblyInfo.cs file.
- Created `tdd/phase-07` branch in the worktree (branched from main/d2effa1) per D-11 TDD mandate.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added InternalsVisibleTo to enable KeywordNameBuilder test access**
- **Found during:** Task 2 (test suite creation)
- **Issue:** `KeywordNameBuilder` is `internal static class` — test project could not access it, causing CS0122 compilation errors in `KeywordNameBuilderTests.cs`
- **Fix:** Added `InternalsVisibleTo` AssemblyAttribute to `Recrd.Compilers.csproj` pointing to `Recrd.Compilers.Tests` — same pattern used in Phase 6 (documented in STATE.md key decisions)
- **Files modified:** `packages/Recrd.Compilers/Recrd.Compilers.csproj`
- **Verification:** Build succeeded (0 errors), all 45 tests now run and fail with NotImplementedException
- **Committed in:** `2540e72` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Required for the tests to compile at all. Not scope creep — prerequisite for the test contract to be established.

## Issues Encountered

None — all 45 tests compile and fail in the expected red state.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- TDD red phase complete for COMP-01 through COMP-09
- `tdd/phase-07` branch has all 45 tests failing (ready for green phase)
- Next: Plan 07-02 (RobotBrowserCompiler implementation — green phase for COMP-01, COMP-02, COMP-03, COMP-07, COMP-08)

## Self-Check: PASSED

All key files verified present on disk. Both commits (493b3db, 2540e72) confirmed in git log. 45 tests failing in red state.

---
*Phase: 07-compilers*
*Completed: 2026-04-06*
