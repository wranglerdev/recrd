---
phase: 05-ci-pipeline
plan: 01
subsystem: infra
tags: [github-actions, coverlet, dotnet, ci-cd, coverage-gates, tdd]

# Dependency graph
requires:
  - phase: 04-gherkin-generator
    provides: all test projects (Recrd.Core.Tests, Recrd.Data.Tests, Recrd.Gherkin.Tests) that coverage gates now enforce
provides:
  - Per-project 90% line coverage gates for Core, Data, Gherkin, Compilers
  - TDD red-phase branch handling via continue-on-error conditional
  - DOTNET_SYSTEM_NET_DISABLEIPV6 workflow-level env var
  - Push trigger for tdd/phase-* branches
affects: [06-mutation-testing, 07-nuget-publish, all future phases that add test projects]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Two conditional test steps: one for non-TDD branches (hard fail), one for tdd/phase-* (continue-on-error: true)"
    - "Per-project dotnet test invocations with coverlet --threshold flags for isolated coverage enforcement"
    - "Workflow-level DOTNET_SYSTEM_NET_DISABLEIPV6=1 env var so all run steps inherit IPv4 forcing"

key-files:
  created: []
  modified:
    - .github/workflows/ci.yml

key-decisions:
  - "Coverlet inline thresholds via --threshold 90 --threshold-type line --threshold-stat minimum passed to each dotnet test invocation (D-01)"
  - "Separate dotnet test invocations per gated project for fail-fast feedback and clear project name in error (D-02)"
  - "Two conditional test steps via startsWith(github.ref_name, 'tdd/phase-') — TDD step uses continue-on-error: true (D-10)"
  - "Coverage gates run on ALL branches including tdd/phase-* — only the all-tests step gets continue-on-error (D-09)"

patterns-established:
  - "CI coverage gate pattern: dotnet test {project}.csproj --no-build --collect:'XPlat Code Coverage' --threshold 90 --threshold-type line --threshold-stat minimum"
  - "TDD red-phase tolerance: continue-on-error: true on test step, if: startsWith(github.ref_name, 'tdd/phase-')"

requirements-completed: [CI-01, CI-02, CI-03, CI-06]

# Metrics
duration: 1min
completed: 2026-03-29
---

# Phase 05 Plan 01: CI Pipeline Coverage Gates and TDD Branch Handling Summary

**GitHub Actions CI extended with four per-project 90% line coverage gates (Coverlet inline thresholds) and conditional TDD red-phase branch handling via continue-on-error**

## Performance

- **Duration:** ~1 min
- **Started:** 2026-03-29T21:57:06Z
- **Completed:** 2026-03-29T21:57:56Z
- **Tasks:** 1 completed
- **Files modified:** 1

## Accomplishments

- Added workflow-level `DOTNET_SYSTEM_NET_DISABLEIPV6: '1'` env so all `dotnet` steps inherit IPv4 forcing (CLAUDE.md mandate)
- Extended push trigger to include `tdd/phase-*` branches so red-phase work gets CI feedback
- Replaced single combined test step with two conditional steps — non-TDD branches hard-fail, TDD branches use `continue-on-error: true`
- Added four separate coverage gate steps enforcing 90% line coverage per project: Core, Data, Gherkin, Compilers

## Task Commits

Each task was committed atomically:

1. **Task 1: Add per-project coverage gates and TDD red-phase support to ci.yml** - `ba596cc` (feat)

**Plan metadata:** _(docs commit follows)_

## Files Created/Modified

- `.github/workflows/ci.yml` - Extended with IPv4 env, tdd/phase-* trigger, conditional test steps, and four per-project 90% line coverage gates

## Decisions Made

- DOTNET_SYSTEM_NET_DISABLEIPV6 placed at workflow `env:` level (not per-step) so it propagates automatically to all current and future run steps
- Coverage gate steps use YAML block scalar `>` (folded) for readability — each flag on its own line
- Old combined test step removed and replaced; format check step preserved unchanged

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- CI-01, CI-02, CI-03, CI-06 requirements satisfied
- Phase 05 Plan 02 (mutation testing workflow) and Plan 03 (NuGet publish workflow) can proceed independently
- All gated test projects (Core, Data, Gherkin, Compilers) already have tests; coverage gates will enforce quality on every push

---
*Phase: 05-ci-pipeline*
*Completed: 2026-03-29*
