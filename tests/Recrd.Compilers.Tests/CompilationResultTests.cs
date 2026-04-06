using Recrd.Compilers;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Xunit;

namespace Recrd.Compilers.Tests;

public class CompilationResultTests
{
    private static Session MakeSession(params IStep[] steps) =>
        new(SchemaVersion: 1,
            Metadata: new SessionMetadata("test-id", DateTimeOffset.UtcNow, "chromium",
                new ViewportSize(1280, 720), "http://localhost"),
            Variables: [],
            Steps: steps);

    private static Selector MakeSelector(SelectorStrategy strategy, string value) =>
        new([strategy], new Dictionary<SelectorStrategy, string> { [strategy] = value });

    private static ActionStep MakeClickStep() =>
        new(ActionType.Click,
            MakeSelector(SelectorStrategy.DataTestId, "submit-btn"),
            new Dictionary<string, string>());

    [Fact]
    public async Task BrowserCompiler_ResultHasGeneratedFiles()
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            Assert.Equal(2, result.GeneratedFiles.Count);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task BrowserCompiler_ResultHasDependencyManifest()
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            Assert.True(result.DependencyManifest.ContainsKey("robotframework"),
                "DependencyManifest should contain key 'robotframework'");
            Assert.True(result.DependencyManifest.ContainsKey("robotframework-browser"),
                "DependencyManifest should contain key 'robotframework-browser'");
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task SeleniumCompiler_ResultHasDependencyManifest()
    {
        var compiler = new RobotSeleniumCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            Assert.True(result.DependencyManifest.ContainsKey("robotframework"),
                "DependencyManifest should contain key 'robotframework'");
            Assert.True(result.DependencyManifest.ContainsKey("robotframework-seleniumlibrary"),
                "DependencyManifest should contain key 'robotframework-seleniumlibrary'");
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task BrowserCompiler_ResultWarningsIsNotNull()
    {
        var compiler = new RobotBrowserCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            Assert.NotNull(result.Warnings);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }
}
