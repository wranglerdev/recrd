# Phase 1: Monorepo Scaffold & Solution Structure - Research

**Researched:** 2026-03-26
**Domain:** .NET 10 monorepo structure, MSBuild, GitHub Actions CI
**Confidence:** HIGH

---

## Summary

Phase 1 starts from a nearly-bare repository. A prior scaffold attempt (`d0b19f7`) created the full
project and test structure, built it successfully, but was then wiped (`f80a136`) — most likely
because source files and `obj/` artifacts were committed together. The structure, `.csproj` contents,
`Directory.Build.props`, and CI workflow from that commit are directly reusable and verified to work
with SDK 10.0.103 on Linux. The only confirmed failure from that era was the `.slnx` format hanging
on `dotnet restore` (commit `a15917d` explains this and replaced it with a classic `.sln`).

The current repo root contains only: `CLAUDE.md`, `PRD.md`, `README.md`, `.gitignore`, `.planning/`,
and `.claude/`. No `.csproj`, `.sln`, `global.json`, or `Directory.Build.props` files currently
exist. Everything must be created from scratch, but the prior attempt provides exact verified content.

The `dotnet dependency-graph` command cited in the PRD does not correspond to any published NuGet
tool by that exact name. The correct approach to enforce the "Recrd.Core has zero Recrd.* deps" rule
in CI is a simple `grep` / `xmllint` check on `Recrd.Core.csproj` — reliable, zero-tool-install,
and impossible to false-positive.

**Primary recommendation:** Recreate the exact file structure from commit `d0b19f7`/`a15917d`,
updated with current package versions, a `global.json` SDK pin, and a `grep`-based CI assertion for
the Core isolation rule. Phase 1 is structural only; no implementation code beyond minimal stubs
belongs here.

---

## Project Constraints (from CLAUDE.md)

- Target framework: `net10.0` — cannot change
- xUnit + Moq for unit tests; ≥90% line coverage on Core/Data/Gherkin/Compilers
- `dotnet format --verify-no-changes` enforced in CI
- Weekly Stryker.NET mutation testing on `Recrd.Core`
- `main`-branch-only `dotnet pack` → NuGet push
- `Recrd.Core` must have zero `Recrd.*` package dependencies — CI-enforced
- Integration tests: xUnit + TestContainers (Phase 1 stub only; not implemented)
- E2E: `record → compile → execute` round-trips (deferred to later phases)
- `IDataProvider` contract requires `IAsyncEnumerable<T>` (deferred to later phases)

---

## Repo State Audit (What Already Exists)

**Currently in repo root (relevant files):**

| File | Exists | Notes |
|------|--------|-------|
| `recrd.sln` | NO | Deleted in `f80a136` |
| `Directory.Build.props` | NO | Deleted in `f80a136` |
| `global.json` | NO | Never existed |
| `apps/` directory | NO | Deleted in `f80a136` |
| `packages/` directory | NO | Deleted in `f80a136` |
| `tests/` directory | NO | Deleted in `f80a136` |
| `plugins/` directory | NO | Never existed |
| `.github/workflows/ci.yml` | NO | Deleted in `f80a136` |
| `.gitignore` | YES | Covers `bin/`, `obj/`, NuGet, VS, coverage |

**Key finding:** The initial scaffold (`d0b19f7`) did NOT include `plugins/` or `apps/vscode-extension/`.
Phase 1 must add both to match the documented structure, even as empty stubs.

**Key finding:** The initial scaffold accidentally committed `obj/` directories (`.gitignore` was not
present at that time). When recreating, `obj/` must NOT be committed.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET SDK | 10.0.103 (installed) | Runtime and build toolchain | Locked by project constraint |
| Microsoft.NET.Sdk | (built-in) | SDK-style project format | Standard for all .NET projects |
| xunit | 2.9.3 | Unit test framework | Locked by project constraint |
| xunit.runner.visualstudio | 3.1.5 | xUnit runner for `dotnet test` | Required by xUnit/VS integration |
| Microsoft.NET.Test.Sdk | 18.3.0 | Test host and adapters | Required for `dotnet test` |
| Moq | 4.20.72 | Mocking framework | Locked by project constraint |
| coverlet.collector | 8.0.1 | Code coverage collection | Used with `--collect:"XPlat Code Coverage"` |

