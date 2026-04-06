---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: unknown
last_updated: "2026-04-06T03:12:04.921Z"
progress:
  total_phases: 12
  completed_phases: 6
  total_plans: 31
  completed_plans: 28
---

# State: recrd

## Project Reference

**Core Value:** Record once, compile to a round-trip-verified, data-driven Robot Framework 7 suite with zero manual keyword writing.

**Current Focus:** Phase 07 — compilers

---

## Current Position

| Field | Value |
|-------|-------|
| Phase | 07 — compilers |
| Plan | 2 of 4 |
| Status | In Progress |
| TDD State | Red phase complete (tdd/phase-07 branch) |

**Progress**

```
Phase: 07 (compilers) — EXECUTING
Plan: 3 of 4
         [██████████] 100%
```

---

## Performance Metrics

| Metric | Value |
|--------|-------|
| Phases complete | 0 / 12 |
| Plans complete | 0 / ? |
| Requirements delivered | 0 / 78 |
| CI status | Not configured |

---
| Phase 01 P01 | 151s | 2 tasks | 3 files |
| Phase 01 P04 | 31 | 1 tasks | 2 files |
| Phase 01 P06 | 40 | 1 tasks | 1 files |
| Phase 01 P03 | 120 | 2 tasks | 3 files |
| Phase 01 P02 | 814 | 3 tasks | 11 files |
| Phase 01 P05 | 10 | 2 tasks | 13 files |
| Phase 01 P07 | 1 | 2 tasks | 3 files |
| Phase 02 P01 | 2 | 2 tasks | 7 files |
| Phase 02 P02 | 127 | 2 tasks | 13 files |
| Phase 02 P03 | 2 | 2 tasks | 10 files |
| Phase 02 P04 | 233 | 2 tasks | 9 files |
| Phase 03 P01 | 3 | 2 tasks | 6 files |
| Phase 03 P02 | 2 | 1 tasks | 2 files |
| Phase 03 P03 | 5 | 1 tasks | 2 files |
| Phase 03 P04 | 3 | 1 tasks | 2 files |
| Phase 04 P01 | 4 | 2 tasks | 10 files |
| Phase 04 P02 | 6 | 2 tasks | 3 files |
| Phase 04 P03 | 15 | 1 tasks | 2 files |
| Phase 04 P04 | 5 | 1 tasks | 2 files |
| Phase 05 P03 | 41 | 1 tasks | 1 files |
| Phase 05 P01 | 1 | 1 tasks | 1 files |
| Phase 05 P02 | 52 | 2 tasks | 2 files |
| Phase 06 P02 | 25 | 2 tasks | 5 files |
| Phase 06 P01 | 313 | 3 tasks | 10 files |
| Phase 06 P03 | 4 | 2 tasks | 5 files |
| Phase 06 P04 | 20 | 2 tasks | 3 files |
| Phase 06 P05 | 457 | 2 tasks | 7 files |
| Phase 07-compilers P01 | 5min | 2 tasks | 15 files |
| Phase 07 P02 | 4 | 2 tasks | 6 files |

## Accumulated Context

### Key Decisions Logged

