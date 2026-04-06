# Phase 7: Compilers - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-05
**Phase:** 07-compilers
**Areas discussed:** RF output structure, E2E fixture app, Selenium wait approach, Unresolvable selector handling

---

## RF Output Structure

| Option | Description | Selected |
|--------|-------------|----------|
| Page-object keywords | .resource contains higher-level page keywords (pt-BR named); .robot test case calls them. Idiomatic RF7. | ✓ |
| Thin shim — inline in .robot | .resource has only Library + suite setup/teardown; all step keywords inlined in .robot test case. | |

**User's choice:** Page-object keywords in .resource

---

### Keyword naming

| Option | Description | Selected |
|--------|-------------|----------|
| Action + element label in pt-BR | Slug-normalize selector value, prefix with pt-BR verb. E.g., Click data-testid="submit-btn" → "Clicar Em Submit Btn" | ✓ |
| Action + selector verbatim | Raw selector in keyword name — predictable but ugly. | |
| Step index fallback | Semantic name when possible; "Passo_N" for messy selectors. | |

**User's choice:** Action + element label in pt-BR

---

## E2E Fixture App

### Fixture coverage

| Option | Description | Selected |
|--------|-------------|----------|
| Full action type coverage | Static HTML with one element per ActionType (Click, Type, Select, Upload, DragDrop, Navigate) + assertable text/URL. | ✓ |
| Happy path only | Click + type + navigate + one assertion. DragDrop/Upload covered by unit tests. | |
| Two pages: static HTML + SPA | Static page + vanilla JS SPA for cross-page navigation. | |

**User's choice:** Full action type coverage

---

### Fixture serving (recording side)

| Option | Description | Selected |
|--------|-------------|----------|
| Playwright Page.SetContentAsync | No server, no ports. Consistent with Phase 6 recording tests. | ✓ |
| Kestrel TestServer (in-process) | Real HTTP server; supports multi-page navigation. | |
| Playwright BrowserContext.RouteAsync | Intercept requests; real URLs, no port binding. | |

**User's choice:** Page.SetContentAsync (recording side)

---

### Execute step (round-trip)

| Option | Description | Selected |
|--------|-------------|----------|
| RF subprocess + Kestrel fixture server | Kestrel serves fixture at localhost; Process.Start("robot") runs compiled suite; assert exit code = 0. | ✓ |
| robot --dryrun | Validates syntax/keywords without browser execution. No server needed. | |
| Compile-and-assert only | Assert compiled output content matches expected RF7 patterns. No RF binary required. | |

**User's choice:** Robot Framework subprocess + Kestrel fixture server

**Notes:** CI must install `robotframework`, `robotframework-browser`, and `robotframework-seleniumlibrary` as a new dependency in the Phase 7 CI step.

---

## Selenium Wait Approach

### Wait model

| Option | Description | Selected |
|--------|-------------|----------|
| Implicit wait only | "Set Selenium Implicit Wait ${TIMEOUT}s" in Suite Setup keyword. No per-step boilerplate. | ✓ |
| Explicit wait per interaction | "Wait Until Element Is Visible" before each interaction — mirrors robot-browser output. | |

**User's choice:** Implicit wait only

---

### Timeout source

| Option | Description | Selected |
|--------|-------------|----------|
| Use CompilerOptions.TimeoutSeconds | Uniform for both compilers; COMP-06's "10s" superseded by defined interface default (30s). | ✓ |
| Override default to 10s for Selenium only | Add SeleniumTimeoutSeconds to CompilerOptions. More granular but modifies Core type. | |

**User's choice:** CompilerOptions.TimeoutSeconds (uniform across compilers)

---

## Unresolvable Selector Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Warning + XPath fallback | Walk full priority chain; if exhausted, emit warning in CompilationResult.Warnings and use last available value. Never throw. | ✓ |
| Hard error — CompilerException | Data integrity failure; throw. User must fix session. Consistent with Gherkin's hard-error for missing variable columns. | |
| Placeholder comment + warning | Emit "# TODO: no selector found for step N" comment; test step is a no-op. | |

**User's choice:** Warning + XPath fallback (always produce valid RF output)

---

## Claude's Discretion

- Exact keyword slug normalization algorithm
- DependencyManifest content
- Suite Teardown placement (.resource vs .robot)
- Internal compiler class structure (helpers vs monolithic)
- Whether E2E round-trip lives in Recrd.Integration.Tests or a new sub-suite
