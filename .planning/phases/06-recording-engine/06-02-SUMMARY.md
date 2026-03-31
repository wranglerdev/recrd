---
phase: 06-recording-engine
plan: 02
subsystem: recording
tags: [playwright, dotnet, javascript, embedded-resource, channel, selector, dom-events]

# Dependency graph
requires:
  - phase: 06-01
    provides: "IRecorderEngine interface, RecorderOptions, test scaffold, Recrd.Recording.csproj with EmbeddedResource entries"
  - phase: 02-core-ast-types-interfaces
    provides: "RecordedEvent, Session, Selector, SelectorStrategy, IRecordingChannel, RecordingChannel, ActionStep, ActionType, Variable"
provides:
  - "PlaywrightRecorderEngine: IRecorderEngine implementation with clean BrowserContext, ExposeFunctionAsync, AddInitScriptAsync"
  - "recording-agent.js: embedded JS agent with 7 DOM event listeners, ranked selector extraction, right-click overlay menu"
  - "SelectorExtractor: C# parser converting JS selector JSON to Recrd.Core.Ast.Selector records"
  - "RecordedEventBuilder: maps JSON payload from __recrdCapture to RecordedEvent (7 types) + special event parsing"
  - "SessionBuilder: accumulates steps/variables, converts RecordedEvent to ActionStep with priority selector"
affects: [06-03, 06-04, 06-05, 07-compilers, integration-tests]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ExposeFunctionAsync before AddInitScriptAsync — critical ordering (RESEARCH.md Pitfall 1)"
    - "Embedded JS resource loaded via Assembly.GetManifestResourceStream at runtime"
    - "BrowserContext.ExposeFunctionAsync (not Page-level) for popup coverage"
    - "IIFE-wrapped JS agent with __recrdMode state and __recrdSetMode control"
    - "Selector priority chain: DataTestId > Id > Role > Css > XPath in both JS and C#"

key-files:
  created:
    - packages/Recrd.Recording/Scripts/recording-agent.js
    - packages/Recrd.Recording/Engine/PlaywrightRecorderEngine.cs
    - packages/Recrd.Recording/Engine/RecordedEventBuilder.cs
    - packages/Recrd.Recording/Engine/SessionBuilder.cs
    - packages/Recrd.Recording/Selectors/SelectorExtractor.cs
  modified: []

key-decisions:
  - "Hover events are opt-in via data-recrd-hover='true' attribute to avoid noise from mouse movement"
  - "Navigation events use window.addEventListener (not document) since beforeunload/popstate fire on window"
  - "CS0414 suppressed with pragma for _isPaused field — needed for Plan 03/04 lifecycle"
  - "HandleSpecialEvent is a stub logging to Debug output — Plan 04 implements full tag/assert flow"
  - "RecordedEventBuilder.ParseSpecialEvent does not dispose JsonDocument when returning — caller owns the clone"

patterns-established:
  - "SelectorExtractor: always ensure Css is in the fallback strategies list for guaranteed minimum coverage"
  - "RecordedEventBuilder: two-phase parsing — Build() for regular events, ParseSpecialEvent() for tag/assert"
  - "SessionBuilder.ConvertToStep: Hover maps to ActionType.Click (recorded as-is, no separate hover action)"

requirements-completed: [REC-01, REC-02, REC-03, REC-04, REC-05]

# Metrics
duration: 25min
completed: 2026-03-31
---

# Phase 06 Plan 02: Core Recording Engine Summary

**Playwright BrowserContext engine with JS agent injecting 7 DOM event listeners (click/input/select/hover/navigation/fileupload/dragdrop), ranked selector extraction (DataTestId > Id > Role > Css > XPath), and Channel pipeline integration via ExposeFunctionAsync**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-03-31T00:30:00Z
- **Completed:** 2026-03-31T00:55:00Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- Created `recording-agent.js` IIFE with 7 DOM event listeners, ranked selector extraction with XPath builder, right-click overlay menu for Tag as Variable / Add Assertion, and mode control (`__recrdSetMode`)
- Implemented `PlaywrightRecorderEngine` with correct `ExposeFunctionAsync`-before-`AddInitScriptAsync` ordering, embedded script loading via `GetManifestResourceStream`, clean `BrowserContext` launch (zero cookies/localStorage), and `StopAsync` that serializes via `RecrdJsonContext`
- Created `SelectorExtractor`, `RecordedEventBuilder`, and `SessionBuilder` as composable helpers for the event pipeline
- `dotnet build packages/Recrd.Recording` exits 0 with 0 warnings

## Task Commits

1. **Task 1: JS recording agent** - `35e7fc8` (feat)
2. **Task 2: PlaywrightRecorderEngine, SelectorExtractor, RecordedEventBuilder, SessionBuilder** - `141fda7` (feat)

## Files Created/Modified

- `packages/Recrd.Recording/Scripts/recording-agent.js` — IIFE JS agent: 7 event listeners, __extractSelectors (5 strategies), right-click overlay menu, __recrdSetMode
- `packages/Recrd.Recording/Engine/PlaywrightRecorderEngine.cs` — IRecorderEngine implementation: BrowserContext launch, ExposeFunctionAsync callback, embedded script loading, PauseAsync/ResumeAsync/StopAsync
- `packages/Recrd.Recording/Engine/RecordedEventBuilder.cs` — JSON payload parser: maps 7 type strings to RecordedEventType enum, separates special events
- `packages/Recrd.Recording/Engine/SessionBuilder.cs` — In-memory session accumulator: ConvertToStep maps RecordedEventType to ActionType
- `packages/Recrd.Recording/Selectors/SelectorExtractor.cs` — Parses JS {strategies, values} JSON to Recrd.Core.Ast.Selector records

## Decisions Made

- Hover events are opt-in (`data-recrd-hover="true"`) to prevent mouseover noise; recorded as-is in SessionBuilder (ActionType.Click)
- `_isPaused` field suppressed with `#pragma warning disable CS0414` — Plan 03 will use it for snapshot timer logic
- `HandleSpecialEvent` is a stub that logs to `Debug.WriteLine` — Plan 04 (Inspector) implements the full tag/assert confirmation flows
- `ParseSpecialEvent` returns a cloned `JsonElement` and does not dispose the `JsonDocument` — caller owns the returned clone

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

- `CS0414` warning treated as error for `_isPaused` field (assigned but never read in this plan). Fixed with `#pragma warning disable CS0414` with comment explaining Plan 03/04 will wire it. This is correct behavior since the field is intentionally a stub.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- `PlaywrightRecorderEngine` is ready for Plan 03 (session lifecycle: 30s snapshots, `.recrd.partial`, `RecoverAsync`)
- `SessionBuilder` and event pipeline are ready for Plan 04 (inspector UI wiring)
- `recording-agent.js` right-click overlay sends `TagStart`/`AssertStart` events — Plan 04 implements confirmation flows
- Build is clean (0 warnings, 0 errors)

## Known Stubs

- `PlaywrightRecorderEngine.HandleSpecialEvent`: logs to `Debug.WriteLine` only — Plan 04 implements full tag/assert confirmation flow
- `PlaywrightRecorderEngine.RecoverAsync`: throws `NotImplementedException` — Plan 03 implements session recovery from `.recrd.partial`
- `PlaywrightRecorderEngine._snapshotCts`: declared but not started — Plan 03 starts the `PeriodicTimer` snapshot loop

---
*Phase: 06-recording-engine*
*Completed: 2026-03-31*

## Self-Check: PASSED

All files confirmed present, both commits confirmed in git log.