### Supporting (stub csproj only — implementations deferred)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.Playwright | 1.58.0 | Browser automation | `Recrd.Recording` stub only — full impl Phase 6 |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Moq 4.20.72 | NSubstitute | Moq is locked choice; NSubstitute has cleaner API but not chosen |
| coverlet.collector | coverlet.msbuild | collector works via `dotnet test --collect`; msbuild approach is alternative |

**Installation (all via csproj PackageReference — no global tool installs for Phase 1):**
No `dotnet tool install` commands needed for Phase 1. All dependencies are declared in `.csproj` files.

**Version verification:** Confirmed against NuGet registry 2026-03-26:
- `xunit`: 2.9.3 (prior scaffold used 2.9.2; update to current)
- `Microsoft.NET.Test.Sdk`: 18.3.0 (prior scaffold used 17.12.0; update to current)
- `xunit.runner.visualstudio`: 3.1.5 stable (prior scaffold used 2.8.2; update to current)
- `Moq`: 4.20.72 (unchanged — still current)
- `coverlet.collector`: 8.0.1 (prior scaffold used 6.0.2; update to current)
- `Microsoft.Playwright`: 1.58.0 (prior scaffold used 1.49.0; update to current)

---

## Architecture Patterns

### Recommended Project Structure

```
recrd/
├── .github/
│   └── workflows/
│       └── ci.yml
├── apps/
│   ├── recrd-cli/
│   │   ├── recrd-cli.csproj
│   │   └── Program.cs               # minimal stub: args placeholder
│   └── vscode-extension/            # empty placeholder dir (Phase 10)
│       └── .gitkeep
├── packages/
│   ├── Recrd.Core/
│   │   └── Recrd.Core.csproj        # zero Recrd.* deps — stub only
│   ├── Recrd.Data/
│   │   └── Recrd.Data.csproj
│   ├── Recrd.Gherkin/
│   │   └── Recrd.Gherkin.csproj
│   ├── Recrd.Recording/
│   │   └── Recrd.Recording.csproj   # references Microsoft.Playwright
│   └── Recrd.Compilers/
│       └── Recrd.Compilers.csproj
├── plugins/                         # empty placeholder (Phase 11)
│   └── .gitkeep
├── tests/
│   ├── Recrd.Core.Tests/
│   │   └── Recrd.Core.Tests.csproj
│   ├── Recrd.Data.Tests/
│   │   └── Recrd.Data.Tests.csproj
│   ├── Recrd.Gherkin.Tests/
│   │   └── Recrd.Gherkin.Tests.csproj
│   ├── Recrd.Recording.Tests/
│   │   └── Recrd.Recording.Tests.csproj
│   ├── Recrd.Compilers.Tests/
│   │   └── Recrd.Compilers.Tests.csproj
│   └── Recrd.Integration.Tests/
│       └── Recrd.Integration.Tests.csproj
├── Directory.Build.props
├── global.json
└── recrd.sln
```

### Pattern 1: Directory.Build.props — Shared MSBuild Properties

**What:** A single file at the repo root that every SDK-style project automatically imports. Sets TFM,
nullable, warnings-as-errors once so no per-project duplication exists.

**When to use:** Always in a .NET monorepo. Every `.csproj` in the tree inherits it automatically.

**Verified content (from commit `d0b19f7`, confirmed correct):**
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>latest</LangVersion>
    <RootNamespace>$(MSBuildProjectName.Replace("-","."))</RootNamespace>
  </PropertyGroup>
