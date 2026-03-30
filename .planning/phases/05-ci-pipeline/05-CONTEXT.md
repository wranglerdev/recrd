# Phase 5: CI Pipeline - Context

**Gathered:** 2026-03-29 (discuss mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

Every push to the repository triggers a fully automated quality gate: build, test, coverage, format, with scheduled mutation testing and gated NuGet publish on `main`. Scope is GitHub Actions YAML configuration only — no new application code.

Existing `.github/workflows/ci.yml` already covers: restore → build → core-isolation-check → test → format check. This phase adds the four missing pieces: per-project coverage gate, weekly Stryker.NET mutation run, gated NuGet publish, and TDD red-phase branch handling.
</domain>

<decisions>
## Implementation Decisions

### Coverage Enforcement
- **D-01:** Coverlet inline thresholds — pass `--threshold 90 --threshold-type line --threshold-stat minimum` to each gated `dotnet test` invocation. Coverlet fails the step with the project name in the error message; no extra tooling required.
- **D-02:** Separate `dotnet test` invocations per gated project — fail fast on the first project that drops below 90%. Simpler YAML, faster feedback loop.
- **D-03:** Gated projects: `Recrd.Core`, `Recrd.Data`, `Recrd.Gherkin`, `Recrd.Compilers` (as specified by CI-02). `Recrd.Recording` and integration tests are excluded from the gate.

### Mutation Testing
- **D-04:** Stryker.NET report destination: GitHub Actions Summary only (`$GITHUB_STEP_SUMMARY`). No PR comment, no tracking issue, no artifact upload. No extra tokens or write permissions needed.
- **D-05:** Report-only — the weekly mutation workflow always succeeds regardless of mutation score. Score is informational, never a build blocker.

### NuGet Publish
- **D-06:** Tag trigger pattern: `v*-*` (pre-release only). Tags matching `v1.0.0-preview.1`, `v1.0.0-alpha.2`, etc. trigger the publish pipeline. Stable tags (e.g., `v1.0.0`) do NOT auto-publish.
- **D-07:** Target feed: GitHub Packages only. Uses `GITHUB_TOKEN` (auto-provided by Actions — no additional secret required).

### TDD Red-Phase Branch Handling
- **D-08:** On `tdd/phase-*` branches, the test step uses `continue-on-error: true` — test failures do not fail the workflow.
- **D-09:** All other steps remain enforced on `tdd/phase-*` branches: core isolation check, per-project coverage gate, and `dotnet format --verify-no-changes`. Red branches must still compile, be well-formatted, and maintain Core isolation.
- **D-10:** Implementation via conditional steps: two versions of the test step — one with `if: startsWith(github.ref_name, 'tdd/phase-')` and `continue-on-error: true`, one without the condition for all other branches.

### Claude's Discretion
- Exact YAML step naming and grouping within the workflow file
- Whether to split into multiple workflow files or keep everything in `ci.yml`
- Stryker.NET configuration file location and Stryker version pinning
- Specific `dotnet-stryker` tool installation approach (global tool vs manifest)
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` §Foundation — CI Pipeline (CI-01 through CI-06) — exact acceptance criteria for all six CI requirements
- `.planning/ROADMAP.md` §Phase 5 — Success Criteria (6 items) — the verification targets

### Existing CI
- `.github/workflows/ci.yml` — current workflow to extend; existing steps must be preserved and not regressed
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `.github/workflows/ci.yml` — existing workflow with restore → build → core-isolation-check → test → format check. All existing steps carry forward; phase adds to them, does not replace.
- `Directory.Build.props` — shared MSBuild properties for all projects; no coverage threshold configuration currently present.
- `recrd.sln` — solution file; all `dotnet test` invocations should filter to specific test projects in `tests/`.

### Established Patterns
- All `dotnet` commands must be prefixed with `DOTNET_SYSTEM_NET_DISABLEIPV6=1` on Linux runners to force IPv4 (NuGet restore hangs on IPv6). This applies to every `run:` step in the workflow.
- Test projects follow `tests/Recrd.{Package}.Tests` naming: `Recrd.Core.Tests`, `Recrd.Data.Tests`, `Recrd.Gherkin.Tests`, `Recrd.Compilers.Tests`, `Recrd.Integration.Tests`, `Recrd.Recording.Tests`.
- Integration tests are filtered out of the main CI run with `--filter "Category!=Integration"`.

### Integration Points
- Coverage gate steps integrate with existing test step — they will run the four gated test projects individually with thresholds, replacing or supplementing the existing combined test step.
- Mutation workflow (`mutation.yml`) is a separate workflow file triggered by `schedule: cron` — does not affect the main `ci.yml` flow.
- NuGet publish workflow (`publish.yml`) is a separate workflow file triggered by `push: tags: ['v*-*']`.
</code_context>

<specifics>
## Specific Ideas

- `continue-on-error: true` pattern for TDD red-phase (from discussion): two conditional `Test` steps, one for `tdd/phase-*` branches and one for all others.
- Coverage enforcement example from discussion: per-project invocations emitting `Recrd.Core: Line coverage 87.3% is below threshold 90%` style messages via Coverlet.
</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.
</deferred>

---

*Phase: 05-ci-pipeline*
*Context gathered: 2026-03-29*
