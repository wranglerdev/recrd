using System.Collections.Generic;
using Recrd.Core.Ast;
using Xunit;

namespace Recrd.Core.Tests;

public sealed class StepModelTests
{
    public static TheoryData<ActionType> AllActionTypes => new(Enum.GetValues<ActionType>());

    public static TheoryData<AssertionType> AllAssertionTypes => new(Enum.GetValues<AssertionType>());

    public static TheoryData<GroupType> AllGroupTypes => new(Enum.GetValues<GroupType>());

    private static Selector TestSelector => new Selector(
        Strategies: new List<SelectorStrategy> { SelectorStrategy.DataTestId },
        Values: new Dictionary<SelectorStrategy, string>
        {
            [SelectorStrategy.DataTestId] = "[data-testid=\"test\"]"
        }
    );

    [Theory]
    [MemberData(nameof(AllActionTypes))]
    public void ActionStep_IsConstructible_ForAllSubtypes(ActionType actionType)
    {
        var step = new ActionStep(
            ActionType: actionType,
            Selector: TestSelector,
            Payload: new Dictionary<string, string>()
        );

        Assert.Equal(actionType, step.ActionType);
    }

    [Theory]
    [MemberData(nameof(AllAssertionTypes))]
    public void AssertionStep_IsConstructible_ForAllSubtypes(AssertionType assertionType)
    {
        var step = new AssertionStep(
            AssertionType: assertionType,
            Selector: TestSelector,
            Payload: new Dictionary<string, string>()
        );

        Assert.Equal(assertionType, step.AssertionType);
    }

    [Fact]
    public void GroupStep_ContainsChildSteps()
    {
        var childStep = new ActionStep(
            ActionType: ActionType.Click,
            Selector: TestSelector,
            Payload: new Dictionary<string, string>()
        );

        var groupStep = new GroupStep(
            GroupType: GroupType.Given,
            Steps: new List<IStep> { childStep }
        );

        Assert.Equal(1, groupStep.Steps.Count);
        Assert.Equal(GroupType.Given, groupStep.GroupType);
    }

    [Theory]
    [MemberData(nameof(AllGroupTypes))]
    public void GroupStep_IsConstructible_ForAllGroupTypes(GroupType groupType)
    {
        var step = new GroupStep(
            GroupType: groupType,
            Steps: new List<IStep>()
        );

        Assert.Equal(groupType, step.GroupType);
    }

    [Fact]
    public void ActionType_HasSixValues()
    {
        Assert.Equal(6, Enum.GetValues<ActionType>().Length);
    }

    [Fact]
    public void AssertionType_HasFiveValues()
    {
        Assert.Equal(5, Enum.GetValues<AssertionType>().Length);
    }
}
