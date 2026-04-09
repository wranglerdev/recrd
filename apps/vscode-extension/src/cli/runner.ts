import * as vscode from 'vscode';
import * as cp from 'child_process';

export class CliRunner {
    private static _outputChannel: vscode.OutputChannel;

    public static get outputChannel(): vscode.OutputChannel {
        if (!this._outputChannel) {
            this._outputChannel = vscode.window.createOutputChannel('recrd');
        }
        return this._outputChannel;
    }

    public static spawn(args: string[]): cp.ChildProcess {
        const config = vscode.workspace.getConfiguration('recrd');
        const executablePath = config.get<string>('executablePath') || 'recrd';

        this.outputChannel.appendLine(`[CLI] Executing: ${executablePath} ${args.join(' ')}`);

        const childProcess = cp.spawn(executablePath, args, {
            shell: true,
            env: { ...process.env, DOTNET_SYSTEM_NET_DISABLEIPV6: '1' }
        });

        childProcess.stdout?.on('data', (data) => {
            this.outputChannel.append(data.toString());
        });

        childProcess.stderr?.on('data', (data) => {
            this.outputChannel.append(data.toString());
        });

        childProcess.on('error', (err) => {
            this.outputChannel.appendLine(`[CLI Error] ${err.message}`);
            vscode.window.showErrorMessage(`Failed to start recrd CLI: ${err.message}`);
        });

        return childProcess;
    }

    public static async runCommand(args: string[]): Promise<number> {
        return new Promise((resolve) => {
            const childProcess = this.spawn(args);
            childProcess.on('close', (code) => {
                resolve(code || 0);
            });
        });
    }
}
