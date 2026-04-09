import * as vscode from 'vscode';
import { StatusBarManager } from './statusBar.js';
import { RecordingManager } from './commands/recording.js';

let statusBarManager: StatusBarManager;
let recordingManager: RecordingManager;

export function activate(context: vscode.ExtensionContext) {
    console.log('recrd extension is now active');

    statusBarManager = new StatusBarManager();
    recordingManager = new RecordingManager(statusBarManager);
    
    context.subscriptions.push(statusBarManager);

    // Register commands
    context.subscriptions.push(
        vscode.commands.registerCommand('recrd.showInfo', () => {
            vscode.window.showInformationMessage('recrd E2E Recorder - Status Info');
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('recrd.start', () => {
            recordingManager.start();
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('recrd.stop', () => {
            recordingManager.stop();
        })
    );
}

export function deactivate() {
    if (recordingManager) {
        recordingManager.kill();
    }
    if (statusBarManager) {
        statusBarManager.dispose();
    }
}
