#!/usr/bin/env bash
# Playwright Linux Wrapper

# Determine the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Identify the OS and architecture
OS=$(uname -s | tr '[:upper:]' '[:lower:]')
ARCH=$(uname -m)

if [ "$ARCH" == "x86_64" ]; then
    ARCH="x64"
elif [ "$ARCH" == "aarch64" ]; then
    ARCH="arm64"
fi

# Define paths to bundled Node and Playwright CLI
# Note: These paths are relative to the output directory where playwright.sh is copied
NODE_PATH="$SCRIPT_DIR/.playwright/node/$OS-$ARCH/node"
CLI_PATH="$SCRIPT_DIR/.playwright/package/cli.js"

# Check for existence of Node.js
if [ ! -f "$NODE_PATH" ]; then
    echo "Error: Bundled Node.js not found at $NODE_PATH"
    echo "Please ensure you have built the project and that Microsoft.Playwright has initialized the .playwright directory."
    exit 1
fi

# Check for existence of Playwright CLI
if [ ! -f "$CLI_PATH" ]; then
    echo "Error: Playwright CLI not found at $CLI_PATH"
    exit 1
fi

# Run the command
"$NODE_PATH" "$CLI_PATH" "$@"
