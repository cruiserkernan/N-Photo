using Editor.Domain.Imaging;

namespace Editor.Domain.Graph;

public readonly record struct ParameterValue(ParameterValueKind Kind, object Value)
{
    public static ParameterValue Float(float value) => new(ParameterValueKind.Float, value);

    public static ParameterValue Integer(int value) => new(ParameterValueKind.Integer, value);

    public static ParameterValue Boolean(bool value) => new(ParameterValueKind.Boolean, value);

    public static ParameterValue Enum(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Enum parameter value cannot be empty.", nameof(value));
        }

        return new(ParameterValueKind.Enum, value);
    }

    public static ParameterValue Color(RgbaColor value) => new(ParameterValueKind.Color, value);

    public float AsFloat() => Kind == ParameterValueKind.Float
        ? (float)Value
        : throw new InvalidOperationException($"Parameter is '{Kind}', expected '{ParameterValueKind.Float}'.");

    public int AsInteger() => Kind == ParameterValueKind.Integer
        ? (int)Value
        : throw new InvalidOperationException($"Parameter is '{Kind}', expected '{ParameterValueKind.Integer}'.");

    public bool AsBoolean() => Kind == ParameterValueKind.Boolean
        ? (bool)Value
        : throw new InvalidOperationException($"Parameter is '{Kind}', expected '{ParameterValueKind.Boolean}'.");

    public string AsEnum() => Kind == ParameterValueKind.Enum
        ? (string)Value
        : throw new InvalidOperationException($"Parameter is '{Kind}', expected '{ParameterValueKind.Enum}'.");

    public RgbaColor AsColor() => Kind == ParameterValueKind.Color
        ? (RgbaColor)Value
        : throw new InvalidOperationException($"Parameter is '{Kind}', expected '{ParameterValueKind.Color}'.");
}
