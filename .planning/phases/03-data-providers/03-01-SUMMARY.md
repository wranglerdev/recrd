---
phase: 03-data-providers
plan: 01
subsystem: testing
tags: [csvhelper, xunit, tdd, data-providers, csv, json, async-enumerable]

# Dependency graph
requires:
  - phase: 02-core-ast-types-interfaces
    provides: IDataProvider interface in Recrd.Core
provides:
  - DataParseException with LineNumber, OffendingLine, FilePath diagnostic properties
  - CsvDataProvider stub implementing IDataProvider (NotImplementedException)
  - JsonDataProvider stub implementing IDataProvider (NotImplementedException)
  - CsvHelper 33.1.0 PackageReference in Recrd.Data.csproj
  - 21 red (failing) tests for DATA-01 through DATA-05 on tdd/phase-03 branch
affects: [03-02-csv-implementation, 03-03-json-implementation, 03-04-integration]

# Tech tracking
tech-stack:
  added: [CsvHelper 33.1.0]
  patterns: [TDD red-green cycle, async iterator with pragma-suppressed unreachable yield break]

key-files:
  created:
    - packages/Recrd.Data/DataParseException.cs
    - packages/Recrd.Data/CsvDataProvider.cs
    - packages/Recrd.Data/JsonDataProvider.cs
    - tests/Recrd.Data.Tests/CsvDataProviderTests.cs
    - tests/Recrd.Data.Tests/JsonDataProviderTests.cs
  modified:
    - packages/Recrd.Data/Recrd.Data.csproj

key-decisions:
  - "Added await Task.CompletedTask + CS0162 pragma in stubs — async iterator with throw needs a yield, but yield after throw is unreachable (compiler error); pragma suppresses the warning cleanly while keeping the NotImplementedException throw"
  - "tdd/phase-03 branch created from main per D-08 TDD mandate; all red tests committed there before any implementation"

patterns-established:
  - "Stub async iterators use await Task.CompletedTask before throw NotImplementedException to satisfy async method requirements, with #pragma CS0162 around yield break"
  - "Test files create temp files via Path.GetTempFileName() with try/finally cleanup for isolation"
  - "DataParseException tests use Assert.ThrowsAsync with full await foreach enumeration to trigger exceptions"

requirements-completed: [DATA-01, DATA-02, DATA-03, DATA-04, DATA-05]

# Metrics
duration: 3min
completed: 2026-03-27
---

# Phase 03 Plan 01: TDD Red Phase — Data Providers Summary

**DataParseException, CsvDataProvider/JsonDataProvider stubs with CsvHelper 33.1.0, and 21 failing xUnit tests covering RFC 4180 streaming, dot-notation JSON flattening, parse error diagnostics, and 50MB memory bounds — all red on tdd/phase-03**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-27T00:47:13Z
- **Completed:** 2026-03-27T00:50:21Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- Created `DataParseException` with `LineNumber`, `OffendingLine`, `FilePath` diagnostic properties for rich parse error reporting
- Added CsvHelper 33.1.0 as PackageReference; created `CsvDataProvider` and `JsonDataProvider` stubs implementing `IDataProvider` — project builds clean
- Wrote 21 red xUnit tests on `tdd/phase-03` branch: 11 for CSV (RFC 4180, BOM, custom delimiter, quoted fields, parse errors, 50MB memory test, cancellation) and 9 for JSON (flat/nested/deep flattening, array-skip, null/bool/number rendering, non-array root errors)

## Task Commits

Each task was committed atomically:

1. **Task 1: DataParseException, stub providers, CsvHelper** - `c82397e` (feat)
2. **Task 2: Red test suites on tdd/phase-03** - `11f43e3` (test)

**Plan metadata:** _(docs commit below)_

## Files Created/Modified
- `packages/Recrd.Data/DataParseException.cs` - Sealed exception with LineNumber, OffendingLine, FilePath
- `packages/Recrd.Data/CsvDataProvider.cs` - Stub IDataProvider — NotImplementedException in StreamAsync
- `packages/Recrd.Data/JsonDataProvider.cs` - Stub IDataProvider — NotImplementedException in StreamAsync
- `packages/Recrd.Data/Recrd.Data.csproj` - Added CsvHelper 33.1.0 PackageReference; Placeholder.cs deleted
- `tests/Recrd.Data.Tests/CsvDataProviderTests.cs` - 11 tests: DATA-01/02/03 (all red)
- `tests/Recrd.Data.Tests/JsonDataProviderTests.cs` - 9 tests + 1 Theory: DATA-04/05 (all red)

## Decisions Made
- Used `await Task.CompletedTask` + `#pragma CS0162` in stub `StreamAsync` methods. The plan specified `yield break` after `throw new NotImplementedException()` as "unreachable — satisfies compiler requirement", but the C# compiler emits CS0162 (unreachable code) as an error by default. The pragma cleanly suppresses it without changing behavior.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed CS0162 unreachable code error in stub async iterators**
- **Found during:** Task 1 (verify build step)
- **Issue:** Plan specified `yield break` after `throw new NotImplementedException()`. The C# compiler rejects this with error CS0162 (unreachable code detected), causing the build to fail.
- **Fix:** Added `await Task.CompletedTask` before the throw, and wrapped `yield break` in `#pragma warning disable/restore CS0162` to suppress the error while retaining the yield that satisfies the async iterator contract.
- **Files modified:** `packages/Recrd.Data/CsvDataProvider.cs`, `packages/Recrd.Data/JsonDataProvider.cs`
- **Verification:** `dotnet build packages/Recrd.Data/Recrd.Data.csproj` exits 0, 0 warnings, 0 errors
- **Committed in:** `c82397e` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — compiler error in plan-provided stub code)
**Impact on plan:** Minimal scope change — only affects stub syntax, not contract or behavior. All acceptance criteria met.

## Issues Encountered
- None beyond the CS0162 deviation above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- `tdd/phase-03` branch has all red tests committed — Plans 02 and 03 can immediately begin implementing `CsvDataProvider` and `JsonDataProvider` to turn tests green
- `Recrd.Data.csproj` already has CsvHelper 33.1.0 available for Plan 02's CSV implementation
- `DataParseException` is defined and ready to be thrown from both implementations

---
*Phase: 03-data-providers*
*Completed: 2026-03-27*
