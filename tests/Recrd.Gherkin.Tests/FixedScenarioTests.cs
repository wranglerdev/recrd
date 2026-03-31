using System.Text;
using Recrd.Core.Ast;
using Recrd.Gherkin;
using Xunit;

namespace Recrd.Gherkin.Tests;

/// <summary>
/// Tests covering GHER-01 (Cenário for zero-variable sessions),
/// GHER-07 (deterministic output), GHER-08 (UTF-8 no-BOM, language header, feature name).
/// </summary>
public class FixedScenarioTests
{
    private static Selector MakeSelector(string testId) =>
        new(
            Strategies: [SelectorStrategy.DataTestId],
            Values: new Dictionary<SelectorStrategy, string> { [SelectorStrategy.DataTestId] = testId }.AsReadOnly()
        );

    private static Session MakeZeroVariableSession(string? baseUrl = null) =>
        new(
            SchemaVersion: 1,
            Metadata: new SessionMetadata(
                Id: "test-id",
                CreatedAt: DateTimeOffset.UtcNow,
                BrowserEngine: "chromium",
                ViewportSize: new ViewportSize(1280, 720),
                BaseUrl: baseUrl),
            Variables: [],
            Steps: [new ActionStep(
                ActionType: ActionType.Navigate,
                Selector: MakeSelector("nav"),
                Payload: new Dictionary<string, string> { ["url"] = "https://example.com" }.AsReadOnly()
            )]
        );

    [Fact]
    public async Task GenerateAsync_ZeroVariableSession_EmitsCenario()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();

        await generator.GenerateAsync(MakeZeroVariableSession(), null, sw);

        var output = sw.ToString();
        Assert.StartsWith("# language: pt", output);
        Assert.Contains("Funcionalidade:", output);
        Assert.Contains("Cen\u00e1rio:", output);
        Assert.DoesNotContain("Esquema do Cen\u00e1rio", output);
        Assert.DoesNotContain("Exemplos", output);
    }

    [Fact]
    public async Task GenerateAsync_ZeroVariableSession_NoExemplosTable()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();

        await generator.GenerateAsync(MakeZeroVariableSession(), null, sw);

        var output = sw.ToString();
        Assert.DoesNotContain("|", output);
    }

    [Fact]
    public async Task GenerateAsync_Output_IsUtf8NoBom()
    {
        var generator = new GherkinGenerator();
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        await generator.GenerateAsync(MakeZeroVariableSession(), null, writer);
        await writer.FlushAsync();

        var bytes = ms.ToArray();
        Assert.True(bytes.Length >= 3, "Output should not be empty");
        // BOM is 0xEF 0xBB 0xBF — assert first bytes are NOT the BOM
        bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        Assert.False(hasBom, "Output must not have UTF-8 BOM");
    }

    [Fact]
    public async Task GenerateAsync_Output_StartsWithLanguageHeader()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();

        await generator.GenerateAsync(MakeZeroVariableSession(), null, sw);

        var firstLine = sw.ToString().Split('\n')[0].TrimEnd('\r');
        Assert.Equal("# language: pt", firstLine);
    }

    [Fact]
    public async Task GenerateAsync_FeatureName_DerivedFromBaseUrl()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        const string baseUrl = "https://example.com";

        await generator.GenerateAsync(MakeZeroVariableSession(baseUrl), null, sw);

        Assert.Contains($"Funcionalidade: {baseUrl}", sw.ToString());
    }

    // D-04: Tags emitted above Cenário when options.Tags is provided
    [Fact]
    public async Task GenerateAsync_WithTags_EmitsTagsAboveScenario()
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var options = new GherkinGeneratorOptions { Tags = ["@smoke", "@regression"] };

        await generator.GenerateAsync(MakeZeroVariableSession(), null, sw, options);

        var output = sw.ToString();
        Assert.Contains("@smoke", output);
        Assert.Contains("@regression", output);
        // Tags must appear before Cenário
        var tagPos = output.IndexOf("@smoke", StringComparison.Ordinal);
        var scenarioPos = output.IndexOf("Cenário:", StringComparison.Ordinal);
        Assert.True(tagPos < scenarioPos);
    }

    // StepTextRenderer: assertion types TextEquals, TextContains, Enabled, UrlMatches
    [Theory]
    [InlineData(AssertionType.TextEquals, "O texto do elemento")]
    [InlineData(AssertionType.TextContains, "contém")]
    [InlineData(AssertionType.Enabled, "habilitado")]
    [InlineData(AssertionType.UrlMatches, "URL corresponde")]
    public async Task GenerateAsync_AssertionTypes_RenderCorrectPtBrText(
        AssertionType assertionType, string expectedFragment)
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = new Session(
            SchemaVersion: 1,
            Metadata: new SessionMetadata("id", DateTimeOffset.UtcNow, "chromium", new ViewportSize(1280, 720), null),
            Variables: [],
            Steps: [new AssertionStep(
                AssertionType: assertionType,
                Selector: MakeSelector("btn"),
                Payload: new Dictionary<string, string>
                {
                    ["expected"] = "value",
                    ["pattern"] = ".*example.*"
                }.AsReadOnly()
            )]
        );

        await generator.GenerateAsync(session, null, sw);

        Assert.Contains(expectedFragment, sw.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    // StepTextRenderer: Upload and DragDrop action types render pt-BR text
    [Theory]
    [InlineData(ActionType.Upload, "Envia o arquivo")]
    [InlineData(ActionType.DragDrop, "Arrasta")]
    public async Task GenerateAsync_ActionTypes_RenderCorrectPtBrText(
        ActionType actionType, string expectedFragment)
    {
        var generator = new GherkinGenerator();
        var sw = new StringWriter();
        var session = new Session(
            SchemaVersion: 1,
            Metadata: new SessionMetadata("id", DateTimeOffset.UtcNow, "chromium", new ViewportSize(1280, 720), null),
            Variables: [],
            Steps: [new ActionStep(
                ActionType: actionType,
                Selector: MakeSelector("dropzone"),
                Payload: new Dictionary<string, string>
                {
                    ["filename"] = "file.csv",
                    ["target"] = "[data-testid=\"target\"]"
                }.AsReadOnly()
            )]
        );

        await generator.GenerateAsync(session, null, sw);

        Assert.Contains(expectedFragment, sw.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
