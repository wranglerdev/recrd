# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

`recrd` is a .NET 10 CLI tool that records browser interactions, emits pt-BR Gherkin (`.feature` files), and compiles executable Robot Framework test suites with data-driven testing support.

## Build & Test Commands

> The project is in the specification phase — the following commands apply once implementation begins.

> **Linux networking note:** Always prefix `dotnet` commands with `DOTNET_SYSTEM_NET_DISABLEIPV6=1` to force IPv4. Without it, NuGet restore hangs waiting for IPv6 responses.

```bash
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet restore
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet build --no-restore
```

### Playwright Browser Installation (Linux)

After building `Recrd.Recording`, install browsers using the shell script (not `.ps1` — PowerShell is not available):

```bash
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet build packages/Recrd.Recording
bash packages/Recrd.Recording/bin/Debug/net10.0/playwright.sh install
```

```bash
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet build --no-restore
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --collect:"XPlat Code Coverage"
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet test --no-build --filter "FullyQualifiedName~Recrd.Data.Tests"  # run a single test project
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet format --verify-no-changes                                      # lint/code style check
```

CI pipeline (`.github/workflows`): restore → build → test → coverage gate → format check. Weekly scheduled mutation testing via Stryker.NET on `Recrd.Core`. On `main` only: `dotnet pack` → NuGet push.

## Monorepo Structure

```
recrd/
├── apps/
│   ├── recrd-cli/          .NET 10 console app (entry point)
│   └── vscode-extension/   TypeScript VS Code extension (thin CLI wrapper)
├── packages/
│   ├── Recrd.Core/         AST types, all interfaces, Channel<T> pipeline
│   ├── Recrd.Recording/    CDP/Playwright recording engine (heavy dep — isolated)
│   ├── Recrd.Data/         CSV and JSON IDataProvider implementations
│   ├── Recrd.Gherkin/      AST → pt-BR .feature generator
│   └── Recrd.Compilers/    robot-browser and robot-selenium ITestCompiler implementations
├── plugins/                Example third-party plugin implementations
├── tests/                  Test projects mirroring packages/ structure
├── Directory.Build.props   Shared MSBuild properties
└── recrd.sln
```

## Architecture

### Data Flow

```
Browser (Playwright/CDP)
  → RecordedEvent via Channel<T>
  → AST Builder
  → .recrd session file (JSON, versioned with schemaVersion field)
      ├── Recrd.Gherkin  → .feature (pt-BR Gherkin)
      ├── Recrd.Data     → row data merged into Exemplos table
      └── Recrd.Compilers → .robot suite + .resource file
```

### Key Interfaces (all in `Recrd.Core`)

| Interface | Implemented in |
|---|---|
| `IRecorderEngine` | `Recrd.Recording` |
| `ITestCompiler` | `Recrd.Compilers` (robot-browser, robot-selenium) |
| `IDataProvider` | `Recrd.Data` (CSV, JSON) |
| `IEventInterceptor` | Plugin extension point |
| `IAssertionProvider` | Plugin extension point |

### Dependency Rules

- `Recrd.Core` depends on zero other `Recrd.*` packages — enforced in CI.
- `Recrd.Recording` is separate from `Recrd.Core` to avoid pulling Playwright's ~200 MB browser binaries into compile-only consumers.
- No circular dependencies permitted (verified via `dotnet dependency-graph`).

### Plugin System

Plugins are NuGet packages named `Recrd.Plugin.*`, loaded at startup from `~/.recrd/plugins/` via `AssemblyLoadContext` isolation. The host rejects plugins built against an incompatible major version of `Recrd.Core`.

## AST & Session Format

The `.recrd` file is a UTF-8 JSON file with `"schemaVersion": 1` at root. Schema:

```
Session
├── metadata: { id, createdAt, browserEngine, viewportSize, baseUrl }
├── variables: Variable[]
└── steps: (ActionStep | AssertionStep | GroupStep)[]
```

`GroupStep` maps to BDD sections: `given` → `Dado`, `when` → `Quando`, `then` → `Então`. Default heuristic when no grouping: first navigation → `Dado`, interactions → `Quando`, assertions → `Então`.

## Gherkin Output Rules

- Session with zero variables → `Cenário`
- Session with ≥ 1 variable → `Esquema do Cenário` + `Exemplos` table
- Variable naming: `^[a-z][a-z0-9_]{0,63}$`
- Missing data column for a variable = hard error; extra data column = warning to stderr
- Output must be deterministic: same AST + same data = byte-identical `.feature`

## Compiler Output

Both compilers (`robot-browser`, `robot-selenium`) emit a header comment in every generated file with: `recrd` version, compilation timestamp, source `.recrd` SHA-256, and compiler target name.

Selector priority: `data-testid` > `id` > `role`-based > CSS class chain > XPath. Configurable via `--selector-strategy`.

## VS Code Extension

Located in `apps/vscode-extension/`. Communicates with CLI exclusively via stdout/stderr and exit codes — no proprietary IPC. Uses `fs.watch` on the `.recrd` file for live preview via WebView.

## Test Strategy

- Unit tests: xUnit + Moq, ≥ 90% line coverage on Core/Data/Gherkin/Compilers
- Integration tests: xUnit + TestContainers
- E2E: full `record → compile → execute` round-trips against a fixture web app
- `IDataProvider` contract requires `IAsyncEnumerable<T>` streaming; batch size ≤ 1000 rows in-memory
