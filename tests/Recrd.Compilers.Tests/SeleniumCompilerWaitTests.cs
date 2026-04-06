using Recrd.Compilers;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Xunit;

namespace Recrd.Compilers.Tests;

public class SeleniumCompilerWaitTests
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
    public async Task ImplicitWait_EmittedInSuiteSetupKeyword()
    {
        var compiler = new RobotSeleniumCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.Contains("Set Selenium Implicit Wait    ${TIMEOUT}s", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task TimeoutValue_ComesFromCompilerOptions()
    {
        var compiler = new RobotSeleniumCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session,
                new CompilerOptions { OutputDirectory = outDir, TimeoutSeconds = 45 });
            var robotPath = result.GeneratedFiles.Single(f => f.EndsWith(".robot"));
            var content = await File.ReadAllTextAsync(robotPath);
            Assert.Contains("${TIMEOUT}    45", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task NoPerStepExplicitWait()
    {
        var compiler = new RobotSeleniumCompiler();
        var session = MakeSession(MakeClickStep());
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await compiler.CompileAsync(session, new CompilerOptions { OutputDirectory = outDir });
            var resourcePath = result.GeneratedFiles.Single(f => f.EndsWith(".resource"));
            var content = await File.ReadAllTextAsync(resourcePath);
            Assert.DoesNotContain("Wait Until", content);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }
}
