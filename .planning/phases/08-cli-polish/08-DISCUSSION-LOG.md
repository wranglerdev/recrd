# Phase 8: CLI Polish - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md — this log preserves the discussion.

**Date:** 2026-04-07
**Phase:** 08-cli-polish
**Mode:** discuss
**Areas discussed:** Session IPC, CLI Framework, Terminal Output Style, sanitize I/O

---

## Gray Areas Presented

| Area | Description |
|------|-------------|
| IPC for live session | How pause/resume/stop communicate with the running start process |
| CLI framework choice | System.CommandLine vs Spectre.Console.Cli vs minimal |
| Terminal output style | Plain text vs rich colored output |
| sanitize I/O behavior | New file vs in-place overwrite |

All four areas selected by user.

---

## Decisions Made

### Session IPC

| Question | Answer |
|----------|--------|
| IPC mechanism | Named pipe / Unix domain socket at `~/.recrd/session.sock` |
| Single vs multi-session | Single session — start fails if socket already exists |

**Rationale:** Cross-platform (unlike OS signals), clean protocol, consistent with tools like Docker and Playwright's test runner.

### CLI Framework

| Question | Answer |
|----------|--------|
| Framework | System.CommandLine (Microsoft) |

**Rationale:** Trimming/NativeAOT support for < 500 ms cold-start. Built-in --help, --verbosity, tab completion.

### Terminal Output Style

| Question | Answer |
|----------|--------|
| Output richness | Plain text + ANSI color, no Spectre.Console |

**Rationale:** Scriptable, minimal binary size, no heavy deps. JSON mode handles machine output.

### sanitize I/O

| Question | Answer |
|----------|--------|
| Output file | New file alongside original: `<basename>.sanitized.recrd` |

**Rationale:** Original never modified. Safe by default; `--out` overrides output path.

---

## No Corrections

All recommended options accepted without correction.

---

## No External Research

Codebase provided sufficient context for all decisions.
