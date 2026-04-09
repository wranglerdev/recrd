---
phase: 10-vscode-extension
plan: 04
subsystem: vscode-extension
tags: [webview, preview, packaging, typescript]

# Dependency graph
requires:
  - phase: 10-03
    provides: Compiler integration, UI pickers
provides:
  - Live Preview WebView for generated artifacts
  - Automatic preview refresh on .recrd file changes
  - VS Code Marketplace-ready package configuration

# Accomplishments
- Implemented `PreviewPanel` using `WebviewPanel` and basic HTML/CSS tabs.
- Integrated `vscode.workspace.createFileSystemWatcher` to monitor `.recrd` file changes.
- Added automatic background compilation to refresh the preview when sessions are saved.
- Added a "Show Live Preview" icon to the editor title bar for `.recrd` files.
- Completed the extension metadata in `package.json` and added a comprehensive `README.md`.
- Configured `.vscodeignore` for clean packaging.
- Verified that the extension builds successfully with the new WebView and dependencies.

# Technical debt
- WebView styling uses a custom minimal CSS instead of the full `@vscode/webview-ui-toolkit` components (due to time/complexity of bundling webview scripts).
- Background compilation for preview refresh doesn't currently handle errors gracefully in the UI (only logs to console).

# Validation
- **Build:** `cd apps/vscode-extension && node esbuild.js` completes without errors.
- **Manifest:** `package.json` contains all required fields for Marketplace publication.
- **UI:** Preview command and icon are correctly registered.
