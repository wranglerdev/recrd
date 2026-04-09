#!/bin/bash
set -e

# Package validation script for recrd
# Usage: ./validate-pkg.sh <package_dir>

PKG_DIR=$1

if [ -z "$PKG_DIR" ]; then
    echo "Usage: $0 <package_dir>"
    exit 1
fi

if [ ! -d "$PKG_DIR" ]; then
    echo "Error: Directory $PKG_DIR does not exist."
    exit 1
fi

echo "Validating package in: $PKG_DIR"

# 1. Main executable
if [ ! -f "$PKG_DIR/recrd" ] && [ ! -f "$PKG_DIR/recrd.exe" ]; then
    echo "Error: Main executable (recrd or recrd.exe) missing."
    exit 1
fi

# 2. .playwright/ directory
if [ ! -d "$PKG_DIR/.playwright" ]; then
    echo "Error: .playwright/ directory missing."
    exit 1
fi

# 3. .playwright/package/cli.js
if [ ! -f "$PKG_DIR/.playwright/package/cli.js" ]; then
    echo "Error: .playwright/package/cli.js missing."
    exit 1
fi

# 4. Platform-specific node binary
# We check for node in .playwright/node/ (could be .playwright/node/linux-x64/node etc, but plan says .playwright/node/)
# Actually, playwright usually puts it in a platform specific subfolder.
# Let's be a bit flexible but ensure 'node' exists somewhere under .playwright/node/
if [ -z "$(find "$PKG_DIR/.playwright/node" -name "node" -o -name "node.exe" 2>/dev/null)" ]; then
    echo "Error: Platform-specific node binary missing in .playwright/node/"
    exit 1
fi

echo "Package validation PASSED"
exit 0
