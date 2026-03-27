---
phase: 04-gherkin-generator
plan: 01
subsystem: testing
tags: [dotnet, xunit, tdd, gherkin, csharp]

# Dependency graph
requires:
  - phase: 02-core-ast-types-interfaces
    provides: Session, ActionStep, AssertionStep, GroupStep, Selector, Variable AST types and IDataProvider interface
  - phase: 03-data-providers
    provides: IDataProvider contract and InMemoryDataProvider pattern for streaming test data

provides:
  - GherkinException sealed exception class with VariableName and DataFilePath properties
  - IGherkinGenerator interface with GenerateAsync method
  - GherkinGenerator stub (throws NotImplementedException)
  - GherkinGeneratorOptions record with Tags, DataFilePath, and WarningWriter properties
  - 5 test suite files covering all GHER-01 through GHER-09 requirements (22 tests, all failing)
  - tdd/phase-04 branch with committed red tests

affects:
  - 04-02-PLAN (green phase — implements GherkinGenerator against these tests)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "InMemoryDataProvider inner class pattern for streaming test data in async-enumerable tests"
    - "GherkinGeneratorOptions carries WarningWriter for stderr injection in tests"
    - "MakeSelector static helper in each test class for building Selector instances"

key-files:
  created:
    - packages/Recrd.Gherkin/GherkinException.cs
    - packages/Recrd.Gherkin/IGherkinGenerator.cs
    - packages/Recrd.Gherkin/GherkinGenerator.cs
    - packages/Recrd.Gherkin/GherkinGeneratorOptions.cs
    - tests/Recrd.Gherkin.Tests/FixedScenarioTests.cs
    - tests/Recrd.Gherkin.Tests/DataDrivenTests.cs
    - tests/Recrd.Gherkin.Tests/VariableMismatchTests.cs
    - tests/Recrd.Gherkin.Tests/GroupingTests.cs
    - tests/Recrd.Gherkin.Tests/DeterminismTests.cs
  modified:
    - packages/Recrd.Gherkin/GherkinGeneratorOptions.cs (extended with DataFilePath and WarningWriter)

key-decisions:
  - "GherkinGeneratorOptions extended with DataFilePath and WarningWriter to support GHER-03/04 test contracts"
  - "WarningWriter injected via options (not constructor) to match the stateless GherkinGenerator design"
  - "InMemoryDataProvider as inner class per test suite (not shared) to keep test files self-contained"

patterns-established:
  - "TDD red phase: all tests committed failing before any implementation begins"
  - "InMemoryDataProvider inner class: implements IDataProvider as async enumerable over in-memory rows"

requirements-completed: [GHER-01, GHER-02, GHER-03, GHER-04, GHER-05, GHER-06, GHER-07, GHER-08, GHER-09]

# Metrics
duration: 4min
completed: 2026-03-27
---

# Phase 04 Plan 01: Gherkin Generator Red Phase Summary

**TDD red phase: GherkinException, IGherkinGenerator, GherkinGenerator stub, and 22 failing tests covering all GHER-01 through GHER-09 requirements committed on tdd/phase-04 branch**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-27T19:32:33Z
- **Completed:** 2026-03-27T19:36:42Z
- **Tasks:** 2 completed
- **Files modified:** 10

## Accomplishments

- Created the public `Recrd.Gherkin` API surface: `GherkinException`, `IGherkinGenerator`, `GherkinGenerator` (stub), `GherkinGeneratorOptions`
- Deleted `Placeholder.cs` from both `packages/Recrd.Gherkin/` and `tests/Recrd.Gherkin.Tests/`
- Wrote 22 failing tests across 5 test suites covering every GHER requirement on the `tdd/phase-04` branch

## Task Commits

Each task was committed atomically:

1. **Task 1: Create public types** - `0f804b9` (feat)
2. **Task 2: Create all 5 red test suites on tdd/phase-04 branch** - `c4ae095` (test)

## Files Created/Modified

- `packages/Recrd.Gherkin/GherkinException.cs` - Sealed exception with VariableName and DataFilePath properties
- `packages/Recrd.Gherkin/IGherkinGenerator.cs` - Public interface with single GenerateAsync method
- `packages/Recrd.Gherkin/GherkinGenerator.cs` - Stub implementation (throws NotImplementedException)
- `packages/Recrd.Gherkin/GherkinGeneratorOptions.cs` - Options record (Tags, DataFilePath, WarningWriter)
- `tests/Recrd.Gherkin.Tests/FixedScenarioTests.cs` - 5 tests for GHER-01, GHER-07, GHER-08
- `tests/Recrd.Gherkin.Tests/DataDrivenTests.cs` - 4 tests for GHER-02, GHER-09
- `tests/Recrd.Gherkin.Tests/VariableMismatchTests.cs` - 4 tests for GHER-03, GHER-04
- `tests/Recrd.Gherkin.Tests/GroupingTests.cs` - 7 tests for GHER-05, GHER-06
- `tests/Recrd.Gherkin.Tests/DeterminismTests.cs` - 2 tests for GHER-07

## Decisions Made

- `GherkinGeneratorOptions` extended with `DataFilePath` and `WarningWriter` to satisfy GHER-03/GHER-04 test contracts requiring path in exception and warning text injection
- `WarningWriter` injected via options (not constructor) to match the stateless `GherkinGenerator` design pattern

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added DataFilePath and WarningWriter to GherkinGeneratorOptions**
- **Found during:** Task 2 (VariableMismatchTests creation)
- **Issue:** Plan specified testing `GherkinException.DataFilePath` and `extra_col` warning text, but `GherkinGeneratorOptions` only had `Tags`. Without `DataFilePath` in options, tests could not pass the file path context to the generator, and without `WarningWriter`, tests had no way to capture warning output.
- **Fix:** Added `string? DataFilePath { get; init; }` and `TextWriter? WarningWriter { get; init; }` to `GherkinGeneratorOptions`
- **Files modified:** `packages/Recrd.Gherkin/GherkinGeneratorOptions.cs`
- **Verification:** Test project builds with no warnings; format check passes
- **Committed in:** `c4ae095` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (missing critical options properties)
**Impact on plan:** Required to make tests compilable against the declared GHER-03/04 contract. No scope creep.

## Issues Encountered

- Worktree required `dotnet restore` before build (assets file not present in fresh worktree) — resolved with restore command before building.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All 22 tests compile and fail with `NotImplementedException` on the `tdd/phase-04` branch
- Plan 04-02 (green phase) can now implement `GherkinGenerator` against these contracts
- The `tdd/phase-04` branch diverged from `worktree-agent-a773b487` and carries the red tests

---
*Phase: 04-gherkin-generator*
*Completed: 2026-03-27*

## Self-Check: PASSED

All required files verified present:
- packages/Recrd.Gherkin/GherkinException.cs: FOUND
- packages/Recrd.Gherkin/IGherkinGenerator.cs: FOUND
- packages/Recrd.Gherkin/GherkinGenerator.cs: FOUND
- packages/Recrd.Gherkin/GherkinGeneratorOptions.cs: FOUND
- tests/Recrd.Gherkin.Tests/FixedScenarioTests.cs: FOUND
- tests/Recrd.Gherkin.Tests/DataDrivenTests.cs: FOUND
- tests/Recrd.Gherkin.Tests/VariableMismatchTests.cs: FOUND
- tests/Recrd.Gherkin.Tests/GroupingTests.cs: FOUND
- tests/Recrd.Gherkin.Tests/DeterminismTests.cs: FOUND

Commits verified: 0f804b9 (FOUND), c4ae095 (FOUND)
