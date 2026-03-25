using Recrd.Core.Ast;
using Recrd.Core.Exceptions;
using Recrd.Gherkin;

namespace Recrd.Gherkin.Tests;

/// <summary>
/// User Story: As a QA analyst, I want the Gherkin generator to produce
/// valid pt-BR .feature files from the AST so that BDD scenarios are
/// human-readable and executable by Cucumber-compatible tools.
///
/// Acceptance criteria:
/// - Sessions without variables emit Cenário
/// - Sessions with variables emit Esquema do Cenário + Exemplos table
/// - Variable mismatch with data produces a hard error
/// - Default step grouping heuristic: navigation→Dado, actions→Quando, assertions→Então
/// - Output is deterministic (same input = byte-identical output)
/// </summary>
public sealed class GherkinGeneratorTests
{
    private static readonly SessionMetadata DefaultMetadata =
        new(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000000"), new DateTimeOffset(2026, 3, 25, 0, 0, 0, TimeSpan.Zero), "chromium", "1280x720", null);

    private static Session BuildSession(IReadOnlyList<Variable> variables, IReadOnlyList<Step> steps) =>
        new(1, DefaultMetadata, variables, steps);

    [Fact]
    public async Task GenerateAsync_NoVariables_EmitsCenario()
    {
        var steps = new Step[]
        {
            new ActionStep(Guid.NewGuid(), "navigation", [], null, null),
            new ActionStep(Guid.NewGuid(), "click", [new Selector("css", "[data-testid='btn']", 1)], null, null),
            new AssertionStep(Guid.NewGuid(), "text_equals", [new Selector("css", "#msg", 1)], "Olá"),
        };
        var session = BuildSession([], steps);
        var generator = new GherkinGenerator();

        var feature = await generator.GenerateAsync(session);

        Assert.Contains("Cenário", feature);
        Assert.DoesNotContain("Esquema do Cenário", feature);
        Assert.DoesNotContain("Exemplos", feature);
    }

    [Fact]
    public async Task GenerateAsync_WithVariables_EmitsEsquemaDoCenarioAndExemplos()
    {
        var variables = new List<Variable> { new("login"), new("senha") };
        var steps = new Step[]
        {
            new ActionStep(Guid.NewGuid(), "navigation", [], null, null),
            new ActionStep(Guid.NewGuid(), "input", [new Selector("css", "#user", 1)], "<login>", "login"),
            new ActionStep(Guid.NewGuid(), "input", [new Selector("css", "#pass", 1)], "<senha>", "senha"),
        };
        var session = BuildSession(variables, steps);
        var data = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string> { ["login"] = "admin", ["senha"] = "123" },
            new Dictionary<string, string> { ["login"] = "user",  ["senha"] = "abc" },
        };
        var generator = new GherkinGenerator();

        var feature = await generator.GenerateAsync(session, data);

        Assert.Contains("Esquema do Cenário", feature);
        Assert.Contains("Exemplos", feature);
        Assert.Contains("| login |", feature);
        Assert.Contains("admin", feature);
    }

    [Fact]
    public async Task GenerateAsync_VariableMissingFromData_ThrowsGherkinException()
    {
        var variables = new List<Variable> { new("email") };
        var steps = new Step[]
        {
            new ActionStep(Guid.NewGuid(), "input", [new Selector("css", "#email", 1)], "<email>", "email"),
        };
        var session = BuildSession(variables, steps);
        // Data has "login" but not "email"
        var data = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string> { ["login"] = "admin" },
        };
        var generator = new GherkinGenerator();

        await Assert.ThrowsAsync<GherkinException>(() => generator.GenerateAsync(session, data));
    }

    [Fact]
    public async Task GenerateAsync_DefaultHeuristic_NavigationMapsToGiven()
    {
        var steps = new Step[]
        {
            new ActionStep(Guid.NewGuid(), "navigation", [], null, null),
        };
        var session = BuildSession([], steps);
        var generator = new GherkinGenerator();

        var feature = await generator.GenerateAsync(session);

        Assert.Contains("Dado", feature);
    }

    [Fact]
    public async Task GenerateAsync_DefaultHeuristic_AssertionMapsToEntao()
    {
        var steps = new Step[]
        {
            new AssertionStep(Guid.NewGuid(), "text_equals", [new Selector("css", "#msg", 1)], "OK"),
        };
        var session = BuildSession([], steps);
        var generator = new GherkinGenerator();

        var feature = await generator.GenerateAsync(session);

        Assert.Contains("Então", feature);
    }

    [Fact]
    public async Task GenerateAsync_DefaultHeuristic_InteractionMapsToQuando()
    {
        var steps = new Step[]
        {
            new ActionStep(Guid.NewGuid(), "click", [new Selector("css", "#btn", 1)], null, null),
        };
        var session = BuildSession([], steps);
        var generator = new GherkinGenerator();

        var feature = await generator.GenerateAsync(session);

        Assert.Contains("Quando", feature);
    }

    [Fact]
    public async Task GenerateAsync_IsIdempotent_SameInputProducesByteIdenticalOutput()
    {
        var variables = new List<Variable> { new("login") };
        var stepId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
        var selId = new Selector("css", "#user", 1);
        var steps = new Step[]
        {
            new ActionStep(stepId, "input", [selId], "<login>", "login"),
        };
        var session = BuildSession(variables, steps);
        var data = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string> { ["login"] = "admin" },
        };
        var generator = new GherkinGenerator();

        var first = await generator.GenerateAsync(session, data);
        var second = await generator.GenerateAsync(session, data);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task GenerateAsync_OutputContainsPtBrLanguageHeader()
    {
        var session = BuildSession([], []);
        var generator = new GherkinGenerator();

        var feature = await generator.GenerateAsync(session);

        Assert.Contains("# language: pt", feature);
    }

    [Fact]
    public async Task GenerateAsync_GroupStep_Given_EmitsGivenKeyword()
    {
        var inner = new ActionStep(Guid.NewGuid(), "navigation", [], null, null);
        var group = new GroupStep(Guid.NewGuid(), GroupType.Given, [inner]);
        var session = BuildSession([], [group]);
        var generator = new GherkinGenerator();

        var feature = await generator.GenerateAsync(session);

        Assert.Contains("Dado", feature);
    }
}
