---
phase: 01-monorepo-scaffold-solution-structure
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - recrd.sln
  - Directory.Build.props
  - global.json
autonomous: true
requirements: []
must_haves:
  truths:
    - "recrd.sln exists at repo root in classic Format Version 12.00 (not .slnx)"
    - "Directory.Build.props at repo root sets TargetFramework, Nullable, ImplicitUsings, TreatWarningsAsErrors, LangVersion, RootNamespace"
    - "global.json at repo root pins SDK 10.0.103 with rollForward latestPatch"
  artifacts:
    - path: "recrd.sln"
      provides: "Solution file — entry point for dotnet build/test/restore"
      contains: "Format Version 12.00"
    - path: "Directory.Build.props"
      provides: "Shared MSBuild properties for all projects"
      contains: "<TargetFramework>net10.0</TargetFramework>"
    - path: "global.json"
      provides: "SDK version pin"
      contains: "10.0.103"
  key_links:
    - from: "Directory.Build.props"
      to: "all .csproj files"
      via: "MSBuild auto-import (SDK-style projects inherit parent directory props)"
      pattern: "<TargetFramework>net10.0</TargetFramework>"
    - from: "global.json"
      to: "dotnet CLI"
      via: "SDK resolution — dotnet reads global.json before invoking MSBuild"
      pattern: "rollForward.*latestPatch"
---

<objective>
Create the three repo-root foundation files that every subsequent plan depends on: the solution file (classic .sln format), shared MSBuild properties (Directory.Build.props), and SDK version pin (global.json).

Purpose: Nothing else can be built until these exist. Plans 02–07 all depend on this plan being complete.
Output: recrd.sln, Directory.Build.props, global.json
</objective>

<execution_context>
@/home/gil/dev/recrd/.claude/get-shit-done/workflows/execute-plan.md
@/home/gil/dev/recrd/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@/home/gil/dev/recrd/.planning/PROJECT.md
@/home/gil/dev/recrd/.planning/ROADMAP.md
@/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create Directory.Build.props and global.json</name>
  <files>Directory.Build.props, global.json</files>
  <read_first>
    - /home/gil/dev/recrd/.gitignore (confirm bin/ and obj/ are covered before creating any .NET files)
    - /home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md (Pattern 1 and Pattern 2 — exact verified content)
  </read_first>
  <action>
Create /home/gil/dev/recrd/Directory.Build.props with this exact content:

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

Create /home/gil/dev/recrd/global.json with this exact content:

```json
{
  "sdk": {
    "version": "10.0.103",
    "rollForward": "latestPatch"
  }
}
```

Do NOT add any other properties. The RootNamespace expression handles recrd-cli → recrd.cli correctly — do not override it per-project.
  </action>
  <verify>
    <automated>grep -c "net10.0" /home/gil/dev/recrd/Directory.Build.props && grep -c "TreatWarningsAsErrors" /home/gil/dev/recrd/Directory.Build.props && grep -c "10.0.103" /home/gil/dev/recrd/global.json</automated>
  </verify>
  <acceptance_criteria>
    - /home/gil/dev/recrd/Directory.Build.props exists and contains `<TargetFramework>net10.0</TargetFramework>`
    - /home/gil/dev/recrd/Directory.Build.props contains `<Nullable>enable</Nullable>`
    - /home/gil/dev/recrd/Directory.Build.props contains `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
    - /home/gil/dev/recrd/Directory.Build.props contains `<ImplicitUsings>enable</ImplicitUsings>`
    - /home/gil/dev/recrd/Directory.Build.props contains `<LangVersion>latest</LangVersion>`
    - /home/gil/dev/recrd/Directory.Build.props contains `RootNamespace`
    - /home/gil/dev/recrd/global.json contains `"version": "10.0.103"`
    - /home/gil/dev/recrd/global.json contains `"rollForward": "latestPatch"`
    - `git status` does NOT show any obj/ or bin/ directories
  </acceptance_criteria>
  <done>Both files exist at repo root with the exact content specified. No obj/ or bin/ directories tracked.</done>
</task>

<task type="auto">
  <name>Task 2: Create recrd.sln (classic Format Version 12.00)</name>
  <files>recrd.sln</files>
  <read_first>
    - /home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md (Pattern 3 — classic .sln format, NOT .slnx; solution folder GUIDs)
    - /home/gil/dev/recrd/Directory.Build.props (confirm Task 1 complete)
  </read_first>
  <action>
Create /home/gil/dev/recrd/recrd.sln using the classic Visual Studio solution format (Format Version 12.00). The file must NOT use .slnx XML format — that format hangs dotnet restore on Linux with SDK 10.0.103.

Use `dotnet new sln --name recrd` to create the file, which produces the correct classic format. Do NOT use any `--format slnx` flag.

The solution starts empty. Projects will be added in Plans 02, 03, and 04. The solution folders (apps, packages, tests, plugins) will be added as projects are created.

After creation, verify the first line of recrd.sln reads exactly:
```

Microsoft Visual Studio Solution File, Format Version 12.00
```
(The file starts with a blank line, then that header line.)

Do not add any projects in this task — project additions happen in later plans when the .csproj files exist.
  </action>
  <verify>
    <automated>head -3 /home/gil/dev/recrd/recrd.sln && echo "---" && grep -c "Format Version 12.00" /home/gil/dev/recrd/recrd.sln</automated>
  </verify>
  <acceptance_criteria>
    - /home/gil/dev/recrd/recrd.sln exists
    - recrd.sln contains `Format Version 12.00`
    - recrd.sln does NOT contain `<?xml` (which would indicate .slnx format)
    - `dotnet sln /home/gil/dev/recrd/recrd.sln list` exits 0 (even with empty solution)
  </acceptance_criteria>
  <done>recrd.sln exists at repo root in classic Format Version 12.00. dotnet sln list exits 0.</done>
</task>

</tasks>

<verification>
After both tasks complete:
1. `grep "Format Version 12.00" /home/gil/dev/recrd/recrd.sln` — exits 0
2. `grep "net10.0" /home/gil/dev/recrd/Directory.Build.props` — exits 0
3. `grep "10.0.103" /home/gil/dev/recrd/global.json` — exits 0
4. `git status` shows only recrd.sln, Directory.Build.props, global.json as new files (no obj/ or bin/)
</verification>

<success_criteria>
- recrd.sln at repo root, classic .sln format, dotnet sln list exits 0
- Directory.Build.props at repo root with all 6 properties (TargetFramework, Nullable, ImplicitUsings, TreatWarningsAsErrors, LangVersion, RootNamespace)
- global.json at repo root with SDK 10.0.103 and rollForward: latestPatch
- No build artifacts committed
</success_criteria>

<output>
After completion, create `/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-01-SUMMARY.md`
</output>
