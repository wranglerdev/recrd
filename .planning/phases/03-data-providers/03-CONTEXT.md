# Phase 3: Data Providers - Context

**Gathered:** 2026-03-26
**Status:** Ready for planning

<domain>
## Phase Boundary

`Recrd.Data` delivers production-quality `CsvDataProvider` and `JsonDataProvider` — both implementing `IDataProvider.StreamAsync()` (`IAsyncEnumerable<IReadOnlyDictionary<string,string>>`), streaming rows with bounded memory (≤1000 rows in-memory, ≤100MB peak heap delta on 50MB input), and throwing `DataParseException` with typed diagnostic info on malformed input.

Gherkin generation, compiler integration, and CLI wiring are separate phases.

</domain>

<decisions>
## Implementation Decisions

### CSV Parsing Approach

- **D-01:** Use **CsvHelper** (JoshClose/CsvHelper NuGet) as the CSV parsing engine. It is RFC 4180 compliant, BOM-tolerant, handles configurable delimiters, and is actively maintained. Adds one NuGet dependency to `Recrd.Data`.
- **D-02:** `CsvDataProvider` wraps CsvHelper with `IAsyncEnumerable<T>` streaming — yield one `IReadOnlyDictionary<string,string>` per row, never buffer all rows.

### JSON Handling

- **D-03:** `JsonDataProvider` flattens nested objects using dot-notation (e.g., `{ "user": { "name": "Gil" } }` → `user.name`).
- **D-04:** Array fields encountered during flattening are **silently skipped** — not included in the output dictionary. They are not an error. This keeps output predictable for data-driven `Exemplos` tables.
- **D-05:** Use **System.Text.Json** for JSON parsing (consistent with Phase 2's D-06 decision — no Newtonsoft.Json anywhere in the codebase).

### DataParseException Design

- **D-06:** `DataParseException` carries three properties: `LineNumber` (int), `OffendingLine` (string — raw text of the bad line), and `FilePath` (string — path to the source file). This enriched form helps QA engineers debugging bad data files from CI output.
- **D-07:** Both `CsvDataProvider` and `JsonDataProvider` throw `DataParseException` (not generic exceptions) on all parse failures.

### TDD Structure

- **D-08:** Following Phase 2's established pattern: all tests committed red on a `tdd/phase-03` branch before any implementation. Test files organized as behavior suites:
  - `CsvDataProviderTests.cs` — RFC 4180 parsing, BOM tolerance, delimiter config, streaming correctness (DATA-01, DATA-02, DATA-03)
  - `JsonDataProviderTests.cs` — flat object flattening, dot-notation, array field skipping, non-array root error (DATA-04, DATA-05)
  - Memory footprint verified in `CsvDataProviderTests` via `GC.GetTotalMemory` on a 50MB fixture file

### Claude's Discretion

- CsvHelper version to pin (latest stable at planning time)
- Whether `CsvDataProvider` exposes `IAsyncEnumerable` directly or wraps via an adapter
- How null/empty cell values are represented in the output dictionary (`""` or absent key)
- Whether to use a `CsvConfiguration` builder pattern or constructor parameters for delimiter configuration

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements

- `.planning/REQUIREMENTS.md` §DATA-01–DATA-05 — Full specification for both providers: RFC 4180 compliance, BOM tolerance, streaming constraints, dot-notation flattening, `DataParseException` contract

### Project Guidelines

- `CLAUDE.md` — TDD mandate (red-green per phase, `tdd/phase-*` branch prefix for CI-06), build commands, `DOTNET_SYSTEM_NET_DISABLEIPV6=1` prefix requirement

### Phase 2 Decisions (binding on this phase)

- `.planning/phases/02-core-ast-types-interfaces/02-CONTEXT.md` §D-06 — System.Text.Json only; no Newtonsoft.Json anywhere in the codebase

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- `packages/Recrd.Data/Recrd.Data.csproj` — Empty library project already referencing `Recrd.Core`; `AssemblyName` set to `Recrd.Data`. Ready for implementation.
- `tests/Recrd.Data.Tests/Recrd.Data.Tests.csproj` — xUnit + Moq + coverlet already configured; `ProjectReference` to `Recrd.Data` in place.
- `Directory.Build.props` — `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true` apply automatically.

### Established Patterns

- `PlaceholderTests.cs` pattern: delete and replace with real behavior-suite test files (same as Phase 2).
- `Placeholder.cs` in `Recrd.Data`: delete and replace with real types.
- `[Theory]` + `[MemberData]` for covering multiple input variants (established in Phase 2's `StepModelTests`).
- `IsPackable=false` on test projects — already set.

### Integration Points

- `IDataProvider` interface is defined in `Recrd.Core` (Phase 2) — `Recrd.Data` classes implement it directly.
- Phase 4 (Gherkin Generator) will call `IDataProvider.StreamAsync()` to merge rows into `Exemplos` tables — this phase must deliver a correct, stable streaming contract.
- `Recrd.Integration.Tests` already references `Recrd.Data` — integration tests spanning the full pipeline can run once this phase is complete.

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

*Phase: 03-data-providers*
*Context gathered: 2026-03-26*
