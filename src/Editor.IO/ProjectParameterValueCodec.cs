using Editor.Domain.Graph;
using Editor.Domain.Imaging;

namespace Editor.IO;

public static class ProjectParameterValueCodec
{
    public static ProjectParameterValue FromDomain(ParameterValue value)
    {
        return value.Kind switch
        {
            ParameterValueKind.Float => new ProjectParameterValue
            {
                Kind = nameof(ParameterValueKind.Float),
                FloatValue = value.AsFloat()
            },
            ParameterValueKind.Integer => new ProjectParameterValue
            {
                Kind = nameof(ParameterValueKind.Integer),
                IntegerValue = value.AsInteger()
            },
            ParameterValueKind.Boolean => new ProjectParameterValue
            {
                Kind = nameof(ParameterValueKind.Boolean),
                BooleanValue = value.AsBoolean()
            },
            ParameterValueKind.Enum => new ProjectParameterValue
            {
                Kind = nameof(ParameterValueKind.Enum),
                EnumValue = value.AsEnum()
            },
            ParameterValueKind.Color => new ProjectParameterValue
            {
                Kind = nameof(ParameterValueKind.Color),
                ColorValue = new ProjectColorValue
                {
                    R = value.AsColor().R,
                    G = value.AsColor().G,
                    B = value.AsColor().B,
                    A = value.AsColor().A
                }
            },
            _ => throw new InvalidOperationException($"Unsupported parameter kind '{value.Kind}'.")
        };
    }

    public static bool TryToDomain(ProjectParameterValue serialized, out ParameterValue value, out string errorMessage)
    {
        value = default;
        errorMessage = string.Empty;

        if (serialized is null)
        {
            errorMessage = "Parameter value is required.";
            return false;
        }

        if (!Enum.TryParse<ParameterValueKind>(serialized.Kind, ignoreCase: false, out var kind))
        {
            errorMessage = $"Unsupported parameter kind '{serialized.Kind}'.";
            return false;
        }

        try
        {
            switch (kind)
            {
                case ParameterValueKind.Float:
                    if (!serialized.FloatValue.HasValue)
                    {
                        errorMessage = "Float parameter is missing FloatValue.";
                        return false;
                    }

                    value = ParameterValue.Float(serialized.FloatValue.Value);
                    return true;

                case ParameterValueKind.Integer:
                    if (!serialized.IntegerValue.HasValue)
                    {
                        errorMessage = "Integer parameter is missing IntegerValue.";
                        return false;
                    }

                    value = ParameterValue.Integer(serialized.IntegerValue.Value);
                    return true;

                case ParameterValueKind.Boolean:
                    if (!serialized.BooleanValue.HasValue)
                    {
                        errorMessage = "Boolean parameter is missing BooleanValue.";
                        return false;
                    }

                    value = ParameterValue.Boolean(serialized.BooleanValue.Value);
                    return true;

                case ParameterValueKind.Enum:
                    if (string.IsNullOrWhiteSpace(serialized.EnumValue))
                    {
                        errorMessage = "Enum parameter is missing EnumValue.";
                        return false;
                    }

                    value = ParameterValue.Enum(serialized.EnumValue);
                    return true;

                case ParameterValueKind.Color:
                    if (serialized.ColorValue is null)
                    {
                        errorMessage = "Color parameter is missing ColorValue.";
                        return false;
                    }

                    value = ParameterValue.Color(new RgbaColor(
                        serialized.ColorValue.R,
                        serialized.ColorValue.G,
                        serialized.ColorValue.B,
                        serialized.ColorValue.A));
                    return true;

                default:
                    errorMessage = $"Unsupported parameter kind '{serialized.Kind}'.";
                    return false;
            }
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }
}
