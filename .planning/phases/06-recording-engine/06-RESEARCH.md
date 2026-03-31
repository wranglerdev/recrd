# Phase 6: Recording Engine - Research

**Researched:** 2026-03-29
**Domain:** Playwright .NET 1.58.0, C# recording architecture, embedded resources, inspector UI
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Test Strategy**
- D-01: Use in-process Playwright fixtures — tests call `Page.SetContentAsync()` to inject fixture HTML inline. No external server, no TestContainers, no Docker. Fast, CI-friendly, runs on `ubuntu-latest` after `playwright.sh install`.
- D-02: The coverage gate (90% line coverage) applies to `Recrd.Recording` — same bar as Core, Data, Gherkin, Compilers.
- D-03: TDD red-green pattern carries forward: all tests committed failing on `tdd/phase-06` branch before any implementation; green phase commits implementation only after all tests pass.

**JS Agent to C# Communication**
- D-04: Use `Page.ExposeFunctionAsync` to expose a named C# async callback to the injected JavaScript agent as `window.__recrdCapture(event)`. No CDP plumbing, no WebSocket, no reconnection logic.
- D-05: The JavaScript recording agent lives as an embedded resource in `Recrd.Recording` — a `.js` file with build action `EmbeddedResource`, loaded at runtime via `GetManifestResourceStream()`. Injected via `Page.AddInitScriptAsync()` on every frame navigation.

**Inspector Side-Panel UI**
- D-06: The inspector side-panel is a single self-contained HTML file (inline CSS + vanilla JS) embedded in the `Recrd.Recording` assembly. Served to the secondary `BrowserContext` via `Page.RouteAsync`. No npm, no build step, no external dependencies.
- D-07: Live event stream updates are pushed by calling `Page.EvaluateAsync` on the inspector page from C# whenever a new `RecordedEvent` arrives on the channel.
- D-08: The right-click "Tag as Variable" context menu lives in the recording page, not the inspector. The injected JS agent intercepts `contextmenu` events, renders a custom overlay menu, and fires `window.__recrdCapture` with a tag event when the user selects "Tag as Variable".
- D-09: The assertion builder (pause mode) also lives in the recording page — the injected agent shows an overlay in pause mode when the user right-clicks, offering "Add Assertion" which inserts an `AssertionStep` via `window.__recrdCapture`.

**Partial Snapshot Design**
- D-10: `.recrd.partial` uses the same JSON format as `.recrd` — the current in-memory `Session` serialized via `RecrdJsonContext`. No separate format, no event-log WAL.
- D-11: On successful `recrd stop`, the `.recrd.partial` file is deleted. Partial files only exist for incomplete/crashed sessions.

### Claude's Discretion
- Exact name of the embedded JS agent file (e.g., `recording-agent.js`)
- How `IRecorderEngine.StartAsync`, `PauseAsync`, `ResumeAsync`, `StopAsync` are structured
- Whether the 30s snapshot timer uses `System.Threading.Timer` or a `PeriodicTimer` (.NET 6+)
- How duplicate variable names are detected and how the warning is surfaced to the user in the inspector overlay
- Exact event payload structure for each of the 7 DOM event types in `RecordedEvent.Payload`
- Whether to use `Page.RouteAsync` or a local Kestrel endpoint to serve the inspector HTML

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| REC-01 | Launch clean `BrowserContext` (incognito, zero cookies, zero localStorage) via Playwright .NET | `Browser.NewContextAsync()` with no `StorageState` creates isolated context; verified in official docs |
| REC-02 | Inject JavaScript recording agent into every frame via `Page.EvaluateAsync` on navigation | Use `BrowserContext.AddInitScriptAsync()` — fires in every frame on every navigation automatically |
| REC-03 | DOM events captured: click, input/change, select, hover (explicit only), navigation, file upload, drag-and-drop | Seven `addEventListener` calls in JS agent; `window.__recrdCapture` sends to C# |
| REC-04 | Each captured event wrapped as `RecordedEvent` and pushed to `Channel<RecordedEvent>` | `BrowserContext.ExposeFunctionAsync` callback writes to `IRecordingChannel.WriteAsync` |
| REC-05 | Selector extraction per element: data-testid > id > role-based > CSS class chain > XPath; minimum 3 strategies per element | JS agent reads element attributes at event time; `SelectorStrategy` enum in `Recrd.Core` maps to the 5 strategies |
| REC-06 | `recrd pause` freezes event capture, enables assertion mode | C# calls `Page.EvaluateAsync("window.__recrdSetMode('pause')")` on recording page; JS agent checks mode flag before capturing |
| REC-07 | `recrd resume` returns to recording mode | C# calls `Page.EvaluateAsync("window.__recrdSetMode('record')")` on recording page |
| REC-08 | `recrd stop` flushes AST to `.recrd` session file (JSON, UTF-8) | `RecrdJsonContext` serializes `Session`; `File.WriteAllTextAsync` with UTF-8 encoding |
| REC-09 | Incremental `.recrd.partial` snapshots written every 30 seconds during session | `PeriodicTimer` (.NET 6+) pattern with `WaitForNextTickAsync(cancellationToken)` |
| REC-10 | `recrd recover` reconstructs session from latest `.recrd.partial` snapshot | Plain `JsonSerializer.Deserialize<Session>` from `.recrd.partial` file using `RecrdJsonContext` |
| REC-11 | Inspector side-panel opens as secondary `BrowserContext` with `--app` flag | `BrowserType.LaunchAsync` with `Args: ["--app=..."]` for app-mode window; separate `BrowserContext.NewPageAsync()` |
| REC-12 | Inspector panel displays live event stream from `Channel<RecordedEvent>` | Background consumer task calls `Page.EvaluateAsync("window.__recrdPush({json})")` on inspector page |
| REC-13 | Right-click "Tag as Variable" replaces literal with named placeholder; duplicate names rejected with visible warning | JS agent `contextmenu` handler; tag-start/tag-confirm event flow through `window.__recrdCapture` |
| REC-14 | Right-click assertion builder (pause mode) inserts `AssertionStep` into AST | assert-start/assert-confirm event flow; C# creates `AssertionStep` and appends to in-memory session |
| REC-15 | Single-level OAuth popup handling — events captured, popup scope marker | `BrowserContext.Page` event or `Page.Popup` event catches new pages; init script + expose function re-attached |
</phase_requirements>

