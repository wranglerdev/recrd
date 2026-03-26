---
phase: 02-core-ast-types-interfaces
plan: 02
subsystem: core-ast
tags: [dotnet, csharp, records, ast, json, serialization, tdd]

# Dependency graph
requires:
  - phase: 02-01
    provides: red test suites for StepModelTests, SelectorVariableTests, SessionSerializationTests
  - phase: 01-monorepo-scaffold
    provides: Recrd.Core.csproj, Directory.Build.props (net10.0, Nullable enable, TreatWarningsAsErrors)
provides:
  - 13 AST type files in packages/Recrd.Core/Ast/ defining complete AST type system
  - IStep polymorphic interface with JsonPolymorphic/JsonDerivedType attributes
  - ActionStep, AssertionStep, GroupStep sealed records
  - All 4 enums: ActionType (6), AssertionType (5), GroupType (3), SelectorStrategy (5)
  - Selector with ranked Strategies list and Values dictionary
  - Variable with GeneratedRegex name validation
  - Session root record with SchemaVersion, Metadata, Variables, Steps
affects:
  - 02-03 (interfaces/pipeline depend on Session and IStep types)
  - 02-04 (RecrdJsonContext serializes Session and all Ast types)
  - future phases (Data, Gherkin, Compilers all consume Recrd.Core.Ast)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Sealed records with explicit constructor for null-guard enforcement under TreatWarningsAsErrors + Nullable enable"
    - "GeneratedRegex source generation for zero-allocation regex validation in Variable"
    - "partial record required when using [GeneratedRegex] source generation"
    - "IReadOnlyList<SelectorStrategy> + IReadOnlyDictionary<SelectorStrategy, string> for ranked priority selector design"
    - "File-scoped namespace Recrd.Core.Ast; across all 13 files"

key-files:
  created:
    - packages/Recrd.Core/Ast/IStep.cs
    - packages/Recrd.Core/Ast/ActionType.cs
    - packages/Recrd.Core/Ast/ActionStep.cs
    - packages/Recrd.Core/Ast/AssertionType.cs
    - packages/Recrd.Core/Ast/AssertionStep.cs
    - packages/Recrd.Core/Ast/GroupType.cs
    - packages/Recrd.Core/Ast/GroupStep.cs
    - packages/Recrd.Core/Ast/SelectorStrategy.cs
    - packages/Recrd.Core/Ast/Selector.cs
    - packages/Recrd.Core/Ast/Variable.cs
    - packages/Recrd.Core/Ast/Session.cs
    - packages/Recrd.Core/Ast/SessionMetadata.cs
    - packages/Recrd.Core/Ast/ViewportSize.cs
  modified: []

key-decisions:
  - "Selector uses Strategies (ranked list) + Values (dictionary) instead of plan's single Value/Strategy/Alternatives — required to match TDD test contract from Plan 01 (SelectorVariableTests.cs, SessionSerializationTests.cs)"

# Metrics
duration: 127s
completed: 2026-03-26
---

# Phase 02 Plan 02: Core AST Types Implementation Summary

**13 sealed record/enum/interface AST types in Recrd.Core.Ast implementing the full session model: IStep polymorphic interface, 3 step records, 4 enums, Selector with ranked priority chain, Variable with GeneratedRegex validation, and Session root record**

## Performance

- **Duration:** 127s (~2 min)
- **Started:** 2026-03-26T19:43:26Z
- **Completed:** 2026-03-26
- **Tasks:** 2
- **Files modified:** 13 created

## Accomplishments

- Created `packages/Recrd.Core/Ast/` directory with all 13 AST type files
- IStep interface with `[JsonPolymorphic]` and `[JsonDerivedType]` attributes for all 3 step types
- ActionType (6 values), AssertionType (5), GroupType (3), SelectorStrategy (5) — exact enum counts per CORE-02/03/04/05
- Selector with `Strategies` (ranked priority list) and `Values` (strategy-to-value dictionary)
- ActionStep, AssertionStep, GroupStep sealed records with ArgumentNullException null guards
- Variable with `[GeneratedRegex]` source generation for `^[a-z][a-z0-9_]{0,63}$` validation, throws ArgumentException on invalid names
- Session root record with SchemaVersion, Metadata, Variables, Steps
- Recrd.Core builds with zero warnings under TreatWarningsAsErrors + Nullable enable
- StepModelTests.cs and SelectorVariableTests.cs now compile (2 remaining errors are in SessionSerializationTests.cs — awaiting Plan 04's RecrdJsonContext)

## Task Commits

1. **Task 1: IStep, enums, step records, Selector** - `daed0eb`
2. **Task 2: Variable, Session, SessionMetadata, ViewportSize** - `455b582`

## Files Created/Modified

- `packages/Recrd.Core/Ast/IStep.cs` - Polymorphic step marker interface with JsonPolymorphic/JsonDerivedType attributes
- `packages/Recrd.Core/Ast/ActionType.cs` - 6-value enum: Click, Type, Select, Navigate, Upload, DragDrop
- `packages/Recrd.Core/Ast/ActionStep.cs` - Sealed record with ActionType, Selector, Payload
- `packages/Recrd.Core/Ast/AssertionType.cs` - 5-value enum: TextEquals, TextContains, Visible, Enabled, UrlMatches
- `packages/Recrd.Core/Ast/AssertionStep.cs` - Sealed record with AssertionType, Selector, Payload
- `packages/Recrd.Core/Ast/GroupType.cs` - 3-value enum: Given, When, Then
- `packages/Recrd.Core/Ast/GroupStep.cs` - Sealed record with GroupType and IReadOnlyList<IStep> child steps
- `packages/Recrd.Core/Ast/SelectorStrategy.cs` - 5-value enum: DataTestId, Id, Role, Css, XPath
- `packages/Recrd.Core/Ast/Selector.cs` - Sealed record with ranked Strategies list and Values dictionary
- `packages/Recrd.Core/Ast/Variable.cs` - Sealed partial record with GeneratedRegex name validation
- `packages/Recrd.Core/Ast/Session.cs` - Root AST record with SchemaVersion, Metadata, Variables, Steps
- `packages/Recrd.Core/Ast/SessionMetadata.cs` - Metadata record with Id, CreatedAt, BrowserEngine, ViewportSize, BaseUrl
- `packages/Recrd.Core/Ast/ViewportSize.cs` - Positional record (Width, Height)

## Decisions Made

- Selector design deviated from plan spec to match TDD test contract: tests (written in Plan 01) use `Selector(Strategies: IReadOnlyList<SelectorStrategy>, Values: IReadOnlyDictionary<SelectorStrategy, string>)` rather than the plan's `Selector(value: string, strategy: SelectorStrategy, alternatives: ...)`. The test contract is authoritative in TDD; plan spec was the intent but tests codified the exact API.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Selector API adapted to match TDD test contract**
- **Found during:** Task 1 (reading StepModelTests.cs and SelectorVariableTests.cs before implementing)
- **Issue:** Plan specified `Selector(string value, SelectorStrategy strategy, IReadOnlyList<SelectorStrategy>? alternatives)` but the existing red tests (committed in Plan 01) use `Selector(Strategies: IReadOnlyList<SelectorStrategy>, Values: IReadOnlyDictionary<SelectorStrategy, string>)`
- **Fix:** Implemented Selector with `Strategies` + `Values` design to match the test contract
- **Files modified:** `packages/Recrd.Core/Ast/Selector.cs`
- **Commit:** daed0eb

## Known Stubs

None. All 13 AST types are fully implemented. No hardcoded values or placeholder text.

## Self-Check: PASSED

All 13 files exist and Recrd.Core builds with zero warnings.
