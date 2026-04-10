using Recrd.Cli.Plugins;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Xunit;

namespace Recrd.Cli.Tests.Plugins;

/// <summary>
/// Tests for PLUG-04: Exception safety — plugin exceptions must not crash the host process.
/// All calls to plugin CompileAsync must be wrapped in try/catch(Exception) so a misbehaving
/// plugin cannot bring down the CLI. Warnings are added to CompilationResult.Warnings.
/// RED PHASE: GetCompilers() and SafeCompileAsync() throw NotImplementedException from stub.
/// </summary>
public class ExceptionSafetyTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Session _testSession;
    private readonly CompilerOptions _testOptions;

    public ExceptionSafetyTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "recrd-exception-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _testSession = new Session(
            SchemaVersion: 1,
            Metadata: new SessionMetadata(
                Id: "test-session",
                CreatedAt: DateTimeOffset.UtcNow,
                BrowserEngine: "chromium",
                ViewportSize: new ViewportSize(1280, 720)),
            Variables: Array.Empty<Variable>(),
            Steps: Array.Empty<IStep>());

        _testOptions = new CompilerOptions { OutputDirectory = _tempDir };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CompileWithPluginException_CatchesAndAddsWarning()
    {
        // Arrange: PluginManager loaded with FakeThrowingCompiler (via published FakePlugin).
        // When FakeThrowingCompiler.CompileAsync throws InvalidOperationException("Plugin kaboom"),
        // PluginManager.SafeCompileAsync must catch it and add a warning "[plugin:fake-throwing] ...".
        var (dllPath, publishDir) = PluginTestFixture.PublishFakePlugin(_tempDir);
        var pluginsDir = PluginTestFixture.CreatePluginDirectory(_tempDir, "Recrd.Plugin.Test", dllPath);
        var manager = new PluginManager(pluginsDir);

        // Act — RED PHASE: GetCompilers() throws NotImplementedException
        var compilers = manager.GetCompilers();
        var throwingCompiler = compilers.Single(c => c.TargetName == "fake-throwing");
        var result = await manager.SafeCompileAsync(throwingCompiler, _testSession, _testOptions);

        // Assert: warning added with plugin name prefix; host did not crash
        Assert.Contains(result.Warnings, w => w.StartsWith("[plugin:fake-throwing]"));
    }

    [Fact]
    public async Task CompileWithPluginException_HostDoesNotCrash()
    {
        // Arrange: FakeThrowingCompiler throws during CompileAsync.
        // SafeCompileAsync must complete without propagating the exception.
        var (dllPath, publishDir) = PluginTestFixture.PublishFakePlugin(_tempDir);
        var pluginsDir = PluginTestFixture.CreatePluginDirectory(_tempDir, "Recrd.Plugin.Test", dllPath);
        var manager = new PluginManager(pluginsDir);

        // Act — RED PHASE: GetCompilers() throws NotImplementedException
        var compilers = manager.GetCompilers();
        var throwingCompiler = compilers.Single(c => c.TargetName == "fake-throwing");

        // Must complete without throwing (exception is caught internally by SafeCompileAsync)
        var result = await manager.SafeCompileAsync(throwingCompiler, _testSession, _testOptions);

        // Assert: returned a result (not null, not an exception)
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CompileWithPluginException_OtherCompilersStillRun()
    {
        // Arrange: Two compilers — FakeCompiler (succeeds) and FakeThrowingCompiler (throws).
        // The successful compiler must still produce output even when the other fails.
        var (dllPath, publishDir) = PluginTestFixture.PublishFakePlugin(_tempDir);
        var pluginsDir = PluginTestFixture.CreatePluginDirectory(_tempDir, "Recrd.Plugin.Test", dllPath);
        var manager = new PluginManager(pluginsDir);

        // Act — RED PHASE: GetCompilers() throws NotImplementedException
        var compilers = manager.GetCompilers();
        Assert.Equal(2, compilers.Count);

        var results = new List<CompilationResult>();
        foreach (var compiler in compilers)
        {
            var result = await manager.SafeCompileAsync(compiler, _testSession, _testOptions);
            results.Add(result);
        }

        // Assert: fake-compiler succeeded with generated files; fake-throwing has warning
        var successResult = results.First(r => r.GeneratedFiles.Count > 0);
        Assert.Single(successResult.GeneratedFiles);
        Assert.DoesNotContain(successResult.Warnings, w => w.StartsWith("[plugin:"));

        var failResult = results.First(r => r.Warnings.Any(w => w.StartsWith("[plugin:fake-throwing]")));
        Assert.Contains(failResult.Warnings, w => w.StartsWith("[plugin:fake-throwing]"));
    }
}
