# Phase 11: Plugin System - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md — this log preserves the discussion.

**Date:** 2026-04-09
**Phase:** 11-plugin-system
**Mode:** discuss

## Gray Areas Presented

| Area | Question | Options presented |
|------|----------|-------------------|
| Plugin directory layout | AssemblyDependencyResolver requires DLL + .deps.json together — flat vs subdirectory? | Subdirectory-per-plugin / Flat layout |
| `plugins install` behavior | Implement NuGet download or stay helpful stub? | Stay stub (manual copy) / Implement NuGet download |
| `plugins list` richness | Just names or informative table with interfaces + status? | Informative table / Minimal names only |

## Decisions Made

### Plugin Directory Layout
- **Chosen:** Subdirectory-per-plugin
- `~/.recrd/plugins/<PluginName>/` with DLL + `.deps.json` + transitive deps
- Rationale: Required for `AssemblyDependencyResolver` to resolve transitive NuGet dependencies

### `plugins install` Behavior
- **Chosen:** Stay stub — manual copy with helpful instructions
- Print `dotnet publish` + copy instructions, exit code 0
- Rationale: Keeps CLI self-contained; no NuGet HTTP surface; good enough for v1

### `plugins list` Output
- **Chosen:** Informative table
- Columns: name, version, interfaces provided, load status (✓ loaded / ✗ version mismatch)
- Rationale: Users need to know whether their plugin actually loaded and what it provides

## No Corrections
All recommendations accepted.

## Prior Context Applied
- Phase 8 D-05/D-06/D-07 (plain-text ANSI output style, no Spectre.Console) — carried forward to plugin table output
- Phase 8 Claude's Discretion item "How `plugins list` discovers assemblies" — resolved here: subdirectory scan + ALC loading
- PROJECT.md / STATE.md `AssemblyLoadContext` decision — reconfirmed, no discussion needed
