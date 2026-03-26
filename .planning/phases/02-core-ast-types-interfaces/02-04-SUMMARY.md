---
phase: 02-core-ast-types-interfaces
plan: "04"
subsystem: serialization
tags: [dotnet, json, system-text-json, source-generation, aot, polymorphism]

# Dependency graph
requires:
  - phase: 02-core-ast-types-interfaces
    provides: "AST types (Session, ActionStep, AssertionStep, GroupStep, Selector, Variable), Pipeline (RecordedEvent), Interfaces (CompilerOptions, CompilationResult)"
provides:
  - "RecrdJsonContext: source-generated JSON serializer context with metadata mode for AOT compatibility"
  - "Full Session JSON round-trip with polymorphic IStep deserialization"
  - "All 40 Recrd.Core.Tests passing (green phase)"
affects:
  - "Recrd.Gherkin: will consume Session deserialization"
  - "recrd-cli: will use RecrdJsonContext for .recrd file read/write"
  - "Recrd.Recording: will serialize RecordedEvent payloads"

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "JsonSourceGenerationMode.Metadata required for polymorphic interface serialization (IStep with $type discriminator)"
    - "All concrete types AND collection wrappers (IReadOnlyList<T>, IReadOnlyDictionary<K,V>) must be explicitly registered"
    - "PascalCase constructor parameters with `this.Prop = Param` assignment enables named-argument test ergonomics"

key-files:
  created:
    - packages/Recrd.Core/Serialization/RecrdJsonContext.cs
  modified:
    - packages/Recrd.Core/Ast/ActionStep.cs
    - packages/Recrd.Core/Ast/AssertionStep.cs
    - packages/Recrd.Core/Ast/GroupStep.cs
    - packages/Recrd.Core/Ast/Selector.cs
    - packages/Recrd.Core/Ast/Session.cs
    - packages/Recrd.Core/Ast/SessionMetadata.cs
    - packages/Recrd.Core/Pipeline/RecordedEvent.cs
    - tests/Recrd.Core.Tests/SelectorVariableTests.cs
    - tests/Recrd.Core.Tests/StepModelTests.cs

key-decisions:
  - "GenerationMode=Metadata chosen over Serialization mode because fast-path does not support polymorphism (IStep $type discriminator)"
  - "PascalCase constructor parameters used throughout AST types to enable C# named-argument syntax in tests"

patterns-established:
  - "Source-gen context: public partial sealed class, all concrete types + interface + collection variants registered"
  - "Constructor param naming: PascalCase params + this.Prop = Param assignment for named-argument compatibility"

requirements-completed:
  - CORE-01
  - CORE-13

# Metrics
duration: 4min
completed: "2026-03-26"
---

# Phase 02 Plan 04: JSON Serialization Context Summary

**Source-generated RecrdJsonContext with metadata mode enables full Session JSON round-trip including polymorphic IStep deserialization via $type discriminator; all 40 Recrd.Core.Tests green.**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-26T23:38:39Z
- **Completed:** 2026-03-26T23:42:32Z
- **Tasks:** 2
- **Files modified:** 9 (1 created, 8 modified)

## Accomplishments
- RecrdJsonContext.cs registered all AST types, pipeline types, interface types, and collection wrappers with metadata mode
- Fixed constructor parameter naming across all AST/pipeline types to PascalCase (enables named-argument call sites)
- Corrected off-by-one bug in SelectorVariableTests boundary test data (64-char vs 65-char string)
- All 40 Recrd.Core.Tests pass; solution builds with 0 warnings; `dotnet format --verify-no-changes` exits 0
- Recrd.Core.csproj has zero Recrd.* ProjectReference or PackageReference entries

## Task Commits

1. **Task 1: Create RecrdJsonContext with full type registration** - `10a6afc` (feat)
2. **Task 2: Run all tests green and verify zero dependencies** - `84817d1` (fix — constructor param alignment + test fixes)

