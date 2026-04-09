# recrd

A .NET 10 CLI tool that records browser interactions and compiles them into pt-BR Gherkin (`.feature`) files and executable Robot Framework test suites.

## Features

- **Interactive Recording Engine**: Record clicks, inputs, and navigations directly from Chromium.
- **pt-BR Gherkin Generation**: Automatically emit BDD scenarios in Portuguese.
- **Robot Framework Compilers**: Target both `robot-browser` (Playwright) and `robot-selenium`.
- **Data-Driven Testing**: Merges external CSV/JSON data into your test scenarios.

## Installation

### Homebrew (macOS / Linux)
```bash
brew tap recrd/recrd
brew install recrd
```

### WinGet (Windows)
```powershell
winget install recrd
```

### Direct Download
Download the latest binaries for your platform from [GitHub Releases](https://github.com/recrd/recrd/releases).

For more details, see [docs/INSTALL.md](docs/INSTALL.md).

## Quick Start

1. Start a recording: `recrd start --base-url https://example.com`
2. Interact with the browser.
3. Stop and save the session: `recrd stop`
4. Compile into a Robot Framework suite: `recrd compile session.recrd --target robot-browser`

## Documentation

- [Full PRD](PRD.md)
- [Installation Guide](docs/INSTALL.md)
- [Architecture & GEMINI Context](GEMINI.md)

## License

MIT
