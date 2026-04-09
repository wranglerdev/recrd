---
phase: 10-vscode-extension
plan: 01
subsystem: vscode-extension
tags: [scaffold, ui, statusbar, typescript]

# Dependency graph
requires:
  - phase: 08
    provides: recrd CLI (for future invocation)
provides:
  - Extension scaffold in apps/vscode-extension
  - StatusBarManager for recording state visualization
  - Configuration 'recrd.executablePath'

# Accomplishments
- Scaffolded the VS Code extension using a manual setup (package.json, tsconfig.json).
- Implemented a fast build pipeline using `esbuild.js`.
- Created `StatusBarManager` to manage the recording state in the VS Code status bar.
- Registered placeholder commands `recrd.showInfo`, `recrd.start`, and `recrd.stop`.
- Verified that the project builds successfully using `node esbuild.js`.

# Technical debt
- The extension currently only has placeholder logic for start/stop; actual CLI integration follows in Plan 10-02.
- No automated tests for the extension yet (VS Code extension testing is complex and deferred to Wave 4).

# Validation
- **Build:** `cd apps/vscode-extension && node esbuild.js` completes without errors.
- **Manifest:** `package.json` contains the required engine, contributes, and settings.
- **UI:** `StatusBarManager` implemented with support for Idle, Recording, and Paused states.
