---
phase: 08-cli-polish
plan: "01"
subsystem: recrd-cli
tags: [cli, system-commandline, tdd-red, logging, ipc-stubs]
dependency_graph:
  requires: []
  provides:
    - Recrd.Cli.Tests project (xUnit, Moq, coverlet) — TDD red baseline
    - System.CommandLine 2.0.5 command tree (11 subcommands)
    - LoggingSetup.Create — verbosity to LogLevel mapping + JSON console
    - CliOutput — ANSI color helpers + stop summary formatter
    - SessionSocket / SessionClient stubs (Plan 02 wires)
    - Command handler stubs (Plans 02-04 implement)
  affects:
    - recrd.sln (new test project registered)
    - .github/workflows/ci.yml (coverage gate added)
tech_stack:
  added:
    - System.CommandLine 2.0.5 (in recrd-cli.csproj)
    - Microsoft.Extensions.Logging 10.0.5 (in recrd-cli.csproj)
    - Microsoft.Extensions.Logging.Console 10.0.5 (in recrd-cli.csproj)
  patterns:
    - System.CommandLine 2.0.5 stable API — SetAction, Options.Add, Subcommands.Add (not SetHandler/AddOption/AddCommand)
    - LoggerFactory.Create with SetMinimumLevel from verbosity switch expression
    - Console.ForegroundColor for ANSI color (D-05 — no Spectre.Console)
    - Static Create() factory methods per command class (one class per command)
    - Assert.Fail() for TDD red stubs (xUnit2020 rule — not Assert.True(false))
key_files:
  created:
    - apps/recrd-cli/Program.cs
    - apps/recrd-cli/Logging/LoggingSetup.cs
    - apps/recrd-cli/Output/CliOutput.cs
    - apps/recrd-cli/Commands/StartCommand.cs
    - apps/recrd-cli/Commands/SessionControlCommand.cs
    - apps/recrd-cli/Commands/CompileCommand.cs
    - apps/recrd-cli/Commands/ValidateCommand.cs
    - apps/recrd-cli/Commands/SanitizeCommand.cs
    - apps/recrd-cli/Commands/RecoverCommand.cs
    - apps/recrd-cli/Commands/VersionCommand.cs
    - apps/recrd-cli/Commands/PluginsCommand.cs
    - apps/recrd-cli/Ipc/SessionSocket.cs
    - apps/recrd-cli/Ipc/SessionClient.cs
    - tests/Recrd.Cli.Tests/Recrd.Cli.Tests.csproj
    - tests/Recrd.Cli.Tests/PlaceholderTests.cs
    - tests/Recrd.Cli.Tests/Commands/StartCommandTests.cs
    - tests/Recrd.Cli.Tests/Commands/SessionControlCommandTests.cs
    - tests/Recrd.Cli.Tests/Commands/CompileCommandTests.cs
    - tests/Recrd.Cli.Tests/Commands/ValidateCommandTests.cs
    - tests/Recrd.Cli.Tests/Commands/SanitizeCommandTests.cs
    - tests/Recrd.Cli.Tests/Commands/RecoverCommandTests.cs
    - tests/Recrd.Cli.Tests/Commands/VersionCommandTests.cs
    - tests/Recrd.Cli.Tests/Commands/PluginsCommandTests.cs
    - tests/Recrd.Cli.Tests/Ipc/SessionSocketTests.cs
    - tests/Recrd.Cli.Tests/Ipc/SessionClientTests.cs
    - tests/Recrd.Cli.Tests/Logging/LoggingSetupTests.cs
  modified:
    - apps/recrd-cli/recrd-cli.csproj (System.CommandLine + MEL packages + InternalsVisibleTo)
    - recrd.sln (Recrd.Cli.Tests added under tests solution folder)
    - .github/workflows/ci.yml (Coverage gate - recrd-cli 90% line step added)
decisions:
  - "Command handler stubs return Task.FromResult(0) — implementation deferred to Plans 02-04 per plan scope"
  - "Assert.Fail() used for TDD red stubs — xUnit analyzer xUnit2020 rejects Assert.True(false, message)"
  - "System.CommandLine and Microsoft.Extensions.Logging added to test project csproj — transitive assembly refs not sufficient for test compilation"
  - "Static Create() factory methods with optional verbosity/logOutput params — allows tests to call Create() without wiring global options"
metrics:
  duration: 388s
  completed: 2026-04-07
  tasks_completed: 2
  files_created: 26
  files_modified: 3
---

# Phase 08 Plan 01: CLI Skeleton + TDD Red Phase Summary

**One-liner:** System.CommandLine 2.0.5 command tree with 11 subcommands, verbosity-mapped LoggingSetup, ANSI CliOutput, and 42 failing TDD red test stubs across 12 test files.

## What Was Built

**Task 1 — TDD Red Phase (commit 29c003b)**

Created `tests/Recrd.Cli.Tests/` project with xUnit 2.9.3, Moq 4.20.72, coverlet.msbuild 6.0.4. Added 42 `[Fact]` test methods across 12 files — all red via `Assert.Fail("Not implemented — red phase")`. Tests reference production types (`StartCommand.Create()`, `LoggingSetup.Create()`, `SessionSocket`, etc.) that are created in Task 2.

Also added:
- `InternalsVisibleTo Include="Recrd.Cli.Tests"` to `recrd-cli.csproj`
- `Recrd.Cli.Tests` registered in `recrd.sln` under `tests` solution folder
- Coverage gate step for `recrd-cli` (90% line) in `.github/workflows/ci.yml`

**Task 2 — Command Tree + Infrastructure (commit 88bde32)**

