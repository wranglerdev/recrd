---
phase: 11-plugin-system
plan: 01
subsystem: testing
tags: [dotnet, xunit, AssemblyLoadContext, plugin-system, tdd, red-phase]

requires:
  - phase: 10-vscode-extension
    provides: completed CLI structure and recrd-cli.csproj with all dependencies

provides:
  - TDD red phase: 13 failing tests covering PLUG-01 through PLUG-04
  - FakePlugin.csproj fixture with EnableDynamicLoading (produces .deps.json)
  - Production stubs: PluginManager, RecrdPluginLoadContext, PluginInfo
  - Test contract for plugin discovery, ALC isolation, version gating, exception safety

affects:
  - 11-02 (green phase: PluginManager implementation)
  - 11-03 (green phase: RecrdPluginLoadContext implementation)
  - 11-04 (CLI integration and plugins list command)

tech-stack:
  added: []
  patterns:
    - "FakePlugin.csproj with EnableDynamicLoading=true for .deps.json production"
    - "Compile Remove for TestFixtures to prevent glob pickup of fixture obj/ files"
    - "PluginManager hostCoreVersion constructor param for testable version gating"
    - "SafeCompileAsync stub on PluginManager for PLUG-04 exception isolation contract"

key-files:
  created:
    - tests/Recrd.Cli.Tests/Plugins/TestFixtures/FakePlugin.csproj
    - tests/Recrd.Cli.Tests/Plugins/TestFixtures/FakeCompiler.cs
    - tests/Recrd.Cli.Tests/Plugins/TestFixtures/FakeThrowingCompiler.cs
    - tests/Recrd.Cli.Tests/Plugins/PluginDiscoveryTests.cs
    - tests/Recrd.Cli.Tests/Plugins/PluginLoadContextTests.cs
    - tests/Recrd.Cli.Tests/Plugins/VersionGatingTests.cs
    - tests/Recrd.Cli.Tests/Plugins/ExceptionSafetyTests.cs
    - apps/recrd-cli/Plugins/PluginManager.cs
    - apps/recrd-cli/Plugins/RecrdPluginLoadContext.cs
    - apps/recrd-cli/Plugins/PluginInfo.cs
  modified:
    - tests/Recrd.Cli.Tests/Recrd.Cli.Tests.csproj

key-decisions:
  - "FakePlugin.csproj not added to recrd.sln — built on-demand via dotnet publish in test setup"
  - "Compile Remove for Plugins/TestFixtures/** prevents duplicate assembly attribute errors from MSBuild glob picking up fixture obj/ files"
  - "PluginManager accepts optional hostCoreVersion parameter for testable version gating without reflection mocking"
  - "SafeCompileAsync added to PluginManager stub to define the PLUG-04 exception isolation API surface"
  - "PluginLoadContextTests use Assert.ThrowsAny wrapping inner NotImplementedException — runtime wraps Load() exceptions in FileLoadException"
  - "Red phase uses direct API calls with expected-behavior assertions (not Assert.Throws) so tests fail naturally when stubs throw"

patterns-established:
  - "TestFixtures projects: standalone csproj with EnableDynamicLoading, excluded from parent test project compilation"
  - "PluginTestFixture.FindFakePluginCsproj: resolves fixture path via AppContext.BaseDirectory + 5 parent traversals to repo root"

requirements-completed:
  - PLUG-01
  - PLUG-02
  - PLUG-03
  - PLUG-04

duration: 13min
completed: 2026-04-09
---

# Phase 11 Plan 01: Plugin System — TDD Red Phase Summary

**13 failing tests across 4 files establish the full plugin system test contract: discovery (PLUG-01), ALC isolation (PLUG-02), version gating (PLUG-03), and exception safety (PLUG-04), with a FakePlugin fixture that produces a real DLL with .deps.json**

## Performance

- **Duration:** 13 min
- **Started:** 2026-04-09T22:00:44Z
- **Completed:** 2026-04-09T22:13:04Z
- **Tasks:** 2
- **Files modified:** 11

## Accomplishments

- Created `FakePlugin.csproj` with `EnableDynamicLoading=true` — publishes and produces `FakePlugin.deps.json` for real ALC testing
- Created `FakeCompiler` (happy path) and `FakeThrowingCompiler` (exception path) as test fixtures implementing `ITestCompiler`
- Created 3 production stubs (`PluginManager`, `RecrdPluginLoadContext`, `PluginInfo`) that compile cleanly against `Recrd.Core`
- Wrote 13 failing tests covering all 4 PLUG requirements — red phase confirmed with `dotnet test`

## Task Commits

1. **Task 1: Create test fixture plugin project and production stubs** — `cb81190` (chore)
2. **Task 2: Write all failing tests for PLUG-01 through PLUG-04** — `b9516d4` (test)

## Files Created/Modified

