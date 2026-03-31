---
phase: 6
slug: recording-engine
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-29
---

# Phase 6 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9 + Moq |
| **Config file** | `tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj` |
| **Quick run command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --filter "FullyQualifiedName~Recrd.Recording.Tests" --logger "console;verbosity=minimal"` |
| **Full suite command** | `DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"` |
| **Estimated runtime** | ~30 seconds (unit/integration), ~120 seconds (Playwright E2E) |

---

## Sampling Rate

- **After every task commit:** Run quick run command
- **After every plan wave:** Run full suite command
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds (unit/integration tasks)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 6-01-01 | 01 | 0 | REC-01 | unit | `dotnet test --filter "FullyQualifiedName~BrowserContextTests"` | tests/Recrd.Recording.Tests/BrowserContextTests.cs | ⬜ pending |
| 6-01-02 | 01 | 0 | REC-02,REC-03,REC-04,REC-05 | unit | `dotnet test --filter "FullyQualifiedName~EventCaptureTests"` | tests/Recrd.Recording.Tests/EventCaptureTests.cs | ⬜ pending |
| 6-01-03 | 01 | 0 | REC-06,REC-07,REC-08 | unit | `dotnet test --filter "FullyQualifiedName~SessionLifecycleTests"` | tests/Recrd.Recording.Tests/SessionLifecycleTests.cs | ⬜ pending |
| 6-01-04 | 01 | 0 | REC-09,REC-10 | unit | `dotnet test --filter "FullyQualifiedName~SnapshotRecoveryTests"` | tests/Recrd.Recording.Tests/SnapshotRecoveryTests.cs | ⬜ pending |
| 6-01-05 | 01 | 0 | REC-11,REC-12,REC-13,REC-14 | E2E | `dotnet test --filter "FullyQualifiedName~InspectorPanelTests"` | tests/Recrd.Recording.Tests/InspectorPanelTests.cs | ⬜ pending |
| 6-01-06 | 01 | 0 | REC-15 | integration | `dotnet test --filter "FullyQualifiedName~PopupHandlingTests"` | tests/Recrd.Recording.Tests/PopupHandlingTests.cs | ⬜ pending |
| 6-02-01 | 02 | 1 | REC-01,REC-02,REC-03,REC-04,REC-05 | integration | `dotnet test --filter "FullyQualifiedName~BrowserContextTests\|EventCaptureTests"` | W0 created | ⬜ pending |
| 6-03-01 | 03 | 2 | REC-06,REC-07,REC-08,REC-09,REC-10 | integration | `dotnet test --filter "FullyQualifiedName~SessionLifecycleTests\|SnapshotRecoveryTests"` | W0 created | ⬜ pending |
| 6-04-01 | 04 | 3 | REC-11,REC-12,REC-13,REC-14 | E2E | `dotnet test --filter "FullyQualifiedName~InspectorPanelTests"` | W0 created | ⬜ pending |
| 6-05-01 | 05 | 4 | REC-15 | integration | `dotnet test --filter "FullyQualifiedName~PopupHandlingTests"` | W0 created | ⬜ pending |
| 6-05-02 | 05 | 4 | ALL | full suite | `dotnet test tests/Recrd.Recording.Tests --no-build` | W0 created | ⬜ pending |

*Status: ⬜ pending / ✅ green / ❌ red / ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj` — add `Microsoft.Playwright.Xunit` 1.58.0 and switch `coverlet.collector` -> `coverlet.msbuild` 6.0.4 (Plan 06-01, Task 1)
- [x] `tests/Recrd.Recording.Tests/BrowserContextTests.cs` — 4 test stubs for REC-01 (Plan 06-01, Task 2)
- [x] `tests/Recrd.Recording.Tests/EventCaptureTests.cs` — 11 test stubs for REC-02/REC-03/REC-04/REC-05 (Plan 06-01, Task 2)
- [x] `tests/Recrd.Recording.Tests/SessionLifecycleTests.cs` — 6 test stubs for REC-06/REC-07/REC-08 (Plan 06-01, Task 2)
- [x] `tests/Recrd.Recording.Tests/SnapshotRecoveryTests.cs` — 5 test stubs for REC-09/REC-10 (Plan 06-01, Task 2)
- [x] `tests/Recrd.Recording.Tests/InspectorPanelTests.cs` — 8 test stubs for REC-11/REC-12/REC-13/REC-14 (Plan 06-01, Task 2)
- [x] `tests/Recrd.Recording.Tests/PopupHandlingTests.cs` — 3 test stubs for REC-15 (Plan 06-01, Task 2)
- [ ] Playwright Chromium installed: `bash packages/Recrd.Recording/bin/Debug/net10.0/playwright.sh install`

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Inspector side-panel visual layout and right-click menu | REC-08 | Requires visual browser interaction | Launch `recrd start`, verify panel renders per UI-SPEC, right-click target element |
| Variable name collision warning UI | REC-10 | Visual feedback element | Tag a variable, try to reuse name, verify warning appears in panel |
| Shadow DOM selector fallback | REC-03 | Not fully automatable without fixture | Create page with shadow root, record click on slotted element, inspect selector |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
