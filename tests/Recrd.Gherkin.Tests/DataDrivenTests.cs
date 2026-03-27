using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Recrd.Gherkin;
using Xunit;

namespace Recrd.Gherkin.Tests;

/// <summary>
/// Tests covering GHER-02 (Esquema do Cenário + Exemplos for variable sessions)
/// and GHER-09 (column order matches first appearance in body).
/// </summary>
public class DataDrivenTests
{
    private static Selector MakeSelector(string testId) =>
        new(
            Strategies: [SelectorStrategy.DataTestId],
            Values: new Dictionary<SelectorStrategy, string> { [SelectorStrategy.DataTestId] = testId }.AsReadOnly()
        );

    private static async IAsyncEnumerable<IReadOnlyDictionary<string, string>> AsyncRows(
        params IReadOnlyDictionary<string, string>[] rows)
    {
        foreach (var row in rows) yield return row;
        await Task.CompletedTask;
    }

    private static Session MakeSingleVariableSession(string variableName = "login") =>
        new(
            SchemaVersion: 1,
            Metadata: new SessionMetadata(
                Id: "test-id",
                CreatedAt: DateTimeOffset.UtcNow,
                BrowserEngine: "chromium",
                ViewportSize: new ViewportSize(1280, 720)),
            Variables: [new Variable(variableName)],
            Steps: [new ActionStep(
                ActionType: ActionType.Type,
                Selector: MakeSelector("input-login"),
                Payload: new Dictionary<string, string> { ["value"] = $"<{variableName}>" }.AsReadOnly()
            )]
        );

    private class InMemoryDataProvider : IDataProvider
    {
        private readonly IReadOnlyDictionary<string, string>[] _rows;
        public InMemoryDataProvider(params IReadOnlyDictionary<string, string>[] rows) => _rows = rows;

        public async IAsyncEnumerable<IReadOnlyDictionary<string, string>> StreamAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var row in _rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return row;
            }
            await Task.CompletedTask;
        }
    }

    [Fact]
    public async Task GenerateAsync_SessionWithVariables_EmitsEsquemaDoCenario()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = MakeSingleVariableSession("login");
        var provider = new InMemoryDataProvider(
            new Dictionary<string, string> { ["login"] = "alice" }.AsReadOnly());

        await generator.GenerateAsync(session, provider, sw);

        var output = sw.ToString();
        Assert.Contains("Esquema do Cen\u00e1rio:", output);
        Assert.Contains("Exemplos:", output);
    }

    [Fact]
    public async Task GenerateAsync_SessionWithVariables_EmitsPipeDelimitedExemplosTable()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = MakeSingleVariableSession("login");
        var provider = new InMemoryDataProvider(
            new Dictionary<string, string> { ["login"] = "alice" }.AsReadOnly());

        await generator.GenerateAsync(session, provider, sw);

        var output = sw.ToString();
        Assert.Contains("| login |", output);
        Assert.Contains("| alice |", output);
    }

    [Fact]
    public async Task GenerateAsync_ExemplosColumnOrder_MatchesFirstAppearanceInBody()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();

        // Variables declared as password, login (that order)
        // But steps reference <login> first, <password> second
        var session = new Session(
            SchemaVersion: 1,
            Metadata: new SessionMetadata(
                Id: "test-id",
                CreatedAt: DateTimeOffset.UtcNow,
                BrowserEngine: "chromium",
                ViewportSize: new ViewportSize(1280, 720)),
            Variables: [new Variable("password"), new Variable("login")],
            Steps: [
                new ActionStep(
                    ActionType: ActionType.Type,
                    Selector: MakeSelector("input-login"),
                    Payload: new Dictionary<string, string> { ["value"] = "<login>" }.AsReadOnly()
                ),
                new ActionStep(
                    ActionType: ActionType.Type,
                    Selector: MakeSelector("input-password"),
                    Payload: new Dictionary<string, string> { ["value"] = "<password>" }.AsReadOnly()
                )
            ]
        );

        var provider = new InMemoryDataProvider(
            new Dictionary<string, string> { ["login"] = "alice", ["password"] = "s3cr3t" }.AsReadOnly());

        await generator.GenerateAsync(session, provider, sw);

        var output = sw.ToString();
        // login should appear before password in the Exemplos header
        var loginIdx = output.IndexOf("| login |", StringComparison.Ordinal);
        var passwordIdx = output.IndexOf("| password |", StringComparison.Ordinal);
        // Find header line containing both: verify login comes before password
        Assert.True(loginIdx >= 0, "Should contain '| login |'");
        Assert.True(passwordIdx >= 0, "Should contain '| password |'");
        // The header row has both on the same line: | login | password |
        Assert.Contains("| login | password |", output);
    }

    [Fact]
    public async Task GenerateAsync_MultipleDataRows_AllEmittedInExemplosTable()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = MakeSingleVariableSession("login");
        var provider = new InMemoryDataProvider(
            new Dictionary<string, string> { ["login"] = "alice" }.AsReadOnly(),
            new Dictionary<string, string> { ["login"] = "bob" }.AsReadOnly(),
            new Dictionary<string, string> { ["login"] = "carol" }.AsReadOnly());

        await generator.GenerateAsync(session, provider, sw);

        var output = sw.ToString();
        Assert.Contains("| alice |", output);
        Assert.Contains("| bob |", output);
        Assert.Contains("| carol |", output);
    }
}
