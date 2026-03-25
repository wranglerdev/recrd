using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Recrd.Compilers;

namespace Recrd.Compilers.Tests;

/// <summary>
/// User Story: As a QA engineer, I want the robot-browser compiler to emit
/// valid Robot Framework + Browser library keywords from the AST so that
/// the generated suite can execute without manual edits.
///
/// Acceptance criteria:
/// - Click ActionStep → Click keyword with css selector
/// - Text assertion → Get Text keyword with == comparator
/// - Every generated file contains a traceability header comment
/// - Compilation result includes both .robot suite and .resource file
/// </summary>
public sealed class RobotBrowserCompilerTests
{
    private static readonly SessionMetadata DefaultMetadata =
        new(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000000"), new DateTimeOffset(2026, 3, 25, 0, 0, 0, TimeSpan.Zero), "chromium", "1280x720", null);

    private static Session BuildSession(IReadOnlyList<Step> steps) =>
        new(1, DefaultMetadata, [], steps);

    [Fact]
    public async Task CompileAsync_ClickStep_EmitsClickKeyword()
    {
        var steps = new Step[]
        {
            new ActionStep(Guid.NewGuid(), "click",
                [new Selector("data-testid", "submit", 1)], null, null),
        };
        var session = BuildSession(steps);
        var compiler = new RobotBrowserCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        var suiteContent = result.Files.First(f => f.RelativePath.EndsWith(".robot")).Content;
        Assert.Contains("Click", suiteContent);
        Assert.Contains("data-testid=submit", suiteContent);
    }

    [Fact]
    public async Task CompileAsync_TextAssertion_EmitsGetTextKeyword()
    {
        var steps = new Step[]
        {
            new AssertionStep(Guid.NewGuid(), "text_equals",
                [new Selector("css", "#welcome", 1)], "Olá, Admin"),
        };
        var session = BuildSession(steps);
        var compiler = new RobotBrowserCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        var suiteContent = result.Files.First(f => f.RelativePath.EndsWith(".robot")).Content;
        Assert.Contains("Get Text", suiteContent);
        Assert.Contains("==", suiteContent);
        Assert.Contains("Olá, Admin", suiteContent);
    }

    [Fact]
    public async Task CompileAsync_Output_IncludesTraceabilityHeader()
    {
        var session = BuildSession([]);
        var compiler = new RobotBrowserCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        foreach (var file in result.Files)
        {
            Assert.True(
                file.Content.StartsWith('#') || file.Content.Contains("# recrd"),
                $"File {file.RelativePath} missing traceability header");
            Assert.Contains("robot-browser", file.Content);
        }
    }

    [Fact]
    public async Task CompileAsync_Output_IncludesBothSuiteAndResourceFile()
    {
        var session = BuildSession([]);
        var compiler = new RobotBrowserCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        Assert.Contains(result.Files, f => f.RelativePath.EndsWith(".robot"));
        Assert.Contains(result.Files, f => f.RelativePath.EndsWith(".resource"));
    }

    [Fact]
    public async Task CompileAsync_VisibilityAssertion_EmitsGetElementStateKeyword()
    {
        var steps = new Step[]
        {
            new AssertionStep(Guid.NewGuid(), "element_visible",
                [new Selector("css", "#modal", 1)], null),
        };
        var session = BuildSession(steps);
        var compiler = new RobotBrowserCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        var suiteContent = result.Files.First(f => f.RelativePath.EndsWith(".robot")).Content;
        Assert.Contains("Get Element State", suiteContent);
        Assert.Contains("visible", suiteContent);
    }

    [Fact]
    public async Task CompileAsync_NavigationStep_EmitsGoToKeyword()
    {
        var steps = new Step[]
        {
            new ActionStep(Guid.NewGuid(), "navigation", [], "https://example.com", null),
        };
        var session = BuildSession(steps);
        var compiler = new RobotBrowserCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        var suiteContent = result.Files.First(f => f.RelativePath.EndsWith(".robot")).Content;
        Assert.Contains("Go To", suiteContent);
    }
}
