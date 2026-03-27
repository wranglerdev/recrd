---
phase: 4
slug: gherkin-generator
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-27
---

# Phase 4 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit + Moq (.NET 10) |
| **Config file** | `tests/Recrd.Gherkin.Tests/Recrd.Gherkin.Tests.csproj` |
| **Quick run command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --filter "FullyQualifiedName~Recrd.Gherkin.Tests" --no-restore` |
| **Full suite command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run quick run command
- **After every plan wave:** Run full suite command
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** ~5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | Status |
|---------|------|------|-------------|-----------|-------------------|--------|
| 04-01-01 | 01 | 0 | GHER-01..09 | unit (red) | `dotnet test --filter "FullyQualifiedName~Recrd.Gherkin.Tests"` exits non-zero | ⬜ pending |
| 04-02-01 | 02 | 1 | GHER-01,07,08 | unit | `dotnet test --filter "FixedScenarioTests"` | ⬜ pending |
| 04-02-02 | 02 | 1 | GHER-05,06 | unit | `dotnet test --filter "GroupingTests"` | ⬜ pending |
| 04-03-01 | 03 | 2 | GHER-02,09 | unit | `dotnet test --filter "DataDrivenTests"` | ⬜ pending |
| 04-03-02 | 03 | 2 | GHER-03,04 | unit | `dotnet test --filter "VariableMismatchTests"` | ⬜ pending |
| 04-04-01 | 04 | 3 | GHER-07 | unit | `dotnet test --filter "DeterminismTests"` | ⬜ pending |
| 04-04-02 | 04 | 3 | all | integration | `dotnet test --no-build` exits 0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Recrd.Gherkin.Tests/FixedScenarioTests.cs` — stubs for GHER-01, GHER-07, GHER-08
- [ ] `tests/Recrd.Gherkin.Tests/DataDrivenTests.cs` — stubs for GHER-02, GHER-09
- [ ] `tests/Recrd.Gherkin.Tests/VariableMismatchTests.cs` — stubs for GHER-03, GHER-04
- [ ] `tests/Recrd.Gherkin.Tests/GroupingTests.cs` — stubs for GHER-05, GHER-06
- [ ] `tests/Recrd.Gherkin.Tests/DeterminismTests.cs` — stubs for GHER-07

All 5 test files committed red on `tdd/phase-04` branch. CI-06 tolerates failures on `tdd/phase-*` prefix.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Output is valid Gherkin parsed by Cucumber | GHER-01, GHER-02 | No .NET Gherkin parser dep in test project | Paste output into https://app.cucumber.io/parser or use `gherkin` CLI |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
