using Recrd.Cli.Plugins;
using Xunit;

namespace Recrd.Cli.Tests.Plugins;

/// <summary>
/// Tests for PLUG-01: Plugin discovery scans ~/.recrd/plugins/ subdirectories.
/// Plugins must be in their own subdirectory named Recrd.Plugin.* containing
/// a matching DLL. Flat DLLs in the plugins root are ignored.
/// RED PHASE: All tests fail with NotImplementedException from PluginManager stub.
/// </summary>
public class PluginDiscoveryTests : IDisposable
{
    private readonly string _tempDir;

    public PluginDiscoveryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "recrd-plugin-tests-" + Guid.NewGuid().ToString("N"));
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
    public void DiscoverPlugins_EmptyDirectory_ReturnsEmptyList()
    {
        // Arrange
        var pluginsDir = Path.Combine(_tempDir, "plugins");
        Directory.CreateDirectory(pluginsDir);
        var manager = new PluginManager(pluginsDir);

        // Act — RED PHASE: throws NotImplementedException
        var result = manager.DiscoverPlugins();

        // Assert expected final behavior
        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverPlugins_SubdirectoryWithMatchingDll_ReturnsPluginInfo()
    {
        // Arrange
        var pluginsDir = Path.Combine(_tempDir, "plugins");
        var pluginSubDir = Path.Combine(pluginsDir, "Recrd.Plugin.Test");
        Directory.CreateDirectory(pluginSubDir);

        // Use FakePlugin publish output for a real DLL with .deps.json
        var (dllPath, _) = PluginTestFixture.PublishFakePlugin(_tempDir);
        var pluginDll = Path.Combine(pluginSubDir, "Recrd.Plugin.Test.dll");
        File.Copy(dllPath, pluginDll);

        var manager = new PluginManager(pluginsDir);

        // Act — RED PHASE: throws NotImplementedException
        var result = manager.DiscoverPlugins();

        // Assert expected final behavior: one PluginInfo for Recrd.Plugin.Test
        var plugin = Assert.Single(result);
        Assert.Equal("Recrd.Plugin.Test", plugin.Name);
        Assert.True(plugin.Loaded);
        Assert.Null(plugin.Error);
    }

    [Fact]
    public void DiscoverPlugins_SubdirectoryWithNonMatchingDll_IgnoresIt()
    {
        // Arrange: directory named "Foo" with "Bar.dll" — does not match Recrd.Plugin.* pattern
        var pluginsDir = Path.Combine(_tempDir, "plugins");
        var pluginSubDir = Path.Combine(pluginsDir, "Foo");
        Directory.CreateDirectory(pluginSubDir);
        File.WriteAllBytes(Path.Combine(pluginSubDir, "Bar.dll"), new byte[] { 0x4D, 0x5A });
        var manager = new PluginManager(pluginsDir);

        // Act — RED PHASE: throws NotImplementedException
        var result = manager.DiscoverPlugins();

        // Assert: directory "Foo" doesn't match Recrd.Plugin.* — result must be empty
        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverPlugins_FlatDllInRoot_IgnoresIt()
    {
        // Arrange: DLL placed directly in plugins/ root (old layout) — must be ignored per D-01
        var pluginsDir = Path.Combine(_tempDir, "plugins");
        Directory.CreateDirectory(pluginsDir);
        File.WriteAllBytes(Path.Combine(pluginsDir, "Recrd.Plugin.Flat.dll"), new byte[] { 0x4D, 0x5A });
        var manager = new PluginManager(pluginsDir);

        // Act — RED PHASE: throws NotImplementedException
        var result = manager.DiscoverPlugins();

        // Assert: flat DLLs in root are ignored — only subdirectory layout is valid
        Assert.Empty(result);
    }
}

/// <summary>
/// Shared test fixture helper for plugin tests.
/// Provides utilities for building and publishing the FakePlugin test fixture.
/// </summary>
internal static class PluginTestFixture
{
    /// <summary>
    /// Resolves the FakePlugin.csproj path relative to the test assembly location.
    /// Goes up from bin/Debug/net10.0/ to the repo root, then to TestFixtures.
    /// </summary>
    private static string FindFakePluginCsproj()
    {
        // AppContext.BaseDirectory = {worktree}/tests/Recrd.Cli.Tests/bin/Debug/net10.0/
        // Go up 5 levels to reach the worktree root: net10.0 -> Debug -> bin -> Recrd.Cli.Tests -> tests -> root
        var baseDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "tests", "Recrd.Cli.Tests", "Plugins", "TestFixtures", "FakePlugin.csproj");
    }

    /// <summary>
    /// Publishes FakePlugin.csproj and returns the DLL path and publish directory.
    /// Uses dotnet publish to produce a real DLL with .deps.json.
    /// </summary>
    public static (string DllPath, string PublishDir) PublishFakePlugin(string tempDir)
    {
        var publishDir = Path.Combine(tempDir, "fakeplugin-publish");
        Directory.CreateDirectory(publishDir);

        var projectPath = FindFakePluginCsproj();

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectPath}\" -o \"{publishDir}\" --no-restore",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.Environment["DOTNET_SYSTEM_NET_DISABLEIPV6"] = "1";

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet process");
        process.WaitForExit(60_000);

        var dllPath = Path.Combine(publishDir, "FakePlugin.dll");
        if (!File.Exists(dllPath))
        {
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"FakePlugin publish failed (exit {process.ExitCode}). Project: {projectPath}\nStdout: {stdout}\nStderr: {stderr}");
        }

        return (dllPath, publishDir);
    }

    /// <summary>
    /// Creates a temp plugins directory with a valid plugin subdirectory structure.
    /// </summary>
    public static string CreatePluginDirectory(string tempDir, string pluginName, string dllPath)
    {
        var pluginsDir = Path.Combine(tempDir, "plugins");
        var pluginSubDir = Path.Combine(pluginsDir, pluginName);
        Directory.CreateDirectory(pluginSubDir);

        var publishDir = Path.GetDirectoryName(dllPath)!;
        foreach (var file in Directory.GetFiles(publishDir))
        {
            File.Copy(file, Path.Combine(pluginSubDir, Path.GetFileName(file)));
        }

        return pluginsDir;
    }
}
