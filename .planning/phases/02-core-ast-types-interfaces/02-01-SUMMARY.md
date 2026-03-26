---
phase: 02-core-ast-types-interfaces
plan: 01
subsystem: testing
tags: [xunit, dotnet, tdd, csharp, records, system-text-json, channels]

# Dependency graph
requires:
  - phase: 01-monorepo-scaffold
    provides: test project scaffolding with xUnit + Moq + coverlet, Recrd.Core.Tests.csproj with ProjectReference to Recrd.Core
provides:
  - 5 red test suite files covering all 13 CORE requirements (TDD red phase)
  - tdd/phase-02 branch with atomic red commit
  - Placeholder files deleted (PlaceholderTests.cs and Placeholder.cs)
affects:
  - 02-02 (implements production types these tests will exercise)
  - 02-03 (implements channel pipeline these tests reference)
  - 02-04 (implements interfaces these tests reflect on)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Behavior-suite test organization: tests grouped by behavior domain, not by type (SessionSerializationTests, StepModelTests, SelectorVariableTests, ChannelPipelineTests, InterfaceContractTests)"
    - "TheoryData<T> with Enum.GetValues<T>() for exhaustive enum-driven theory tests"
    - "Reflection-based interface contract tests using typeof(IFoo).GetMethod/GetProperty"
    - "TDD red phase: tests committed with compilation errors expected; tdd/phase-* branch prefix enables CI-06 tolerance"

key-files:
  created:
    - tests/Recrd.Core.Tests/SessionSerializationTests.cs
    - tests/Recrd.Core.Tests/StepModelTests.cs
    - tests/Recrd.Core.Tests/SelectorVariableTests.cs
    - tests/Recrd.Core.Tests/ChannelPipelineTests.cs
    - tests/Recrd.Core.Tests/InterfaceContractTests.cs
  modified: []

key-decisions:
  - "All 5 test suites committed in one atomic red commit on tdd/phase-02 branch per D-13; CI-06 tolerates failures on tdd/phase-* prefix"
  - "RecordingChannel chosen as wrapper class name (D-09 discretion item)"
  - "RecordedEventType enum used as EventType discriminator on RecordedEvent (CORE-12)"
  - "Variable name validation via constructor ArgumentException (D-06 discretion: chose constructor throw over static factory)"

patterns-established:
  - "TDD red commit: syntactically valid C# that would compile once production types exist, committed before any implementation"
  - "Interface contract tests via reflection: typeof(IFoo).GetMethod('MethodName') asserts method existence and return type"
  - "Repo root discovery via walking up to recrd.sln for path-independent tests"

requirements-completed:
  - CORE-01
  - CORE-02
  - CORE-03
  - CORE-04
  - CORE-05
  - CORE-06
  - CORE-07
  - CORE-08
  - CORE-09
  - CORE-10
  - CORE-11
  - CORE-12
  - CORE-13

# Metrics
duration: 2min
completed: 2026-03-26
---

# Phase 02 Plan 01: Core AST Red Tests Summary

**5 red test suites (599 lines) covering all 13 CORE requirements committed on tdd/phase-02 branch, with xUnit Theory/Fact tests for Session JSON round-trip, step constructibility, selector priority, channel backpressure, and interface reflection contracts**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-26T19:38:40Z
- **Completed:** 2026-03-26T19:40:38Z
- **Tasks:** 2
- **Files modified:** 7 (5 created, 2 deleted)

## Accomplishments
- Created 5 behavior-suite test files covering all CORE-01 through CORE-13 requirements
- Deleted PlaceholderTests.cs and Placeholder.cs as specified
- Committed all 5 test files atomically on `tdd/phase-02` branch in a single red commit
- Tests reference `Recrd.Core.Ast`, `Recrd.Core.Serialization`, `Recrd.Core.Pipeline`, `Recrd.Core.Interfaces` namespaces that don't exist yet — build produces 16 CS errors as expected

## Task Commits

Each task was committed atomically:

1. **Task 1: Create all 5 red test suite files** - (no separate commit; staged for Task 2's atomic commit per plan)
2. **Task 2: Create tdd/phase-02 branch and commit red tests** - `c89ec64` (test)

## Files Created/Modified
- `tests/Recrd.Core.Tests/SessionSerializationTests.cs` - Round-trip JSON tests for Session with polymorphic steps and variable fields (CORE-01)
- `tests/Recrd.Core.Tests/StepModelTests.cs` - Constructibility tests for all ActionType, AssertionType, GroupType enum values via Theory (CORE-02, 03, 04)
- `tests/Recrd.Core.Tests/SelectorVariableTests.cs` - Selector priority ranking and Variable name regex validation tests (CORE-05, 06)
- `tests/Recrd.Core.Tests/ChannelPipelineTests.cs` - RecordingChannel backpressure, cancellation, drain-without-deadlock, and RecordedEvent field tests (CORE-11, 12)
- `tests/Recrd.Core.Tests/InterfaceContractTests.cs` - Reflection-based interface contract tests and Recrd.Core zero-dep assertion (CORE-07 to 10, CORE-13)
- `tests/Recrd.Core.Tests/PlaceholderTests.cs` - DELETED (replaced by real test suites)
- `packages/Recrd.Core/Placeholder.cs` - DELETED (replaced by real implementation in plan 02)

## Decisions Made
- Used `RecordingChannel` as the wrapper class name (discretion item from D-09)
- Used `RecordedEventType` enum to discriminate event types on `RecordedEvent` (natural C# pattern matching fit)
- Constructor validation for `Variable` name (throws `ArgumentException`) rather than static factory — keeps record initialization consistent
- Repo root discovery via walking directory tree up to `recrd.sln` for path-independent `RecrdCore_HasZeroRecrdPackageDependencies` test

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None. Build produced 16 compilation errors as expected — all from missing production types in `Recrd.Core.Ast`, `Recrd.Core.Serialization`, `Recrd.Core.Pipeline`, `Recrd.Core.Interfaces` namespaces. This is the correct TDD red phase state.

## Known Stubs

None. This plan produces only test files; no production code or UI rendering paths were created.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- All 5 red test files committed on `tdd/phase-02` branch; ready for Plan 02 to implement production types
- Plan 02 must implement: `Recrd.Core.Ast` (Session, ActionStep, AssertionStep, GroupStep, Selector, Variable, SessionMetadata, ViewportSize, enums), `Recrd.Core.Serialization` (RecrdJsonContext), `Recrd.Core.Pipeline` (RecordingChannel, RecordedEvent, RecordedEventType), `Recrd.Core.Interfaces` (ITestCompiler, IDataProvider, IEventInterceptor, IAssertionProvider, CompilationResult, CompilerOptions)
- No blockers

---
*Phase: 02-core-ast-types-interfaces*
*Completed: 2026-03-26*