- **`apps/recrd-cli/Program.cs`**: Full System.CommandLine 2.0.5 entry point with `RootCommand` and 11 subcommands registered via `Subcommands.Add`. Uses `SetAction`, `Options.Add` — the stable 2.0.5 API. Entry point: `return await rootCommand.Parse(args).InvokeAsync()`.

- **`Logging/LoggingSetup.cs`**: `LoggingSetup.Create(verbosity, jsonOutput)` maps `quiet→Error`, `normal→Information`, `detailed→Debug`, `diagnostic→Trace`. Uses `LoggerFactory.Create` with `AddConsole` or `AddJsonConsole`.

- **`Output/CliOutput.cs`**: `WriteError` (red/stderr), `WriteSuccess` (green/stdout), `WriteInfo`, `WriteSummary` (D-09 stop summary block: events, variables, duration, file size, partial file).

- **`Ipc/SessionSocket.cs` + `Ipc/SessionClient.cs`**: Stub classes referenced by test files. Full IPC implementation in Plan 02.

- **All 8 Command classes**: Each has a `Create()` static factory that builds the `System.CommandLine.Command` with correct options/arguments and a stub `SetAction` returning `Task.FromResult(0)`.

## Deviations from Plan

**1. [Rule 1 - Bug] Switched Assert.True(false) to Assert.Fail**
- **Found during:** Task 1 / Task 2 build verification
- **Issue:** xUnit analyzer xUnit2020 treats `Assert.True(false, message)` as a build error — requires `Assert.Fail(message)` instead
- **Fix:** Replaced all 42 occurrences across all test files using sed bulk replacement
- **Files modified:** All 11 test stub files in tests/Recrd.Cli.Tests/
- **Commit:** 88bde32 (included in Task 2 commit)

**2. [Rule 2 - Missing] Added System.CommandLine + MEL package refs to test project**
- **Found during:** Task 2 build verification
- **Issue:** Test files reference `Command` (System.CommandLine) and `ILoggerFactory` (MEL) types — transitive references through `recrd-cli.csproj` are not sufficient for test project compilation in .NET
- **Fix:** Added `System.CommandLine 2.0.5` and `Microsoft.Extensions.Logging 10.0.5` as direct `PackageReference` in `Recrd.Cli.Tests.csproj`
- **Files modified:** `tests/Recrd.Cli.Tests/Recrd.Cli.Tests.csproj`
- **Commit:** 88bde32

**3. [Rule 2 - Missing] Added parameterless Create() overloads for test accessibility**
- **Found during:** Task 2 (design decision)
- **Issue:** Test stubs call `StartCommand.Create()` without arguments, but production `Create(verbosityOption, logOutputOption)` requires `Option<string>` parameters
- **Fix:** Added `internal static Command Create()` overloads in `StartCommand` that construct local dummy options — enables tests to call `Create()` without global option wiring
- **Files modified:** `apps/recrd-cli/Commands/StartCommand.cs`
- **Commit:** 88bde32

## Known Stubs

The following command handler stubs are intentional per plan scope — implementation deferred to Plans 02-04:

| File | Stub | Wired in |
|------|------|----------|
| `Commands/StartCommand.cs:47` | `Task.FromResult(0)` — IRecorderEngine not yet wired | Plan 02 |
| `Commands/SessionControlCommand.cs:17,30,43` | `Task.FromResult(0)` — socket IPC not yet wired | Plan 02 |
| `Commands/RecoverCommand.cs:25` | `Task.FromResult(0)` — IRecorderEngine.RecoverAsync not yet wired | Plan 02 |
| `Commands/CompileCommand.cs:66` | `Task.FromResult(0)` — ITestCompiler dispatch not yet wired | Plan 03 |
| `Commands/ValidateCommand.cs:23` | `Task.FromResult(0)` — AST validation not yet wired | Plan 03 |
| `Commands/SanitizeCommand.cs:31` | `Task.FromResult(0)` — literal stripping not yet wired | Plan 03 |
| `Commands/PluginsCommand.cs:44` (install) | `Task.FromResult(0)` — NuGet install not yet wired | Plan 04 |
| `Ipc/SessionSocket.cs` | Empty IAsyncDisposable stub | Plan 02 |
| `Ipc/SessionClient.cs` | Empty class stub | Plan 02 |

`PluginsCommand.list` is fully implemented (directory scan + "No plugins installed" message).
`VersionCommand` is fully implemented (Assembly.GetEntryAssembly version + runtime description).

## Threat Surface Scan

No new threat surface beyond what the plan's threat model already captures:
- File path arguments (`--data`, `--out`, `<session>`) — path canonicalization and `.recrd` extension validation deferred to Plans 02-04 where the handlers are implemented (T-8-01)
- `--verbosity` / `--log-output` fall through to defaults via switch expression (T-8-02 accepted)

## Self-Check: PASSED

| Check | Result |
|-------|--------|
| apps/recrd-cli/Program.cs | FOUND |
| apps/recrd-cli/Logging/LoggingSetup.cs | FOUND |
| apps/recrd-cli/Output/CliOutput.cs | FOUND |
| apps/recrd-cli/Commands/StartCommand.cs | FOUND |
| apps/recrd-cli/Commands/PluginsCommand.cs | FOUND |
| apps/recrd-cli/Ipc/SessionSocket.cs | FOUND |
| tests/Recrd.Cli.Tests/Recrd.Cli.Tests.csproj | FOUND |
| tests/Recrd.Cli.Tests/Logging/LoggingSetupTests.cs | FOUND |
| .github/workflows/ci.yml | FOUND |
| Commit 29c003b (TDD red) | FOUND |
| Commit 88bde32 (implementation) | FOUND |
| dotnet build apps/recrd-cli | PASSED (0 errors) |
