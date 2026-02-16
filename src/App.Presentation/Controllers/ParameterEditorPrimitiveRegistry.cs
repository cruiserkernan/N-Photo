using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

public sealed class ParameterEditorPrimitiveRegistry
{
    private readonly IReadOnlyList<IParameterEditorPrimitive> _primitives;
    private readonly Dictionary<string, IParameterEditorPrimitive> _byId;

    public ParameterEditorPrimitiveRegistry(IEnumerable<IParameterEditorPrimitive> primitives)
    {
        _primitives = primitives.ToArray();
        _byId = _primitives.ToDictionary(primitive => primitive.Id, StringComparer.OrdinalIgnoreCase);
    }

    public static ParameterEditorPrimitiveRegistry CreateDefault()
    {
        return new ParameterEditorPrimitiveRegistry(
            [
                new ColorParameterEditorPrimitive(),
                new BooleanParameterEditorPrimitive(),
                new EnumParameterEditorPrimitive(),
                new NumericSliderParameterEditorPrimitive(),
                new NumericInputParameterEditorPrimitive(),
                new UnsupportedParameterEditorPrimitive()
            ]);
    }

    public IParameterEditorPrimitive Resolve(NodeParameterDefinition definition, ParameterValue currentValue)
    {
        if (!string.IsNullOrWhiteSpace(definition.EditorPrimitive) &&
            _byId.TryGetValue(definition.EditorPrimitive, out var explicitPrimitive) &&
            explicitPrimitive.CanCreate(definition, currentValue))
        {
            return explicitPrimitive;
        }

        foreach (var primitive in _primitives)
        {
            if (primitive.CanCreate(definition, currentValue))
            {
                return primitive;
            }
        }

        return new UnsupportedParameterEditorPrimitive();
    }
}
