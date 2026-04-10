using Recrd.Core.Interfaces;

namespace Recrd.Cli.Plugins;

public class PluginManager
{
    private readonly string _pluginsDirectory;

    public PluginManager(string pluginsDirectory)
    {
        _pluginsDirectory = pluginsDirectory;
    }

    public IReadOnlyList<PluginInfo> DiscoverPlugins()
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<ITestCompiler> GetCompilers()
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<IDataProvider> GetDataProviders()
    {
        throw new NotImplementedException();
    }
}
