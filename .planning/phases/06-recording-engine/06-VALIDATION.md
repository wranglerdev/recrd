---
phase: 6
slug: recording-engine
status: draft
nyquist_compliant: false
wave_0_complete: false
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
| 6-01-01 | 01 | 0 | REC-01 | unit | `dotnet test --filter "FullyQualifiedName~RecordedEventTests"` | ❌ W0 | ⬜ pending |
| 6-01-02 | 01 | 0 | REC-02 | unit | `dotnet test --filter "FullyQualifiedName~SelectorStrategyTests"` | ❌ W0 | ⬜ pending |
| 6-01-03 | 01 | 0 | REC-03 | unit | `dotnet test --filter "FullyQualifiedName~EventTypeTests"` | ❌ W0 | ⬜ pending |
| 6-02-01 | 02 | 1 | REC-01,REC-02 | integration | `dotnet test --filter "FullyQualifiedName~PlaywrightRecorderTests"` | ❌ W0 | ⬜ pending |
| 6-02-02 | 02 | 1 | REC-04 | unit | `dotnet test --filter "FullyQualifiedName~ChannelPipelineTests"` | ❌ W0 | ⬜ pending |
| 6-03-01 | 03 | 1 | REC-05,REC-06 | integration | `dotnet test --filter "FullyQualifiedName~RecordingLifecycleTests"` | ❌ W0 | ⬜ pending |
| 6-03-02 | 03 | 1 | REC-07 | unit | `dotnet test --filter "FullyQualifiedName~PartialSnapshotTests"` | ❌ W0 | ⬜ pending |
| 6-04-01 | 04 | 2 | REC-08,REC-09 | E2E | `dotnet test --filter "FullyQualifiedName~InspectorPanelTests"` | ❌ W0 | ⬜ pending |
| 6-04-02 | 04 | 2 | REC-10 | E2E | `dotnet test --filter "FullyQualifiedName~VariableTaggingTests"` | ❌ W0 | ⬜ pending |
| 6-05-01 | 05 | 2 | REC-11 | integration | `dotnet test --filter "FullyQualifiedName~AssertionInsertionTests"` | ❌ W0 | ⬜ pending |
| 6-06-01 | 06 | 3 | REC-14,REC-15 | integration | `dotnet test --filter "FullyQualifiedName~PopupCaptureTests"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Recrd.Recording.Tests/Recrd.Recording.Tests.csproj` — add `Microsoft.Playwright.Xunit` 1.58.0 and switch `coverlet.collector` → `coverlet.msbuild` 6.0.4
- [ ] `tests/Recrd.Recording.Tests/RecordedEventTests.cs` — stubs for REC-01/REC-02/REC-03
- [ ] `tests/Recrd.Recording.Tests/PlaywrightRecorderTests.cs` — integration test stubs for REC-01/REC-02
- [ ] `tests/Recrd.Recording.Tests/ChannelPipelineTests.cs` — stubs for REC-04
- [ ] `tests/Recrd.Recording.Tests/RecordingLifecycleTests.cs` — stubs for REC-05/REC-06
- [ ] `tests/Recrd.Recording.Tests/PartialSnapshotTests.cs` — stubs for REC-07
- [ ] `tests/Recrd.Recording.Tests/InspectorPanelTests.cs` — E2E stubs for REC-08/REC-09
- [ ] `tests/Recrd.Recording.Tests/VariableTaggingTests.cs` — stubs for REC-10
- [ ] `tests/Recrd.Recording.Tests/AssertionInsertionTests.cs` — stubs for REC-11
- [ ] `tests/Recrd.Recording.Tests/PopupCaptureTests.cs` — stubs for REC-14/REC-15
- [ ] Playwright Chromium installed: `node ~/.nuget/packages/microsoft.playwright/1.58.0/.playwright/package/cli.js install chromium`

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Inspector side-panel visual layout and right-click menu | REC-08 | Requires visual browser interaction | Launch `recrd start`, verify panel renders per UI-SPEC, right-click target element |
| Variable name collision warning UI | REC-10 | Visual feedback element | Tag a variable, try to reuse name, verify warning appears in panel |
| Shadow DOM selector fallback | REC-03 | Not fully automatable without fixture | Create page with shadow root, record click on slotted element, inspect selector |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
