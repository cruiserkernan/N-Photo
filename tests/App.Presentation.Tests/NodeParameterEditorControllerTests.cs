using App.Presentation.Controllers;
using Editor.Domain.Graph;
using Editor.Domain.Imaging;

namespace App.Presentation.Tests;

public class NodeParameterEditorControllerTests
{
    [Fact]
    public void ParseTextParameterValue_ParsesFloat()
    {
        var definition = new NodeParameterDefinition(
            "Exposure",
            ParameterValueKind.Float,
            ParameterValue.Float(0f),
            MinFloat: -5,
            MaxFloat: 5);

        var parsed = NodeParameterEditorController.ParseTextParameterValue(definition, "1.25");

        Assert.Equal(1.25f, parsed.AsFloat());
    }

    [Fact]
    public void GetParameterLabel_UsesParameterName()
    {
        var definition = new NodeParameterDefinition(
            "Radius",
            ParameterValueKind.Integer,
            ParameterValue.Integer(2),
            MinInt: 0,
            MaxInt: 32);

        var label = NodeParameterEditorController.GetParameterLabel(definition);

        Assert.StartsWith("Radius", label, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatParameterValue_FormatsInteger()
    {
        var text = NodeParameterEditorController.FormatParameterValue(ParameterValue.Integer(7));

        Assert.Equal("7", text);
    }

    [Fact]
    public void ParseTextParameterValue_ParsesColorHex()
    {
        var definition = new NodeParameterDefinition(
            "Tint",
            ParameterValueKind.Color,
            ParameterValue.Color(new RgbaColor(0, 0, 0, 1)));

        var parsed = NodeParameterEditorController.ParseTextParameterValue(definition, "#3366CC80");
        var color = parsed.AsColor();

        Assert.Equal(0.2f, color.R, precision: 2);
        Assert.Equal(0.4f, color.G, precision: 2);
        Assert.Equal(0.8f, color.B, precision: 2);
        Assert.Equal(0.5f, color.A, precision: 2);
    }
}
