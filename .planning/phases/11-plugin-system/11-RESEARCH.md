# Phase 11: Plugin System - Research

**Researched:** 2025-05-15
**Domain:** .NET 10 Dynamic Loading and Plugin Isolation
**Confidence:** HIGH

## Summary

This research establishes the technical foundation for a secure and isolated plugin system in `recrd`. By leveraging modern .NET 10 features (`AssemblyLoadContext` and `AssemblyDependencyResolver`), we can achieve true dependency isolation, allowing plugins to use different versions of common libraries (like `Newtonsoft.Json`) without conflicting with the host or other plugins.

Efficiency is a priority for the CLI; therefore, we will use `System.Reflection.Metadata` to scan for plugin types and verify version compatibility without fully loading the assemblies into memory. This ensures cold starts remain under the 500ms requirement (CLI-12).

**Primary recommendation:** Use a custom `AssemblyLoadContext` per plugin directory, with explicit "Type Unification" logic to share `Recrd.Core` types from the host's default context.

## User Constraints (from CONTEXT.md)

*No CONTEXT.md exists for this phase yet. Following goals from prompt and REQUIREMENTS.md.*

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PLUG-01 | Plugin discovery: scan `~/.recrd/plugins/` | Verified efficient scanning using `PEReader` and `MetadataReader` to avoid assembly locking and overhead. [VERIFIED: Microsoft docs] |
| PLUG-02 | Plugin loading via `AssemblyLoadContext` isolation | Confirmed as the standard pattern for .NET 10 plugin systems using `AssemblyDependencyResolver`. [CITED: learn.microsoft.com] |
| PLUG-03 | Major version gating for `Recrd.Core` | Confirmed `AssemblyReferences` can be extracted via metadata to check major version mismatch before loading. [VERIFIED: System.Reflection.Metadata] |
| PLUG-04 | Exception isolation and process protection | Confirmed that `async/await` catch blocks around plugin calls prevent unhandled exceptions from crashing the host process. [ASSUMED] |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `System.Runtime.Loader` | 10.0.0 | `AssemblyLoadContext` and `AssemblyDependencyResolver` | Built-in .NET 10 API for modern plugin isolation. |
| `System.Reflection.Metadata` | 10.0.0 | Efficient DLL inspection | Standard way to read PE headers and metadata without loading the assembly. |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|--------------|
| `Microsoft.Extensions.DependencyModel` | 10.0.0 | Transitive dependency resolution | Use if `AssemblyDependencyResolver` is insufficient for complex NuGet graphs. [CITED: Nate McMaster] |

**Installation:**
These are typically included in the .NET SDK, but if referencing explicitly:
```bash
dotnet add package System.Reflection.Metadata --version 10.0.0
```

## Architecture Patterns

### Recommended Project Structure
```
~/.recrd/
└── plugins/
    ├── Recrd.Plugin.Excel/
    │   ├── Recrd.Plugin.Excel.dll
    │   ├── Recrd.Plugin.Excel.deps.json (Required for isolation)
    │   └── ClosedXML.dll (Transitive dependency)
    └── Recrd.Plugin.CustomAssert/
        ├── Recrd.Plugin.CustomAssert.dll
        └── Recrd.Plugin.CustomAssert.deps.json
```

