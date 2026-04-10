using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Cli.Plugins;

public class PluginManager
{
    private readonly string _pluginsDirectory;
    private readonly Version _hostCoreVersion;
    private readonly List<PluginInfo> _cachedPlugins = new();
    private readonly List<RecrdPluginLoadContext> _loadContexts = new();
    private bool _discovered;

    public PluginManager(string pluginsDirectory, Version? hostCoreVersion = null)
    {
        _pluginsDirectory = pluginsDirectory;
        _hostCoreVersion = hostCoreVersion
            ?? typeof(Session).Assembly.GetName().Version
            ?? new Version(1, 0, 0, 0);
    }

    public IReadOnlyList<PluginInfo> DiscoverPlugins()
    {
        if (_discovered)
        {
            return _cachedPlugins;
        }

        _discovered = true;
        _cachedPlugins.Clear();

        if (!Directory.Exists(_pluginsDirectory))
        {
            return _cachedPlugins;
        }

        var subdirectories = Directory.GetDirectories(_pluginsDirectory);
        foreach (var subdir in subdirectories)
        {
            var dirName = Path.GetFileName(subdir);
            var pluginDllPath = Path.Combine(subdir, dirName + ".dll");

            if (!File.Exists(pluginDllPath))
            {
                continue;
            }

            try
            {
                var referencedCoreVersion = GetReferencedCoreVersion(pluginDllPath);
                if (referencedCoreVersion != null && referencedCoreVersion.Major != _hostCoreVersion.Major)
                {
                    _cachedPlugins.Add(new PluginInfo(
                        Name: dirName,
                        Version: null, // Version could be read from metadata but task says Version from AssemblyName.Version after loading
                        Interfaces: Array.Empty<string>(),
                        Loaded: false,
                        Error: $"version mismatch (requires Core v{referencedCoreVersion.Major})"));
                    continue;
                }

                var alc = new RecrdPluginLoadContext(pluginDllPath);
                var assembly = alc.LoadFromAssemblyPath(pluginDllPath);
                _loadContexts.Add(alc);

                var interfaces = new List<string>();
                var types = assembly.GetExportedTypes();

                foreach (var type in types)
                {
                    if (typeof(ITestCompiler).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                        interfaces.Add(nameof(ITestCompiler));
                    if (typeof(IDataProvider).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                        interfaces.Add(nameof(IDataProvider));
                    if (typeof(IEventInterceptor).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                        interfaces.Add(nameof(IEventInterceptor));
                    if (typeof(IAssertionProvider).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                        interfaces.Add(nameof(IAssertionProvider));
                }

                _cachedPlugins.Add(new PluginInfo(
                    Name: dirName,
                    Version: assembly.GetName().Version,
                    Interfaces: interfaces.Distinct().ToArray(),
                    Loaded: true,
                    Error: null));
            }
            catch (Exception ex)
            {
                _cachedPlugins.Add(new PluginInfo(
                    Name: dirName,
                    Version: null,
                    Interfaces: Array.Empty<string>(),
                    Loaded: false,
                    Error: $"load error: {ex.Message}"));
            }
        }

        return _cachedPlugins;
    }

    public IReadOnlyList<ITestCompiler> GetCompilers()
    {
        var plugins = DiscoverPlugins();
        var compilers = new List<ITestCompiler>();

        foreach (var plugin in plugins.Where(p => p.Loaded))
        {
            var alc = _loadContexts.FirstOrDefault(c => Path.GetFileNameWithoutExtension(c.Name) == plugin.Name);
            if (alc == null) continue;

            var assembly = alc.Assemblies.FirstOrDefault(a => a.GetName().Name == plugin.Name);
            if (assembly == null) continue;

            foreach (var type in assembly.GetExportedTypes())
            {
                if (typeof(ITestCompiler).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                {
                    if (Activator.CreateInstance(type) is ITestCompiler compiler)
                    {
                        compilers.Add(compiler);
                    }
                }
            }
        }

        return compilers;
    }

    public IReadOnlyList<IDataProvider> GetDataProviders()
    {
        var plugins = DiscoverPlugins();
        var providers = new List<IDataProvider>();

        foreach (var plugin in plugins.Where(p => p.Loaded))
        {
            var alc = _loadContexts.FirstOrDefault(c => Path.GetFileNameWithoutExtension(c.Name) == plugin.Name);
            if (alc == null) continue;

            var assembly = alc.Assemblies.FirstOrDefault(a => a.GetName().Name == plugin.Name);
            if (assembly == null) continue;

            foreach (var type in assembly.GetExportedTypes())
            {
                if (typeof(IDataProvider).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                {
                    if (Activator.CreateInstance(type) is IDataProvider provider)
                    {
                        providers.Add(provider);
                    }
                }
            }
        }

        return providers;
    }

    public bool IsPluginCompiler(ITestCompiler compiler)
    {
        var assembly = compiler.GetType().Assembly;
        return _loadContexts.Any(alc => alc.Assemblies.Contains(assembly));
    }

    /// <summary>
    /// Invokes compiler.CompileAsync with exception isolation — catches all exceptions
    /// from plugin code and returns a CompilationResult with a warning instead of crashing.
    /// Implements PLUG-04 exception safety contract.
    /// </summary>
    public async Task<CompilationResult> SafeCompileAsync(ITestCompiler compiler, Session session, CompilerOptions options)
    {
        try
        {
            return await compiler.CompileAsync(session, options);
        }
        catch (Exception ex)
        {
            return new CompilationResult(
                generatedFiles: Array.Empty<string>(),
                warnings: new[] { $"[plugin:{compiler.TargetName}] {ex.Message}" },
                dependencyManifest: new Dictionary<string, string>());
        }
    }

    private static Version? GetReferencedCoreVersion(string dllPath)
    {
        using var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var peReader = new PEReader(fs);
        
        if (!peReader.HasMetadata)
        {
            return null;
        }

        var mdReader = peReader.GetMetadataReader();
        foreach (var handle in mdReader.AssemblyReferences)
        {
            var reference = mdReader.GetAssemblyReference(handle);
            var name = mdReader.GetString(reference.Name);
            if (name == "Recrd.Core")
            {
                return reference.Version;
            }
        }

        return null;
    }
}
