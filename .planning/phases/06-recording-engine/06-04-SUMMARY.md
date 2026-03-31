---
phase: 06-recording-engine
plan: 04
subsystem: ui
tags: [playwright, inspector, browser-context, variable-tagging, assertion-builder, embedded-resource]

# Dependency graph
requires:
  - phase: 06-recording-engine/06-03
    provides: PlaywrightRecorderEngine with session lifecycle, PartialSnapshotWriter, RecoverAsync
  - phase: 06-recording-engine/06-02
    provides: RecordedEventBuilder.ParseSpecialEvent, HandleCapturedEventAsync stub

provides:
  - Inspector side-panel HTML (Panel/inspector.html) served as embedded resource via RouteAsync
  - InspectorServer.cs: secondary BrowserContext lifecycle, ExposeFunctionAsync, PushEventAsync, dialog control
  - Live event stream: every RecordedEvent pushed to inspector via window.__recrdPush
  - Variable tagging flow: TagStart → ShowTagDialogAsync → TagConfirm callback → uniqueness check → AddVariable
  - Assertion builder flow: AssertStart → ShowAssertDialogAsync → AssertConfirm callback → AssertionStep into AST
  - State badge sync: PauseAsync/ResumeAsync update inspector via SetStateAsync

affects:
  - 06-recording-engine/06-05 (multi-tab support needs inspector context awareness)
  - 07-robot-browser-compiler (assertion types must match compiler expectations)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Secondary BrowserContext for inspector with --app flag (app mode, no browser chrome)
    - RouteAsync + RouteFulfillAsync serving embedded HTML from Assembly.GetManifestResourceStream
    - ExposeFunctionAsync(__recrdInspectorCallback) on inspector context for bidirectional JS→C# callbacks
    - PlaywrightException swallowing in PushEventAsync/SetStateAsync (inspector closed by user is non-fatal)
    - Defense-in-depth server-side regex validation despite client-side validation in inspector JS

key-files:
  created:
    - packages/Recrd.Recording/Panel/inspector.html
    - packages/Recrd.Recording/Inspector/InspectorServer.cs
  modified:
    - packages/Recrd.Recording/Engine/PlaywrightRecorderEngine.cs

key-decisions:
  - "inspector.html uses window.__recrdInspectorCallback (not __recrdCapture) because the inspector is in a separate BrowserContext — ExposeFunctionAsync is context-scoped, not shared"
  - "AssertConfirm selector stored as display string in dialog._assertData; converted to minimal Css Selector on C# side since full selector JSON is unavailable after user interaction"
  - "ExtractDisplaySelector extracts best human-readable selector value from ranked strategies for dialog display, skipping wildcard fallback"
  - "HandleSpecialEventAsync routes TagStart/AssertStart to inspector; TagConfirm/AssertConfirm come back via HandleInspectorCallbackAsync (not through recording context)"

patterns-established:
  - "InspectorServer pattern: IAsyncDisposable wrapper around secondary IBrowser/IBrowserContext/IPage lifecycle"
  - "Inspector dialog communication: C# calls EvaluateAsync to open dialog, JS calls ExposeFunctionAsync binding to respond"
  - "Swallow PlaywrightException on any inspector EvaluateAsync and set _isOpen=false (closed inspector is non-fatal)"

requirements-completed: [REC-11, REC-12, REC-13, REC-14]

# Metrics
duration: 20min
completed: 2026-03-31
---

# Phase 06 Plan 04: Inspector Panel Summary

**Self-contained inspector side-panel with live event stream, variable tagging flow, and assertion builder wired into PlaywrightRecorderEngine via secondary BrowserContext and ExposeFunctionAsync bidirectional callbacks**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-03-31T22:05:00Z
- **Completed:** 2026-03-31T22:25:00Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- Inspector HTML panel (Panel/inspector.html) fully self-contained: dark theme, live event list (MAX_EVENTS=500), state badge, variable chips, tag dialog, assert dialog — no external resource loads
- InspectorServer.cs: secondary Chromium BrowserContext in --app mode, serves inspector.html via RouteAsync, registers `__recrdInspectorCallback` via ExposeFunctionAsync, exposes PushEventAsync/SetStateAsync/ShowTagDialogAsync/ShowAssertDialogAsync
- PlaywrightRecorderEngine fully wired: live PushEventAsync on every RecordedEvent, TagStart/AssertStart routed to inspector dialogs, TagConfirm/AssertConfirm callbacks handled with validation and AST mutation
- Variable tagging: name validated server-side against `^[a-z][a-z0-9_]{0,63}$`, duplicate checked via `SessionBuilder.HasVariable`, Variable added to session on success
- Assertion builder: AssertConfirm creates `AssertionStep` with `AssertionType` enum parse and minimal Css selector, added to session

