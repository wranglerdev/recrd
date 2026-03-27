using Recrd.Core.Ast;
using Recrd.Gherkin;
using Xunit;

namespace Recrd.Gherkin.Tests;

/// <summary>
/// Tests covering GHER-07: same AST + same data must produce byte-identical output across runs.
/// </summary>
public class DeterminismTests
{
    private static Selector MakeSelector(string testId) =>
        new(
            Strategies: [SelectorStrategy.DataTestId],
            Values: new Dictionary<SelectorStrategy, string> { [SelectorStrategy.DataTestId] = testId }.AsReadOnly()
        );

    private static Session MakeNonTrivialSession() =>
        new(
            SchemaVersion: 1,
            Metadata: new SessionMetadata(
                Id: "determinism-test",
                CreatedAt: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                BrowserEngine: "chromium",
                ViewportSize: new ViewportSize(1920, 1080),
                BaseUrl: "https://example.com"),
            Variables: [new Variable("username"), new Variable("password")],
            Steps: [
                new GroupStep(
                    GroupType: GroupType.Given,
                    Steps: [
                        new ActionStep(
                            ActionType: ActionType.Navigate,
                            Selector: MakeSelector("nav"),
                            Payload: new Dictionary<string, string> { ["url"] = "https://example.com/login" }.AsReadOnly()
                        )
                    ]
                ),
                new GroupStep(
                    GroupType: GroupType.When,
                    Steps: [
                        new ActionStep(
                            ActionType: ActionType.Type,
                            Selector: MakeSelector("input-user"),
                            Payload: new Dictionary<string, string> { ["value"] = "<username>" }.AsReadOnly()
                        ),
                        new ActionStep(
                            ActionType: ActionType.Type,
                            Selector: MakeSelector("input-pass"),
                            Payload: new Dictionary<string, string> { ["value"] = "<password>" }.AsReadOnly()
                        ),
                        new ActionStep(
                            ActionType: ActionType.Click,
                            Selector: MakeSelector("btn-login"),
                            Payload: new Dictionary<string, string>().AsReadOnly()
                        )
                    ]
                ),
                new GroupStep(
                    GroupType: GroupType.Then,
                    Steps: [
                        new AssertionStep(
                            AssertionType: AssertionType.Visible,
                            Selector: MakeSelector("dashboard"),
                            Payload: new Dictionary<string, string>().AsReadOnly()
                        )
                    ]
                )
            ]
        );

    [Fact]
    public async Task GenerateAsync_SameInput_ProducesByteIdenticalOutput()
    {
        var session = MakeNonTrivialSession();
        var generator = new GherkinGenerator();

        var sw1 = new StringWriter();
        await generator.GenerateAsync(session, null, sw1);
        var output1 = sw1.ToString();

        var sw2 = new StringWriter();
        await generator.GenerateAsync(session, null, sw2);
        var output2 = sw2.ToString();

        Assert.True(
            string.Equals(output1, output2, StringComparison.Ordinal),
            "Two runs with identical input must produce byte-identical output");
    }

    [Fact]
    public async Task GenerateAsync_SameInput_TenRuns_AllIdentical()
    {
        var session = MakeNonTrivialSession();
        var generator = new GherkinGenerator();

        var sw0 = new StringWriter();
        await generator.GenerateAsync(session, null, sw0);
        var reference = sw0.ToString();

        for (int i = 0; i < 9; i++)
        {
            var sw = new StringWriter();
            await generator.GenerateAsync(session, null, sw);
            Assert.True(
                string.Equals(reference, sw.ToString(), StringComparison.Ordinal),
                $"Run {i + 2} produced output different from run 1");
        }
    }
}
