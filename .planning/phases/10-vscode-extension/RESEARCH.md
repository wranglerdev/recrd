# Phase 10: VS Code Extension - Research

**Researched:** 2024-05-23
**Domain:** VS Code Extension Development, CLI Wrapping, WebViews
**Confidence:** HIGH

## Summary

This phase focuses on creating a VS Code extension that serves as a graphical wrapper for the `recrd` CLI. The extension will provide a seamless recording experience by managing CLI lifecycle, showing status in the UI, and providing live previews of generated artifacts.

**Primary recommendation:** Use `child_process.spawn` for streaming CLI output to a dedicated `OutputChannel`, manage recording state via a `StatusBarItem`, and implement a `WebView` for live preview using `@vscode/webview-ui-toolkit` for a native look and feel.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Execution Mechanism:** Use `child_process.spawn` to invoke the 'recrd' CLI. [VERIFIED: CONTEXT.md]
- **Communication:** No custom IPC; communicate strictly via CLI stdout/stderr and exit codes. [VERIFIED: CONTEXT.md]
- **Trigger/Update:** WebView live preview updates when `.recrd` file changes on disk (monitored via `fs.watch` or `createFileSystemWatcher`). [VERIFIED: CONTEXT.md]
- **UI Components:** 
  - Status bar shows recording state and elapsed time. [VERIFIED: CONTEXT.md]
  - QuickPick for compiler target and data file selection. [VERIFIED: CONTEXT.md]
- **Compatibility:** Minimum VS Code version 1.85. [VERIFIED: CONTEXT.md]
- **Publishing:** Publishable to VS Code Marketplace via `vsce`. [VERIFIED: CONTEXT.md]

### the agent's Discretion
- **WebView Framework:** Choice of WebView UI toolkit (e.g., VS Code WebView UI Toolkit, or vanilla HTML/CSS). [VERIFIED: CONTEXT.md]
- **CLI Management:** How to handle multiple concurrent CLI processes (though usually only one recording at a time). [VERIFIED: CONTEXT.md]
- **Mocking Strategy:** How to mock the CLI for testing and extension development. [VERIFIED: CONTEXT.md]
- **Error Handling:** How to present CLI errors to the user (Notifications, OutputChannel, etc.). [VERIFIED: CONTEXT.md]

### Deferred Ideas (OUT OF SCOPE)
- **Custom IPC:** Explicitly deferred/forbidden for this phase. [VERIFIED: CONTEXT.md]
- **Plugin Management UI:** Not required for this phase (handled by CLI or future phase). [VERIFIED: CONTEXT.md]
</user_constraints>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `vscode` | ^1.85.0 | Extension API | Official VS Code API. [CITED: docs] |
| `@vscode/webview-ui-toolkit` | Latest | WebView UI | Official toolkit for native-looking components. [VERIFIED: npm registry] |
| `child_process` | Built-in | CLI Spawning | Standard Node.js for process management. [VERIFIED: Node.js docs] |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|--------------|
| `@vscode/vsce` | Latest | Packaging/Publishing | Required for Marketplace deployment. [CITED: docs] |
| `esbuild` | Latest | Bundling | Recommended for fast extension builds. [CITED: docs] |
| `path` | Built-in | Path Resolution | Handling workspace and CLI paths. [VERIFIED: Node.js docs] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `child_process.spawn` | `child_process.exec` | `exec` buffers output and can crash if logs are large. `spawn` is better for streaming. [VERIFIED] |
| `WebviewPanel` | `CustomEditor` | `CustomEditor` is better for binary files, but `WebviewPanel` is more flexible for "previews". [ASSUMED] |

**Installation:**
```bash
# In apps/vscode-extension
npm install --save-dev @types/vscode @vscode/vsce esbuild
npm install @vscode/webview-ui-toolkit
```

## Architecture Patterns

### Recommended Project Structure
```
apps/vscode-extension/
├── src/
│   ├── extension.ts          # Main entry point (activate/deactivate)
│   ├── commands/             # Command handlers (record, stop, compile)
│   ├── webviews/             # WebView providers (Live Preview)
│   ├── statusBar/            # StatusBarItem management
│   ├── cli/                  # CLI wrapper (child_process.spawn logic)
│   └── utils/                # Config and path helpers
├── media/                    # Icons (PNG) and styles
├── package.json              # Extension manifest
└── tsconfig.json             # TypeScript config
```

### Pattern 1: CLI Execution Wrapper
**What:** A class to handle the lifecycle of the `recrd` process, streaming output to an `OutputChannel`.
**When to use:** Every time a command needs to run the CLI (record, compile).
**Example:**
```typescript
// Source: Community Best Practices
export class CliRunner {
    private static outputChannel = vscode.window.createOutputChannel("recrd");

    static async run(args: string[]): Promise<number> {
        this.outputChannel.show(true);
        const child = spawn('recrd', args, { shell: true });
        
        child.stdout.on('data', data => this.outputChannel.append(data.toString()));
        child.stderr.on('data', data => this.outputChannel.append(`[ERR] ${data.toString()}`));

        return new Promise((resolve) => {
            child.on('close', code => resolve(code ?? 0));
        });
    }
}
```

