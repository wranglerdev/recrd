# GEMINI.md - Project Context & Instructions

This project, `recrd`, is a .NET 10 (LTS) CLI tool designed to automate the creation of E2E tests by recording browser interactions and compiling them into pt-BR Gherkin (`.feature`) files and executable Robot Framework test suites.

## 🚀 Quick Start (Development)

### Environment Prerequisites
- **.NET SDK:** 10.0.100 or later (check `global.json`)
- **Node.js:** v22+ (required for `rfbrowser init` and VS Code extension)
- **Python:** For Robot Framework execution
- **Playwright:** Browsers must be installed for recording/testing

### Critical Linux Network Note
Always set the following environment variable to prevent NuGet restore hangs:
```bash
export DOTNET_SYSTEM_NET_DISABLEIPV6=1
```

### Build & Test Commands
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build --no-restore

# Install Playwright browsers (required for Recording engine tests)
bash packages/Recrd.Recording/bin/Debug/net10.0/playwright.sh install chromium

# Run all tests with coverage gate (90% threshold)
dotnet test --no-build

# Check code formatting
dotnet format --verify-no-changes
```

## 🏗️ Architecture & Monorepo Structure

The project follows a clean, plugin-oriented architecture centered around an Abstract Syntax Tree (AST).

### Project Layout
- `apps/recrd-cli`: The main entry point (Console App).
- `apps/vscode-extension`: TypeScript wrapper for VS Code integration.
- `packages/Recrd.Core`: **The Core.** Contains AST models and all interfaces. It must have **zero** references to other `Recrd.*` projects.
- `packages/Recrd.Recording`: Handles browser communication via Playwright .NET and CDP.
- `packages/Recrd.Data`: Implements `IDataProvider` for CSV and JSON data injection.
- `packages/Recrd.Gherkin`: Translates the AST into pt-BR Gherkin.
- `packages/Recrd.Compilers`: Translates the AST into Robot Framework (`robot-browser` and `robot-selenium`).
- `tests/`: Parallel directory containing xUnit test projects for each package.

### Key Interfaces
- `IRecorderEngine`: Browser interaction capture.
- `ITestCompiler`: AST to test suite translation.
- `IDataProvider`: Data source parsing (CSV/JSON/etc).
- `IEventInterceptor` / `IAssertionProvider`: Extensibility points for plugins.

## 🛠️ Development Conventions

### Test-Driven Development (TDD)
- All new features **must** include tests that verify the change.
- **Line Coverage:** A minimum of **90%** coverage is required for all core packages (Core, Data, Gherkin, Compilers, Recording).
- **Integration Tests:** Use `Category=Integration` filter for E2E round-trip tests (`record → compile → execute`).

### Coding Standards
- **Idiomatic .NET:** Use C# 13 features where applicable (Primary constructors, Collection expressions, etc.).
- **Immutability:** Prefer `record` and `IReadOnlyList<T>` for AST models and data structures.
- **Async/Await:** All I/O and browser operations must be asynchronous.
- **Naming:** Variables in AST/Gherkin follow `^[a-z][a-z0-9_]{0,63}$`.

### Data Flow
1. **Record:** Browser events → `RecordedEvent` (via System.Threading.Channels) → `AST`.
2. **Persist:** `AST` → `.recrd` (JSON file, versioned).
3. **Generate:** `.recrd` + `Data` → `.feature` (pt-BR Gherkin).
4. **Compile:** `.recrd` → `.robot` + `.resource` (Robot Framework).

## 📄 Output Formats
- **Gherkin:** Always pt-BR. Deterministic output (same input = same bytes).
- **Robot Framework:** Supports both `Browser` (Playwright-based) and `SeleniumLibrary` targets.
- **Traceability:** Every generated file includes a header with the `recrd` version, timestamp, and source session SHA-256.

## 🔌 Plugin System
Plugins are NuGet packages (`Recrd.Plugin.*`) loaded from `~/.recrd/plugins/`. They use `AssemblyLoadContext` for isolation.
