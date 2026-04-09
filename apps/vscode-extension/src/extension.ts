import * as vscode from 'vscode';
import { StatusBarManager, RecordingState } from './statusBar.js';

let statusBarManager: StatusBarManager;

export function activate(context: vscode.ExtensionContext) {
    console.log('recrd extension is now active');

    statusBarManager = new StatusBarManager();
    context.subscriptions.push(statusBarManager);

    // Placeholder command for status bar click
    let showInfoDisposable = vscode.commands.registerCommand('recrd.showInfo', () => {
        vscode.window.showInformationMessage('recrd E2E Recorder - Status Info');
    });
    context.subscriptions.push(showInfoDisposable);

    // Basic start/stop placeholders to test state transitions
    let startDisposable = vscode.commands.registerCommand('recrd.start', () => {
        statusBarManager.update(RecordingState.Recording, 0);
        vscode.window.showInformationMessage('Recording started (UI only)');
    });
    context.subscriptions.push(startDisposable);

    let stopDisposable = vscode.commands.registerCommand('recrd.stop', () => {
        statusBarManager.update(RecordingState.Idle);
        vscode.window.showInformationMessage('Recording stopped (UI only)');
    });
    context.subscriptions.push(stopDisposable);
}

export function deactivate() {
    if (statusBarManager) {
        statusBarManager.dispose();
    }
}
