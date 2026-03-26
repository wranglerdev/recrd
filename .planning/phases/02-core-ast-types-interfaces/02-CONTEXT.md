# Phase 2: Core AST Types & Interfaces - Context

**Gathered:** 2026-03-26
**Status:** Ready for planning

<domain>
## Phase Boundary

`Recrd.Core` library contains all AST types (`Session`, `ActionStep`, `AssertionStep`, `GroupStep`, `Selector`, `Variable`), all four interfaces (`ITestCompiler`, `IDataProvider`, `IEventInterceptor`, `IAssertionProvider`), the `Channel<RecordedEvent>` pipeline infrastructure, and `RecordedEvent` envelope — fully unit-tested with zero dependencies on other `Recrd.*` packages.

Recording engine, data providers, Gherkin generator, and compilers are separate phases.

</domain>

<decisions>
## Implementation Decisions

### C# Type Modeling

- **D-01:** AST step types (`ActionStep`, `AssertionStep`, `GroupStep`) are sealed C# **records** with init-only properties — immutable, structural equality, with-expressions for transformation, clean pattern matching. Not classes or abstract hierarchies.
- **D-02:** `ActionStep` uses a single record with an `ActionType` enum (`Click`, `Type`, `Select`, `Navigate`, `Upload`, `DragDrop`) plus a `Payload` dictionary for subtype-specific data. No separate ClickStep/TypeStep etc. types.
- **D-03:** `AssertionStep` uses a single record with an `AssertionType` enum (`TextEquals`, `TextContains`, `Visible`, `Enabled`, `UrlMatches`) plus a `Payload` dictionary. Same flat pattern as ActionStep.
- **D-04:** `GroupStep` is a sealed record with a `GroupType` enum (`Given`, `When`, `Then`) and a `Steps` collection of child steps.
- **D-05:** `Session`, `Selector`, `Variable`, `RecordedEvent` are also sealed records where applicable.

### JSON Serialization

- **D-06:** Use **System.Text.Json** only — no Newtonsoft.Json. Keeps `Recrd.Core` dependency-free beyond the .NET 10 BCL.
- **D-07:** Polymorphic step serialization uses **`[JsonPolymorphic]` + `[JsonDerivedType]` attributes** on the step base type (or `IStep` interface). Emits a `$type` discriminator automatically. No hand-written JsonConverter for polymorphism.
- **D-08:** Use **source-generated `JsonSerializerContext`** (`[JsonSourceGenerationOptions]`) for AOT and self-contained single-file publish compatibility.

### Channel\<T\> Pipeline Design

- **D-09:** Expose the pipeline as a **thin wrapper class** (name at Claude's discretion, e.g., `RecordingChannel`) over `System.Threading.Channels.Channel<RecordedEvent>`. Wrapper exposes explicit `WriteAsync`, `ReadAllAsync`, `Complete`, and `Cancel` surface. Extract an interface (e.g., `IRecordingChannel`) to make it mockable in tests.
- **D-10:** Underlying channel is **bounded** (`BoundedChannel<RecordedEvent>`) with a configurable capacity (default 1000). Capacity is constructor-injectable so tests can use small values. Provides natural backpressure — `WriteAsync` awaits if full.

### Test Structure

- **D-11:** Tests in `Recrd.Core.Tests` are organized into **behavior-suite files**, not per-type files:
  - `SessionSerializationTests.cs` — round-trip JSON serialization/deserialization (CORE-01)
  - `StepModelTests.cs` — constructibility of all step types and subtypes (CORE-02, CORE-03, CORE-04)
  - `SelectorVariableTests.cs` — selector priority ranking, variable name validation (CORE-05, CORE-06)
  - `ChannelPipelineTests.cs` — backpressure, cancellation, drain-without-deadlock (CORE-11, CORE-12)
  - `InterfaceContractTests.cs` — all four interfaces defined (CORE-07, CORE-08, CORE-09, CORE-10), zero `Recrd.*` dep (CORE-13)
- **D-12:** Subtype coverage in `StepModelTests` uses **`[Theory]` + `[MemberData]`** providing one test case per `ActionType`/`AssertionType` enum value. Single theory method verifies all subtypes constructible and serialization-round-trippable.
- **D-13:** **All test files are committed red in one atomic commit** on a `tdd/phase-02` branch prefix before any implementation begins. CI-06 tolerates test failures on that branch prefix.

### Claude's Discretion

- Exact wrapper class name (`RecordingChannel`, `EventPipeline`, or similar)
- Exact interface name for the channel wrapper (`IRecordingChannel`, etc.)
- JSON property naming convention (camelCase vs PascalCase for the `.recrd` file)
- How `Selector` priority array is represented in the record (ranked `IReadOnlyList<SelectorStrategy>` or `SelectorType` + fallback chain)
- Whether `Variable` name validation throws at record constructor or via a static factory method
- `Payload` dictionary key/value types (`Dictionary<string, string>` vs `Dictionary<string, object?>`)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements

- `.planning/REQUIREMENTS.md` §CORE-01–CORE-13 — Full specification for every type, interface, and pipeline contract in this phase

### Project Guidelines

- `CLAUDE.md` — TDD mandate (red-green per phase, `tdd/phase-*` branch prefix for CI-06), build commands, test commands, `DOTNET_SYSTEM_NET_DISABLEIPV6=1` prefix requirement

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- `packages/Recrd.Core/Recrd.Core.csproj` — Empty library project, ready for types; `AssemblyName` and `PackageId` already set to `Recrd.Core`
- `tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj` — xUnit + Moq + coverlet already configured; `ProjectReference` to `Recrd.Core` in place
- `Directory.Build.props` — `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true`, `LangVersion latest` apply automatically to all projects

### Established Patterns

- `PlaceholderTests.cs` pattern: existing placeholder comment says "Tests will be added in Phase 2" — delete and replace with real test suites
- `Placeholder.cs` in `Recrd.Core`: namespace `Recrd.Core` already declared — delete and replace with real types
- `IsPackable=false` on all test projects — already set, do not pack test assemblies

### Integration Points

- `Recrd.Integration.Tests` references all 5 packages — `Recrd.Core` types will be visible there once implemented
- Phase 3 (Data Providers) depends on `IDataProvider` interface defined in this phase
- Phase 6 (Recording Engine) depends on `IRecordingChannel`/`Channel<RecordedEvent>` from this phase
- Phase 7 (Compilers) depends on `ITestCompiler`, `Session`, all step types from this phase

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches for items under Claude's Discretion.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 02-core-ast-types-interfaces*
*Context gathered: 2026-03-26*
