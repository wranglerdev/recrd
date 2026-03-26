---
phase: 1
slug: monorepo-scaffold-solution-structure
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-26
audited: 2026-03-26
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build + dotnet test (xUnit stubs) |
| **Config file** | recrd.sln, Directory.Build.props |
| **Quick run command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet build recrd.sln --no-restore` |
| **Full suite command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet build recrd.sln && DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test recrd.sln --no-build` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet build recrd.sln --no-restore`
- **After every plan wave:** Run `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet build recrd.sln && dotnet test recrd.sln --no-build`
- **Before `/gsd:verify-work`:** Full suite must be green (all stubs compile and pass)
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 1-01-01 | 01 | 1 | structural | build | `dotnet build recrd.sln` | ✅ | ✅ green |
| 1-01-02 | 01 | 1 | structural | build | `dotnet build recrd.sln` | ✅ | ✅ green |
| 1-01-03 | 01 | 1 | structural | build | `dotnet build recrd.sln` | ✅ | ✅ green |
| 1-02-01 | 02 | 2 | structural | build | `dotnet build recrd.sln` | ✅ | ✅ green |
| 1-02-02 | 02 | 2 | structural | lint | `dotnet format recrd.sln --verify-no-changes` | ✅ | ✅ green |
| 1-03-01 | 03 | 3 | structural | script | `grep -v "Recrd\." packages/Recrd.Core/Recrd.Core.csproj \| grep PackageReference` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `recrd.sln` — solution file with all project references
- [x] `Directory.Build.props` — shared TFM, nullable, warnings-as-errors
- [x] `global.json` — .NET 10 SDK pin with `latestPatch` rollForward
- [x] All `.csproj` stubs compiled (no build errors)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| `dotnet build` from clean checkout | structural | Requires clean environment without prior restore | `rm -rf **/obj **/bin && DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet restore && dotnet build recrd.sln` |
| Recrd.Core zero-dep CI gate | structural | Requires CI workflow execution | Push to branch, verify GitHub Actions passes |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 15s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** 2026-03-26 — all 6 automated verifications green; 2 manual-only items logged above

---

## Validation Audit 2026-03-26

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated to manual-only | 0 |
| Already covered | 6 |
| Manual-only (pre-existing) | 2 |