## Task Commits

Each task was committed atomically:

1. **Task 1: Create inspector.html embedded resource with full UI** - `a1916b5` (feat)
2. **Task 2: Create InspectorServer and wire tag/assert flows into PlaywrightRecorderEngine** - `6e04019` (feat)

## Files Created/Modified

- `packages/Recrd.Recording/Panel/inspector.html` - Self-contained inspector panel HTML with inline CSS/JS, all required window.__recrd* functions, dialogs, and dark theme
- `packages/Recrd.Recording/Inspector/InspectorServer.cs` - Secondary BrowserContext lifecycle, RouteAsync HTML serving, ExposeFunctionAsync binding, all dialog control methods
- `packages/Recrd.Recording/Engine/PlaywrightRecorderEngine.cs` - Replaced HandleSpecialEvent stub with HandleSpecialEventAsync + HandleInspectorCallbackAsync; added PushEventAsync wiring; added ExtractDisplaySelector helper; added inspector using import

## Decisions Made

- `window.__recrdInspectorCallback` registered on the inspector BrowserContext (not `__recrdCapture`) because ExposeFunctionAsync is context-scoped — the inspector's context doesn't inherit the recording context's bindings
- `AssertConfirm` sends `selector` as a display string (not full JSON object) because `dialog._assertData.selector` is the display string; C# converts it to a minimal `Css` strategy Selector
- `ExtractDisplaySelector` skips the `*` wildcard CSS fallback to avoid showing a meaningless selector in the dialog title

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added defense-in-depth server-side name validation in HandleInspectorCallbackAsync**
- **Found during:** Task 2 (HandleInspectorCallbackAsync implementation)
- **Issue:** Plan specified server-side validation but used `Regex.IsMatch` directly. Since `Variable` constructor already throws `ArgumentException` for invalid names, adding the check before construction prevents a silent exception
- **Fix:** Added `Regex.IsMatch` check before `new Variable(name)` call; matched behavior to existing `Variable` constructor validation
- **Files modified:** packages/Recrd.Recording/Engine/PlaywrightRecorderEngine.cs
- **Verification:** Build passes with zero warnings
- **Committed in:** `6e04019` (Task 2 commit)

**2. [Rule 2 - Missing Critical] Added SelectorExtractor using import to PlaywrightRecorderEngine**
- **Found during:** Task 2 (ExtractDisplaySelector helper implementation)
- **Issue:** `SelectorExtractor` is in `Recrd.Recording.Selectors` namespace — not imported in engine file
- **Fix:** Added `using Recrd.Recording.Selectors;` to the using block
- **Files modified:** packages/Recrd.Recording/Engine/PlaywrightRecorderEngine.cs
- **Verification:** Build succeeds
- **Committed in:** `6e04019` (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (both Rule 2 — missing critical functionality)
**Impact on plan:** Both necessary for correctness. No scope creep.

## Issues Encountered

None — plan executed cleanly. Build: `Compilação com êxito. 0 Aviso(s). 0 Erro(s).`

## Known Stubs

None — all flows are fully wired. The `HandleInspectorCallbackAsync` handles both `TagConfirm` and `AssertConfirm`. The inspector HTML contains no placeholder text that would prevent the plan's goal from being achieved.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Inspector panel is feature-complete for Phase 06 recording engine
- `Recrd.Recording` builds cleanly; all inspector wiring is in place
- Plan 06-05 (multi-tab/popup support) can build on existing InspectorServer and engine wiring
- The inspector's `ExposeFunctionAsync` pattern is established and can be extended for future inspector features

## Self-Check: PASSED

- FOUND: packages/Recrd.Recording/Panel/inspector.html
- FOUND: packages/Recrd.Recording/Inspector/InspectorServer.cs
- FOUND: .planning/phases/06-recording-engine/06-04-SUMMARY.md
- FOUND commit: a1916b5 (Task 1)
- FOUND commit: 6e04019 (Task 2)
- Build: Compilação com êxito. 0 Aviso(s). 0 Erro(s).

---
*Phase: 06-recording-engine*
*Completed: 2026-03-31*
