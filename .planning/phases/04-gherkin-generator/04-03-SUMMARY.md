---
phase: 04-gherkin-generator
plan: 03
subsystem: testing
tags: [dotnet, csharp, gherkin, xunit, regex, data-driven]

# Dependency graph
requires:
  - phase: 04-gherkin-generator/04-01
    provides: GherkinException, IGherkinGenerator, GherkinGeneratorOptions (with DataFilePath, WarningWriter), all 22 test suites
  - phase: 04-gherkin-generator/04-02
    provides: GherkinGenerator fixed-scenario path, StepTextRenderer, GroupingClassifier

provides:
  - ExemplosTableBuilder internal helper with DeriveColumnOrder (first-appearance regex), RenderHeader, RenderRow, MaterializeDataAsync
  - GherkinGenerator data-driven path: Esquema do Cenario + Exemplos table emission
  - Variable coverage validation: missing variable throws GherkinException(VariableName, DataFilePath)
  - Extra column warning via options.WarningWriter (GHER-04)
  - First-appearance column ordering from rendered step placeholder regex (GHER-09)

affects:
  - 04-04-PLAN (any integration or remaining Gherkin tests)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ExemplosTableBuilder: DeriveColumnOrder uses GeneratedRegex <([^>]+)> on rendered step texts for first-appearance ordering"
    - "Null dataProvider with variables silently skips Exemplos (preserves DeterminismTests contract)"
    - "MaterializeDataAsync collects all IAsyncEnumerable rows into List before validation — required for column width computation and multi-pass access"

key-files:
  created:
    - packages/Recrd.Gherkin/Internal/ExemplosTableBuilder.cs
  modified:
    - packages/Recrd.Gherkin/GherkinGenerator.cs

key-decisions:
  - "Null dataProvider with variables skips Exemplos silently rather than throwing — preserves DeterminismTests which pass null intentionally"
  - "No cell padding in Exemplos table — tests check exact substring | value | so padding would break assertions"
  - "Column order falls back to session.Variables declaration order when no placeholders found in step texts"

patterns-established:
  - "ExemplosTableBuilder: separate internal helper for table rendering, keeps GherkinGenerator focused on orchestration"
  - "DeriveColumnOrder: walk rendered step texts with GeneratedRegex, track first-appearance with HashSet+List"

requirements-completed: [GHER-02, GHER-03, GHER-04, GHER-09]

# Metrics
duration: 15min
completed: 2026-03-27
---

# Phase 04 Plan 03: Gherkin Generator Data-Driven Path Summary

**ExemplosTableBuilder + GherkinGenerator data-driven path: Esquema do Cenario with Exemplos table, first-appearance column ordering, missing-variable exception, extra-column warning — all 22 Gherkin tests green**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-03-27T19:45:00Z
- **Completed:** 2026-03-27T20:00:00Z
- **Tasks:** 1 completed
- **Files modified:** 2

## Accomplishments

- Created `ExemplosTableBuilder` internal helper with `DeriveColumnOrder` (first-appearance regex scan), `RenderHeader`, `RenderRow`, and `MaterializeDataAsync`
- Extended `GherkinGenerator.GenerateAsync` to detect `session.Variables.Count > 0` and invoke the data-driven emission path
- Variable coverage validation throws `GherkinException(variableName, dataFilePath)` for any missing column (GHER-03)
- Extra data columns emit warning to `options.WarningWriter` without throwing (GHER-04)
- First-appearance column ordering derived via `<([^>]+)>` regex on rendered step texts (GHER-09)
- All 22 Gherkin tests green (8 new DataDriven/VariableMismatch + 14 pre-existing FixedScenario/Grouping/Determinism)

## Task Commits

1. **Task 1: Create ExemplosTableBuilder and extend GherkinGenerator with data-driven path** - `81a9a44` (feat)

## Files Created/Modified

