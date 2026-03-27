using Recrd.Core.Ast;
using Recrd.Gherkin;
using Xunit;

namespace Recrd.Gherkin.Tests;

/// <summary>
/// Tests covering GHER-05 (GroupStep → Dado/Quando/Então/E)
/// and GHER-06 (default heuristic when no GroupStep: navigate→Dado, interactions→Quando, assertions→Então).
/// </summary>
public class GroupingTests
{
    private static Selector MakeSelector(string testId) =>
        new(
            Strategies: [SelectorStrategy.DataTestId],
            Values: new Dictionary<SelectorStrategy, string> { [SelectorStrategy.DataTestId] = testId }.AsReadOnly()
        );

    private static SessionMetadata DefaultMeta() =>
        new(
            Id: "test-id",
            CreatedAt: DateTimeOffset.UtcNow,
            BrowserEngine: "chromium",
            ViewportSize: new ViewportSize(1280, 720));

    [Fact]
    public async Task GenerateAsync_GroupStepGiven_EmitsDado()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = new Session(
            SchemaVersion: 1,
            Metadata: DefaultMeta(),
            Variables: [],
            Steps: [new GroupStep(
                GroupType: GroupType.Given,
                Steps: [new ActionStep(
                    ActionType: ActionType.Navigate,
                    Selector: MakeSelector("nav"),
                    Payload: new Dictionary<string, string> { ["url"] = "https://example.com" }.AsReadOnly()
                )]
            )]
        );

        await generator.GenerateAsync(session, null, sw);

        Assert.Contains("Dado ", sw.ToString());
    }

    [Fact]
    public async Task GenerateAsync_GroupStepWhen_EmitsQuando()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = new Session(
            SchemaVersion: 1,
            Metadata: DefaultMeta(),
            Variables: [],
            Steps: [new GroupStep(
                GroupType: GroupType.When,
                Steps: [new ActionStep(
                    ActionType: ActionType.Click,
                    Selector: MakeSelector("btn"),
                    Payload: new Dictionary<string, string>().AsReadOnly()
                )]
            )]
        );

        await generator.GenerateAsync(session, null, sw);

        Assert.Contains("Quando ", sw.ToString());
    }

    [Fact]
    public async Task GenerateAsync_GroupStepThen_EmitsEntao()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = new Session(
            SchemaVersion: 1,
            Metadata: DefaultMeta(),
            Variables: [],
            Steps: [new GroupStep(
                GroupType: GroupType.Then,
                Steps: [new AssertionStep(
                    AssertionType: AssertionType.Visible,
                    Selector: MakeSelector("result"),
                    Payload: new Dictionary<string, string>().AsReadOnly()
                )]
            )]
        );

        await generator.GenerateAsync(session, null, sw);

        Assert.Contains("Ent\u00e3o ", sw.ToString());
    }

    [Fact]
    public async Task GenerateAsync_MultipleStepsInGroup_UsesEContinuation()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = new Session(
            SchemaVersion: 1,
            Metadata: DefaultMeta(),
            Variables: [],
            Steps: [new GroupStep(
                GroupType: GroupType.Given,
                Steps: [
                    new ActionStep(
                        ActionType: ActionType.Navigate,
                        Selector: MakeSelector("nav"),
                        Payload: new Dictionary<string, string> { ["url"] = "https://example.com" }.AsReadOnly()
                    ),
                    new ActionStep(
                        ActionType: ActionType.Click,
                        Selector: MakeSelector("btn"),
                        Payload: new Dictionary<string, string>().AsReadOnly()
                    )
                ]
            )]
        );

        await generator.GenerateAsync(session, null, sw);

        var output = sw.ToString();
        Assert.Contains("Dado ", output);
        Assert.Contains("E ", output);
    }

    [Fact]
    public async Task GenerateAsync_DefaultHeuristic_FirstNavigateIsDado()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = new Session(
            SchemaVersion: 1,
            Metadata: DefaultMeta(),
            Variables: [],
            Steps: [
                new ActionStep(
                    ActionType: ActionType.Navigate,
                    Selector: MakeSelector("nav"),
                    Payload: new Dictionary<string, string> { ["url"] = "https://example.com" }.AsReadOnly()
                ),
                new ActionStep(
                    ActionType: ActionType.Click,
                    Selector: MakeSelector("btn"),
                    Payload: new Dictionary<string, string>().AsReadOnly()
                ),
                new AssertionStep(
                    AssertionType: AssertionType.Visible,
                    Selector: MakeSelector("result"),
                    Payload: new Dictionary<string, string>().AsReadOnly()
                )
            ]
        );

        await generator.GenerateAsync(session, null, sw);

        Assert.Contains("Dado", sw.ToString());
    }

    [Fact]
    public async Task GenerateAsync_DefaultHeuristic_InteractionsAreQuando()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = new Session(
            SchemaVersion: 1,
            Metadata: DefaultMeta(),
            Variables: [],
            Steps: [
                new ActionStep(
                    ActionType: ActionType.Navigate,
                    Selector: MakeSelector("nav"),
                    Payload: new Dictionary<string, string> { ["url"] = "https://example.com" }.AsReadOnly()
                ),
                new ActionStep(
                    ActionType: ActionType.Click,
                    Selector: MakeSelector("btn"),
                    Payload: new Dictionary<string, string>().AsReadOnly()
                ),
                new AssertionStep(
                    AssertionType: AssertionType.Visible,
                    Selector: MakeSelector("result"),
                    Payload: new Dictionary<string, string>().AsReadOnly()
                )
            ]
        );

        await generator.GenerateAsync(session, null, sw);

        Assert.Contains("Quando", sw.ToString());
    }

    [Fact]
    public async Task GenerateAsync_DefaultHeuristic_AssertionsAreEntao()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = new Session(
            SchemaVersion: 1,
            Metadata: DefaultMeta(),
            Variables: [],
            Steps: [
                new ActionStep(
                    ActionType: ActionType.Navigate,
                    Selector: MakeSelector("nav"),
                    Payload: new Dictionary<string, string> { ["url"] = "https://example.com" }.AsReadOnly()
                ),
                new ActionStep(
                    ActionType: ActionType.Click,
                    Selector: MakeSelector("btn"),
                    Payload: new Dictionary<string, string>().AsReadOnly()
                ),
                new AssertionStep(
                    AssertionType: AssertionType.Visible,
                    Selector: MakeSelector("result"),
                    Payload: new Dictionary<string, string>().AsReadOnly()
                )
            ]
        );

        await generator.GenerateAsync(session, null, sw);

        var output = sw.ToString();
        Assert.True(
            output.Contains("Ent\u00e3o") || output.Contains("E "),
            "Should contain Então or E continuation for assertions");
    }
}
