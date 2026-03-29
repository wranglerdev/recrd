# Phase 5: CI Pipeline - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md — this log preserves the Q&A.

**Date:** 2026-03-29
**Phase:** 05-ci-pipeline
**Mode:** discuss
**Areas discussed:** Coverage enforcement, Mutation reporting, NuGet publish, TDD red-phase

---

## Questions & Answers

### Coverage Enforcement

| Question | Options presented | Selected |
|----------|------------------|----------|
| How to implement the 90% per-project gate? | Coverlet inline thresholds / Post-test XML script / ReportGenerator | Coverlet inline thresholds |
| Fail fast or report all failures? | Separate invocations (fail fast) / Run all, report all | Separate invocations — fail fast |

### Mutation Reporting

| Question | Options presented | Selected |
|----------|------------------|----------|
| Where to post the weekly report? | GitHub Actions Summary / Issue comment / Artifact upload | GitHub Actions Summary |
| Fail on score threshold or report-only? | Report-only (always green) / Fail below threshold | Report-only — always green |

### NuGet Publish

| Question | Options presented | Selected |
|----------|------------------|----------|
| Tag pattern to trigger publish? | v*-* (pre-release only) / v* (any version) | v*-* (pre-release only) |
| Which NuGet feed? | nuget.org / GitHub Packages / Both | GitHub Packages only |

### TDD Red-Phase

| Question | Options presented | Selected |
|----------|------------------|----------|
| Which steps lenient on tdd/phase-*? | Tests only / Tests + coverage / Tests + coverage + format | Tests only — everything else enforced |

---

## Corrections Made

None — all recommended defaults accepted.
