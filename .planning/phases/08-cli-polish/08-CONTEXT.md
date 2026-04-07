# Phase 8: CLI Polish - Context

**Gathered:** 2026-04-07 (discuss mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

`recrd-cli` exposes the complete command surface of the tool: `start`, `pause`, `resume`, `stop`, `compile`, `validate`, `sanitize`, `recover`, `version`, `plugins list`, `plugins install`. Every command has `--help` documentation, `--verbosity` control, and structured JSON logging via `--log-output json`. Cold-start time (`time recrd version`) must be under 500 ms on all target platforms.

CLI implementation lives in `apps/recrd-cli/Program.cs` — currently a placeholder. Recording engine, compilers, data providers, and Gherkin generator are already implemented in prior phases. This phase wires them together into the CLI surface.

Plugin system internals (AssemblyLoadContext isolation, plugin discovery) and VS Code extension are out of scope — those are Phases 11 and 10 respectively. `plugins list` and `plugins install` stubs are in scope for this phase (surface and help text, with minimal discovery logic that reads `~/.recrd/plugins/`).

</domain>

<decisions>
## Implementation Decisions

### CLI Framework

- **D-01:** Use **System.CommandLine** (Microsoft). It supports trimming and NativeAOT, is the framework used by `dotnet` CLI itself, and is most likely to achieve the < 500 ms cold-start target (CLI-12). It has built-in `--help` generation, `--verbosity`, and tab completion support.

### Session IPC Model

- **D-02:** `recrd start` creates a **Unix domain socket at `~/.recrd/session.sock`** and listens for control commands. `recrd pause`, `recrd resume`, and `recrd stop` connect to that socket and send a command message. The socket file is deleted when the session ends.
- **D-03:** **Single session only** — `recrd start` checks for an existing `session.sock` and exits with a clear error if one is found: `"A session is already running. Use 'recrd stop' to end it first."` No multi-session or session IDs in this phase.
- **D-04:** The IPC protocol is a minimal JSON message over the socket: `{ "command": "pause" | "resume" | "stop" }`. The `stop` command triggers the summary output from the running process, then the socket is closed and deleted.

### Terminal Output Style

- **D-05:** **Plain text + ANSI color** using `Console.ForegroundColor` or minimal ANSI escape codes (no Spectre.Console dependency). Errors print in red to stderr; success/info in default/green to stdout. No tables, no spinners — clean and scriptable.
- **D-06:** `--log-output json` switches to machine-parseable structured JSON logs via `Microsoft.Extensions.Logging` with a JSON console formatter. This is the machine output mode (CLI-10). Human and JSON modes are mutually exclusive.
- **D-07:** `--verbosity quiet|normal|detailed|diagnostic` maps to log level filtering: quiet = errors only, normal = warnings + info, detailed = debug, diagnostic = trace. Applied globally across all commands (CLI-09).

### `sanitize` I/O Behavior

- **D-08:** `recrd sanitize <session.recrd>` emits **`<basename>.sanitized.recrd`** in the same directory as the input file. The original is never modified. Example: `session.recrd` → `session.sanitized.recrd`. Explicit `--out <path>` overrides the default output location.

### `recrd stop` Summary

- **D-09:** The summary printed by `recrd stop` (CLI-11) is triggered by the stop command received over the session socket. The running `recrd start` process prints it to stdout before exiting:
  ```
  Session complete
    Events captured:  142
    Variables:        3
    Duration:         4m 22s
    Output:           session.recrd (48 KB)
    Partial file:     session.recrd.partial (deleted)
  ```

### TDD Mandate (carries forward)

- **D-10:** All CLI tests committed failing on `tdd/phase-08` branch before any implementation begins. Green phase only after all tests pass. Coverage gate ≥ 90% applies to `recrd-cli` command handler logic. Tests are a mix of unit tests (command handler classes) and integration tests (CLI subprocess invocation for exit codes and output assertions).

### Claude's Discretion

- Exact System.CommandLine command tree structure (subcommand grouping, root command setup)
- Socket message framing / length-prefix vs newline-delimited JSON
- How `plugins list` discovers assemblies in `~/.recrd/plugins/` (basic directory scan, no AssemblyLoadContext yet — that's Phase 11)
- Whether `recrd recover` runs automatically on startup if a partial file exists, or requires explicit invocation
- Internal class structure for command handlers (one class per command vs grouped)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements

- `.planning/REQUIREMENTS.md` §CLI Surface (CLI-01–CLI-12) — full command spec: all commands, flags, verbosity, cold-start target, stop summary, JSON output mode
- `.planning/REQUIREMENTS.md` §Plugin System (PLUG-01) — plugins list discovery scope for this phase (basic `~/.recrd/plugins/` scan only)

### Project Guidelines

- `CLAUDE.md` — TDD mandate (red-green per phase, `tdd/phase-*` branch prefix for CI-06), `DOTNET_SYSTEM_NET_DISABLEIPV6=1` prefix requirement

### Prior Phase Decisions (binding)

- `.planning/phases/02-core-ast-types-interfaces/02-CONTEXT.md` — `ITestCompiler`, `IDataProvider`, `IRecorderEngine` interface contracts; `Session` serialization; `RecordedEvent` channel
- `.planning/phases/06-recording-engine/06-CONTEXT.md` — `IRecorderEngine` (start/pause/resume/stop), inspector panel, `.recrd` / `.recrd.partial` file format, `recrd recover` implementation
- `.planning/phases/07-compilers/07-CONTEXT.md` — `CompilerOptions`, `CompilationResult`, compiler target names (`robot-browser`, `robot-selenium`)
- `.planning/phases/05-ci-pipeline/05-CONTEXT.md` — coverage gate pattern (per-project `dotnet test --threshold 90`)

### Core Entry Point

- `apps/recrd-cli/Program.cs` — current placeholder; Phase 8 replaces this entirely
- `apps/recrd-cli/recrd-cli.csproj` — already references all 5 packages (Core, Recording, Data, Gherkin, Compilers)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- `packages/Recrd.Recording/` — `IRecorderEngine` implementation: `StartAsync`, `PauseAsync`, `ResumeAsync`, `StopAsync` already exist. CLI `start` wires to `StartAsync`; session socket dispatches pause/resume/stop.
- `packages/Recrd.Compilers/` — `RobotBrowserCompiler` and `RobotSeleniumCompiler` implement `ITestCompiler`. `compile` command resolves target by name and calls `CompileAsync`.
- `packages/Recrd.Gherkin/` — `GherkinGenerator` used by `compile` command to emit `.feature` alongside compiler output (or separately if user passes `--gherkin`).
- `packages/Recrd.Data/` — `CsvDataProvider` and `JsonDataProvider` for `--data` flag on `compile`.
- `packages/Recrd.Core/Ast/` — `Session` with `RecrdJsonContext` for `validate` and `sanitize` commands (deserialize → validate → re-serialize stripped of literals).

### Established Patterns

- **TDD red-green**: All prior phases committed tests on `tdd/phase-*` before implementation. Phase 8 follows the same.
- **`DOTNET_SYSTEM_NET_DISABLEIPV6=1`**: Required on all `dotnet` commands in CI and locally (NuGet restore hangs on IPv6).
- **UTF8Encoding(false)**: Used for `.recrd` and `.recrd.partial` files — no BOM. `sanitize` output must follow same encoding.
- **`IAsyncEnumerable<T>` + streaming**: `IDataProvider` pattern; `compile --data` must not load all rows into memory.

### Integration Points

- **`recrd start`** → instantiates `PlaywrightRecorderEngine`, opens browser, creates `~/.recrd/session.sock`, blocks until `stop` command received
- **`recrd stop`** → connects to `session.sock`, sends `{ "command": "stop" }`, engine prints summary and exits
- **`recrd compile`** → loads session file, selects `ITestCompiler` by `--target`, calls `CompileAsync`
- **`recrd validate`** → deserializes session JSON with `RecrdJsonContext`, checks schema + variable consistency, exits non-zero on error
- **`recrd sanitize`** → deserializes session, strips all `Selector.Values` literal values and step literal data, re-serializes to `<basename>.sanitized.recrd`

</code_context>

<specifics>
## Specific Ideas

- Socket protocol: minimal `{ "command": "..." }` JSON messages — no custom framing needed for this phase
- `recrd start` error when session already running: `"A session is already running. Use 'recrd stop' to end it first."`
- `recrd sanitize` output naming: `<basename>.sanitized.recrd` (same directory as input, unless `--out` provided)
- Stop summary format: plain text block (no tables), labels left-aligned, values tab-indented

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 08-cli-polish*
*Context gathered: 2026-04-07*
