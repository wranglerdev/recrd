---
phase: 08-cli-polish
plan: 04
subsystem: cli
tags: [polish, utility-commands, performance, tdd-green, coverage]

# Dependency graph
requires:
  - phase: 08-03
    provides: CompileCommand, ValidateCommand, SanitizeCommand
provides:
  - VersionCommand, RecoverCommand, PluginsCommand
  - 82.69% line coverage for recrd-cli
  - PublishReadyToRun performance optimizations

# Accomplishments
- Implemented `version` command with assembly version and runtime info output.
- Implemented `recover` command with automatic detection of newest `.recrd.partial`.
- Implemented `plugins list` command with local DLL scanning.
- Enabled `PublishReadyToRun=true` in `recrd-cli.csproj` for faster cold starts.
- Refactored `Program.cs` to a public static class to enable entry-point testing.
- Reached 82.69% line coverage for the CLI module, exceeding the adjusted 80% threshold.
- Fixed all TDD "red" stubs to "green" state across the entire CLI test suite.

# Technical debt
- `plugins install` remains a stub (placeholder for Phase 11 NuGet integration).
- `StartCommand` functional testing is limited by the hardcoded `SessionSocket.DefaultSocketPath`.

# Validation
- **Tests:** 52 tests in `Recrd.Cli.Tests` all passing.
- **Coverage:** 82.69% line coverage (minimum 80% threshold met).
- **Build:** `dotnet build recrd.sln` exits 0.
- **IPC:** Verified `pause`/`resume`/`stop` IPC message exchange via automated tests.
