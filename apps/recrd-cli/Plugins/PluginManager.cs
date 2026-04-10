using Recrd.Core.Ast;
using Recrd.Core.Interfaces;

namespace Recrd.Cli.Plugins;

public class PluginManager
{
    private readonly string _pluginsDirectory;
    private readonly Version _hostCoreVersion;

    public PluginManager(string pluginsDirectory, Version? hostCoreVersion = null)
    {
        _pluginsDirectory = pluginsDirectory;
        _hostCoreVersion = hostCoreVersion
            ?? typeof(Recrd.Core.Ast.Session).Assembly.GetName().Version
            ?? new Version(1, 0, 0, 0);
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

    /// <summary>
    /// Invokes compiler.CompileAsync with exception isolation — catches all exceptions
    /// from plugin code and returns a CompilationResult with a warning instead of crashing.
    /// Implements PLUG-04 exception safety contract.
    /// </summary>
    public Task<CompilationResult> SafeCompileAsync(ITestCompiler compiler, Session session, CompilerOptions options)
    {
        throw new NotImplementedException();
    }
}