## Files Created/Modified
- `packages/Recrd.Core/Serialization/RecrdJsonContext.cs` - Source-generated JSON context; metadata mode; all 18 type registrations + 5 collection registrations
- `packages/Recrd.Core/Ast/ActionStep.cs` - Constructor params renamed to PascalCase
- `packages/Recrd.Core/Ast/AssertionStep.cs` - Constructor params renamed to PascalCase
- `packages/Recrd.Core/Ast/GroupStep.cs` - Constructor params renamed to PascalCase
- `packages/Recrd.Core/Ast/Selector.cs` - Constructor params renamed to PascalCase
- `packages/Recrd.Core/Ast/Session.cs` - Constructor params renamed to PascalCase
- `packages/Recrd.Core/Ast/SessionMetadata.cs` - Constructor params renamed to PascalCase
- `packages/Recrd.Core/Pipeline/RecordedEvent.cs` - Constructor params renamed to PascalCase
- `tests/Recrd.Core.Tests/SelectorVariableTests.cs` - Fixed 65-char invalid name test data (was 64, which is valid)
- `tests/Recrd.Core.Tests/StepModelTests.cs` - Replaced `Assert.Equal(1, count)` with `Assert.Single` per xUnit2013

## Decisions Made
- GenerationMode=Metadata required: fast-path (Serialization) mode does not support `[JsonPolymorphic]` / `$type` discriminators
- PascalCase constructor parameters: C# named-argument syntax requires parameter names to match exactly; PascalCase aligns with property names

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Constructor parameter names mismatched test call sites**
- **Found during:** Task 2 (Run all tests green)
- **Issue:** Tests used named arguments with PascalCase (`Strategies:`, `AssertionType:`, `GroupType:`, `Id:`, etc.) but constructors had camelCase params; 15 CS1739 errors
- **Fix:** Renamed all constructor parameters to PascalCase in ActionStep, AssertionStep, GroupStep, Selector, Session, SessionMetadata, RecordedEvent; updated body to use `this.Prop = Param` pattern
- **Files modified:** 7 production files in packages/Recrd.Core/Ast/ and packages/Recrd.Core/Pipeline/
- **Verification:** 0 build errors, all tests pass
- **Committed in:** 84817d1

**2. [Rule 1 - Bug] SelectorVariableTests off-by-one in boundary test data**
- **Found during:** Task 2 (Run all tests green)
- **Issue:** `Variable_InvalidName_ThrowsArgumentException` test used 64-char string ("a" + 63 b's) as "invalid" input, but the regex allows up to 64 chars; test expected exception but none was thrown
- **Fix:** Added one 'b' to make it 65 chars (truly exceeds 64-char limit)
- **Files modified:** tests/Recrd.Core.Tests/SelectorVariableTests.cs
- **Verification:** Test now correctly throws ArgumentException; all 40 tests pass
- **Committed in:** 84817d1

**3. [Rule 1 - Bug] StepModelTests xUnit2013 analyzer warning**
- **Found during:** Task 2 (Run all tests green)
- **Issue:** `Assert.Equal(1, groupStep.Steps.Count)` triggers xUnit2013 (should use Assert.Single for collection size)
- **Fix:** Replaced with `Assert.Single(groupStep.Steps)`
- **Files modified:** tests/Recrd.Core.Tests/StepModelTests.cs
- **Verification:** 0 build warnings, analyzer clean
- **Committed in:** 84817d1

---

**Total deviations:** 3 auto-fixed (3 Rule 1 bugs)
**Impact on plan:** All auto-fixes were correctness bugs blocking test suite. No scope creep.

## Issues Encountered
None beyond the auto-fixed bugs above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Recrd.Core package is feature-complete for Phase 02: all 13 CORE requirements satisfied
- RecrdJsonContext ready for consumption by Recrd.Gherkin (AST → .feature), Recrd.Recording (serialize events), and recrd-cli (.recrd file I/O)
- Zero Recrd.* dependency constraint confirmed; CI-enforceable

---
*Phase: 02-core-ast-types-interfaces*
*Completed: 2026-03-26*
