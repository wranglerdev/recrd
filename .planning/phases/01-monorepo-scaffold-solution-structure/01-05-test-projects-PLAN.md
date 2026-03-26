---
phase: 01-monorepo-scaffold-solution-structure
plan: 05
type: execute
wave: 3
depends_on:
  - 01-PLAN-package-projects
  - 01-PLAN-app-project
files_modified:
  - tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj
  - tests/Recrd.Core.Tests/PlaceholderTests.cs
  - tests/Recrd.Data.Tests/Recrd.Data.Tests.csproj
  - tests/Recrd.Data.Tests/PlaceholderTests.cs
  - tests/Recrd.Gherkin.Tests/Recrd.Gherkin.Tests.csproj
  - tests/Recrd.Gherkin.Tests/PlaceholderTests.cs
  - tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj
  - tests/Recrd.Recording.Tests/PlaceholderTests.cs
  - tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj
  - tests/Recrd.Compilers.Tests/PlaceholderTests.cs
  - tests/Recrd.Integration.Tests/Recrd.Integration.Tests.csproj
  - tests/Recrd.Integration.Tests/PlaceholderTests.cs
  - recrd.sln
autonomous: true
requirements: []
must_haves:
  truths:
    - "6 test projects exist under tests/, each mirroring a package or integration scope"
    - "All test projects declare IsPackable=false to prevent dotnet pack from including them"
    - "Each test project has xunit 2.9.3, Microsoft.NET.Test.Sdk 18.3.0, xunit.runner.visualstudio 3.1.5, Moq 4.20.72, coverlet.collector 8.0.1"
    - "Each test project has a PlaceholderTests.cs with an empty public class so dotnet test exits 0"
    - "dotnet test recrd.sln --no-build exits 0 (all placeholder test classes pass — zero failures)"
    - "All 6 test projects are registered in recrd.sln under a tests solution folder"
  artifacts:
    - path: "tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj"
      provides: "Test project for Recrd.Core — Phase 2 will populate"
      contains: "xunit"
    - path: "tests/Recrd.Integration.Tests/Recrd.Integration.Tests.csproj"
      provides: "Integration test project — references all 5 packages"
      contains: "Microsoft.NET.Test.Sdk"
    - path: "tests/Recrd.Core.Tests/PlaceholderTests.cs"
      provides: "Empty test class so xunit finds at least one test class"
      contains: "PlaceholderTests"
  key_links:
    - from: "tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj"
      to: "packages/Recrd.Core/Recrd.Core.csproj"
      via: "ProjectReference"
      pattern: "ProjectReference.*Recrd.Core.csproj"
    - from: "tests/Recrd.Integration.Tests/Recrd.Integration.Tests.csproj"
      to: "all 5 package projects"
      via: "5 ProjectReference entries"
      pattern: "ProjectReference.*Recrd"
---

<objective>
Create 6 xUnit test project stubs under tests/ — one per package and one integration test project. Each gets a PlaceholderTests.cs so dotnet test exits 0 with no "no tests found" error. All 6 are registered in recrd.sln.

Purpose: Phase 2's TDD mandate requires test projects to already exist so failing tests can be committed. The test projects must be in the solution and building before any implementation begins.
Output: 6 .csproj + 6 PlaceholderTests.cs + recrd.sln updated with tests solution folder
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
  <name>Task 1: Create unit test project stubs (Core, Data, Gherkin, Recording, Compilers)</name>
  <files>
    tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj,
    tests/Recrd.Core.Tests/PlaceholderTests.cs,
    tests/Recrd.Data.Tests/Recrd.Data.Tests.csproj,
    tests/Recrd.Data.Tests/PlaceholderTests.cs,
    tests/Recrd.Gherkin.Tests/Recrd.Gherkin.Tests.csproj,
    tests/Recrd.Gherkin.Tests/PlaceholderTests.cs,
    tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj,
    tests/Recrd.Recording.Tests/PlaceholderTests.cs,
    tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj,
    tests/Recrd.Compilers.Tests/PlaceholderTests.cs
  </files>
  <read_first>
    - /home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md (Pattern 7 — exact .csproj template for xUnit test projects; Pitfall 3 — PlaceholderTests class required to avoid "no tests found" error; Pitfall note about IsPackable=false)
    - /home/gil/dev/recrd/packages/Recrd.Core/Recrd.Core.csproj (confirm Core exists — test projects reference it)
  </read_first>
  <action>
Create 5 test project directories and files. The .csproj template for each is:

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
    <ProjectReference Include="..\..\packages\[PACKAGE]\[PACKAGE].csproj" />
  </ItemGroup>
