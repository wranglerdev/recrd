# Phase 6: Recording Engine - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-29
**Phase:** 06-recording-engine
**Areas discussed:** Test strategy, JS agent ↔ C# communication, Inspector side-panel UI, Partial snapshot design

---

## Test Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| In-process Playwright fixtures | Tests use Page.SetContentAsync() to inject fixture HTML inline — no external server, no TestContainers. Fast, CI-friendly, follows TDD pattern. | ✓ |
| TestContainers + fixture web app | Spin up a static-file container per test run. Closer to real-world E2E but slower, harder to run locally, adds Docker dependency to CI. | |
| Mock-heavy unit tests | Abstract Playwright behind IPage/IBrowserContext, mock everything. Maximum isolation but very low confidence. | |

**User's choice:** In-process Playwright fixtures

---

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — gate Recrd.Recording too | Same 90% bar as Core/Data/Gherkin/Compilers. | ✓ |
| No — exclude from coverage gate | Only gate the packages explicitly listed in CI-02. | |

**User's choice:** Coverage gate applies to Recrd.Recording (90%)

---

## JS Agent ↔ C# Communication

| Option | Description | Selected |
|--------|-------------|----------|
| Page.ExposeFunctionAsync | Playwright exposes a named C# async callback to JS as window.__recrdCapture(event). Simple, synchronous call from JS into C#, no CDP plumbing. | ✓ |
| CDP channel | Use CDP Runtime.bindingCalled or custom CDP channel. More powerful but brittle across browser versions. | |
| WebSocket from JS to recording host | JS opens a WebSocket to a local server. Adds port management and reconnection logic. | |

**User's choice:** Page.ExposeFunctionAsync

---

| Option | Description | Selected |
|--------|-------------|----------|
| Embedded resource in Recrd.Recording | .js file with EmbeddedResource build action, loaded via GetManifestResourceStream(). Clean separation, version-controlled. | ✓ |
| Inline C# string constant | JS as a string literal in C#. Unworkable as the agent grows; no IDE syntax highlighting. | |

**User's choice:** Embedded resource

---

## Inspector Side-Panel UI

| Option | Description | Selected |
|--------|-------------|----------|
| Static HTML embedded resource | Single self-contained HTML file (inline CSS + vanilla JS) embedded in the assembly. Served via Page.RouteAsync. No npm, no build step. | ✓ |
| Minimal SPA with vanilla JS modules | HTML + small vanilla JS module from embedded resources. More structured but still no framework. | |
| React/Lit mini-app with build step | Proper frontend build (vite/esbuild). Adds npm and build toolchain to the .NET project. | |

**User's choice:** Static HTML embedded resource

---

| Option | Description | Selected |
|--------|-------------|----------|
| In the recording page itself | Injected JS agent intercepts contextmenu, shows custom overlay, calls window.__recrdCapture with tag event. No cross-window messaging. | ✓ |
| In the inspector side-panel | User selects an event row in the inspector and clicks a button. Requires cross-window communication. | |

**User's choice:** Right-click context menu in the recording page

---

## Partial Snapshot Design

| Option | Description | Selected |
|--------|-------------|----------|
| Same JSON format as .recrd | Serialize current in-memory Session to .recrd.partial every 30s. recrd recover just deserializes it. | ✓ |
| Append-only WAL (event log) | Each RecordedEvent appended as a JSON line. Recovery replays all events. More complex. | |

**User's choice:** Same JSON format as .recrd

---

| Option | Description | Selected |
|--------|-------------|----------|
| Delete on successful stop | Clean state: partial only exists for incomplete sessions. Recovery path unambiguous. | ✓ |
| Keep alongside .recrd | Preserves both final session and last snapshot. Redundant after successful stop. | |

**User's choice:** Delete .recrd.partial on successful stop

---

## Claude's Discretion

- Exact name of the embedded JS agent file
- Structure of IRecorderEngine async surface (StartAsync, PauseAsync, ResumeAsync, StopAsync)
- 30s snapshot timer implementation (System.Threading.Timer vs PeriodicTimer)
- Duplicate variable name detection and warning UX in inspector overlay
- Exact Payload structure per DOM event type
- Whether Page.RouteAsync or local Kestrel serves the inspector HTML

## Deferred Ideas

None — discussion stayed within phase scope.
