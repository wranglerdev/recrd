import * as vscode from 'vscode';
import * as fs from 'fs/promises';
import * as path from 'path';
import { StatusBarManager } from './statusBar.js';
import { RecordingManager } from './commands/recording.js';
import { compileSession } from './commands/compile.js';
import { PreviewPanel } from './webviews/PreviewPanel.js';
import { CliRunner } from './cli/runner.js';

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

    context.subscriptions.push(
        vscode.commands.registerCommand('recrd.compile', (uri?: vscode.Uri) => {
            compileSession(uri);
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('recrd.showPreview', () => {
            PreviewPanel.createOrShow(context.extensionUri);
            refreshPreview();
        })
    );

    // File Watcher
    const watcher = vscode.workspace.createFileSystemWatcher('**/*.recrd');
    watcher.onDidChange(() => refreshPreview());
    watcher.onDidCreate(() => refreshPreview());
    context.subscriptions.push(watcher);
}

async function refreshPreview() {
    if (!PreviewPanel.currentPanel) { return; }

    const activeEditor = vscode.window.activeTextEditor;
    if (!activeEditor || !activeEditor.document.fileName.endsWith('.recrd')) { return; }

    const sessionPath = activeEditor.document.fileName;
    const tempDir = path.join(path.dirname(sessionPath), '.recrd-preview');
    
    try {
        await fs.mkdir(tempDir, { recursive: true });
        
        // Compile to Gherkin
        await CliRunner.runCommand(['compile', `"${sessionPath}"`, '--target', 'gherkin', '--out', `"${tempDir}"`]);
        // Compile to Robot
        await CliRunner.runCommand(['compile', `"${sessionPath}"`, '--target', 'robot-browser', '--out', `"${tempDir}"`]);

        const baseName = path.basename(sessionPath, '.recrd');
        const gherkinPath = path.join(tempDir, `${baseName}.feature`);
        const robotPath = path.join(tempDir, `${baseName}.robot`);

        const gherkin = await fs.readFile(gherkinPath, 'utf-8').catch(() => 'Gherkin not generated yet.');
        const robot = await fs.readFile(robotPath, 'utf-8').catch(() => 'Robot not generated yet.');

        PreviewPanel.currentPanel.update(gherkin, robot);
    } catch (err) {
        console.error('Failed to refresh preview:', err);
    }
}

export function deactivate() {
    if (recordingManager) {
        recordingManager.kill();
    }
    if (statusBarManager) {
        statusBarManager.dispose();
    }
}
