using System.Globalization;
using Editor.Domain.Imaging;
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
            ParameterValueKind.Color => ParameterValue.Color(ParseColor(rawText)),
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
            ParameterValueKind.Color => "Use #RRGGBB or #RRGGBBAA",
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
            ParameterValueKind.Color => FormatColor(value.AsColor()),
            _ => string.Empty
        };
    }

    public static string FormatColor(RgbaColor color)
    {
        var clamped = RgbaColor.Clamp(color);
        return string.Create(
            9,
            clamped,
            static (buffer, source) =>
            {
                buffer[0] = '#';
                WriteByteHex(buffer, 1, ToByte(source.R));
                WriteByteHex(buffer, 3, ToByte(source.G));
                WriteByteHex(buffer, 5, ToByte(source.B));
                WriteByteHex(buffer, 7, ToByte(source.A));
            });
    }

    public static RgbaColor ParseColor(string rawText)
    {
        var text = (rawText ?? string.Empty).Trim();
        if (text.StartsWith('#'))
        {
            text = text[1..];
        }

        if (text.Length is not (6 or 8))
        {
            throw new FormatException("Expected #RRGGBB or #RRGGBBAA.");
        }

        var r = ParseHexByte(text, 0);
        var g = ParseHexByte(text, 2);
        var b = ParseHexByte(text, 4);
        var a = text.Length == 8 ? ParseHexByte(text, 6) : (byte)255;

        return new RgbaColor(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
    }

    private static byte ParseHexByte(string text, int index)
    {
        var high = ParseHexDigit(text[index]);
        var low = ParseHexDigit(text[index + 1]);
        return (byte)((high << 4) | low);
    }

    private static int ParseHexDigit(char value)
    {
        return value switch
        {
            >= '0' and <= '9' => value - '0',
            >= 'a' and <= 'f' => 10 + (value - 'a'),
            >= 'A' and <= 'F' => 10 + (value - 'A'),
            _ => throw new FormatException("Invalid hex color value.")
        };
    }

    private static void WriteByteHex(Span<char> buffer, int index, byte value)
    {
        const string digits = "0123456789ABCDEF";
        buffer[index] = digits[value >> 4];
        buffer[index + 1] = digits[value & 0x0F];
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp((int)Math.Round(RgbaColor.Clamp01(value) * 255.0f), 0, 255);
    }
}
