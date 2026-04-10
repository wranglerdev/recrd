using System.Reflection;
using System.Runtime.Loader;

namespace Recrd.Cli.Plugins;

internal sealed class RecrdPluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public RecrdPluginLoadContext(string pluginPath) 
        : base(name: Path.GetFileNameWithoutExtension(pluginPath), isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // D-07: Type unification. Host's Recrd.Core is used by returning null.
        if (assemblyName.Name == "Recrd.Core")
        {
            return null;
        }

        // Resolve dependencies via .deps.json in the plugin directory
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