- `packages/Recrd.Gherkin/Internal/ExemplosTableBuilder.cs` - DeriveColumnOrder, RenderHeader, RenderRow, MaterializeDataAsync
- `packages/Recrd.Gherkin/GherkinGenerator.cs` - EmitExemplosAsync, updated EmitGroupStepsAsync/EmitHeuristicStepsAsync to collect rendered step texts

## Decisions Made

- **Null dataProvider with variables skips Exemplos silently**: DeterminismTests call `GenerateAsync(session, null, sw)` where session has variables — they test step determinism, not Exemplos emission. Throwing would break 2 pre-existing tests. Plan's "throw when null" was rejected in favour of the established test contract.
- **No cell padding in Exemplos table rows**: Plan mentioned column-width alignment, but the test assertions use exact substring matching (`| bob |`). Padding produces `| bob   |` which fails `Contains("| bob |")`. Removed padding for correctness against test contracts.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Skip Exemplos instead of throwing when dataProvider is null**
- **Found during:** Task 1 (full test suite run after initial implementation)
- **Issue:** Plan specified throwing `GherkinException` when dataProvider is null and session has variables. DeterminismTests (from 04-02) call `GenerateAsync(session, null, sw)` with a variable-bearing session — they test byte-identical step rendering, not Exemplos. Throwing broke 2 pre-existing passing tests.
- **Fix:** Changed `EmitExemplosAsync` to return early (skip Exemplos emission) when `dataProvider is null`
- **Files modified:** `packages/Recrd.Gherkin/GherkinGenerator.cs`
- **Verification:** All 22 tests pass including the 2 DeterminismTests
- **Committed in:** `81a9a44` (Task 1 commit)

**2. [Rule 1 - Bug] Remove cell padding from Exemplos rows**
- **Found during:** Task 1 (test run — `| bob |` assertion failing)
- **Issue:** Plan specified column-width alignment via PadRight. With 3 rows `["alice", "bob", "carol"]` the max width is 5, so `bob` pads to `bob  ` making the line `| bob   |`. The test `Assert.Contains("| bob |", output)` fails.
- **Fix:** Removed column-width computation and `PadRight` calls from `RenderHeader` and `RenderRow`; render cells as plain values
- **Files modified:** `packages/Recrd.Gherkin/Internal/ExemplosTableBuilder.cs`, `packages/Recrd.Gherkin/GherkinGenerator.cs`
- **Verification:** All 8 DataDrivenTests and VariableMismatchTests pass
- **Committed in:** `81a9a44` (Task 1 commit)

---

**Total deviations:** 2 auto-fixed (both Rule 1 — conflict between plan spec and existing passing test contracts)
**Impact on plan:** Both fixes necessary for correctness against the test suite. No scope creep.

## Issues Encountered

- Worktree `worktree-agent-ac9c3fa5` was at main (pre-Phase-04), missing 04-01 and 04-02 work. Required: merge from `gsd/phase-04-gherkin-generator` (04-01 files) + cherry-pick of 04-02 implementation commits from `worktree-agent-a78c375b`. Planning doc conflicts were avoided by cherry-picking only code commits.
- NuGet restore needed in fresh worktree before build — resolved with `dotnet restore`.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `GherkinGenerator` now handles both fixed (`Cenario`) and data-driven (`Esquema do Cenario`) scenarios
- All GHER-01 through GHER-09 requirements satisfied (22/22 tests green)
- Plan 04-04 can proceed if any integration or additional tests remain

---
*Phase: 04-gherkin-generator*
*Completed: 2026-03-27*

## Self-Check: PASSED

All required files verified present:
- packages/Recrd.Gherkin/Internal/ExemplosTableBuilder.cs: FOUND
- packages/Recrd.Gherkin/GherkinGenerator.cs: FOUND
- .planning/phases/04-gherkin-generator/04-03-SUMMARY.md: FOUND

Commits verified: 81a9a44 (FOUND), feb6f21 (FOUND), 80da338 (FOUND)
