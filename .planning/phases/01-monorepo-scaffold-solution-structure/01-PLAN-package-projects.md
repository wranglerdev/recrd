---
phase: 01-monorepo-scaffold-solution-structure
plan: 02
type: execute
wave: 2
depends_on:
  - 01-PLAN-solution-scaffold
files_modified:
  - packages/Recrd.Core/Recrd.Core.csproj
  - packages/Recrd.Core/Placeholder.cs
  - packages/Recrd.Data/Recrd.Data.csproj
  - packages/Recrd.Data/Placeholder.cs
  - packages/Recrd.Gherkin/Recrd.Gherkin.csproj
  - packages/Recrd.Gherkin/Placeholder.cs
  - packages/Recrd.Recording/Recrd.Recording.csproj
  - packages/Recrd.Recording/Placeholder.cs
  - packages/Recrd.Compilers/Recrd.Compilers.csproj
  - packages/Recrd.Compilers/Placeholder.cs
  - recrd.sln
autonomous: true
requirements: []
must_haves:
  truths:
    - "All 5 package projects exist under packages/ and build without errors"
    - "Recrd.Core.csproj contains zero ProjectReference or PackageReference entries pointing to Recrd.*"
    - "Recrd.Recording.csproj references Microsoft.Playwright 1.58.0 and is the only package to do so"
    - "Recrd.Data, Recrd.Gherkin, Recrd.Compilers each reference Recrd.Core via ProjectReference"
    - "All 5 packages are registered in recrd.sln"
  artifacts:
    - path: "packages/Recrd.Core/Recrd.Core.csproj"
      provides: "Core library stub — zero Recrd.* deps"
      contains: "AssemblyName>Recrd.Core"
    - path: "packages/Recrd.Recording/Recrd.Recording.csproj"
      provides: "Recording engine stub — isolated Playwright dep"
      contains: "Microsoft.Playwright"
    - path: "packages/Recrd.Compilers/Recrd.Compilers.csproj"
      provides: "Compilers stub — references Core and Gherkin"
      contains: "Recrd.Gherkin"
  key_links:
    - from: "packages/Recrd.Data/Recrd.Data.csproj"
      to: "packages/Recrd.Core/Recrd.Core.csproj"
      via: "ProjectReference"
      pattern: "ProjectReference.*Recrd.Core"
    - from: "packages/Recrd.Core/Recrd.Core.csproj"
      to: "(nothing)"
      via: "no ProjectReference to any Recrd.*"
      pattern: "grep -E 'ProjectReference.*Recrd\\.' returns empty"
---

<objective>
Create the 5 package library project stubs under packages/ and register them in recrd.sln. Each project gets a minimal .csproj and a single placeholder .cs file so dotnet build succeeds without warnings.

Purpose: Package projects define the compilation units that all later phases implement. They must exist and build clean before test projects can reference them (Plan 04) and before the CLI app can reference them (Plan 03).
Output: 5 .csproj files + 5 Placeholder.cs stubs + recrd.sln updated with all 5 projects
</objective>

<execution_context>
@/home/gil/dev/recrd/.claude/get-shit-done/workflows/execute-plan.md
@/home/gil/dev/recrd/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md
@/home/gil/dev/recrd/Directory.Build.props
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create Recrd.Core and Recrd.Data package stubs</name>
  <files>
    packages/Recrd.Core/Recrd.Core.csproj,
    packages/Recrd.Core/Placeholder.cs,
    packages/Recrd.Data/Recrd.Data.csproj,
    packages/Recrd.Data/Placeholder.cs
  </files>
  <read_first>
    - /home/gil/dev/recrd/Directory.Build.props (confirms shared properties — do NOT duplicate TargetFramework per-project)
    - /home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md (Pattern 6 — exact .csproj content for Recrd.Core and Recrd.Data; Pitfall 4 — PackageId required on Core)
  </read_first>
  <action>
Create directory /home/gil/dev/recrd/packages/Recrd.Core/ and write Recrd.Core.csproj:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Recrd.Core</AssemblyName>
    <PackageId>Recrd.Core</PackageId>
  </PropertyGroup>
