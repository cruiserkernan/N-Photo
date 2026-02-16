using App.Presentation.Controllers;
using Avalonia.Controls;
using Editor.Domain.Graph;
using Editor.Domain.Imaging;

namespace App.Presentation.Tests;

public class ParameterEditorPrimitiveRegistryTests
{
    [Fact]
    public void Resolve_FloatWithRange_UsesSliderPrimitive()
    {
        var definition = new NodeParameterDefinition(
            "Exposure",
            ParameterValueKind.Float,
            ParameterValue.Float(0f),
            MinFloat: -2f,
            MaxFloat: 2f);

        var registry = ParameterEditorPrimitiveRegistry.CreateDefault();

        var primitive = registry.Resolve(definition, ParameterValue.Float(0.4f));

        Assert.Equal(ParameterEditorPrimitiveIds.Slider, primitive.Id);
    }

    [Fact]
    public void Resolve_ExplicitEditorPrimitive_OverridesDefault()
    {
        var definition = new NodeParameterDefinition(
            "Exposure",
            ParameterValueKind.Float,
            ParameterValue.Float(0f),
            MinFloat: -2f,
            MaxFloat: 2f,
            EditorPrimitive: ParameterEditorPrimitiveIds.NumberInput);

        var registry = ParameterEditorPrimitiveRegistry.CreateDefault();

        var primitive = registry.Resolve(definition, ParameterValue.Float(0.4f));

        Assert.Equal(ParameterEditorPrimitiveIds.NumberInput, primitive.Id);
    }

    [Fact]
    public void Resolve_ColorKind_UsesColorPrimitive()
    {
        var definition = new NodeParameterDefinition(
            "Tint",
            ParameterValueKind.Color,
            ParameterValue.Color(new RgbaColor(0.1f, 0.2f, 0.3f, 1f)));

        var registry = ParameterEditorPrimitiveRegistry.CreateDefault();

        var primitive = registry.Resolve(definition, ParameterValue.Color(new RgbaColor(1f, 0f, 0f, 1f)));

        Assert.Equal(ParameterEditorPrimitiveIds.Color, primitive.Id);
    }

    [Fact]
    public void Resolve_UnknownKindFallsBackToUnsupported()
    {
        var definition = new NodeParameterDefinition(
            "Exposure",
            ParameterValueKind.Float,
            ParameterValue.Float(0f),
            MinFloat: -2f,
            MaxFloat: 2f);

        var registry = new ParameterEditorPrimitiveRegistry(
            [
                new NeverMatchingPrimitive()
            ]);

        var primitive = registry.Resolve(definition, definition.DefaultValue);

        Assert.Equal("unsupported", primitive.Id);
    }

    [Fact]
    public void Resolve_InvalidExplicitPrimitiveStillUsesBestMatch()
    {
        var definition = new NodeParameterDefinition(
            "Exposure",
            ParameterValueKind.Float,
            ParameterValue.Float(0f),
            MinFloat: -2f,
            MaxFloat: 2f,
            EditorPrimitive: "not-real");

        var registry = ParameterEditorPrimitiveRegistry.CreateDefault();

        var primitive = registry.Resolve(definition, ParameterValue.Float(0.1f));

        Assert.Equal(ParameterEditorPrimitiveIds.Slider, primitive.Id);
    }

    private sealed class NeverMatchingPrimitive : IParameterEditorPrimitive
    {
        public string Id => "never";

        public bool CanCreate(NodeParameterDefinition definition, ParameterValue currentValue)
        {
            return false;
        }

        public Control Create(ParameterEditorPrimitiveContext context)
        {
            return new TextBlock();
        }
    }
}