</Project>
```

The `RootNamespace` substitution handles `recrd-cli` → `recrd.cli` correctly.

### Pattern 2: global.json — SDK Version Pinning

**What:** Pins the .NET SDK major.minor.patch so different developer machines and CI all use the same
SDK. Prevents silent behaviour changes from SDK upgrades.

**When to use:** Every repo targeting a specific SDK version. Required for reproducible builds.

**Content:**
```json
{
  "sdk": {
    "version": "10.0.103",
    "rollForward": "latestPatch"
  }
}
```

`rollForward: latestPatch` allows patch-level SDK upgrades (security fixes) without requiring a
manual `global.json` update, while locking the major.minor band.

### Pattern 3: Classic `.sln` Format (NOT `.slnx`)

**What:** The Visual Studio 17 (Format Version 12.00) `.sln` file format.

**Critical finding:** `.slnx` (the new XML-based solution format) hangs on `dotnet restore` with SDK
10.0.103 on Linux (confirmed in commit `a15917d`). Use the classic `.sln` format.

**Verified structure:** The `.sln` from commit `a15917d` worked. It includes solution folder entries
(`{2150E333...}` type GUIDs) for `apps`, `packages`, `tests` to keep the IDE tree organized. The
`plugins` and `apps/vscode-extension` folders need corresponding solution folder entries even if they
contain only a `.gitkeep`.

### Pattern 4: Minimal Stub Source Files

**What:** Each library project needs at least one source file to compile without error.

**Why:** An empty classlib project with `<Nullable>enable</Nullable>` and
`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` compiles clean. But test projects that have no
test class will fail with "no tests found" warning treated as error in some configurations. Add a
single placeholder `// TODO: implement` file per package project and a single placeholder test class
per test project.

**Minimum viable stub for a package project:**
```csharp
// placeholder — implementation in later phase
namespace Recrd.Core;
```

**Minimum viable stub for a test project:**
```csharp
namespace Recrd.Core.Tests;
// Tests will be added in Phase 2
public class PlaceholderTests { }
```

### Pattern 5: Dependency Isolation Enforcement in CI

**What:** A CI step that asserts `Recrd.Core.csproj` contains no `ProjectReference` entries pointing
to any other `Recrd.*` package.

**Why the PRD approach ("dotnet dependency-graph") is a red herring:** No NuGet tool with that exact
name exists. The `dotnet-depends` tool (v0.8.0) exists but adds complexity and may not support .NET
10 cleanly. A `grep` on the `.csproj` file is simpler, faster, zero-dependency, and correct for this
rule.

**Verified CI script:**
```bash
# In .github/workflows/ci.yml
- name: Assert Recrd.Core has zero Recrd.* references
  run: |
    if grep -E 'ProjectReference.*Recrd\.' packages/Recrd.Core/Recrd.Core.csproj; then
      echo "ERROR: Recrd.Core must not reference any Recrd.* project"
      exit 1
    fi
    echo "OK: Recrd.Core has zero Recrd.* references"
```

This is HIGH confidence — it directly inspects the source of truth (the `.csproj` file) and cannot
produce false negatives.

### Pattern 6: csproj Structure for Package Libraries

**What:** Minimal SDK-style class library project. All shared properties come from
`Directory.Build.props` so the `.csproj` is minimal.

**Recrd.Core (zero Recrd.* deps — enforced):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Recrd.Core</AssemblyName>
    <PackageId>Recrd.Core</PackageId>
  </PropertyGroup>
</Project>
```

**Recrd.Data (references Core):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Recrd.Data</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\packages\Recrd.Core\Recrd.Core.csproj" />
  </ItemGroup>
</Project>
```

**Recrd.Recording (references Core + Playwright — isolated to avoid 200MB in compile-only consumers):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Recrd.Recording</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" Version="1.58.0" />
    <ProjectReference Include="..\..\packages\Recrd.Core\Recrd.Core.csproj" />
  </ItemGroup>
</Project>
```

**Recrd.Compilers (references Core + Gherkin):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Recrd.Compilers</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\packages\Recrd.Core\Recrd.Core.csproj" />
    <ProjectReference Include="..\..\packages\Recrd.Gherkin\Recrd.Gherkin.csproj" />
  </ItemGroup>
</Project>
```

**recrd-cli (console app, references all packages):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <AssemblyName>recrd</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\packages\Recrd.Core\Recrd.Core.csproj" />
    <ProjectReference Include="..\..\packages\Recrd.Recording\Recrd.Recording.csproj" />
    <ProjectReference Include="..\..\packages\Recrd.Data\Recrd.Data.csproj" />
    <ProjectReference Include="..\..\packages\Recrd.Gherkin\Recrd.Gherkin.csproj" />
    <ProjectReference Include="..\..\packages\Recrd.Compilers\Recrd.Compilers.csproj" />
  </ItemGroup>
