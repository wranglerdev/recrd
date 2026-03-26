using System.Collections.Generic;
using System.Text.Json;
using Recrd.Core.Ast;
using Recrd.Core.Serialization;
using Xunit;

namespace Recrd.Core.Tests;

public sealed class SessionSerializationTests
{
    [Fact]
    public void Session_RoundTrips_WithAllFields()
    {
        var session = new Session(
            SchemaVersion: 1,
            Metadata: new SessionMetadata(
                Id: "test-session-id",
                CreatedAt: new System.DateTimeOffset(2026, 1, 1, 0, 0, 0, System.TimeSpan.Zero),
                BrowserEngine: "chromium",
                ViewportSize: new ViewportSize(Width: 1280, Height: 720),
                BaseUrl: "https://example.com"
            ),
            Variables: new List<Variable>(),
            Steps: new List<IStep>
            {
                new ActionStep(
                    ActionType: ActionType.Click,
                    Selector: new Selector(
                        Strategies: new List<SelectorStrategy> { SelectorStrategy.DataTestId },
                        Values: new Dictionary<SelectorStrategy, string>
                        {
                            [SelectorStrategy.DataTestId] = "[data-testid=\"submit\"]"
                        }
                    ),
                    Payload: new Dictionary<string, string>()
                )
            }
        );

        var json = JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session);
        var deserialized = JsonSerializer.Deserialize(json, RecrdJsonContext.Default.Session);

        Assert.NotNull(deserialized);
        Assert.Equal(1, deserialized.SchemaVersion);
        Assert.Equal("test-session-id", deserialized.Metadata.Id);
        Assert.Equal("chromium", deserialized.Metadata.BrowserEngine);
        Assert.Equal(1280, deserialized.Metadata.ViewportSize.Width);
        Assert.Equal(720, deserialized.Metadata.ViewportSize.Height);
        Assert.Equal("https://example.com", deserialized.Metadata.BaseUrl);
        Assert.Empty(deserialized.Variables);
        Assert.Single(deserialized.Steps);
    }

    [Fact]
    public void Session_RoundTrips_WithPolymorphicSteps()
    {
        var session = new Session(
            SchemaVersion: 1,
            Metadata: new SessionMetadata(
                Id: "poly-session",
                CreatedAt: System.DateTimeOffset.UtcNow,
                BrowserEngine: "chromium",
                ViewportSize: new ViewportSize(Width: 1280, Height: 720),
                BaseUrl: "https://example.com"
            ),
            Variables: new List<Variable>(),
            Steps: new List<IStep>
            {
                new ActionStep(
                    ActionType: ActionType.Click,
                    Selector: new Selector(
                        Strategies: new List<SelectorStrategy> { SelectorStrategy.DataTestId },
                        Values: new Dictionary<SelectorStrategy, string>
                        {
                            [SelectorStrategy.DataTestId] = "[data-testid=\"btn\"]"
                        }
                    ),
                    Payload: new Dictionary<string, string>()
                ),
                new AssertionStep(
                    AssertionType: AssertionType.Visible,
                    Selector: new Selector(
                        Strategies: new List<SelectorStrategy> { SelectorStrategy.Id },
                        Values: new Dictionary<SelectorStrategy, string>
                        {
                            [SelectorStrategy.Id] = "#result"
                        }
                    ),
                    Payload: new Dictionary<string, string>()
                ),
                new GroupStep(
                    GroupType: GroupType.Given,
                    Steps: new List<IStep>()
                )
            }
        );

        var json = JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session);
        var deserialized = JsonSerializer.Deserialize(json, RecrdJsonContext.Default.Session);

        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Steps.Count);
        Assert.IsType<ActionStep>(deserialized.Steps[0]);
        Assert.IsType<AssertionStep>(deserialized.Steps[1]);
        Assert.IsType<GroupStep>(deserialized.Steps[2]);
    }

    [Fact]
    public void Session_RoundTrips_WithVariables()
    {
        var session = new Session(
            SchemaVersion: 1,
            Metadata: new SessionMetadata(
                Id: "vars-session",
                CreatedAt: System.DateTimeOffset.UtcNow,
                BrowserEngine: "chromium",
                ViewportSize: new ViewportSize(Width: 1280, Height: 720),
                BaseUrl: "https://example.com"
            ),
            Variables: new List<Variable>
            {
                new Variable("username"),
                new Variable("password")
            },
            Steps: new List<IStep>()
        );

        var json = JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session);
        var deserialized = JsonSerializer.Deserialize(json, RecrdJsonContext.Default.Session);

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Variables.Count);
        Assert.Equal("username", deserialized.Variables[0].Name);
        Assert.Equal("password", deserialized.Variables[1].Name);
    }

    [Fact]
    public void Session_Serialization_EmitsCamelCaseKeys()
    {
        var session = new Session(
            SchemaVersion: 1,
            Metadata: new SessionMetadata(
                Id: "key-test",
                CreatedAt: System.DateTimeOffset.UtcNow,
                BrowserEngine: "chromium",
                ViewportSize: new ViewportSize(Width: 1280, Height: 720),
                BaseUrl: "https://example.com"
            ),
            Variables: new List<Variable>(),
            Steps: new List<IStep>
            {
                new ActionStep(
                    ActionType: ActionType.Navigate,
                    Selector: new Selector(
                        Strategies: new List<SelectorStrategy> { SelectorStrategy.DataTestId },
                        Values: new Dictionary<SelectorStrategy, string>
                        {
                            [SelectorStrategy.DataTestId] = "[data-testid=\"nav\"]"
                        }
                    ),
                    Payload: new Dictionary<string, string>()
                )
            }
        );

        var json = JsonSerializer.Serialize(session, RecrdJsonContext.Default.Session);

        Assert.Contains("\"schemaVersion\"", json);
        Assert.Contains("\"metadata\"", json);
        Assert.Contains("\"steps\"", json);
        Assert.Contains("\"$type\"", json);
    }
}
