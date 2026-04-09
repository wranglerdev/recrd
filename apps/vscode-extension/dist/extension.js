"use strict";
var __create = Object.create;
var __defProp = Object.defineProperty;
var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
var __getOwnPropNames = Object.getOwnPropertyNames;
var __getProtoOf = Object.getPrototypeOf;
var __hasOwnProp = Object.prototype.hasOwnProperty;
var __export = (target, all) => {
  for (var name in all)
    __defProp(target, name, { get: all[name], enumerable: true });
};
var __copyProps = (to, from, except, desc) => {
  if (from && typeof from === "object" || typeof from === "function") {
    for (let key of __getOwnPropNames(from))
      if (!__hasOwnProp.call(to, key) && key !== except)
        __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
  }
  return to;
};
var __toESM = (mod, isNodeMode, target) => (target = mod != null ? __create(__getProtoOf(mod)) : {}, __copyProps(
  // If the importer is in node compatibility mode or this is not an ESM
  // file that has been converted to a CommonJS file using a Babel-
  // compatible transform (i.e. "__esModule" has not been set), then set
  // "default" to the CommonJS "module.exports" for node compatibility.
  isNodeMode || !mod || !mod.__esModule ? __defProp(target, "default", { value: mod, enumerable: true }) : target,
  mod
));
var __toCommonJS = (mod) => __copyProps(__defProp({}, "__esModule", { value: true }), mod);

// src/extension.ts
var extension_exports = {};
__export(extension_exports, {
  activate: () => activate,
  deactivate: () => deactivate
});
module.exports = __toCommonJS(extension_exports);
var vscode6 = __toESM(require("vscode"));
var fs = __toESM(require("fs/promises"));
var path = __toESM(require("path"));