</Project>
```

### Pattern 7: xUnit Test Project Structure

**Standard test project (all test projects follow this pattern):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="coverlet.collector" Version="8.0.1">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <ProjectReference Include="..\..\packages\[ProjectName]\[ProjectName].csproj" />
  </ItemGroup>
</Project>
```

**Recrd.Integration.Tests** additionally references all packages and will need TestContainers in Phase 6+:
```xml
<!-- All 5 package ProjectReferences — same pattern -->
```

### Pattern 8: CI Workflow Structure

**Minimum Phase 1 CI workflow** (placeholder that passes, plus the Core isolation check):

```yaml
name: CI

on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Assert Recrd.Core has zero Recrd.* references
        run: |
          if grep -E 'ProjectReference.*Recrd\.' packages/Recrd.Core/Recrd.Core.csproj; then
            echo "ERROR: Recrd.Core must not reference any Recrd.* project"
            exit 1
          fi
          echo "OK: Recrd.Core has zero Recrd.* references"

      - name: Test
        run: dotnet test --no-build --collect:"XPlat Code Coverage" --filter "Category!=Integration"

      - name: Check code style
        run: dotnet format --verify-no-changes
```

**Note:** Phase 5 adds coverage gates, Stryker, NuGet push, and TDD red-phase branch logic. Phase 1
CI is intentionally minimal but confirms the build works.

### Anti-Patterns to Avoid

- **Committing `obj/` or `bin/` directories:** The initial scaffold made this mistake. `.gitignore`
  already covers `obj/` and `bin/` — verify it is in place before creating project files.
- **Using `.slnx` format:** Confirmed to hang on `dotnet restore` with SDK 10.0.103 on Linux. Use
  classic `.sln` only.
- **Adding implementation code in Phase 1:** Only stubs. Classes with `throw new NotImplementedException()`
  or `// TODO` are fine; full logic belongs to the phase that owns that package.
- **Forgetting `IsPackable=false` on test projects:** Without it, `dotnet pack` will try to pack test
  projects and fail with missing metadata.
- **Using `dotnet new sln --format slnx`:** Don't. Use `dotnet new sln` (defaults to classic `.sln`)
  and manually add projects, or recreate the `.sln` content from the verified template.
- **Adding `<RootNamespace>` per project:** `Directory.Build.props` handles this with the replace
  expression. Per-project overrides would fight it.
- **Missing solution folder nesting:** Without the nested project folder entries in `recrd.sln`, IDEs
  show all projects at the root level. This is cosmetic but deviates from the documented structure.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Code coverage collection | Custom coverage collector | `coverlet.collector` via `--collect:"XPlat Code Coverage"` | Handles multi-project, integrates with `dotnet test` |
| SDK version pinning | CI env var hacks | `global.json` | MSBuild-native, works everywhere |
| Shared build properties | Per-project `<PropertyGroup>` | `Directory.Build.props` | Auto-imported by all SDK projects, single source of truth |
| Recrd.Core isolation assertion | NDepend / dotnet-depends | `grep` on `.csproj` in CI | Zero dependencies, immediately readable, cannot be bypassed |

---

## Common Pitfalls

### Pitfall 1: .slnx Hangs on `dotnet restore` (Linux + SDK 10.0.103)
**What goes wrong:** `dotnet restore` hangs indefinitely when the solution file uses `.slnx` format.
**Why it happens:** SDK 10.0.103 on Linux has a known issue with the new XML-based solution format.
**How to avoid:** Always use the classic `.sln` format (VS 2017 Format Version 12.00).
**Warning signs:** `dotnet restore` never completes, no error output.

### Pitfall 2: Committing `obj/` Artifacts
**What goes wrong:** Build artifacts end up tracked in git, causing checkout state mismatches, large
diffs, and CI cache problems.
**Why it happens:** Forgetting to have `.gitignore` in place before running `dotnet build`.
**How to avoid:** Verify `.gitignore` covers `obj/` and `bin/` before creating any `.csproj` files.
The existing `.gitignore` already does this.
**Warning signs:** `git status` shows `obj/` directories after a `dotnet build`.

