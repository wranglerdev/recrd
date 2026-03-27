---
phase: 04-gherkin-generator
plan: 02
subsystem: testing
tags: [dotnet, csharp, gherkin, bdd, pt-BR, xunit]

# Dependency graph
requires:
  - phase: 04-gherkin-generator/04-01
    provides: GherkinGenerator stub, IGherkinGenerator interface, GherkinGeneratorOptions, red test suites (FixedScenarioTests, GroupingTests, DeterminismTests)
  - phase: 02-core-ast-types-interfaces
    provides: Session, ActionStep, AssertionStep, GroupStep, Selector, ActionType, AssertionType, GroupType, SelectorStrategy AST types

provides:
  - StepTextRenderer internal static class mapping all 6 ActionTypes and 5 AssertionTypes to pt-BR sentences
  - GroupingClassifier internal static class implementing default heuristic (Navigate→Given, interactions→When, assertions→Then)
  - GherkinGenerator full implementation for fixed (Cenario) scenarios with GroupStep and default heuristic paths
  - 14 tests passing: FixedScenarioTests (5), GroupingTests (7), DeterminismTests (2)

affects:
  - 04-03-PLAN (data-driven Esquema do Cenario + Exemplos table — extends this implementation)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "StepTextRenderer internal static class: switch expression dispatching IStep subtypes to pt-BR text"
    - "GroupingClassifier two-pass heuristic: first-Navigate index determines Given/When boundary"
    - "GherkinGenerator dual-path step emission: GroupStep path vs default heuristic path"
    - "BestSelectorValue priority: DataTestId > Id > Role > first-in-Strategies fallback"

key-files:
  created:
    - packages/Recrd.Gherkin/Internal/StepTextRenderer.cs
    - packages/Recrd.Gherkin/Internal/GroupingClassifier.cs
  modified:
    - packages/Recrd.Gherkin/GherkinGenerator.cs

key-decisions:
  - "GroupStep detection uses Any(s => s is GroupStep) on top-level steps to choose emission path"
  - "Trailing newline added after all steps for valid Gherkin file structure"
  - "Non-GroupStep at top level in GroupStep path uses E continuation (edge case)"

patterns-established:
  - "Internal/ subdirectory for implementation helpers not exposed in public API"
  - "Two-pass classifier: O(n) scan for first-Navigate index, then O(n) classification"

requirements-completed: [GHER-01, GHER-05, GHER-06, GHER-07, GHER-08]

# Metrics
duration: 6min
completed: 2026-03-27
---

# Phase 04 Plan 02: Gherkin Generator Fixed Scenarios Summary

**GherkinGenerator emits valid pt-BR .feature for fixed scenarios using StepTextRenderer (6 ActionTypes, 5 AssertionTypes) and GroupingClassifier heuristic; 14 tests green**

## Performance

- **Duration:** 6 min
- **Started:** 2026-03-27T19:42:32Z
- **Completed:** 2026-03-27T19:48:00Z
- **Tasks:** 2 completed
- **Files modified:** 3

## Accomplishments

- Created `StepTextRenderer` mapping all ActionTypes and AssertionTypes to pt-BR sentences with BestSelectorValue priority (DataTestId > Id > Role)
- Created `GroupingClassifier` implementing the default heuristic: first-Navigate determines Given boundary, subsequent actions are When, assertions are Then
- Replaced stub `GherkinGenerator` with full implementation: language header, Funcionalidade line, Cenario/Esquema keyword branching, GroupStep and heuristic emission paths
- All 14 targeted tests pass (FixedScenarioTests, GroupingTests, DeterminismTests); `dotnet format --verify-no-changes` exits 0

## Task Commits

Each task was committed atomically:

1. **Task 1: StepTextRenderer and GroupingClassifier** - `95aaef1` (feat)
2. **Task 2: GherkinGenerator full implementation** - `826b229` (feat)

## Files Created/Modified

- `packages/Recrd.Gherkin/Internal/StepTextRenderer.cs` - Maps IStep subtypes to pt-BR sentences; BestSelectorValue uses DataTestId > Id > Role priority
- `packages/Recrd.Gherkin/Internal/GroupingClassifier.cs` - Default heuristic: scans for first Navigate, assigns Given/When/Then per position and type
- `packages/Recrd.Gherkin/GherkinGenerator.cs` - Full GenerateAsync: header, feature line, scenario keyword, GroupStep path, heuristic path, deterministic output

## Decisions Made

- GroupStep detection on top-level steps uses `Any(s => s is GroupStep)` — if any top-level step is a GroupStep, the GroupStep emission path is used
- Trailing `await output.WriteLineAsync()` after all steps ensures the file ends with a newline
- Non-GroupStep at top level in the GroupStep path gets `E` continuation (edge case per plan spec)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- Worktree branch `worktree-agent-a78c375b` did not have Plan 01 content — resolved by merging `tdd/phase-04` into the worktree before starting (fast-forward merge, no conflicts)
- NuGet assets file missing in fresh worktree — resolved with `dotnet restore` before first build (same pattern as Plan 01 notes)

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 04-02 complete: GherkinGenerator produces valid pt-BR `.feature` for fixed (zero-variable) sessions
- Plan 04-03 can now extend GherkinGenerator with `Esquema do Cenário` + `Exemplos` table (data-driven path)
- The `dataProvider` parameter and `session.Variables.Count > 0` branch are ready stubs for Plan 03 to fill in

---
*Phase: 04-gherkin-generator*
*Completed: 2026-03-27*

## Self-Check: PASSED

All required files verified present:
- packages/Recrd.Gherkin/Internal/StepTextRenderer.cs: FOUND
- packages/Recrd.Gherkin/Internal/GroupingClassifier.cs: FOUND
- packages/Recrd.Gherkin/GherkinGenerator.cs: FOUND (modified)

Commits verified: 95aaef1 (FOUND), 826b229 (FOUND)