---

## Summary

`Recrd.Recording` is the most complex package in this monorepo. It coordinates three concurrent environments: the recording `BrowserContext` (Playwright-controlled Chrome), the inspector `BrowserContext` (a separate headed window), and the C# host process. All three communicate through a combination of Playwright's `ExposeFunctionAsync`, `EvaluateAsync`, and the existing `IRecordingChannel` channel pipeline.

The architecture is clean because every key decision is locked. The JS recording agent is an embedded resource injected via `BrowserContext.AddInitScriptAsync`, which fires automatically in every frame and child frame on navigation — this is the correct API for frame-safe event capture. `BrowserContext.ExposeFunctionAsync` (context-level, not page-level) is the right choice over `Page.ExposeFunctionAsync` because it propagates to all pages in the context, including popups (REC-15). The inspector panel is served via `Page.RouteAsync` + `RouteFulfillAsync`, avoiding any external web server dependency.

The biggest implementation complexity is the selector extraction algorithm. At event capture time in JavaScript, the agent must synchronously extract at least three ranked selectors from the DOM element. The `SelectorStrategy` enum already defines the five strategies (`DataTestId`, `Id`, `Role`, `Css`, `XPath`); the JS agent must implement each extraction rule and return the ranked list in `RecordedEvent.Selectors`.

**Primary recommendation:** Use `BrowserContext.ExposeFunctionAsync` (not `Page.ExposeFunctionAsync`) for the `window.__recrdCapture` callback — it covers the recording page and all popup pages through a single registration, which is essential for REC-15.

---

## Project Constraints (from CLAUDE.md)

- Prefix all `dotnet` commands with `DOTNET_SYSTEM_NET_DISABLEIPV6=1` — IPv6 hangs NuGet restore on Linux.
- Install Playwright browsers via `bash playwright.sh install` (not `.ps1` — PowerShell unavailable on Linux). Alternatively: `node ~/.nuget/packages/microsoft.playwright/1.58.0/.playwright/package/cli.js install chromium`.
- TDD mandate: all tests committed failing on `tdd/phase-06` branch before implementation.
- `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"` for standard test runs.
- `dotnet format --verify-no-changes` is the lint/code style gate.
- No circular dependencies: `Recrd.Recording` may reference `Recrd.Core` but not the reverse.
- `Recrd.Core` has zero `Recrd.*` dependencies (CI-enforced).

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.Playwright | 1.58.0 | Browser automation, JS injection, route interception | Already in Recrd.Recording.csproj; pinned version |
| Microsoft.Playwright.Xunit | 1.58.0 | xUnit base classes (PageTest) for Playwright tests | Same version as Microsoft.Playwright; provides isolated Page per test |
| System.Threading.Channels | .NET 10 BCL | `Channel<RecordedEvent>` pipeline | Already implemented in `RecordingChannel`; zero external dep |
| System.Text.Json | .NET 10 BCL | Session serialization via `RecrdJsonContext` | Already implemented; locked decision D-06 from Phase 2 |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| coverlet.msbuild | 6.0.4 | Coverage collection via `/p:CollectCoverage=true` | CI coverage gate — must match other test projects (quick task 260329-w2f switched all others to msbuild) |
| xunit | 2.9.3 | Test framework | Already in Recrd.Recording.Tests.csproj |
| Moq | 4.20.72 | Mock `IRecordingChannel`, `IRecorderEngine` in unit tests | Already in Recrd.Recording.Tests.csproj |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `BrowserContext.ExposeFunctionAsync` | `Page.ExposeFunctionAsync` | Context-level covers all pages including popups automatically; page-level requires re-registration on each new page |
| `BrowserContext.AddInitScriptAsync` | `Page.AddInitScriptAsync` per navigation | Context-level fires in all pages and child frames; page-level would miss iframes and popup pages |
| `Page.RouteAsync` to serve inspector HTML | Local Kestrel server | RouteAsync has zero extra dependencies; Kestrel adds HTTP server overhead for a single static file |
| `PeriodicTimer` (.NET 6+) | `System.Threading.Timer` | PeriodicTimer is async-native, never stacks ticks, single-consumer design; `System.Threading.Timer` fires on ThreadPool with potential overlap |