| Decision | Rationale |
|----------|-----------|
| RF7 only | Single compiler test surface; cleaner keyword syntax |
| Multi-tab: constrained popup only | Full multi-tab AST complexity not worth v1 risk |
| AI step grouping deferred to plugin | ONNX dependency too heavy for core; heuristic sufficient for v1 |
| `.side` import excluded | Selenium IDE format unstable, no format coverage guarantee |
| Playwright .NET over raw CDP | Transport reconnection, multi-browser, stable API surface |
| `AssemblyLoadContext` for plugins | Prevents version conflicts; host rejects incompatible major versions |
| `Channel<T>` for event pipeline | No HTTP/socket/serialization overhead between recording and inspector |
| Recrd.Integration.Tests references all 5 packages | Integration tests span the full recording-to-compiler pipeline |
| PlaceholderTests.cs pattern in all test projects | Prevents xunit no-tests-found exit-code 1 on empty test projects |
| IsPackable=false on all test .csproj files | Prevents dotnet pack from emitting test NuGet packages |
| dotnet-tools.json tools object left empty | dotnet format is SDK-built-in; manifest ready for Phase 5 Stryker addition |
| Removed .gitignore exclusion of .config/dotnet-tools.json | Tool manifests must be tracked in source control for CI reproducibility |
| RecrdJsonContext uses GenerationMode=Metadata | Fast-path (Serialization) mode does not support JsonPolymorphic/$type discriminators |
| PascalCase constructor parameters in AST types | Enables named-argument call sites in tests; aligns param names with property names |
| Stub async iterators use CS0162 pragma around yield break | C# async iterator stubs throw NotImplementedException but need a yield; compiler rejects unreachable yield without pragma |
| tdd/phase-03 branch for Phase 3 red tests | TDD mandate D-08: all 21 tests committed failing before any CSV/JSON implementation begins |
| BuildRow helper method for async iterator exception wrapping | yield return cannot appear inside try/catch in C# (CS1626) — extracted row building to separate synchronous method |
| JsonDataProvider MoveNextAsync pattern | Restructured to call MoveNextAsync in try/catch and yield return outside to satisfy CS1626 restriction |
| GroupStep detection uses Any(s => s is GroupStep) on top-level steps | Chooses between GroupStep emission path and default heuristic path in GherkinGenerator |
| StepTextRenderer Internal/ subdirectory pattern | Implementation helpers not exposed in public Recrd.Gherkin API |
| Null dataProvider with variables skips Exemplos silently | Preserves DeterminismTests contract which pass null intentionally for step-text determinism testing |
| No cell padding in Exemplos table | Test assertions use exact substring matching; padding would break Contains("| bob |") assertions |
| Merged gsd/phase-04-gherkin-generator to main (not tdd/phase-04) | Implementation lived in gsd branch after worktree-based multi-plan execution; tdd branch only had red-phase scaffold |
| NU1900 NuGet audit error is pre-existing environment constraint | Offline/restricted network cannot reach api.nuget.org for vulnerability DB; binaries build and tests pass correctly |
| Coverlet inline thresholds via --threshold 90 --threshold-type line --threshold-stat minimum per project | Fail-fast per project; project name in error message; no extra tooling required (D-01/D-02) |
| Two conditional test steps for TDD red-phase: continue-on-error on tdd/phase-* branches | Hard-fail on non-TDD branches; coverage gates still enforced on all branches including TDD red-phase (D-08/D-10) |
| Hover events opt-in via data-recrd-hover attribute | Prevents mouseover noise from passive UI interactions in recording agent |
| ExposeFunctionAsync before AddInitScriptAsync | JS agent requires __recrdCapture defined before first frame load (RESEARCH.md Pitfall 1) |
| BrowserContext.ExposeFunctionAsync over Page.ExposeFunctionAsync | Propagates __recrdCapture to all pages in context including popups (REC-15 coverage) |
| UTF8Encoding(false) for .recrd and .recrd.partial files | Encoding.UTF8 emits BOM, breaking JSON spec; new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) required |
| PartialSnapshotWriter takes Func<Session> for decoupling | Snapshot writer calls SessionBuilder.Build() at each tick without coupling to builder directly |
| InternalsVisibleTo via AssemblyAttribute in csproj | Exposes internal WriteSnapshotAsync to test project without separate AssemblyInfo.cs |
| window.__recrdInspectorCallback registered on inspector BrowserContext | ExposeFunctionAsync is context-scoped; inspector context doesn't inherit recording context bindings |
| AssertConfirm selector sent as display string from inspector dialog | Full selector JSON unavailable after user interaction; converted to minimal Css Selector in HandleInspectorCallbackAsync |
| InspectorServer wraps all EvaluateAsync calls in try/catch PlaywrightException | Closed inspector is non-fatal; sets _isOpen=false and continues recording normally |
| InspectorPanel tests use SessionBuilder directly for TagConfirm/AssertConfirm | Avoids two-browser overhead per test; tests C# logic without Playwright browser |
| BrowserContextTests.ZeroLocalStorage uses StorageStateAsync JSON check | Avoids SecurityError from opaque-origin pages (about:blank, data: URLs) |
| isPopup top-level JSON field + __popupScope in RecordedEvent.Payload | Popup scope via window.opener; RecordedEventBuilder enriches Payload dict from top-level fields |
| InternalsVisibleTo in Recrd.Compilers.csproj for test project | Exposes KeywordNameBuilder (internal) to Recrd.Compilers.Tests without separate AssemblyInfo.cs |
| tdd/phase-07 branch for Phase 7 red tests | TDD mandate D-11: all 45 compiler tests committed failing before any implementation begins |
| SeleniumKeywordEmitter uses implicit wait only | Set Selenium Implicit Wait emitted once in Abrir Suite; no per-step Wait Until Element calls (D-08) |

### TDD Mandate

Every phase follows red-green: all tests for the phase are written and committed as failing (red) before any implementation begins. CI must be green at every commit after implementation. The `tdd/phase-*` branch prefix enables CI test-failure tolerance during the red phase (CI-06).

### Active Blockers

None.

### Todos

- Plan Phase 1 (`/gsd:plan-phase 1`)

---

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260326-eol | Fix Playwright NuGet package in Recrd.Recording causing infinite loading during dotnet restore | 2026-03-26 | 470c456 | [260326-eol-fix-playwright-nuget-package-in-recrd-re](./quick/260326-eol-fix-playwright-nuget-package-in-recrd-re/) |
| 260326-t3b | Fix CI failure: global.json SDK 10.0.103 unavailable on runners — changed to latestFeature/10.0.100 | 2026-03-26 | 7fc79bc | [260326-t3b-fix-ci-failure-global-json-requires-net-](./quick/260326-t3b-fix-ci-failure-global-json-requires-net-/) |
| 260329-w2f | Fix CI pipeline coverage gate — switch coverlet.collector to coverlet.msbuild in 4 test projects; update ci.yml gate steps to /p: property syntax | 2026-03-29 | 3b28b93 | [260329-w2f-fix-ci-pipeline-coverage-gate](./quick/260329-w2f-fix-ci-pipeline-coverage-gate/) |

---

## Session Continuity

**Last updated:** 2026-04-06 — Completed 07-03-PLAN.md: RobotSeleniumCompiler green phase, all 45 compiler tests pass

**To resume:** Phase 07 Plan 04 — integration tests or next plan in phase.
