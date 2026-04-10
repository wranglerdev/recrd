using Recrd.Cli.Plugins;
using Xunit;

namespace Recrd.Cli.Tests.Plugins;

/// <summary>
/// Tests for PLUG-03: Major version gating for Recrd.Core compatibility.
/// The host must reject plugins built against an incompatible major version of Recrd.Core.
/// Discovery uses PEReader/MetadataReader to inspect AssemblyReferences without loading.
/// RED PHASE: DiscoverPlugins() throws NotImplementedException from stub.
/// </summary>
public class VersionGatingTests : IDisposable
{
    private readonly string _tempDir;

    public VersionGatingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "recrd-version-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void IsCompatible_SameMajorVersion_ReturnsTrue()
    {
        // Arrange: FakePlugin references the same Recrd.Core major version as the host.
        // PluginManager must accept it and return it as Loaded=true.
        var (dllPath, _) = PluginTestFixture.PublishFakePlugin(_tempDir);
        var pluginsDir = Path.Combine(_tempDir, "plugins");
        var pluginSubDir = Path.Combine(pluginsDir, "Recrd.Plugin.Test");
        Directory.CreateDirectory(pluginSubDir);
        File.Copy(dllPath, Path.Combine(pluginSubDir, "Recrd.Plugin.Test.dll"));

        var manager = new PluginManager(pluginsDir);

        // Act — RED PHASE: DiscoverPlugins() throws NotImplementedException
        var plugins = manager.DiscoverPlugins();

        // Assert: FakePlugin references same major version — must be accepted (Loaded=true)
        var plugin = Assert.Single(plugins);
        Assert.True(plugin.Loaded, "Plugin built against same major version should be loaded successfully");
        Assert.Null(plugin.Error);
    }

    [Fact]
    public void IsCompatible_DifferentMajorVersion_ReturnsFalse()
    {
        // Arrange: Simulate host running an incompatible major version (v99).
        // FakePlugin references Recrd.Core v1.x, so v99 host must reject it.
        var (dllPath, _) = PluginTestFixture.PublishFakePlugin(_tempDir);
        var pluginsDir = Path.Combine(_tempDir, "plugins");
        var pluginSubDir = Path.Combine(pluginsDir, "Recrd.Plugin.Test");
        Directory.CreateDirectory(pluginSubDir);
        File.Copy(dllPath, Path.Combine(pluginSubDir, "Recrd.Plugin.Test.dll"));

        var incompatibleHostVersion = new Version(99, 0, 0, 0);
        var manager = new PluginManager(pluginsDir, hostCoreVersion: incompatibleHostVersion);

        // Act — RED PHASE: DiscoverPlugins() throws NotImplementedException
        var plugins = manager.DiscoverPlugins();

        // Assert: FakePlugin references major=1, host claims major=99 — must be rejected (Loaded=false)
        var plugin = Assert.Single(plugins);
        Assert.False(plugin.Loaded, "Plugin with incompatible major version should not be loaded");
    }

    [Fact]
    public void DiscoverPlugins_IncompatiblePlugin_ReturnsPluginInfoWithError()
    {
        // Arrange: Plugin with mismatched major version — DiscoverPlugins must return
        // PluginInfo with Loaded=false and Error containing "version mismatch".
        var (dllPath, _) = PluginTestFixture.PublishFakePlugin(_tempDir);
        var pluginsDir = Path.Combine(_tempDir, "plugins");
        var pluginSubDir = Path.Combine(pluginsDir, "Recrd.Plugin.Test");
        Directory.CreateDirectory(pluginSubDir);
        File.Copy(dllPath, Path.Combine(pluginSubDir, "Recrd.Plugin.Test.dll"));

        var incompatibleHostVersion = new Version(99, 0, 0, 0);
        var manager = new PluginManager(pluginsDir, hostCoreVersion: incompatibleHostVersion);

        // Act — RED PHASE: DiscoverPlugins() throws NotImplementedException
        var plugins = manager.DiscoverPlugins();

        // Assert: PluginInfo.Loaded=false, PluginInfo.Error contains "version mismatch"
        var plugin = Assert.Single(plugins);
        Assert.False(plugin.Loaded);
        Assert.NotNull(plugin.Error);
        Assert.Contains("version mismatch", plugin.Error, StringComparison.OrdinalIgnoreCase);
    }
}
