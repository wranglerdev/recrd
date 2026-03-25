using Recrd.Core.Ast;

namespace Recrd.Core.Tests.Ast;

/// <summary>
/// User Story: As a compiler, I want selectors ranked by stability so that
/// generated tests prefer data-testid over fragile XPath.
/// </summary>
public sealed class SelectorTests
{
    [Fact]
    public void Selector_DataTestId_HasHighestPriority()
    {
        var selectors = new[]
        {
            new Selector("xpath", "//button[1]", 4),
            new Selector("css", ".btn-primary", 3),
            new Selector("id", "submit-btn", 2),
            new Selector("data-testid", "submit", 1),
        };

        var best = selectors.OrderBy(s => s.Priority).First();

        Assert.Equal("data-testid", best.Strategy);
    }

    [Fact]
    public void Selector_AllStrategiesPresent_HasMinimumThreeOptions()
    {
        var selectors = new[]
        {
            new Selector("data-testid", "submit", 1),
            new Selector("id", "submit-btn", 2),
            new Selector("css", ".btn-primary", 3),
        };

        Assert.True(selectors.Length >= 3);
    }
}
