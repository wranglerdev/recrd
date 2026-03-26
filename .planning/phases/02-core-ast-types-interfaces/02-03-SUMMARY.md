---
phase: 02-core-ast-types-interfaces
plan: 03
subsystem: api
tags: [dotnet, csharp, records, channels, interfaces, pipeline]

# Dependency graph
requires:
  - phase: 02-core-ast-types-interfaces plan 01
    provides: red test suites for InterfaceContractTests and ChannelPipelineTests
  - phase: 02-core-ast-types-interfaces plan 02
    provides: AST types (Session, Selector, SelectorStrategy, AssertionStep) used in interface signatures
provides:
  - ITestCompiler interface with TargetName and CompileAsync(Session, CompilerOptions) -> Task<CompilationResult>
  - IDataProvider interface with StreamAsync() returning IAsyncEnumerable<IReadOnlyDictionary<string,string>>
  - IEventInterceptor plugin extension point interface
  - IAssertionProvider plugin extension point interface
  - CompilerOptions and CompilationResult companion records
  - RecordedEvent sealed record (Id, TimestampMs, EventType, Selectors, Payload, DataVariable)
  - RecordedEventType enum (Click, InputChange, Select, Hover, Navigation, FileUpload, DragDrop)
  - IRecordingChannel interface for testable pipeline abstraction
  - RecordingChannel bounded channel wrapper with backpressure and cancellation
affects:
  - phase 03 (Recrd.Data needs IDataProvider)
  - phase 06 (Recrd.Recording needs IRecordingChannel, RecordedEvent, RecordedEventType)
  - phase 07 (Recrd.Compilers needs ITestCompiler, CompilerOptions, CompilationResult)
  - phase 11 (plugins need IEventInterceptor, IAssertionProvider)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Bounded System.Threading.Channels.Channel<T> with BoundedChannelFullMode.Wait for backpressure
    - Sealed records with explicit constructors and ArgumentNullException guards
    - IDisposable cleanup for CancellationTokenSource in RecordingChannel
    - CancellationTokenSource.CreateLinkedTokenSource for cooperative cancellation

key-files:
  created:
    - packages/Recrd.Core/Interfaces/ITestCompiler.cs
    - packages/Recrd.Core/Interfaces/CompilerOptions.cs
    - packages/Recrd.Core/Interfaces/CompilationResult.cs
    - packages/Recrd.Core/Interfaces/IDataProvider.cs
    - packages/Recrd.Core/Interfaces/IEventInterceptor.cs
    - packages/Recrd.Core/Interfaces/IAssertionProvider.cs
    - packages/Recrd.Core/Pipeline/RecordedEventType.cs
    - packages/Recrd.Core/Pipeline/RecordedEvent.cs
    - packages/Recrd.Core/Pipeline/IRecordingChannel.cs
    - packages/Recrd.Core/Pipeline/RecordingChannel.cs
  modified: []

key-decisions:
  - "RecordedEventType named with full prefix (not EventType) to match test expectations and avoid ambiguity with ActionType/AssertionType"
  - "ITestCompiler.CompileAsync has 2 parameters (Session, CompilerOptions) without CancellationToken — matches InterfaceContractTests assertion of exactly 2 parameters"
  - "RecordingChannel uses CreateLinkedTokenSource to combine external and internal cancellation tokens"

patterns-established:
  - "Interface namespace: Recrd.Core.Interfaces for all contracts consumed by downstream packages"
  - "Pipeline namespace: Recrd.Core.Pipeline for event streaming types"
  - "Bounded channel default capacity 1000 is constructor-injectable for test isolation"

requirements-completed:
  - CORE-07
  - CORE-08
  - CORE-09
  - CORE-10
  - CORE-11
  - CORE-12

# Metrics
duration: 2min
completed: 2026-03-26
---

# Phase 02 Plan 03: Interfaces and Channel Pipeline Summary

**Six C# contracts plus a bounded System.Threading.Channels pipeline — ITestCompiler, IDataProvider, IEventInterceptor, IAssertionProvider, RecordedEvent, and RecordingChannel — all zero Recrd.* dependencies**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-26T19:43:31Z
- **Completed:** 2026-03-26T19:45:57Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments

- All 4 required interfaces (ITestCompiler, IDataProvider, IEventInterceptor, IAssertionProvider) with companion records (CompilerOptions, CompilationResult)
- RecordedEvent sealed record with all 6 required fields (Id, TimestampMs, EventType, Selectors, Payload, DataVariable)
- RecordingChannel backed by bounded System.Threading.Channels.Channel<T> with Wait backpressure, cooperative cancellation, and IDisposable cleanup
- IRecordingChannel interface extracted for full mockability in tests
- Recrd.Core builds with zero warnings and zero Recrd.* dependencies

## Task Commits

Each task was committed atomically:

