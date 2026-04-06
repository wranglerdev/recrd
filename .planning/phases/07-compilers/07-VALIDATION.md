---
phase: 7
slug: compilers
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-05
---

# Phase 7 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 |
| **Config file** | `tests/Recrd.Compilers.Tests/Recrd.Compilers.Tests.csproj` (existing) |
| **Quick run command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Compilers.Tests --no-build --filter "Category!=Integration"` |
| **Full suite command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Integration.Tests --no-build --filter "Category=Integration"` |
| **Estimated runtime** | ~15 seconds (unit), ~60 seconds (full with E2E) |

---

## Sampling Rate

- **After every task commit:** Run `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Compilers.Tests --no-build --filter "Category!=Integration"`
- **After every plan wave:** Run full suite (unit + integration)
- **Before `/gsd:verify-work`:** Full suite must be green (including COMP-10 E2E)
- **Max feedback latency:** ~15 seconds (unit), ~60 seconds (E2E)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 07-W0-01 | 01 | 0 | COMP-01, COMP-08 | unit | Quick run | ❌ W0 | ⬜ pending |
| 07-W0-02 | 01 | 0 | COMP-02 | unit | Quick run | ❌ W0 | ⬜ pending |
| 07-W0-03 | 01 | 0 | COMP-03 | unit | Quick run | ❌ W0 | ⬜ pending |
| 07-W0-04 | 01 | 0 | COMP-04, COMP-08 | unit | Quick run | ❌ W0 | ⬜ pending |
| 07-W0-05 | 01 | 0 | COMP-05 | unit | Quick run | ❌ W0 | ⬜ pending |
| 07-W0-06 | 01 | 0 | COMP-06 | unit | Quick run | ❌ W0 | ⬜ pending |
| 07-W0-07 | 01 | 0 | COMP-07 | unit | Quick run | ❌ W0 | ⬜ pending |
| 07-W0-08 | 01 | 0 | COMP-09 | unit | Quick run | ❌ W0 | ⬜ pending |
| 07-W0-09 | 01 | 0 | D-02 (slug) | unit | Quick run | ❌ W0 | ⬜ pending |
| 07-W0-10 | 01 | 0 | COMP-10 | integration | Full suite | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Recrd.Compilers.Tests/BrowserCompilerOutputTests.cs` — covers COMP-01, COMP-08
- [ ] `tests/Recrd.Compilers.Tests/BrowserCompilerSelectorTests.cs` — covers COMP-02
- [ ] `tests/Recrd.Compilers.Tests/BrowserCompilerWaitTests.cs` — covers COMP-03
- [ ] `tests/Recrd.Compilers.Tests/SeleniumCompilerOutputTests.cs` — covers COMP-04, COMP-08
- [ ] `tests/Recrd.Compilers.Tests/SeleniumCompilerSelectorTests.cs` — covers COMP-05
- [ ] `tests/Recrd.Compilers.Tests/SeleniumCompilerWaitTests.cs` — covers COMP-06
- [ ] `tests/Recrd.Compilers.Tests/TraceabilityHeaderTests.cs` — covers COMP-07
- [ ] `tests/Recrd.Compilers.Tests/CompilationResultTests.cs` — covers COMP-09
- [ ] `tests/Recrd.Compilers.Tests/KeywordNameBuilderTests.cs` — covers D-02 slug normalization
- [ ] `tests/Recrd.Integration.Tests/RoundTripTests.cs` — covers COMP-10 (E2E)
- [ ] `tests/Recrd.Integration.Tests/Recrd.Integration.Tests.csproj` — needs `Microsoft.AspNetCore.TestHost` PackageReference
- [ ] `.github/workflows/ci.yml` — needs pip install + rfbrowser init step before integration tests

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| None | — | — | — |

*All phase behaviors have automated verification.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