- `tests/Recrd.Cli.Tests/Plugins/TestFixtures/FakePlugin.csproj` — Standalone fixture plugin with `EnableDynamicLoading=true`
- `tests/Recrd.Cli.Tests/Plugins/TestFixtures/FakeCompiler.cs` — Happy-path `ITestCompiler` implementation
- `tests/Recrd.Cli.Tests/Plugins/TestFixtures/FakeThrowingCompiler.cs` — Exception-throwing `ITestCompiler` for PLUG-04 tests
- `tests/Recrd.Cli.Tests/Plugins/PluginDiscoveryTests.cs` — 4 tests for PLUG-01 (subdirectory scanning, matching, flat DLL ignore)
- `tests/Recrd.Cli.Tests/Plugins/PluginLoadContextTests.cs` — 3 tests for PLUG-02 (ALC isolation, type unification, ITestCompiler cast)
- `tests/Recrd.Cli.Tests/Plugins/VersionGatingTests.cs` — 3 tests for PLUG-03 (same major version, different major version, error reporting)
- `tests/Recrd.Cli.Tests/Plugins/ExceptionSafetyTests.cs` — 3 tests for PLUG-04 (warning added, host no crash, other compilers still run)
- `apps/recrd-cli/Plugins/PluginManager.cs` — Stub with `DiscoverPlugins`, `GetCompilers`, `GetDataProviders`, `SafeCompileAsync`
- `apps/recrd-cli/Plugins/RecrdPluginLoadContext.cs` — Stub inheriting `AssemblyLoadContext(isCollectible: true)`
- `apps/recrd-cli/Plugins/PluginInfo.cs` — Record type: `Name, Version?, Interfaces, Loaded, Error`
- `tests/Recrd.Cli.Tests/Recrd.Cli.Tests.csproj` — Added `<Compile Remove="Plugins\TestFixtures\**" />`

## Decisions Made

- `FakePlugin.csproj` is NOT added to `recrd.sln` — it is built on-demand via `dotnet publish` in test setup (`PluginTestFixture.PublishFakePlugin`), exactly as the plan specified
- Added `<Compile Remove="Plugins\TestFixtures\**" />` to `Recrd.Cli.Tests.csproj` to prevent MSBuild's default `**/*.cs` glob from picking up the fixture project's source and `obj/` generated files (which caused duplicate assembly attribute errors)
- `PluginManager` constructor accepts an optional `Version? hostCoreVersion` parameter so version gating tests can simulate an incompatible host without reflection tricks
- `SafeCompileAsync(ITestCompiler, Session, CompilerOptions)` added to `PluginManager` stub to define the PLUG-04 API surface before implementation
- Red phase tests assert final expected behavior (not `Assert.Throws<NotImplementedException>`), so they fail naturally when stubs throw

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added `hostCoreVersion` parameter to `PluginManager` constructor**
- **Found during:** Task 2 (writing VersionGatingTests)
- **Issue:** Tests need to simulate an incompatible host major version (v99) to test rejection logic, but plan's stub had no way to inject that
- **Fix:** Added `Version? hostCoreVersion = null` optional parameter; defaults to actual `Recrd.Core` assembly version
- **Files modified:** `apps/recrd-cli/Plugins/PluginManager.cs`
- **Committed in:** `b9516d4` (Task 2 commit)

**2. [Rule 2 - Missing Critical] Added `SafeCompileAsync` stub to `PluginManager`**
- **Found during:** Task 2 (writing ExceptionSafetyTests)
- **Issue:** PLUG-04 tests invoke `manager.SafeCompileAsync()` which wasn't in the original plan's stub definition — without it tests wouldn't compile
- **Fix:** Added `public Task<CompilationResult> SafeCompileAsync(ITestCompiler, Session, CompilerOptions)` throwing `NotImplementedException`
- **Files modified:** `apps/recrd-cli/Plugins/PluginManager.cs`
- **Committed in:** `b9516d4` (Task 2 commit)

**3. [Rule 3 - Blocking] Added `<Compile Remove>` to prevent fixture obj/ glob collision**
- **Found during:** Task 2 (build verification)
- **Issue:** MSBuild's default `**/*.cs` glob picked up `Plugins/TestFixtures/obj/Release/net10.0/FakePlugin.AssemblyInfo.cs` and `.NETCoreApp...AssemblyAttributes.cs`, causing duplicate assembly attribute compile errors
- **Fix:** Added `<Compile Remove="Plugins\TestFixtures\**" />` to `Recrd.Cli.Tests.csproj`
- **Files modified:** `tests/Recrd.Cli.Tests/Recrd.Cli.Tests.csproj`
- **Committed in:** `b9516d4` (Task 2 commit)

---

**Total deviations:** 3 auto-fixed (2 missing critical, 1 blocking)
**Impact on plan:** All auto-fixes necessary for compilation and test correctness. No scope creep.

## Issues Encountered

- **Worktree base mismatch:** Worktree was initialized from an older commit (`e491843`) rather than the target base (`c41aa61`). Resolved with `git reset --soft c41aa61` followed by `git checkout c41aa61 -- apps/ packages/ tests/ recrd.sln` to restore working tree files.
- **PluginLoadContext red-phase assertion:** `AssemblyLoadContext.LoadFromAssemblyName` wraps `Load()` exceptions in `FileLoadException`. Tests were adjusted to call the real API and assert the expected result directly (assembly not null, correct name), which fails naturally when `Load()` throws.

## Known Stubs

All stubs are intentional — this is the TDD red phase. No production logic exists yet:
- `PluginManager.DiscoverPlugins()` → throws `NotImplementedException`
- `PluginManager.GetCompilers()` → throws `NotImplementedException`
- `PluginManager.GetDataProviders()` → throws `NotImplementedException`
- `PluginManager.SafeCompileAsync()` → throws `NotImplementedException`
- `RecrdPluginLoadContext.Load()` → throws `NotImplementedException`

Implementation begins in Plan 11-02 (green phase).

## Next Phase Readiness

- Test contract fully established: 13 failing tests define the exact behavior required
- FakePlugin fixture is proven: publishes successfully with `.deps.json` in place
- Production stubs compile cleanly with zero warnings
- Ready for Plan 11-02: implement `RecrdPluginLoadContext` (ALC isolation + type unification)

---
*Phase: 11-plugin-system*
*Completed: 2026-04-09*
