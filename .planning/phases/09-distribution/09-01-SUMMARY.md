---
phase: 09-distribution
plan: 01
subsystem: distribution
tags: [distribution, validation, tdd]
tech-stack: [bash, xUnit]
key-files:
  - scripts/validate-pkg.sh
  - tests/Recrd.Cli.Tests/DistValidationTests.cs
metrics:
  duration: 10m
  completed_date: 2026-04-09
---

# Phase 09 Plan 01: Distribution Validation Summary

Established a "Definition of Done" for distribution artifacts by creating a package validation script and a suite of failing tests (TDD Red).

## Key Deliverables

- **scripts/validate-pkg.sh**: A bash script that verifies a package's structure, ensuring the presence of:
    - Main executable (recrd/recrd.exe)
    - `.playwright/` directory
    - `cli.js` entry point for Playwright
    - Node.js binary
- **DistValidationTests.cs**: An xUnit test suite that:
    - Verifies valid mock package structures
    - Identifies missing assets in empty directories
    - Fails against the (currently non-existent) `publish/` directory as a TDD Red gate.

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check: PASSED

- [x] scripts/validate-pkg.sh exists and is executable
- [x] DistValidationTests.cs created and correctly invokes the validation script
- [x] Red phase confirmed: Tests fail because the actual publish directory is missing.
- [x] Commits made per task.

## Commits

- b728e7d: chore(09-01): add package validation script
- a441c81: test(09-01): add distribution validation tests (TDD red)
