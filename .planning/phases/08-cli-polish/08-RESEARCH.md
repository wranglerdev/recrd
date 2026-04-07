# Phase 8: CLI Polish - Research

**Researched:** 2026-04-07
**Domain:** .NET 10 CLI framework (System.CommandLine), Unix domain socket IPC, structured logging, cold-start performance
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Use **System.CommandLine** (Microsoft). Supports trimming and NativeAOT, is the framework used by `dotnet` CLI itself, most likely to achieve the < 500 ms cold-start target (CLI-12). Built-in `--help`, `--verbosity`, tab completion support.
- **D-02:** `recrd start` creates a **Unix domain socket at `~/.recrd/session.sock`** and listens for control commands. `recrd pause`, `recrd resume`, `recrd stop` connect to that socket and send a command message. Socket file is deleted when session ends.
- **D-03:** **Single session only** — `recrd start` checks for existing `session.sock` and exits with clear error if found: `"A session is already running. Use 'recrd stop' to end it first."` No multi-session or session IDs in this phase.
- **D-04:** IPC protocol: minimal JSON message over socket: `{ "command": "pause" | "resume" | "stop" }`. `stop` command triggers summary output from the running process, then socket is closed and deleted.
- **D-05:** **Plain text + ANSI color** using `Console.ForegroundColor` or minimal ANSI escape codes (no Spectre.Console dependency). Errors print in red to stderr; success/info in default/green to stdout. No tables, no spinners — clean and scriptable.
- **D-06:** `--log-output json` switches to machine-parseable structured JSON logs via `Microsoft.Extensions.Logging` with JSON console formatter. Human and JSON modes are mutually exclusive.
- **D-07:** `--verbosity quiet|normal|detailed|diagnostic` maps to log level filtering: quiet = errors only, normal = warnings + info, detailed = debug, diagnostic = trace. Applied globally across all commands.
- **D-08:** `recrd sanitize <session.recrd>` emits **`<basename>.sanitized.recrd`** in same directory. Original never modified. Explicit `--out <path>` overrides.
- **D-09:** Stop summary triggered by stop command over socket. Running `recrd start` process prints it to stdout before exiting (format: plain text block, labels left-aligned, values tab-indented).
- **D-10:** TDD mandate: all CLI tests committed failing on `tdd/phase-08` branch before any implementation. ≥ 90% coverage on `recrd-cli` command handler logic.

### Claude's Discretion

- Exact System.CommandLine command tree structure (subcommand grouping, root command setup)
- Socket message framing / length-prefix vs newline-delimited JSON
- How `plugins list` discovers assemblies in `~/.recrd/plugins/` (basic directory scan, no AssemblyLoadContext yet)
- Whether `recrd recover` runs automatically on startup if a partial file exists, or requires explicit invocation
- Internal class structure for command handlers (one class per command vs grouped)

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CLI-01 | `recrd start [--browser chromium|firefox|webkit] [--headed] [--viewport WxH] [--base-url url]` | System.CommandLine Option<T> for each flag; maps to RecorderOptions |
| CLI-02 | `recrd pause`, `recrd resume`, `recrd stop` | Unix socket client sending JSON command message to session.sock |
| CLI-03 | `recrd compile <session.recrd> [--target ...] [--data file] [--csv-delimiter char] [--out dir] [--selector-strategy chain] [--timeout secs] [--intercept]` | System.CommandLine Argument<FileInfo> + multiple Option<T>; maps to CompilerOptions |
| CLI-04 | `recrd validate <session.recrd>` — exits non-zero on invalid AST or variable consistency failure | Deserialize with RecrdJsonContext, validate, exit code pattern |
| CLI-05 | `recrd sanitize <session.recrd>` — strips literals, keeps structure | Deserialize + strip Selector.Values + step literal data, re-serialize |
| CLI-06 | `recrd recover` — reconstructs session from latest .recrd.partial | IRecorderEngine.RecoverAsync already exists in Recrd.Recording |
| CLI-07 | `recrd version` — prints version and runtime info | Assembly.GetEntryAssembly().GetName().Version or VersionOption |
| CLI-08 | `recrd plugins list` / `recrd plugins install <pkg>` — plugin management stubs | Directory scan of ~/.recrd/plugins/; no AssemblyLoadContext yet |
| CLI-09 | `--verbosity quiet|normal|detailed|diagnostic` across all commands | ILogger minimum level filter mapped to enum |
| CLI-10 | Structured logging via Microsoft.Extensions.Logging; `--log-output json` | AddJsonConsole() on ILoggingBuilder |
| CLI-11 | `recrd stop` prints summary: events, variables, duration, file sizes | Triggered by stop message over socket; running process prints before exit |
| CLI-12 | CLI cold start < 500 ms on Windows, macOS, Linux | ReadyToRun publish + trim; NativeAOT path available but Playwright dep may block |

</phase_requirements>

---

## Summary

