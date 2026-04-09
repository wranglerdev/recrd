# recrd for VS Code

`recrd` is a powerful E2E test recorder and Robot Framework compiler. This extension provides a seamless interface to the `recrd` CLI directly from VS Code.

## Features

- **One-Click Recording:** Start and stop browser interaction recordings from the status bar.
- **Live Preview:** See your Gherkin and Robot Framework code generated in real-time as you record.
- **Integrated Compilation:** Compile your `.recrd` sessions into executable Robot Framework suites with custom data providers.
- **Status Visualization:** Real-time recording state and elapsed time in the status bar.

## Requirements

The `recrd` CLI must be installed on your system. You can configure the path to the executable in the extension settings.

## Extension Settings

- `recrd.executablePath`: Path to the `recrd` CLI executable (defaults to `recrd`).

## Usage

1. Open a workspace.
2. Click the `$(record)` icon in the status bar to start recording.
3. Perform your browser interactions.
4. Open the `.recrd` file and click the **Live Preview** icon in the editor title bar to see generated code.
5. Right-click a `.recrd` file in the Explorer to compile it.

## License

MIT
