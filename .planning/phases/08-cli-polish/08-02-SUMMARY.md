---
phase: 08-cli-polish
plan: 02
subsystem: cli
tags: [dotnet, unix-socket, ipc, system.commandline, playwright]

# Dependency graph
requires:
  - phase: 08-01
    provides: StartCommand/SessionControlCommand stubs, CliOutput, LoggingSetup, command tree

provides:
  - Unix domain socket server (SessionSocket) with stale-socket detection and command validation
  - Unix domain socket client (SessionClient) for sending control commands
  - IpcMessage record for newline-delimited JSON IPC protocol
  - StartCommand fully wired to PlaywrightRecorderEngine with session lifecycle control
  - SessionControlCommand (pause/resume/stop) as thin IPC clients

affects: [08-03, 08-04, tests/Recrd.Cli.Tests]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Unix domain socket IPC: socket server in start process, thin clients in control commands"
    - "Newline-delimited JSON: one IpcMessage per line over NetworkStream"
    - "Stale socket detection: probe connect on startup, delete file on SocketException"
    - "Command validation against allowlist before dispatch (T-8-04 mitigation)"

key-files:
  created:
    - apps/recrd-cli/Ipc/IpcMessage.cs
    - apps/recrd-cli/Ipc/SessionSocket.cs (replaced stub)
    - apps/recrd-cli/Ipc/SessionClient.cs (replaced stub)
  modified:
    - apps/recrd-cli/Commands/StartCommand.cs
    - apps/recrd-cli/Commands/SessionControlCommand.cs

key-decisions:
  - "SessionSocket validates commands against allowlist (pause/resume/stop) before dispatch — ignores unknown commands with warning log (T-8-04)"
  - "SessionClient is a static class, not an instance — thin client pattern with no state"
  - "RecordingChannel created in StartCommand and passed to PlaywrightRecorderEngine constructor"
  - "Partial file deletion handled in StartCommand after stop: file.partial deleted, partialDeleted flag passed to WriteSummary"
  - "IpcMessage.Command in comment in SessionControlCommand is acceptable — actual code never instantiates IRecorderEngine"

patterns-established:
  - "IPC control commands are fire-and-forget: connect, send one line, disconnect"
  - "Socket server loop breaks on stop command, then finalizes session in the start process"
  - "DisposeAsync on SessionSocket is belt-and-suspenders: deletes socket file if missed by loop finally block"

requirements-completed: [CLI-01, CLI-02, CLI-11]

# Metrics
duration: 15min
completed: 2026-04-07
---

# Phase 08 Plan 02: Session Lifecycle IPC Summary

**Unix domain socket IPC for session control: SessionSocket server + SessionClient thin clients + StartCommand wired to PlaywrightRecorderEngine with stop summary**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-04-07T03:52:00Z
- **Completed:** 2026-04-07T04:07:07Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- Created full IPC infrastructure: IpcMessage record, SessionSocket server, SessionClient thin client
- StartCommand wired to PlaywrightRecorderEngine via RecordingChannel, opens socket, dispatches pause/resume, prints stop summary via CliOutput.WriteSummary
- SessionControlCommand (pause/resume/stop) replaced stubs with SessionClient.SendCommandAsync calls — zero engine instantiation in control commands
- Stale socket detection implemented: IsSessionActiveAsync probes, deletes dead socket file on SocketException (T-8-05)
- IPC command validation against allowlist before dispatch (T-8-04)

## Task Commits

1. **Task 1: IPC infrastructure + StartCommand** - `49247c1` (feat)
2. **Task 2: SessionControlCommand pause/resume/stop** - `067c418` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `apps/recrd-cli/Ipc/IpcMessage.cs` - Newline-delimited JSON IPC record: `record IpcMessage(string Command)`
- `apps/recrd-cli/Ipc/SessionSocket.cs` - Unix domain socket server with stale-socket detection, command allowlist validation, loop-exits on stop
- `apps/recrd-cli/Ipc/SessionClient.cs` - Static thin client: connects, sends one JSON line, disconnects
- `apps/recrd-cli/Commands/StartCommand.cs` - Full implementation: parses RecorderOptions, checks duplicate session (D-03), creates RecordingChannel, starts engine, opens socket, dispatches commands, prints summary (D-09/CLI-11)
- `apps/recrd-cli/Commands/SessionControlCommand.cs` - pause/resume/stop replaced stubs with SessionClient.SendCommandAsync + WriteSuccess

## Decisions Made

- `RecordingChannel` created in `StartCommand.SetAction` and passed to `PlaywrightRecorderEngine(channel)` — the constructor signature requires `IRecordingChannel`, not `ILogger` (discovered during task execution)
- `SessionClient` is a `static class` (not an instance class) — fits the fire-and-forget pattern with no shared state
- Partial file (.recrd.partial) deletion is handled in StartCommand after StopAsync — check existence, delete, set `partialDeleted` flag for summary

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] PlaywrightRecorderEngine constructor takes IRecordingChannel, not ILogger**
- **Found during:** Task 1 (StartCommand implementation)
- **Issue:** Plan showed `new PlaywrightRecorderEngine(logger)` but the actual constructor signature is `PlaywrightRecorderEngine(IRecordingChannel channel)` — passing a logger would not compile
- **Fix:** Created `RecordingChannel` instance in StartCommand and passed it as the constructor argument
- **Files modified:** apps/recrd-cli/Commands/StartCommand.cs (added `using Recrd.Core.Pipeline` + `using var channel = new RecordingChannel()`)
- **Verification:** `dotnet build apps/recrd-cli/ --no-restore` exits 0 with 0 warnings
- **Committed in:** `49247c1` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — Bug)
**Impact on plan:** Necessary fix for compilation. No scope creep.

## Issues Encountered

None — build succeeded on first attempt after the constructor fix.

## Known Stubs

None — all IPC and command implementations are fully wired.

## Threat Flags

No new security surface beyond what the plan's threat model already covers (T-8-03, T-8-04, T-8-05 all mitigated).

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- IPC infrastructure complete and ready for use by 08-03 (compile/validate/sanitize/recover) and 08-04 (distribution/version/plugins)
- StartCommand is fully functional; manual testing requires Playwright browsers installed (`bash packages/Recrd.Recording/bin/Debug/net10.0/playwright.sh install`)
- All three control commands (pause/resume/stop) are socket clients ready for integration testing

---
*Phase: 08-cli-polish*
*Completed: 2026-04-07*
