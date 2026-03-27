using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Recrd.Gherkin;
using Xunit;

namespace Recrd.Gherkin.Tests;

/// <summary>
/// Tests covering GHER-03 (missing variable column is hard error)
/// and GHER-04 (extra data column emits warning, does not throw).
/// </summary>
public class VariableMismatchTests
{
    private static Selector MakeSelector(string testId) =>
        new(
            Strategies: [SelectorStrategy.DataTestId],
            Values: new Dictionary<SelectorStrategy, string> { [SelectorStrategy.DataTestId] = testId }.AsReadOnly()
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

    private static Session MakeSessionWithVariable(string variableName) =>
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
                Selector: MakeSelector("input"),
                Payload: new Dictionary<string, string> { ["value"] = $"<{variableName}>" }.AsReadOnly()
            )]
        );

    [Fact]
    public async Task GenerateAsync_MissingVariableInData_ThrowsGherkinException()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = MakeSessionWithVariable("login");
        // Provider has "password" column but NOT "login"
        var provider = new InMemoryDataProvider(
            new Dictionary<string, string> { ["password"] = "s3cr3t" }.AsReadOnly());

        var ex = await Assert.ThrowsAsync<GherkinException>(
            async () => await generator.GenerateAsync(session, provider, sw));

        Assert.Equal("login", ex.VariableName);
    }

    [Fact]
    public async Task GenerateAsync_GherkinException_CarriesDataFilePath()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = MakeSessionWithVariable("login");
        var provider = new InMemoryDataProvider(
            new Dictionary<string, string> { ["password"] = "s3cr3t" }.AsReadOnly());

        var options = new GherkinGeneratorOptions { DataFilePath = "data/test.csv" };

        var ex = await Assert.ThrowsAsync<GherkinException>(
            async () => await generator.GenerateAsync(session, provider, sw, options));

        Assert.Equal("data/test.csv", ex.DataFilePath);
    }

    [Fact]
    public async Task GenerateAsync_ExtraColumnInData_DoesNotThrow()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = MakeSessionWithVariable("login");
        // Provider has both "login" (used) and "extra_col" (extra)
        var provider = new InMemoryDataProvider(
            new Dictionary<string, string>
            {
                ["login"] = "alice",
                ["extra_col"] = "ignored"
            }.AsReadOnly());

        // Should NOT throw — extra columns are warnings, not errors
        await generator.GenerateAsync(session, provider, sw);
    }

    [Fact]
    public async Task GenerateAsync_ExtraColumnInData_EmitsWarningToStderr()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var warningWriter = new StringWriter();
        var session = MakeSessionWithVariable("login");
        var provider = new InMemoryDataProvider(
            new Dictionary<string, string>
            {
                ["login"] = "alice",
                ["extra_col"] = "ignored"
            }.AsReadOnly());

        var options = new GherkinGeneratorOptions { WarningWriter = warningWriter };

        await generator.GenerateAsync(session, provider, sw, options);

        Assert.Contains("extra_col", warningWriter.ToString());
    }
}