1. **Task 1: Create all 4 interfaces with companion types** - `cede759` (feat)
2. **Task 2: Create RecordedEvent, EventType, and RecordingChannel pipeline** - `d38fad9` (feat)

## Files Created/Modified

- `packages/Recrd.Core/Interfaces/ITestCompiler.cs` - Compiler contract: TargetName + CompileAsync(Session, CompilerOptions)
- `packages/Recrd.Core/Interfaces/CompilerOptions.cs` - Compiler options record (OutputDirectory, PreferredSelectorStrategy, TimeoutSeconds)
- `packages/Recrd.Core/Interfaces/CompilationResult.cs` - Compilation result record (GeneratedFiles, Warnings, DependencyManifest)
- `packages/Recrd.Core/Interfaces/IDataProvider.cs` - Data provider contract: StreamAsync() returning IAsyncEnumerable
- `packages/Recrd.Core/Interfaces/IEventInterceptor.cs` - Plugin extension point: InterceptAsync returning ValueTask<RecordedEvent?>
- `packages/Recrd.Core/Interfaces/IAssertionProvider.cs` - Plugin extension point: AssertionName + CreateAssertion(Selector, payload)
- `packages/Recrd.Core/Pipeline/RecordedEventType.cs` - Enum with 7 event types (Click through DragDrop)
- `packages/Recrd.Core/Pipeline/RecordedEvent.cs` - Sealed record with all 6 required fields
- `packages/Recrd.Core/Pipeline/IRecordingChannel.cs` - Channel abstraction interface for testability
- `packages/Recrd.Core/Pipeline/RecordingChannel.cs` - Bounded channel wrapper with backpressure, cancellation, IDisposable

## Decisions Made

- Named the enum `RecordedEventType` (not `EventType`) to match test expectations and avoid confusion with `ActionType`/`AssertionType` enums in the Ast namespace
- `ITestCompiler.CompileAsync` defined with exactly 2 parameters (`Session`, `CompilerOptions`) — the test uses reflection to assert `parameters.Length == 2`
- `RecordingChannel.WriteAsync` creates a linked `CancellationTokenSource` to ensure both external cancellation and internal `Cancel()` calls are respected

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Renamed EventType enum to RecordedEventType**
- **Found during:** Task 2 (Create RecordedEvent, EventType, and RecordingChannel pipeline)
- **Issue:** Plan specified creating `EventType.cs` with enum `EventType`, but `ChannelPipelineTests.cs` uses `RecordedEventType.Click` — the name `EventType` would conflict with the test's expectations and cause a compile error
- **Fix:** Created `RecordedEventType.cs` with enum `RecordedEventType` matching the test file
- **Files modified:** `packages/Recrd.Core/Pipeline/RecordedEventType.cs`
- **Verification:** Build succeeds with zero warnings
- **Committed in:** d38fad9 (Task 2 commit)

**2. [Rule 1 - Bug] Removed CancellationToken from ITestCompiler.CompileAsync**
- **Found during:** Task 1 (Create all 4 interfaces)
- **Issue:** Plan showed `CompileAsync(Session, CompilerOptions, CancellationToken)` with 3 params, but `InterfaceContractTests.cs` asserts `parameters.Length == 2` — a 3-parameter signature would fail the test
- **Fix:** Defined `CompileAsync(Session session, CompilerOptions options)` with exactly 2 parameters
- **Files modified:** `packages/Recrd.Core/Interfaces/ITestCompiler.cs`
- **Verification:** Build succeeds; `InterfaceContractTests` assertion `Assert.Equal(2, parameters.Length)` will pass
- **Committed in:** cede759 (Task 1 commit)

---

**Total deviations:** 2 auto-fixed (2 Rule 1 bug fixes)
**Impact on plan:** Both fixes required to match committed red tests. No scope creep.

## Issues Encountered

The test project build fails due to missing `Recrd.Core.Serialization` namespace (`SessionSerializationTests.cs`). This is plan 02-04's responsibility (JSON serialization context) and is expected in parallel phase execution. Tests for `InterfaceContractTests` and `ChannelPipelineTests` will compile and pass once plan 02-04 completes.

## Known Stubs

None — all interfaces define real contracts with no placeholder implementations or hardcoded values.

## Next Phase Readiness

- Phase 3 (Recrd.Data): `IDataProvider` contract ready for `CsvDataProvider` and `JsonDataProvider` implementations
- Phase 6 (Recrd.Recording): `IRecordingChannel`, `RecordedEvent`, `RecordedEventType` ready for Playwright CDP integration
- Phase 7 (Recrd.Compilers): `ITestCompiler`, `CompilerOptions`, `CompilationResult` ready for `RobotBrowserCompiler` and `RobotSeleniumCompiler`
- Phase 11 (Plugins): `IEventInterceptor`, `IAssertionProvider` ready for plugin implementations

---
*Phase: 02-core-ast-types-interfaces*
*Completed: 2026-03-26*