Phase 8 wires the already-implemented packages (Recrd.Recording, Recrd.Compilers, Recrd.Gherkin, Recrd.Data, Recrd.Core) together through `apps/recrd-cli/Program.cs`, replacing the placeholder with a full System.CommandLine command tree. The architecture is straightforward: the CLI is a thin dispatch layer — it parses arguments, creates options objects, and calls the right interface methods.

The two technically novel pieces are (1) the Unix domain socket IPC for session lifecycle control, and (2) meeting the 500 ms cold-start target on all platforms. The cold-start target is achievable without NativeAOT using ReadyToRun (`PublishReadyToRun=true`) combined with trimming. NativeAOT is possible for the CLI itself but is blocked by Playwright's ~200 MB browser binaries and reflection-heavy internals — this is a Phase 9 distribution concern, not Phase 8. For Phase 8 development builds, standard JVM-based dotnet CLI (70–80 ms startup per benchmarks) plus ReadyToRun should comfortably clear 500 ms.

System.CommandLine reached stable 2.0.5 on NuGet. The API changed significantly from the beta series — `SetHandler` → `SetAction`, `CommandLineBuilder` removed, `IConsole` removed in favor of `TextWriter` properties. Any tutorial or sample predating beta5 uses the wrong API. Use the migration guide as the authoritative pattern source.

**Primary recommendation:** Use `System.CommandLine 2.0.5` with `SetAction` (async, with `CancellationToken`), organize handlers as one class per command group, wire `Microsoft.Extensions.Logging` with a global `ILoggerFactory` created from parsed `--verbosity` and `--log-output` flags before command dispatch.

---

## Project Constraints (from CLAUDE.md)

| Directive | Applies To |
|-----------|-----------|
| Prefix all `dotnet` commands with `DOTNET_SYSTEM_NET_DISABLEIPV6=1` | All build/test commands in CI and locally |
| Use `bash playwright.sh install` not `.ps1` | Playwright browser install on Linux |
| xUnit + Moq, ≥ 90% line coverage | CLI command handler test project |
| TDD red-green mandate: `tdd/phase-08` branch prefix | CI test-failure tolerance during red phase |
| `UTF8Encoding(false)` for `.recrd` files | `sanitize` output encoding |
| `IAsyncEnumerable<T>` streaming for `IDataProvider` | `compile --data` flag, do not buffer all rows |
| `PlaceholderTests.cs` pattern in all test projects | Prevents xunit no-tests-found exit code 1 on empty projects |
| `IsPackable=false` on all test `.csproj` files | Prevents `dotnet pack` from emitting test NuGet packages |

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.CommandLine | 2.0.5 | CLI command tree, option/argument parsing, `--help` generation | Microsoft-official, NativeAOT/trimming support, used by dotnet CLI itself |
| Microsoft.Extensions.Logging | 10.0.5 | Structured logging abstraction, ILogger / ILoggerFactory | Microsoft-official, ship in-box with .NET 10 |
| Microsoft.Extensions.Logging.Console | 10.0.5 | Console and JSON console formatters (`AddConsole`, `AddJsonConsole`) | Ships with .NET 10; provides built-in JsonConsoleFormatter |

[VERIFIED: NuGet registry via `dotnet package search`]

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Net.Sockets (built-in) | .NET 10 SDK | Unix domain socket server (recrd start) and client (pause/resume/stop) | Built-in; no additional NuGet package needed |
| System.Text.Json (built-in) | .NET 10 SDK | JSON serialization for IPC messages and session files | Already used by RecrdJsonContext in Recrd.Core |

[VERIFIED: .NET 10 SDK includes both; confirmed by Directory.Build.props targeting net10.0]

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| System.CommandLine | Cocona, CliFx, Spectre.Console.Cli | System.CommandLine is locked by D-01; others have worse trim/AOT support |
| Microsoft.Extensions.Logging | Serilog, NLog | MEL is locked by D-06; fewer dependencies, no extra NuGet |
| Unix domain socket | Named pipe (NamedPipeServerStream) | Named pipes work on Windows too but D-02 locks UDS for simplicity |
| Newline-delimited JSON (IPC) | Length-prefix framing | Newline-delimited is simpler; messages are small, no partial-write risk |

**Installation:**
```bash
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet add apps/recrd-cli/recrd-cli.csproj package System.CommandLine --version 2.0.5
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet add apps/recrd-cli/recrd-cli.csproj package Microsoft.Extensions.Logging --version 10.0.5
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet add apps/recrd-cli/recrd-cli.csproj package Microsoft.Extensions.Logging.Console --version 10.0.5
```

**Version verification:** All versions confirmed via `dotnet package search` against NuGet registry on 2026-04-07.
- `System.CommandLine 2.0.5` — stable GA (not beta), Microsoft-owned
- `Microsoft.Extensions.Logging 10.0.5` — aligns with .NET 10.0.x
- `Microsoft.Extensions.Logging.Console 10.0.5` — aligns with .NET 10.0.x

---

## Architecture Patterns

### Recommended Project Structure

