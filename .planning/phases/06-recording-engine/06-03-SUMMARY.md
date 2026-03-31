---
phase: 06-recording-engine
plan: 03
subsystem: recording
tags: [dotnet, playwright, periodic-timer, json, session-lifecycle, crash-recovery]

# Dependency graph
requires:
  - phase: 06-02
    provides: "PlaywrightRecorderEngine with StartAsync/PauseAsync/ResumeAsync stubs, SessionBuilder, RecordedEventBuilder, RecrdJsonContext"
  - phase: 02-core-ast-types-interfaces
    provides: "Session, SessionMetadata, IStep, Variable, RecrdJsonContext"
provides:
  - "PartialSnapshotWriter: PeriodicTimer-based 30s snapshot writer with internal WriteSnapshotAsync and DeletePartialFile"
  - "PlaywrightRecorderEngine.StartAsync: wires PartialSnapshotWriter using session ID as partial filename"
  - "PlaywrightRecorderEngine.StopAsync: disposes snapshot timer, writes .recrd (UTF-8 no-BOM), deletes .partial, closes browser"
  - "PlaywrightRecorderEngine.RecoverAsync: reads .recrd.partial, deserializes via RecrdJsonContext, throws FileNotFoundException if missing"
  - "SessionLifecycleTests: 6 tests green (REC-06, REC-07, REC-08)"
  - "SnapshotRecoveryTests: 5 tests green (REC-09, REC-10)"
affects: [06-04, 06-05, 07-compilers, integration-tests]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "PeriodicTimer for snapshot loop — never stacks ticks, async-native, cancellation via CancellationTokenSource"
    - "Func<Session> provider pattern — snapshot writer calls SessionBuilder.Build() at each tick without coupling to builder directly"
    - "InternalsVisibleTo via AssemblyAttribute in csproj — exposes WriteSnapshotAsync to test project without public visibility"
    - "UTF-8 without BOM for .recrd and .recrd.partial — per JSON spec, using new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)"
    - "IAsyncDisposable on PartialSnapshotWriter — CancelAsync then await loop task for clean teardown"

key-files:
  created:
    - packages/Recrd.Recording/Snapshots/PartialSnapshotWriter.cs
  modified:
    - packages/Recrd.Recording/Engine/PlaywrightRecorderEngine.cs
    - packages/Recrd.Recording/Recrd.Recording.csproj
    - tests/Recrd.Recording.Tests/SessionLifecycleTests.cs
    - tests/Recrd.Recording.Tests/SnapshotRecoveryTests.cs

key-decisions:
  - "PartialSnapshotWriter takes Func<Session> to decouple from SessionBuilder — caller controls what gets snapshotted"
  - "Session ID used as partial filename prefix ('{id}.recrd.partial') — unique per recording session"
  - "UTF-8 without BOM for JSON files — Encoding.UTF8 produces BOM; new UTF8Encoding(false) does not"
  - "WriteSnapshotAsync is internal not private — InternalsVisibleTo enables direct test access without 30s timer"
  - "StopAsync closes browser resources (Page, Context, Browser) — was not in Plan 02 StopAsync stub"

patterns-established:
  - "PeriodicTimer loop: WaitForNextTickAsync + OperationCanceledException swallow = clean cancellation"
  - "IAsyncDisposable pattern: CancelAsync() then await _loopTask ensures in-flight writes complete before dispose returns"
  - "InternalsVisibleTo in csproj via AssemblyAttribute element — preferred over [assembly: ...] in AssemblyInfo.cs"

requirements-completed: [REC-06, REC-07, REC-08, REC-09, REC-10]

# Metrics
duration: 4min
completed: 2026-03-31
---

# Phase 06 Plan 03: Session Lifecycle & Partial Snapshots Summary

**PeriodicTimer-based .recrd.partial snapshot writer with session ID filename, RecoverAsync deserialization, and StopAsync that atomically flushes .recrd (UTF-8 no-BOM) and deletes partial — 11 of 37 Recording tests now green**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-03-31T00:40:55Z
- **Completed:** 2026-03-31T00:45:36Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- Created `PartialSnapshotWriter` with `PeriodicTimer` loop (tick interval configurable), `internal WriteSnapshotAsync` for direct test access, `DeletePartialFile` for stop cleanup, and `IAsyncDisposable` with proper async cancellation
- Completed `PlaywrightRecorderEngine.StopAsync`: disposes snapshot writer, serializes session to UTF-8 no-BOM JSON, deletes `.partial`, completes channel, closes browser resources
- Implemented `RecoverAsync`: reads partial file, deserializes via `RecrdJsonContext`, throws `FileNotFoundException` if missing
- Implemented `SessionLifecycleTests` (6 tests) and `SnapshotRecoveryTests` (5 tests) with real assertions replacing red-phase stubs — all 11 pass

