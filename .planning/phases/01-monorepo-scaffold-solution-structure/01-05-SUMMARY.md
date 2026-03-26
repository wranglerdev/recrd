---
phase: 01-monorepo-scaffold-solution-structure
plan: 05
subsystem: testing
tags: [xunit, dotnet, csproj, solution, moq, coverlet]

# Dependency graph
requires:
  - phase: 01-02
    provides: "5 package projects (Recrd.Core, Recrd.Data, Recrd.Gherkin, Recrd.Recording, Recrd.Compilers)"
  - phase: 01-03
    provides: "recrd-cli app project"
provides:
  - "6 xUnit test project stubs under tests/ — one per package plus integration"
  - "PlaceholderTests.cs in each project ensuring dotnet test exits 0"
  - "All 6 test projects registered in recrd.sln under a tests solution folder"
affects: [phase-02, phase-03, phase-04, phase-06, phase-07, ci-workflow]

# Tech tracking
tech-stack:
  added: [xunit 2.9.3, Microsoft.NET.Test.Sdk 18.3.0, xunit.runner.visualstudio 3.1.5, Moq 4.20.72, coverlet.collector 8.0.1]
  patterns: [test-project-per-package, placeholder-test-class, IsPackable-false]

key-files:
  created:
    - tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj
    - tests/Recrd.Core.Tests/PlaceholderTests.cs
    - tests/Recrd.Data.Tests/Recrd.Data.Tests.csproj
    - tests/Recrd.Data.Tests/PlaceholderTests.cs
    - tests/Recrd.Gherkin.Tests/Recrd.Gherkin.Tests.csproj
    - tests/Recrd.Gherkin.Tests/PlaceholderTests.cs
    - tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj
    - tests/Recrd.Recording.Tests/PlaceholderTests.cs
    - tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj
    - tests/Recrd.Compilers.Tests/PlaceholderTests.cs
    - tests/Recrd.Integration.Tests/Recrd.Integration.Tests.csproj
    - tests/Recrd.Integration.Tests/PlaceholderTests.cs
  modified:
    - recrd.sln

key-decisions:
  - "xunit 2.9.3 + xunit.runner.visualstudio 3.1.5 pinned — compatible with .NET 10 and Microsoft.NET.Test.Sdk 18.3.0"
  - "Recrd.Integration.Tests references all 5 packages to support full pipeline tests in Phase 6+"
  - "PlaceholderTests.cs pattern chosen over [Fact] test method to avoid xunit no-tests-found exit code 1"
  - "IsPackable=false on all test projects prevents dotnet pack from emitting test NuGet packages"

patterns-established:
  - "Test project naming: {Package}.Tests mirrors the package namespace"
  - "PlaceholderTests.cs: empty public class with phase comment acts as xunit test-class anchor"
  - "IsPackable=false in PropertyGroup is the standard guard on all test .csproj files"

requirements-completed: []

# Metrics
duration: 10min
completed: 2026-03-26
---

# Phase 01 Plan 05: Test Projects Summary

**6 xUnit test project stubs wired to their package ProjectReferences, registered in recrd.sln, building clean with dotnet test exiting 0**

## Performance

- **Duration:** ~10 min (files committed in prior session, verification run in current session)
- **Started:** 2026-03-26T18:36:36Z
- **Completed:** 2026-03-26T18:37:30Z
- **Tasks:** 2
- **Files modified:** 13 (12 created + recrd.sln)

## Accomplishments

- 6 xUnit test projects created under tests/ — Recrd.Core.Tests, Recrd.Data.Tests, Recrd.Gherkin.Tests, Recrd.Recording.Tests, Recrd.Compilers.Tests, Recrd.Integration.Tests
- Each project carries pinned versions of xunit 2.9.3, Moq 4.20.72, coverlet.collector 8.0.1 and IsPackable=false
- All 6 projects registered in recrd.sln under the `tests` solution folder; `dotnet sln list` returns 12 entries
- `dotnet build recrd.sln --no-restore` exits 0 with 0 warnings; `dotnet test recrd.sln --no-build` exits 0

## Task Commits

Each task was committed atomically:

1. **Task 1 + Task 2: Create 6 xUnit test project stubs** - `33c263d` (feat)

**Plan metadata:** (this commit — docs: complete plan)

## Files Created/Modified

- `tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj` - xUnit test project referencing packages/Recrd.Core
- `tests/Recrd.Core.Tests/PlaceholderTests.cs` - empty class; Phase 2 populates
- `tests/Recrd.Data.Tests/Recrd.Data.Tests.csproj` - xUnit test project referencing packages/Recrd.Data
- `tests/Recrd.Data.Tests/PlaceholderTests.cs` - empty class; Phase 3 populates
- `tests/Recrd.Gherkin.Tests/Recrd.Gherkin.Tests.csproj` - xUnit test project referencing packages/Recrd.Gherkin
- `tests/Recrd.Gherkin.Tests/PlaceholderTests.cs` - empty class; Phase 4 populates
- `tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj` - xUnit test project referencing packages/Recrd.Recording
- `tests/Recrd.Recording.Tests/PlaceholderTests.cs` - empty class; Phase 6 populates
- `tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj` - xUnit test project referencing packages/Recrd.Compilers
- `tests/Recrd.Compilers.Tests/PlaceholderTests.cs` - empty class; Phase 7 populates
- `tests/Recrd.Integration.Tests/Recrd.Integration.Tests.csproj` - integration test project with 5 ProjectReferences
- `tests/Recrd.Integration.Tests/PlaceholderTests.cs` - empty class; Phase 6+ populates
- `recrd.sln` - added tests solution folder with all 6 test projects

## Decisions Made

- Pinned xunit.runner.visualstudio 3.1.5 (not 2.x) — required for .NET 10 compatibility with VSTest 18.x
- Recrd.Integration.Tests references all 5 packages to allow full record → compile pipeline tests without mocking at the seam level

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Known Stubs

The following intentional stubs exist by design — they are the deliverable of this plan:

| File | Stub | Reason |
|------|------|--------|
| tests/*/PlaceholderTests.cs | Empty `public class PlaceholderTests { }` | Anchor class so xunit finds ≥1 class; each future phase will replace with real tests |

These are not data stubs that block the plan's goal — the plan's goal is to have test infrastructure ready for Phase 2's TDD mandate, which is achieved.

## Next Phase Readiness

- All 6 test projects exist and build — Phase 2 TDD mandate can begin immediately
- Test projects pre-wired to their package targets; Phase 2 only needs to write tests, not configure projects
- CI workflow (plan 01-06) can reference `dotnet test recrd.sln` as the test command without further setup

## Self-Check: PASSED

- FOUND: tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj
- FOUND: tests/Recrd.Core.Tests/PlaceholderTests.cs
- FOUND: tests/Recrd.Integration.Tests/Recrd.Integration.Tests.csproj
- FOUND: tests/Recrd.Integration.Tests/PlaceholderTests.cs
- FOUND: .planning/phases/01-monorepo-scaffold-solution-structure/01-05-SUMMARY.md
- FOUND commit: 33c263d (feat: create 6 xUnit test project stubs)
- FOUND commit: 2fd0d8f (docs: commit recrd.sln test-project registrations and update STATE.md)
- `dotnet build recrd.sln --no-restore` exits 0 (verified)
- `dotnet test recrd.sln --no-build --filter "Category!=Integration"` exits 0 (verified)
- `dotnet sln recrd.sln list` returns 12 entries (14 lines including header/blank)

---
*Phase: 01-monorepo-scaffold-solution-structure*
*Completed: 2026-03-26*
