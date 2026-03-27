---
phase: 3
slug: data-providers
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-26
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit + Moq (.NET 10) |
| **Config file** | `tests/Recrd.Data.Tests/Recrd.Data.Tests.csproj` |
| **Quick run command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --filter "FullyQualifiedName~Recrd.Data.Tests"` |
| **Full suite command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --filter "FullyQualifiedName~Recrd.Data.Tests"`
- **After every plan wave:** Run `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 3-01-01 | 01 | 0 | DATA-01..05 | unit (red) | `dotnet test --filter "FullyQualifiedName~Recrd.Data.Tests"` | ❌ W0 | ⬜ pending |
| 3-02-01 | 02 | 1 | DATA-01, DATA-02 | unit (green) | `dotnet test --filter "FullyQualifiedName~CsvDataProvider"` | ✅ W0 | ⬜ pending |
| 3-03-01 | 03 | 2 | DATA-03 | unit (green) | `dotnet test --filter "FullyQualifiedName~JsonDataProvider"` | ✅ W0 | ⬜ pending |
| 3-04-01 | 04 | 2 | DATA-04, DATA-05 | unit+perf | `dotnet test --filter "FullyQualifiedName~Recrd.Data.Tests"` | ✅ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Recrd.Data.Tests/CsvDataProviderTests.cs` — stubs for DATA-01, DATA-02
- [ ] `tests/Recrd.Data.Tests/JsonDataProviderTests.cs` — stubs for DATA-03, DATA-04, DATA-05
- [ ] Branch `tdd/phase-03` created and all test stubs committed failing

*Wave 0 must commit all test files before any implementation begins.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| BOM-prefixed UTF-8 file parsing | DATA-01 | File encoding edge case | Run `CsvDataProvider` against a file saved with BOM using a hex editor to verify |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
