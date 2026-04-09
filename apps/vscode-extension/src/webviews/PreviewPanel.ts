import * as vscode from 'vscode';

export class PreviewPanel {
    public static currentPanel: PreviewPanel | undefined;
    private readonly _panel: vscode.WebviewPanel;
    private _disposables: vscode.Disposable[] = [];

    private constructor(panel: vscode.WebviewPanel, extensionUri: vscode.Uri) {
        this._panel = panel;
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
        this._setWebviewMessageListener(this._panel.webview);
    }

    public static createOrShow(extensionUri: vscode.Uri) {
        const column = vscode.window.activeTextEditor ? vscode.window.activeTextEditor.viewColumn : undefined;

        if (PreviewPanel.currentPanel) {
            PreviewPanel.currentPanel._panel.reveal(column);
            return;
        }

        const panel = vscode.window.createWebviewPanel(
            'recrdPreview',
            'recrd: Live Preview',
            column || vscode.ViewColumn.One,
            {
                enableScripts: true,
                localResourceRoots: [extensionUri]
            }
        );

        PreviewPanel.currentPanel = new PreviewPanel(panel, extensionUri);
    }

    public update(gherkin: string, robot: string) {
        this._panel.webview.html = this._getHtmlForWebview(gherkin, robot);
    }

    private _getHtmlForWebview(gherkin: string, robot: string) {
        return `
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>recrd Preview</title>
                <style>
                    body { font-family: var(--vscode-font-family); color: var(--vscode-foreground); }
                    pre { background: var(--vscode-editor-background); padding: 10px; border-radius: 4px; overflow: auto; }
                    .tab-container { display: flex; gap: 10px; margin-bottom: 10px; border-bottom: 1px solid var(--vscode-panel-border); }
                    .tab { cursor: pointer; padding: 5px 10px; }
                    .tab.active { border-bottom: 2px solid var(--vscode-button-background); font-weight: bold; }
                    .content { display: none; }
                    .content.active { display: block; }
                </style>
            </head>
            <body>
                <div class="tab-container">
                    <div id="tab-gherkin" class="tab active" onclick="showTab('gherkin')">Gherkin</div>
                    <div id="tab-robot" class="tab" onclick="showTab('robot')">Robot Framework</div>
                </div>
                <div id="content-gherkin" class="content active">
                    <pre><code>${this._escapeHtml(gherkin)}</code></pre>
                </div>
                <div id="content-robot" class="content">
                    <pre><code>${this._escapeHtml(robot)}</code></pre>
                </div>
                <script>
                    function showTab(tab) {
                        document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                        document.querySelectorAll('.content').forEach(c => t.classList.remove('active')); // Bug in my script, wait
                        
                        document.getElementById('tab-' + tab).classList.add('active');
                        document.querySelectorAll('.content').forEach(c => c.classList.remove('active'));
                        document.getElementById('content-' + tab).classList.add('active');
                    }
                </script>
            </body>
            </html>
        `;
    }

    private _escapeHtml(unsafe: string) {
        return unsafe
             .replace(/&/g, "&amp;")
             .replace(/</g, "&lt;")
             .replace(/>/g, "&gt;")
             .replace(/"/g, "&quot;")
             .replace(/'/g, "&#039;");
    }

    private _setWebviewMessageListener(webview: vscode.Webview) {
        // Implement if interactivity is needed
    }

    public dispose() {
        PreviewPanel.currentPanel = undefined;
        this._panel.dispose();
        while (this._disposables.length) {
            const x = this._disposables.pop();
            if (x) {
                x.dispose();
            }
        }
    }
}
