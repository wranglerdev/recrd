---
phase: 1
slug: monorepo-scaffold-solution-structure
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-26
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build + dotnet test (xUnit stubs) |
| **Config file** | recrd.sln, Directory.Build.props |
| **Quick run command** | `dotnet build recrd.sln --no-restore` |
| **Full suite command** | `dotnet build recrd.sln && dotnet test recrd.sln --no-build` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build recrd.sln --no-restore`
- **After every plan wave:** Run `dotnet build recrd.sln && dotnet test recrd.sln --no-build`
- **Before `/gsd:verify-work`:** Full suite must be green (all stubs compile and pass)
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 1-01-01 | 01 | 1 | structural | build | `dotnet build recrd.sln` | ❌ W0 | ⬜ pending |
| 1-01-02 | 01 | 1 | structural | build | `dotnet build recrd.sln` | ❌ W0 | ⬜ pending |
| 1-01-03 | 01 | 1 | structural | build | `dotnet build recrd.sln` | ❌ W0 | ⬜ pending |
| 1-02-01 | 02 | 2 | structural | build | `dotnet build recrd.sln` | ❌ W0 | ⬜ pending |
| 1-02-02 | 02 | 2 | structural | lint | `dotnet format recrd.sln --verify-no-changes` | ❌ W0 | ⬜ pending |
| 1-03-01 | 03 | 3 | structural | script | `grep -v "Recrd\." packages/Recrd.Core/Recrd.Core.csproj | grep PackageReference` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `recrd.sln` — solution file with all project references
- [ ] `Directory.Build.props` — shared TFM, nullable, warnings-as-errors
- [ ] `global.json` — .NET 10 SDK pin with `latestPatch` rollForward
- [ ] All `.csproj` stubs compiled (no build errors)

*Existing infrastructure: none — Wave 0 creates the scaffold from scratch.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| `dotnet build` from clean checkout | structural | Requires clean environment without prior restore | `rm -rf **/obj **/bin && dotnet restore && dotnet build recrd.sln` |
| Recrd.Core zero-dep CI gate | structural | Requires CI workflow execution | Push to branch, verify GitHub Actions passes |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
