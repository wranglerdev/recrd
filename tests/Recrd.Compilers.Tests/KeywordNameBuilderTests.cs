using Recrd.Compilers.Internal;
using Recrd.Core.Ast;
using Xunit;

namespace Recrd.Compilers.Tests;

public class KeywordNameBuilderTests
{
    [Fact]
    public void Click_DataTestId_ProducesCorrectName()
    {
        var result = KeywordNameBuilder.Build(ActionType.Click, "submit-btn");
        Assert.Equal("Clicar Em Submit Btn", result);
    }

    [Fact]
    public void Type_Id_ProducesCorrectName()
    {
        var result = KeywordNameBuilder.Build(ActionType.Type, "email-input");
        Assert.Equal("Digitar Em Email Input", result);
    }

    [Fact]
    public void Select_ProducesCorrectName()
    {
        var result = KeywordNameBuilder.Build(ActionType.Select, "country-dropdown");
        Assert.Equal("Selecionar Em Country Dropdown", result);
    }

    [Fact]
    public void Navigate_NoSlug()
    {
        var result = KeywordNameBuilder.Build(ActionType.Navigate, "");
        Assert.Equal("Navegar Para", result);
    }

    [Fact]
    public void Upload_ProducesCorrectName()
    {
        var result = KeywordNameBuilder.Build(ActionType.Upload, "file-input");
        Assert.Equal("Enviar Arquivo Em File Input", result);
    }

    [Fact]
    public void DragDrop_ProducesCorrectName()
    {
        var result = KeywordNameBuilder.Build(ActionType.DragDrop, "draggable");
        Assert.Equal("Arrastar Draggable", result);
    }
}
