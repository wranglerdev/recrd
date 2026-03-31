---
phase: 06-recording-engine
plan: 01
subsystem: testing
tags: [dotnet, xunit, playwright, coverlet, tdd, recording-engine]

# Dependency graph
requires:
  - phase: 05-ci-pipeline
    provides: CI pipeline with coverage gates pattern for Recrd.* packages
  - phase: 02-core-ast-types-interfaces
    provides: AST types (Session, ActionStep, AssertionStep, Variable, Selector, ViewportSize) and IRecordingChannel
provides:
  - IRecorderEngine interface in Recrd.Core.Interfaces with 5 async methods (StartAsync, PauseAsync, ResumeAsync, StopAsync, RecoverAsync)
  - RecorderOptions sealed record with BrowserEngine, Headed, ViewportSize, BaseUrl, OutputDirectory, SnapshotInterval
  - 37 failing test stubs across 6 suites on tdd/phase-06 branch covering all 15 REC requirements
  - coverlet.msbuild 6.0.4 + Microsoft.Playwright.Xunit 1.58.0 in Recrd.Recording.Tests
  - CI coverage gate enforcing 90% line coverage for Recrd.Recording (D-02 compliance)
affects:
  - 06-02 (PlaywrightRecorderEngine implementation — uses IRecorderEngine contract)
  - 06-03 (lifecycle management — StopAsync/RecoverAsync test stubs)
  - 06-04 (inspector panel — InspectorPanelTests stubs)
  - 06-05 (popup handling — PopupHandlingTests stubs)

# Tech tracking
tech-stack:
  added:
    - coverlet.msbuild 6.0.4 (replaces coverlet.collector 8.0.1 in Recrd.Recording.Tests)
    - Microsoft.Playwright.Xunit 1.58.0 (Playwright xUnit integration for Recording tests)
  patterns:
    - TDD red phase: all 37 tests committed failing before implementation begins (D-03)
    - Assert.Fail pattern for red-phase stubs (compile-time valid, always fails at runtime)
    - tdd/phase-* branch prefix for CI test-failure tolerance during red phase

key-files:
  created:
    - packages/Recrd.Core/Interfaces/IRecorderEngine.cs
    - packages/Recrd.Core/Interfaces/RecorderOptions.cs
    - tests/Recrd.Recording.Tests/BrowserContextTests.cs
    - tests/Recrd.Recording.Tests/EventCaptureTests.cs
    - tests/Recrd.Recording.Tests/SessionLifecycleTests.cs
    - tests/Recrd.Recording.Tests/SnapshotRecoveryTests.cs
    - tests/Recrd.Recording.Tests/InspectorPanelTests.cs
    - tests/Recrd.Recording.Tests/PopupHandlingTests.cs
  modified:
    - packages/Recrd.Recording/Recrd.Recording.csproj (added conditional EmbeddedResource stubs for Plans 02/04)
    - tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj (replaced coverlet.collector with coverlet.msbuild, added Playwright.Xunit)
    - .github/workflows/ci.yml (added Playwright browser install + Recrd.Recording 90% coverage gate)

key-decisions:
  - "Assert.Fail used for all red-phase stubs — compiles immediately without production types, always fails at runtime (satisfies D-03)"
  - "EmbeddedResource items in Recrd.Recording.csproj use Condition=Exists() to avoid build warnings before Plans 02 and 04 create the actual files"
  - "Microsoft.Playwright added directly to test csproj alongside Microsoft.Playwright.Xunit — needed for Page/BrowserContext types in future test implementations"
  - "Playwright browser install step placed before Recrd.Recording coverage gate in CI — required so chromium binary exists before Recording tests run"

patterns-established:
  - "Red-phase stubs use Assert.Fail('Red: REC-XX — description') naming for traceability to requirement IDs"
  - "Test file name maps directly to requirement group: BrowserContextTests→REC-01, EventCaptureTests→REC-02 to 05, etc."

requirements-completed:
  - REC-01
  - REC-02
  - REC-03
  - REC-04
  - REC-05
  - REC-06
  - REC-07
  - REC-08
  - REC-09
  - REC-10
  - REC-11
  - REC-12
  - REC-13
  - REC-14
  - REC-15

# Metrics
duration: 6min
completed: 2026-03-31
---

