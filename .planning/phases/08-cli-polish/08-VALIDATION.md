---
phase: 8
slug: cli-polish
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-07
---

# Phase 8 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit + Moq (.NET) |
| **Config file** | `tests/Recrd.Cli.Tests/Recrd.Cli.Tests.csproj` (Wave 0 creates) |
| **Quick run command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --filter "FullyQualifiedName~Recrd.Cli.Tests" --logger "console;verbosity=minimal"` |
| **Full suite command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --filter "FullyQualifiedName~Recrd.Cli.Tests" --logger "console;verbosity=minimal"`
- **After every plan wave:** Run `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 08-01-01 | 01 | 0 | CLI-01 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~Recrd.Cli.Tests.CommandRouting"` | ❌ W0 | ⬜ pending |
| 08-01-02 | 01 | 0 | CLI-09 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~Recrd.Cli.Tests.IpcSocket"` | ❌ W0 | ⬜ pending |
| 08-02-01 | 02 | 1 | CLI-02, CLI-03, CLI-04 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~Recrd.Cli.Tests.CommandRouting"` | ❌ W0 | ⬜ pending |
| 08-03-01 | 03 | 1 | CLI-05, CLI-06 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~Recrd.Cli.Tests.ValidateSanitize"` | ❌ W0 | ⬜ pending |
| 08-04-01 | 04 | 2 | CLI-07, CLI-08 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~Recrd.Cli.Tests.Logging"` | ❌ W0 | ⬜ pending |
| 08-05-01 | 04 | 2 | CLI-10, CLI-11, CLI-12 | — | N/A | perf | `time dotnet run --project apps/recrd-cli -- version` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Recrd.Cli.Tests/Recrd.Cli.Tests.csproj` — new test project for CLI commands
- [ ] `tests/Recrd.Cli.Tests/CommandRoutingTests.cs` — stubs for CLI-01, CLI-02, CLI-03, CLI-04
- [ ] `tests/Recrd.Cli.Tests/ValidateSanitizeTests.cs` — stubs for CLI-05, CLI-06
- [ ] `tests/Recrd.Cli.Tests/LoggingTests.cs` — stubs for CLI-07, CLI-08
- [ ] `tests/Recrd.Cli.Tests/IpcSocketTests.cs` — stubs for CLI-09
- [ ] Add `Recrd.Cli.Tests` to `recrd.sln`
- [ ] `InternalsVisibleTo` in `recrd-cli.csproj` for test access to command handlers

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Cold-start < 500 ms | CLI-12 | `time` measurement varies per platform | Run `time dotnet run --project apps/recrd-cli -- version` on Windows, macOS, Linux; each must complete < 500 ms |
| `--help` output accuracy | CLI-01 | Output format requires human review | Run `recrd --help` and each subcommand `--help`; verify all flags described |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
