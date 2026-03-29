---
phase: 05-ci-pipeline
verified: 2026-03-29T23:15:00Z
status: human_needed
score: 9/9 must-haves verified
re_verification:
  previous_status: gaps_found
  previous_score: 8/9
  gaps_closed:
    - "Pushing a tag on a non-main branch does NOT trigger packaging — publish.yml if condition fixed to remove the always-true || clause"
  gaps_remaining: []
  regressions: []
human_verification:
  - test: "Push a pre-release tag from a feature branch and confirm the publish job is skipped"
    expected: "Job does not run; no packages are published"
    why_human: "github.event.base_ref must be verified at runtime — it may be empty for lightweight tags depending on how the tag is created, which would flip the guard to false (safe) but is worth confirming"
  - test: "Artificially drop coverage below 90% in one gated project and push to a PR branch"
    expected: "The specific Coverage gate — Recrd.{Project} (90% line) step fails, naming the project"
    why_human: "Requires a running .NET environment with coverlet — threshold enforcement cannot be verified from file inspection alone"
  - test: "Push a failing test on a tdd/phase-* branch"
    expected: "Test (TDD red-phase — failures allowed) step shows warning but job does not fail; subsequent steps run"
    why_human: "Requires an actual GitHub Actions run to confirm continue-on-error does not block downstream steps"
---

# Phase 05: CI Pipeline Verification Report

**Phase Goal:** Every push to the repository triggers a fully automated quality gate: build, test, coverage, format, with scheduled mutation testing and gated NuGet publish on `main`.
**Verified:** 2026-03-29T23:15:00Z
**Status:** human_needed (all automated checks pass; three runtime behaviours require human confirmation)
**Re-verification:** Yes — after gap closure (previous status: gaps_found, score: 8/9)

## Re-verification Summary

The single gap from the initial verification has been closed. `publish.yml` line 15 previously read:

```
if: github.ref_type == 'tag' && github.event.base_ref == 'refs/heads/main' || github.event.repository.default_branch == 'main'
```

The `|| github.event.repository.default_branch == 'main'` clause was always true for this repository, making the entire condition always true and rendering the branch guard a no-op. The fix removes that clause entirely. The condition now reads:

```
if: github.ref_type == 'tag' && github.event.base_ref == 'refs/heads/main'
```

This is the correct, minimal expression: a tag push only proceeds when the base ref is `refs/heads/main`. No regressions were detected in any previously-passing items.

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | A PR against main runs restore, build, test with per-project coverage gates, and format check | VERIFIED | ci.yml lines 3-9: triggers on push/PR to main; lines 25-86: restore → build → 4 coverage gates → format |
| 2 | Dropping any gated project below 90% line coverage fails the build with the project name in the error | VERIFIED | ci.yml lines 49-83: four named steps `Coverage gate — Recrd.{Core,Data,Gherkin,Compilers} (90% line)` each with `--threshold 90 --threshold-type line --threshold-stat minimum` |
| 3 | A formatting violation fails the build | VERIFIED | ci.yml line 86: `dotnet format --verify-no-changes` with no `continue-on-error`; runs on all branches |
| 4 | Pushing to a tdd/phase-* branch runs tests without failing the build on test failures | VERIFIED | ci.yml lines 44-47: step `Test (TDD red-phase — failures allowed)` with `if: ${{ startsWith(github.ref_name, 'tdd/phase-') }}` and `continue-on-error: true`; push trigger added for `tdd/phase-*` at line 7 |
| 5 | A weekly scheduled workflow runs Stryker.NET on Recrd.Core and posts a mutation score report to GitHub Actions Summary | VERIFIED | mutation.yml lines 4-6: `cron: '0 6 * * 1'`; lines 28-35: `dotnet stryker --project packages/Recrd.Core/Recrd.Core.csproj`; lines 38-50: report posted to `$GITHUB_STEP_SUMMARY` |
| 6 | The mutation workflow never fails the build regardless of mutation score | VERIFIED | mutation.yml line 36: `continue-on-error: true` on the Stryker step |
| 7 | Pushing a pre-release tag matching v*-* on main triggers dotnet pack followed by NuGet push to GitHub Packages | VERIFIED | publish.yml lines 4-6: trigger `tags: - 'v*-*'`; lines 44-48: `dotnet pack --configuration Release -p:PackageVersion=...`; lines 50-55: `dotnet nuget push ... nuget.pkg.github.com ... --api-key ${{ secrets.GITHUB_TOKEN }}` |
| 8 | Pushing a tag on a non-main branch does NOT trigger packaging | VERIFIED | publish.yml line 15: `if: github.ref_type == 'tag' && github.event.base_ref == 'refs/heads/main'` — the previously broken `\|\| repository.default_branch == 'main'` clause has been removed; condition is now a strict two-term AND with no always-true escape path |
| 9 | dotnet-stryker tool is registered in the local tool manifest | VERIFIED | `.config/dotnet-tools.json`: `"dotnet-stryker": { "version": "4.6.0", "commands": ["dotnet-stryker"], "rollForward": false }` |

