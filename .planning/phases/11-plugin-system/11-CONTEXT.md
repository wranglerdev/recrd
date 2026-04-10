# Phase 11: Plugin System - Context

**Gathered:** 2026-04-09 (discuss mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

The CLI loads third-party `Recrd.Plugin.*` assemblies from `~/.recrd/plugins/` in isolated `AssemblyLoadContext` contexts, enforces major-version compatibility against `Recrd.Core`, and never lets a plugin crash the host process. `recrd plugins list` shows what's installed and whether each plugin loaded successfully. `recrd compile --target <plugin-target>` routes to plugin-provided compilers.

The Phase 8 CLI stubs for `plugins list` and `plugins install` are extended/replaced here. Plugin directory layout is also finalized here (Phase 8 used a placeholder flat scan). Recording-side plugin hooks (`IEventInterceptor`) are wired in if recording is running, but the recording engine itself is not changed.

</domain>

<decisions>
## Implementation Decisions

### Plugin Directory Layout

- **D-01:** Subdirectory-per-plugin: `~/.recrd/plugins/<PluginName>/` contains the main DLL, `.deps.json`, and any transitive dependency DLLs. `AssemblyDependencyResolver` is initialized with the main plugin DLL path — it discovers deps via the `.deps.json` in the same directory.
- **D-02:** Discovery scans subdirectories of `~/.recrd/plugins/`: for each subdirectory, find the `*.dll` whose name matches the directory name (e.g., `Recrd.Plugin.Excel/Recrd.Plugin.Excel.dll`). Ignore other DLLs in the subdirectory (they are transitive deps).
- **D-03:** The Phase 8 flat `Directory.GetFiles(pluginsDir, "*.dll")` scan in `PluginsCommand.cs` is replaced with this subdirectory scan. No migration needed — Phase 8 was a stub.

### `plugins install` Behavior

- **D-04:** `plugins install <pkg>` stays a helpful stub — it does NOT download from NuGet or shell out. Instead, it prints the expected directory structure and the commands the user should run to prepare a plugin for manual installation:
  ```
  To install Recrd.Plugin.MyPlugin:
    1. dotnet publish Recrd.Plugin.MyPlugin -c Release --no-self-contained
    2. Copy the publish output to ~/.recrd/plugins/Recrd.Plugin.MyPlugin/
  
  The directory must contain Recrd.Plugin.MyPlugin.dll and Recrd.Plugin.MyPlugin.deps.json.
  ```
  Exit code 0 (this is help text, not a failure). The existing exit-code-1 behavior from Phase 8 is changed to exit-code-0.

### `plugins list` Output

- **D-05:** `plugins list` loads each discovered plugin via `AssemblyLoadContext` and displays an informative table using the plain-text ANSI style from Phase 8 (no Spectre.Console):
  ```
  Installed plugins:
    Recrd.Plugin.Excel     v1.0.0  IDataProvider     ✓ loaded
    Recrd.Plugin.OldOne   v0.9.0  ITestCompiler     ✗ version mismatch (requires Core v2)
  ```
  Columns: name, version (from `AssemblyName.Version`), interfaces provided (comma-separated if multiple), load status.
- **D-06:** A plugin that fails version gating shows `✗ version mismatch (requires Core vN)` without loading its ALC. A plugin that passes version gating but throws during type discovery shows `✗ load error` with the exception message.

### AssemblyLoadContext Architecture

- **D-07:** One `RecrdPluginLoadContext : AssemblyLoadContext` per plugin subdirectory, created with `isCollectible: true`. `Load()` override: return `null` for `Recrd.Core` (host handles it — type unification), resolve all other assemblies via `AssemblyDependencyResolver`.
- **D-08:** Version gating uses `System.Reflection.Metadata` (`PEReader` + `MetadataReader`) to inspect the plugin DLL's `AssemblyReferences` *before* creating the ALC. Only plugins where the referenced `Recrd.Core` major version matches the host's `Recrd.Core` major version proceed to loading.
- **D-09:** A `PluginManager` class in `apps/recrd-cli/Plugins/` owns the plugin lifecycle: discovery → gating → loading → registration. Both `PluginsCommand` and `CompileCommand` ask `PluginManager` for compilers/providers rather than hardcoding.

### `recrd compile` Integration

- **D-10:** `CompileCommand` queries `PluginManager.GetCompilers()` to build the full set of available targets (built-in + plugin). If `--target <name>` matches a plugin compiler's `TargetName`, it is invoked. If the target is not found, the error message lists all available targets (built-in and loaded plugins).

### Exception Safety

- **D-11:** Plugin calls (`ITestCompiler.CompileAsync`, `IDataProvider.StreamAsync`) are wrapped in `try/catch(Exception)`. Caught exceptions are added to `CompilationResult.Warnings` as `[plugin:<name>] <message>` and the host exits zero. This satisfies PLUG-04.

### TDD Mandate (carries forward)

- **D-12:** All plugin system tests committed failing on `tdd/phase-11` branch before any implementation begins. Coverage gate ≥ 90% applies. Plugin tests live in `tests/Recrd.Cli.Tests/` (existing project) under `Plugins/` subdirectory — no new test project.

### Claude's Discretion

- Whether `PluginManager` is injected via DI or constructed inline in `Program.cs`
- Exact column widths and padding in the `plugins list` table
- Whether `isCollectible: true` ALCs are explicitly unloaded after `plugins list` or left to GC
- How `IEventInterceptor` plugins are chained during recording (Phase 6's recording engine calls them)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Plugin System Requirements
- `.planning/REQUIREMENTS.md` §PLUG-01 through PLUG-04 — The four plugin requirements with acceptance criteria
- `.planning/ROADMAP.md` §Phase 11 — Success criteria (4 items), plan breakdown

### Existing CLI Stub (to be replaced)
- `apps/recrd-cli/Commands/PluginsCommand.cs` — Current Phase 8 stub: flat *.dll scan, install stub exits code 1
- `tests/Recrd.Cli.Tests/Commands/PluginsCommandTests.cs` — Existing tests (some will need updating for new layout)

### CLI Patterns (Phase 8, carry forward)
- `.planning/phases/08-cli-polish/08-CONTEXT.md` — D-05/D-06/D-07: plain-text ANSI output style, verbosity model, logging setup
- `apps/recrd-cli/Output/CliOutput.cs` — Output helpers to reuse
- `apps/recrd-cli/Commands/CompileCommand.cs` — Needs plugin compiler discovery wired in (D-10)

### Research
- `.planning/phases/11-plugin-system/11-RESEARCH.md` — ALC patterns, AssemblyDependencyResolver, MetadataReader version gating, pitfalls (type unification, DLL locking)

### Core Interfaces (plugin contracts)
- `packages/Recrd.Core/` — `ITestCompiler`, `IDataProvider`, `IEventInterceptor`, `IAssertionProvider` — all plugin extension points

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `apps/recrd-cli/Output/CliOutput.cs` — `WriteInfo`, `WriteError`, `WriteWarning` helpers with ANSI color; use for plugin load status output
- `apps/recrd-cli/Commands/PluginsCommand.cs` — Scaffold exists; replace the flat scan and stub install handler
- `apps/recrd-cli/Commands/CompileCommand.cs` — Has hardcoded compiler lookup; needs `PluginManager` injected for dynamic target resolution
- `tests/Recrd.Cli.Tests/Commands/PluginsCommandTests.cs` — 4 existing tests; `PluginsList_WithAssemblies_PrintsPluginNames` uses a temp dir with fake DLLs — will need updating for subdirectory layout

### Established Patterns
- All commands use `System.CommandLine` with `SetAction` + `ParseResult` — new `PluginManager` plumbing follows same wiring
- `CompileCommand` passes `CompilerOptions` to `ITestCompiler.CompileAsync` — plugin compilers receive the same options with no special treatment
- `Directory.Build.props` has `IsPackable=false` on test projects — plugin test helpers (fake plugin DLLs) should be built as test fixtures, not packages
- `InternalsVisibleTo` via AssemblyAttribute in `.csproj` — use same pattern if `PluginManager` internals need exposure to tests

### Integration Points
- `Program.cs` — `PluginManager` constructed at startup, passed to both `PluginsCommand.Create(pluginManager)` and `CompileCommand.Create(pluginManager, ...)`
- `apps/recrd-cli/Commands/CompileCommand.cs` — Replace hardcoded `{ "robot-browser": ..., "robot-selenium": ... }` with `pluginManager.GetCompilers()` merged with built-ins
- `packages/Recrd.Core/` — `Recrd.Core.dll` must be shared from the host ALC (`return null` in `RecrdPluginLoadContext.Load()`) to avoid `InvalidCastException` on interface casts
- Test fixture strategy: build a minimal `TestPlugin.csproj` that references `Recrd.Core` and exports an `ITestCompiler`; publish it to a temp dir in test setup to get a real `.deps.json` for integration tests

</code_context>

<specifics>
## Specific Ideas

- `plugins list` table uses the exact format from the discussion: `  <name>     v<version>  <interfaces>     ✓ loaded` / `✗ version mismatch (requires Core vN)` — same plain-text ANSI style as Phase 8 output
- `plugins install` prints a two-step manual guide (dotnet publish → copy to `~/.recrd/plugins/<name>/`) and exits 0 — it's a help command, not a failure

</specifics>

<deferred>
## Deferred Ideas

- Actual NuGet download in `plugins install` — noted as future enhancement if users find manual copy too burdensome
- `IEventInterceptor` plugin chaining during live recording — architecture is wired (D-09), but full recording-side integration test is Phase 12 hardening territory
- Plugin signing / trusted source validation — security enhancement, not v1

</deferred>

---

*Phase: 11-plugin-system*
*Context gathered: 2026-04-09*
