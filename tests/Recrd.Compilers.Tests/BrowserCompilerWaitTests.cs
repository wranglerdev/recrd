using Recrd.Compilers;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Xunit;

namespace Recrd.Compilers.Tests;

public class BrowserCompilerWaitTests
{
    private static Session MakeSession(params IStep[] steps) =>
        new(SchemaVersion: 1,
            Metadata: new SessionMetadata("test-id", DateTimeOffset.UtcNow, "chromium",
                new ViewportSize(1280, 720), "http://localhost"),
            Variables: [],
            Steps: steps);

    private static Selector MakeSelector(SelectorStrategy strategy, string value) =>
        new([strategy], new Dictionary<SelectorStrategy, string> { [strategy] = value });

    private static ActionStep MakeStep(ActionType actionType, string selectorValue = "target") =>
        new(actionType, MakeSelector(SelectorStrategy.DataTestId, selectorValue), new Dictionary<string, string>());

    [Theory]
    [InlineData(ActionType.Click)]
    [InlineData(ActionType.Type)]
    [InlineData(ActionType.Select)]
    [InlineData(ActionType.Upload)]
    public async Task InteractiveStep_EmitsWaitBeforeKeyword(ActionType actionType)
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeStep(actionType));
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains("Wait For Elements State", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task Navigate_DoesNotEmitWait()
    {
        var compiler = new RobotBrowserCompiler();
        var navigateStep = new ActionStep(
            ActionType.Navigate,
            new Selector([], new Dictionary<SelectorStrategy, string>()),
            new Dictionary<string, string> { ["url"] = "http://example.com" });
        var session = MakeSession(navigateStep);
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.DoesNotContain("Wait For Elements State", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task WaitTimeout_UsesCompilerOptionsValue()
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeStep(ActionType.Click));
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session,
                new CompilerOptions { OutputDirectory = outDir, TimeoutSeconds = 45 });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains("timeout=45s", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }
}
