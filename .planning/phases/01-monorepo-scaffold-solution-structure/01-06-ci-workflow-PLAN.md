---
phase: 01-monorepo-scaffold-solution-structure
plan: 06
type: execute
wave: 2
depends_on:
  - 01-PLAN-solution-scaffold
files_modified:
  - .github/workflows/ci.yml
autonomous: true
requirements: []
must_haves:
  truths:
    - ".github/workflows/ci.yml exists and triggers on push/PR to main"
    - "CI runs restore → build → Core isolation check → test in sequence"
    - "Core isolation check uses grep to assert Recrd.Core.csproj has zero Recrd.* ProjectReferences"
    - "dotnet format --verify-no-changes is included as a code style step"
    - "The workflow uses actions/setup-dotnet@v4 with dotnet-version 10.0.x"
  artifacts:
    - path: ".github/workflows/ci.yml"
      provides: "GitHub Actions CI workflow"
      contains: "actions/setup-dotnet@v4"
  key_links:
    - from: ".github/workflows/ci.yml"
      to: "packages/Recrd.Core/Recrd.Core.csproj"
      via: "grep assertion step checks the file directly"
      pattern: "grep -E 'ProjectReference.*Recrd\\.' packages/Recrd.Core/Recrd.Core.csproj"
    - from: ".github/workflows/ci.yml"
      to: "recrd.sln"
      via: "dotnet restore / build / test operate on recrd.sln"
      pattern: "dotnet build --no-restore"
---

<objective>
Create the GitHub Actions CI workflow at .github/workflows/ci.yml. This is the Phase 1 skeleton: restore → build → Core isolation check → test → format check. Phase 5 adds coverage gates, Stryker, and NuGet push logic.

Purpose: The CI workflow enforces the Core isolation rule from day one. A push to main without CI would allow Recrd.Core to silently acquire Recrd.* dependencies.
Output: .github/workflows/ci.yml
</objective>

<execution_context>
@/home/gil/dev/recrd/.claude/get-shit-done/workflows/execute-plan.md
@/home/gil/dev/recrd/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md
@/home/gil/dev/recrd/.planning/ROADMAP.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create .github/workflows/ci.yml</name>
  <files>.github/workflows/ci.yml</files>
  <read_first>
    - /home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md (Pattern 8 — full CI workflow YAML; Pattern 5 — grep assertion for Core isolation; note about Phase 5 adding coverage/Stryker/NuGet)
    - /home/gil/dev/recrd/recrd.sln (confirm it exists — CI references it implicitly via dotnet build/test)
  </read_first>
  <action>
Create directory /home/gil/dev/recrd/.github/workflows/ if it does not exist. Create .github/workflows/ci.yml with this exact content:

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
          if grep -E '<ProjectReference[^>]*Include="[^"]*Recrd\.' \
              packages/Recrd.Core/Recrd.Core.csproj; then
            echo "ERROR: Recrd.Core must not reference any Recrd.* project"
            exit 1
          fi
          echo "OK: Recrd.Core has zero Recrd.* references"

      - name: Test
        run: dotnet test --no-build --collect:"XPlat Code Coverage" --filter "Category!=Integration"

      - name: Check code style
        run: dotnet format --verify-no-changes
```

Notes:
- `dotnet-version: '10.0.x'` matches the global.json `10.0.103` with `latestPatch` policy
- `--filter "Category!=Integration"` skips integration tests (no Docker/containers available in Phase 1 CI)
- The `Assert Recrd.Core` step runs BEFORE test — isolation failure fails fast
- `dotnet format --verify-no-changes` runs last because it requires a clean build
- Phase 5 will add: coverage gate with threshold, Stryker.NET scheduled run, NuGet pack/push on main tag, TDD red-phase branch logic (tdd/phase-*)
  </action>
  <verify>
    <automated>grep -c "actions/setup-dotnet@v4" /home/gil/dev/recrd/.github/workflows/ci.yml && grep -c "Assert Recrd.Core" /home/gil/dev/recrd/.github/workflows/ci.yml && grep -c "dotnet format --verify-no-changes" /home/gil/dev/recrd/.github/workflows/ci.yml</automated>
  </verify>
  <acceptance_criteria>
    - /home/gil/dev/recrd/.github/workflows/ci.yml exists
    - ci.yml contains `actions/checkout@v4`
    - ci.yml contains `actions/setup-dotnet@v4`
    - ci.yml contains `dotnet-version: '10.0.x'`
    - ci.yml contains `dotnet restore`
    - ci.yml contains `dotnet build --no-restore`
    - ci.yml contains the grep assertion: `grep -E '<ProjectReference[^>]*Include="[^"]*Recrd\.'`
    - ci.yml contains `dotnet test --no-build --collect:"XPlat Code Coverage"`
    - ci.yml contains `--filter "Category!=Integration"`
    - ci.yml contains `dotnet format --verify-no-changes`
    - ci.yml triggers on push to `main` and pull_request to `main`
    - YAML is valid (no tabs in indentation — YAML requires spaces)
  </acceptance_criteria>
  <done>ci.yml created with all required steps. YAML is valid. Core isolation grep assertion is present.</done>
</task>

</tasks>

<verification>
1. `grep "Assert Recrd.Core" /home/gil/dev/recrd/.github/workflows/ci.yml` exits 0
2. `grep "dotnet format" /home/gil/dev/recrd/.github/workflows/ci.yml` exits 0
3. `python3 -c "import yaml; yaml.safe_load(open('/home/gil/dev/recrd/.github/workflows/ci.yml'))"` exits 0 (YAML valid)
   OR use: `cat /home/gil/dev/recrd/.github/workflows/ci.yml | grep "  " | head -5` to spot-check indentation
</verification>

<success_criteria>
- .github/workflows/ci.yml exists with valid YAML
- Contains restore → build → Core isolation check → test → format check in sequence
- Core isolation check uses grep on Recrd.Core.csproj
- Triggers on push and PR to main
</success_criteria>

<output>
After completion, create `/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-06-SUMMARY.md`
</output>