</Project>
```

Replace [PACKAGE] with the package name. Create the following:

**tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj** — ProjectReference to `..\..\packages\Recrd.Core\Recrd.Core.csproj`

**tests/Recrd.Data.Tests/Recrd.Data.Tests.csproj** — ProjectReference to `..\..\packages\Recrd.Data\Recrd.Data.csproj`

**tests/Recrd.Gherkin.Tests/Recrd.Gherkin.Tests.csproj** — ProjectReference to `..\..\packages\Recrd.Gherkin\Recrd.Gherkin.csproj`

**tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj** — ProjectReference to `..\..\packages\Recrd.Recording\Recrd.Recording.csproj`

**tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj** — ProjectReference to `..\..\packages\Recrd.Compilers\Recrd.Compilers.csproj`

For each test project, create a PlaceholderTests.cs using this pattern (substituting the namespace):

tests/Recrd.Core.Tests/PlaceholderTests.cs:
```csharp
namespace Recrd.Core.Tests;
// Tests will be added in Phase 2
public class PlaceholderTests { }
```

tests/Recrd.Data.Tests/PlaceholderTests.cs:
```csharp
namespace Recrd.Data.Tests;
// Tests will be added in Phase 3
public class PlaceholderTests { }
```

tests/Recrd.Gherkin.Tests/PlaceholderTests.cs:
```csharp
namespace Recrd.Gherkin.Tests;
// Tests will be added in Phase 4
public class PlaceholderTests { }
```

tests/Recrd.Recording.Tests/PlaceholderTests.cs:
```csharp
namespace Recrd.Recording.Tests;
// Tests will be added in Phase 6
public class PlaceholderTests { }
```

tests/Recrd.Compilers.Tests/PlaceholderTests.cs:
```csharp
namespace Recrd.Compilers.Tests;
// Tests will be added in Phase 7
public class PlaceholderTests { }
```
  </action>
  <verify>
    <automated>cd /home/gil/dev/recrd && dotnet build tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj --no-restore 2>&1 | tail -3 && dotnet build tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj --no-restore 2>&1 | tail -3</automated>
  </verify>
  <acceptance_criteria>
    - All 5 .csproj files exist under tests/ with correct paths
    - Each .csproj contains `<IsPackable>false</IsPackable>`
    - Each .csproj contains `xunit" Version="2.9.3"`
    - Each .csproj contains `Microsoft.NET.Test.Sdk" Version="18.3.0"`
    - Each .csproj contains `xunit.runner.visualstudio" Version="3.1.5"`
    - Each .csproj contains `Moq" Version="4.20.72"`
    - Each .csproj contains `coverlet.collector" Version="8.0.1"`
    - All 5 PlaceholderTests.cs files exist with `public class PlaceholderTests { }`
    - `dotnet build tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj` exits 0
    - `dotnet build tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj` exits 0
  </acceptance_criteria>
  <done>5 unit test project stubs build clean. PlaceholderTests classes ensure dotnet test will not report "no tests found".</done>
</task>

<task type="auto">
  <name>Task 2: Create Recrd.Integration.Tests stub and register all 6 in recrd.sln</name>
  <files>
    tests/Recrd.Integration.Tests/Recrd.Integration.Tests.csproj,
    tests/Recrd.Integration.Tests/PlaceholderTests.cs,
    recrd.sln
  </files>
  <read_first>
    - /home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md (Pattern 7 — Integration.Tests references all 5 packages)
    - /home/gil/dev/recrd/recrd.sln (current state)
  </read_first>
  <action>
Create tests/Recrd.Integration.Tests/Recrd.Integration.Tests.csproj — this project references ALL 5 package projects because integration tests span the full pipeline:

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
    <ProjectReference Include="..\..\packages\Recrd.Core\Recrd.Core.csproj" />
    <ProjectReference Include="..\..\packages\Recrd.Data\Recrd.Data.csproj" />
    <ProjectReference Include="..\..\packages\Recrd.Gherkin\Recrd.Gherkin.csproj" />
    <ProjectReference Include="..\..\packages\Recrd.Recording\Recrd.Recording.csproj" />
    <ProjectReference Include="..\..\packages\Recrd.Compilers\Recrd.Compilers.csproj" />
  </ItemGroup>
</Project>
```

Create tests/Recrd.Integration.Tests/PlaceholderTests.cs:
```csharp
namespace Recrd.Integration.Tests;
// Integration tests will be added in Phase 6+
public class PlaceholderTests { }
```

Then register all 6 test projects in recrd.sln. Run from /home/gil/dev/recrd:

```bash
dotnet sln recrd.sln add \
  tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj \
  tests/Recrd.Data.Tests/Recrd.Data.Tests.csproj \
  tests/Recrd.Gherkin.Tests/Recrd.Gherkin.Tests.csproj \
  tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj \
  tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj \
  tests/Recrd.Integration.Tests/Recrd.Integration.Tests.csproj \
  --solution-folder tests
```
  </action>
  <verify>
    <automated>cd /home/gil/dev/recrd && dotnet build recrd.sln --no-restore 2>&1 | tail -5</automated>
  </verify>
  <acceptance_criteria>
    - /home/gil/dev/recrd/tests/Recrd.Integration.Tests/Recrd.Integration.Tests.csproj exists with all 5 ProjectReferences
    - `dotnet sln /home/gil/dev/recrd/recrd.sln list` output contains all 6 test projects
    - `dotnet sln /home/gil/dev/recrd/recrd.sln list | wc -l` shows at least 12 entries (5 packages + 1 CLI + 6 tests)
    - `dotnet build recrd.sln --no-restore` exits 0
    - `dotnet test recrd.sln --no-build --filter "Category!=Integration"` exits 0
  </acceptance_criteria>
  <done>All 6 test projects registered in recrd.sln. dotnet build recrd.sln exits 0. dotnet test exits 0 (placeholder classes found, no failures).</done>
</task>

</tasks>

<verification>
After all tasks:
1. `dotnet sln /home/gil/dev/recrd/recrd.sln list | wc -l` — 12+ entries (5 packages + 1 CLI + 6 tests)
2. `dotnet build /home/gil/dev/recrd/recrd.sln` exits 0
3. `dotnet test /home/gil/dev/recrd/recrd.sln --no-build --filter "Category!=Integration"` exits 0
4. `grep "IsPackable" /home/gil/dev/recrd/tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj` shows `false`
</verification>

<success_criteria>
- 6 test projects exist under tests/, each with correct xUnit/Moq/coverlet dependencies
- 6 PlaceholderTests.cs files ensure dotnet test exits 0
- All 6 registered in recrd.sln under tests solution folder
- dotnet build recrd.sln exits 0
- dotnet test recrd.sln --no-build exits 0
</success_criteria>

<output>
After completion, create `/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-05-SUMMARY.md`
</output>
