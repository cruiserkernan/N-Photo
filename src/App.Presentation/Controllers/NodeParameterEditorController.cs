using System.Globalization;
using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

public static class NodeParameterEditorController
{
    public static ParameterValue ParseTextParameterValue(NodeParameterDefinition definition, string rawText)
    {
        return definition.Kind switch
        {
            ParameterValueKind.Float => ParameterValue.Float(float.Parse(rawText, CultureInfo.InvariantCulture)),
            ParameterValueKind.Integer => ParameterValue.Integer(int.Parse(rawText, CultureInfo.InvariantCulture)),
            _ => throw new InvalidOperationException($"Unsupported text parameter kind '{definition.Kind}'.")
        };
    }

    public static string GetParameterLabel(NodeParameterDefinition definition)
    {
        var displayName = definition.Name;

        return definition.Kind switch
        {
            ParameterValueKind.Float when definition.MinFloat.HasValue || definition.MaxFloat.HasValue =>
                $"{displayName} ({definition.MinFloat?.ToString("0.###", CultureInfo.InvariantCulture) ?? "-inf"} .. {definition.MaxFloat?.ToString("0.###", CultureInfo.InvariantCulture) ?? "+inf"})",
            ParameterValueKind.Integer when definition.MinInt.HasValue || definition.MaxInt.HasValue =>
                $"{displayName} ({definition.MinInt?.ToString(CultureInfo.InvariantCulture) ?? "-inf"} .. {definition.MaxInt?.ToString(CultureInfo.InvariantCulture) ?? "+inf"})",
            _ => displayName
        };
    }

    public static string GetParameterHint(NodeParameterDefinition definition)
    {
        return definition.Kind switch
        {
            ParameterValueKind.Float => "Enter decimal value",
            ParameterValueKind.Integer => "Enter integer value",
            _ => string.Empty
        };
    }

    public static string FormatParameterValue(ParameterValue value)
    {
        return value.Kind switch
        {
            ParameterValueKind.Float => value.AsFloat().ToString("0.###", CultureInfo.InvariantCulture),
            ParameterValueKind.Integer => value.AsInteger().ToString(CultureInfo.InvariantCulture),
            ParameterValueKind.Boolean => value.AsBoolean().ToString(),
            ParameterValueKind.Enum => value.AsEnum(),
            _ => string.Empty
        };
    }
}