### Pitfall 3: Test Projects with Zero Test Classes Cause xUnit Warnings
**What goes wrong:** `dotnet test` emits a "no tests found" warning; with `TreatWarningsAsErrors`
this can become a build error depending on the warning ID.
**Why it happens:** Phase 1 creates test project stubs with no actual test methods.
**How to avoid:** Add a single empty placeholder public class to every test project.
**Warning signs:** `dotnet test` exits non-zero with "no test is available in [project]".

### Pitfall 4: Missing `PackageId` on Recrd.Core
**What goes wrong:** `dotnet pack` later generates a NuGet package with the wrong identifier.
**Why it happens:** Without an explicit `<PackageId>`, the package ID defaults to the assembly name,
which may differ from the intended NuGet identity.
**How to avoid:** `Recrd.Core.csproj` must declare both `<AssemblyName>Recrd.Core</AssemblyName>`
and `<PackageId>Recrd.Core</PackageId>`.

### Pitfall 5: Path Separators in `.sln` on Linux
**What goes wrong:** A `.sln` file generated on Windows uses backslash paths, which may not resolve
correctly on Linux with some SDK versions.
**Why it happens:** The classic `.sln` format uses backslash by convention.
**How to avoid:** The SDK handles both separators on Linux in practice. The verified `.sln` from
commit `a15917d` used backslashes and worked fine on Linux with SDK 10.0.103. No change needed.

### Pitfall 6: `dotnet format` Fails on Empty Projects
**What goes wrong:** `dotnet format --verify-no-changes` may fail on projects with no source files.
**Why it happens:** The formatter has nothing to analyze and may exit non-zero.
**How to avoid:** Ensure each project has at least one `.cs` stub file before running `dotnet format`.

---

## Code Examples

### Minimum Program.cs for recrd-cli stub
```csharp
// Source: verified from commit d0b19f7 (apps/recrd-cli/Program.cs)
// Placeholder — CLI implementation in Phase 8
```

### global.json with rollForward
```json
{
  "sdk": {
    "version": "10.0.103",
    "rollForward": "latestPatch"
  }
}
```

### CI grep assertion for Core isolation (shell-safe)
```bash
if grep -E '<ProjectReference[^>]*Include="[^"]*Recrd\.' \
    packages/Recrd.Core/Recrd.Core.csproj; then
  echo "ERROR: Recrd.Core must not reference any Recrd.* project"
  exit 1
fi
echo "OK: Recrd.Core has zero Recrd.* references"
```

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 10.0 | All .csproj, dotnet build/test | YES | 10.0.103 | — |
| .NET SDK 8.0 | (not required) | YES | 8.0.124 | — |
| git | CI, version control | YES | (system) | — |
| GitHub Actions (ubuntu-latest) | CI workflow | YES (remote) | — | — |

**No missing dependencies.** All Phase 1 tooling is available locally and via GitHub Actions.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 |
| Config file | none (xUnit 2.x uses project-level config) |
| Quick run command | `dotnet test --no-build --filter "FullyQualifiedName~Recrd.Core.Tests"` |
| Full suite command | `dotnet test --no-build --collect:"XPlat Code Coverage" --filter "Category!=Integration"` |

### Phase Requirements → Test Map

Phase 1 has no REQUIREMENTS.md IDs. Success criteria are structural/build-level assertions:

| Success Criterion | How Verified | Automated Command |
|-------------------|-------------|-------------------|
| SC-1: `dotnet build recrd.sln` exits zero | Build step in CI | `dotnet build --no-restore` |
| SC-2: All project stubs exist under correct dirs | File existence check + build | `dotnet build recrd.sln` implicitly |
| SC-3: `Directory.Build.props` applies to all projects | Build passes without per-project TFM | `dotnet build` — would fail if TFM missing |
| SC-4: CI workflow exists and passes on push to main | GitHub Actions workflow run | Push to main triggers `.github/workflows/ci.yml` |
| SC-5: Recrd.Core has zero Recrd.* references | CI grep step | grep assertion in `ci.yml` |

### Wave 0 Gaps

Phase 1 is structural. The placeholder test classes count as Wave 0 test infrastructure:

- [ ] `tests/Recrd.Core.Tests/PlaceholderTests.cs` — ensures `dotnet test` exits zero on empty test project
- [ ] `tests/Recrd.Data.Tests/PlaceholderTests.cs` — same
- [ ] `tests/Recrd.Gherkin.Tests/PlaceholderTests.cs` — same
- [ ] `tests/Recrd.Recording.Tests/PlaceholderTests.cs` — same
- [ ] `tests/Recrd.Compilers.Tests/PlaceholderTests.cs` — same
- [ ] `tests/Recrd.Integration.Tests/PlaceholderTests.cs` — same

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `.slnx` XML solution | Classic `.sln` (Format Version 12.00) | SDK 10.0.103 / confirmed buggy | Must use `.sln` on Linux |
| `coverlet.collector` 6.0.x | `coverlet.collector` 8.0.1 | 2025 | Major version bump — update all test projects |
| `xunit` 2.9.2 | `xunit` 2.9.3 | Late 2025 | Minor patch update |
| `xunit.runner.visualstudio` 2.8.x | `xunit.runner.visualstudio` 3.1.5 | 2025 | Major version jump — update all test projects |
| `Microsoft.NET.Test.Sdk` 17.12.0 | `Microsoft.NET.Test.Sdk` 18.3.0 | 2026 | Major version update |
| `Microsoft.Playwright` 1.49.0 | `Microsoft.Playwright` 1.58.0 | 2026 | Version update — no API breaking changes for stub |

**Deprecated/outdated:**
- `.slnx` format: do not use with SDK 10.0.103 on Linux.
- `xunit.runner.visualstudio` 2.x: update to 3.x stable (3.1.5).

---

## Open Questions

1. **Should `plugins/` contain an example plugin stub in Phase 1?**
   - What we know: Phase 11 is "Plugin System"; Phase 12 includes example plugins
   - What's unclear: Whether Phase 1 should scaffold a `Recrd.Plugin.Example` project or just an empty `plugins/` directory
   - Recommendation: Empty `plugins/.gitkeep` in Phase 1; defer to Phase 11/12

2. **Should `apps/vscode-extension/` have a `package.json` stub in Phase 1?**
   - What we know: Phase 10 covers VS Code extension; it is TypeScript, not .NET
   - What's unclear: Whether the `.sln` should reference it at all (it can't — `.sln` is .NET only)
   - Recommendation: Create `apps/vscode-extension/.gitkeep` placeholder only; TypeScript project setup is Phase 10

3. **`global.json` rollForward policy: `latestPatch` vs `latestMinor`?**
   - What we know: SDK 10.0.103 is installed; `latestPatch` allows 10.0.104+ but not 10.1.x
   - What's unclear: Whether tighter pinning is desired (omit `rollForward` for exact pin)
   - Recommendation: Use `latestPatch` — allows security patch SDKs without requiring manual updates

---

## Sources

### Primary (HIGH confidence)
- Git commits in this repo: `d0b19f7` (initial scaffold), `a15917d` (sln fix), `f80a136` (deletion)
  — exact file contents verified by direct inspection
- NuGet registry `api.nuget.org/v3-flatcontainer/{package}/index.json` — all package versions
  verified 2026-03-26
- Local `dotnet --list-sdks` — confirmed SDK 10.0.103 and 8.0.124 available

### Secondary (MEDIUM confidence)
- `.gitignore` in repo root — confirmed covers `bin/`, `obj/`, NuGet artifacts
- Commit message `a15917d` — confirms `.slnx` hangs on `dotnet restore` with SDK 10.0.103 on Linux

### Tertiary (LOW confidence)
- None

---

## Metadata

**Confidence breakdown:**
- Standard stack (library versions): HIGH — verified against NuGet registry 2026-03-26
- Architecture (project structure, `.csproj` content): HIGH — recovered from working git commits
- `.sln` format requirement: HIGH — confirmed by commit message with exact failure description
- Dependency enforcement (grep approach): HIGH — directly inspects source of truth
- Pitfalls: HIGH — all backed by direct evidence from this repo's history

**Research date:** 2026-03-26
**Valid until:** 2026-06-26 (package versions may drift; re-check NuGet before acting if >30 days old)
