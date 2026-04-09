---
phase: 10-vscode-extension
plan: 03
subsystem: vscode-extension
tags: [compiler, ui, quickpick, typescript]

# Dependency graph
requires:
  - phase: 10-02
    provides: CliRunner, basic extension logic
provides:
  - recrd.compile command with UI pickers
  - Context menu integration for .recrd files

# Accomplishments
- Implemented `recrd.compile` command handler in `src/commands/compile.ts`.
- Integrated `vscode.window.showQuickPick` for selecting compiler targets (`robot-browser`, `robot-selenium`, `gherkin`).
- Integrated `vscode.window.showOpenDialog` for optional data file (CSV/JSON) and output directory selection.
- Added support for compiling from the Explorer context menu (right-click on `.recrd` files).
- Wired UI selections to the `recrd compile` CLI command via `CliRunner`.
- Added progress notification for the compilation process.

# Technical debt
- Path quoting in CLI arguments is manual; should use a more robust argument escaping logic if complex paths are encountered.
- No validation of the selected data file schema before calling the CLI.

# Validation
- **Build:** `cd apps/vscode-extension && node esbuild.js` completes without errors.
- **Manifest:** `package.json` correctly registers the `recrd.compile` command and the explorer menu contribution.
- **UI:** Compilation flow correctly prompts for target, data, and output directory.
