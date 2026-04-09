# Phase 10: VS Code Extension - Research

**Researched:** 2026-03-29
**Domain:** VS Code Extension Development, CLI Wrapper, Webview UI
**Confidence:** HIGH

## Summary

This phase involves wrapping the `recrd` CLI into a VS Code extension to provide a seamless recording and preview experience. Research confirms that `child_process.spawn` is the standard for long-running CLI tasks, provided lifecycle cleanup is handled via `context.subscriptions`. For the UI, the VS Code Webview UI Toolkit is deprecated; using native CSS variables or the community-driven "VS Code Elements" is recommended. Syntax highlighting for the live preview can be efficiently handled using Shiki (which supports Gherkin and Robot Framework) on the extension host side to avoid Webview performance/security overhead.

**Primary recommendation:** Use `child_process.spawn` with an `OutputChannel` (or `Terminal` for ANSI support) for CLI execution, and a Webview powered by Shiki-highlighted HTML for the live preview.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Execution Mechanism:** Use `child_process.spawn` to invoke the 'recrd' CLI.
- **Communication:** No custom IPC; communicate strictly via CLI stdout/stderr and exit codes.
- **Trigger/Update:** WebView live preview updates when `.recrd` file changes on disk (monitored via `fs.watch` or `createFileSystemWatcher`).
- **UI Components:** 
  - Status bar shows recording state and elapsed time.
  - QuickPick for compiler target and data file selection.
- **Compatibility:** Minimum VS Code version 1.85.
- **Publishing:** Publishable to VS Code Marketplace via `vsce`.

### the agent's Discretion
- **WebView Framework:** Choice of WebView UI toolkit (e.g., VS Code WebView UI Toolkit, or vanilla HTML/CSS).
- **CLI Management:** How to handle multiple concurrent CLI processes (though usually only one recording at a time).
- **Mocking Strategy:** How to mock the CLI for testing and extension development.
- **Error Handling:** How to present CLI errors to the user (Notifications, OutputChannel, etc.).

### Deferred Ideas (OUT OF SCOPE)
- **Custom IPC:** Explicitly deferred/forbidden for this phase.
- **Plugin Management UI:** Not required for this phase (handled by CLI or future phase).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| VSCE-01 | Start/stop recording via `recrd start` / `recrd stop` | Verified `child_process.spawn` lifecycle management. [CITED: code.visualstudio.com] |
| VSCE-02 | Compiler target picker (QuickPick) | `vscode.window.showQuickPick` for selection. [CITED: VS Code API] |
| VSCE-03 | Data file picker | `vscode.window.showOpenDialog` with workspace filters. [CITED: VS Code API] |
| VSCE-04 | Live preview WebView | `createFileSystemWatcher` + Shiki for highlighting. [VERIFIED: npm registry] |
| VSCE-05 | Status bar shows state/time | `vscode.window.createStatusBarItem` with interval timer. [CITED: VS Code API] |
| VSCE-06 | CLI-only communication | Piped `stdout`/`stderr` to `OutputChannel`. [VERIFIED: code search] |
| VSCE-07 | Marketplace publish | `vsce` CLI, mandatory PNG icon, and `package.json` fields. [CITED: code.visualstudio.com] |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `@vscode/vsce` | 3.2.1 | Packaging/Publishing | Official tool for Marketplace deployment. [VERIFIED: npm registry] |
| `shiki` | 2.1.0 | Syntax Highlighting | Native-equivalent highlighting for Gherkin/Robot. [VERIFIED: npm registry] |
| `vscode` | ^1.85.0 | Extension API | Target platform compatibility. [CITED: VS Code API] |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|--------------|
| `tree-kill` | 1.2.2 | Process Cleanup | Killing process trees on Windows (critical for Playwright). [VERIFIED: npm registry] |
| `sinon` | 19.0.2 | Mocking | Mocking `cp.spawn` in extension tests. [VERIFIED: npm registry] |