**Installation for test project:**

```bash
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet add tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj package Microsoft.Playwright.Xunit --version 1.58.0
```

Switch existing `coverlet.collector` to `coverlet.msbuild`:

```bash
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet remove tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj package coverlet.collector
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet add tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj package coverlet.msbuild --version 6.0.4
```

**Version verification (confirmed via `dotnet package search`):**
- `Microsoft.Playwright` 1.58.0 — already resolved in project
- `Microsoft.Playwright.Xunit` 1.58.0 — confirmed available on nuget.org
- `coverlet.msbuild` 6.0.4 — same version as other test projects post quick-task 260329-w2f

---

## Architecture Patterns

### Recommended Project Structure

```
packages/Recrd.Recording/
├── Interfaces/
│   └── IRecorderEngine.cs       # StartAsync, PauseAsync, ResumeAsync, StopAsync, RecoverAsync
├── Engine/
│   └── PlaywrightRecorderEngine.cs  # IRecorderEngine implementation
├── Selectors/
│   └── SelectorExtractor.cs     # Parses JS-supplied selector maps into Recrd.Core Selector records
├── Inspector/
│   └── InspectorServer.cs       # Owns secondary BrowserContext and inspector page lifecycle
├── Scripts/
│   └── recording-agent.js       # EmbeddedResource — injected into every recording frame
├── Panel/
│   └── inspector.html           # EmbeddedResource — self-contained inspector panel
└── Snapshots/
    └── PartialSnapshotWriter.cs  # PeriodicTimer-based snapshot writer

tests/Recrd.Recording.Tests/
├── BrowserContextTests.cs       # REC-01: clean launch, zero cookies/localStorage
├── EventCaptureTests.cs         # REC-02, REC-03, REC-04, REC-05: 7 event types + selector extraction
├── SessionLifecycleTests.cs     # REC-06, REC-07, REC-08: pause/resume/stop/flush
├── SnapshotRecoveryTests.cs     # REC-09, REC-10: partial snapshots and recover
├── InspectorPanelTests.cs       # REC-11, REC-12, REC-13, REC-14: inspector lifecycle and variable/assertion flows
└── PopupHandlingTests.cs        # REC-15: constrained popup capture
```

### Pattern 1: BrowserContext.ExposeFunctionAsync for JS-to-C# callback

**What:** Register `window.__recrdCapture` at the context level before any page navigation. The JS recording agent calls this global with a serialized event object.

**When to use:** This is the only approach that automatically covers all pages within the context, including popup pages (REC-15), without re-registration.

```csharp
// Source: https://playwright.dev/dotnet/docs/api/class-browsercontext#browser-context-expose-function
// Register BEFORE adding init script — order matters
await recordingContext.ExposeFunctionAsync("__recrdCapture", async (JsonElement eventData) =>
{
    var evt = BuildRecordedEvent(eventData);
    await _channel.WriteAsync(evt);
});

// Then add the init script that uses the global
await recordingContext.AddInitScriptAsync(script: _agentScript);
```

**Critical:** Register `ExposeFunctionAsync` before `AddInitScriptAsync`. The init script runs at page load; if the function isn't registered yet, the script will fail to find it on first execution.

### Pattern 2: BrowserContext.AddInitScriptAsync for frame-safe agent injection

**What:** The recording agent script is loaded from an embedded resource and passed as the `script` string argument (not `scriptPath`). Fires in every frame (including iframes) on every navigation.

**When to use:** This is the canonical pattern for injecting behavior before page scripts run.

```csharp
// Source: https://playwright.dev/dotnet/docs/api/class-browsercontext#browser-context-add-init-script
// Load from embedded resource
using var stream = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("Recrd.Recording.Scripts.recording-agent.js")!;
using var reader = new StreamReader(stream);
var agentScript = await reader.ReadToEndAsync();

await recordingContext.AddInitScriptAsync(script: agentScript);
```

**Embedded resource naming convention:** `{AssemblyName}.{FolderPath}.{FileName}` — a file at `Scripts/recording-agent.js` in `Recrd.Recording` becomes `Recrd.Recording.Scripts.recording-agent.js`.

**.csproj entry:**
```xml
<ItemGroup>
  <EmbeddedResource Include="Scripts\recording-agent.js" />
  <EmbeddedResource Include="Panel\inspector.html" />
</ItemGroup>
```

### Pattern 3: Page.RouteAsync to serve the inspector HTML

**What:** The inspector page navigates to a sentinel URL (e.g., `http://recrd-inspector.local/`); a `Page.RouteAsync` handler intercepts that URL and responds with the embedded HTML.

**When to use:** Preferred over Kestrel for a single embedded HTML file — no port conflicts, no server startup latency, works offline.

