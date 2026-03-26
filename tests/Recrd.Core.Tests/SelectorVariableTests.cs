using System.Collections.Generic;
using Recrd.Core.Ast;
using Xunit;

namespace Recrd.Core.Tests;

public sealed class SelectorVariableTests
{
    [Fact]
    public void Selector_PriorityArray_RanksCorrectly()
    {
        var strategies = new List<SelectorStrategy>
        {
            SelectorStrategy.DataTestId,
            SelectorStrategy.Id,
            SelectorStrategy.Role,
            SelectorStrategy.Css,
            SelectorStrategy.XPath
        };

        var selector = new Selector(
            Strategies: strategies,
            Values: new Dictionary<SelectorStrategy, string>
            {
                [SelectorStrategy.DataTestId] = "[data-testid=\"btn\"]",
                [SelectorStrategy.Id] = "#btn",
                [SelectorStrategy.Role] = "button[name='Submit']",
                [SelectorStrategy.Css] = ".btn-primary",
                [SelectorStrategy.XPath] = "//button[@class='btn-primary']"
            }
        );

        Assert.Equal(SelectorStrategy.DataTestId, selector.Strategies[0]);
        Assert.Equal(SelectorStrategy.XPath, selector.Strategies[selector.Strategies.Count - 1]);
    }

    [Fact]
    public void Variable_ValidName_Succeeds()
    {
        var variable = new Variable("my_var_1");

        Assert.Equal("my_var_1", variable.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("1abc")]
    [InlineData("my-var")]
    [InlineData("abbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")] // 64 b's + 'a' prefix = 65 chars
    public void Variable_InvalidName_ThrowsArgumentException(string invalidName)
    {
        Assert.Throws<ArgumentException>(() => new Variable(invalidName));
    }

    [Fact]
    public void Variable_NameRegex_AcceptsBoundary()
    {
        // 1 char — minimum valid
        var single = new Variable("a");
        Assert.Equal("a", single.Name);

        // 64 chars — maximum valid (1 letter + 63 alphanumeric/underscore)
        var maxLength = new Variable("a" + new string('b', 63));
        Assert.Equal(64, maxLength.Name.Length);
    }

    [Fact]
    public void SelectorStrategy_HasFiveValues()
    {
        Assert.Equal(5, Enum.GetValues<SelectorStrategy>().Length);
    }
}
