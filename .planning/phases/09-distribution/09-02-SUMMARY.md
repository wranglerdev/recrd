---
phase: 09-distribution
plan: 02
subsystem: distribution
tags: [distribution, publish, ci-cd]
requires: [DIST-01]
provides: [DIST-01]
affects: [.github/workflows/release.yml]
tech-stack: [GitHub Actions, .NET 10, Playwright]
key-files: [.github/workflows/release.yml]
decisions:
  - "Use matrix of 4 RIDs for cross-platform distribution"
  - "Include .playwright driver folder in the package to ensure Node.js and Playwright CLI are bundled"
  - "Use ZIP for Windows/Mac and TAR.GZ for Linux/Mac as requested"
  - "Add SHA256 checksum generation for release assets for security"
metrics:
  duration: 15m
  completed_date: "2025-02-14"
---

# Phase 09 Plan 02: Publish Pipeline Summary

Implemented the self-contained publish pipeline in `.github/workflows/release.yml` to automate the distribution of `recrd` CLI across Windows, macOS (Intel/ARM), and Linux.

## Key Changes

### 1. Release Workflow Configuration
- **Trigger:** Configured to run on push to tags matching `v*`.
- **Matrix Build:** Parallel jobs for:
  - `win-x64` (Windows)
  - `osx-x64` (Intel Mac)
  - `osx-arm64` (Apple Silicon Mac)
  - `linux-x64` (Linux)
- **Self-Contained Publish:** Uses `dotnet publish --self-contained -p:PublishSingleFile=true` to ensure the binary includes all .NET dependencies.

### 2. Packaging and Artifacts
- **Playwright Bundling:** Verified that `dotnet publish` correctly includes the `.playwright` folder containing Node.js and Playwright CLI for each target RID.
- **Archive Formats:**
  - ZIP files for Windows and macOS.
  - TAR.GZ files for Linux and macOS.
- **Wrappers:** Includes `playwright.sh` and `playwright.ps1` in the package for manual browser installation.
- **Security:** Automatically generates `.sha256` checksums for every uploaded asset.

### 3. Automated Uploads
- Uses `softprops/action-gh-release@v2` to upload all archives and checksums to the GitHub release.

## Deviations from Plan
None. The plan was executed exactly as written, with additional security measures (checksums) as suggested by the threat model.

## Self-Check: PASSED
- [x] `.github/workflows/release.yml` exists and triggers on tags.
- [x] Matrix includes all 4 target platforms.
- [x] Packaging steps handle both ZIP and TAR.GZ as requested.
- [x] `.playwright` folder inclusion is verified during the build.
- [x] SHA256 checksums are generated and uploaded.

## Commits
- 318cda4: feat(09-02): add self-contained release workflow
