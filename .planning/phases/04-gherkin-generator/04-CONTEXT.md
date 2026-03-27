# Phase 4: Gherkin Generator - Context

**Gathered:** 2026-03-27
**Status:** Ready for planning

<domain>
## Phase Boundary

`Recrd.Gherkin` walks the `Session` AST (from `Recrd.Core`) and an optional `IDataProvider` data source to emit a valid, deterministic pt-BR `.feature` file. Scope: single-file output per session, `Cenário` vs `Esquema do Cenário` branching, variable substitution, default heuristic grouping, and deterministic byte-identical output.

Recording, compilation to `.robot`, and CLI wiring are out of scope for this phase.

</domain>

<decisions>
## Implementation Decisions

### Step Text Templates

- **D-01:** Element references in generated steps use the **best human-readable selector value** — prefer `data-testid` value, then `id`, then `role`. E.g. `Clica no elemento "submit-btn"`, `Digita "<login>" no campo "username"`. No raw CSS/XPath expressions in step text.
- **D-02:** Variable placeholders use the `<variable_name>` syntax in the scenario body (Gherkin `Esquema do Cenário` standard). E.g. `Digita "<login>" no campo "username"`.

### Feature File Structure

- **D-03:** The `Feature:` name is derived from `Session.Metadata.BaseUrl`. Every session has a base URL, so this is always available and produces unique, traceable feature files.
- **D-04:** Scenario / Esquema do Cenário tags are **configurable via `GherkinGeneratorOptions`** — no tags emitted by default. QA engineers add their own tags post-generation.
- **D-05:** Minimal file structure: `# language: pt` header → blank line → `Feature:` → blank line → scenario keyword (no `Background:` section, no scenario description prose).

### GherkinException Design

- **D-06:** `GherkinException` is a `sealed class` (not record) extending `Exception`. It carries exactly two structured properties: `VariableName` (string) and `DataFilePath` (string). Follows the `DataParseException` precedent of carrying only what's actionable.
- **D-07:** GHER-04 extra-column warning is emitted to `stderr` as plain text (not a thrown exception). The generator's `TextWriter output` param is the `.feature` target; stderr warnings use `Console.Error` or an injected `TextWriter` at Claude's discretion.

### Generator Public API

- **D-08:** Expose `IGherkinGenerator` interface + `GherkinGenerator` sealed class. Follows the `ITestCompiler`/`IDataProvider` pattern — mockable in tests, composable in the CLI phase.
- **D-09:** Signature: `Task GenerateAsync(Session session, IDataProvider? dataProvider, TextWriter output, GherkinGeneratorOptions? options = null, CancellationToken ct = default)`.
- **D-10:** `GherkinGeneratorOptions` carries at minimum: `IReadOnlyList<string>? Tags` (null = no tags).

### TDD Structure

- **D-11:** Follows Phase 2/3 pattern: all tests committed **red** on `tdd/phase-04` branch before any implementation. Green phase commits implementation only after all tests pass.
- **D-12:** Test file organization mirrors behavior suites:
  - `FixedScenarioTests.cs` — zero-variable session → `Cenário` (GHER-01, GHER-07, GHER-08)
  - `DataDrivenTests.cs` — variable sessions → `Esquema do Cenário` + `Exemplos` table, column ordering (GHER-02, GHER-09)
  - `VariableMismatchTests.cs` — missing variable → `GherkinException`, extra column → stderr warning (GHER-03, GHER-04)
  - `GroupingTests.cs` — `GroupStep` keyword mapping + default heuristic (GHER-05, GHER-06)
  - `DeterminismTests.cs` — byte-identical output across multiple runs (GHER-07)

### Claude's Discretion

- Exact pt-BR verb templates for each `ActionType` (`Click`, `Type`, `Select`, `Navigate`, `Upload`, `DragDrop`) and `AssertionType` (`TextEquals`, `TextContains`, `Visible`, `Enabled`, `UrlMatches`)
- Whether `GherkinGeneratorOptions` is a record or class
- Whether stderr warning injection uses a `TextWriter` constructor parameter or `Console.Error` directly
- Indentation style in `.feature` output (2 spaces vs 4 spaces — pick one consistently)
- Whether `IDataProvider` null means "no data" (emit `Cenário`) or "use session variable count to decide"

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements

- `.planning/REQUIREMENTS.md` §GHER-01–GHER-09 — Full specification: scenario type branching, variable mismatch errors, keyword mapping, default heuristic, determinism, encoding, column ordering

### Project Guidelines

- `CLAUDE.md` — TDD mandate (red-green per phase, `tdd/phase-*` branch prefix for CI-06), build commands, `DOTNET_SYSTEM_NET_DISABLEIPV6=1` prefix requirement

### Phase 2 Decisions (binding)

- `.planning/phases/02-core-ast-types-interfaces/02-CONTEXT.md` §D-01–D-05 — AST types are sealed records; `ActionType`, `AssertionType`, `GroupType` enums; `Payload` is `IReadOnlyDictionary<string,string>`
- `.planning/phases/02-core-ast-types-interfaces/02-CONTEXT.md` §D-06 — System.Text.Json only; no Newtonsoft.Json

### Phase 3 Decisions (pattern reference)

- `.planning/phases/03-data-providers/03-CONTEXT.md` §D-06–D-07 — `DataParseException` structured property pattern; `GherkinException` should follow the same design discipline

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- `Recrd.Core.Ast.Session` — root type; carries `Variables`, `Steps`, `Metadata.BaseUrl` (used for Feature name)
- `Recrd.Core.Ast.ActionStep` — `ActionType` enum + `Selector` + `Payload` dict
- `Recrd.Core.Ast.AssertionStep` — `AssertionType` enum + `Selector` + `Payload` dict
- `Recrd.Core.Ast.GroupStep` — `GroupType` enum + `Steps` children
- `Recrd.Core.Ast.Selector` — ranked `IReadOnlyList<SelectorStrategy>` for element identification
- `Recrd.Core.Interfaces.IDataProvider` — `IAsyncEnumerable<IReadOnlyDictionary<string,string>> StreamAsync()` — the data source interface the generator consumes

### Established Patterns

- Sealed records for all AST types (immutable, structural equality)
- Behavior-suite test organization (one file per concern, not per type)
- TDD red-green on `tdd/phase-*` branch prefix
- Typed exceptions with structured properties (`DataParseException` precedent)
- Interface + concrete sealed class pattern (`IDataProvider`/`CsvDataProvider`, etc.)

### Integration Points

- `Recrd.Gherkin` depends on `Recrd.Core` (AST types + `IDataProvider`) — no other `Recrd.*` deps permitted
- `Recrd.Gherkin.Tests` project already exists in `tests/Recrd.Gherkin.Tests/`
- `packages/Recrd.Gherkin/Placeholder.cs` is the only existing file — clean slate for implementation
- Phase 8 (CLI) will wire `IGherkinGenerator` into the `recrd compile` command

</code_context>

<specifics>
## Specific Ideas

- Step text uses best-readable selector value: `data-testid` → `id` → `role`. Selector value appears quoted in the step, e.g. `Clica no elemento "submit-btn"`.
- Feature name = `Session.Metadata.BaseUrl` (always present, unique per session).
- Tags configurable via `GherkinGeneratorOptions.Tags` — default null (no tags emitted).

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 04-gherkin-generator*
*Context gathered: 2026-03-27*