# Phase 06 Plan 01: Recording Engine TDD Red Phase Summary

**IRecorderEngine interface defined in Recrd.Core, 37 failing xUnit test stubs across 6 suites covering all 15 REC requirements committed on tdd/phase-06 branch, and 90% line coverage gate added to CI**

## Performance

- **Duration:** 6 min
- **Started:** 2026-03-31T00:31:36Z
- **Completed:** 2026-03-31T00:37:49Z
- **Tasks:** 3
- **Files modified:** 10

## Accomplishments

- `IRecorderEngine` interface with 5 async methods (StartAsync, PauseAsync, ResumeAsync, StopAsync, RecoverAsync) added to `Recrd.Core.Interfaces`
- `RecorderOptions` sealed record with 6 configurable properties added to `Recrd.Core.Interfaces`
- 37 failing test stubs across 6 files on `tdd/phase-06` branch — all compile, all fail (Assert.Fail), covering REC-01 through REC-15
- CI pipeline extended with Playwright browser install step and 90% line coverage gate for `Recrd.Recording`

## Task Commits

Each task was committed atomically:

1. **Task 1: Define IRecorderEngine interface and update project dependencies** - `56a6fe2` (feat)
2. **Task 2: Create all 6 red test suites with failing tests** - `f992294` (test)
3. **Task 3: Add 90% line coverage gate for Recrd.Recording to CI pipeline** - `f0db2e3` (chore)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `packages/Recrd.Core/Interfaces/IRecorderEngine.cs` - Interface contract for recording engine with 5 async methods and IAsyncDisposable
- `packages/Recrd.Core/Interfaces/RecorderOptions.cs` - Configuration record for browser engine, viewport, base URL, snapshot interval
- `tests/Recrd.Recording.Tests/BrowserContextTests.cs` - 4 tests for REC-01 (clean browser context, engine selection, viewport)
- `tests/Recrd.Recording.Tests/EventCaptureTests.cs` - 11 tests for REC-02 to REC-05 (7 event types, JS agent injection, selector extraction, channel push)
- `tests/Recrd.Recording.Tests/SessionLifecycleTests.cs` - 6 tests for REC-06 to REC-08 (pause, resume, stop/flush to .recrd file)
- `tests/Recrd.Recording.Tests/SnapshotRecoveryTests.cs` - 5 tests for REC-09 to REC-10 (partial snapshots every 30s, recover from partial)
- `tests/Recrd.Recording.Tests/InspectorPanelTests.cs` - 8 tests for REC-11 to REC-14 (inspector context, event stream, variable tagging, assertion builder)
- `tests/Recrd.Recording.Tests/PopupHandlingTests.cs` - 3 tests for REC-15 (popup event capture, scope marker, init script injection)
- `packages/Recrd.Recording/Recrd.Recording.csproj` - Added conditional EmbeddedResource stubs for Plans 02 and 04
- `tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj` - Replaced coverlet.collector with coverlet.msbuild 6.0.4, added Playwright.Xunit 1.58.0
- `.github/workflows/ci.yml` - Added Playwright browser install and Recrd.Recording 90% coverage gate

## Decisions Made

- `Assert.Fail` used for all red-phase stubs — no fake production types needed; tests compile immediately against existing Core types and always fail at runtime
- `EmbeddedResource` items in `Recrd.Recording.csproj` use `Condition=Exists()` to prevent build errors before Plans 02 and 04 create the actual script files
- Both `Microsoft.Playwright` and `Microsoft.Playwright.Xunit` added to test csproj — `.Xunit` provides the test base class, `.Playwright` provides `IPage`/`IBrowserContext` types needed in future green-phase tests
- Playwright browser install step uses `chromium` only (not all browsers) to keep CI fast

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- `IRecorderEngine` interface is the contract for all Plans 02–05 implementation work
- `tdd/phase-06` branch has red commit with 37 failing tests, CI tolerates failures on `tdd/phase-*` branches
- Plan 02 can immediately begin implementing `PlaywrightRecorderEngine` against the defined interface
- Coverage gate is in CI but will not block until the production implementation hits 90% line coverage

## Self-Check: PASSED

All files verified present/deleted. All 3 task commits verified in git log.

---
*Phase: 06-recording-engine*
*Completed: 2026-03-31*
