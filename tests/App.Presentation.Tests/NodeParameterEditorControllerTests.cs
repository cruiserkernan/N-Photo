using App.Presentation.Controllers;
using Editor.Domain.Graph;

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
}
