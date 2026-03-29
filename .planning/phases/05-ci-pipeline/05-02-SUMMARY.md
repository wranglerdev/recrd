---
phase: 05-ci-pipeline
plan: 02
subsystem: infra
tags: [stryker, mutation-testing, github-actions, dotnet-tools]

requires:
  - phase: 05-ci-pipeline/05-01
    provides: Base CI workflow with build, test, coverage, format steps

provides:
  - Weekly Stryker.NET mutation testing workflow on Recrd.Core
  - dotnet-stryker 4.6.0 registered in local tool manifest

affects:
  - 05-ci-pipeline/05-03 (NuGet publish workflow — same pattern for separate workflow file)

tech-stack:
  added: [Stryker.NET 4.6.0]
  patterns:
    - Separate workflow file for scheduled/non-push triggers
    - continue-on-error for informational-only CI steps
    - GITHUB_STEP_SUMMARY for report posting without extra permissions

key-files:
  created:
    - .github/workflows/mutation.yml
  modified:
    - .config/dotnet-tools.json

key-decisions:
  - "Stryker report destination is GITHUB_STEP_SUMMARY only — no PR comments, no artifact upload (per D-04)"
  - "Workflow always succeeds regardless of mutation score — informational only (per D-05)"
  - "workflow_dispatch included for manual ad-hoc runs"
  - "rollForward: false in dotnet-tools.json for reproducible Stryker version pinning"

patterns-established:
  - "Separate workflow file pattern: mutation.yml and publish.yml are separate from ci.yml, triggered by non-push events"
  - "Stryker tool installed via dotnet tool restore from local manifest, not global install"

requirements-completed:
  - CI-04
---

# Phase 05 Plan 02: Mutation Testing Workflow Summary

**Weekly Stryker.NET 4.6.0 mutation workflow on Recrd.Core posting markdown score to GitHub Actions Summary, never failing the build**

## Performance

- **Duration:** 1 min
- **Started:** 2026-03-29T21:57:15Z
- **Completed:** 2026-03-29T21:58:07Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Registered dotnet-stryker 4.6.0 as a local tool in `.config/dotnet-tools.json` with `rollForward: false` for reproducibility
- Created `.github/workflows/mutation.yml` with weekly Monday 06:00 UTC schedule and `workflow_dispatch` for manual runs
- Stryker targets `Recrd.Core.csproj` with `Recrd.Core.Tests.csproj`, emits markdown report appended to `$GITHUB_STEP_SUMMARY`
- `continue-on-error: true` on the Stryker step ensures the workflow never fails regardless of mutation score

## Task Commits

Each task was committed atomically:

1. **Task 1: Add dotnet-stryker to tool manifest** - `da70057` (chore)
2. **Task 2: Create weekly Stryker.NET mutation workflow** - `f96a6f3` (feat)

## Files Created/Modified

- `.config/dotnet-tools.json` - Added dotnet-stryker 4.6.0 local tool entry
- `.github/workflows/mutation.yml` - Weekly mutation testing workflow for Recrd.Core

## Decisions Made

- Per D-04: Report destination is `$GITHUB_STEP_SUMMARY` only — no artifact upload, no PR comments, no extra tokens or write permissions needed
- Per D-05: `continue-on-error: true` on Stryker step — mutation score is informational, never a build blocker
- `--reporter markdown` produces the `.md` file appended to the summary; `--reporter html` included for local inspection without upload step

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required. The workflow uses `GITHUB_TOKEN` (auto-provided) for writing to the Actions Summary.

## Next Phase Readiness

- Mutation workflow ready to trigger on next Monday or via `workflow_dispatch`
- Plan 05-03 (NuGet publish workflow) can proceed — same separate-workflow-file pattern established here applies

---
*Phase: 05-ci-pipeline*
*Completed: 2026-03-29*

## Self-Check: PASSED

- FOUND: `.github/workflows/mutation.yml`
- FOUND: `.config/dotnet-tools.json`
- FOUND: `.planning/phases/05-ci-pipeline/05-02-SUMMARY.md`
- FOUND commit: `da70057` chore(05-02): add dotnet-stryker 4.6.0 to tool manifest
- FOUND commit: `f96a6f3` feat(05-02): create weekly Stryker.NET mutation testing workflow
