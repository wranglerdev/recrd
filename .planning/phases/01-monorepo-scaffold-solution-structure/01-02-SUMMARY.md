---
phase: 01-monorepo-scaffold-solution-structure
plan: 02
subsystem: infra
tags: [dotnet, csproj, monorepo, playwright, solution-structure]

# Dependency graph
requires:
  - phase: 01-monorepo-scaffold-solution-structure/01
    provides: recrd.sln and Directory.Build.props
provides:
  - 5 package library stubs under packages/ (Core, Data, Gherkin, Recording, Compilers)
  - All packages registered in recrd.sln under packages solution folder
  - Dependency graph: Core (zero deps), Data/Gherkin/Recording -> Core, Compilers -> Core+Gherkin
affects: [01-monorepo-scaffold-solution-structure/03, 01-monorepo-scaffold-solution-structure/04, phase-02, phase-03, phase-04]

# Tech tracking
tech-stack:
  added: [Microsoft.Playwright 1.58.0 (Recording only)]
  patterns: [ProjectReference for inter-package deps, PackageId on Core for NuGet publishing]

key-files:
  created:
    - packages/Recrd.Core/Recrd.Core.csproj
    - packages/Recrd.Core/Placeholder.cs
    - packages/Recrd.Data/Recrd.Data.csproj
    - packages/Recrd.Data/Placeholder.cs
    - packages/Recrd.Gherkin/Recrd.Gherkin.csproj
    - packages/Recrd.Gherkin/Placeholder.cs
    - packages/Recrd.Recording/Recrd.Recording.csproj
    - packages/Recrd.Recording/Placeholder.cs
    - packages/Recrd.Compilers/Recrd.Compilers.csproj
    - packages/Recrd.Compilers/Placeholder.cs
  modified:
    - recrd.sln

key-decisions:
  - "Recrd.Core has PackageId for NuGet publishing, zero Recrd.* dependencies"
  - "Recrd.Recording is the sole Microsoft.Playwright consumer, isolating ~200MB browser binaries"
  - "Recrd.Compilers references both Core and Gherkin (needs Gherkin AST types for compilation)"

patterns-established:
  - "Package isolation: Core depends on nothing, all other packages depend on Core via ProjectReference"
  - "Playwright isolation: only Recording references Microsoft.Playwright"

requirements-completed: []

# Metrics
duration: 14min
completed: 2026-03-26
---

# Phase 01 Plan 02: Package Projects Summary

**5 .NET library stubs (Core, Data, Gherkin, Recording, Compilers) with correct ProjectReference dependency graph and Playwright isolation in Recording**

## Performance

- **Duration:** 14 min (includes prior partial execution)
- **Started:** 2026-03-26T03:37:59Z
- **Completed:** 2026-03-26T03:51:33Z
- **Tasks:** 3
- **Files modified:** 11

## Accomplishments
- Created 5 package library projects with correct .csproj configurations and Placeholder.cs stubs
- Established dependency graph: Core (zero deps), Data/Gherkin/Recording -> Core, Compilers -> Core+Gherkin
- Isolated Microsoft.Playwright 1.58.0 to Recrd.Recording only
- Registered all 5 packages in recrd.sln under a dedicated "packages" solution folder
- All projects build clean with zero warnings

## Task Commits

Each task was committed atomically:

1. **Task 1: Create Recrd.Core and Recrd.Data package stubs** - `1929497` (feat) - prior execution
2. **Task 2: Create Recrd.Gherkin, Recrd.Recording, and Recrd.Compilers stubs** - `7561160` (feat)
3. **Task 3: Register all 5 package projects in recrd.sln** - `dff7bf4` (fix)

**Plan metadata:** pending (docs: complete plan)

## Files Created/Modified
- `packages/Recrd.Core/Recrd.Core.csproj` - Core library stub, zero Recrd.* deps, PackageId for NuGet
- `packages/Recrd.Core/Placeholder.cs` - Namespace placeholder for Phase 2
- `packages/Recrd.Data/Recrd.Data.csproj` - Data library stub, references Core
- `packages/Recrd.Data/Placeholder.cs` - Namespace placeholder for Phase 3
- `packages/Recrd.Gherkin/Recrd.Gherkin.csproj` - Gherkin library stub, references Core
- `packages/Recrd.Gherkin/Placeholder.cs` - Namespace placeholder for Phase 4
- `packages/Recrd.Recording/Recrd.Recording.csproj` - Recording engine stub, references Core + Microsoft.Playwright 1.58.0
- `packages/Recrd.Recording/Placeholder.cs` - Namespace placeholder for Phase 6
- `packages/Recrd.Compilers/Recrd.Compilers.csproj` - Compilers stub, references Core + Gherkin
- `packages/Recrd.Compilers/Placeholder.cs` - Namespace placeholder for Phase 7
- `recrd.sln` - Updated with all 5 packages under packages solution folder

## Decisions Made
- Recrd.Core gets PackageId property for NuGet publishing (per research Pitfall 4)
- Recrd.Recording is the sole Playwright consumer to avoid pulling ~200MB browser binaries into other packages
- Recrd.Compilers references both Core and Gherkin (needs Gherkin AST types for robot compilation)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed solution folder nesting for package projects**
- **Found during:** Task 3 (Register packages in recrd.sln)
- **Issue:** Previous partial execution nested all 5 package projects under the "apps" solution folder instead of a dedicated "packages" solution folder
- **Fix:** Added a "packages" solution folder GUID and re-nested the 5 package project GUIDs under it; recrd-cli remains under "apps"
- **Files modified:** recrd.sln
- **Verification:** `dotnet sln recrd.sln list` shows all 5 packages; `dotnet build recrd.sln` succeeds
- **Committed in:** dff7bf4

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Minor organizational fix to solution folder nesting. No scope creep.

## Issues Encountered
None - all projects built clean on first attempt.

## Known Stubs
All Placeholder.cs files are intentional stubs -- they exist solely to satisfy the .NET compiler requirement for at least one source file per project. Each will be replaced with real implementations in their respective phases (Core in Phase 2, Data in Phase 3, Gherkin in Phase 4, Recording in Phase 6, Compilers in Phase 7).

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All 5 package projects exist and build clean, ready for test project references (Plan 04)
- recrd-cli app can now reference these packages (Plan 03)
- Dependency graph is correct and enforced via ProjectReference

## Self-Check: PASSED

- All 10 files (5 .csproj + 5 Placeholder.cs) verified present
- All 3 commits (1929497, 7561160, dff7bf4) verified in git log

---
*Phase: 01-monorepo-scaffold-solution-structure*
*Completed: 2026-03-26*
