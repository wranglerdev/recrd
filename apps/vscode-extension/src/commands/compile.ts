import * as vscode from 'vscode';
import { CliRunner } from '../cli/runner.js';

export async function compileSession(uri?: vscode.Uri) {
    let sessionPath: string | undefined;

    if (uri) {
        sessionPath = uri.fsPath;
    } else {
        const activeEditor = vscode.window.activeTextEditor;
        if (activeEditor && activeEditor.document.fileName.endsWith('.recrd')) {
            sessionPath = activeEditor.document.fileName;
        } else {
            const files = await vscode.window.showOpenDialog({
                canSelectFiles: true,
                canSelectFolders: false,
                canSelectMany: false,
                filters: { 'recrd Sessions': ['recrd'] },
                title: 'Select .recrd session file to compile'
            });
            if (files && files.length > 0) {
                sessionPath = files[0].fsPath;
            }
        }
    }

    if (!sessionPath) {
        vscode.window.showErrorMessage('No .recrd session selected for compilation.');
        return;
    }

    // 1. Select Target
    const target = await vscode.window.showQuickPick(
        ['robot-browser', 'robot-selenium', 'gherkin'],
        { placeHolder: 'Select compiler target' }
    );

    if (!target) { return; }

    // 2. Select Data File (optional)
    const useData = await vscode.window.showQuickPick(
        ['No', 'Yes'],
        { placeHolder: 'Use a data file (CSV/JSON)?' }
    );

    let dataPath: string | undefined;
    if (useData === 'Yes') {
        const dataFiles = await vscode.window.showOpenDialog({
            canSelectFiles: true,
            canSelectFolders: false,
            canSelectMany: false,
            filters: { 'Data Files': ['csv', 'json'] },
            title: 'Select data file'
        });
        if (dataFiles && dataFiles.length > 0) {
            dataPath = dataFiles[0].fsPath;
        }
    }

    // 3. Select Output Directory (optional)
    const outDirFiles = await vscode.window.showOpenDialog({
        canSelectFiles: false,
        canSelectFolders: true,
        canSelectMany: false,
        title: 'Select output directory (optional - defaults to current dir)'
    });
    const outDir = outDirFiles?.[0]?.fsPath;

    // 4. Run CLI
    const args = ['compile', `"${sessionPath}"`, '--target', target];
    if (dataPath) {
        args.push('--data', `"${dataPath}"`);
    }
    if (outDir) {
        args.push('--out', `"${outDir}"`);
    }

    vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        title: "Compiling session...",
        cancellable: false
    }, async (progress) => {
        const exitCode = await CliRunner.runCommand(args);
        if (exitCode === 0) {
            vscode.window.showInformationMessage(`Compilation successful for ${target}!`);
        } else {
            vscode.window.showErrorMessage(`Compilation failed with exit code ${exitCode}. Check 'recrd' output channel for details.`);
        }
    });
}