```csharp
// Source: https://playwright.dev/dotnet/docs/api/class-route#route-fulfill
await inspectorPage.RouteAsync("**/recrd-inspector*", async route =>
{
    var html = LoadEmbeddedResource("Recrd.Recording.Panel.inspector.html");
    await route.FulfillAsync(new RouteFulfillOptions
    {
        Status = 200,
        ContentType = "text/html; charset=utf-8",
        Body = html
    });
});
await inspectorPage.GotoAsync("http://recrd-inspector.local/");
```

### Pattern 4: PeriodicTimer for 30-second snapshots

**What:** `PeriodicTimer` (.NET 6+) is async-native and single-consumer. Unlike `System.Threading.Timer`, ticks do not stack up if the callback is slow — it simply delays the next tick.

**When to use:** All periodic background work in .NET 6+ applications.

```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/system.threading.periodictimer
private async Task RunSnapshotLoopAsync(CancellationToken ct)
{
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
    while (await timer.WaitForNextTickAsync(ct))
    {
        await WritePartialSnapshotAsync();
    }
}
```

### Pattern 5: Popup page capture (REC-15)

**What:** Listen to `BrowserContext.Page` event — fires for every new page in the context, including `window.open` popups. Because `ExposeFunctionAsync` was registered at the context level, the popup page already has `window.__recrdCapture` available.

**When to use:** All new page handling within a recording context.

```csharp
// Source: https://playwright.dev/dotnet/docs/pages
recordingContext.Page += async (_, newPage) =>
{
    // ExposeFunctionAsync is already context-level — no re-registration needed
    // The init script also fires automatically in the new page
    // Tag events from popup with popup scope marker
    await newPage.WaitForLoadStateAsync();
    // Track popup so we can close it when it navigates back
    RegisterPopupPage(newPage);
};
```

### Pattern 6: In-process xUnit test setup

**What:** Tests inherit from `Microsoft.Playwright.Xunit.PageTest` which provides a fresh `Page` per test. `Page.SetContentAsync` loads fixture HTML inline, no server needed.

**When to use:** All `Recrd.Recording.Tests` — locked decision D-01.

```csharp
using Microsoft.Playwright.Xunit;

public class EventCaptureTests : PageTest
{
    [Fact]
    public async Task Click_ProducesRecordedEventWithThreeSelectors()
    {
        await Page.SetContentAsync(@"
            <button data-testid=""submit"" id=""btn"">Click me</button>
        ");
        // wire channel, expose function, verify RecordedEvent
    }
}
```

**Note:** `PageTest` runs Chromium headless by default. The `PLAYWRIGHT_BROWSERS_PATH` env var or `ms-playwright` cache directory must contain Chromium. Verified: `~/.cache/ms-playwright/chromium-1208` and `chromium_headless_shell-1208` are installed on this machine.

### Anti-Patterns to Avoid

- **Registering `ExposeFunctionAsync` after `AddInitScriptAsync`:** The init script runs at page load. If the exposed function isn't registered yet, `window.__recrdCapture` will be `undefined` on first execution. Always register the function first.
- **Using `Page.ExposeFunctionAsync` instead of `BrowserContext.ExposeFunctionAsync`:** Page-level registration does not propagate to popup pages. This would break REC-15 silently.
- **Injecting the agent via `Page.EvaluateAsync` on navigation event:** The Playwright `Page.Navigate` event fires after scripts run. Use `AddInitScriptAsync` — it fires before page scripts.
- **`System.Threading.Timer` for snapshots:** Callback fires on a ThreadPool thread and can stack if the previous write is slow. `PeriodicTimer` prevents tick stacking.
- **Serving inspector HTML from a real URL:** Any CDN/external resource load in the inspector HTML violates the constraint in D-06 and may fail in offline/CI environments.
- **Registering `IRecorderEngine` in `Recrd.Core`:** The interface defines the contract but its concrete type must live in `Recrd.Recording` to avoid pulling Playwright's ~200 MB browsers into compile-only consumers. Consider: `IRecorderEngine` interface may need to be added to `Recrd.Core.Interfaces` (it currently does not exist there).

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Frame-safe script injection | Manual navigation event handler | `BrowserContext.AddInitScriptAsync` | Fires in iframes and child frames automatically; `Page.Navigate` event is too late |
| JS-to-C# event bridge | WebSocket, CDP message bus | `BrowserContext.ExposeFunctionAsync` | Playwright manages the wire protocol; reconnection is handled; available in all frames |
| HTML file serving for inspector | Local HTTP server (Kestrel/HttpListener) | `Page.RouteAsync` + `RouteFulfillAsync` | No port allocation, no startup latency, zero dependencies |
| Periodic snapshot timer | `while (true) { await Task.Delay(...) }` | `PeriodicTimer.WaitForNextTickAsync` | Tick skipping, async-native, cancellation-aware |
| Browser popup detection | CDP protocol events | `BrowserContext.Page` event | Playwright wraps the protocol; event fires for all popup types |

**Key insight:** Playwright .NET's context-level APIs (`ExposeFunctionAsync`, `AddInitScriptAsync`) do the frame/popup propagation work automatically. Any custom propagation logic is redundant and error-prone.

---

## Common Pitfalls

### Pitfall 1: `ExposeFunctionAsync` registration order

