using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Recrd.Data;
using Recrd.Gherkin;
using Recrd.Compilers;

namespace Recrd.Integration.Tests;

/// <summary>
/// User Story: As a QA team, we want a full record → compile → execute
/// round-trip to succeed without manual edits so that the tool delivers
/// on its zero-boilerplate promise.
///
/// Acceptance criteria:
/// - A session with variables + CSV data produces a valid .feature and .robot
/// - The generated .robot suite references keywords defined in the .resource file
/// - Output is deterministic across two compile runs of the same session
/// </summary>
public sealed class RoundTripTests
{
    private static Session BuildLoginSession()
    {
        var metadata = new SessionMetadata(
            Guid.Parse("a1b2c3d4-0000-0000-0000-000000000000"),
            new DateTimeOffset(2026, 3, 25, 0, 0, 0, TimeSpan.Zero),
            "chromium", "1280x720", "https://example.com");

        var variables = new List<Variable> { new("login"), new("senha"), new("mensagem") };

        var steps = new Step[]
        {
            new ActionStep(Guid.NewGuid(), "navigation", [], "https://example.com/login", null),
            new ActionStep(Guid.NewGuid(), "input",
                [new Selector("data-testid", "username", 1), new Selector("id", "user", 2)],
                "<login>", "login"),
            new ActionStep(Guid.NewGuid(), "input",
                [new Selector("data-testid", "password", 1), new Selector("id", "pass", 2)],
                "<senha>", "senha"),
            new ActionStep(Guid.NewGuid(), "click",
                [new Selector("data-testid", "submit", 1), new Selector("id", "btn-login", 2)],
                null, null),
            new AssertionStep(Guid.NewGuid(), "text_equals",
                [new Selector("css", "#welcome-msg", 1)], "<mensagem>"),
        };

        return new Session(1, metadata, variables, steps);
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> BuildTestData() =>
    [
        new Dictionary<string, string> { ["login"] = "admin", ["senha"] = "123", ["mensagem"] = "Olá, Admin" },
        new Dictionary<string, string> { ["login"] = "user",  ["senha"] = "abc", ["mensagem"] = "Olá, Usuário" },
    ];

    [Fact]
    public async Task GherkinPipeline_SessionWithVariablesAndData_ProducesValidFeature()
    {
        var session = BuildLoginSession();
        var data = BuildTestData();
        var generator = new GherkinGenerator();

        var feature = await generator.GenerateAsync(session, data);

        Assert.Contains("# language: pt", feature);
        Assert.Contains("Esquema do Cenário", feature);
        Assert.Contains("Exemplos", feature);
        Assert.Contains("admin", feature);
        Assert.Contains("Olá, Admin", feature);
    }

    [Fact]
    public async Task BrowserCompilerPipeline_SessionWithSteps_ProducesSuiteAndResource()
    {
        var session = BuildLoginSession();
        var compiler = new RobotBrowserCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        Assert.Contains(result.Files, f => f.RelativePath.EndsWith(".robot"));
        Assert.Contains(result.Files, f => f.RelativePath.EndsWith(".resource"));

        var suite = result.Files.First(f => f.RelativePath.EndsWith(".robot")).Content;
        var resource = result.Files.First(f => f.RelativePath.EndsWith(".resource")).Content;

        // Suite must import the resource file
        Assert.Contains(".resource", suite);
        // Resource must define at least one keyword
        Assert.Contains("*** Keywords ***", resource);
    }

    [Fact]
    public async Task BrowserCompilerPipeline_TwoRunsSameSession_ProducesByteIdenticalOutput()
    {
        var session = BuildLoginSession();
        var compiler = new RobotBrowserCompiler();
        var options = new CompilerOptions();

        var first  = await compiler.CompileAsync(session, options);
        var second = await compiler.CompileAsync(session, options);

        Assert.Equal(first.Files.Count, second.Files.Count);
        foreach (var (a, b) in first.Files.Zip(second.Files))
        {
            Assert.Equal(a.RelativePath, b.RelativePath);
            Assert.Equal(a.Content, b.Content);
        }
    }

    [Fact]
    public async Task SeleniumCompilerPipeline_SessionWithSteps_ProducesSeleniumKeywords()
    {
        var session = BuildLoginSession();
        var compiler = new RobotSeleniumCompiler();

        var result = await compiler.CompileAsync(session, new CompilerOptions());

        var suite = result.Files.First(f => f.RelativePath.EndsWith(".robot")).Content;
        Assert.Contains("Click Element", suite);
    }
}
