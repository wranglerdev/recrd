---
phase: 260326-eol
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - packages/Recrd.Recording/Recrd.Recording.csproj
autonomous: true
requirements: []
must_haves:
  truths:
    - "dotnet restore completes successfully with Recrd.Recording in the solution"
    - "Microsoft.Playwright 1.58.0 is referenced in Recrd.Recording.csproj"
  artifacts:
    - path: "packages/Recrd.Recording/Recrd.Recording.csproj"
      provides: "Playwright NuGet package reference"
      contains: "Microsoft.Playwright"
  key_links:
    - from: "packages/Recrd.Recording/Recrd.Recording.csproj"
      to: "NuGet: Microsoft.Playwright 1.58.0"
      via: "PackageReference Include"
      pattern: "Microsoft\\.Playwright"
---

<objective>
Restore the Microsoft.Playwright 1.58.0 PackageReference that was accidentally removed from Recrd.Recording.csproj.

Purpose: Recrd.Recording implements IRecorderEngine using Playwright. Without the package reference, the project cannot compile or reference any Playwright types. The "infinite loading" during dotnet restore was the expected ~190 MB download for the Playwright package — not a bug.

Output: Recrd.Recording.csproj with Microsoft.Playwright 1.58.0 restored.
</objective>

<execution_context>
@/home/gil/dev/recrd/.claude/get-shit-done/workflows/execute-plan.md
@/home/gil/dev/recrd/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@/home/gil/dev/recrd/.planning/STATE.md
@/home/gil/dev/recrd/packages/Recrd.Recording/Recrd.Recording.csproj
</context>

<tasks>

<task type="auto">
  <name>Task 1: Restore Microsoft.Playwright PackageReference</name>
  <files>packages/Recrd.Recording/Recrd.Recording.csproj</files>
  <action>
    Add the Microsoft.Playwright 1.58.0 PackageReference back into the ItemGroup in Recrd.Recording.csproj.

    The file currently contains only a ProjectReference to Recrd.Core. Add the following line inside the existing ItemGroup (alongside the ProjectReference):

      &lt;PackageReference Include="Microsoft.Playwright" Version="1.58.0" /&gt;

    Do not create a new ItemGroup — add it to the existing one. Do not change any other property.

    Note: dotnet restore will download ~190 MB for this package. This is expected behavior, not an error. The download may take several minutes on first restore.
  </action>
  <verify>
    <automated>grep -c "Microsoft.Playwright" /home/gil/dev/recrd/packages/Recrd.Recording/Recrd.Recording.csproj</automated>
  </verify>
  <done>Recrd.Recording.csproj contains a PackageReference for Microsoft.Playwright Version="1.58.0"</done>
</task>

</tasks>

<verification>
After the file is updated:
- grep confirms Microsoft.Playwright appears in Recrd.Recording.csproj
- Running `dotnet restore packages/Recrd.Recording/Recrd.Recording.csproj` from the repo root completes (may take several minutes on first run due to the ~190 MB download)
</verification>

<success_criteria>
Microsoft.Playwright 1.58.0 is present in Recrd.Recording.csproj and dotnet restore succeeds.
</success_criteria>

<output>
After completion, create `.planning/quick/260326-eol-fix-playwright-nuget-package-in-recrd-re/260326-eol-SUMMARY.md`
</output>