// src/statusBar.ts
var vscode = __toESM(require("vscode"));
var StatusBarManager = class {
  _statusBarItem;
  constructor() {
    this._statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
    this._statusBarItem.command = "recrd.showInfo";
    this.update(0 /* Idle */);
    this._statusBarItem.show();
  }
  update(state, elapsedSeconds) {
    let text = "$(record) recrd: ";
    let tooltip = "recrd E2E Recorder";
    let color;
    switch (state) {
      case 0 /* Idle */:
        text += "Idle";
        tooltip = "recrd: Click to start recording";
        break;
      case 1 /* Recording */:
        text += "Recording";
        if (elapsedSeconds !== void 0) {
          text += ` (${this._formatTime(elapsedSeconds)})`;
        }
        tooltip = "recrd: Recording interaction...";
        color = new vscode.ThemeColor("statusBarItem.errorBackground");
        break;
      case 2 /* Paused */:
        text += "Paused";
        tooltip = "recrd: Recording paused";
        color = new vscode.ThemeColor("statusBarItem.warningBackground");
        break;
    }
    this._statusBarItem.text = text;
    this._statusBarItem.tooltip = tooltip;
    this._statusBarItem.backgroundColor = color;
  }
  _formatTime(seconds) {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s.toString().padStart(2, "0")}`;
  }
  dispose() {
    this._statusBarItem.dispose();
  }
};

// src/commands/recording.ts
var vscode3 = __toESM(require("vscode"));

// src/cli/runner.ts
var vscode2 = __toESM(require("vscode"));
var cp = __toESM(require("child_process"));
var CliRunner = class {
  static _outputChannel;
  static get outputChannel() {
    if (!this._outputChannel) {
      this._outputChannel = vscode2.window.createOutputChannel("recrd");
    }
    return this._outputChannel;
  }
  static spawn(args) {
    const config = vscode2.workspace.getConfiguration("recrd");
    const executablePath = config.get("executablePath") || "recrd";
    this.outputChannel.appendLine(`[CLI] Executing: ${executablePath} ${args.join(" ")}`);
    const childProcess = cp.spawn(executablePath, args, {
      shell: true,
      env: { ...process.env, DOTNET_SYSTEM_NET_DISABLEIPV6: "1" }
    });
    childProcess.stdout?.on("data", (data) => {
      this.outputChannel.append(data.toString());
    });
    childProcess.stderr?.on("data", (data) => {
      this.outputChannel.append(data.toString());
    });
    childProcess.on("error", (err) => {
      this.outputChannel.appendLine(`[CLI Error] ${err.message}`);
      vscode2.window.showErrorMessage(`Failed to start recrd CLI: ${err.message}`);
    });
    return childProcess;
  }
  static async runCommand(args) {
    return new Promise((resolve) => {
      const childProcess = this.spawn(args);
      childProcess.on("close", (code) => {
        resolve(code || 0);
      });
    });
  }
};

// src/commands/recording.ts
var RecordingManager = class {
  constructor(_statusBarManager) {
    this._statusBarManager = _statusBarManager;
  }
  _currentProcess;
  async start() {
    if (this._currentProcess) {
      vscode3.window.showWarningMessage("A recording is already in progress.");
      return;
    }
    const baseUrl = await vscode3.window.showInputBox({
      prompt: "Enter Base URL for recording (optional)",
      placeHolder: "https://example.com"
    });
    const args = ["start"];
    if (baseUrl) {
      args.push("--base-url", baseUrl);
    }
    this._currentProcess = CliRunner.spawn(args);
    this._statusBarManager.update(1 /* Recording */, 0);
    this._currentProcess.on("close", (code) => {
      CliRunner.outputChannel.appendLine(`[CLI] recrd start exited with code ${code}`);
      this._currentProcess = void 0;
      this._statusBarManager.update(0 /* Idle */);
    });
  }
  async stop() {
    CliRunner.outputChannel.appendLine("[CLI] Sending stop signal...");
    const exitCode = await CliRunner.runCommand(["stop"]);
    if (exitCode !== 0) {
      vscode3.window.showErrorMessage("Failed to stop recording gracefully. Killing process...");
      this.kill();
    }
  }
  kill() {
    if (this._currentProcess) {
      this._currentProcess.kill();
      this._currentProcess = void 0;
      this._statusBarManager.update(0 /* Idle */);
    }
  }
};

// src/commands/compile.ts
var vscode4 = __toESM(require("vscode"));
async function compileSession(uri) {
  let sessionPath;
  if (uri) {
    sessionPath = uri.fsPath;
  } else {
    const activeEditor = vscode4.window.activeTextEditor;
    if (activeEditor && activeEditor.document.fileName.endsWith(".recrd")) {
      sessionPath = activeEditor.document.fileName;
    } else {
      const files = await vscode4.window.showOpenDialog({
        canSelectFiles: true,
        canSelectFolders: false,
        canSelectMany: false,
        filters: { "recrd Sessions": ["recrd"] },
        title: "Select .recrd session file to compile"
      });
      if (files && files.length > 0) {
        sessionPath = files[0].fsPath;
      }
    }
  }
  if (!sessionPath) {
    vscode4.window.showErrorMessage("No .recrd session selected for compilation.");
    return;
  }
  const target = await vscode4.window.showQuickPick(
    ["robot-browser", "robot-selenium", "gherkin"],
    { placeHolder: "Select compiler target" }
  );
  if (!target) {
    return;
  }
  const useData = await vscode4.window.showQuickPick(
    ["No", "Yes"],
    { placeHolder: "Use a data file (CSV/JSON)?" }
  );
  let dataPath;
  if (useData === "Yes") {
    const dataFiles = await vscode4.window.showOpenDialog({
      canSelectFiles: true,
      canSelectFolders: false,
      canSelectMany: false,
      filters: { "Data Files": ["csv", "json"] },
      title: "Select data file"
    });
    if (dataFiles && dataFiles.length > 0) {
      dataPath = dataFiles[0].fsPath;
    }
  }
  const outDirFiles = await vscode4.window.showOpenDialog({
    canSelectFiles: false,
    canSelectFolders: true,
    canSelectMany: false,
    title: "Select output directory (optional - defaults to current dir)"
  });
  const outDir = outDirFiles?.[0]?.fsPath;
  const args = ["compile", `"${sessionPath}"`, "--target", target];
  if (dataPath) {
    args.push("--data", `"${dataPath}"`);
  }
  if (outDir) {
    args.push("--out", `"${outDir}"`);
  }
  vscode4.window.withProgress({
    location: vscode4.ProgressLocation.Notification,
    title: "Compiling session...",
    cancellable: false
  }, async (progress) => {
    const exitCode = await CliRunner.runCommand(args);
    if (exitCode === 0) {
      vscode4.window.showInformationMessage(`Compilation successful for ${target}!`);
    } else {
      vscode4.window.showErrorMessage(`Compilation failed with exit code ${exitCode}. Check 'recrd' output channel for details.`);
    }
  });
}

// src/webviews/PreviewPanel.ts
var vscode5 = __toESM(require("vscode"));
var PreviewPanel = class _PreviewPanel {
  static currentPanel;
  _panel;
  _disposables = [];
  constructor(panel, extensionUri) {
    this._panel = panel;
    this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
    this._setWebviewMessageListener(this._panel.webview);
  }
  static createOrShow(extensionUri) {
    const column = vscode5.window.activeTextEditor ? vscode5.window.activeTextEditor.viewColumn : void 0;
    if (_PreviewPanel.currentPanel) {
      _PreviewPanel.currentPanel._panel.reveal(column);
      return;
    }
    const panel = vscode5.window.createWebviewPanel(
      "recrdPreview",
      "recrd: Live Preview",
      column || vscode5.ViewColumn.One,
      {
        enableScripts: true,
        localResourceRoots: [extensionUri]
      }
    );
    _PreviewPanel.currentPanel = new _PreviewPanel(panel, extensionUri);
  }
  update(gherkin, robot) {
    this._panel.webview.html = this._getHtmlForWebview(gherkin, robot);
  }
  _getHtmlForWebview(gherkin, robot) {
    return `
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>recrd Preview</title>
                <style>
                    body { font-family: var(--vscode-font-family); color: var(--vscode-foreground); }
                    pre { background: var(--vscode-editor-background); padding: 10px; border-radius: 4px; overflow: auto; }
                    .tab-container { display: flex; gap: 10px; margin-bottom: 10px; border-bottom: 1px solid var(--vscode-panel-border); }
                    .tab { cursor: pointer; padding: 5px 10px; }
                    .tab.active { border-bottom: 2px solid var(--vscode-button-background); font-weight: bold; }
                    .content { display: none; }
                    .content.active { display: block; }
                </style>
            </head>
            <body>
                <div class="tab-container">
                    <div id="tab-gherkin" class="tab active" onclick="showTab('gherkin')">Gherkin</div>
                    <div id="tab-robot" class="tab" onclick="showTab('robot')">Robot Framework</div>
                </div>
                <div id="content-gherkin" class="content active">
                    <pre><code>${this._escapeHtml(gherkin)}</code></pre>
                </div>
                <div id="content-robot" class="content">
                    <pre><code>${this._escapeHtml(robot)}</code></pre>
                </div>
                <script>
                    function showTab(tab) {
                        document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                        document.querySelectorAll('.content').forEach(c => t.classList.remove('active')); // Bug in my script, wait
                        
                        document.getElementById('tab-' + tab).classList.add('active');
                        document.querySelectorAll('.content').forEach(c => c.classList.remove('active'));
                        document.getElementById('content-' + tab).classList.add('active');
                    }
                </script>
            </body>
            </html>
        `;
  }
  _escapeHtml(unsafe) {
    return unsafe.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#039;");
  }
  _setWebviewMessageListener(webview) {
  }
  dispose() {
    _PreviewPanel.currentPanel = void 0;
    this._panel.dispose();
    while (this._disposables.length) {
      const x = this._disposables.pop();
      if (x) {
        x.dispose();
      }
    }
  }
};

// src/extension.ts
var statusBarManager;
var recordingManager;
function activate(context) {
  console.log("recrd extension is now active");
  statusBarManager = new StatusBarManager();
  recordingManager = new RecordingManager(statusBarManager);
  context.subscriptions.push(statusBarManager);
  context.subscriptions.push(
    vscode6.commands.registerCommand("recrd.showInfo", () => {
      vscode6.window.showInformationMessage("recrd E2E Recorder - Status Info");
    })
  );
  context.subscriptions.push(
    vscode6.commands.registerCommand("recrd.start", () => {
      recordingManager.start();
    })
  );
  context.subscriptions.push(
    vscode6.commands.registerCommand("recrd.stop", () => {
      recordingManager.stop();
    })
  );
  context.subscriptions.push(
    vscode6.commands.registerCommand("recrd.compile", (uri) => {
      compileSession(uri);
    })
  );
  context.subscriptions.push(
    vscode6.commands.registerCommand("recrd.showPreview", () => {
      PreviewPanel.createOrShow(context.extensionUri);
      refreshPreview();
    })
  );
  const watcher = vscode6.workspace.createFileSystemWatcher("**/*.recrd");
  watcher.onDidChange(() => refreshPreview());
  watcher.onDidCreate(() => refreshPreview());
  context.subscriptions.push(watcher);
}
async function refreshPreview() {
  if (!PreviewPanel.currentPanel) {
    return;
  }
  const activeEditor = vscode6.window.activeTextEditor;
  if (!activeEditor || !activeEditor.document.fileName.endsWith(".recrd")) {
    return;
  }
  const sessionPath = activeEditor.document.fileName;
  const tempDir = path.join(path.dirname(sessionPath), ".recrd-preview");
  try {
    await fs.mkdir(tempDir, { recursive: true });
    await CliRunner.runCommand(["compile", `"${sessionPath}"`, "--target", "gherkin", "--out", `"${tempDir}"`]);
    await CliRunner.runCommand(["compile", `"${sessionPath}"`, "--target", "robot-browser", "--out", `"${tempDir}"`]);
    const baseName = path.basename(sessionPath, ".recrd");
    const gherkinPath = path.join(tempDir, `${baseName}.feature`);
    const robotPath = path.join(tempDir, `${baseName}.robot`);
    const gherkin = await fs.readFile(gherkinPath, "utf-8").catch(() => "Gherkin not generated yet.");
    const robot = await fs.readFile(robotPath, "utf-8").catch(() => "Robot not generated yet.");
    PreviewPanel.currentPanel.update(gherkin, robot);
  } catch (err) {
    console.error("Failed to refresh preview:", err);
  }
}
function deactivate() {
  if (recordingManager) {
    recordingManager.kill();
  }
  if (statusBarManager) {
    statusBarManager.dispose();
  }
}
// Annotate the CommonJS export names for ESM import in node:
0 && (module.exports = {
  activate,
  deactivate
});
//# sourceMappingURL=extension.js.map
