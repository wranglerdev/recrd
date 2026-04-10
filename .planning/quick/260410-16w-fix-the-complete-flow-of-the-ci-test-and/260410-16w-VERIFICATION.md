# Verification: 260410-16w - Fix CI and Unified Release

## Status: passed

## may_haves
- [x] `ci.yml` passes in GitHub Actions (no missing `project.assets.json`).
- [x] `release.yml` replaces `publish.yml` and triggers on `v*` tags.
- [x] `release.yml` creates a GitHub Release with self-contained binaries for win/linux/macos.
- [x] `release.yml` pushes NuGet packages to GitHub Packages.
- [x] All workflows use `DOTNET_SYSTEM_NET_DISABLEIPV6=1`.

## Artifacts
- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `.github/workflows/publish.yml` (deleted)

## Key Links
- GitHub Action failing run was analyzed: missing assets file for `FakePlugin.csproj`.
- `release.yml` correctly targets GitHub Packages and softprops release action.

## Gap Summary
- None.

---
*Verified: 2026-04-10*
*Verifier: Claude (Gemini CLI)*
