# Phase 6: Recording Engine - Context

**Gathered:** 2026-03-29 (discuss mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

`Recrd.Recording` implements `IRecorderEngine` using Playwright .NET to capture live browser interactions into the `Channel<RecordedEvent>` pipeline. Scope: BrowserContext launch, JavaScript agent injection, 7 DOM event types, ranked selector extraction (min 3 strategies per element), pause/resume/stop, `.recrd` session flush, `.recrd.partial` snapshots every 30s, `recrd recover`, and an inspector side-panel with live event stream, variable tagging, and assertion builder.

CLI wiring, compiler integration, and full multi-tab support are out of scope for this phase.

</domain>

<decisions>
## Implementation Decisions

### Test Strategy

- **D-01:** Use **in-process Playwright fixtures** — tests call `Page.SetContentAsync()` to inject fixture HTML inline. No external server, no TestContainers, no Docker. Fast, CI-friendly, runs on `ubuntu-latest` after `playwright.sh install`.
- **D-02:** The **coverage gate (90% line coverage) applies to `Recrd.Recording`** — same bar as Core, Data, Gherkin, Compilers. Even though CI-02 doesn't explicitly list Recording, parity is enforced.
- **D-03:** TDD red-green pattern carries forward: all tests committed failing on `tdd/phase-06` branch before any implementation; green phase commits implementation only after all tests pass.

### JS Agent ↔ C# Communication

- **D-04:** Use **`Page.ExposeFunctionAsync`** to expose a named C# async callback to the injected JavaScript agent as `window.__recrdCapture(event)`. The injected script calls this global for every captured DOM event. No CDP plumbing, no WebSocket, no reconnection logic.
- **D-05:** The **JavaScript recording agent lives as an embedded resource** in `Recrd.Recording` — a `.js` file with build action `EmbeddedResource`, loaded at runtime via `GetManifestResourceStream()`. Injected via `Page.AddInitScriptAsync()` on every frame navigation.

### Inspector Side-Panel UI

- **D-06:** The inspector side-panel is a **single self-contained HTML file** (inline CSS + vanilla JS) embedded in the `Recrd.Recording` assembly. Served to the secondary `BrowserContext` via `Page.RouteAsync`. No npm, no build step, no external dependencies.
- **D-07:** Live event stream updates are pushed by calling **`Page.EvaluateAsync`** on the inspector page from C# whenever a new `RecordedEvent` arrives on the channel.
- **D-08:** The **right-click "Tag as Variable" context menu lives in the recording page**, not the inspector. The injected JS agent intercepts `contextmenu` events, renders a custom overlay menu, and fires `window.__recrdCapture` with a tag event when the user selects "Tag as Variable". No cross-window messaging required.
- **D-09:** The **assertion builder (pause mode)** also lives in the recording page — the injected agent shows an overlay in pause mode when the user right-clicks, offering "Add Assertion" which inserts an `AssertionStep` via `window.__recrdCapture`.

### Partial Snapshot Design

- **D-10:** `.recrd.partial` uses the **same JSON format as `.recrd`** — the current in-memory `Session` serialized via `RecrdJsonContext`. No separate format, no event-log WAL. `recrd recover` just deserializes the partial file and returns the `Session`.
- **D-11:** On **successful `recrd stop`**, the `.recrd.partial` file is **deleted**. Partial files only exist for incomplete/crashed sessions. `recrd recover` has an unambiguous signal: if a `.recrd.partial` exists, there is a session to recover.

### Claude's Discretion

- Exact name of the embedded JS agent file (e.g., `recording-agent.js`)
- How `IRecorderEngine.StartAsync`, `PauseAsync`, `ResumeAsync`, `StopAsync` are structured
- Whether the 30s snapshot timer uses `System.Threading.Timer` or a `PeriodicTimer` (`dotnet 6+`)
- How duplicate variable names are detected and how the warning is surfaced to the user in the inspector overlay
- Exact event payload structure for each of the 7 DOM event types in `RecordedEvent.Payload`
- Whether to use `Page.RouteAsync` or a local Kestrel endpoint to serve the inspector HTML

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements

- `.planning/REQUIREMENTS.md` §Recording Engine (REC-01–REC-15) — full spec: BrowserContext launch, JS injection, 7 event types, selector ranking, pause/resume/stop, session flush, partial snapshots, recovery, inspector panel, variable tagging, assertion builder, popup handling

### Project Guidelines

- `CLAUDE.md` — TDD mandate (red-green per phase, `tdd/phase-*` branch prefix for CI-06), `DOTNET_SYSTEM_NET_DISABLEIPV6=1` prefix requirement, Playwright browser install via `playwright.sh install`

### Phase 2 Decisions (binding)

- `.planning/phases/02-core-ast-types-interfaces/02-CONTEXT.md` §D-09–D-10 — `IRecordingChannel` interface, bounded `Channel<RecordedEvent>` wrapper, `WriteAsync`/`ReadAllAsync`/`Complete`/`Cancel` surface
- `.planning/phases/02-core-ast-types-interfaces/02-CONTEXT.md` §D-06–D-08 — System.Text.Json + `RecrdJsonContext` source-generated serialization; no Newtonsoft.Json

### Phase 5 Decisions (binding)

- `.planning/phases/05-ci-pipeline/05-CONTEXT.md` §D-01–D-03 — Coverage gate pattern (per-project `dotnet test --threshold 90`); `Recrd.Recording` now added to gated projects

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- `packages/Recrd.Recording/Recrd.Recording.csproj` — already references `Recrd.Core` and `Microsoft.Playwright 1.58.0`; clean slate (`Placeholder.cs` only)
- `tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj` — xUnit + Moq + coverlet already configured; `PlaceholderTests.cs` is the only file
- `Recrd.Core.Pipeline.IRecordingChannel` + `RecordingChannel` — bounded channel wrapper already implemented; `Recrd.Recording` writes events via `WriteAsync`
- `Recrd.Core.Ast.*` — `RecordedEvent`, `Session`, `ActionStep`, `AssertionStep`, `GroupStep`, `Selector` all defined and JSON-serializable
- `RecrdJsonContext` — source-generated `JsonSerializerContext`; any new types emitted by this phase (e.g., partial snapshot envelope) must be registered here

### Established Patterns

- Behavior-suite test organization: one file per concern, not per type (established Phases 2–5)
- `[Theory]` + `[MemberData]` for covering multiple event-type variants (established Phase 2)
- `IsPackable=false` on all test projects — already set
- `PlaceholderTests.cs` pattern — delete and replace with real test suites

### Integration Points

- `Recrd.Recording.Tests` must install Playwright browsers before running: `bash playwright.sh install` (see CLAUDE.md)
- `Recrd.Integration.Tests` references `Recrd.Recording` — E2E round-trip tests in Phase 7 depend on this phase's `IRecorderEngine`
- Phase 7 (`Recrd.Compilers`) will call `ITestCompiler.CompileAsync(session, ...)` with a `Session` produced by this phase — the `.recrd` flush format must be stable

</code_context>

<specifics>
## Specific Ideas

- `window.__recrdCapture(event)` as the JS-to-C# callback name — explicit, unlikely to collide with page globals
- Right-click overlay menu lives in the recording page, injected by the JS agent — no cross-window messaging complexity
- Partial file = full Session snapshot (not delta); recovery is a plain JSON deserialize

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 06-recording-engine*
*Context gathered: 2026-03-29*
