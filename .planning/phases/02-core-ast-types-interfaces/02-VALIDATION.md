---
phase: 2
slug: core-ast-types-interfaces
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-26
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.x (already installed) |
| **Config file** | `tests/Recrd.Core.Tests/Recrd.Core.Tests.csproj` |
| **Quick run command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Core.Tests --no-build` |
| **Full suite command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test tests/Recrd.Core.Tests --no-build`
- **After every plan wave:** Run `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 2-01-01 | 01 | 0 | CORE-01–13 | unit | `dotnet test tests/Recrd.Core.Tests --no-build` | ❌ W0 | ⬜ pending |
| 2-02-01 | 02 | 1 | CORE-01 | unit | `dotnet test tests/Recrd.Core.Tests --filter "SessionSerialization" --no-build` | ❌ W0 | ⬜ pending |
| 2-02-02 | 02 | 1 | CORE-02,03 | unit | `dotnet test tests/Recrd.Core.Tests --filter "StepModel" --no-build` | ❌ W0 | ⬜ pending |
| 2-02-03 | 02 | 1 | CORE-04,05 | unit | `dotnet test tests/Recrd.Core.Tests --filter "SelectorVariable" --no-build` | ❌ W0 | ⬜ pending |
| 2-02-04 | 02 | 1 | CORE-06 | unit | `dotnet test tests/Recrd.Core.Tests --filter "ChannelPipeline" --no-build` | ❌ W0 | ⬜ pending |
| 2-02-05 | 02 | 1 | CORE-07–10 | unit | `dotnet test tests/Recrd.Core.Tests --filter "InterfaceContract" --no-build` | ❌ W0 | ⬜ pending |
| 2-03-01 | 03 | 2 | CORE-11 | build | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet build packages/Recrd.Core --no-restore` | ✅ | ⬜ pending |
| 2-03-02 | 03 | 2 | CORE-12 | unit | `dotnet test tests/Recrd.Core.Tests --no-build` | ❌ W0 | ⬜ pending |
| 2-03-03 | 03 | 2 | CORE-13 | build+test | `dotnet build && dotnet test --no-build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Recrd.Core.Tests/SessionSerializationTests.cs` — red stubs for CORE-01
- [ ] `tests/Recrd.Core.Tests/StepModelTests.cs` — red stubs for CORE-02, CORE-03
- [ ] `tests/Recrd.Core.Tests/SelectorVariableTests.cs` — red stubs for CORE-04, CORE-05
- [ ] `tests/Recrd.Core.Tests/ChannelPipelineTests.cs` — red stubs for CORE-06
- [ ] `tests/Recrd.Core.Tests/InterfaceContractTests.cs` — red stubs for CORE-07–CORE-10
- [ ] All test files committed on `tdd/phase-02` branch prefix (per D-13 decision)
- [ ] CI-06 configured to tolerate test failures on `tdd/phase-02` prefix (verify existing CI config)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Zero `Recrd.*` references in `Recrd.Core` | CORE-11 | Build check confirms it but `dotnet dependency-graph` output must be reviewed | Run `dotnet dependency-graph` and verify no `Recrd.*` package references appear in Core's node |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
