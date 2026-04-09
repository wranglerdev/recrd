import * as vscode from 'vscode';

export enum RecordingStatus {
    Idle,
    Recording,
    Paused
}

export class StatusBarManager {
    private _statusBarItem: vscode.StatusBarItem;
    private _status: RecordingStatus = RecordingStatus.Idle;

    constructor() {
        this._statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
        this._statusBarItem.command = 'recrd.showInfo';
        this.update();
        this._statusBarItem.show();
    }

    public setStatus(status: RecordingStatus) {
        this._status = status;
        this.update();
    }

    private update() {
        let text = '$(record) recrd: ';
        let tooltip = 'recrd E2E Recorder';

        switch (this._status) {
            case RecordingStatus.Idle:
                text += 'Idle';
                tooltip += ' (Idle)';
                break;
            case RecordingStatus.Recording:
                text += 'Recording';
                tooltip += ' (Recording...)';
                this._statusBarItem.backgroundColor = new vscode.ThemeColor('statusBarItem.errorBackground');
                break;
            case RecordingStatus.Paused:
                text += 'Paused';
                tooltip += ' (Paused)';
                this._statusBarItem.backgroundColor = new vscode.ThemeColor('statusBarItem.warningBackground');
                break;
        }

        this._statusBarItem.text = text;
        this._statusBarItem.tooltip = tooltip;

        if (this._status === RecordingStatus.Idle) {
            this._statusBarItem.backgroundColor = undefined;
        }
    }

    public dispose() {
        this._statusBarItem.dispose();
    }
}