</Project>
```

CRITICAL: No PackageReference entries. No ProjectReference entries. Recrd.Core must have zero Recrd.* dependencies — this is CI-enforced.

Create /home/gil/dev/recrd/packages/Recrd.Core/Placeholder.cs:

```csharp
// placeholder — implementation in Phase 2
namespace Recrd.Core;
```

Create directory /home/gil/dev/recrd/packages/Recrd.Data/ and write Recrd.Data.csproj:

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

Create /home/gil/dev/recrd/packages/Recrd.Data/Placeholder.cs:

```csharp
// placeholder — implementation in Phase 3
namespace Recrd.Data;
```
  </action>
  <verify>
    <automated>cd /home/gil/dev/recrd && dotnet build packages/Recrd.Data/Recrd.Data.csproj --no-restore 2>&1 | tail -5</automated>
  </verify>
  <acceptance_criteria>
    - /home/gil/dev/recrd/packages/Recrd.Core/Recrd.Core.csproj exists and contains `<AssemblyName>Recrd.Core</AssemblyName>`
    - /home/gil/dev/recrd/packages/Recrd.Core/Recrd.Core.csproj contains `<PackageId>Recrd.Core</PackageId>`
    - `grep -E 'ProjectReference|PackageReference' /home/gil/dev/recrd/packages/Recrd.Core/Recrd.Core.csproj` returns empty (zero Recrd.* deps)
    - /home/gil/dev/recrd/packages/Recrd.Data/Recrd.Data.csproj contains `ProjectReference` pointing to `Recrd.Core.csproj`
    - `dotnet build packages/Recrd.Data/Recrd.Data.csproj` exits 0 (implicitly builds Core too)
    - No obj/ directories appear in `git status`
  </acceptance_criteria>
  <done>Recrd.Core and Recrd.Data build clean. Core has zero references. Data references Core.</done>
</task>

<task type="auto">
  <name>Task 2: Create Recrd.Gherkin, Recrd.Recording, and Recrd.Compilers stubs</name>
  <files>
    packages/Recrd.Gherkin/Recrd.Gherkin.csproj,
    packages/Recrd.Gherkin/Placeholder.cs,
    packages/Recrd.Recording/Recrd.Recording.csproj,
    packages/Recrd.Recording/Placeholder.cs,
    packages/Recrd.Compilers/Recrd.Compilers.csproj,
    packages/Recrd.Compilers/Placeholder.cs
  </files>
  <read_first>
    - /home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md (Pattern 6 — exact .csproj for Recrd.Gherkin, Recrd.Recording with Playwright 1.58.0, Recrd.Compilers referencing Core+Gherkin)
    - /home/gil/dev/recrd/packages/Recrd.Core/Recrd.Core.csproj (confirm Core exists before referencing it)
  </read_first>
  <action>
Create /home/gil/dev/recrd/packages/Recrd.Gherkin/Recrd.Gherkin.csproj:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Recrd.Gherkin</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\packages\Recrd.Core\Recrd.Core.csproj" />
  </ItemGroup>
</Project>
```

Create /home/gil/dev/recrd/packages/Recrd.Gherkin/Placeholder.cs:

```csharp
// placeholder — implementation in Phase 4
namespace Recrd.Gherkin;
```

Create /home/gil/dev/recrd/packages/Recrd.Recording/Recrd.Recording.csproj:

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

NOTE: Recrd.Recording is the ONLY package project that references Microsoft.Playwright. This isolation prevents Playwright's ~200MB browser binaries from being pulled into compile-only consumers (Data, Gherkin, Compilers, Core).

Create /home/gil/dev/recrd/packages/Recrd.Recording/Placeholder.cs:

```csharp
// placeholder — implementation in Phase 6
namespace Recrd.Recording;
```

Create /home/gil/dev/recrd/packages/Recrd.Compilers/Recrd.Compilers.csproj:

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

Create /home/gil/dev/recrd/packages/Recrd.Compilers/Placeholder.cs:

```csharp
// placeholder — implementation in Phase 7
namespace Recrd.Compilers;
```
  </action>
  <verify>
    <automated>cd /home/gil/dev/recrd && dotnet build packages/Recrd.Compilers/Recrd.Compilers.csproj --no-restore 2>&1 | tail -5</automated>
  </verify>
  <acceptance_criteria>
    - /home/gil/dev/recrd/packages/Recrd.Gherkin/Recrd.Gherkin.csproj contains `ProjectReference` to `Recrd.Core.csproj`
    - /home/gil/dev/recrd/packages/Recrd.Recording/Recrd.Recording.csproj contains `Microsoft.Playwright" Version="1.58.0"`
    - /home/gil/dev/recrd/packages/Recrd.Compilers/Recrd.Compilers.csproj contains ProjectReferences to both `Recrd.Core.csproj` and `Recrd.Gherkin.csproj`
    - `dotnet build packages/Recrd.Compilers/Recrd.Compilers.csproj` exits 0
    - `dotnet build packages/Recrd.Recording/Recrd.Recording.csproj` exits 0
    - No obj/ directories in `git status`
  </acceptance_criteria>
  <done>All 5 package projects build clean. Recrd.Compilers depends on Core and Gherkin. Recrd.Recording is the sole Playwright consumer.</done>
</task>

<task type="auto">
  <name>Task 3: Register all 5 package projects in recrd.sln</name>
  <files>recrd.sln</files>
  <read_first>
    - /home/gil/dev/recrd/recrd.sln (current state — must be empty solution from Plan 01)
    - /home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md (Pattern 3 — solution folder nesting; backslash paths work on Linux with SDK 10.0.103)
  </read_first>
  <action>
Add all 5 package projects to recrd.sln using the dotnet sln add command. Run from /home/gil/dev/recrd:

```bash
dotnet sln recrd.sln add packages/Recrd.Core/Recrd.Core.csproj \
  packages/Recrd.Data/Recrd.Data.csproj \
  packages/Recrd.Gherkin/Recrd.Gherkin.csproj \
  packages/Recrd.Recording/Recrd.Recording.csproj \
  packages/Recrd.Compilers/Recrd.Compilers.csproj \
  --solution-folder packages
```

The `--solution-folder packages` flag places all 5 projects under a "packages" solution folder in IDEs, matching the documented monorepo structure.

After adding, verify with `dotnet sln recrd.sln list` — it must show all 5 projects.
  </action>
  <verify>
    <automated>cd /home/gil/dev/recrd && dotnet sln recrd.sln list</automated>
  </verify>
  <acceptance_criteria>
    - `dotnet sln recrd.sln list` output contains `packages/Recrd.Core/Recrd.Core.csproj`
    - `dotnet sln recrd.sln list` output contains `packages/Recrd.Data/Recrd.Data.csproj`
    - `dotnet sln recrd.sln list` output contains `packages/Recrd.Gherkin/Recrd.Gherkin.csproj`
    - `dotnet sln recrd.sln list` output contains `packages/Recrd.Recording/Recrd.Recording.csproj`
    - `dotnet sln recrd.sln list` output contains `packages/Recrd.Compilers/Recrd.Compilers.csproj`
    - `dotnet build recrd.sln` (with only these 5 projects) exits 0
  </acceptance_criteria>
  <done>recrd.sln contains all 5 package projects. dotnet build recrd.sln exits 0 with only these projects registered.</done>
</task>

</tasks>

<verification>
After all tasks:
1. `grep -E 'ProjectReference.*Recrd\.' /home/gil/dev/recrd/packages/Recrd.Core/Recrd.Core.csproj` returns empty (Core isolation intact)
2. `dotnet sln /home/gil/dev/recrd/recrd.sln list | wc -l` shows at least 5 entries
3. `dotnet build /home/gil/dev/recrd/recrd.sln` exits 0
</verification>

<success_criteria>
- 5 .csproj files created under packages/ with correct content
- 5 Placeholder.cs stubs created (one per package)
- All 5 registered in recrd.sln under packages solution folder
- dotnet build recrd.sln exits 0
- Recrd.Core has zero ProjectReference or PackageReference entries
</success_criteria>

<output>
After completion, create `/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-02-SUMMARY.md`
</output>