**What goes wrong:** `window.__recrdCapture is not a function` in the browser console on first page load.
**Why it happens:** `AddInitScriptAsync` script runs at document creation; if `ExposeFunctionAsync` hasn't been called yet in C#, the global doesn't exist when the script tries to use it.
**How to avoid:** Call `context.ExposeFunctionAsync("__recrdCapture", ...)` before `context.AddInitScriptAsync(...)`.
**Warning signs:** Events from the very first navigation are missing; subsequent navigations work.

### Pitfall 2: `contextmenu` event default prevention breaks browser menus

**What goes wrong:** Calling `event.preventDefault()` inside the JS agent's `contextmenu` handler suppresses the browser's native context menu everywhere on the page, not just for elements the user intends to tag.
**Why it happens:** `contextmenu` bubbles; if the agent always prevents default, the user can never right-click the browser's own UI elements (address bar etc.).
**How to avoid:** Only call `event.preventDefault()` inside the `contextmenu` handler when recording mode is active (not paused). Check the internal mode flag before suppressing.
**Warning signs:** User cannot dismiss custom overlay in non-recording mode.

### Pitfall 3: Selector extraction fails on elements inside shadow DOM

**What goes wrong:** `data-testid` query returns `null`; XPath doesn't cross shadow boundaries. 3 required selectors cannot be generated.
**Why it happens:** Shadow DOM elements are not accessible from document root selectors.
**How to avoid:** For this phase, scoped to the light DOM only (shadow DOM recording is v2+, per REQUIREMENTS.md out-of-scope). Fall back gracefully: if fewer than 3 strategies are available, return all available; the minimum-3 requirement applies to elements where at least 3 are extractable.
**Warning signs:** `Selectors.Count < 3` in tests using normal HTML fixtures.

### Pitfall 4: `BrowserContext.Page` event fires before the page is navigable

**What goes wrong:** Calling `newPage.ExposeFunctionAsync` immediately in the `Page` event handler fails because the page object is not yet ready.
**Why it happens:** The `Page` event fires at page object creation, before any URL has loaded.
**How to avoid:** Because `ExposeFunctionAsync` is registered at the context level (D-04 uses `BrowserContext.ExposeFunctionAsync`), there is no need to re-register on the popup page. The popup page inherits the function automatically. Only use the `Page` event to track popup references for scope tagging.
**Warning signs:** Duplicate function registration errors in the browser console.

### Pitfall 5: Inspector page `EvaluateAsync` called on closed page

**What goes wrong:** `TargetClosedException` thrown when the inspector browser window is closed by the user while the recording continues.
**Why it happens:** The background consumer task calls `Page.EvaluateAsync` in a loop; if the inspector page is closed, the call throws.
**How to avoid:** Wrap inspector `EvaluateAsync` calls in `try/catch` that catches `PlaywrightException`; log and continue. The recording should survive without the inspector.
**Warning signs:** Recording stops unexpectedly after user closes the inspector panel.

### Pitfall 6: `playwright.sh` is not generated on Linux

**What goes wrong:** CLAUDE.md says `bash playwright.sh install` but `playwright.sh` does not exist in the build output on Linux — only `playwright.ps1` is generated.
**Why it happens:** The MSBuild target in the Playwright NuGet only generates the `.ps1` on Linux/macOS (PowerShell cross-platform). The shell script must be invoked via Node directly.
**How to avoid:** Use `node ~/.nuget/packages/microsoft.playwright/1.58.0/.playwright/package/cli.js install chromium` on Linux. CLAUDE.md's `playwright.sh` instruction applies to CI (GitHub Actions ubuntu-latest where Playwright provides the shell script). On developer machines, use the Node CLI directly.
**Warning signs:** `bash: playwright.sh: No such file or directory` after build.

### Pitfall 7: `coverlet.collector` in Recording.Tests vs `coverlet.msbuild` in all other projects

**What goes wrong:** Coverage gate `/p:CollectCoverage=true` silently produces no coverage data; CI gate passes with 0% coverage or skips.
**Why it happens:** The quick task 260329-w2f switched Core, Data, Gherkin, Compilers tests from `coverlet.collector` to `coverlet.msbuild`. `Recrd.Recording.Tests` still has `coverlet.collector` (1.58.0 pin was before that fix).
**How to avoid:** Wave 0 task must swap `coverlet.collector` for `coverlet.msbuild` 6.0.4 in `Recrd.Recording.Tests.csproj`.
**Warning signs:** Coverage gate step for Recording shows 0% but does not fail.

### Pitfall 8: `IRecorderEngine` interface does not exist yet

**What goes wrong:** Phase 6 plan references `IRecorderEngine` as if it exists in `Recrd.Core.Interfaces`, but grep confirms it is absent.
**Why it happens:** Phase 2 defined the other four interfaces (`ITestCompiler`, `IDataProvider`, `IEventInterceptor`, `IAssertionProvider`) but not `IRecorderEngine` — it was explicitly listed as a Phase 6 concern in CONTEXT.md D-code area.
**How to avoid:** Wave 0 or Plan 1 must define `IRecorderEngine` in `Recrd.Core.Interfaces/IRecorderEngine.cs` with `StartAsync`, `PauseAsync`, `ResumeAsync`, `StopAsync`, `RecoverAsync`. Register it in `RecrdJsonContext` if any return types need serialization.
**Warning signs:** Compilation errors in `Recrd.Recording` referencing `IRecorderEngine`.