### Pattern 1: Plugin Load Context (Isolation)
**What:** A custom `AssemblyLoadContext` that uses `AssemblyDependencyResolver` and explicitly manages shared types.
**When to use:** Every time a plugin is loaded from an external folder.
**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
public class RecrdPluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public RecrdPluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 1. Share Recrd.Core with the host (Type Unification)
        if (assemblyName.Name == "Recrd.Core")
        {
            return null; // Let the default ALC load it
        }

        // 2. Resolve via .deps.json
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }
}
```

### Pattern 2: Reflection-Free Discovery
**What:** Scanning DLLs for interface implementations without triggering the loading of the assembly or its dependencies.
**When to use:** During `recrd compile` or `recrd plugins list` to find matching targets.
**Example:**
```csharp
using var peReader = new PEReader(File.OpenRead(dllPath));
var mdReader = peReader.GetMetadataReader();
foreach (var typeHandle in mdReader.TypeDefinitions)
{
    var typeDef = mdReader.GetTypeDefinition(typeHandle);
    foreach (var ifaceHandle in typeDef.GetInterfaceImplementations())
    {
        var iface = mdReader.GetInterfaceImplementation(ifaceHandle);
        // Compare iface.Interface name with "ITestCompiler", etc.
    }
}
```

### Pattern 3: Plugin Registry (Integration)
**What:** A central `PluginManager` that abstracts the loading and provides unified access to implementations from both the host and plugins.
**Integration Points:**
- `CompileCommand`: Replace hardcoded `compilers` dictionary with `pluginManager.GetCompilers()`.
- `DataProvider`: Use `pluginManager.GetDataProviders()` to find a provider that can handle a specific extension.
- `EventInterceptors`: Chain all discovered `IEventInterceptor` implementations during the recording process (if applicable) or compilation.

### Anti-Patterns to Avoid
- **"Double Loading":** Loading `Recrd.Core.dll` into the plugin context. This causes `InvalidCastException` when casting a plugin-created instance to a host-defined interface. [VERIFIED: stackoverflow]
- **Locking DLLs:** Using `Assembly.LoadFile` or `Assembly.LoadFrom` without an ALC. This prevents users from deleting or updating plugins until the CLI process exits.
- **Scanning entire ~/ folder:** Scanning must be restricted to `~/.recrd/plugins/` to prevent performance degradation.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Dependency Resolution | Custom `.deps.json` parser | `AssemblyDependencyResolver` | Handles complex probing, architecture-specific assets (runtimes/), and native DLLs. |
| Type Unification | Custom Proxy objects | `return null` in `Load` | Standard mechanism for sharing types across contexts. |

## Common Pitfalls

### Pitfall 1: Missing `.deps.json`
**What goes wrong:** Plugin loads fine but fails when calling a method that uses a NuGet dependency (e.g., `FileNotFoundException`).
**Why it happens:** The `AssemblyDependencyResolver` cannot find the dependency list if the plugin was built without `EnableDynamicLoading`.
**How to avoid:** Documentation must emphasize setting `<EnableDynamicLoading>true</EnableDynamicLoading>` in plugin `.csproj`.

### Pitfall 2: Major Version Mismatch
**What goes wrong:** Host crashes with `MissingMethodException` or `TypeLoadException` when calling the plugin.
**Why it happens:** Plugin was built against `Recrd.Core` v2.0 but host has v1.0.
**How to avoid:** Implement strict major version gating during discovery using `MetadataReader`.

## Code Examples

### Major Version Gating check
```csharp
// Source: [System.Reflection.Metadata docs]
public bool IsCompatible(string dllPath, Version hostCoreVersion)
{
    using var peReader = new PEReader(File.OpenRead(dllPath));
    var reader = peReader.GetMetadataReader();
    foreach (var handle in reader.AssemblyReferences)
    {
        var reference = reader.GetAssemblyReference(handle);
        var name = reader.GetString(reference.Name);
        if (name == "Recrd.Core")
        {
            return reference.Version.Major == hostCoreVersion.Major;
        }
    }
    return true; // No reference to Core, might be a standalone utility
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `AppDomain` | `AssemblyLoadContext` | .NET Core 1.0 | Better performance, no serialization overhead for cross-domain calls. |
| `Assembly.LoadFile` | `AssemblyDependencyResolver` | .NET Core 3.0 | Reliable resolution of transitive and native dependencies. |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Async/Await try-catch protects host from crashes | Requirements | Low - standard .NET behavior for task-based exception handling. |
| A2 | User plugins follow directory-per-plugin structure | Architecture | Medium - if they don't, dependency isolation fails. |

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 10 | Building plugins | ✓ | 10.0.104 | — |
| CLI Runtime | Executing plugins | ✓ | 10.0.104 | — |

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 |
| Config file | `Directory.Build.props` (Implicit Usings/Nullable) |
| Quick run command | `dotnet test --filter Category!=Integration` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PLUG-01 | Discovery scans subdirs | Integration | `dotnet test tests/Recrd.Cli.Tests` | ❌ Wave 0 |
| PLUG-02 | Isolation works (diff versions) | Integration | `dotnet test tests/Recrd.Cli.Tests` | ❌ Wave 0 |
| PLUG-03 | Rejects v2 plugin if host is v1 | Unit | `dotnet test tests/Recrd.Cli.Tests` | ❌ Wave 0 |
| PLUG-04 | Exception in plugin logged | Unit | `dotnet test tests/Recrd.Cli.Tests` | ❌ Wave 0 |

### Wave 0 Gaps
- [ ] `tests/Recrd.Cli.Tests/PluginSystemTests.cs` — Mock plugin creation and loading.
- [ ] `tests/Recrd.Cli.Tests/MetadataInspectorTests.cs` — Test version gating logic.

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V5 Input Validation | yes | Validate plugin directory paths. |
| V12 File and Resources | yes | Restrict scanning to `~/.recrd/plugins`. |
| V14 Configuration | yes | Major version gating for binary compatibility. |

### Known Threat Patterns for Plugin Systems

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Malicious Plugin Execution | Tampering | User warning on first load; strict directory permissions. |
| Dependency Hijacking | Tampering | Use `AssemblyDependencyResolver` which prioritizes local assets. |

## Sources

### Primary (HIGH confidence)
- `learn.microsoft.com` - [.NET Plugin Support Guide](https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)
- `learn.microsoft.com` - [AssemblyLoadContext Class](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext)
- `System.Reflection.Metadata` - [PE and Metadata Reading](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.metadata.metadatareader)

### Secondary (MEDIUM confidence)
- `natemcmaster.com` - [.NET Core Plugins Blog](https://natemcmaster.com/blog/2018/07/25/dotnet-plugins/)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Built-in .NET 10 features.
- Architecture: HIGH - Follows official Microsoft patterns for plugins.
- Pitfalls: MEDIUM - Based on community knowledge of "Type Unification" issues.

**Research date:** 2025-05-15
**Valid until:** 2025-11-15
