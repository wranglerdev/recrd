using Recrd.Core.Ast;

namespace Recrd.Core.Tests.Ast;

/// <summary>
/// User Story: As a recording engine, I want to represent browser interactions
/// as typed AST step nodes so that compilers can emit correct test keywords.
/// </summary>
public sealed class StepTests
{
    [Fact]
    public void ActionStep_WithDataVariable_ExposesVariableName()
    {
        var selectors = new[] { new Selector("css", "[data-testid='username']", 1) };
        var step = new ActionStep(Guid.NewGuid(), "input", selectors, "<login>", "login");

        Assert.Equal("login", step.DataVariable);
        Assert.Equal("<login>", step.Value);
    }

    [Fact]
    public void AssertionStep_WithExpectedValue_ExposesAssertionType()
    {
        var selectors = new[] { new Selector("css", "#welcome", 1) };
        var step = new AssertionStep(Guid.NewGuid(), "text_equals", selectors, "Olá, Admin");

        Assert.Equal("text_equals", step.AssertionType);
        Assert.Equal("Olá, Admin", step.ExpectedValue);
    }

    [Fact]
    public void GroupStep_Given_HasCorrectGroupType()
    {
        var inner = new ActionStep(Guid.NewGuid(), "navigation", [], null, null);
        var group = new GroupStep(Guid.NewGuid(), GroupType.Given, [inner]);

        Assert.Equal(GroupType.Given, group.GroupType);
        Assert.Single(group.Steps);
    }

    [Fact]
    public void ActionStep_IsAssignableToStep()
    {
        Step step = new ActionStep(Guid.NewGuid(), "click", [], null, null);
        Assert.IsAssignableFrom<Step>(step);
    }
}
