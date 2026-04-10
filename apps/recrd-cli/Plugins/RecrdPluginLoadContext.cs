using System.Reflection;
using System.Runtime.Loader;

namespace Recrd.Cli.Plugins;

public class RecrdPluginLoadContext : AssemblyLoadContext
{
    private readonly string _pluginPath;

    public RecrdPluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _pluginPath = pluginPath;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        throw new NotImplementedException();
    }
}
