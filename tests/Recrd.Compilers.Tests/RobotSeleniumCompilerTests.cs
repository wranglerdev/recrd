using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Recrd.Compilers;

namespace Recrd.Compilers.Tests;

/// <summary>
/// User Story: As a QA engineer working on legacy systems, I want the
/// robot-selenium compiler to emit valid SeleniumLibrary keywords so that
/// I can run tests against systems that don't support Browser library.
///
/// Acceptance criteria:
/// - Click ActionStep → Click Element keyword with id: or css: locator
/// - Text assertion → Element Text Should Be keyword
/// - Traceability header present in all output files
/// </summary>
public sealed class RobotSeleniumCompilerTests
{
    private static readonly SessionMetadata DefaultMetadata =
        new(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000000"), new DateTimeOffset(2026, 3, 25, 0, 0, 0, TimeSpan.Zero), "chromium", "1280x720", null);

    private static Session BuildSession(IReadOnlyList<Step> steps) =>
        new(1, DefaultMetadata, [], steps);

    [Fact]
    public async Task CompileAsync_ClickStep_EmitsClickElementKeyword()
    {
        var steps = new Step[]
        {
            new ActionStep(Guid.NewGuid(), "click",
                [new Selector("id", "submit-btn", 1)], null, null),
        };
        var session = BuildSession(steps);
        var compiler = new RobotSeleniumCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        var suiteContent = result.Files.First(f => f.RelativePath.EndsWith(".robot")).Content;
        Assert.Contains("Click Element", suiteContent);
        Assert.Contains("id:submit-btn", suiteContent);
    }

    [Fact]
    public async Task CompileAsync_TextAssertion_EmitsElementTextShouldBe()
    {
        var steps = new Step[]
        {
            new AssertionStep(Guid.NewGuid(), "text_equals",
                [new Selector("id", "welcome", 1)], "Olá, Admin"),
        };
        var session = BuildSession(steps);
        var compiler = new RobotSeleniumCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        var suiteContent = result.Files.First(f => f.RelativePath.EndsWith(".robot")).Content;
        Assert.Contains("Element Text Should Be", suiteContent);
        Assert.Contains("Olá, Admin", suiteContent);
    }

    [Fact]
    public async Task CompileAsync_Output_IncludesTraceabilityHeader()
    {
        var session = BuildSession([]);
        var compiler = new RobotSeleniumCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        foreach (var file in result.Files)
        {
            Assert.Contains("robot-selenium", file.Content);
        }
    }

    [Fact]
    public async Task CompileAsync_VisibilityAssertion_EmitsElementShouldBeVisible()
    {
        var steps = new Step[]
        {
            new AssertionStep(Guid.NewGuid(), "element_visible",
                [new Selector("id", "modal", 1)], null),
        };
        var session = BuildSession(steps);
        var compiler = new RobotSeleniumCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        var suiteContent = result.Files.First(f => f.RelativePath.EndsWith(".robot")).Content;
        Assert.Contains("Element Should Be Visible", suiteContent);
    }
}
