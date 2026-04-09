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
var vscode2 = __toESM(require("vscode"));

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

// src/extension.ts
var statusBarManager;
function activate(context) {
  console.log("recrd extension is now active");
  statusBarManager = new StatusBarManager();
  context.subscriptions.push(statusBarManager);
  let showInfoDisposable = vscode2.commands.registerCommand("recrd.showInfo", () => {
    vscode2.window.showInformationMessage("recrd E2E Recorder - Status Info");
  });
  context.subscriptions.push(showInfoDisposable);
  let startDisposable = vscode2.commands.registerCommand("recrd.start", () => {
    statusBarManager.update(1 /* Recording */, 0);
    vscode2.window.showInformationMessage("Recording started (UI only)");
  });
  context.subscriptions.push(startDisposable);
  let stopDisposable = vscode2.commands.registerCommand("recrd.stop", () => {
    statusBarManager.update(0 /* Idle */);
    vscode2.window.showInformationMessage("Recording stopped (UI only)");
  });
  context.subscriptions.push(stopDisposable);
}
function deactivate() {
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