```
apps/recrd-cli/
├── Program.cs                   # Entry point — build command tree, parse, invoke
├── recrd-cli.csproj             # Add System.CommandLine, MEL packages
├── Commands/
│   ├── StartCommand.cs          # recrd start — IRecorderEngine wiring + socket server
│   ├── SessionControlCommand.cs # recrd pause/resume/stop — socket client
│   ├── CompileCommand.cs        # recrd compile — ITestCompiler dispatch
│   ├── ValidateCommand.cs       # recrd validate — AST schema check
│   ├── SanitizeCommand.cs       # recrd sanitize — literal stripping
│   ├── RecoverCommand.cs        # recrd recover — partial file reconstruction
│   ├── VersionCommand.cs        # recrd version — version + runtime info
│   └── PluginsCommand.cs        # recrd plugins list/install — stubs
├── Ipc/
│   ├── SessionSocket.cs         # Unix domain socket server (used by StartCommand)
│   └── SessionClient.cs         # Unix domain socket client (used by pause/resume/stop)
├── Logging/
│   └── LoggingSetup.cs          # Build ILoggerFactory from --verbosity + --log-output flags
└── Output/
    └── CliOutput.cs             # ANSI color helpers, summary formatting
```

```
tests/Recrd.Cli.Tests/           # NEW test project — mirroring apps/recrd-cli/
├── Recrd.Cli.Tests.csproj
├── PlaceholderTests.cs          # Prevent xunit no-tests-found on red phase
├── Commands/
│   ├── StartCommandTests.cs
│   ├── SessionControlCommandTests.cs
│   ├── CompileCommandTests.cs
│   ├── ValidateCommandTests.cs
│   ├── SanitizeCommandTests.cs
│   ├── RecoverCommandTests.cs
│   ├── VersionCommandTests.cs
│   └── PluginsCommandTests.cs
├── Ipc/
│   ├── SessionSocketTests.cs
│   └── SessionClientTests.cs
└── Integration/
    └── CliSubprocessTests.cs    # subprocess invocation for exit code + stdout/stderr assertions
```

### Pattern 1: System.CommandLine 2.0.5 Command Tree (SetAction, not SetHandler)

**What:** Build a `RootCommand` with subcommands using mutable collections. Attach handlers via `SetAction` (GA API). `CommandLineBuilder` is REMOVED in 2.0.5.
**When to use:** All commands in this phase.

```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/standard/commandline/migration-guide-2.0.0-beta5

var rootCommand = new RootCommand("recrd — record browser interactions, compile Robot Framework suites");

// Global options (added to root, inherited by subcommands via rootCommand.Options)
var verbosityOption = new Option<string>("--verbosity", "-v")
{
    Description = "Output verbosity: quiet|normal|detailed|diagnostic",
    DefaultValueFactory = _ => "normal"
};
var logOutputOption = new Option<string>("--log-output")
{
    Description = "Log format: json for machine-parseable output"
};
rootCommand.Options.Add(verbosityOption);
rootCommand.Options.Add(logOutputOption);

// Subcommand with options
var startCommand = new Command("start", "Start a recording session");
var browserOption = new Option<string>("--browser")
{
    Description = "Browser engine: chromium|firefox|webkit",
    DefaultValueFactory = _ => "chromium"
};
startCommand.Options.Add(browserOption);
startCommand.SetAction(async (ParseResult result, CancellationToken ct) =>
{
    // resolve ILoggerFactory from rootCommand options first
    var verbosity = result.GetValue(verbosityOption);
    var logJson = result.GetValue(logOutputOption) == "json";
    using var loggerFactory = LoggingSetup.Create(verbosity, logJson);
    return await new StartCommandHandler(loggerFactory).ExecuteAsync(result, ct);
});
rootCommand.Subcommands.Add(startCommand);

// Subcommand grouping (plugins list / plugins install)
var pluginsCommand = new Command("plugins", "Plugin management");
var pluginsListCommand = new Command("list", "List installed plugins");
pluginsListCommand.SetAction((ParseResult result) => { /* ... */ return 0; });
pluginsCommand.Subcommands.Add(pluginsListCommand);
rootCommand.Subcommands.Add(pluginsCommand);

return await rootCommand.Parse(args).InvokeAsync();
```

**Key 2.0.5 API changes from pre-beta5:**
- `Command.AddOption` → `Command.Options.Add` [VERIFIED: Microsoft Learn migration guide]
- `Command.AddCommand` → `Command.Subcommands.Add` [VERIFIED: Microsoft Learn migration guide]
- `Command.SetHandler` → `Command.SetAction` [VERIFIED: Microsoft Learn migration guide]
- `InvocationContext` removed — `ParseResult` passed directly to action [VERIFIED]
- `CommandLineBuilder` removed — use `ParserConfiguration` / `InvocationConfiguration` [VERIFIED]
- `IConsole` removed — use `InvocationConfiguration.Output` (`TextWriter`) for testability [VERIFIED]

### Pattern 2: Unix Domain Socket IPC (session lifecycle)