**Installation:**
```bash
npm install shiki tree-kill
npm install --save-dev @vscode/vsce sinon @types/sinon
```

## Architecture Patterns

### Recommended Project Structure
```
apps/vscode-extension/
├── src/
│   ├── extension.ts          # Entry point (Activation/Deactivation)
│   ├── cliManager.ts         # Wrapper for child_process.spawn
│   ├── statusBarProvider.ts  # Logic for status bar updates
│   ├── webviewProvider.ts    # Webview creation and Shiki rendering
│   └── pickers.ts            # showQuickPick/showOpenDialog helpers
├── media/                    # Icons and CSS for Webview
├── .vscodeignore             # Exclude src/ and tests/ from package
└── package.json              # Extension manifest
```

### Pattern: Safe CLI Spawning
**What:** Manage the child process lifecycle to prevent orphaned processes.
**Example:**
```typescript
// Source: https://code.visualstudio.com/api/references/vscode-api
const child = cp.spawn('recrd', ['start'], { shell: true });
context.subscriptions.push({
    dispose: () => child.kill() // kills process on extension deactivation
});
```

### Anti-Patterns to Avoid
- **Raw ANSI in Output Channels:** Standard `OutputChannel` shows `^[[31m`. Use a `Terminal` if colors are critical, or strip codes for `OutputChannel`. [VERIFIED: web search]
- **Reloading Webview on Change:** Don't replace `panel.webview.html` entirely; use `postMessage` to update inner content for better UX. [VERIFIED: web search]

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Syntax Highlighting | Custom regex | Shiki | Matches VS Code native highlighting perfectly. |
| Process Killing | `process.kill` | `tree-kill` | Handles child/grandchild processes on Windows. |
| UI Components | Custom HTML buttons | VS Code Elements | Ensures theme consistency and accessibility. |

## Common Pitfalls

### Pitfall 1: Orphaned Windows Processes
**What goes wrong:** `child.kill()` fails to kill the browser spawned by the CLI on Windows.
**How to avoid:** Use `tree-kill` or `taskkill /F /T`. [VERIFIED: web search]

### Pitfall 2: Path Resolution
**What goes wrong:** `recrd` is not in the user's `PATH`.
**How to avoid:** Check `recrd` availability on activation; allow user to configure path in settings. [VERIFIED: web search]

## Code Examples

### Mocking `cp.spawn` for Tests
```typescript
// Source: Sinon + NodeJS docs
const mockProcess = new EventEmitter() as any;
mockProcess.stdout = new Readable({ read() {} });
const spawnStub = sinon.stub(cp, 'spawn').returns(mockProcess);
// ... simulate output ...
mockProcess.stdout.push('Recording started');
mockProcess.emit('close', 0);
```

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Node.js | Extension Runtime | ✓ | v22.22.0 | — |
| npm | Dependency Management | ✓ | 10.9.4 | — |
| vsce | Packaging | ✗ | — | `npm install -g @vscode/vsce` |
| recrd CLI | Core Functionality | ✓ | (Local build) | — |

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Mocha (Standard for VS Code) |
| Quick run command | `npm test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| VSCE-01 | CLI Spawn/Kill | Unit | `npm test` | ❌ Wave 0 |
| VSCE-05 | Status Bar Update | Integration | `npm test` | ❌ Wave 0 |

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V5 Input Validation | yes | Validate CLI arguments before spawning. |
| V12 Communications | yes | Secure `postMessage` origin checking in Webview. |

## Sources

### Primary (HIGH confidence)
- [Official VS Code API Docs] - Spawning, Webview, QuickPick, Status Bar.
- [Marketplace Publishing Guide] - Technical requirements for submission.
- [Shiki Style Docs] - Support for Gherkin/Robot Framework.

## Metadata
**Confidence breakdown:**
- Standard stack: HIGH
- Architecture: HIGH
- Pitfalls: MEDIUM (Windows specifics need verification during implementation)

**Research date:** 2026-03-29
**Valid until:** 2026-04-29
