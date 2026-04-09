import * as vscode from 'vscode';
import * as cp from 'child_process';
import { CliRunner } from '../cli/runner.js';
import { StatusBarManager, RecordingState } from '../statusBar.js';

export class RecordingManager {
    private _currentProcess: cp.ChildProcess | undefined;

    constructor(private _statusBarManager: StatusBarManager) {}

    public async start() {
        if (this._currentProcess) {
            vscode.window.showWarningMessage('A recording is already in progress.');
            return;
        }

        const baseUrl = await vscode.window.showInputBox({
            prompt: 'Enter Base URL for recording (optional)',
            placeHolder: 'https://example.com'
        });

        const args = ['start'];
        if (baseUrl) {
            args.push('--base-url', baseUrl);
        }

        this._currentProcess = CliRunner.spawn(args);
        this._statusBarManager.update(RecordingState.Recording, 0);

        this._currentProcess.on('close', (code) => {
            CliRunner.outputChannel.appendLine(`[CLI] recrd start exited with code ${code}`);
            this._currentProcess = undefined;
            this._statusBarManager.update(RecordingState.Idle);
        });
    }

    public async stop() {
        // We use the 'recrd stop' command which communicates via IPC socket
        CliRunner.outputChannel.appendLine('[CLI] Sending stop signal...');
        const exitCode = await CliRunner.runCommand(['stop']);
        
        if (exitCode !== 0) {
            vscode.window.showErrorMessage('Failed to stop recording gracefully. Killing process...');
            this.kill();
        }
    }

    public kill() {
        if (this._currentProcess) {
            this._currentProcess.kill();
            this._currentProcess = undefined;
            this._statusBarManager.update(RecordingState.Idle);
        }
    }
}