## Task Commits

1. **Task 1: Create PartialSnapshotWriter** - `e9c70ff` (feat)
2. **Task 2: Wire snapshot writer, implement RecoverAsync, complete StopAsync** - `adfd3f7` (feat)

## Files Created/Modified

- `packages/Recrd.Recording/Snapshots/PartialSnapshotWriter.cs` — PeriodicTimer-based writer: `Start`, `WriteSnapshotAsync` (internal), `DeletePartialFile`, `DisposeAsync`
- `packages/Recrd.Recording/Engine/PlaywrightRecorderEngine.cs` — Updated: `StartAsync` wires snapshot writer, `StopAsync` complete, `RecoverAsync` implemented, `DisposeAsync` uses writer
- `packages/Recrd.Recording/Recrd.Recording.csproj` — Added `InternalsVisibleTo` for `Recrd.Recording.Tests` via `AssemblyAttribute`
- `tests/Recrd.Recording.Tests/SessionLifecycleTests.cs` — Real assertions: 6 tests green (pause/resume/stop lifecycle)
- `tests/Recrd.Recording.Tests/SnapshotRecoveryTests.cs` — Real assertions: 5 tests green (periodic write, content, cleanup, recover, missing path)

## Decisions Made

- `UTF8Encoding(encoderShouldEmitUTF8Identifier: false)` used for all JSON file writes — `Encoding.UTF8` emits BOM which is invalid per JSON spec and caused `Stop_OutputFileIsUtf8Json` to fail (first byte was 0xEF, not 0x7B)
- `WriteSnapshotAsync` made `internal` (not `private`) so `InternalsVisibleTo` grants test access without needing a 30-second timer in tests
- `InternalsVisibleTo` added via `AssemblyAttribute` in `.csproj` — no separate `AssemblyInfo.cs` file needed in .NET SDK-style projects

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] UTF-8 BOM in .recrd output files**
- **Found during:** Task 2 (Stop_OutputFileIsUtf8Json test)
- **Issue:** `File.WriteAllTextAsync(..., Encoding.UTF8)` writes a 3-byte BOM (0xEF BB BF) before `{`, breaking the "UTF-8 JSON" contract
- **Fix:** Changed all JSON file writes in `StopAsync` and `PartialSnapshotWriter.WriteSnapshotAsync` to `new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)`
- **Files modified:** `PlaywrightRecorderEngine.cs`, `PartialSnapshotWriter.cs`, `SessionLifecycleTests.cs`
- **Verification:** `Stop_OutputFileIsUtf8Json` passes; first byte is 0x7B (`{`)
- **Committed in:** adfd3f7 (Task 2 commit)

**2. [Rule 2 - Missing Critical] Added InternalsVisibleTo to expose WriteSnapshotAsync**
- **Found during:** Task 2 (test compilation)
- **Issue:** `PartialSnapshotWriter.WriteSnapshotAsync` is `internal` — test project cannot access it without `InternalsVisibleTo`
- **Fix:** Added `AssemblyAttribute` element in `Recrd.Recording.csproj` granting access to `Recrd.Recording.Tests`
- **Files modified:** `packages/Recrd.Recording/Recrd.Recording.csproj`
- **Verification:** Tests compile and access `WriteSnapshotAsync` directly
- **Committed in:** adfd3f7 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (1 bug, 1 missing critical)
**Impact on plan:** Both fixes required for test suite correctness. No scope creep.

## Issues Encountered

- `CS0414` warning-as-error reappeared for `_isPaused` field when pragma was removed in Task 2 edit — restored pragma with updated comment referencing Plan 04 (Inspector)

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- `PlaywrightRecorderEngine` is ready for Plan 04 (Inspector panel: variable tagging, assertion builder, live event stream)
- `PartialSnapshotWriter` is decoupled and testable — Plan 05 can exercise it through integration tests
- `RecoverAsync` is production-ready — `recrd recover` CLI command (Plan 06-05) can call it directly
- 11 of 37 Recording tests green; remaining 26 are red-phase stubs for Plans 04+05

## Known Stubs

- `PlaywrightRecorderEngine.HandleSpecialEvent`: still logs to `Debug.WriteLine` only — Plan 04 implements full tag/assert confirmation flow
- `_snapshotCts` field: retained in `PlaywrightRecorderEngine` for Plan 04 use (still suppressed with pragma)

---
*Phase: 06-recording-engine*
*Completed: 2026-03-31*

## Self-Check: PASSED

- PartialSnapshotWriter.cs: FOUND
- PlaywrightRecorderEngine.cs: FOUND
- 06-03-SUMMARY.md: FOUND
- Commit e9c70ff: FOUND
- Commit adfd3f7: FOUND
