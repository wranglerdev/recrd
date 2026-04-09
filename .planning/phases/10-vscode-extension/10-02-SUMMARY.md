---
phase: 10-vscode-extension
plan: 02
subsystem: vscode-extension
tags: [cli, process, recording, typescript]

# Dependency graph
requires:
  - phase: 10-01
    provides: Extension scaffold, StatusBarManager
provides:
  - CliRunner for spawning recrd CLI
  - RecordingManager for start/stop lifecycle
  - Integration of CLI output into VS Code OutputChannel

# Accomplishments
- Implemented `CliRunner` wrapper for `child_process.spawn` with OutputChannel streaming.
- Implemented `RecordingManager` to handle `recrd start` and `recrd stop` commands.
- Added graceful stop logic using the CLI's IPC mechanism (via `recrd stop`).
- Ensured process cleanup in `deactivate()` to prevent orphan CLI processes.
- Connected the Status Bar to the recording lifecycle (updates to Recording/Idle states).
- Verified the build successfully bundles all new components.

# Technical debt
- Command arguments are not yet fully sanitized (mitigated by using `recrd start` with minimal flags for now).
- No automated tests for the extension's process management.

# Validation
- **Build:** `cd apps/vscode-extension && node esbuild.js` completes without errors.
- **Commands:** `recrd.start` and `recrd.stop` are registered and wired to `RecordingManager`.
- **Cleanup:** `deactivate()` calls `recordingManager.kill()`.