---

## Code Examples

### Loading an embedded JavaScript resource

```csharp
// Source: https://khalidabuhakmeh.com/how-to-use-embedded-resources-in-dotnet
// Resource name format: {AssemblyName}.{Folder}.{FileName}
var assembly = Assembly.GetExecutingAssembly();
using var stream = assembly.GetManifestResourceStream("Recrd.Recording.Scripts.recording-agent.js")
    ?? throw new InvalidOperationException("recording-agent.js not found as embedded resource");
using var reader = new StreamReader(stream, Encoding.UTF8);
var script = await reader.ReadToEndAsync();
```

### RouteFulfillAsync to serve embedded inspector HTML

```csharp
// Source: https://playwright.dev/dotnet/docs/api/class-route#route-fulfill
await inspectorPage.RouteAsync("**/*", async route =>
{
    var html = LoadEmbeddedHtml(); // reads from GetManifestResourceStream
    await route.FulfillAsync(new RouteFulfillOptions
    {
        Status = 200,
        ContentType = "text/html; charset=utf-8",
        Body = html
    });
});
await inspectorPage.GotoAsync("http://recrd-inspector.local/");
```

### PeriodicTimer snapshot loop pattern

```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/system.threading.periodictimer
private async Task RunSnapshotLoopAsync(string outputPath, CancellationToken ct)
{
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
    try
    {
        while (await timer.WaitForNextTickAsync(ct))
        {
            var snapshot = BuildCurrentSession();
            var json = JsonSerializer.Serialize(snapshot, RecrdJsonContext.Default.Session);
            await File.WriteAllTextAsync(outputPath + ".partial", json, Encoding.UTF8, ct);
        }
    }
    catch (OperationCanceledException)
    {
        // Normal cancellation on stop — swallow
    }
}
```

### BrowserContext popup tracking via Page event

```csharp
// Source: https://playwright.dev/dotnet/docs/pages
recordingContext.Page += (_, newPage) =>
{
    // ExposeFunctionAsync + AddInitScriptAsync are context-level — no action needed
    // Just tag subsequent events from this page as popup-scoped
    var popupId = Guid.NewGuid().ToString("N")[..8];
    _activePopups[newPage] = popupId;

    newPage.Close += (_, _) => _activePopups.TryRemove(newPage, out _);
};
```

### xUnit in-process Playwright test skeleton