**What:** `recrd start` creates a `UnixDomainSocketEndPoint` at `~/.recrd/session.sock`, listens in a background task, dispatches commands to IRecorderEngine. Control commands connect, send one JSON line, and disconnect.
**When to use:** StartCommand (server side), SessionControlCommand (client side).

```csharp
// Source: System.Net.Sockets — .NET 10 built-in

// Server side (in StartCommand, runs as background Task)
var socketPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".recrd", "session.sock");

if (File.Exists(socketPath))
{
    Console.Error.WriteLine("A session is already running. Use 'recrd stop' to end it first.");
    return 1;
}

var endpoint = new UnixDomainSocketEndPoint(socketPath);
using var server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
server.Bind(endpoint);
server.Listen(1);

try
{
    while (true)
    {
        using var client = await server.AcceptAsync(ct);
        using var stream = new NetworkStream(client);
        using var reader = new StreamReader(stream);
        var line = await reader.ReadLineAsync(ct);
        if (line is null) continue;
        var msg = JsonSerializer.Deserialize<IpcMessage>(line);
        // dispatch msg.Command to engine.PauseAsync / ResumeAsync / StopAsync
        if (msg?.Command == "stop") break;
    }
}
finally
{
    File.Delete(socketPath);
}

// Client side (pause, resume, stop commands)
var endpoint = new UnixDomainSocketEndPoint(socketPath);
using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
await socket.ConnectAsync(endpoint, ct);
using var stream = new NetworkStream(socket);
using var writer = new StreamWriter(stream) { AutoFlush = true };
await writer.WriteLineAsync("""{ "command": "stop" }""");
```

