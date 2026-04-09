---
phase: 09-distribution
plan: 04
subsystem: distribution
tags: [homebrew, winget, documentation, CI]
requirements: [DIST-03, DIST-04]
tech-stack: [Homebrew, Winget, GitHub Actions]
key-files: [dist/homebrew/recrd.rb, dist/winget/recrd.yaml, README.md, docs/INSTALL.md, .github/workflows/release.yml]
metrics:
  duration: "10m"
  completed_at: "2026-04-09T05:30:00Z"
  tasks_completed: 3
  files_modified: 5
---

# Phase 09 Plan 04: Distribution Manifests Summary

Created distribution manifests for Homebrew and Winget, and updated documentation to include platform-specific installation instructions. Integrated manifest validation into the GitHub Actions release workflow.

## Key Changes

### Homebrew Formula
- Created a Homebrew formula template in `dist/homebrew/recrd.rb`.
- The formula handles both Intel (x64) and Apple Silicon (ARM64) macOS.
- Uses `libexec` to store the binary and the `.playwright` driver, with a relative symlink to `bin/recrd` for a clean install.

### Winget Manifest
- Created a Winget manifest template in `dist/winget/recrd.yaml`.
- Follows the standard `portable` installer type for `.zip` artifacts.
- Includes metadata for the `recrd` package and placeholder URLs for Windows x64.

### Release Workflow Integration
- Added a `validate` job to `.github/workflows/release.yml` that runs on `macos-latest` and `windows-latest`.
- The job substitutes placeholders (`{{VERSION}}`, `{{SHA256}}`) with actual values from built artifacts.
- Performs a dry-run install of the Homebrew formula and verifies the binary version.
- Validates the Winget manifest syntax where possible.

### Documentation
- Updated `README.md` with a comprehensive English overview and "Installation" section.
- Created `docs/INSTALL.md` with detailed steps for Homebrew, Winget, direct download, and manual build.
- Added explicit mention of the `.playwright` directory requirement.

## Deviations from Plan

None - plan executed as written.

## Self-Check: PASSED

- [x] Homebrew formula created at `dist/homebrew/recrd.rb`
- [x] Winget manifest created at `dist/winget/recrd.yaml`
- [x] README updated with installation section
- [x] docs/INSTALL.md created
- [x] release.yml updated with validation job
- [x] Commit 54d8334: feat(09-04): create Homebrew formula and add validation to release workflow
- [x] Commit 9849d1a: feat(09-04): create Winget manifest template
- [x] Commit 24370b6: docs(09-04): add installation instructions for Homebrew and Winget
