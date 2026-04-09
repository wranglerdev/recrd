import * as vscode from 'vscode';

export enum RecordingState {
    Idle,
    Recording,
    Paused
}

export class StatusBarManager {
    private _statusBarItem: vscode.StatusBarItem;

    constructor() {
        this._statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
        this._statusBarItem.command = 'recrd.showInfo';
        this.update(RecordingState.Idle);
        this._statusBarItem.show();
    }

    public update(state: RecordingState, elapsedSeconds?: number) {
        let text = '$(record) recrd: ';
        let tooltip = 'recrd E2E Recorder';
        let color: vscode.ThemeColor | undefined;

        switch (state) {
            case RecordingState.Idle:
                text += 'Idle';
                tooltip = 'recrd: Click to start recording';
                break;
            case RecordingState.Recording:
                text += 'Recording';
                if (elapsedSeconds !== undefined) {
                    text += ` (${this._formatTime(elapsedSeconds)})`;
                }
                tooltip = 'recrd: Recording interaction...';
                color = new vscode.ThemeColor('statusBarItem.errorBackground');
                break;
            case RecordingState.Paused:
                text += 'Paused';
                tooltip = 'recrd: Recording paused';
                color = new vscode.ThemeColor('statusBarItem.warningBackground');
                break;
        }

        this._statusBarItem.text = text;
        this._statusBarItem.tooltip = tooltip;
        this._statusBarItem.backgroundColor = color;
    }

    private _formatTime(seconds: number): string {
        const m = Math.floor(seconds / 60);
        const s = seconds % 60;
        return `${m}:${s.toString().padStart(2, '0')}`;
    }

    public dispose() {
        this._statusBarItem.dispose();
    }
}