**Score:** 9/9 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `.github/workflows/ci.yml` | Complete CI pipeline with coverage gates, TDD red-phase support, and format check | VERIFIED | 87 lines; valid YAML; IPv4 env at workflow level; 4 named coverage gates; conditional TDD test step |
| `.github/workflows/mutation.yml` | Weekly Stryker.NET mutation testing workflow | VERIFIED | 51 lines; valid YAML; schedule + workflow_dispatch; Recrd.Core target; GITHUB_STEP_SUMMARY report; continue-on-error |
| `.github/workflows/publish.yml` | Tag-triggered NuGet publish workflow gated to main branch | VERIFIED | 56 lines; valid YAML; correct tag trigger; branch guard now correct: `github.ref_type == 'tag' && github.event.base_ref == 'refs/heads/main'`; pack and push wired correctly |
| `.config/dotnet-tools.json` | dotnet-stryker tool manifest entry | VERIFIED | Valid JSON; dotnet-stryker 4.6.0 with rollForward: false |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `.github/workflows/ci.yml` | `tests/Recrd.Core.Tests` | `dotnet test --threshold 90` | WIRED | Line 51-56: explicit project path + threshold flags |
| `.github/workflows/ci.yml` | `tests/Recrd.Data.Tests` | `dotnet test --threshold 90` | WIRED | Line 59-65: explicit project path + threshold flags |
| `.github/workflows/ci.yml` | `tests/Recrd.Gherkin.Tests` | `dotnet test --threshold 90` | WIRED | Line 67-74: explicit project path + threshold flags |
| `.github/workflows/ci.yml` | `tests/Recrd.Compilers.Tests` | `dotnet test --threshold 90` | WIRED | Line 76-83: explicit project path + threshold flags |
| `.github/workflows/ci.yml` | `tdd/phase-* branch detection` | `continue-on-error conditional` | WIRED | Lines 41, 45-46: two-step conditional pattern; push trigger at line 7 |
| `.github/workflows/mutation.yml` | `packages/Recrd.Core` | Stryker target project | WIRED | Line 31: `--project packages/Recrd.Core/Recrd.Core.csproj` |
| `.github/workflows/mutation.yml` | `$GITHUB_STEP_SUMMARY` | Report destination | WIRED | Lines 43, 47: appended via `>>` redirect |
| `.github/workflows/publish.yml` | GitHub Packages NuGet feed | `dotnet nuget push` with GITHUB_TOKEN | WIRED | Lines 52-55: push to `nuget.pkg.github.com` with `secrets.GITHUB_TOKEN` |
| `.github/workflows/publish.yml` | tag filter | push tags `v*-*` | WIRED | Line 6: `- 'v*-*'` |
| `.github/workflows/publish.yml` | non-main branch guard | job-level if condition | WIRED | Line 15: `github.ref_type == 'tag' && github.event.base_ref == 'refs/heads/main'` — clean two-term AND, no escape path |

