---
phase: 03-data-providers
plan: 02
subsystem: data
tags: [csvhelper, csv, streaming, iasyncenumerable, data-providers]

# Dependency graph
requires:
  - phase: 03-01
    provides: DataParseException, CsvDataProvider/JsonDataProvider stubs, CsvHelper 33.1.0, 11 red CsvDataProvider tests
provides:
  - Production CsvDataProvider implementing IDataProvider via CsvHelper 33.1.0
  - RFC 4180 CSV parsing with BOM tolerance (detectEncodingFromByteOrderMarks: true)
  - Configurable delimiter support via constructor parameter
  - DataParseException on malformed input with LineNumber and OffendingLine
  - Streaming IAsyncEnumerable with no ToList/ToArray — 50MB passes 100MB heap delta
affects: [03-03-json-provider, 03-04-integration-tests]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - yield-outside-try-catch: Use helper method (BuildRow) to wrap CsvHelper calls that may throw, keeping yield return in the async iterator body outside try/catch (C# restriction CS1626)
    - null-conditional in delegates: Use ?. null-conditional operators in BadDataFound lambda to avoid CS8602 nullable dereference errors

key-files:
  created: []
  modified:
    - packages/Recrd.Data/CsvDataProvider.cs
    - packages/Recrd.Data/JsonDataProvider.cs

key-decisions:
  - "BuildRow helper method pattern: yield return cannot appear inside a try/catch in C# — extracted row building to a separate synchronous method that can use try/catch freely"
  - "JsonDataProvider MoveNextAsync pattern: restructured to call MoveNextAsync in try/catch and yield return outside, satisfying CS1626 restriction"

patterns-established:
  - "yield-outside-try-catch: async iterators with exception translation must use helper methods or MoveNextAsync pattern to separate yield from catch"

requirements-completed: [DATA-01, DATA-02, DATA-03]

# Metrics
duration: 2min
completed: 2026-03-27
---

# Phase 03 Plan 02: CsvDataProvider Implementation Summary

**CsvDataProvider fully implemented with CsvHelper 33.1.0: RFC 4180 streaming, BOM tolerance, semicolon delimiter, column-count validation, and DataParseException wrapping — all 11 tests green**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-27T00:53:01Z
- **Completed:** 2026-03-27T00:55:49Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments

- Replaced CsvDataProvider stub with full CsvHelper 33.1.0 implementation passing all 11 CsvDataProviderTests
- Streaming via manual `while (await csv.ReadAsync())` loop with `yield return` — no ToList/ToArray; 50MB CSV within 100MB heap delta
- BOM-tolerant `StreamReader(detectEncodingFromByteOrderMarks: true)` prevents first-column corruption on UTF-8 BOM files
- Column-count mismatch throws `DataParseException` with `LineNumber` and `OffendingLine`; `BadDataFound` and `CsvHelperException` also translated to `DataParseException`
- Fixed pre-existing `JsonDataProvider.cs` compile error (CS1626: yield in try/catch) that was blocking the build

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement CsvDataProvider with CsvHelper streaming and exception wrapping** - `39d6ed5` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified

- `packages/Recrd.Data/CsvDataProvider.cs` - Full CsvHelper 33.1.0 implementation: StreamReader with BOM detection, CsvConfiguration with configurable delimiter and BadDataFound, BuildRow helper for try/catch isolation, column-count validation, CsvHelperException translation to DataParseException
- `packages/Recrd.Data/JsonDataProvider.cs` - Refactored to use MoveNextAsync pattern (yield outside try/catch) to fix CS1626 compile error

## Decisions Made

- **BuildRow helper method:** C# CS1626 prevents `yield return` inside a `try/catch` block. Extracted row-building logic to a synchronous `BuildRow` method so the async iterator body only contains the `yield return` statement outside any catch.
- **JsonDataProvider MoveNextAsync pattern:** Applied same pattern as BuildRow — call `MoveNextAsync()` in try/catch, then `yield return Current` outside. This satisfies CS1626 while preserving exception translation to `DataParseException`.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fixed JsonDataProvider CS1626 compile error blocking build**
- **Found during:** Task 1 (CsvDataProvider implementation)
- **Issue:** Pre-existing `JsonDataProvider.cs` stub had `yield return` inside a `try/catch` block, causing CS1626 compiler error that prevented the build from succeeding
- **Fix:** Restructured JsonDataProvider to use `GetAsyncEnumerator()` + `MoveNextAsync()` pattern, calling MoveNextAsync in try/catch and yielding Current outside
- **Files modified:** `packages/Recrd.Data/JsonDataProvider.cs`
- **Verification:** `dotnet build packages/Recrd.Data` succeeds with 0 errors; all 11 CSV tests pass
- **Committed in:** `39d6ed5` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Necessary fix to unblock the build. JsonDataProvider restructuring is correct C# and does not affect observable behavior.

## Issues Encountered

- CS1626 (yield in try/catch) affects both CsvDataProvider and JsonDataProvider — C# language restriction. Solved via helper method extraction for CsvDataProvider and MoveNextAsync pattern for JsonDataProvider.
- CS8602 nullable dereference in `BadDataFound` lambda on `args.Context.Parser` — fixed by switching to null-conditional operators (`?.`) per linter suggestion.

## Known Stubs

None — CsvDataProvider is fully implemented. JsonDataProvider remains a stub (Plan 03 will implement it).

## Next Phase Readiness

- CsvDataProvider fully functional and tested — ready for use in integration tests (Plan 04)
- JsonDataProvider compile error fixed; implementation still stubbed — Plan 03 will implement it
- DATA-01, DATA-02, DATA-03 requirements delivered

## Self-Check: PASSED

- FOUND: packages/Recrd.Data/CsvDataProvider.cs
- FOUND: packages/Recrd.Data/JsonDataProvider.cs
- FOUND: .planning/phases/03-data-providers/03-02-SUMMARY.md
- FOUND: commit 39d6ed5 (feat: CsvDataProvider implementation)
- FOUND: commit 1d23ece (docs: plan completion)

---
*Phase: 03-data-providers*
*Completed: 2026-03-27*