**IPC framing decision (Claude's Discretion):** Use **newline-delimited JSON** (one message per line, `WriteLineAsync`/`ReadLineAsync`). Messages are small (`{ "command": "stop" }` is 20 bytes); no partial-write risk. No length-prefix needed.

### Pattern 3: Structured Logging with MEL + JsonConsole

**What:** Build `ILoggerFactory` from parsed `--verbosity` and `--log-output` flags. All command handlers receive `ILogger<T>`.
**When to use:** CLI-09, CLI-10.

```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter

public static ILoggerFactory Create(string verbosity, bool jsonOutput)
{
    var level = verbosity switch
    {
        "quiet"      => LogLevel.Error,
        "normal"     => LogLevel.Warning,   // warnings + info maps to Warning min level
        "detailed"   => LogLevel.Debug,
        "diagnostic" => LogLevel.Trace,
        _            => LogLevel.Warning
    };

    return LoggerFactory.Create(builder =>
    {
        builder.SetMinimumLevel(level);
        if (jsonOutput)
            builder.AddJsonConsole();
        else
            builder.AddConsole();
    });
}
```

**Note on "normal" verbosity:** D-07 says normal = warnings + info. `LogLevel.Information` (value 2) is below `LogLevel.Warning` (3). Setting minimum to `LogLevel.Information` captures both info and warning. Use `LogLevel.Information` for normal mode, not `LogLevel.Warning`. [ASSUMED — the exact log level mapping is a planner/implementation detail; the code here may need adjustment]

### Pattern 4: Exit Code Convention

**What:** `recrd validate` (CLI-04) exits non-zero on invalid schema. All commands return `int` from `SetAction`.
**When to use:** All commands — System.CommandLine uses the return value of `SetAction` as process exit code.

```csharp
validateCommand.SetAction((ParseResult result, CancellationToken ct) =>
{
    // ... validate session
    if (validationFailed)
    {
        Console.Error.WriteLine($"Validation failed: {error}");
        return Task.FromResult(1);  // non-zero exit
    }
    return Task.FromResult(0);
});
```

### Pattern 5: testability via InvocationConfiguration.Output

**What:** For CLI subprocess integration tests, redirect output. For unit tests of command handler classes, pass `ILoggerFactory` + `TextWriter` directly — no subprocess needed.
**When to use:** All command handler tests.

```csharp
// Unit test: capture output without subprocess
var output = new StringWriter();
var config = new InvocationConfiguration { Output = output };
var result = rootCommand.Parse(["validate", "test.recrd"]);
int exitCode = await result.InvokeAsync(config);
Assert.Equal(1, exitCode);
Assert.Contains("Validation failed", output.ToString());
```

### Anti-Patterns to Avoid

- **Using `SetHandler` / `InvocationContext` / `CommandLineBuilder`:** These are the old beta API. System.CommandLine 2.0.5 removed them. Any sample using these is pre-beta5 and will not compile.
- **Using `Command.AddOption` / `Command.AddCommand`:** Beta4 API. Use `Command.Options.Add` and `Command.Subcommands.Add`.
- **Loading `IRecorderEngine` in `pause`/`resume`/`stop` commands:** These commands do not start the engine — they connect to the socket and send a message. The engine lives in the `start` process.
- **Writing Playwright-starting code in recrd-cli directly:** `start` instantiates `PlaywrightRecorderEngine` from `Recrd.Recording` — the CLI is a thin wrapper, not a Playwright host.
- **Buffering `IDataProvider` rows in `compile --data`:** Must use `IAsyncEnumerable<T>` streaming per project contract.
- **Emitting BOM in sanitize output:** Use `new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)` — same as existing `.recrd` file writing.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Argument parsing, `--help` generation | Custom arg parser | System.CommandLine 2.0.5 | Edge cases: quoting, aliases, error reporting, tab completion |
| Structured JSON log output | Custom JSON serialization to Console | `builder.AddJsonConsole()` | Handles log level, exception formatting, timestamp formats |
| Session file deserialization | Custom JSON reader | `RecrdJsonContext` (already in Recrd.Core) | Polymorphic `$type` discriminators require metadata mode; already implemented |
| ANSI color output | Third-party library (Spectre.Console) | `Console.ForegroundColor` / direct ANSI codes | D-05 explicitly forbids Spectre.Console; plain ANSI is sufficient |
| Selector validation logic | Custom validator in CLI | `Recrd.Core.Ast.Variable` regex + existing validation | Already enforced at AST construction; `validate` command re-runs it |

**Key insight:** The CLI layer owns zero business logic. Every meaningful operation delegates to an already-implemented interface: `IRecorderEngine`, `ITestCompiler`, `IDataProvider`, `GherkinGenerator`. The only new logic is argument wiring, IPC socket protocol, and output formatting.

---

## Common Pitfalls

### Pitfall 1: Using pre-beta5 System.CommandLine API

**What goes wrong:** Code using `SetHandler`, `ICommandHandler`, `CommandLineBuilder`, `InvocationContext`, `AddOption`, `AddCommand` will fail to compile against 2.0.5.
**Why it happens:** Almost all tutorials, blog posts, and StackOverflow answers were written against beta3/beta4.
**How to avoid:** Use only `SetAction`, `ParseResult` (direct), mutable collections (`Options.Add`, `Subcommands.Add`), `InvocationConfiguration`.
**Warning signs:** Compiler error `CS0117: 'Command' does not contain a definition for 'AddOption'`.

[VERIFIED: Microsoft Learn migration guide — https://learn.microsoft.com/en-us/dotnet/standard/commandline/migration-guide-2.0.0-beta5]

### Pitfall 2: Global options not inherited by subcommands without explicit parent lookup

**What goes wrong:** `--verbosity` added to `RootCommand.Options` is parsed as part of the root, but a subcommand's `SetAction` only sees its own `ParseResult`. The global option value must be retrieved from the same `ParseResult` (which is the full parse result including parent options).
**Why it happens:** `ParseResult` in 2.0.5 does include parent command option values — `result.GetValue(verbosityOption)` works even in a subcommand's action as long as `verbosityOption` is in scope.
**How to avoid:** Keep the option reference (`verbosityOption`) in a shared scope accessible to all `SetAction` lambdas. Avoid re-creating option instances inside each command builder.
**Warning signs:** `GetValue` returns `null` for global options when invoked from subcommand context.

[ASSUMED — inferred from System.CommandLine 2.0.5 ParseResult semantics; verify with a minimal test]

### Pitfall 3: Socket file cleanup on abnormal termination

**What goes wrong:** `recrd start` crashes or is SIGKILL'd without cleaning up `session.sock`. Next `recrd start` finds the stale socket file and refuses to start.
**Why it happens:** `File.Delete` in a `finally` block works for exceptions but not SIGKILL.
**How to avoid:** At `recrd start` startup, check if socket file exists AND try connecting to it. If connection fails (nobody listening), delete the stale file and proceed. Only refuse to start if a live session is actually answering.
**Warning signs:** After `kill -9` on the recrd process, subsequent `recrd start` always fails.

[ASSUMED — standard Unix socket cleanup pattern; verify with implementation test]

### Pitfall 4: Cold-start regression from eager DI container construction

**What goes wrong:** Constructing a full `IServiceCollection` + `ServiceProvider` at startup adds 30–100 ms to cold start, potentially pushing past the 500 ms target.
**Why it happens:** `ServiceProvider` validation and reflection-based resolution has startup cost.
**How to avoid:** For Phase 8, do not use a DI container. Construct dependencies manually in each `SetAction`. The CLI's dependency graph is shallow (ILoggerFactory → ILogger → command handler). Full DI can be added in Phase 12 Hardening if needed.
**Warning signs:** `time recrd version` takes > 300 ms — leaves no headroom for logic.

[ASSUMED — general .NET startup analysis; verify with timing test during green phase]

### Pitfall 5: Using `IRecorderEngine` from `pause`/`resume`/`stop` processes

**What goes wrong:** Developer calls `PlaywrightRecorderEngine.PauseAsync()` directly from the `pause` subcommand. This creates a second engine instance that doesn't share state with the recording session.
**Why it happens:** The engine holds a live browser context; creating a second one is meaningless.
**How to avoid:** `pause`, `resume`, `stop` commands ONLY talk to the socket. They never instantiate `IRecorderEngine`. Only `start` and `recover` instantiate the engine.

[VERIFIED: CONTEXT.md D-02, D-04]

### Pitfall 6: `recrd sanitize` BOM on output file

**What goes wrong:** Using `Encoding.UTF8` to write the sanitized `.recrd` file emits a BOM, breaking the JSON spec and existing consumers.
**Why it happens:** `Encoding.UTF8` in .NET emits a BOM by default; `.recrd` files must be BOM-free.
**How to avoid:** `new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)` — same pattern as all existing `.recrd` writers.

[VERIFIED: STATE.md — "UTF8Encoding(false) for .recrd and .recrd.partial files"]

---

## Code Examples

### RootCommand + Global Options (2.0.5 pattern)

```csharp
// Source: System.CommandLine 2.0.5 — https://learn.microsoft.com/en-us/dotnet/standard/commandline/migration-guide-2.0.0-beta5

var verbosityOption = new Option<string>("--verbosity", "-v")
{
    Description = "quiet|normal|detailed|diagnostic",
    DefaultValueFactory = _ => "normal"
};
var logOutputOption = new Option<string>("--log-output")
{
    Description = "json for machine-parseable output"
};

var rootCommand = new RootCommand("recrd — record, compile, validate Robot Framework suites");
rootCommand.Options.Add(verbosityOption);
rootCommand.Options.Add(logOutputOption);

// Subcommand
var versionCommand = new Command("version", "Print version and runtime info");
versionCommand.SetAction((ParseResult result) =>
{
    var ver = typeof(Program).Assembly.GetName().Version;
    Console.WriteLine($"recrd {ver}");
    Console.WriteLine($".NET {Environment.Version}");
    return 0;
});
rootCommand.Subcommands.Add(versionCommand);

return await rootCommand.Parse(args).InvokeAsync();
```

### Session Socket — Check for Stale Socket

```csharp
// Source: [ASSUMED] — standard Unix socket stale-file detection pattern
private static async Task<bool> IsSessionActiveAsync(string socketPath)
{
    if (!File.Exists(socketPath)) return false;
    try
    {
        using var probe = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await probe.ConnectAsync(new UnixDomainSocketEndPoint(socketPath));
        return true; // live session
    }
    catch (SocketException)
    {
        File.Delete(socketPath); // stale — clean up
        return false;
    }
}
```

### JSON Console Logging Setup

```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information); // "normal" verbosity
    builder.AddJsonConsole(opts =>
    {
        opts.IncludeScopes = false;
        opts.TimestampFormat = "HH:mm:ss ";
    });
});
ILogger logger = loggerFactory.CreateLogger("recrd");
logger.LogInformation("Session started");
// Output: {"Timestamp":"10:42:01 ","EventId":0,"LogLevel":"Information","Category":"recrd","Message":"Session started"}
```

### sanitize — Strip Literal Values

```csharp
// Source: [ASSUMED] — based on CONTEXT.md D-08 + Recrd.Core.Ast types
// Deserialize with RecrdJsonContext (already handles polymorphic steps)
var session = JsonSerializer.Deserialize<Session>(json, RecrdJsonContext.Default.Session)!;

// Strip selector literal values — replace with variable placeholder or empty
Session sanitized = session with
{
    Steps = SanitizeSteps(session.Steps)
};

var outputPath = Path.Combine(
    Path.GetDirectoryName(inputPath)!,
    Path.GetFileNameWithoutExtension(inputPath) + ".sanitized.recrd");

await File.WriteAllTextAsync(outputPath,
    JsonSerializer.Serialize(sanitized, RecrdJsonContext.Default.Session),
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
```

### recrd stop Summary Format (D-09)

```
Session complete
  Events captured:  142
  Variables:        3
  Duration:         4m 22s
  Output:           session.recrd (48 KB)
  Partial file:     session.recrd.partial (deleted)
```

Implementation: The running `recrd start` process prints this to `Console.Out` immediately before the socket server loop exits (after receiving `{ "command": "stop" }`).

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `SetHandler` + `ICommandHandler` | `SetAction` with `ParseResult` + `CancellationToken` | System.CommandLine beta5 (2.0.0-beta5) | All tutorials before beta5 use wrong API |
| `Command.AddOption` / `AddCommand` | `Command.Options.Add` / `Subcommands.Add` | System.CommandLine beta5 | Mutable collection pattern; AddOption gone |
| `CommandLineBuilder` + `AddMiddleware` | `ParserConfiguration` + `InvocationConfiguration` | System.CommandLine beta5 | Builder removed entirely |
| `InvocationContext` in handler | `ParseResult` passed directly | System.CommandLine beta5 | Simpler, no context indirection |
| `IConsole` for test output capture | `InvocationConfiguration.Output = new StringWriter()` | System.CommandLine beta5 | Standard `TextWriter` instead of custom interface |

**Deprecated/outdated:**
- `System.CommandLine.Hosting` NuGet package: discontinued in 2.0.0 GA — do not add this dependency [VERIFIED: migration guide]
- `System.CommandLine.NamingConventionBinder` NuGet package: discontinued in 2.0.0 GA [VERIFIED: migration guide]
- `CommandLineConfiguration` (beta4): split into `ParserConfiguration` + `InvocationConfiguration` [VERIFIED: migration guide]

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `result.GetValue(verbosityOption)` works in subcommand `SetAction` when option is on `RootCommand` | Architecture Patterns, Pattern 1 | Verbosity/log-output would need to be passed differently; minor rework |
| A2 | Stale socket detection via failed `ConnectAsync` is the correct pattern | Common Pitfalls, Pitfall 3 | Could leave orphan sessions; requires OS-level socket state check alternative |
| A3 | DI container construction adds 30–100 ms startup overhead | Common Pitfalls, Pitfall 4 | May be fine in practice; avoid DI is still the safe approach for cold-start |
| A4 | `LogLevel.Information` is the correct minimum for "normal" verbosity mapping | Architecture Patterns, Pattern 3 | D-07 says "warnings + info" — if Warning is min, Info messages are dropped |
| A5 | `sanitize` strips `Selector.Values` + step literal data fields (exact field names) | Code Examples | Incorrect sanitization scope — needs cross-reference with Phase 2 AST types |

---

## Open Questions

1. **Exact sanitize field scope (CLI-05)**
   - What we know: D-08 says "strips all literal values, keeps variable placeholders and structure"
   - What's unclear: Which exact fields on `ActionStep`, `AssertionStep`, `Selector` constitute "literal values" — is it only `Selector.Values[].Value`, or also `ActionStep.Value` (typed text), `AssertionStep.ExpectedValue`?
   - Recommendation: Read `packages/Recrd.Core/Ast/` AST types before implementing `SanitizeCommand`. The planner should include a task to inspect AST types and document which fields are sanitized.

2. **`recrd recover` — explicit vs automatic (Claude's Discretion)**
   - What we know: `IRecorderEngine.RecoverAsync(string partialPath)` exists. CLI-06 says "reconstructs from latest partial."
   - What's unclear: Should `recrd recover` auto-select the newest `.recrd.partial` in the current directory, or require explicit `--partial-file` argument?
   - Recommendation: Auto-select the newest `.recrd.partial` in the current directory (glob `*.recrd.partial`, sort by LastWriteTime). If none found, exit with clear error. `--partial-file` as optional override.

3. **Coverage gate for `recrd-cli` in CI**
   - What we know: D-10 says ≥ 90% coverage on command handler logic. CI gates exist per-project.
   - What's unclear: `recrd-cli` is an `Exe` project, not a library. `coverlet.msbuild` coverage collection on console app projects can be tricky because the entry point is difficult to cover.
   - Recommendation: Add `Recrd.Cli.Tests` as a separate class library test project that references `recrd-cli` internals via `InternalsVisibleTo`. Set `PublishSingleFile=false` for test builds. Add coverage gate to `ci.yml` mirroring the existing per-project pattern.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| dotnet SDK | All build/test | ✓ | 10.0.104 | — |
| xunit | CLI test project | ✓ (in existing test projects) | 2.9.3 | — |
| Unix domain socket support | IPC (D-02) | ✓ (Linux native) | .NET 10 built-in | — |

[VERIFIED: `dotnet --version` = 10.0.104 on 2026-04-07; Unix domain sockets are .NET 6+ built-in]

**Windows note:** `UnixDomainSocketEndPoint` is supported on Windows 10 1803+ via .NET 6+. CLI-12 requires Windows support. No fallback needed — Windows supports UDS natively since 2018. [ASSUMED — verify with a Windows CI runner if Windows support is actively tested in Phase 8]

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Moq 4.20.72 |
| Config file | none — inherited from Directory.Build.props |
| Quick run command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Cli.Tests/ --no-build --filter "Category!=Integration"` |
| Full suite command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --filter "Category!=Integration"` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CLI-01 | `recrd start` flag parsing → correct RecorderOptions fields | unit | `dotnet test --filter "FullyQualifiedName~StartCommandTests"` | ❌ Wave 0 |
| CLI-02 | `recrd pause/resume/stop` socket client sends correct JSON | unit | `dotnet test --filter "FullyQualifiedName~SessionClientTests"` | ❌ Wave 0 |
| CLI-03 | `recrd compile` flags → CompilerOptions + ITestCompiler call | unit | `dotnet test --filter "FullyQualifiedName~CompileCommandTests"` | ❌ Wave 0 |
| CLI-04 | `recrd validate` exits 1 + error message on invalid AST | unit + subprocess | `dotnet test --filter "FullyQualifiedName~ValidateCommandTests"` | ❌ Wave 0 |
| CLI-05 | `recrd sanitize` produces .sanitized.recrd with no literals | unit | `dotnet test --filter "FullyQualifiedName~SanitizeCommandTests"` | ❌ Wave 0 |
| CLI-06 | `recrd recover` calls RecoverAsync with newest .recrd.partial | unit | `dotnet test --filter "FullyQualifiedName~RecoverCommandTests"` | ❌ Wave 0 |
| CLI-07 | `recrd version` prints version string, exits 0 | unit | `dotnet test --filter "FullyQualifiedName~VersionCommandTests"` | ❌ Wave 0 |
| CLI-08 | `recrd plugins list` scans ~/.recrd/plugins/, exits 0 | unit | `dotnet test --filter "FullyQualifiedName~PluginsCommandTests"` | ❌ Wave 0 |
| CLI-09 | `--verbosity quiet` suppresses info/warning log messages | unit | `dotnet test --filter "FullyQualifiedName~LoggingSetupTests"` | ❌ Wave 0 |
| CLI-10 | `--log-output json` output is valid JSON lines | unit | `dotnet test --filter "FullyQualifiedName~LoggingSetupTests"` | ❌ Wave 0 |
| CLI-11 | `recrd stop` summary printed: events, vars, duration, sizes | unit (socket + handler) | `dotnet test --filter "FullyQualifiedName~SessionSocketTests"` | ❌ Wave 0 |
| CLI-12 | `time recrd version` < 500 ms | manual / smoke | `time dotnet run --project apps/recrd-cli -- version` | manual |

### Sampling Rate

- **Per task commit:** `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Cli.Tests/ --no-build --filter "Category!=Integration"`
- **Per wave merge:** `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --filter "Category!=Integration"`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `tests/Recrd.Cli.Tests/Recrd.Cli.Tests.csproj` — new test project for CLI command handlers
- [ ] `tests/Recrd.Cli.Tests/PlaceholderTests.cs` — prevent xunit no-tests-found exit code 1
- [ ] `recrd.sln` — add `Recrd.Cli.Tests` project reference
- [ ] `ci.yml` — add coverage gate step for `Recrd.Cli.Tests` (90% line threshold)
- [ ] `apps/recrd-cli/recrd-cli.csproj` — add `System.CommandLine 2.0.5`, `Microsoft.Extensions.Logging 10.0.5`, `Microsoft.Extensions.Logging.Console 10.0.5`

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | No auth in CLI tool |
| V3 Session Management | no | Session = recording session, not auth session |
| V4 Access Control | no | Single-user local tool |
| V5 Input Validation | yes | Session file path arguments — validate file exists, extension is .recrd |
| V6 Cryptography | no | No crypto in CLI dispatch layer |

### Known Threat Patterns for CLI tools

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Path traversal via session file argument | Tampering | Canonicalize with `Path.GetFullPath`; verify extension is `.recrd` |
| Socket hijacking (malicious process creating session.sock first) | Spoofing | Check socket ownership on connect; or use process-owned temp directory. For Phase 8: document as known limitation, no mitigation required (single-user local tool). |
| ANSI escape injection in session content | Tampering | When printing session content to terminal, strip/escape control sequences. In Phase 8: simple strings only, no raw session content echoed. |

---

## Sources

### Primary (HIGH confidence)

- `NuGet registry (dotnet package search)` — System.CommandLine 2.0.5, MEL 10.0.5, MEL.Console 10.0.5 versions verified 2026-04-07
- [Microsoft Learn: System.CommandLine migration guide to 2.0.0-beta5+](https://learn.microsoft.com/en-us/dotnet/standard/commandline/migration-guide-2.0.0-beta5) — SetAction, mutable collections, InvocationConfiguration, removed APIs
- [Microsoft Learn: Console log formatting](https://learn.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter) — AddJsonConsole, JsonConsoleFormatterOptions
- `packages/Recrd.Core/Interfaces/IRecorderEngine.cs` — StartAsync, PauseAsync, ResumeAsync, StopAsync, RecoverAsync signatures
- `packages/Recrd.Core/Interfaces/RecorderOptions.cs` and `CompilerOptions.cs` — option fields for CLI-01, CLI-03
- `.planning/phases/08-cli-polish/08-CONTEXT.md` — locked decisions D-01 through D-10

### Secondary (MEDIUM confidence)

- [NuGet Gallery: System.CommandLine 2.0.5](https://www.nuget.org/packages/System.CommandLine) — stable GA, 74M downloads
- [Microsoft Learn: JsonConsoleFormatterOptions .NET 10](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.console.jsonconsoleformatteroptions?view=net-10.0-pp) — confirmed available in .NET 10

### Tertiary (LOW confidence)

- WebSearch result: System.CommandLine NativeAOT startup benchmarks (17 ms AOT vs 77 ms JIT) — not verified against official source, but consistent with Microsoft Learn migration guide

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — NuGet versions verified via registry query
- Architecture: HIGH for System.CommandLine patterns (official migration guide); MEDIUM for socket IPC pattern (built-in .NET, standard pattern)
- Pitfalls: HIGH for beta5 API changes (official docs); MEDIUM/LOW for socket cleanup and DI overhead (training knowledge + general .NET patterns)

**Research date:** 2026-04-07
**Valid until:** 2026-05-07 (System.CommandLine is now stable; MEL versions track .NET 10 release cycle)
