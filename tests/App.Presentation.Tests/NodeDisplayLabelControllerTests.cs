using App.Presentation.Controllers;

namespace App.Presentation.Tests;

public class NodeDisplayLabelControllerTests
{
    [Theory]
    [InlineData("ImageInput", "II")]
    [InlineData("ExposureContrast", "EC")]
    [InlineData("Hsl", "HS")]
    [InlineData("Blur", "BL")]
    [InlineData("Sharpen", "SH")]
    [InlineData("Blend2", "B2")]
    public void GetNodeToolbarLabel_UsesGenericAcronymStrategy(string nodeType, string expected)
    {
        var label = NodeDisplayLabelController.GetNodeToolbarLabel(nodeType);

        Assert.Equal(expected, label);
    }

    [Fact]
    public void GetNodeToolbarLabel_ReturnsFallbackForEmpty()
    {
        var label = NodeDisplayLabelController.GetNodeToolbarLabel("   ");

        Assert.Equal("?", label);
    }
}
