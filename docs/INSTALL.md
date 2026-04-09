# Installation Guide

`recrd` is a self-contained CLI tool. You don't need the .NET SDK installed to run the distributed binaries.

## Homebrew (macOS / Linux)

If you have Homebrew installed, you can install `recrd` via our tap:

```bash
brew tap recrd/recrd
brew install recrd
```

## WinGet (Windows)

On Windows 10 and 11, you can use `winget`:

```powershell
winget install recrd
```

## Direct Download

You can download the latest binaries directly from our [GitHub Releases](https://github.com/recrd/recrd/releases) page.

1. Download the archive for your platform (`win-x64.zip`, `osx-x64.tar.gz`, `linux-x64.tar.gz`).
2. Extract the contents to a directory of your choice.
3. Add that directory to your system's `PATH`.

### Important Note on `.playwright` directory

The `recrd` binary requires the `.playwright` directory to be located in the same folder as the executable. This directory contains the necessary browser drivers. If you move the `recrd` binary, make sure to move the `.playwright` folder along with it.

## Manual Installation from Source

If you have the .NET 10 SDK installed, you can build and install from source:

```bash
git clone https://github.com/recrd/recrd.git
cd recrd
dotnet publish apps/recrd-cli/recrd-cli.csproj -c Release -r <your-rid> --self-contained -o ./install
```

Then add the `./install` directory to your `PATH`.
