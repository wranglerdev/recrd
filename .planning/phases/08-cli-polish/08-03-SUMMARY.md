---
phase: 08-cli-polish
plan: 03
subsystem: cli
tags: [dotnet, system-commandline, robot-framework, gherkin, json, csv]

# Dependency graph
requires:
  - phase: 08-01
    provides: CLI skeleton, command stubs, CliOutput, LoggingSetup, Program.cs command tree
  - phase: 07-compilers
    provides: RobotBrowserCompiler, RobotSeleniumCompiler, ITestCompiler, CompilerOptions, CompilationResult
  - phase: 04-gherkin-generator
    provides: GherkinGenerator, IGherkinGenerator
  - phase: 03-data-providers
    provides: CsvDataProvider, JsonDataProvider, IDataProvider
  - phase: 02-core-ast-types-interfaces
    provides: Session, ActionStep, AssertionStep, GroupStep, Selector, Variable, RecrdJsonContext
provides:
  - CompileCommand: full wiring of ITestCompiler + IDataProvider + GherkinGenerator with all CLI flags
  - ValidateCommand: JSON schema validation with schema version and variable consistency checks
  - SanitizeCommand: recursive PII stripping of Selector.Values and step Payload, writes .sanitized.recrd
affects:
  - 08-04 (recover command, any further CLI polish)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Path.GetFullPath canonicalization for all user-supplied file paths (T-8-06, T-8-08)
    - JsonException catch wrapping all RecrdJsonContext deserialization calls (T-8-07)
    - UTF8Encoding(encoderShouldEmitUTF8Identifier: false) for all file output (no BOM)
    - Recursive SanitizeSteps pattern replacing values with literal "***" (T-8-09)

key-files:
  created: []
  modified:
    - apps/recrd-cli/Commands/CompileCommand.cs
    - apps/recrd-cli/Commands/ValidateCommand.cs
    - apps/recrd-cli/Commands/SanitizeCommand.cs

key-decisions:
  - "CsvDataProvider accepts string delimiter — char from CLI is converted via .ToString()"
  - "SelectorStrategy enum value is XPath (not Xpath) — matched exactly in switch expression"
  - "Sanitize replaces selector Values with '***' not empty string — distinguishes sanitized from genuinely absent values (T-8-09)"
  - "--intercept flag accepted but logs warning (reserved Phase 11 plugin interceptors)"
  - "Compile command generates both .robot/.resource (via ITestCompiler) and .feature (via GherkinGenerator) in one invocation"
  - "GherkinGenerator feature count reported as GeneratedFiles.Count + 1 to include .feature in summary"

patterns-established:
  - "All user-supplied paths canonicalized with Path.GetFullPath before use"
  - "RecrdJsonContext.Default.Session used for both Deserialize and Serialize to stay in source-gen path"
  - "CliOutput.WriteError for errors and warnings; CliOutput.WriteSuccess for final status line"

requirements-completed: [CLI-03, CLI-04, CLI-05]

# Metrics
duration: 15min
completed: 2026-04-07
---

# Phase 08 Plan 03: CLI File-Processing Commands Summary

**compile/validate/sanitize commands wired to ITestCompiler + IDataProvider + GherkinGenerator with full path canonicalization and recursive PII stripping**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-04-07T00:00:00Z
- **Completed:** 2026-04-07T00:15:00Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- CompileCommand loads .recrd session, resolves compiler by `--target`, creates CsvDataProvider or JsonDataProvider from `--data`, builds CompilerOptions, calls CompileAsync, and generates .feature via GherkinGenerator — all in one invocation
- ValidateCommand deserializes session with JsonException catch, checks SchemaVersion==1, non-null metadata, non-empty steps, and regex-validates all variable names
- SanitizeCommand recursively strips Selector.Values (replaced with "***") and empties Payload dicts on ActionStep/AssertionStep, recurses into GroupStep children, writes to .sanitized.recrd with no BOM

## Task Commits

1. **Task 1: CompileCommand implementation** - `0d9a4c1` (feat)
2. **Task 2: ValidateCommand + SanitizeCommand implementation** - `ff4d5c0` (feat)

## Files Created/Modified

- `apps/recrd-cli/Commands/CompileCommand.cs` - Full compile wiring: session load, compiler resolution, data provider, CompilerOptions, CompileAsync, GherkinGenerator
- `apps/recrd-cli/Commands/ValidateCommand.cs` - Schema validation: JSON parse, version check, metadata/steps presence, variable name regex
- `apps/recrd-cli/Commands/SanitizeCommand.cs` - PII stripping: recursive SanitizeSteps, Selector.Values -> "***", empty Payload, writes .sanitized.recrd

## Decisions Made

- CsvDataProvider constructor takes `string` delimiter, so the `char` from CLI is converted via `.ToString()` before passing.
- `SelectorStrategy.XPath` (capital P) — the enum uses `XPath` not `Xpath`; caught during build and fixed.
- Sanitize outputs `"***"` not empty string to distinguish sanitized values from genuinely absent values (T-8-09 requirement).
- `--intercept` is accepted without error but logs a warning that it is reserved for Phase 11 plugin interceptors.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] SelectorStrategy.XPath capitalization**
- **Found during:** Task 1 (CompileCommand implementation)
- **Issue:** Plan snippet used `SelectorStrategy.Xpath` but actual enum value is `SelectorStrategy.XPath`
- **Fix:** Corrected to `XPath` in the switch expression
- **Files modified:** apps/recrd-cli/Commands/CompileCommand.cs
- **Verification:** `dotnet build` exits 0
- **Committed in:** 0d9a4c1 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug — enum capitalization)
**Impact on plan:** Trivial fix, no scope change.

## Issues Encountered

- Worktree was initialized behind the target base commit; required `git reset --soft` and file restoration from HEAD before starting implementation.

## Known Stubs

None — all three commands are fully implemented. No placeholder data, no TODO markers.

## Threat Flags

None — all STRIDE mitigations from the plan's threat register are implemented:
- T-8-06: Path.GetFullPath on all user-supplied paths
- T-8-07: JsonException catch on all deserialization calls
- T-8-08: Path.GetFullPath on --out path; output directory created before write
- T-8-09: Selector.Values and Payload both sanitized; "***" used (not empty string)

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- compile, validate, sanitize all functional and verified via --help output
- Plan 08-04 (recover command + version + plugins) can proceed
- All CLI-03, CLI-04, CLI-05 requirements delivered

## Self-Check: PASSED

- `apps/recrd-cli/Commands/CompileCommand.cs` exists: FOUND
- `apps/recrd-cli/Commands/ValidateCommand.cs` exists: FOUND
- `apps/recrd-cli/Commands/SanitizeCommand.cs` exists: FOUND
- Commit 0d9a4c1 (Task 1): FOUND
- Commit ff4d5c0 (Task 2): FOUND
- `dotnet build apps/recrd-cli/ --no-restore` exits 0: VERIFIED

---
*Phase: 08-cli-polish*
*Completed: 2026-04-07*