```csharp
// Source: https://playwright.dev/dotnet/docs/test-runners
using Microsoft.Playwright.Xunit;

public class EventCaptureTests : PageTest
{
    [Theory]
    [MemberData(nameof(ClickFixtures))]
    public async Task Click_ProducesRecordedEventWithMinimumThreeSelectors(string html, string selector)
    {
        // D-01: no server, inline fixture HTML
        await Page.SetContentAsync(html);
        // Exercise recording logic, assert RecordedEvent.Selectors.Count >= 3
    }

    public static IEnumerable<object[]> ClickFixtures() => new[]
    {
        new object[] { @"<button data-testid=""submit"" id=""btn"">Go</button>", "[data-testid=submit]" },
        new object[] { @"<input id=""email"" type=""email"" />", "#email" },
    };
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `System.Threading.Timer` for periodic work | `PeriodicTimer` | .NET 6 (2021) | Async-native, no tick stacking, single-consumer design |
| `Page.ExposeFunctionAsync` per page | `BrowserContext.ExposeFunctionAsync` | Playwright early versions | One registration covers all pages and popups |
| `coverlet.collector` for msbuild coverage | `coverlet.msbuild` with `/p:CollectCoverage=true` | Applied in project via quick task 260329-w2f | Consistent per-project threshold enforcement |

**Deprecated/outdated in this project:**
- `coverlet.collector` in `Recrd.Recording.Tests.csproj` — all other test projects now use `coverlet.msbuild`; Recording.Tests was missed in quick task 260329-w2f.

---

## Open Questions

1. **`IRecorderEngine` interface surface in `Recrd.Core`**
   - What we know: The four other interfaces (`ITestCompiler`, `IDataProvider`, `IEventInterceptor`, `IAssertionProvider`) are in `Recrd.Core.Interfaces`. `IRecorderEngine` is not defined anywhere.
   - What's unclear: Does `IRecorderEngine` need to be in `Recrd.Core` (for CLI and integration test access without pulling Playwright) or can it live in `Recrd.Recording`?
   - Recommendation: Define in `Recrd.Core.Interfaces/IRecorderEngine.cs` (following the established pattern for other interfaces) so the CLI and integration tests can depend on it without a Playwright dependency. `Recrd.Recording` implements it.

2. **`RecrdJsonContext` registration for new types**
   - What we know: CONTEXT.md states "any new types emitted by this phase must be registered here."
   - What's unclear: The partial snapshot just serializes `Session` (already registered). The only new type consideration is the event payload dictionary key, which uses `string, string` (also already registered).
   - Recommendation: No new `JsonSerializable` attributes needed unless `IRecorderEngine` produces a new return type. Confirm during Plan 2 implementation.

3. **Selector extraction for `role`-based strategy**
   - What we know: `SelectorStrategy.Role` is in the enum. In JavaScript, ARIA role can be computed via `element.getAttribute('role')` or the accessibility tree.
   - What's unclear: For elements without an explicit `role` attribute, should the implicit ARIA role (inferred from tag name) be used? E.g., `<button>` has implicit role `button`.
   - Recommendation: Use explicit `role` attribute first; fall back to implicit role from tag name for standard HTML elements (`button`, `input`, `a`, `select`, `checkbox`). Document the fallback table in the JS agent.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 10 SDK | All dotnet commands | Yes | 10.0.104 | — |
| Node.js | Playwright CLI (`cli.js`) | Yes | v22.22.0 | — |
| Playwright Chromium (headless shell) | REC-01 tests, in-process PageTest | Yes | chromium_headless_shell-1208 (~145) | — |
| Google Chrome (system) | Optional headed sessions | Yes | 146.0.7680.164 | Not needed for tests |
| `playwright.sh` (shell script) | CLAUDE.md-documented install command | No (Linux only generates `.ps1`) | — | `node ~/.nuget/packages/microsoft.playwright/1.58.0/.playwright/package/cli.js install chromium` |
| `Microsoft.Playwright.Xunit` NuGet | xUnit PageTest base class | Available (not yet added to project) | 1.58.0 | — |
| `coverlet.msbuild` NuGet | CI coverage gate | Not yet in Recording.Tests | 6.0.4 | — |

**Missing dependencies with no fallback:**
- None — all required tools are available.

**Missing dependencies with fallback:**
- `playwright.sh` shell script: Use Node CLI directly. Wave 0 should document the correct install command for Linux developer machines.

**Action required:**
- `Microsoft.Playwright.Xunit` 1.58.0 must be added to `Recrd.Recording.Tests.csproj` (Wave 0)
- `coverlet.collector` must be replaced with `coverlet.msbuild` 6.0.4 in `Recrd.Recording.Tests.csproj` (Wave 0)
- CI `ci.yml` must gain a `Coverage gate — Recrd.Recording (90% line)` step (Wave 0 or Plan 1)

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.Playwright.Xunit 1.58.0 |
| Config file | none (uses default xUnit discovery) |
| Quick run command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Recording.Tests/ --no-build` |
| Full suite command | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Recording.Tests/ --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=90 /p:ThresholdType=line /p:ThresholdStat=minimum` |

### Phase Requirements to Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| REC-01 | `BrowserContext` launches with zero cookies and zero localStorage | unit (in-process Playwright) | `dotnet test ... --filter "FullyQualifiedName~BrowserContextTests"` | Wave 0 |
| REC-02 | JS agent injected into every frame via `AddInitScriptAsync` | unit (in-process Playwright) | `dotnet test ... --filter "FullyQualifiedName~EventCaptureTests"` | Wave 0 |
| REC-03 | All 7 event types produce `RecordedEvent` with correct `EventType` | unit (in-process Playwright, `[Theory]`) | `dotnet test ... --filter "FullyQualifiedName~EventCaptureTests"` | Wave 0 |
| REC-04 | Each event is pushed to `IRecordingChannel` | unit (in-process Playwright) | `dotnet test ... --filter "FullyQualifiedName~EventCaptureTests"` | Wave 0 |
| REC-05 | Each element produces at least 3 ranked selectors | unit (in-process Playwright) | `dotnet test ... --filter "FullyQualifiedName~EventCaptureTests"` | Wave 0 |
| REC-06 | `pause` mode stops event capture | unit (in-process Playwright) | `dotnet test ... --filter "FullyQualifiedName~SessionLifecycleTests"` | Wave 0 |
| REC-07 | `resume` mode restarts event capture | unit (in-process Playwright) | `dotnet test ... --filter "FullyQualifiedName~SessionLifecycleTests"` | Wave 0 |
| REC-08 | `stop` flushes Session to `.recrd` file, round-trips via `RecrdJsonContext` | unit (in-process Playwright + file I/O) | `dotnet test ... --filter "FullyQualifiedName~SessionLifecycleTests"` | Wave 0 |
| REC-09 | `.recrd.partial` written every 30s | unit (mock `PeriodicTimer` / fast timer) | `dotnet test ... --filter "FullyQualifiedName~SnapshotRecoveryTests"` | Wave 0 |
| REC-10 | `recover` deserializes `.recrd.partial` to `Session` | unit (file I/O) | `dotnet test ... --filter "FullyQualifiedName~SnapshotRecoveryTests"` | Wave 0 |
| REC-11 | Inspector panel opens as secondary BrowserContext | unit (in-process Playwright) | `dotnet test ... --filter "FullyQualifiedName~InspectorPanelTests"` | Wave 0 |
| REC-12 | Live event stream appears in inspector via `EvaluateAsync` push | unit (in-process Playwright) | `dotnet test ... --filter "FullyQualifiedName~InspectorPanelTests"` | Wave 0 |
| REC-13 | "Tag as Variable" flow replaces literal; duplicate name shows warning | unit (in-process Playwright) | `dotnet test ... --filter "FullyQualifiedName~InspectorPanelTests"` | Wave 0 |
| REC-14 | "Add Assertion" in pause mode inserts `AssertionStep` | unit (in-process Playwright) | `dotnet test ... --filter "FullyQualifiedName~InspectorPanelTests"` | Wave 0 |
| REC-15 | Popup page events captured with popup scope marker | unit (in-process Playwright) | `dotnet test ... --filter "FullyQualifiedName~PopupHandlingTests"` | Wave 0 |

### Sampling Rate

- **Per task commit:** `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Recording.Tests/ --no-build`
- **Per wave merge:** Full suite with coverage threshold (see Full suite command above)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `tests/Recrd.Recording.Tests/BrowserContextTests.cs` — covers REC-01
- [ ] `tests/Recrd.Recording.Tests/EventCaptureTests.cs` — covers REC-02, REC-03, REC-04, REC-05
- [ ] `tests/Recrd.Recording.Tests/SessionLifecycleTests.cs` — covers REC-06, REC-07, REC-08
- [ ] `tests/Recrd.Recording.Tests/SnapshotRecoveryTests.cs` — covers REC-09, REC-10
- [ ] `tests/Recrd.Recording.Tests/InspectorPanelTests.cs` — covers REC-11, REC-12, REC-13, REC-14
- [ ] `tests/Recrd.Recording.Tests/PopupHandlingTests.cs` — covers REC-15
- [ ] Delete `tests/Recrd.Recording.Tests/PlaceholderTests.cs` (replace, not append)
- [ ] Add `Microsoft.Playwright.Xunit` 1.58.0 to `Recrd.Recording.Tests.csproj`
- [ ] Replace `coverlet.collector` with `coverlet.msbuild` 6.0.4 in `Recrd.Recording.Tests.csproj`
- [ ] Add `Coverage gate — Recrd.Recording (90% line)` step to `.github/workflows/ci.yml`
- [ ] Define `IRecorderEngine` in `packages/Recrd.Core/Interfaces/IRecorderEngine.cs`

---

## Sources

### Primary (HIGH confidence)

- [Playwright .NET — BrowserContext.ExposeFunctionAsync](https://playwright.dev/dotnet/docs/api/class-browsercontext#browser-context-expose-function) — confirmed: context-level, all frames, all pages
- [Playwright .NET — BrowserContext.AddInitScriptAsync](https://playwright.dev/dotnet/docs/api/class-browsercontext#browser-context-add-init-script) — confirmed: fires before page scripts, covers child frames
- [Playwright .NET — Route.FulfillAsync](https://playwright.dev/dotnet/docs/api/class-route#route-fulfill) — confirmed: `Body`, `ContentType`, `Status` parameters for serving HTML
- [Playwright .NET — Browser.NewContextAsync](https://playwright.dev/dotnet/docs/api/class-browser#browser-new-context) — confirmed: no `StorageState` = zero cookies/localStorage
- [Playwright .NET — BrowserType.LaunchAsync](https://playwright.dev/dotnet/docs/api/class-browsertype#browser-type-launch) — confirmed: `Args` accepts `--app=...` for app-mode windows
- [Playwright .NET — Pages (popup events)](https://playwright.dev/dotnet/docs/pages) — confirmed: `BrowserContext.Page` event, `Page.Popup` event
- [Playwright .NET — Test Runners](https://playwright.dev/dotnet/docs/test-runners) — confirmed: `Microsoft.Playwright.Xunit` package, `PageTest` base class
- [PeriodicTimer docs — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.threading.periodictimer) — confirmed: `WaitForNextTickAsync(CancellationToken)`, .NET 6+
- [Embedded Resources in .NET — Khalid Abuhakmeh](https://khalidabuhakmeh.com/how-to-use-embedded-resources-in-dotnet) — confirmed: `GetManifestResourceStream`, resource naming convention
- Direct code inspection: `packages/Recrd.Core/`, `packages/Recrd.Recording/`, `tests/Recrd.Recording.Tests/` — HIGH confidence on all existing types and gaps

### Secondary (MEDIUM confidence)

- [PeriodicTimer .NET 6 introduction — Adrien Torris](https://adrientorris.github.io/dotnet/periodic-timer-new-async-timer-dotnet-6) — multiple sources confirm `PeriodicTimer` is the correct async timer pattern
- [BrowserStack — Playwright Selectors 2026](https://www.browserstack.com/guide/playwright-selectors) — verified the selector priority hierarchy aligns with `SelectorStrategy` enum

### Tertiary (LOW confidence)

- None — all critical claims verified via official docs or direct code inspection.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — Playwright 1.58.0 pinned in .csproj; Microsoft.Playwright.Xunit 1.58.0 confirmed on nuget.org; all BCL types are .NET 10 stable
- Architecture: HIGH — all locked decisions (D-01 through D-11) are specific and verified against official Playwright .NET API docs
- Pitfalls: HIGH for registration order and coverlet issues (verified by direct inspection); MEDIUM for shadow DOM and popup page timing (inferred from Playwright behavior, not specifically tested)

**Research date:** 2026-03-29
**Valid until:** 2026-06-29 (stable API surface; Playwright .NET 1.58.0 is pinned)
