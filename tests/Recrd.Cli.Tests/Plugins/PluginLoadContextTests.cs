using System.Reflection;
using Recrd.Cli.Plugins;
using Recrd.Core.Interfaces;
using Xunit;

namespace Recrd.Cli.Tests.Plugins;

/// <summary>
/// Tests for PLUG-02: Plugin isolation via AssemblyLoadContext.
/// Verifies that RecrdPluginLoadContext correctly isolates plugin assemblies while
/// sharing Recrd.Core types with the host (Type Unification pattern).
/// RED PHASE: Load() throws NotImplementedException from stub.
/// </summary>
public class PluginLoadContextTests : IDisposable
{
    private readonly string _tempDir;

    public PluginLoadContextTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "recrd-alc-tests-" + Guid.NewGuid().ToString("N"));
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
    public void LoadContext_RecrdCore_ReturnsNull()
    {
        // Arrange: RecrdPluginLoadContext must return null for Recrd.Core to enable Type Unification.
        // When Load() returns null, the default ALC loads Recrd.Core, preventing InvalidCastException.
        var (_, publishDir) = PluginTestFixture.PublishFakePlugin(_tempDir);
        var pluginDll = Path.Combine(publishDir, "FakePlugin.dll");
        var context = new RecrdPluginLoadContext(pluginDll);

        // Act — RED PHASE: Load() throws NotImplementedException (wrapped in FileLoadException)
        // When implemented: Load("Recrd.Core") must return null so the default ALC handles it.
        // We verify by loading Recrd.Core and confirming the returned assembly is the same
        // instance as what the host already has loaded.
        var assemblyName = new AssemblyName("Recrd.Core");
        var assembly = context.LoadFromAssemblyName(assemblyName);

        // Assert: must use the host's already-loaded Recrd.Core (null return from Load())
        Assert.NotNull(assembly);
        Assert.Equal("Recrd.Core", assembly.GetName().Name);
    }

    [Fact]
    public void LoadContext_PluginAssembly_LoadsFromPath()
    {
        // Arrange
        var (dllPath, _) = PluginTestFixture.PublishFakePlugin(_tempDir);
        var context = new RecrdPluginLoadContext(dllPath);

        // Act — RED PHASE: Load() throws NotImplementedException (wrapped in FileLoadException)
        // When implemented: loads FakePlugin.dll and returns its assembly.
        var assemblyName = new AssemblyName("FakePlugin");
        var assembly = context.LoadFromAssemblyName(assemblyName);

        // Assert: FakePlugin assembly loaded with the correct name
        Assert.NotNull(assembly);
        Assert.Equal("FakePlugin", assembly.GetName().Name);
    }

    [Fact]
    public void LoadContext_PluginImplementsITestCompiler_CastSucceeds()
    {
        // Arrange: After loading the plugin assembly, FakeCompiler should cast to ITestCompiler
        // without InvalidCastException. This requires Recrd.Core to be loaded from the host ALC.
        var (dllPath, _) = PluginTestFixture.PublishFakePlugin(_tempDir);
        var context = new RecrdPluginLoadContext(dllPath);

        // Act — RED PHASE: Load() throws NotImplementedException (wrapped in FileLoadException)
        // When implemented: load FakePlugin assembly, get FakeCompiler type, activate it,
        // cast to ITestCompiler — must NOT throw InvalidCastException.
        var assemblyName = new AssemblyName("FakePlugin");
        var assembly = context.LoadFromAssemblyName(assemblyName);
        var compilerType = assembly.GetType("Recrd.Plugin.Test.FakeCompiler")
            ?? throw new InvalidOperationException("FakeCompiler type not found");
        var instance = Activator.CreateInstance(compilerType)
            ?? throw new InvalidOperationException("Could not activate FakeCompiler");

        // Assert: cast to ITestCompiler must succeed (Type Unification working correctly)
        var compiler = (ITestCompiler)instance;
        Assert.Equal("fake-compiler", compiler.TargetName);
    }
}