### Anti-Patterns to Avoid
- **Hardcoding CLI Path:** Don't assume `recrd` is in the PATH. Use a configuration setting (`recrd.executablePath`) and default to a search in the workspace or global path.
- **Leaking Processes:** Forgetting to call `child.kill()` when the extension is deactivated or when a new recording starts while one is active.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| UI Components | Custom HTML/CSS Buttons | `@vscode/webview-ui-toolkit` | Accessibility, theme compatibility, and native feel. |
| Icons | Custom SVG rendering | VS Code Octicons | Built-in support for `$(icon-name)` in Status Bar and labels. |
| Path Manipulation | String concatenation | Node `path` module | OS compatibility (Windows vs Linux). |

## Common Pitfalls

### Pitfall 1: Process Orphans
**What goes wrong:** If VS Code crashes or the extension is reloaded, the `recrd` CLI (and its browser instance) might keep running.
**How to avoid:** Store the `ChildProcess` reference and implement a robust `deactivate()` function. Consider using a "heartbeat" or checking for parent process existence in the CLI.

### Pitfall 2: WebView State Loss
**What goes wrong:** Closing a WebView tab and reopening it loses the current preview state.
**How to avoid:** Use `retainContextWhenHidden: true` in WebView options or implement `getState`/`setState` to persist state across tab reloads.

## Code Examples

### Status Bar Management
```typescript
// Source: https://code.visualstudio.com/api/references/vscode-api
const recordingStatus = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
recordingStatus.text = "$(record) Recording...";
recordingStatus.tooltip = "recrd: Click to stop";
recordingStatus.command = "recrd.stopRecording";
recordingStatus.backgroundColor = new vscode.ThemeColor('statusBarItem.errorBackground');
recordingStatus.show();
```

### Live Preview File Watcher
```typescript
// Source: https://code.visualstudio.com/api/references/vscode-api
const watcher = vscode.workspace.createFileSystemWatcher('**/*.recrd');
watcher.onDidChange(uri => {
    // Notify WebView to refresh
    panel.webview.postMessage({ type: 'update', uri: uri.fsPath });
});
```

### QuickPick for CLI Options
```typescript
// Source: https://code.visualstudio.com/api/references/vscode-api
const target = await vscode.window.showQuickPick([
    { label: 'robot-browser', description: 'Playwright-based Robot Framework' },
    { label: 'robot-selenium', description: 'Selenium-based Robot Framework' },
    { label: 'gherkin', description: 'pt-BR Gherkin feature files' }
], { placeHolder: 'Select compiler target' });
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `child_process.exec` | `child_process.spawn` | Always | Better memory usage and real-time output. |
| Vanilla WebViews | WebView UI Toolkit | 2021 | Consistent UI with VS Code theme. |
| Webpack | esbuild | ~2022 | Significantly faster build times. |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `recrd` executable name is `recrd` | Architecture | Wrapper will fail to find CLI if name is different. |
| A2 | Users have `dotnet` runtime installed | Environment | CLI won't run even if extension is installed. |

## Open Questions

1. **How to bundle the CLI?** Should the extension download the CLI on first run, or assume it's pre-installed?
   - Recommendation: Add a setting for `recrd.path` and provide a "Download/Install" command if missing.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Node.js | Extension Runtime | ✓ | v22.22.0 | — |
| dotnet | CLI Execution | ✓ | 10.0.104 | Prompt user to install .NET 10 |
| vsce | Packaging | ✗ | — | Install as devDependency |
| recrd CLI | Functionality | ✓ | 1.0.0 | Prompt user to set path |

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | `@vscode/test-electron` |
| Config file | `src/test/runTest.ts` |
| Quick run command | `npm test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| VS-01 | Spawns CLI | Integration | `npm test` | ❌ Wave 0 |
| VS-02 | Updates Status Bar | UI/Unit | `npm test` | ❌ Wave 0 |
| VS-03 | Shows WebView | Integration | `npm test` | ❌ Wave 0 |

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V5 Input Validation | yes | Validate arguments before passing to `spawn`. |
| V10 Malicious Code | yes | Use official `vsce` for packaging; avoid untrusted dependencies. |

### Known Threat Patterns for VS Code Extensions

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Command Injection | Tampering | Never pass unsanitized user input directly to shell. |
| Insecure WebView | Information Disclosure | Content Security Policy (CSP) in WebViews. |

## Sources

### Primary (HIGH confidence)
- [Official VS Code API Docs](https://code.visualstudio.com/api) - Core APIs
- [VS Code Webview UI Toolkit](https://github.com/microsoft/vscode-webview-ui-toolkit) - UI components
- [Node.js child_process Docs](https://nodejs.org/api/child_process.html) - Process management

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH
- Architecture: HIGH
- Pitfalls: MEDIUM

**Research date:** 2024-05-23
**Valid until:** 2024-06-23
