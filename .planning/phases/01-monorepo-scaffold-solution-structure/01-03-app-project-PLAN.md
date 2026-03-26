---
phase: 01-monorepo-scaffold-solution-structure
plan: 03
type: execute
wave: 2
depends_on:
  - 01-PLAN-solution-scaffold
files_modified:
  - apps/recrd-cli/recrd-cli.csproj
  - apps/recrd-cli/Program.cs
  - recrd.sln
autonomous: true
requirements: []
must_haves:
  truths:
    - "apps/recrd-cli/ exists with a console app .csproj and a Program.cs stub"
    - "recrd-cli.csproj has OutputType Exe and AssemblyName recrd"
    - "recrd-cli is registered in recrd.sln under an apps solution folder"
    - "dotnet build apps/recrd-cli/recrd-cli.csproj exits 0"
  artifacts:
    - path: "apps/recrd-cli/recrd-cli.csproj"
      provides: "Console app entry point stub"
      contains: "<OutputType>Exe</OutputType>"
    - path: "apps/recrd-cli/Program.cs"
      provides: "Minimal CLI stub that compiles"
      contains: "Placeholder"
  key_links:
    - from: "apps/recrd-cli/recrd-cli.csproj"
      to: "packages/Recrd.Core/Recrd.Core.csproj"
      via: "ProjectReference (will reference all 5 packages once they exist)"
      pattern: "ProjectReference.*Recrd.Core"
---

<objective>
Create the recrd-cli console app stub under apps/ and register it in recrd.sln. The CLI references all 5 package projects — this is the composition root for the entire tool.

Purpose: The CLI is the entry point that ties all packages together. Test plan 04 does not need this, but CI needs it in the solution to verify the full dependency graph builds.
Output: apps/recrd-cli/recrd-cli.csproj, apps/recrd-cli/Program.cs, recrd.sln updated
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
  <name>Task 1: Create recrd-cli console app stub</name>
  <files>apps/recrd-cli/recrd-cli.csproj, apps/recrd-cli/Program.cs</files>
  <read_first>
    - /home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md (Pattern 6 — recrd-cli .csproj content with OutputType Exe, AssemblyName recrd, all 5 ProjectReferences)
    - /home/gil/dev/recrd/Directory.Build.props (confirm TargetFramework not needed per-project; RootNamespace replace expression handles recrd-cli → recrd.cli)
  </read_first>
  <action>
Create directory /home/gil/dev/recrd/apps/recrd-cli/ and write recrd-cli.csproj:

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

NOTE: AssemblyName is `recrd` (not `recrd-cli`) so the compiled binary is named `recrd`. The RootNamespace from Directory.Build.props will be `recrd.cli` (hyphen replaced by dot) — that is correct for the CLI namespace.

Create /home/gil/dev/recrd/apps/recrd-cli/Program.cs:

```csharp
// placeholder — CLI implementation in Phase 8
// recrd: record browser interactions, emit pt-BR Gherkin, compile Robot Framework suites
```

The Program.cs stub is intentionally empty (no Main method) because .NET 10 top-level statements require at least one statement to compile. Use a comment-only file OR add a minimal statement:

```csharp
// placeholder — CLI implementation in Phase 8
```

If the compiler requires at least one statement due to implicit Program class requirements, use:

```csharp
// placeholder — CLI implementation in Phase 8
_ = args;
```

Whichever compiles without error or warning with TreatWarningsAsErrors=true.
  </action>
  <verify>
    <automated>cd /home/gil/dev/recrd && dotnet build apps/recrd-cli/recrd-cli.csproj --no-restore 2>&1 | tail -5</automated>
  </verify>
  <acceptance_criteria>
    - /home/gil/dev/recrd/apps/recrd-cli/recrd-cli.csproj exists and contains `<OutputType>Exe</OutputType>`
    - /home/gil/dev/recrd/apps/recrd-cli/recrd-cli.csproj contains `<AssemblyName>recrd</AssemblyName>`
    - /home/gil/dev/recrd/apps/recrd-cli/recrd-cli.csproj contains 5 ProjectReference entries (Core, Recording, Data, Gherkin, Compilers)
    - /home/gil/dev/recrd/apps/recrd-cli/Program.cs exists
    - `dotnet build apps/recrd-cli/recrd-cli.csproj` exits 0 with no errors and no warnings
  </acceptance_criteria>
  <done>recrd-cli.csproj builds clean as a console app referencing all 5 package projects.</done>
</task>

<task type="auto">
  <name>Task 2: Register recrd-cli in recrd.sln</name>
  <files>recrd.sln</files>
  <read_first>
    - /home/gil/dev/recrd/recrd.sln (current state — must contain the 5 package projects from Plan 02 if run after it, OR just the empty solution if run in parallel)
    - /home/gil/dev/recrd/apps/recrd-cli/recrd-cli.csproj (confirm Task 1 complete)
  </read_first>
  <action>
Add recrd-cli to the solution under an "apps" solution folder. Run from /home/gil/dev/recrd:

```bash
dotnet sln recrd.sln add apps/recrd-cli/recrd-cli.csproj --solution-folder apps
```

Verify with `dotnet sln recrd.sln list` — recrd-cli must appear in the list.

NOTE: This plan runs in Wave 2 parallel with Plan 02 (package projects). If Plan 02 has not yet added the package projects to the sln, that is fine — each plan adds only its own project(s). The final sln state will be complete after all Wave 2 plans finish.
  </action>
  <verify>
    <automated>cd /home/gil/dev/recrd && dotnet sln recrd.sln list | grep recrd-cli</automated>
  </verify>
  <acceptance_criteria>
    - `dotnet sln /home/gil/dev/recrd/recrd.sln list` output contains `apps/recrd-cli/recrd-cli.csproj`
    - recrd.sln contains a solution folder entry for "apps"
  </acceptance_criteria>
  <done>recrd-cli registered in recrd.sln under apps solution folder.</done>
</task>

</tasks>

<verification>
After all tasks:
1. `dotnet build /home/gil/dev/recrd/apps/recrd-cli/recrd-cli.csproj` exits 0
2. `dotnet sln /home/gil/dev/recrd/recrd.sln list | grep recrd-cli` exits 0
3. `cat /home/gil/dev/recrd/apps/recrd-cli/recrd-cli.csproj | grep AssemblyName` shows `recrd`
</verification>

<success_criteria>
- apps/recrd-cli/recrd-cli.csproj exists with OutputType Exe, AssemblyName recrd, 5 ProjectReferences
- apps/recrd-cli/Program.cs exists and compiles without errors or warnings
- recrd-cli registered in recrd.sln under apps solution folder
- dotnet build exits 0
</success_criteria>

<output>
After completion, create `/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-03-SUMMARY.md`
</output>