### Data-Flow Trace (Level 4)

Not applicable — this phase produces GitHub Actions workflow YAML files and a tool manifest, not components that render dynamic data. No runtime data flows to verify.

### Behavioral Spot-Checks

Not applicable — workflow files cannot be executed locally without a GitHub Actions runner. All structure checks were performed via YAML parsing and grep pattern matching.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| CI-01 | 05-01-PLAN.md | GitHub Actions pipeline: restore → build → test → coverage gate → format check | SATISFIED | ci.yml implements all five stages in order |
| CI-02 | 05-01-PLAN.md | Coverage gate fails build if Core, Data, Gherkin, or Compilers drop below 90% line coverage | SATISFIED | Four named coverage gate steps with `--threshold 90 --threshold-type line --threshold-stat minimum` |
| CI-03 | 05-01-PLAN.md | `dotnet format --verify-no-changes` enforced on every push/PR | SATISFIED | ci.yml line 86: present without `continue-on-error` |
| CI-04 | 05-02-PLAN.md | Weekly scheduled Stryker.NET mutation run on `Recrd.Core` | SATISFIED | mutation.yml with cron `0 6 * * 1`, targeting Recrd.Core |
| CI-05 | 05-03-PLAN.md | `main`-branch-only: `dotnet pack` → NuGet push (pre-release tag) | SATISFIED | publish.yml line 15 fixed: `github.ref_type == 'tag' && github.event.base_ref == 'refs/heads/main'`; || escape path removed |
| CI-06 | 05-01-PLAN.md | TDD red phase: CI runs tests but does NOT fail the build on test failures during `tdd/phase-*` prefix | SATISFIED | ci.yml conditional TDD step with `continue-on-error: true`; push trigger added for `tdd/phase-*` |

### Anti-Patterns Found

No anti-patterns detected. No TODO/FIXME/placeholder comments. No empty implementations. No stub patterns. All three workflow YAML files and dotnet-tools.json are syntactically valid. The operator precedence bug in publish.yml has been resolved.

### Human Verification Required

#### 1. Branch Guard Behaviour Under Real GitHub Actions

**Test:** Push a pre-release tag (e.g., `v0.1.0-test.1`) from a feature branch and observe whether the `publish` job runs.
**Expected:** Job is skipped; packages are NOT published.
**Why human:** `github.event.base_ref` behaviour for lightweight vs. annotated tags should be confirmed at runtime. For lightweight tags pushed directly, `base_ref` may be empty, which would make the entire condition false (safe). For annotated tags the value is well-defined. Either way the guard is no longer trivially bypassable, but a runtime run confirms the exact skip behaviour.

#### 2. Coverage Gate Threshold Enforcement

**Test:** Artificially drop coverage below 90% in one gated project and push to a PR branch.
**Expected:** The specific `Coverage gate — Recrd.{Project} (90% line)` step fails with an exit code from coverlet, naming the project.
**Why human:** Requires a running .NET environment with coverlet. Cannot verify threshold enforcement behaviour from file inspection alone.

#### 3. TDD Red-Phase Continue-on-Error Behaviour

**Test:** Push a failing test on a `tdd/phase-*` branch.
**Expected:** The `Test (TDD red-phase — failures allowed)` step shows a warning (yellow) but the job does not fail; subsequent coverage gate steps run and pass.
**Why human:** Requires an actual GitHub Actions run to confirm `continue-on-error` does not block downstream steps.

### Gaps Summary

No gaps remain. All 9 must-have truths are verified, all 6 requirement IDs (CI-01 through CI-06) are satisfied, and all key links are wired. The single blocker from the initial verification — the operator precedence bug in `publish.yml` — has been resolved by removing the `|| github.event.repository.default_branch == 'main'` clause. Three human verification items remain open from the initial report; they cover runtime GitHub Actions behaviour that cannot be confirmed from static analysis.

---

_Verified: 2026-03-29T23:15:00Z_
_Verifier: Claude (gsd-verifier)_
