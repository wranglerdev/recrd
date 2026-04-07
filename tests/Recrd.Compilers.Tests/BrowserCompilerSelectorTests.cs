using Recrd.Compilers;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Xunit;

namespace Recrd.Compilers.Tests;

public class BrowserCompilerSelectorTests
{
    private static Session MakeSession(params IStep[] steps) =>
        new(SchemaVersion: 1,
            Metadata: new SessionMetadata("test-id", DateTimeOffset.UtcNow, "chromium",
                new ViewportSize(1280, 720), "http://localhost"),
            Variables: [],
            Steps: steps);

    private static Selector MakeSelector(SelectorStrategy strategy, string value) =>
        new([strategy], new Dictionary<SelectorStrategy, string> { [strategy] = value });

    private static Selector MakeMultiSelector(params (SelectorStrategy strategy, string value)[] entries)
    {
        var strategies = entries.Select(e => e.strategy).ToList();
        var values = entries.ToDictionary(e => e.strategy, e => e.value);
        return new Selector(strategies, values);
    }

    [Fact]
    public async Task PreferredDataTestId_EmitsCssAttributeSelector()
    {
        var compiler = new RobotBrowserCompiler();
        var selector = MakeSelector(SelectorStrategy.DataTestId, "submit-btn");
        var step = new ActionStep(ActionType.Click, selector, new Dictionary<string, string>());
        var session = MakeSession(step);
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains(@"css=[data-testid=""submit-btn""]", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task FallbackToId_WhenDataTestIdMissing()
    {
        var compiler = new RobotBrowserCompiler();
        var selector = MakeSelector(SelectorStrategy.Id, "submit");
        var step = new ActionStep(ActionType.Click, selector, new Dictionary<string, string>());
        var session = MakeSession(step);
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains("id=submit", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task FallbackChain_ExhaustedEmitsWarning()
    {
        var compiler = new RobotBrowserCompiler();
        var selector = new Selector([], new Dictionary<SelectorStrategy, string>());
        var step = new ActionStep(ActionType.Click, selector, new Dictionary<string, string>());
        var session = MakeSession(step);
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            Assert.NotEmpty(result.Warnings);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Theory]
    [InlineData(SelectorStrategy.Css, "css=.my-class")]
    [InlineData(SelectorStrategy.XPath, "xpath=//div")]
    public async Task SelectorStrategy_EmitsCorrectFormat(SelectorStrategy strategy, string expectedFragment)
    {
        var compiler = new RobotBrowserCompiler();
        var rawValue = expectedFragment.Contains("=") ? expectedFragment.Split('=', 2)[1] : expectedFragment;
        var selector = MakeSelector(strategy, rawValue);
        var step = new ActionStep(ActionType.Click, selector, new Dictionary<string, string>());
        var session = MakeSession(step);
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains(expectedFragment, content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }
}
