using Recrd.Compilers;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Xunit;

namespace Recrd.Compilers.Tests;

public class SeleniumCompilerSelectorTests
{
    private static Session MakeSession(params IStep[] steps) =>
        new(SchemaVersion: 1,
            Metadata: new SessionMetadata("test-id", DateTimeOffset.UtcNow, "chromium",
                new ViewportSize(1280, 720), "http://localhost"),
            Variables: [],
            Steps: steps);

    private static Selector MakeSelector(SelectorStrategy strategy, string value) =>
        new([strategy], new Dictionary<SelectorStrategy, string> { [strategy] = value });

    [Fact]
    public async Task PreferredId_EmitsIdLocator()
    {
        var compiler = new RobotSeleniumCompiler();
        var selector = MakeSelector(SelectorStrategy.Id, "submit");
        var step = new ActionStep(ActionType.Click, selector, new Dictionary<string, string>());
        var session = MakeSession(step);
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session,
                new CompilerOptions { OutputDirectory = outDir, PreferredSelectorStrategy = SelectorStrategy.Id });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains("id:submit", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task FallbackToCss_WhenIdMissing()
    {
        var compiler = new RobotSeleniumCompiler();
        var selector = MakeSelector(SelectorStrategy.Css, ".btn");
        var step = new ActionStep(ActionType.Click, selector, new Dictionary<string, string>());
        var session = MakeSession(step);
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains("css:.btn", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task FallbackToXpath_WhenIdAndCssMissing()
    {
        var compiler = new RobotSeleniumCompiler();
        var selector = MakeSelector(SelectorStrategy.XPath, "//button[@type='submit']");
        var step = new ActionStep(ActionType.Click, selector, new Dictionary<string, string>());
        var session = MakeSession(step);
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains("xpath:", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task DataTestId_EmitsCssAttributeLocator()
    {
        var compiler = new RobotSeleniumCompiler();
        var selector = MakeSelector(SelectorStrategy.DataTestId, "x");
        var step = new ActionStep(ActionType.Click, selector, new Dictionary<string, string>());
        var session = MakeSession(step);
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains(@"css:[data-testid=""x""]", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }
}
