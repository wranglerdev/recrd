---
type: quick
id: 260329-w2f
description: Fix CI pipeline coverage gate
completed: "2026-03-29"
duration_seconds: ~180
tasks_completed: 3
files_modified: 5
commits:
  - ef7fe9e
  - 3b28b93
---

# Quick Task 260329-w2f: Fix CI Pipeline Coverage Gate Summary

**One-liner:** Switched four gated test projects from coverlet.collector to coverlet.msbuild and updated ci.yml gate steps to /p:Threshold MSBuild property syntax so the 90% line coverage threshold is actually enforced.

## What Was Done

The CI coverage gate steps were using `--threshold 90 --threshold-type line --threshold-stat minimum` flags which are `coverlet.console`-only flags. When passed to `dotnet test`, these flags caused `MSB1001: Unknown option` errors and the gate silently passed everything without running a single test. The gate was a no-op.

**Fix applied:**
1. Replaced `coverlet.collector 8.0.1` with `coverlet.msbuild 6.0.4` in all four gated test project files.
2. Replaced the broken `--collect:"XPlat Code Coverage" --threshold*` flag syntax in ci.yml with `/p:CollectCoverage=true /p:Threshold=90 /p:ThresholdType=line /p:ThresholdStat=minimum` — the correct MSBuild property syntax for coverlet.msbuild.

## Verification Results

- `dotnet test ... /p:Threshold=90` exits **1** — Recrd.Core is at 66.72% line coverage, correctly detected as breach.
- `dotnet test ... /p:Threshold=50` exits **0** — 66.72% exceeds 50%, correctly passes.
- `dotnet build --no-restore` exits 0 after restore with coverlet.msbuild 6.0.4.
- ci.yml YAML is syntactically valid (python3 yaml.safe_load confirms).

## Tasks Completed

| Task | Description | Commit |
|------|-------------|--------|
| 1 | Switch 4 gated test .csproj files from coverlet.collector to coverlet.msbuild | ef7fe9e |
| 2 | Update ci.yml coverage gate steps to /p: MSBuild property syntax | 3b28b93 |
| 3 | Verify threshold enforcement — exit 1 on breach, exit 0 on pass | (no files changed) |

## Files Modified

| File | Change |
|------|--------|
| `tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj` | coverlet.collector → coverlet.msbuild 6.0.4 |
| `tests/Recrd.Data.Tests/Recrd.Data.Tests.csproj` | coverlet.collector → coverlet.msbuild 6.0.4 |
| `tests/Recrd.Gherkin.Tests/Recrd.Gherkin.Tests.csproj` | coverlet.collector → coverlet.msbuild 6.0.4 |
| `tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj` | coverlet.collector → coverlet.msbuild 6.0.4 |
| `.github/workflows/ci.yml` | Four gate steps: replace --collect/--threshold flags with /p: properties |

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- All four .csproj files reference coverlet.msbuild: confirmed via build success and grep.
- Commits ef7fe9e and 3b28b93 exist in git log.
- ci.yml YAML valid: confirmed.
- Threshold enforcement verified: exit 1 at 90%, exit 0 at 50%.
