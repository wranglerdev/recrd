import * as vscode from 'vscode';
import { StatusBarManager, RecordingStatus } from './statusBar';

let statusBarManager: StatusBarManager;

export function activate(context: vscode.ExtensionContext) {
    console.log('recrd extension is now active!');

    statusBarManager = new StatusBarManager();
    context.subscriptions.push(statusBarManager);

    // Register placeholder command
    const showInfoDisposable = vscode.commands.registerCommand('recrd.showInfo', () => {
        vscode.window.showInformationMessage('recrd E2E Recorder: Placeholder for status details.');
    });
    context.subscriptions.push(showInfoDisposable);

    // Mock cycle status for demonstration if needed
    // In actual use, this will be tied to recording engine state
}

export function deactivate() {
    if (statusBarManager) {
        statusBarManager.dispose();
    }
}
