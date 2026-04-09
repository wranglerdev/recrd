# Phase 10: VS Code Extension - Context

## Decisions
- **Execution Mechanism:** Use `child_process.spawn` to invoke the 'recrd' CLI.
- **Communication:** No custom IPC; communicate strictly via CLI stdout/stderr and exit codes.
- **Trigger/Update:** WebView live preview updates when `.recrd` file changes on disk (monitored via `fs.watch` or `createFileSystemWatcher`).
- **UI Components:** 
  - Status bar shows recording state and elapsed time.
  - QuickPick for compiler target and data file selection.
- **Compatibility:** Minimum VS Code version 1.85.
- **Publishing:** Publishable to VS Code Marketplace via `vsce`.

## the agent's Discretion
- **WebView Framework:** Choice of WebView UI toolkit (e.g., VS Code WebView UI Toolkit, or vanilla HTML/CSS).
- **CLI Management:** How to handle multiple concurrent CLI processes (though usually only one recording at a time).
- **Mocking Strategy:** How to mock the CLI for testing and extension development.
- **Error Handling:** How to present CLI errors to the user (Notifications, OutputChannel, etc.).

## Deferred Ideas
- **Custom IPC:** Explicitly deferred/forbidden for this phase.
- **Plugin Management UI:** Not required for this phase (handled by CLI or future phase).
